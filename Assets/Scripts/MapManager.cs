using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class MinimapProperty
{
    [field: SerializeField]
    public string Name { get; private set; }
    [field: SerializeField]
    public Color Color { get; private set; }
    [field: SerializeField]
    public float Scale { get; private set; }
}
public class MapManager : MonoBehaviour
{
    [SerializeField] private MinimapObject prefab;
    [SerializeField] private MinimapProperty[] minimapProperties;
    [Button]
    public void Bake()
    {
        var colliders = FindObjectsOfType<Collider2D>()
            .Where(x => minimapProperties.Any(y => x.name.Contains(y.Name, StringComparison.InvariantCultureIgnoreCase)))
            .ToArray();
        foreach (var col in colliders)
        {
            var minimapObject = col.GetComponentInChildren<MinimapObject>();
            if (minimapObject != null)
            {
                continue;
            }
            var tf = col.transform;
            minimapObject = Instantiate(prefab,tf);
            minimapObject.transform.localPosition = Vector3.zero;
            var name = col.name;
            var property = minimapProperties.FirstOrDefault(x => name.Contains(x.Name, StringComparison.InvariantCultureIgnoreCase));
            if (property == null)
            {
                continue;
            }
            minimapObject.SetColor(property.Color);
            minimapObject.transform.localScale = Vector3.one * property.Scale;
        }
    }
    [Button]
    public void Clear()
    {
        var minmapObjects = FindObjectsOfType<MinimapObject>();
        foreach (var minimapObject in minmapObjects)
        {
            DestroyImmediate(minimapObject.gameObject);
        }
    }
}
