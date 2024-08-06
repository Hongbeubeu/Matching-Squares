public class VoxelStencil
{
    protected bool _fillType;
    protected int _centerX, _centerY, _radius;
    
    public int XStart => _centerX - _radius;
    public int XEnd => _centerX + _radius;
    public int YStart => _centerY - _radius;
    public int YEnd => _centerY + _radius;

    public virtual void Initialize(bool initFillType, int initRadius)
    {
        _fillType = initFillType;
        _radius = initRadius;
    }

    public virtual bool Apply(int x, int y, bool voxel)
    {
        return _fillType;
    }

    public virtual void SetCenter(int x, int y)
    {
        _centerX = x;
        _centerY = y;
    }
}