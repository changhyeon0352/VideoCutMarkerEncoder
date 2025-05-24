using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VideoCutMarkerEncoder.Models
{
    /// <summary>
    /// 비디오 편집 메타데이터 클래스 - 모바일 앱에서 생성한 .vcm 파일과 호환
    /// </summary>
    public class VideoEditMetadata
    {
        public string Id { get; set; }
        public string VideoFileName { get; set; }
        public string VideoPath { get; set; }
        public string MetadataVersion { get; set; } = "1.0";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Rotation VideoRotation { get; set; } = Rotation.None;

        public List<CropSegmentInfo> Segments { get; set; } = new List<CropSegmentInfo>();
        public Dictionary<int, GroupInfo> Groups { get; set; } = new Dictionary<int, GroupInfo>();
        public int ActiveGroupId { get; set; } = 1; // 현재 활성화된 그룹 ID
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
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
}