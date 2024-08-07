using System;
using UnityEngine;

[Serializable]
public class Voxel
{
    public bool state;
    public Vector2 position, xEdgePosition, yEdgePosition;
    public float xEdge, yEdge;
    public Voxel()
    {
    }

    public Voxel(int x, int y, float size)
    {
        position.x = (x + 0.5f) * size;
        position.y = (y + 0.5f) * size;

        xEdgePosition = position;
        xEdgePosition.x += size * 0.5f;
        yEdgePosition = position;
        yEdgePosition.y += size * 0.5f;

        xEdge = position.x + size * 0.5f;
        yEdge = position.y + size * 0.5f;
    }

    public void BecomeXDummyOf(Voxel voxel, float offset)
    {
        state = voxel.state;
        position = voxel.position;
        xEdgePosition = voxel.xEdgePosition;
        yEdgePosition = voxel.yEdgePosition;
        position.x += offset;
        xEdgePosition.x += offset;
        yEdgePosition.x += offset;
        xEdge = voxel.xEdge + offset;
        yEdge = voxel.yEdge;
    }

    public void BecomeYDummyOf(Voxel voxel, float offset)
    {
        state = voxel.state;
        position = voxel.position;
        xEdgePosition = voxel.xEdgePosition;
        yEdgePosition = voxel.yEdgePosition;
        position.y += offset;
        xEdgePosition.y += offset;
        yEdgePosition.y += offset;
        xEdge = voxel.xEdge;
        yEdge = voxel.yEdge + offset;
    }

    public void BecomeXYDummyOf(Voxel voxel, float offset)
    {
        state = voxel.state;
        position = voxel.position;
        xEdgePosition = voxel.xEdgePosition;
        yEdgePosition = voxel.yEdgePosition;
        position.x += offset;
        position.y += offset;
        xEdgePosition.x += offset;
        xEdgePosition.y += offset;
        yEdgePosition.x += offset;
        yEdgePosition.y += offset;
        xEdge = voxel.xEdge + offset;
        yEdge = voxel.yEdge + offset;
    }
}