using System;
using UnityEngine;
using UnityEngine.SceneManagement;
[Flags]
public enum MapType
{
    Normal,
    HasItem = 1 << 0,
    HasFog = 1 << 1,
}
public class SceneParameter : MonoBehaviour
{
    private const int ForcedEnemyCount = 1;

    [field: SerializeField]
    public string SceneName { get; set; }
    [field: SerializeField]
    public MapType MapType { get; set; }
    [SerializeField] private int startEnemy = ForcedEnemyCount;
    public int StartEnemy
    {
        get => ForcedEnemyCount;
        set => startEnemy = ForcedEnemyCount;
    }
    private static SceneParameter instance;
    public static SceneParameter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SceneParameter>();
            }

            if (instance == null)
            {
                instance = new GameObject("SceneParameter").AddComponent<SceneParameter>();
            }
            return instance;
        }
    }
    private void Start()
    {
        startEnemy = ForcedEnemyCount;
        SceneName = SceneManager.GetActiveScene().name;
        DontDestroyOnLoad(gameObject);
    }

    private void OnValidate()
    {
        startEnemy = ForcedEnemyCount;
    }
}
