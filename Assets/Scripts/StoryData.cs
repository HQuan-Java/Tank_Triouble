using UnityEngine;

// ──────────────────────────────────────────
// Dữ liệu 1 dòng thoại
// ──────────────────────────────────────────
[System.Serializable]
public class StoryLine
{
    [TextArea(2, 5)]
    public string text;

    [Tooltip("Giữ dòng này bao lâu (giây) trước khi sang dòng kế")]
    public float holdTime = 2.0f;

    [Tooltip("Ghi đè badge phía trên (nếu trống = giữ nguyên)")]
    public string overrideYearBadge = "";
}

// ──────────────────────────────────────────
// Dữ liệu 1 chương
// ──────────────────────────────────────────
[System.Serializable]
public class StoryChapter
{
    public string chapterTitle;
    public StoryLine[] lines;

    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
}

// ──────────────────────────────────────────
// ScriptableObject để tái sử dụng
// ──────────────────────────────────────────
[CreateAssetMenu(fileName = "StoryData", menuName = "Game/Story Data")]
public class StoryData : ScriptableObject
{
    public StoryChapter[] chapters;
}