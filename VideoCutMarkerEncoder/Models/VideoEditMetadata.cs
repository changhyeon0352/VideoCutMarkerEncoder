using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VideoCutMarkerEncoder.Models
{
    /// <summary>
    /// 비디오 편집 메타데이터 클래스 - 단순화 (FFmpeg 직접 옵션)
    /// </summary>
    public class VideoEditMetadata
    {
        public string Id { get; set; }
        public string VideoFileName { get; set; }
        public string VideoPath { get; set; }
        public string MetadataVersion { get; set; } = "1.0";
        public int VideoWidth { get; set; }
        public int VideoHeight { get; set; }
        /// <summary>
        /// Merge 모드일 때 사용할 기준 해상도
        /// </summary>

        public List<CropSegmentInfo> Segments { get; set; } = new List<CropSegmentInfo>();
        public Dictionary<int, GroupInfo> Groups { get; set; } = new Dictionary<int, GroupInfo>();
        /// <summary>
        /// 출력 모드 (Separate: 그룹별 분리, Merge: 모든 그룹 병합)
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OutputMode OutputMode { get; set; } = OutputMode.Separate;
        public ReferenceResolution ReferenceResolution { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();

        // FFmpeg 직접 옵션들
        public EncodingSettings EncodingSettings { get; set; } = new EncodingSettings();
    }

    /// <summary>
    /// 크롭 구간 정보를 담는 클래스
    /// </summary>
    public class CropSegmentInfo
    {
        public int CenterX { get; set; }
        public int CenterY { get; set; }
        public int GroupId { get; set; } = 0; // 0: 미선택, 1~n: 그룹 ID
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }

    /// <summary>
    /// 그룹 정보 클래스
    /// </summary>
    public class GroupInfo
    {
        public int Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Rotation Rotation { get; set; } = Rotation.None;
        public GroupInfo(int id, int width, int height)
        {
            Id = id;
            Width = width;
            Height = height;
            Rotation = Rotation.None;
        }

        // JSON 직렬화를 위한 매개변수 없는 생성자
        public GroupInfo() { }

        /// <summary>
        /// 회전된 크기 계산 (90도, 270도일 때 가로세로 바뀜)
        /// </summary>
        /// <returns>(실제 너비, 실제 높이)</returns>
        public (int ActualWidth, int ActualHeight) GetRotatedSize()
        {
            return Rotation switch
            {
                Rotation.CW90 or Rotation.CW270 => (Height, Width), // 90도, 270도 회전시 가로세로 바뀜
                _ => (Width, Height)           // 0도, 180도는 크기 그대로
            };
        }
    }

    /// <summary>
    /// 회전 열거형
    /// </summary>
    public enum Rotation
    {
        None = 0,    // 0° 회전
        CW90 = 1,    // 90° 시계 방향
        CW180 = 2,   // 180° 회전
        CW270 = 3    // 270° 시계 방향 (또는 90° 반시계 방향)
    }
    /// <summary>
    /// 출력 모드
    /// </summary>
    public enum OutputMode
    {
        Separate = 0,  // 그룹별 분리 (기본값)
        Merge = 1      // 모든 그룹 병합
    }
    /// <summary>
    /// 기준 해상도 정보 클래스
    /// </summary>
    public class ReferenceResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public ReferenceResolution() { }

        public ReferenceResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

}