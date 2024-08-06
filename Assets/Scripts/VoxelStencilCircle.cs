public class VoxelStencilCircle : VoxelStencil
{
    private int _sqrRadius;

    public override void Initialize(bool initFillType, int initRadius)
    {
        base.Initialize(initFillType, initRadius);
        _sqrRadius = radius * radius;
    }

    public override bool Apply(int x, int y, bool voxel)
    {
        x -= centerX;
        y -= centerY;

        return x * x + y * y <= _sqrRadius ? fillType : voxel;
    }
}