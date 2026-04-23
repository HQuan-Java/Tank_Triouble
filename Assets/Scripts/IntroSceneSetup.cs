#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Chạy menu Tools → Setup Intro Scene để tự động tạo Canvas + UI
/// </summary>
public class IntroSceneSetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Intro Scene")]
    static void SetupScene()
    {
        // Canvas
        var canvasGO = new GameObject("IntroCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fade Overlay (Image đen phủ toàn màn)
        var fadeGO = CreateUIImage(canvasGO, "FadeOverlay", Color.black);
        StretchFull(fadeGO.GetComponent<RectTransform>());

        // Background (tùy chọn — đặt ảnh chiến trường)
        var bgGO = CreateUIImage(canvasGO, "Background", new Color(0.04f, 0.05f, 0.04f));
        StretchFull(bgGO.GetComponent<RectTransform>());

        // Year badge
        CreateText(canvasGO, "YearBadge", "// CLASSIFIED LOG 2099 //",
            new Vector2(0, 80), 14, new Color(0.3f, 0.5f, 0.3f));

        // Chapter label
        CreateText(canvasGO, "ChapterLabel", "// CHƯƠNG 1 //",
            new Vector2(0, 50), 12, new Color(0.25f, 0.4f, 0.25f));

        // Story text (chính giữa)
        CreateText(canvasGO, "StoryText", "",
            new Vector2(0, 0), 22, new Color(0.8f, 0.95f, 0.8f));

        // Progress bar
        var sliderGO = new GameObject("ProgressBar");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        var slider = sliderGO.AddComponent<Slider>();
        var rt = sliderGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.sizeDelta = new Vector2(0, 4);
        rt.anchoredPosition = Vector2.zero;

        // Skip Button
        var btnGO = new GameObject("SkipButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(1, 1);
        btnRT.sizeDelta = new Vector2(120, 36);
        btnRT.anchoredPosition = new Vector2(-70, -30);
        btnGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        var btn = btnGO.AddComponent<Button>();

        var btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        var btnTMP = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "BỎ QUA  [ESC]";
        btnTMP.fontSize = 11;
        btnTMP.alignment = TextAlignmentOptions.Center;
        var btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;

        // Manager
        var managerGO = new GameObject("IntroStoryManager");
        var manager = managerGO.AddComponent<IntroStoryManager>();

        Debug.Log("✅ Intro Scene đã được tạo! Gán các tham chiếu vào IntroStoryManager.");
        Selection.activeGameObject = managerGO;
    }

    static GameObject CreateUIImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static void CreateText(GameObject parent, string name, string text,
        Vector2 offset, int size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700, 100);
        rt.anchoredPosition = offset;
    }
}
#endif