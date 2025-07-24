using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class MapConfigAuthoring : MonoBehaviour
{
    [Header("网格设置")]
    public Vector3 StartPosition = Vector3.zero;
    public int2 gridSize = new int2(20, 20);
    public float cellSize = 1f;

    [Header("预制体")]
    public GameObject tilePrefab;

    [Header("地形生成")]
    [Range(0, 1)] public float obstacleDensity = 0.3f;
    // public Material[] terrainMaterials;
}

public class MapConfigBaker : Baker<MapConfigAuthoring>
{
    public override void Bake(MapConfigAuthoring authoring)
    {
        // 1. 创建配置实体（单例）
        var entity = GetEntity(TransformUsageFlags.None);

        // 2. 添加并设置ECS组件
        AddComponent(entity, new MapGridSettings
        {
            StartPosition = new float3(authoring.StartPosition.x,authoring.StartPosition.y, authoring.StartPosition.z),
            GridSize = new int2(authoring.gridSize.x, authoring.gridSize.y),
            CellSize = authoring.cellSize,
            TilePrefab = GetEntity(authoring.tilePrefab, TransformUsageFlags.Dynamic),
        });


    }
}