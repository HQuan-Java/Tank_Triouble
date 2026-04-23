using UnityEngine;

public class Enemy : MonoBehaviour
{
    [field: SerializeField] public EnemyHealth Health { get; private set; }
    [field: SerializeField] public EnemyAI AI { get; private set; }

    public void OnDeath()
    {
        EnemyManager.Instance.OnDeath(this);
    }
}