public class VoxelStencil
{
    protected bool fillType;
    protected int centerX, centerY, radius;
    
    public int XStart => centerX - radius;
    public int XEnd => centerX + radius;
    public int YStart => centerY - radius;
    public int YEnd => centerY + radius;

    public virtual void Initialize(bool initFillType, int initRadius)
    {
        fillType = initFillType;
        radius = initRadius;
    }

    public virtual bool Apply(int x, int y, bool voxel)
    {
        return fillType;
    }

    public virtual void SetCenter(int x, int y)
    {
        centerX = x;
        centerY = y;
    }
}