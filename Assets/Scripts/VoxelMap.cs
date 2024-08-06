using System;
using UnityEngine;

public class VoxelMap : MonoBehaviour
{
    public float size = 2f;
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelGrid voxelGridPrefab;

    private VoxelGrid[] _chunks;
    private float _chunkSize, _voxelSize, _halfSize;

    #region Unity Event Functions

    private void Awake()
    {
        _halfSize = size * 0.5f;
        _chunkSize = size / chunkResolution;
        _voxelSize = _chunkSize / voxelResolution;

        _chunks = new VoxelGrid[chunkResolution * chunkResolution];
        for (int i = 0, y = 0; y < chunkResolution; y++)
        {
            for (var x = 0; x < chunkResolution; x++, i++)
            {
                CreateChunk(i, x, y);
            }
        }

        var box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(size, size);
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))
            {
                if (hitInfo.collider.gameObject == gameObject)
                {
                    EditVoxels(transform.InverseTransformPoint(hitInfo.point));
                }
            }
        }
    }

    private void EditVoxels(Vector3 point)
    {
        var voxelX = (int)((point.x + _halfSize) / _voxelSize);
        var voxelY = (int)((point.y + _halfSize) / _voxelSize);
        var chunkX = voxelX / voxelResolution;
        var chunkY = voxelY / voxelResolution;

        voxelX -= chunkX * voxelResolution;
        voxelY -= chunkY * voxelResolution;
        _chunks[chunkY * chunkResolution + chunkX].SetVoxel(voxelX, voxelY, true);
        Debug.Log($"{voxelX}, {voxelY} in chunk {chunkX}, {chunkY}");
    }

    #endregion


    private void CreateChunk(int i, int x, int y)
    {
        var chunk = Instantiate(voxelGridPrefab, transform, true);
        chunk.Initialize(voxelResolution, _chunkSize);
        chunk.transform.localPosition = new Vector3(x * _chunkSize - _halfSize, y * _chunkSize - _halfSize);
        _chunks[i] = chunk;
    }
}