using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct OptimizedSpawnerSystem : ISystem
{
    // ����ͳ������������
    private EntityQuery spawnerQuery;

    public void OnCreate(ref SystemState state) 
    {
        // ��ʼ������ʵ�壨����Random��Counter��
        //state.EntityManager.CreateEntity(
        //    typeof(RandomComponent),
        //    typeof(SpawnCounter)
        //);
        state.EntityManager.CreateEntity(typeof(RandomComponent));

        SystemAPI.SetSingleton(new RandomComponent { Random = new Unity.Mathematics.Random(123) });
        //SystemAPI.SetSingleton(new SpawnCounter { Count = 0 });

        // ������ѯ����ͳ������������
        spawnerQuery = SystemAPI.QueryBuilder()
            .WithAll<Spawner>()
            .Build();

        // ��ʵ���� Editor �ɼ�
        //#if UNITY_EDITOR
        //state.EntityManager.SetName(randomEntity, "RandomGenerator");
        //#endif



    }

    public void OnDestroy(ref SystemState state) { }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        var random = SystemAPI.GetSingletonRW<RandomComponent>();
        //var counter = SystemAPI.GetSingletonRW<SpawnCounter>();
        //Entity singletonEntity = SystemAPI.GetSingletonEntity<SpawnCounter>();
        //var counter = SystemAPI.GetComponentRW<SpawnCounter>(singletonEntity);

        // ��ȡ��ǰ����������
        int spawnerCount = spawnerQuery.CalculateEntityCount();

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        new ProcessSpawnerJob
        {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb,
            Random = random.ValueRO,
            SpawnerCount = spawnerCount
            //Counter = counter
        }.ScheduleParallel();

        // �������״̬��ȷ���´�ʹ����״̬��
        random.ValueRW.Random.NextFloat();
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
}

[BurstCompile]
public partial struct ProcessSpawnerJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public double ElapsedTime;
    public int SpawnerCount;

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

            // ���¼������̰߳�ȫ��ʽ��
            //Counter.ValueRW.Count += 1;

            // ���¼���
            spawner.SpawnCount += 1;

            // Resets the next spawn time.
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate * math.max(1, SpawnerCount / 10);
        }
    }
}
