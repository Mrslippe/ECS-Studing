using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ����
public enum TerrainType { Ground, Water, Mountain }

/// <summary>
/// ��������ͼ��������
/// </summary>
public struct MapGridSettings : IComponentData
{
    public float3 StartPosition;   // ��ʼ���������λ��
    public int2 GridSize;          // ����ߴ� (�� 10x10 )
    public float CellSize;         // ÿ�����ӵ�λ�ߴ�
    public Entity TilePrefab;      // ������ƬԤ����
    public Entity ObstaclePrefab;  // �ϰ���Ԥ����
}

/// <summary>
/// ����λ��
/// </summary>
public struct GridPosition : IComponentData
{
    public int2 Coordinate;  // �������� (�� [2,3])
}


/// <summary>
/// ���ͼ�����ϵ��
/// </summary>
public struct Terrain : IComponentData
{
    public TerrainType Type;
    public float MovementCost; // �ƶ�����ϵ��
}


/// <summary>
/// ��ͼ����״̬
/// </summary>
public struct MapGenerationState : IComponentData
{
    public int GeneratedLayers; // �����ɲ���
    public float NextGenerateTime;
}