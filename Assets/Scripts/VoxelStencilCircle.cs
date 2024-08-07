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
}