using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class SpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnRate;
    public float SpawnRadius;       // ���������ɰ뾶
    public float2 SpawnAreaSize;    // ��������������ߴ�(XZƽ��)
    public int SpawnCount;          // ���������ɵ�ʵ����
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
