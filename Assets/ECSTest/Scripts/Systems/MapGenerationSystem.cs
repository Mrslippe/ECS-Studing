using NUnit.Framework.Interfaces;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


[BurstCompile]
public partial struct MapGenerationSystem : ISystem
{
    // ÿ֡���ɵĸ�������
    private const int BatchSize = 100;

    

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MapGridSettings>();
        state.RequireForUpdate<MapGenerationState>();
        state.EntityManager.CreateEntity(typeof(MapGenerationState));

        // ȷ������ϵͳ����
        state.World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
        // �ӳٻ�ȡ����һ֡���ܻ�ȡ����
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var _ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();    
        var ecb = _ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var mapSettings = SystemAPI.GetSingleton<MapGridSettings>();
        var generationState = SystemAPI.GetSingletonRW<MapGenerationState>();

        int total = mapSettings.GridSize.x * mapSettings.GridSize.y;
        int generated = generationState.ValueRO.GeneratedLayers;
        int toGenerate = math.min(BatchSize, total - generated);

        if (toGenerate <= 0)
        {
            state.Enabled = false; // ������ϣ�����ϵͳ
            return;
        }

        var entityManager = state.EntityManager;

        // �ֽ׶����ɵ�ͼ
        state.Dependency = new GenerateGridJob
        {
            StartIndex = generated,
            Count = toGenerate,
            GridSize = mapSettings.GridSize,
            CellSize = mapSettings.CellSize,
            StartPosition = mapSettings.StartPosition,
            TilePrefab = mapSettings.TilePrefab,
            Ecb = ecb
        }.ScheduleParallel(toGenerate, 64, state.Dependency); ;

        generationState.ValueRW.GeneratedLayers += toGenerate; // �����ۼ�ֵ

    }



    [BurstCompile]
    public partial struct GenerateGridJob : IJobFor
    {
        public int StartIndex;
        public int Count;
        public int2 GridSize;
        public float CellSize;
        public float3 StartPosition;

        [ReadOnly] public Entity TilePrefab;

        public EntityCommandBuffer.ParallelWriter Ecb;


        public void Execute(int index)
        {
            bool isObstacle = false;

            int flatIndex = StartIndex + index;
            if (flatIndex >= GridSize.x * GridSize.y) 
                return; // ��ֹԽ��
            int x = flatIndex % GridSize.x;
            int y = flatIndex / GridSize.x;

            var grid = new Grid { Coordinate = new int2(x, y) };

            // ������������
            float3 worldPos = StartPosition + new float3(x * CellSize, 0, y * CellSize);

            // 30%���������ϰ���
            uint seed = (uint)(x * 0x9E3779B9 + y * 0x6E624EB7);
            if (Unity.Mathematics.Random.CreateFromIndex(seed).NextFloat() < 0.3f)
                isObstacle = true;
            else
                isObstacle = false;

            // �ȴ���ʵ����ͳһ������
            Entity entity = Ecb.Instantiate(index, TilePrefab);
            Ecb.SetComponent(index, entity, LocalTransform.FromPosition(worldPos));
            Ecb.AddComponent(index, entity, new Grid { Coordinate = new int2(x, y) });
            if (isObstacle)
            {
                Ecb.AddComponent(index, entity, new Obstacle { isObstacle = true });
                Ecb.AddComponent(index, entity, new HybridColor { Value = new float4(0f, 0f, 0f, 0f) });
                // Ecb.SetComponent(index, entity, new URPMaterialPropertyBaseColor { Value = new float4(0f, 0f, 0f, 0f) });
            }
            else
            {
                Ecb.AddComponent(index, entity, new HybridColor { Value = new float4(0.7215686f, 1f, 0.5882353f, 1f) });
                // Ecb.SetComponent(index, entity, new URPMaterialPropertyBaseColor 
                                                                // { Value = new float4(0.7215686f, 1f, 0.5882353f, 1f) });
            }
                

        }
    }

}
