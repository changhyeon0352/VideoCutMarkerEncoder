﻿using System;
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

                CleanupShareFiles(task);
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
        /// 비디오 처리 (FFmpeg 사용) - 메타데이터 인코딩 설정 우선 적용
        /// </summary>
        private async Task<string> ProcessVideoAsync(ProcessingTask task)
        {
            var metadata = task.Metadata;

            // 출력 파일 경로 생성 (메타데이터 설정 우선)
            string baseFileName = Path.GetFileNameWithoutExtension(metadata.VideoFileName);
            baseFileName = System.Text.RegularExpressions.Regex.Replace(baseFileName, @"\[VCM_[a-zA-Z0-9]+\]", "");

            // 메타데이터의 파일명 설정 사용 (없으면 기본값)
            string outputPrefix = !string.IsNullOrEmpty(metadata.EncodingSettings.OutputPrefix) ? metadata.EncodingSettings.OutputPrefix : "";
            string outputSuffix = !string.IsNullOrEmpty(metadata.EncodingSettings.OutputSuffix) ? metadata.EncodingSettings.OutputSuffix : "";
            string outputFileName = $"{outputPrefix}{baseFileName}{outputSuffix}.mp4";
            string outputPath = Path.Combine(_settingsManager.Settings.OutputFolder, outputFileName);

            // 세그먼트 처리
            List<string> segmentFiles = new List<string>();

            // 활성 그룹 정보 가져오기
            if (!metadata.Groups.TryGetValue(metadata.ActiveGroupId, out GroupInfo activeGroup))
            {
                throw new Exception("활성 그룹을 찾을 수 없습니다.");
            }

            // 처리할 세그먼트 필터링 (활성 그룹만)
            var activeSegments = metadata.Segments.FindAll(s => s.GroupId == metadata.ActiveGroupId);
            if (activeSegments.Count == 0)
            {
                throw new Exception("처리할 세그먼트가 없습니다.");
            }

            // 각 세그먼트별로 처리
            for (int i = 0; i < activeSegments.Count; i++)
            {
                var segment = activeSegments[i];

                // 시작/종료 시간 및 크롭 영역 계산
                double startTime = segment.StartTime;
                double endTime = segment.EndTime;

                // 크롭 영역 계산
                int cropX = segment.CenterX - (activeGroup.Width / 2);
                int cropY = segment.CenterY - (activeGroup.Height / 2);

                // 범위 조정 (음수 방지)
                cropX = Math.Max(0, cropX);
                cropY = Math.Max(0, cropY);

                // 세그먼트 파일 경로
                string segmentFilePath = Path.Combine(_settingsManager.Settings.OutputFolder, $"segment_{task.TaskId}_{i}.mp4");
                segmentFiles.Add(segmentFilePath);

                // FFmpeg 명령 생성 (메타데이터 설정 우선 사용)
                string ffmpegArgs = BuildFFmpegCommand(metadata, startTime, endTime, cropX, cropY, activeGroup, segmentFilePath);

                // FFmpeg 실행
                UpdateProgress(task, (i * 100) / activeSegments.Count, $"세그먼트 {i + 1}/{activeSegments.Count} 인코딩 중");
                bool success = await RunFFmpegProcessAsync(ffmpegArgs);

                if (!success)
                {
                    throw new Exception($"세그먼트 {i + 1} 처리 실패");
                }
            }

            // 세그먼트가 여러 개면 병합
            if (segmentFiles.Count > 1)
            {
                // 파일 목록 생성
                string listFilePath = Path.Combine(_settingsManager.Settings.OutputFolder, $"segments_{task.TaskId}.txt");
                using (StreamWriter writer = new StreamWriter(listFilePath))
                {
                    foreach (string file in segmentFiles)
                    {
                        writer.WriteLine($"file '{file}'");
                    }
                }

                // 병합 명령
                string concatArgs = $"-y -f concat -safe 0 -i \"{listFilePath}\" -c copy \"{outputPath}\"";

                // FFmpeg 실행
                UpdateProgress(task, 90, "세그먼트 병합 중");
                bool concatSuccess = await RunFFmpegProcessAsync(concatArgs);

                if (!concatSuccess)
                {
                    throw new Exception("세그먼트 병합 실패");
                }

                // 임시 파일 삭제
                foreach (string file in segmentFiles)
                {
                    File.Delete(file);
                }
                File.Delete(listFilePath);
            }
            else if (segmentFiles.Count == 1)
            {
                // 세그먼트가 하나면 이름만 변경
                File.Move(segmentFiles[0], outputPath, true);
            }

            // 진행 상황 업데이트
            UpdateProgress(task, 100, "처리 완료");

            return outputPath;
        }

        /// <summary>
        /// FFmpeg 명령 생성 (메타데이터 인코딩 설정 우선 사용)
        /// </summary>
        private string BuildFFmpegCommand(VideoEditMetadata metadata, double startTime, double endTime,
            int cropX, int cropY, GroupInfo activeGroup, string outputPath)
        {
            var args = new StringBuilder();

            // 입력 파일
            args.Append($"-y -i \"{metadata.VideoPath}\" -ss {startTime} -to {endTime} ");

            // 비디오 필터 체인 구성
            var filters = new List<string>();

            // 1. 회전 필터
            switch (metadata.VideoRotation)
            {
                case Rotation.CW90:
                    filters.Add("transpose=1");
                    break;
                case Rotation.CW180:
                    filters.Add("transpose=2,transpose=2");
                    break;
                case Rotation.CW270:
                    filters.Add("transpose=2");
                    break;
            }

            // 2. 크롭 필터
            filters.Add($"crop={activeGroup.Width}:{activeGroup.Height}:{cropX}:{cropY}");

            // 3. 스케일링 필터 (메타데이터에서)
            if (metadata.EncodingSettings.EnableScaling && !string.IsNullOrEmpty(metadata.EncodingSettings.ScaleFilter))
            {
                filters.Add(metadata.EncodingSettings.ScaleFilter);
                System.Diagnostics.Debug.WriteLine($"스케일링 필터 적용: {metadata.EncodingSettings.ScaleFilter}");
            }

            // 필터 체인 적용
            if (filters.Count > 0)
            {
                args.Append($"-vf \"{string.Join(",", filters)}\" ");
            }

            // 비디오 코덱 (메타데이터 우선, 없으면 PC앱 설정)
            string videoCodec = GetVideoCodec(metadata);
            args.Append($"-c:v {videoCodec} ");

            // 인코딩 속도 (PC앱 설정 사용)
            args.Append($"-preset {_settingsManager.Settings.EncodingSpeed} ");

            // CRF 값 (메타데이터 우선, 없으면 PC앱 설정)
            int crf = metadata.EncodingSettings.CRF > 0 ? metadata.EncodingSettings.CRF : _settingsManager.Settings.VideoQuality;
            args.Append($"-crf {crf} ");

            // 오디오 코덱 (메타데이터 우선, 없으면 PC앱 설정)
            string audioCodec = GetAudioCodec(metadata);
            args.Append($"-c:a {audioCodec} ");

            // 출력 파일
            args.Append($"\"{outputPath}\"");

            System.Diagnostics.Debug.WriteLine($"FFmpeg 명령: {args}");
            return args.ToString();
        }

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
                var outputTask = Task.Run(() => process.StandardOutput.ReadToEnd());
                var errorTask = Task.Run(() => process.StandardError.ReadToEnd());

                await process.WaitForExitAsync();

                // 출력도 완료될 때까지 대기
                await Task.WhenAll(outputTask, errorTask);

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