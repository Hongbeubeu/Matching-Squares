public class VoxelStencilCircle : VoxelStencil
{
    private int sqrRadius;

    public override void Initialize(bool initFillType, int initRadius)
    {
        base.Initialize(initFillType, initRadius);
        sqrRadius = _radius * _radius;
    }

    public override bool Apply(int x, int y, bool voxel)
    {
        x -= _centerX;
        y -= _centerY;

        return x * x + y * y <= sqrRadius ? _fillType : voxel;
    }
}