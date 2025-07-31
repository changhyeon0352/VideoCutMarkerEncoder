using System;

namespace VideoCutMarkerEncoder.Models
{
    /// <summary>
    /// 앱 설정 클래스 (인코딩 설정 제거, 기본 설정만 유지)
    /// </summary>
    public class AppSettings
    {
        // 폴더 설정
        public string ShareFolder { get; set; }
        public string OutputFolder { get; set; }
        public string FFmpegPath { get; set; }

        // 일반 설정
        public string ShareName { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool NotifyOnComplete { get; set; }

        /// <summary>
        /// 생성자 - 기본값 설정
        /// </summary>
        public AppSettings()
        {
            // 폴더 설정 기본값
            ShareFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Share");
            OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FFmpeg", "ffmpeg.exe");

            // 일반 설정 기본값
            ShareName = "VideoCutMarker";
            MinimizeToTray = true;
            NotifyOnComplete = true;
        }
    }
}