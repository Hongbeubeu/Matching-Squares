using UnityEngine;

public class VoxelStencilCircle : VoxelStencil
{
    private float _sqrRadius;

    public override void Initialize(bool initFillType, float initRadius)
    {
        base.Initialize(initFillType, initRadius);
        _sqrRadius = radius * radius;
    }

    public override void Apply(Voxel voxel)
    {
        var x = voxel.position.x - centerX;
        var y = voxel.position.y - centerY;
        if (x * x + y * y <= _sqrRadius)
        {
            voxel.state = fillType;
        }
    }

    protected override void FindHorizontalCrossing(Voxel xMin, Voxel xMax)
    {
        var y2 = xMin.position.y - centerY;
        y2 *= y2;
        if (xMin.state == fillType)
        {
            var x = xMin.position.x - centerX;
            if (x * x + y2 <= _sqrRadius)
            {
                x = centerX + Mathf.Sqrt(_sqrRadius - y2);
                if (xMin.xEdge == float.MinValue || xMin.xEdge < x)
                {
                    xMin.xEdge = x;
                }
            }
        }
        else if (xMax.state == fillType)
        {
            var x = xMax.position.x - centerX;
            if (x * x + y2 <= _sqrRadius)
            {
                x = centerX - Mathf.Sqrt(_sqrRadius - y2);
                if (xMin.xEdge == float.MinValue || xMin.xEdge > x)
                {
                    xMin.xEdge = x;
                }
            }
        }
    }
    
    protected override void FindVerticalCrossing (Voxel yMin, Voxel yMax) {
        var x2 = yMin.position.x - centerX;
        x2 *= x2;
        if (yMin.state == fillType) {
            var y = yMin.position.y - centerY;
            if (y * y + x2 <= _sqrRadius) {
                y = centerY + Mathf.Sqrt(_sqrRadius - x2);
                if (yMin.yEdge == float.MinValue || yMin.yEdge < y) {
                    yMin.yEdge = y;
                }
            }
        }
        else if (yMax.state == fillType) {
            var y = yMax.position.y - centerY;
            if (y * y + x2 <= _sqrRadius) {
                y = centerY - Mathf.Sqrt(_sqrRadius - x2);
                if (yMin.yEdge == float.MinValue || yMin.yEdge > y) {
                    yMin.yEdge = y;
                }
            }
        }
    }
}