using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.8f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}