using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoCutMarkerEncoder.Models;

namespace VideoCutMarkerEncoder.Services
{
    /// <summary>
    /// 처리 작업 정보
    /// </summary>
    public class ProcessingTask
    {
        public VideoEditMetadata Metadata { get; set; }
        public string FilePath { get; set; }
        public string OutputPath { get; set; }
        public string TaskId { get; set; } = Guid.NewGuid().ToString();
        public string Status { get; set; } = "대기 중";
        public int Progress { get; set; } = 0;
    }

    /// <summary>
    /// 처리 진행 상황 이벤트 인자
    /// </summary>
    public class ProcessingProgressEventArgs : EventArgs
    {
        public VideoEditMetadata Metadata { get; set; }
        public string TaskId { get; set; }
        public int Progress { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// 처리 완료 이벤트 인자
    /// </summary>
    public class ProcessingCompletedEventArgs : EventArgs
    {
        public VideoEditMetadata Metadata { get; set; }
        public string TaskId { get; set; }
        public string OutputFilePath { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// 메타데이터 파서 - VCM 파일 해석
    /// </summary>
    public static class MetadataParser
    {
        public static VideoEditMetadata ParseMetadataFile(string filePath)
        {
            try
            {
                // 파일 읽기
                string jsonContent = File.ReadAllText(filePath);

                // JSON 역직렬화
                var metadata = JsonSerializer.Deserialize<VideoEditMetadata>(jsonContent);

                if (metadata == null)
                {
                    throw new Exception("메타데이터 파싱 실패: 유효하지 않은 JSON 형식");
                }

                // 비디오 파일 경로 확인 및 업데이트
                FindVideoFilePath(metadata, filePath);

                return metadata;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"메타데이터 파싱 오류: {ex.Message}");
                throw;
            }
        }

        private static void FindVideoFilePath(VideoEditMetadata metadata, string metadataFilePath)
        {
            if (!string.IsNullOrEmpty(metadata.VideoPath))
            {
                Debug.WriteLine($"SMB 비디오 감지: {metadata.VideoPath}");

                // SMB 경로를 Windows UNC 경로로 변환
                // smb://192.168.50.123/Downloads/video.mp4
                // → \\192.168.50.123\Downloads\video.mp4
                string normalizedUri = metadata.VideoPath;
                if (normalizedUri.StartsWith("smb:/") && !normalizedUri.StartsWith("smb://"))
                {
                    normalizedUri = normalizedUri.Replace("smb:/", "smb://");
                    Debug.WriteLine($"URI 정규화: {normalizedUri}");
                }

                Uri uri = new Uri(normalizedUri);
                string host = uri.Host; // "192.168.50.123"
                string path = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')); // "Downloads/video.mp4"

                string windowsPath = $"\\\\{host}\\{path.Replace('/', '\\')}";

                Debug.WriteLine($"Windows UNC 경로: {windowsPath}");

                if (File.Exists(windowsPath))
                {
                    metadata.VideoPath = windowsPath;
                    Debug.WriteLine("✅ SMB 비디오 파일 확인됨");
                    return;
                }
                else
                {
                    throw new FileNotFoundException($"SMB 경로에서 비디오를 찾을 수 없습니다: {windowsPath}");
                }
            }
            // 이미 유효한 경로가 있고 파일이 존재하면 사용
            if (!string.IsNullOrEmpty(metadata.VideoPath) && File.Exists(metadata.VideoPath))
            {
                return;
            }

            string directory = Path.GetDirectoryName(metadataFilePath);
            string videoFileName = metadata.VideoFileName;

            // [VCM_xxxx] 태그 제거
            videoFileName = System.Text.RegularExpressions.Regex.Replace(videoFileName, @"\[VCM_[a-zA-Z0-9]+\]", "");

            // 1. 동일한 이름으로 찾기
            string possiblePath = Path.Combine(directory, videoFileName);
            if (File.Exists(possiblePath))
            {
                metadata.VideoPath = possiblePath;
                return;
            }

            // 2. 확장자를 제외한 파일명으로 검색
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFileName);
            string[] videoFiles = Directory.GetFiles(directory, fileNameWithoutExt + ".*");

            foreach (string file in videoFiles)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (IsVideoFile(ext))
                {
                    metadata.VideoPath = file;
                    return;
                }
            }

            // 3. 디렉토리의 모든 비디오 파일 검색
            videoFiles = Directory.GetFiles(directory);
            foreach (string file in videoFiles)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (IsVideoFile(ext))
                {
                    metadata.VideoPath = file;
                    return;
                }
            }

            throw new FileNotFoundException($"비디오 파일을 찾을 수 없습니다: {videoFileName}");
        }

