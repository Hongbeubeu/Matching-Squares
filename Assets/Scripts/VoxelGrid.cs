using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{
    public GameObject voxelPrefab;
    public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;

    private int _resolution;
    private float _voxelSize, _gridSize;
    private Voxel[] _voxels;
    private Material[] _voxelMaterials;
    private Mesh _mesh;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    private Voxel _dummyX, _dummyY, _dummyT;

    public void Initialize(int initResolution, float initSize)
    {
        _resolution = initResolution;
        _gridSize = initSize;

        _voxelSize = _gridSize / _resolution;
        var length = _resolution * _resolution;
        _voxels = new Voxel[length];
        _voxelMaterials = new Material[length];

        _dummyX = new Voxel();
        _dummyY = new Voxel();
        _dummyT = new Voxel();

        for (int i = 0, y = 0; y < initResolution; y++)
        {
            for (var x = 0; x < initResolution; x++, i++)
            {
                CreateVoxel(i, x, y);
            }
        }

        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
        _mesh.name = "VoxelGrid Mesh";
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
        Refresh();
    }

    private void CreateVoxel(int i, int x, int y)
    {
        var o = Instantiate(voxelPrefab, transform, true);
        o.transform.localPosition = new Vector3((x + 0.5f) * _voxelSize, (y + 0.5f) * _voxelSize, -0.01f);
        var voxelScale = Vector3.one * _voxelSize * 0.1f;
        o.transform.localScale = voxelScale;
        _voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
        _voxels[i] = new Voxel(x, y, _voxelSize);
    }

    public void Apply(VoxelStencil stencil)
    {
        var xStart = stencil.XStart;
        if (xStart < 0) xStart = 0;

        var xEnd = stencil.XEnd;
        if (xEnd >= _resolution) xEnd = _resolution - 1;

        var yStart = stencil.YStart;
        if (yStart < 0) yStart = 0;

        var yEnd = stencil.YEnd;
        if (yEnd >= _resolution) yEnd = _resolution - 1;

        for (var y = yStart; y <= yEnd; y++)
        {
            var i = y * _resolution + xStart;
            for (var x = xStart; x <= xEnd; x++, i++)
            {
                _voxels[i].state = stencil.Apply(x, y, _voxels[i].state);
            }
        }

        SetVoxelColors();
        Refresh();
    }

    private void Refresh()
    {
        SetVoxelColors();
        Triangulate();
    }

    private void SetVoxelColors()
    {
        for (var i = 0; i < _voxels.Length; i++)
        {
            _voxelMaterials[i].color = _voxels[i].state ? Color.black : Color.white;
        }
    }

    private void Triangulate()
    {
        _vertices.Clear();
        _triangles.Clear();
        _mesh.Clear();

        if (xNeighbor != null)
        {
            _dummyX.BecomeXDummyOf(xNeighbor._voxels[0], _gridSize);
        }

        TriangulateCellRows();
        if (yNeighbor != null)
        {
            TriangulateGapRow();
        }

        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();
    }

    private void TriangulateCellRows()
    {
        var cells = _resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++)
        {
            for (var x = 0; x < cells; x++, i++)
            {
                TriangulateCell(
                    _voxels[i],
                    _voxels[i + 1],
                    _voxels[i + _resolution],
                    _voxels[i + _resolution + 1]
                );
            }

            if (xNeighbor != null)
            {
                TriangulateGapCell(i);
            }
        }
    }

    private void TriangulateGapCell(int i)
    {
        var dummySwap = _dummyT;
        dummySwap.BecomeXDummyOf(xNeighbor._voxels[i + 1], _gridSize);
        _dummyT = _dummyX;
        _dummyX = dummySwap;
        TriangulateCell(_voxels[i], _dummyT, _voxels[i + _resolution], _dummyX);
    }

    private void TriangulateGapRow()
    {
        _dummyY.BecomeYDummyOf(yNeighbor._voxels[0], _gridSize);
        var cells = _resolution - 1;
        var offset = cells * _resolution;

        for (var x = 0; x < cells; x++)
        {
            var dummySwap = _dummyT;
            dummySwap.BecomeYDummyOf(yNeighbor._voxels[x + 1], _gridSize);
            _dummyT = _dummyY;
            _dummyY = dummySwap;
            TriangulateCell(_voxels[x + offset], _voxels[x + offset + 1], _dummyT, _dummyY);
        }

        if (xNeighbor != null)
        {
            _dummyT.BecomeXYDummyOf(xyNeighbor._voxels[0], _gridSize);
            TriangulateCell(_voxels[_voxels.Length - 1], _dummyX, _dummyY, _dummyT);
        }
    }

    private void TriangulateCell(Voxel a, Voxel b, Voxel c, Voxel d)
    {
        var cellType = 0;
        if (a.state) cellType |= 1;
        if (b.state) cellType |= 2;
        if (c.state) cellType |= 4;
        if (d.state) cellType |= 8;

        switch (cellType)
        {
            case 0:
                return;
            case 1:
                AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
                break;
            case 2:
                AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                break;
            case 4:
                AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                break;
            case 8:
                AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                break;
            case 3:
                AddQuad(a.position, a.yEdgePosition, b.yEdgePosition, b.position);
                break;
            case 5:
                AddQuad(a.position, c.position, c.xEdgePosition, a.xEdgePosition);
                break;
            case 10:
                AddQuad(a.xEdgePosition, c.xEdgePosition, d.position, b.position);
                break;
            case 12:
                AddQuad(a.yEdgePosition, c.position, d.position, b.yEdgePosition);
                break;
            case 15:
                AddQuad(a.position, c.position, d.position, b.position);
                break;
            case 7:
                AddPentagon(a.position, c.position, c.xEdgePosition, b.yEdgePosition, b.position);
                break;
            case 11:
                AddPentagon(b.position, a.position, a.yEdgePosition, c.xEdgePosition, d.position);
                break;
            case 13:
                AddPentagon(c.position, d.position, b.yEdgePosition, a.xEdgePosition, a.position);
                break;
            case 14:
                AddPentagon(d.position, b.position, a.xEdgePosition, a.yEdgePosition, c.position);
                break;
            case 6:
                AddTriangle(b.position, a.xEdgePosition, b.yEdgePosition);
                AddTriangle(c.position, c.xEdgePosition, a.yEdgePosition);
                break;
            case 9:
                AddTriangle(a.position, a.yEdgePosition, a.xEdgePosition);
                AddTriangle(d.position, b.yEdgePosition, c.xEdgePosition);
                break;
        }
    }

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        var vertexIndex = _vertices.Count;
        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
    }

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var vertexIndex = _vertices.Count;
        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _vertices.Add(d);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 3);
    }

    private void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
    {
        var vertexIndex = _vertices.Count;
        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _vertices.Add(d);
        _vertices.Add(e);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 1);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 2);
        _triangles.Add(vertexIndex + 3);
        _triangles.Add(vertexIndex);
        _triangles.Add(vertexIndex + 3);
        _triangles.Add(vertexIndex + 4);
    }
}