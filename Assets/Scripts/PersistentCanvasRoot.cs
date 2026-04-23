using UnityEngine;

/// <summary>
/// Gắn lên UI root/canvas muốn giữ xuyên scene.
/// Dùng persistKey để tránh duplicate khi scene sau có cùng prefab UI.
/// </summary>
public class PersistentCanvasRoot : MonoBehaviour
{
    [SerializeField] private string persistKey = "GlobalUI";

    private void Awake()
    {
        if (string.IsNullOrEmpty(persistKey))
            persistKey = gameObject.name;

        string tag = $"__PersistentCanvas__{persistKey}";
        var existing = GameObject.Find(tag);
        if (existing != null && existing != gameObject)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.name = tag;
        DontDestroyOnLoad(gameObject);
    }
}