        private static bool IsVideoFile(string extension)
        {
            string[] videoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v" };
            return Array.IndexOf(videoExtensions, extension.ToLower()) >= 0;
        }
    }

    /// <summary>
    /// 비디오 처리 서비스 - FFmpeg 통합 (메타데이터 기반 인코딩)
    /// </summary>
    public class VideoProcessor
    {
        private readonly string _ffmpegPath;
        private readonly Queue<ProcessingTask> _processingQueue = new Queue<ProcessingTask>();
        private bool _isProcessing;
        private readonly SettingsManager _settingsManager;

        /// <summary>
        /// 처리 진행 상황 이벤트
        /// </summary>
        public event EventHandler<ProcessingProgressEventArgs> ProcessingProgress;

        /// <summary>
        /// 처리 완료 이벤트
        /// </summary>
        public event EventHandler<ProcessingCompletedEventArgs> ProcessingCompleted;

        /// <summary>
        /// 생성자
        /// </summary>
        public VideoProcessor(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;

            // FFmpeg 경로 설정 (앱 폴더 내에 포함된 FFmpeg 사용)
            _ffmpegPath = _settingsManager.Settings.FFmpegPath;

            // FFmpeg가 없으면 다운로드 또는 오류 표시
            CheckFFmpegAvailability();
        }

        /// <summary>
        /// FFmpeg 사용 가능 여부 확인
        /// </summary>
        private void CheckFFmpegAvailability()
        {
            if (!File.Exists(_ffmpegPath))
            {
                // FFmpeg 폴더 생성
                string ffmpegFolder = Path.GetDirectoryName(_ffmpegPath);
                if (!Directory.Exists(ffmpegFolder))
                {
                    Directory.CreateDirectory(ffmpegFolder);
                }

                // 경고 메시지 표시
                Debug.WriteLine("경고: FFmpeg가 없습니다. 비디오 처리를 위해 FFmpeg가 필요합니다.");
            }
        }

        /// <summary>
        /// 처리 작업 큐에 추가
        /// </summary>
        public void EnqueueTask(ProcessingTask task)
        {
            // FFmpeg 존재 확인 - 없으면 처리하지 않음
            if (!File.Exists(_ffmpegPath))
            {
                MessageBox.Show(
                    "FFmpeg가 설치되어 있지 않아 비디오를 처리할 수 없습니다.\n\n" +
                    "설정 메뉴에서 FFmpeg 설치 안내를 확인하세요.",
                    "FFmpeg 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                ProcessingCompleted?.Invoke(this, new ProcessingCompletedEventArgs
                {
                    Metadata = task.Metadata,
                    TaskId = task.TaskId,
                    ErrorMessage = "FFmpeg가 설치되어 있지 않습니다.",
                    Success = false
                });

                return;
            }

            // 기존 코드 (작업 큐에 추가)
            _processingQueue.Enqueue(task);

            // 처리 시작 (아직 처리 중이 아니면)
            if (!_isProcessing)
            {
                ProcessNextTask();
            }
        }

