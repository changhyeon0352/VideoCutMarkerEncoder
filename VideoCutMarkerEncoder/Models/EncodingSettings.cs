using System.Text.Json.Serialization;

namespace VideoCutMarkerEncoder.Models
{
    /// <summary>
    /// 인코딩 설정 데이터 모델 (모바일 앱과 동일한 구조)
    /// </summary>
    public class EncodingSettings
    {
        public VideoCodec Codec { get; set; } = VideoCodec.H265_CPU;
        public int CRF { get; set; } = 28;
        public AudioCodec AudioCodec { get; set; } = AudioCodec.AAC;
        public string OutputPrefix { get; set; } = "";
        public string OutputSuffix { get; set; } = "";
        public bool EnableScaling { get; set; } = false;
        public string ScaleFilter { get; set; } = "";

        public string GetFFmpegVideoCodec()
        {
            return Codec switch
            {
                VideoCodec.H264_CPU => "libx264",
                VideoCodec.H264_NVIDIA => "h264_nvenc",
                VideoCodec.H264_AMD => "h264_amf",
                VideoCodec.H265_CPU => "libx265",
                VideoCodec.H265_NVIDIA => "hevc_nvenc",
                VideoCodec.H265_AMD => "hevc_amf",
                _ => "libx265"
            };
        }

        public string GetFFmpegAudioCodec()
        {
            return AudioCodec switch
            {
                AudioCodec.AAC => "aac",
                AudioCodec.MP3 => "mp3",
                AudioCodec.Copy => "copy",
                _ => "aac"
            };
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VideoCodec
    {
        H264_CPU, H264_NVIDIA, H264_AMD,
        H265_CPU, H265_NVIDIA, H265_AMD
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AudioCodec
    {
        AAC, MP3, Copy
    }

}