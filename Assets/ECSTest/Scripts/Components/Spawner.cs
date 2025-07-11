using Unity.Entities;
using Unity.Mathematics;

public struct Spawner : IComponentData
{
    public Entity Prefab;
    public float3 SpawnPosition;
    public float NextSpawnTime;
    public float SpawnRate;

    public float SpawnRadius;       // 新增：生成半径
    public float2 SpawnAreaSize;    // 新增：生成区域尺寸(XZ平面)
}
