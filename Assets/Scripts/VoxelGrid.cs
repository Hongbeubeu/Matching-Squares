using UnityEngine;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{
    public GameObject voxelPrefab;
    public int resolution;
    private float _voxelSize;
    private bool[] _voxels;
    private Material[] _voxelMaterials;

    public void Initialize(int initResolution, float initSize)
    {
        resolution = initResolution;
        _voxelSize = initSize / resolution;
        var length = resolution * resolution;
        _voxels = new bool[length];
        _voxelMaterials = new Material[length];

        for (int i = 0, y = 0; y < initResolution; y++)
        {
            for (var x = 0; x < initResolution; x++, i++)
            {
                CreateVoxel(i, x, y);
            }
        }

        SetVoxelColors();
    }

    private void CreateVoxel(int i, int x, int y)
    {
        var o = Instantiate(voxelPrefab, transform, true);
        o.transform.localPosition = new Vector3((x + 0.5f) * _voxelSize, (y + 0.5f) * _voxelSize);
        var voxelScale = Vector3.one * _voxelSize * 0.9f;
        o.transform.localScale = voxelScale;
        _voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
    }

    public void Apply(VoxelStencil stencil)
    {
        var xStart = stencil.XStart;
        if (xStart < 0) xStart = 0;

        var xEnd = stencil.XEnd;
        if (xEnd >= resolution) xEnd = resolution - 1;

        var yStart = stencil.YStart;
        if (yStart < 0) yStart = 0;

        var yEnd = stencil.YEnd;
        if (yEnd >= resolution) yEnd = resolution - 1;

        for (var y = yStart; y <= yEnd; y++)
        {
            var i = y * resolution + xStart;
            for (var x = xStart; x <= xEnd; x++, i++)
            {
                _voxels[i] = stencil.Apply(x, y, _voxels[i]);
            }
        }

        SetVoxelColors();
    }

    private void SetVoxelColors()
    {
        for (var i = 0; i < _voxels.Length; i++)
        {
            _voxelMaterials[i].color = _voxels[i] ? Color.black : Color.white;
        }
    }
}