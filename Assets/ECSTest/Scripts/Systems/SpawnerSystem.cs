using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct OptimizedSpawnerSystem : ISystem
{
    // ��ʱ��
    private float _lastDisplayTime;
    private int _cachedTotalCount;

    public void OnCreate(ref SystemState state) 
    {
        state.EntityManager.CreateEntity(typeof(RandomComponent));
        SystemAPI.SetSingleton(new RandomComponent { Random = new Unity.Mathematics.Random(123) });

        _lastDisplayTime = 0;
        _cachedTotalCount = 0;

    }

    public void OnDestroy(ref SystemState state) { }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        var random = SystemAPI.GetSingletonRW<RandomComponent>();
        var spawnerQuery = SystemAPI.QueryBuilder().WithAll<Spawner>().Build();


        // ��ȡ��ǰ����������
        int spawnerCount = spawnerQuery.CalculateEntityCount();

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        state.Dependency = new ProcessSpawnerJob
        {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb,
            Random = random.ValueRO,
            SpawnerCount = spawnerCount,
        }.ScheduleParallel(spawnerQuery, state.Dependency);

        // �������״̬��ȷ���´�ʹ����״̬��
        random.ValueRW.Random.NextFloat();


        // ÿ0.5�����һ����ʾ������ÿ֡���㣩
        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        if (currentTime - _lastDisplayTime > 1f)
        {
            state.Dependency.Complete(); // ȷ��Job���
            _lastDisplayTime = currentTime;
            _cachedTotalCount = CalculateTotalSpawnCount(ref state);

//#if UNITY_EDITOR
//          UnityEngine.Debug.Log($"��������: {_cachedTotalCount} (���� {spawnerQuery.CalculateEntityCount()} ��������)");
//#endif
        }

    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton(out BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton))
        {
            throw new SystemException("ECB System not found");
        }
        ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }


    /// <summary>
    /// �������
    /// </summary>
    /// <returns></returns>
    private int CalculateTotalSpawnCount(ref SystemState state)
    {
        int total = 0;
        foreach (var spawner in SystemAPI.Query<RefRO<Spawner>>())
        {
            total += spawner.ValueRO.SpawnCount;
        }
        return total;
    }
}

[BurstCompile]
public partial struct ProcessSpawnerJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public double ElapsedTime;
    public int SpawnerCount;          // ����������

    // ����������
    public RandomComponent Random;

    [NativeDisableUnsafePtrRestriction]
    // �ɶ�д������
    public RefRW<SpawnCounter> Counter;

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, ref Spawner spawner)
    {
        // If the next spawn time has passed.
        if (spawner.NextSpawnTime < ElapsedTime)
        {
            // ����ʵ��
            Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);

            // ����λ��
            float3 randomOffset = Random.Random.NextFloat3Direction() * spawner.SpawnRadius;
            float3 spawnPosition = spawner.SpawnPosition + randomOffset;

            // ������
            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(spawnPosition));

            // ���¼���
            spawner.SpawnCount += 1;

            // Resets the next spawn time.
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate * math.max(1, SpawnerCount / 10);
        }
    }
}
