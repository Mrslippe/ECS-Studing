using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class MapConfigAuthoring : MonoBehaviour
{
    [Header("��������")]
    public Vector3 StartPosition = Vector3.zero;
    public int2 gridSize = new int2(20, 20);
    public float cellSize = 1f;

    [Header("Ԥ����")]
    public GameObject tilePrefab;

    [Header("��������")]
    [Range(0, 1)] public float obstacleDensity = 0.3f;
    // public Material[] terrainMaterials;
}

public class MapConfigBaker : Baker<MapConfigAuthoring>
{
    public override void Bake(MapConfigAuthoring authoring)
    {
        // 1. ��������ʵ�壨������
        var entity = GetEntity(TransformUsageFlags.None);

        // 2. ��Ӳ�����ECS���
        AddComponent(entity, new MapGridSettings
        {
            StartPosition = new float3(authoring.StartPosition.x,authoring.StartPosition.y, authoring.StartPosition.z),
            GridSize = new int2(authoring.gridSize.x, authoring.gridSize.y),
            CellSize = authoring.cellSize,
            TilePrefab = GetEntity(authoring.tilePrefab, TransformUsageFlags.Dynamic),
        });


    }
}