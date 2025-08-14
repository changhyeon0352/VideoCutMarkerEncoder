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

        // ⭐ 새로 추가: Share 폴더 자동 삭제 설정
        public bool AutoDeleteShareFiles { get; set; }

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
            VideoCodec = "libx265";         // H.265 CPU 인코딩 (압축률 좋음)
            AudioCodec = "aac";             // AAC 오디오 코덱
            VideoQuality = 26;              // CQ 값 (26는 좋은 품질/용량 균형)
            EncodingSpeed = "medium";       // 중간 속도

            AutoDeleteShareFiles = false;
        }
    }
}