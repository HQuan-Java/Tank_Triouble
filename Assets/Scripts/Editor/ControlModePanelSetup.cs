#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Tools → Setup Control Mode Toggle
/// Tạo toggle nhỏ góc trên-phải để bật/tắt chế độ điều khiển.
/// </summary>
public static class ControlModePanelSetup
{
    [MenuItem("Tools/Setup Control Mode Toggle")]
    static void Setup()
    {
        var existing = Object.FindFirstObjectByType<ControlModePanel>();
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Đã tồn tại",
                "ControlModePanel đã có trong scene.\nXóa và tạo lại?",
                "Tạo lại", "Hủy");
            if (!replace) return;

            // Destroy canvas parent
            var canvasParent = existing.GetComponentInParent<Canvas>();
            if (canvasParent != null)
                Object.DestroyImmediate(canvasParent.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        // ══════════════════════════════════════════════
        // CANVAS
        // ══════════════════════════════════════════════
        var canvasGO = new GameObject("ControlModeCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ══════════════════════════════════════════════
        // TOGGLE CONTAINER – góc trên phải
        // ══════════════════════════════════════════════
        var containerGO = new GameObject("ControlModePanel");
        containerGO.transform.SetParent(canvasGO.transform, false);

        var rt = containerGO.AddComponent<RectTransform>();
        // Anchor: top-right
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.sizeDelta        = new Vector2(260, 52);
        rt.anchoredPosition = new Vector2(-20, -20);

        // Background với bo góc nhẹ
        var bg = containerGO.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.44f, 0.90f); // màu keyboard mặc định
        // Thêm bo góc nếu dùng sprite tròn (để mặc định là hình chữ nhật cũng OK)

        var toggle = containerGO.AddComponent<Toggle>();
        toggle.targetGraphic = bg;

        // ── Icon ─────────────────────────────────────
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(containerGO.transform, false);
        var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text      = "⌨";
        iconTMP.fontSize  = 22;
        iconTMP.color     = Color.white;
        iconTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin        = Vector2.zero;
        iconRT.anchorMax        = new Vector2(0f, 1f);
        iconRT.pivot            = new Vector2(0f, 0.5f);
        iconRT.sizeDelta        = new Vector2(44, 0);
        iconRT.anchoredPosition = new Vector2(12, 0);

        // ── Label ────────────────────────────────────
        var labelGO  = new GameObject("ModeLabel");
        labelGO.transform.SetParent(containerGO.transform, false);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = "⌨  BÀN PHÍM";
        labelTMP.fontSize  = 16;
        labelTMP.color     = Color.white;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin        = new Vector2(0f, 0f);
        labelRT.anchorMax        = new Vector2(1f, 1f);
        labelRT.sizeDelta        = Vector2.zero;
        labelRT.anchoredPosition = Vector2.zero;
        // Offset text để không đè icon
        labelRT.offsetMin = new Vector2(14, 0);
        labelRT.offsetMax = Vector2.zero;

        // ══════════════════════════════════════════════
        // WIRE UP script
        // ══════════════════════════════════════════════
        var script           = containerGO.AddComponent<ControlModePanel>();
        script.controlToggle = toggle;
        script.modeLabel     = labelTMP;
        script.toggleBg      = bg;

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("✅ Control Mode Toggle tạo xong!\n" +
                  "Lưu scene (Ctrl+S) rồi bấm Play. Gạt toggle góc trên-phải để đổi chế độ.");

        Selection.activeGameObject = containerGO;
    }

    [MenuItem("Tools/Setup Control Mode Toggle", true)]
    static bool CanSetup() => !Application.isPlaying;
}
#endif
