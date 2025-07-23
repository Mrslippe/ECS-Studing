using NUnit.Framework.Interfaces;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.EventSystems.EventTrigger;


[BurstCompile]
public partial struct MapGenerationSystem : ISystem
{
    // 每帧生成的格子数量
    private const int BatchSize = 10;


    public void OnCreate(ref SystemState state)
    {
        // 依赖声明
        //state.RequireForUpdate<MapGridSettings>();
        //state.RequireForUpdate<MapGenerationState>();

        state.EntityManager.CreateEntity(typeof(MapGenerationState));
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var mapSettings = SystemAPI.GetSingleton<MapGridSettings>();
        var generationState = SystemAPI.GetSingletonRW<MapGenerationState>();

        int total = mapSettings.GridSize.x * mapSettings.GridSize.y;
        int generated = generationState.ValueRO.GeneratedLayers;
        int toGenerate = math.min(BatchSize, total - generated);

        if (toGenerate <= 0)
        {
            state.Enabled = false; // 生成完毕，禁用系统
            return;
        }


        var entityManager = state.EntityManager;
        // var ecb = new EntityCommandBuffer(Allocator.TempJob);
        EntityCommandBuffer ecb = GetEntityCommandBuffer(ref state);

        // 分阶段生成地图
        new GenerateGridJob
        {
            StartIndex = generated,
            Count = toGenerate,
            GridSize = mapSettings.GridSize,
            CellSize = mapSettings.CellSize,
            StartPosition = mapSettings.StartPosition,
            TilePrefab = mapSettings.TilePrefab,
            ObstaclePrefab = mapSettings.ObstaclePrefab,
            Ecb = ecb.AsParallelWriter()
        }.ScheduleParallel(state.Dependency);

        state.Dependency.Complete();

        ecb.Playback(entityManager);
        ecb.Dispose();

        generationState.ValueRW.GeneratedLayers += generated;

    }


    private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton(out BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton))
        {
            throw new SystemException("ECB System not found");
        }
        ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb;
    }


    [BurstCompile]
    public partial struct GenerateGridJob : IJobEntity
    {
        public int StartIndex;
        public int Count;

        public int2 GridSize;
        public float CellSize;
        public float3 StartPosition;
        public Entity TilePrefab;
        public Entity ObstaclePrefab;

        public EntityCommandBuffer.ParallelWriter Ecb;


        public void Execute([EntityIndexInQuery] int index)
        {
            int flatIndex = StartIndex + index;
            int x = flatIndex % GridSize.x;
            int y = flatIndex / GridSize.x;
            var gridEntity = Ecb.CreateEntity(index);
            var gridPos = new GridPosition { Coordinate = new int2(x, y) };
            Ecb.AddComponent(index, gridEntity, gridPos);

            // 计算世界坐标
            float3 worldPos = StartPosition + new float3(x * CellSize, 0, y * CellSize);

            // 创建地形瓦片
            Entity tile = Ecb.Instantiate(index, TilePrefab);
            Ecb.SetComponent(index, tile, LocalTransform.FromPosition(worldPos));

            // 30%概率生成障碍物
            uint seed = (uint)(x * 73856093 ^ y * 19349663);
            if (Unity.Mathematics.Random.CreateFromIndex(seed).NextFloat() < 0.3f)
            {
                Entity obstacle = Ecb.Instantiate(index, ObstaclePrefab);
                Ecb.SetComponent(index, obstacle,
                    LocalTransform.FromPosition(worldPos + new float3(0, 0.1f, 0)));
            }
        }
    }

}
