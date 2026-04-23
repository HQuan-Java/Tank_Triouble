using UnityEngine;

public class MinimapObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer renderer;
    public void SetColor(Color color)
    {
        renderer.color = color;
    }
}
