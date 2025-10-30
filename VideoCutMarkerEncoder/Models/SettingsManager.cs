using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using VideoCutMarkerEncoder.Models;

namespace VideoCutMarkerEncoder.Models
{
    /// <summary>
    /// 앱 설정 저장 및 관리 클래스 (인코딩 설정 제거)
    /// </summary>
    public class SettingsManager
    {
        private readonly string _settingsFilePath;
        private readonly string _appFolder;

        /// <summary>
        /// 앱 설정
        /// </summary>
        public AppSettings Settings { get; private set; }

        /// <summary>
        /// 첫 실행 여부
        /// </summary>
        public bool IsFirstRun { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="appFolder">애플리케이션 폴더 경로</param>
        public SettingsManager(string appFolder)
        {
            _appFolder = appFolder;

            // 설정 파일 경로
            _settingsFilePath = Path.Combine(_appFolder, "Config", "settings.json");

            // 설정 로드
            IsFirstRun = !File.Exists(_settingsFilePath);
            LoadSettings();
        }

        /// <summary>
        /// 설정 로드
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json);

                    // FFmpeg 경로 재설정 (항상 상대 경로로)
                    Settings.FFmpegPath = Path.Combine(_appFolder, "FFmpeg", "ffmpeg.exe");
                }
                else
                {
                    // 기본 설정 생성
                    Settings = new AppSettings
                    {
                        // 폴더 설정 - 애플리케이션 폴더 내에 생성
                        ShareFolder = Path.Combine(_appFolder, "Share"),
                        OutputFolder = Path.Combine(_appFolder, "Output"),
                        FFmpegPath = Path.Combine(_appFolder, "FFmpeg", "ffmpeg.exe"),

                        // 일반 설정
                        ShareName = "VideoCutMarker",
                        MinimizeToTray = false,
                        NotifyOnComplete = true
                    };

                    // 폴더 생성
                    CreateFolders();

                    // 설정 저장
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"설정 로드 오류: {ex.Message}");

                // 오류 발생 시 기본 설정 사용
                Settings = new AppSettings
                {
                    ShareFolder = Path.Combine(_appFolder, "Share"),
                    OutputFolder = Path.Combine(_appFolder, "Output"),
                    FFmpegPath = Path.Combine(_appFolder, "FFmpeg", "ffmpeg.exe"),
                    ShareName = "VideoCutMarker"
                };

                CreateFolders();
            }
        }

        /// <summary>
        /// 필요한 폴더 생성
        /// </summary>
        private void CreateFolders()
        {
            try
            {
                // Config 폴더 생성
                string configFolder = Path.Combine(_appFolder, "Config");
                if (!Directory.Exists(configFolder))
                {
                    Directory.CreateDirectory(configFolder);
                }

                // 공유 폴더 생성
                if (!Directory.Exists(Settings.ShareFolder))
                {
                    Directory.CreateDirectory(Settings.ShareFolder);
                }

                // 출력 폴더 생성
                if (!Directory.Exists(Settings.OutputFolder))
                {
                    Directory.CreateDirectory(Settings.OutputFolder);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"폴더 생성 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // 설정 폴더 확인
                string configFolder = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(configFolder))
                {
                    Directory.CreateDirectory(configFolder);
                }

                // JSON 직렬화 및 저장
                string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"설정 저장 오류: {ex.Message}");
            }
        }
    }
}