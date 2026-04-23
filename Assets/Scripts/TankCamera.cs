using UnityEngine;
using Pathfinding;

public class TankCamera : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Bounds Padding")]
    [Tooltip("Thêm khoảng trống ở viền (nếu cần)")]
    public float paddingX = 0f;
    public float paddingY = 0f;

    private Camera cam;
    
    // Lưu trữ bounds của PathFinder
    [SerializeField]
    private Vector2 minBounds;
    [SerializeField]
    private Vector2 maxBounds;
    private bool hasBounds = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    void Start()
    {
        CalculatePathFinderBounds();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Nếu lúc Start() AstarPath chưa kịp load thì thử lấy lại ở khung hình sau
        if (!hasBounds)
        {
            CalculatePathFinderBounds();
        }

        // Vị trí mong muốn theo sau target
        Vector3 desiredPosition = target.position + offset;
        
        // Clamp (giới hạn) vị trí camera không vượt ra khỏi PathFinder bounds
        if (hasBounds)
        {
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            float minX = minBounds.x + camWidth + paddingX;
            float maxX = maxBounds.x - camWidth - paddingX;
            float minY = minBounds.y + camHeight + paddingY;
            float maxY = maxBounds.y - camHeight - paddingY;

            // Đề phòng trường hợp map nhỏ hơn khung hình camera thì cố định ở giữa
            if (minX > maxX)
            {
                minX = maxX = (minBounds.x + maxBounds.x) / 2f;
            }
            if (minY > maxY)
            {
                minY = maxY = (minBounds.y + maxBounds.y) / 2f;
            }

            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Di chuyển mượt mà tới vị trí mong muốn
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    /// <summary>
    /// Lấy giới hạn nhỏ nhất/lớn nhất của toàn bộ các Node trong A* Pathfinding (PathFinder)
    /// </summary>
    public void CalculatePathFinderBounds()
    {
        if (AstarPath.active == null || AstarPath.active.data == null) return;
        
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        bool foundNode = false;
        
        foreach (var graph in AstarPath.active.data.graphs)
        {
            if (graph == null) continue;
            
            graph.GetNodes(node =>
            {
                Vector3 pos = (Vector3)node.position;
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
                foundNode = true;
            });
        }

        if (foundNode)
        {
            minBounds = new Vector2(minX, minY);
            maxBounds = new Vector2(maxX, maxY);
            hasBounds = true;
        }
    }
}
