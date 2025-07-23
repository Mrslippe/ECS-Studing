using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// 地型
public enum TerrainType { Ground, Water, Mountain }

/// <summary>
/// 单例，地图网格设置
/// </summary>
public struct MapGridSettings : IComponentData
{
    public float3 StartPosition;   // 开始生成网格的位置
    public int2 GridSize;          // 网格尺寸 (如 10x10 )
    public float CellSize;         // 每个格子单位尺寸
    public Entity TilePrefab;      // 基础瓦片预制体
    public Entity ObstaclePrefab;  // 障碍物预制体
}

/// <summary>
/// 网格位置
/// </summary>
public struct GridPosition : IComponentData
{
    public int2 Coordinate;  // 网格坐标 (如 [2,3])
}


/// <summary>
/// 地型及消耗系数
/// </summary>
public struct Terrain : IComponentData
{
    public TerrainType Type;
    public float MovementCost; // 移动消耗系数
}


/// <summary>
/// 地图生成状态
/// </summary>
public struct MapGenerationState : IComponentData
{
    public int GeneratedLayers; // 已生成层数
    public float NextGenerateTime;
}