﻿using UnityEngine;

public class VoxelStencil
{
    protected bool fillType;
    protected float centerX, centerY, radius;

    public float XStart => centerX - radius;
    public float XEnd => centerX + radius;
    public float YStart => centerY - radius;
    public float YEnd => centerY + radius;

    public virtual void Initialize(bool initFillType, float initRadius)
    {
        fillType = initFillType;
        radius = initRadius;
    }

    public virtual void Apply(Voxel voxel)
    {
        var p = voxel.position;
        if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd)
        {
            voxel.state = fillType;
        }
    }

    public virtual void SetCenter(float x, float y)
    {
        centerX = x;
        centerY = y;
    }

    public void SetHorizontalCrossing(Voxel xMin, Voxel xMax)
    {
        if (xMin.state != xMax.state)
        {
            FindHorizontalCrossing(xMin, xMax);
        }
    }

    public void SetVerticalCrossing(Voxel yMin, Voxel yMax)
    {
        if (yMin.state != yMax.state)
        {
            FindVerticalCrossing(yMin, yMax);
        }
    }

    protected virtual void FindHorizontalCrossing(Voxel xMin, Voxel xMax)
    {
        if (xMin.position.y < YStart || xMin.position.y > YEnd)
        {
            return;
        }

        if (xMin.state == fillType)
        {
            if (xMin.position.x <= XEnd && xMax.position.x >= XEnd)
            {
                xMin.xEdge = XEnd;
            }
        }
        else if (xMax.state == fillType)
        {
            if (xMin.position.x <= XStart && xMax.position.x >= XStart)
            {
                xMax.xEdge = XStart;
            }
        }
    }

    protected virtual void FindVerticalCrossing(Voxel yMin, Voxel yMax)
    {
        if (yMin.position.x < XStart || yMin.position.x > XEnd)
        {
            return;
        }

        if (yMin.state == fillType)
        {
            if (yMin.position.y <= YEnd && yMax.position.y >= YEnd)
            {
                yMin.yEdge = YEnd;
            }
        }
        else if (yMax.state == fillType)
        {
            if (yMin.position.y <= YStart && yMax.position.y >= YStart)
            {
                yMax.yEdge = YStart;
            }
        }
    }
}