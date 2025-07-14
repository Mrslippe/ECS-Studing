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
    // 用于统计所有生成器
    private EntityQuery spawnerQuery;

    // 计时器
    private float _lastDisplayTime;
    private int _cachedTotalCount;

    public void OnCreate(ref SystemState state) 
    {
        // 初始化单例实体（包含Random和Counter）
        //state.EntityManager.CreateEntity(
        //    typeof(RandomComponent),
        //    typeof(SpawnCounter)
        //);
        state.EntityManager.CreateEntity(typeof(RandomComponent));
        SystemAPI.SetSingleton(new RandomComponent { Random = new Unity.Mathematics.Random(123) });

        //SystemAPI.SetSingleton(new SpawnCounter { Count = 0 });

        // 创建查询用于统计生成器数量
        spawnerQuery = SystemAPI.QueryBuilder()
            .WithAll<Spawner>()
            .Build();


        _lastDisplayTime = 0;
        _cachedTotalCount = 0;

        // 让实体在 Editor 可见
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

        // 获取当前生成器数量
        int spawnerCount = spawnerQuery.CalculateEntityCount();

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        state.Dependency = new ProcessSpawnerJob
        {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb,
            Random = random.ValueRO,
            SpawnerCount = spawnerCount,
        }.ScheduleParallel(spawnerQuery, state.Dependency);

        // 更新随机状态（确保下次使用新状态）
        random.ValueRW.Random.NextFloat();


        // 每0.5秒更新一次显示（避免每帧计算）
        float currentTime = (float)SystemAPI.Time.ElapsedTime;
        if (currentTime - _lastDisplayTime > 0.5f)
        {
            state.Dependency.Complete(); // 确保Job完成
            _lastDisplayTime = currentTime;
            _cachedTotalCount = CalculateTotalSpawnCount(ref state);

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"总生成数: {_cachedTotalCount} (来自 {spawnerQuery.CalculateEntityCount()} 个生成器)");
#endif
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
    /// 整体计数
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
    public int SpawnerCount;          // 生成器计数

    // 随机组件引用
    public RandomComponent Random;

    [NativeDisableUnsafePtrRestriction]
    // 可读写计数器
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
            // 生成实体
            Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);

            // 计算位置
            float3 randomOffset = Random.Random.NextFloat3Direction() * spawner.SpawnRadius;
            float3 spawnPosition = spawner.SpawnPosition + randomOffset;

            // 添加组件
            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(spawnPosition));

            // 更新计数（线程安全方式）
            //Counter.ValueRW.Count += 1;

            // 更新计数
            spawner.SpawnCount += 1;

            // Resets the next spawn time.
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate * math.max(1, SpawnerCount / 10);
        }
    }
}