        /// <summary>
        /// 다음 작업 처리
        /// </summary>
        private async void ProcessNextTask()
        {
            if (_processingQueue.Count == 0)
            {
                _isProcessing = false;
                return;
            }

            _isProcessing = true;
            var task = _processingQueue.Dequeue();

            try
            {
                // 진행 상황 업데이트
                UpdateProgress(task, 0, "처리 시작");

                // FFmpeg 존재 확인
                if (!File.Exists(_ffmpegPath))
                {
                    throw new FileNotFoundException("FFmpeg를 찾을 수 없습니다. 비디오 처리를 위해 FFmpeg가 필요합니다.");
                }

                // 비디오 파일 존재 확인
                if (!File.Exists(task.Metadata.VideoPath))
                {
                    throw new FileNotFoundException($"비디오 파일을 찾을 수 없습니다: {task.Metadata.VideoPath}");
                }

                // 비디오 처리
                string outputPath = await ProcessVideoAsync(task);

                // 작업 완료 이벤트 발생
                task.Status = "완료";
                task.Progress = 100;
                task.OutputPath = outputPath;

                ProcessingCompleted?.Invoke(this, new ProcessingCompletedEventArgs
                {
                    Metadata = task.Metadata,
                    TaskId = task.TaskId,
                    OutputFilePath = outputPath,
                    Success = true
                });

                // ⭐ 설정에 따라 조건부 Share 폴더 정리
                if (_settingsManager.Settings.AutoDeleteShareFiles)
                {
                    CleanupShareFiles(task);
                    Debug.WriteLine("자동 삭제 설정이 활성화되어 Share 폴더 파일이 삭제되었습니다.");
                }
                else
                {
                   // Debug.WriteLine("자동 삭제 설정이 비활성화되어 Share 폴더 파일이 보존되었습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"비디오 처리 오류: {ex.Message}");

                // 작업 실패 이벤트 발생
                task.Status = "실패";
                task.Progress = 0;

                ProcessingCompleted?.Invoke(this, new ProcessingCompletedEventArgs
                {
                    Metadata = task.Metadata,
                    TaskId = task.TaskId,
                    ErrorMessage = ex.Message,
                    Success = false
                });
            }

            // 다음 작업 처리
            ProcessNextTask();
        }

        private void CleanupShareFiles(ProcessingTask task)
        {
            try
            {
                // 메타데이터 파일 삭제
                if (File.Exists(task.FilePath))
                {
                    File.Delete(task.FilePath);
                }

                // 비디오 파일도 Share 폴더에 있다면 삭제
                string videoFileName = Path.GetFileName(task.Metadata.VideoPath);
                string shareVideoPath = Path.Combine(_settingsManager.Settings.ShareFolder, videoFileName);

                if (File.Exists(shareVideoPath))
                {
                    File.Delete(shareVideoPath);
                }

                Debug.WriteLine($"Share 폴더 정리 완료: {task.Metadata.VideoFileName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Share 폴더 정리 오류: {ex.Message}");
                // 정리 실패해도 메인 작업에는 영향 없음
            }
        }

        /// <summary>
        /// 비디오 처리 (FFmpeg 사용) - 모든 그룹별로 개별 파일 생성
        /// </summary>
        private async Task<string> ProcessVideoAsync(ProcessingTask task)
        {
            var metadata = task.Metadata;

            try
            {
                string outputFolder;
                bool isSmbVideo = metadata.VideoPath.StartsWith("\\\\");

                if (isSmbVideo)
                {
                    // SMB 비디오: 원본과 같은 폴더에 출력
                    outputFolder = Path.GetDirectoryName(metadata.VideoPath);
                    Debug.WriteLine($"SMB 비디오 - 출력 폴더: {outputFolder}");
                }
                else
                {
                    // 일반 비디오: Output 폴더에 출력
                    outputFolder = _settingsManager.Settings.OutputFolder;
                    Debug.WriteLine($"일반 비디오 - 출력 폴더: {outputFolder}");
                }
                if (metadata.OutputMode == OutputMode.Merge)
                {
                    return await ProcessMergeMode(metadata, task, outputFolder);
                }
                else
                {
                    return await ProcessSeparateMode(metadata, task, outputFolder);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"비디오 처리 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Separate 모드 처리 (기존 방식 + 회전 추가)
        /// </summary>
        private async Task<string> ProcessSeparateMode(VideoEditMetadata metadata, ProcessingTask task,string outputFolder)
        {
            var outputFiles = new List<string>();
            var groupSegments = metadata.Segments.GroupBy(s => s.GroupId).ToList();
            int totalGroups = groupSegments.Count();
            int currentGroupIndex = 0;

            foreach (var groupData in groupSegments)
            {
                int groupId = groupData.Key;
                if (groupId == 0) continue; // 미선택 그룹 건너뛰기

                var segments = groupData.OrderBy(s => s.StartTime).ToList();
                if (!metadata.Groups.TryGetValue(groupId, out GroupInfo groupInfo))
                    continue;

                var segmentFiles = new List<string>();

                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    double startTime = segment.StartTime;
                    double endTime = segment.EndTime;

                    // 크롭 영역 계산
                    int cropX = segment.CenterX - (groupInfo.Width / 2);
                    int cropY = (metadata.VideoHeight - segment.CenterY) - (groupInfo.Height / 2);
                    cropX = Math.Max(0, cropX);
                    cropY = Math.Max(0, cropY);

                    // 세그먼트 파일 경로
                    string segmentFilePath = Path.Combine(outputFolder,
                        $"segment_{task.TaskId}_group{groupId}_{i}.mp4");
                    segmentFiles.Add(segmentFilePath);

                    // FFmpeg 명령 생성 (회전 포함)
                    string ffmpegArgs = BuildFFmpegCommand(metadata, startTime, endTime,
                        cropX, cropY, groupInfo, segmentFilePath);

                    // 진행률 계산
                    int overallProgress = (currentGroupIndex * 100 / totalGroups) +
                        ((i * 100 / totalGroups) / segments.Count);

                    // FFmpeg 실행
                    UpdateProgress(task, overallProgress,
                        $"그룹 {groupId} - 세그먼트 {i + 1}/{segments.Count} 인코딩 중");
                    bool success = await RunFFmpegProcessAsync(ffmpegArgs);

                    if (!success)
                        throw new Exception($"그룹 {groupId} 세그먼트 {i + 1} 처리 실패");
                }

                // 그룹 파일명 생성
                string baseName = Path.GetFileNameWithoutExtension(metadata.VideoFileName);
                string extension = ".mp4";
                string prefix = metadata.EncodingSettings?.OutputPrefix ?? "";
                string groupSuffix = totalGroups > 1 ? $"_group{groupId}" : "";
                string suffix = metadata.EncodingSettings?.OutputSuffix ?? "";
                string finalsuffix = $"{suffix}{groupSuffix}";
                string outputFileName = GenerateUniqueFileName(prefix, baseName, finalsuffix, extension);
                string outputPath = Path.Combine(outputFolder, outputFileName);

                // 그룹 내 세그먼트 병합 (필요시)
                await MergeSegmentsIfNeeded(segmentFiles, outputPath, task, groupId);
                outputFiles.Add(outputPath);
                currentGroupIndex++;
            }

            UpdateProgress(task, 100, $"모든 그룹 처리 완료 ({outputFiles.Count}개 파일 생성)");
            return outputFiles.FirstOrDefault() ?? "";
        }

        /// <summary>
        /// Merge 모드 처리 - 시간순 정렬 + 기준 해상도 맞춤
        /// </summary>
        private async Task<string> ProcessMergeMode(VideoEditMetadata metadata, ProcessingTask task, string outputFolder)
        {
            // 기준 해상도 확인
            if (metadata.ReferenceResolution == null)
                throw new Exception("Merge 모드에는 기준 해상도가 필요합니다.");

            // 모든 세그먼트를 시간순으로 정렬
            var allSegments = GetSegmentsSortedByTime(metadata);
            if (allSegments.Count == 0)
                throw new Exception("병합할 세그먼트가 없습니다.");

            var tempSegmentFiles = new List<string>();

            // 각 세그먼트 처리
            for (int i = 0; i < allSegments.Count; i++)
            {
                var segment = allSegments[i];
                if (!metadata.Groups.TryGetValue(segment.GroupId, out GroupInfo groupInfo))
                    continue;

                // 임시 세그먼트 파일 경로
                string tempFilePath = Path.Combine(outputFolder,
                    $"temp_merge_{task.TaskId}_{i}.mp4");
                tempSegmentFiles.Add(tempFilePath);

                // 크롭 영역 계산
                int cropX = segment.CenterX - (groupInfo.Width / 2);
                int cropY = (metadata.VideoHeight - segment.CenterY) - (groupInfo.Height / 2);
                cropX = Math.Max(0, cropX);
                cropY = Math.Max(0, cropY);

                // Merge용 FFmpeg 명령 생성 (회전 + 스케일링 + 패딩)
                string ffmpegArgs = BuildMergeFFmpegCommand(metadata, segment.StartTime, segment.EndTime,
                    cropX, cropY, groupInfo, tempFilePath);

                // 진행률 업데이트
                int progress = (i * 80 / allSegments.Count); // 80%까지 개별 세그먼트 처리
                UpdateProgress(task, progress, $"세그먼트 {i + 1}/{allSegments.Count} 처리 중");

                // FFmpeg 실행
                bool success = await RunFFmpegProcessAsync(ffmpegArgs);
                if (!success)
                    throw new Exception($"세그먼트 {i + 1} 처리 실패");
            }

            // 최종 병합
            string finalOutputPath = await MergeFinalVideo(metadata, tempSegmentFiles, task, outputFolder);

            // 임시 파일 정리
            foreach (string tempFile in tempSegmentFiles)
            {
                try { File.Delete(tempFile); } catch { }
            }

            UpdateProgress(task, 100, "Merge 완료");
            return finalOutputPath;
        }

        /// <summary>
        /// 시간순으로 정렬된 세그먼트 목록 반환
        /// </summary>
        private List<CropSegmentInfo> GetSegmentsSortedByTime(VideoEditMetadata metadata)
        {
            return metadata.Segments
                .Where(s => s.GroupId != 0) // 미선택 그룹 제외
                .OrderBy(s => s.StartTime)
                .ThenBy(s => s.EndTime)
                .ToList();
        }

        /// <summary>
        /// Separate 모드용 FFmpeg 명령 생성 (회전 포함)
        /// </summary>
        private string BuildFFmpegCommand(VideoEditMetadata metadata, double startTime, double endTime,
            int cropX, int cropY, GroupInfo groupInfo, string outputPath)
        {
            var args = new StringBuilder();
            args.Append($"-y -ss {startTime} -i \"{metadata.VideoPath}\" -t {endTime - startTime} ");

            // ✅ Copy 코덱이면 필터 없이 스트림 복사만
            string videoCodec = GetVideoCodec(metadata);
            if (videoCodec == "copy")
            {
                args.Append("-c copy ");
                args.Append($"\"{outputPath}\"");
                Debug.WriteLine($"Copy FFmpeg 명령 (필터 없음): {args}");
                return args.ToString();
            }

            // 비디오 필터 체인 구성
            var filters = new List<string>();

            // 1. 크롭 필터
            filters.Add($"crop={groupInfo.Width}:{groupInfo.Height}:{cropX}:{cropY}");

            // 2. 회전 필터 추가
            string rotationFilter = GetRotationFilter(groupInfo.Rotation);
            if (!string.IsNullOrEmpty(rotationFilter))
            {
                filters.Add(rotationFilter);
            }
            // 3. ✅ 스케일링 필터 (조건부 체크를 C#에서 미리 처리)
            if (metadata.EncodingSettings.EnableScaling)
            {
                string optimizedScaleFilter = GetOptimizedScaleFilter(metadata);
                if (!string.IsNullOrEmpty(optimizedScaleFilter))
                {
                    filters.Add(optimizedScaleFilter);
                    System.Diagnostics.Debug.WriteLine($"최적화된 스케일링 필터: {optimizedScaleFilter}");
                }
            }
            // 필터 체인 적용
            if (filters.Count > 0)
            {
                args.Append($"-vf \"{string.Join(",", filters)}\" ");
            }
            
            // 인코딩 설정
            AppendEncodingSettings(args, metadata);
            args.Append($"\"{outputPath}\"");

            Debug.WriteLine($"Separate FFmpeg 명령: {args}");
            return args.ToString();
        }

        /// <summary>
        /// 조건부 스케일링을 미리 계산하여 고정값 필터로 변환
        /// </summary>
        private string GetOptimizedScaleFilter(VideoEditMetadata metadata)
        {
            string originalFilter = metadata.EncodingSettings.ScaleFilter;

            if (string.IsNullOrEmpty(originalFilter))
                return "";

            try
            {
                // 높이 기준 조건부 스케일링 처리
                if (originalFilter.Contains("if(gt(ih,") && originalFilter.Contains("),") && originalFilter.Contains(",ih)"))
                {
                    // scale=-1:'if(gt(ih,1300),1280,ih)' 패턴 파싱
                    var match = System.Text.RegularExpressions.Regex.Match(
                        originalFilter,
                        @"scale=-1:'if\(gt\(ih,(\d+)\),(\d+),ih\)'"
                    );

                    if (match.Success)
                    {
                        int conditionHeight = int.Parse(match.Groups[1].Value);  // 1300
                        int targetHeight = int.Parse(match.Groups[2].Value);     // 1280

                        // ✅ 조건을 C#에서 미리 계산
                        int videoHeight = metadata.VideoHeight; // 또는 다른 방법으로 가져오기
                        int finalHeight = videoHeight > conditionHeight ? targetHeight : videoHeight;

                        System.Diagnostics.Debug.WriteLine($"스케일링 최적화: {videoHeight}px > {conditionHeight}px ? {targetHeight}px : {videoHeight}px = {finalHeight}px");

                        return $"scale=-1:{finalHeight}";
                    }
                }

                // 너비 기준 조건부 스케일링 처리
                if (originalFilter.Contains("if(gt(iw,") && originalFilter.Contains("),") && originalFilter.Contains(",iw)"))
                {
                    // scale='if(gt(iw,2560),1920,iw)':-1 패턴 파싱
                    var match = System.Text.RegularExpressions.Regex.Match(
                        originalFilter,
                        @"scale='if\(gt\(iw,(\d+)\),(\d+),iw\)':-1"
                    );

                    if (match.Success)
                    {
                        int conditionWidth = int.Parse(match.Groups[1].Value);
                        int targetWidth = int.Parse(match.Groups[2].Value);

                        int videoWidth = metadata.VideoWidth;
                        int finalWidth = videoWidth > conditionWidth ? targetWidth : videoWidth;

                        return $"scale={finalWidth}:-1";
                    }
                }

                // 조건부가 아닌 일반 스케일링은 그대로 반환
                return originalFilter;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"스케일링 필터 최적화 오류: {ex.Message}");
                return originalFilter; // 오류시 원본 사용
            }
        }

        private string SwapScaleDimensions(string scaleFilter)
        {
            // "scale=1280:720" → "scale=720:1280"
            if (scaleFilter.StartsWith("scale="))
            {
                var dimensions = scaleFilter.Substring(6).Split(':');
                if (dimensions.Length == 2)
                {
                    return $"scale={dimensions[1]}:{dimensions[0]}";
                }
            }
            return scaleFilter; // 파싱 실패 시 원본 반환
        }

        /// <summary>
        /// Merge 모드용 FFmpeg 명령 생성 (회전 + 스케일링 + 패딩)
        /// </summary>
        private string BuildMergeFFmpegCommand(VideoEditMetadata metadata, double startTime, double endTime,
            int cropX, int cropY, GroupInfo groupInfo, string outputPath)
        {
            var args = new StringBuilder();
            args.Append($"-y -ss {startTime} -i \"{metadata.VideoPath}\" -t {endTime - startTime} ");

            // 복합 필터 체인 구성
            var filters = new List<string>();

            // 1. 크롭
            filters.Add($"crop={groupInfo.Width}:{groupInfo.Height}:{cropX}:{cropY}");

            // 2. 회전 (회전 후 크기 변경 고려)
            string rotationFilter = GetRotationFilter(groupInfo.Rotation);
            if (!string.IsNullOrEmpty(rotationFilter))
            {
                filters.Add(rotationFilter);
            }

            // 3. 스케일링 + 패딩 (기준 해상도에 맞춤)
            string scaleAndPadFilter = GetScaleAndPadFilter(groupInfo, metadata.ReferenceResolution);
            if (!string.IsNullOrEmpty(scaleAndPadFilter))
            {
                filters.Add(scaleAndPadFilter);
            }

            // 필터 체인 적용
            if (filters.Count > 0)
            {
                args.Append($"-vf \"{string.Join(",", filters)}\" ");
            }

            // 인코딩 설정
            AppendEncodingSettings(args, metadata);
            args.Append($"\"{outputPath}\"");

            Debug.WriteLine($"Merge FFmpeg 명령: {args}");
            return args.ToString();
        }

        /// <summary>
        /// 회전 필터 문자열 생성
        /// </summary>
        private string GetRotationFilter(Rotation rotation)
        {
            return rotation switch
            {
                Rotation.CW90 => "transpose=1",      // 90도 시계방향
                Rotation.CW180 => "transpose=2,transpose=2", // 180도 (90도 두번)
                Rotation.CW270 => "transpose=2",     // 270도 시계방향 (90도 반시계방향)
                _ => ""  // 회전 없음
            };
        }

        /// <summary>
        /// 스케일링 + 패딩 필터 생성 (비율 유지)
        /// </summary>
        private string GetScaleAndPadFilter(GroupInfo groupInfo, ReferenceResolution reference)
        {
            // 회전된 실제 크기 계산
            var (actualWidth, actualHeight) = groupInfo.GetRotatedSize();

            // 기준 해상도와 같으면 스케일링 불필요
            if (actualWidth == reference.Width && actualHeight == reference.Height)
                return "";

            // 스케일링 비율 계산 (작은 쪽에 맞춤 - 비율 유지)
            double scaleRatio = Math.Min(
                (double)reference.Width / actualWidth,
                (double)reference.Height / actualHeight
            );

            int scaledWidth = (int)(actualWidth * scaleRatio);
            int scaledHeight = (int)(actualHeight * scaleRatio);

            // 짝수로 맞춤 (인코딩 오류 방지)
            scaledWidth = (scaledWidth / 2) * 2;
            scaledHeight = (scaledHeight / 2) * 2;

            // scale + pad 필터 조합
            return $"scale={scaledWidth}:{scaledHeight}," +
                   $"pad={reference.Width}:{reference.Height}:" +
                   $"{(reference.Width - scaledWidth) / 2}:" +
                   $"{(reference.Height - scaledHeight) / 2}:black";
        }

        /// <summary>
        /// 세그먼트 병합 (필요시)
        /// </summary>
        private async Task MergeSegmentsIfNeeded(List<string> segmentFiles, string outputPath,
            ProcessingTask task, int groupId)
        {
            if (segmentFiles.Count > 1)
            {
                // 파일 목록 생성
                string listFilePath = Path.Combine(_settingsManager.Settings.OutputFolder,
                    $"segments_group{groupId}_{task.TaskId}.txt");
                using (StreamWriter writer = new StreamWriter(listFilePath))
                {
                    foreach (string file in segmentFiles)
                        writer.WriteLine($"file '{file}'");
                }

                // 병합 명령
                string concatArgs = $"-y -f concat -safe 0 -i \"{listFilePath}\" -c copy \"{outputPath}\"";

                // FFmpeg 실행
                bool success = await RunFFmpegProcessAsync(concatArgs);
                if (!success)
                    throw new Exception($"그룹 {groupId} 세그먼트 병합 실패");

                // 임시 파일 삭제
                foreach (string file in segmentFiles)
                    try { File.Delete(file); } catch { }
                try { File.Delete(listFilePath); } catch { }
            }
            else if (segmentFiles.Count == 1)
            {
                // 세그먼트가 하나면 이름만 변경
                File.Move(segmentFiles[0], outputPath, true);
            }
        }

        /// <summary>
        /// 최종 비디오 병합 (Merge 모드)
        /// </summary>
        private async Task<string> MergeFinalVideo(VideoEditMetadata metadata,
            List<string> tempFiles, ProcessingTask task,string outputFolder)
        {
            // 최종 출력 파일 경로
            string baseName = Path.GetFileNameWithoutExtension(metadata.VideoFileName);
            string prefix = metadata.EncodingSettings?.OutputPrefix ?? "";
            string suffix = metadata.EncodingSettings?.OutputSuffix ?? "";
            string finalSuffix = $"{suffix}_merged";
            string finalFileName = GenerateUniqueFileName(prefix, baseName, finalSuffix, ".mp4");
            string finalPath = Path.Combine(outputFolder, finalFileName);

            if (tempFiles.Count == 1)
            {
                // 파일이 하나면 이름만 변경
                File.Move(tempFiles[0], finalPath, true);
            }
            else
            {
                // 여러 파일 병합
                string listFilePath = Path.Combine(outputFolder,
                    $"final_merge_{task.TaskId}.txt");

                using (StreamWriter writer = new StreamWriter(listFilePath))
                {
                    foreach (string file in tempFiles)
                        writer.WriteLine($"file '{file}'");
                }

                string concatArgs = $"-y -f concat -safe 0 -i \"{listFilePath}\" -c copy \"{finalPath}\"";

                UpdateProgress(task, 90, "최종 병합 중...");
                bool success = await RunFFmpegProcessAsync(concatArgs);

                if (!success)
                    throw new Exception("최종 병합 실패");

                try { File.Delete(listFilePath); } catch { }
            }

            return finalPath;
        }

        /// <summary>
        /// 인코딩 설정 추가
        /// </summary>
        private void AppendEncodingSettings(StringBuilder args, VideoEditMetadata metadata)
        {
            // 비디오 코덱
            string videoCodec = GetVideoCodec(metadata);
            args.Append($"-c:v {videoCodec} ");
            // ✅ Copy 코덱이면 CQ, FPS 옵션 무시
            if (videoCodec == "copy")
            {
                // 오디오 코덱만 추가
                string audioCodec1 = GetAudioCodec(metadata);
                args.Append($"-c:a {audioCodec1} ");
                return;
            }
            // ✅ FPS 제한 옵션 추가
            if (metadata.EncodingSettings?.LimitFrameRate == true)
            {
                args.Append($"-r {metadata.EncodingSettings.TargetFps} ");
            }
            // 품질 설정
            int cq = metadata.EncodingSettings?.CQ > 0 ?
                metadata.EncodingSettings.CQ : _settingsManager.Settings.VideoQuality;
            args.Append($"-cq {cq} ");

            // 오디오 코덱
            string audioCodec = GetAudioCodec(metadata);
            args.Append($"-c:a {audioCodec} ");
        }

        /// <summary>
        /// 고유한 파일명 생성 (덮어쓰기 방지)
        /// </summary>
        private string GenerateUniqueFileName(string prefix, string baseName, string suffix, string extension)
        {
            try
            {
                string outputFolder = _settingsManager.Settings.OutputFolder;

                // 기본 파일명 시도
                string baseFileName = $"{prefix}{baseName}{suffix}{extension}";
                string fullPath = Path.Combine(outputFolder, baseFileName);

                if (!File.Exists(fullPath))
                {
                    return baseFileName; // 기본 이름 사용 가능
                }

                // 파일이 존재하면 번호를 추가해서 고유한 이름 생성
                int counter = 1;
                while (true)
                {
                    string numberedFileName = $"{prefix}{baseName}{suffix}({counter}){extension}";
                    string numberedFullPath = Path.Combine(outputFolder, numberedFileName);

                    if (!File.Exists(numberedFullPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"덮어쓰기 방지: {baseFileName} → {numberedFileName}");
                        return numberedFileName;
                    }

                    counter++;

                    // 무한 루프 방지 (최대 9999개)
                    if (counter > 9999)
                    {
                        // 타임스탬프 추가로 최후 수단
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string timestampFileName = $"{prefix}{baseName}{suffix}_{timestamp}{extension}";
                        System.Diagnostics.Debug.WriteLine($"타임스탬프 파일명 사용: {timestampFileName}");
                        return timestampFileName;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"고유 파일명 생성 오류: {ex.Message}");

                // 오류 발생 시 타임스탬프 기반 파일명 반환
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return $"{prefix}{baseName}{suffix}_{timestamp}{extension}";
            }
        }

        /// <summary>
        /// FFmpeg 명령 생성 (메타데이터 인코딩 설정 우선 사용)
        /// </summary>
        

        /// 비디오 코덱 결정 (메타데이터 우선)
        /// </summary>
        private string GetVideoCodec(VideoEditMetadata metadata)
        {
            // 메타데이터에 코덱 설정이 있으면 사용
            if (!string.IsNullOrEmpty(metadata.EncodingSettings.GetFFmpegVideoCodec()))
            {
                System.Diagnostics.Debug.WriteLine($"메타데이터 비디오 코덱 사용: {metadata.EncodingSettings.GetFFmpegVideoCodec()}");
                return metadata.EncodingSettings.GetFFmpegVideoCodec();
            }

            // 없으면 PC앱 설정 사용
            System.Diagnostics.Debug.WriteLine($"PC앱 비디오 코덱 사용: {_settingsManager.Settings.VideoCodec}");
            return _settingsManager.Settings.VideoCodec;
        }

        /// <summary>
        /// 오디오 코덱 결정 (메타데이터 우선)
        /// </summary>
        private string GetAudioCodec(VideoEditMetadata metadata)
        {
            // 메타데이터에 코덱 설정이 있으면 사용
            if (!string.IsNullOrEmpty(metadata.EncodingSettings.GetFFmpegAudioCodec()))
            {
                System.Diagnostics.Debug.WriteLine($"메타데이터 오디오 코덱 사용: {metadata.EncodingSettings.GetFFmpegAudioCodec()}");
                return metadata.EncodingSettings.GetFFmpegAudioCodec();
            }

            // 없으면 PC앱 설정 사용
            System.Diagnostics.Debug.WriteLine($"PC앱 오디오 코덱 사용: {_settingsManager.Settings.AudioCodec}");
            return _settingsManager.Settings.AudioCodec;
        }


        /// <summary>
        /// FFmpeg 프로세스 실행
        /// </summary>
        private async Task<bool> RunFFmpegProcessAsync(string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = _ffmpegPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                // ★ 핵심: 출력 스트림을 계속 읽어줘야 함!
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                // 출력도 완료될 때까지 대기
                await Task.WhenAll(outputTask, errorTask);
                var errorOutput = await errorTask;

                // ✅ 에러 출력 로깅 추가
                if (process.ExitCode != 0)
                {
                    Debug.WriteLine($"FFmpeg 실패 (Exit Code: {process.ExitCode})");
                    Debug.WriteLine($"Error: {errorOutput}");
                }
                return process.ExitCode == 0;
            }
        }

        /// <summary>
        /// 진행 상황 업데이트
        /// </summary>
        private void UpdateProgress(ProcessingTask task, int progress, string status)
        {
            task.Progress = progress;
            task.Status = status;

            ProcessingProgress?.Invoke(this, new ProcessingProgressEventArgs
            {
                Metadata = task.Metadata,
                TaskId = task.TaskId,
                Progress = progress,
                Status = status
            });
        }



    }
}