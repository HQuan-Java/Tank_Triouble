using UnityEngine;
using UnityEngine.EventSystems;

public class UI3DRotate : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Target")]
    public Transform target;

    [Header("Settings")]
    public float rotateSpeed = 0.3f;
    public bool onlyRotateY = true;
    
    [Header("Invert Controls")]
    public bool invertHorizontal = false;
    public bool invertVertical = false;

    private float _currentX;
    private float _currentY;
    private bool _isInitialized;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (target != null && !_isInitialized)
        {
            Vector3 euler = target.eulerAngles;
            _currentX = euler.x > 180 ? euler.x - 360 : euler.x;
            _currentY = euler.y;
            _isInitialized = true;
        }
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (target == null) return;

        float deltaX = eventData.delta.x * rotateSpeed * (invertHorizontal ? 1f : -1f);
        float deltaY = eventData.delta.y * rotateSpeed * (invertVertical ? -1f : 1f);

        if (onlyRotateY)
        {
            _currentY += deltaX;
            target.rotation = Quaternion.Euler(target.eulerAngles.x, _currentY, target.eulerAngles.z);
        }
        else
        {
            _currentY += deltaX;
            _currentX += deltaY;
            target.rotation = Quaternion.Euler(_currentX, _currentY, 0f);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }
}