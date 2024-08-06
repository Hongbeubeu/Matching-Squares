public class VoxelStencilCircle : VoxelStencil
{
    private int _sqrRadius;

    public override void Initialize(bool initFillType, int initRadius)
    {
        base.Initialize(initFillType, initRadius);
        _sqrRadius = _radius * _radius;
    }

    public override bool Apply(int x, int y, bool voxel)
    {
        x -= _centerX;
        y -= _centerY;

        return x * x + y * y <= _sqrRadius ? _fillType : voxel;
    }
}