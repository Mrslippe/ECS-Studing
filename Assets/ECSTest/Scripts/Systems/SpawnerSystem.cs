using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct OptimizedSpawnerSystem : ISystem
{
    private Entity randomEntity;

    public void OnCreate(ref SystemState state) 
    {
        //创建包含RandomCompoent组件的实体
        randomEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(randomEntity, new RandomComponent
        {
            Random = new Random(123)
        });

        // 让实体在 Editor 可见
        #if UNITY_EDITOR
        state.EntityManager.SetName(randomEntity, "RandomGenerator");
        #endif

    }

    public void OnDestroy(ref SystemState state) { }




    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        //var random = SystemAPI.GetComponent<RandomComponent>(randomEntity);
        var random = SystemAPI.GetComponentRW<RandomComponent>(randomEntity);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        new ProcessSpawnerJob
        {
            ElapsedTime = SystemAPI.Time.ElapsedTime,
            Ecb = ecb,
            Random = random.ValueRO
        }.ScheduleParallel();

        // 更新随机状态（确保下次使用新状态）
        random.ValueRW.Random.NextFloat();

    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

[BurstCompile]
public partial struct ProcessSpawnerJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public double ElapsedTime;
    public RandomComponent Random;  // 随机组件引用

    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, ref Spawner spawner)
    {
        // If the next spawn time has passed.
        if (spawner.NextSpawnTime < ElapsedTime)
        {
            // Spawns a new entity and positions it at the spawner.
            Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);

            float3 randomOffset = Random.Random.NextFloat3Direction() * spawner.SpawnRadius;
            float3 spawnPosition = spawner.SpawnPosition + randomOffset;

            Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(spawnPosition));

            // Resets the next spawn time.
            spawner.NextSpawnTime = (float)ElapsedTime + spawner.SpawnRate;
        }
    }
}
