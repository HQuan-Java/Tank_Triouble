using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private Enemy prefab;
    [SerializeField] private GameObject fog;
    [SerializeField] private ItemDropper itemDropper;
    [SerializeField] private float itemSpawnMinTime = 10f;
    [SerializeField] private float itemSpawnMaxTime = 20f;
    
    private static EnemyManager instance;
    private bool hasDropItem;
    public static EnemyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<EnemyManager>();
            }

            return instance;
        }
    }

    public IReadOnlyList<Enemy> Enemies => enemies;
    private List<Enemy> deadEnemies = new();
    public IReadOnlyList<Enemy> DeadEnemies => deadEnemies;

    private void Start()
    {
        var sceneParams = SceneParameter.Instance;
        var mapType = sceneParams.MapType;
        hasDropItem = mapType.HasFlag(MapType.HasItem);
        var points = this.spawnPoints.Select(x => (Vector2)x.position).ToList();
        var spawnPoints = GetRandomPoints(points, sceneParams.StartEnemy, 3);
        foreach (var spawnPoint in spawnPoints)
        {
            var enemy = Instantiate(prefab, spawnPoint, Quaternion.identity);
            enemies.Add(enemy);
        }
        if (mapType.HasFlag(MapType.HasFog))
        {
            fog.SetActive(true);
        }

        if (hasDropItem && itemDropper != null)
        {
            StartCoroutine(SpawnItemRoutine());
        }
    }

    private void FixedUpdate()
    {
    }

    private IEnumerator SpawnItemRoutine()
    {
        while (true)
        {
            float waitTime = UnityEngine.Random.Range(itemSpawnMinTime, itemSpawnMaxTime);
            yield return new WaitForSeconds(waitTime);

            if (itemDropper != null && spawnPoints != null && spawnPoints.Count > 0)
            {
                Transform randomPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
                itemDropper.Drop(randomPoint.position);
            }
        }
    }
    public void OnDeath(Enemy enemy)
    {
        enemies.Remove(enemy);
        if (!deadEnemies.Contains(enemy))
            deadEnemies.Add(enemy);

        if (enemies.Count == 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameWin();
            }
        }
    }
    private List<Vector2> GetRandomPoints(List<Vector2> source, int n, float minDistance)
    {
        List<Vector2> result = new List<Vector2>();
        List<Vector2> shuffled = new List<Vector2>(source);
        for (int i = 0; i < shuffled.Count; i++)
        {
            Vector2 temp = shuffled[i];
            int rand = UnityEngine.Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[rand];
            shuffled[rand] = temp;
        }

        foreach (var point in shuffled)
        {
            bool valid = true;

            foreach (var selected in result)
            {
                if (Vector2.Distance(point, selected) < minDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                result.Add(point);
                if (result.Count >= n)
                    break;
            }
        }

        return result;
    }
}