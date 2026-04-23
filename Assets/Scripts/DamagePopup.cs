using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn trên prefab popup sát thương (Canvas World Space + TextMeshProUGUI con).
/// Gọi <see cref="Play"/> sau Instantiate.
/// </summary>
[DisallowMultipleComponent]
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private Canvas worldCanvas;

    [Header("Anim (DOTween)")]
    [SerializeField] private float riseHeight = 0.62f;
    [SerializeField] private float driftRadius = 0.14f;
    [SerializeField] private float scaleUpDuration = 0.18f;
    [SerializeField] private float floatDuration = 0.52f;
    [SerializeField] private float fadeDelay = 0.16f;
    [SerializeField] private float fadeDuration = 0.34f;
    [SerializeField] private float startScaleFactor = 0.22f;

    private Sequence _seq;

    void OnDestroy()
    {
        _seq?.Kill();
        transform.DOKill();
        if (damageText != null)
            DOTween.Kill(damageText, false);
    }

    public void Play(int amount, Color color)
    {
        if (damageText == null)
            damageText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (damageText == null)
        {
            Destroy(gameObject);
            return;
        }

        if (damageText.font == null && TMP_Settings.defaultFontAsset != null)
            damageText.font = TMP_Settings.defaultFontAsset;

        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();

        if (worldCanvas != null)
        {
            worldCanvas.overrideSorting = true;
            worldCanvas.sortingOrder = 500;
            if (worldCanvas.renderMode == RenderMode.WorldSpace)
                worldCanvas.worldCamera = Camera.main;
        }

        damageText.text = amount.ToString();
        Color c = color;
        c.a = 1f;
        damageText.color = c;
        damageText.alpha = 1f;

        Vector3 start = transform.position;
        Vector2 drift = Random.insideUnitCircle * driftRadius;
        Vector3 end = start + Vector3.up * riseHeight + new Vector3(drift.x, drift.y, 0f);

        Vector3 baseScale = transform.localScale;
        transform.localScale = baseScale * startScaleFactor;

        transform.DOKill();
        DOTween.Kill(damageText, false);
        _seq?.Kill();

        Tweener fadeTween = DOTween.To(
                () => damageText.alpha,
                a => damageText.alpha = a,
                0f,
                fadeDuration)
            .SetEase(Ease.InQuad)
            .SetDelay(fadeDelay)
            .SetTarget(damageText);

        _seq = DOTween.Sequence();
        _seq.Append(transform.DOScale(baseScale, scaleUpDuration).SetEase(Ease.OutBack));
        _seq.Join(transform.DOMove(end, floatDuration).SetEase(Ease.OutQuad));
        _seq.Join(fadeTween);
        _seq.OnComplete(() => Destroy(gameObject));
    }
}
