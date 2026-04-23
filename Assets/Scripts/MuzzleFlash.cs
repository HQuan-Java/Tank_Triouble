using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public void OnAnimationCompleted()
    {
        Destroy(gameObject);
    }
}
