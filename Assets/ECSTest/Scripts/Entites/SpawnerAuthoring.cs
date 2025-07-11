using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class SpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnRate;
    public float SpawnRadius;       // 新增：生成半径
    public float2 SpawnAreaSize;    // 新增：生成区域尺寸(XZ平面)
    public int SpawnCount;          // 新增：生成的实体数
}

class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Spawner
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            SpawnPosition = authoring.transform.position,
            NextSpawnTime = 0.0f,
            SpawnRate = authoring.SpawnRate,
            SpawnRadius = authoring.SpawnRadius,
            SpawnAreaSize = authoring.SpawnAreaSize,
            SpawnCount = authoring.SpawnCount

        });
    }
}
