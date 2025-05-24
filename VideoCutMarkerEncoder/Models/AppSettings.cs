using System;

namespace VideoCutMarkerEncoder.Models
{
    /// <summary>
    /// 앱 설정 클래스
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

        // 인코딩 설정
        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }
        public int VideoQuality { get; set; }
        public string EncodingSpeed { get; set; }

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

            // 인코딩 설정 기본값
            VideoCodec = "libx264";
            AudioCodec = "aac";
            VideoQuality = 23; // CRF 값 (낮을수록 높은 품질)
            EncodingSpeed = "medium";
        }
    }
}