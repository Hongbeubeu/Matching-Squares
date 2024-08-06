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

    public void SetVoxel(int x, int y, bool state)
    {
        _voxels[y * resolution + x] = state;
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