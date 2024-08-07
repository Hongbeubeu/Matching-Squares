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
    private int[] _rowCacheMax, _rowCacheMin;
    private int _edgeCacheMax, _edgeCacheMin;

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
        _rowCacheMax = new int[_resolution * 2 + 1];
        _rowCacheMin = new int[_resolution * 2 + 1];
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
        var xStart = (int)(stencil.XStart / _voxelSize);
        if (xStart < 0) xStart = 0;

        var xEnd = (int)(stencil.XEnd / _voxelSize);
        if (xEnd >= _resolution) xEnd = _resolution - 1;

        var yStart = (int)(stencil.YStart / _voxelSize);
        if (yStart < 0) yStart = 0;

        var yEnd = (int)(stencil.YEnd / _voxelSize);
        if (yEnd >= _resolution) yEnd = _resolution - 1;

        for (var y = yStart; y <= yEnd; y++)
        {
            var i = y * _resolution + xStart;
            for (var x = xStart; x <= xEnd; x++, i++)
            {
                stencil.Apply(_voxels[i]);
            }
        }

        SetVoxelColors();
        SetCrossings(stencil, xStart, xEnd, yStart, yEnd);
        Refresh();
    }

    private void SetCrossings(VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd)
    {
        var crossHorizontalGap = false;
        var lastVerticalRow = false;
        var crossVerticalGap = false;

        if (xStart > 0) xStart -= 1;
        if (xEnd == _resolution - 1)
        {
            xEnd -= 1;
            crossHorizontalGap = xNeighbor != null;
        }

        if (yStart > 0) yStart -= 1;
        if (yEnd == _resolution - 1)
        {
            yEnd -= 1;
            lastVerticalRow = true;
            crossVerticalGap = yNeighbor != null;
        }

        Voxel a, b;
        for (var y = yStart; y <= yEnd; y++)
        {
            var i = y * _resolution + xStart;
            b = _voxels[i];
            for (var x = xStart; x <= xEnd; x++, i++)
            {
                a = b;
                b = _voxels[i + 1];
                stencil.SetHorizontalCrossing(a, b);
                stencil.SetVerticalCrossing(a, _voxels[i + _resolution]);
            }

            stencil.SetVerticalCrossing(b, _voxels[i + _resolution]);
            if (crossHorizontalGap)
            {
                _dummyX.BecomeXDummyOf(xNeighbor._voxels[y * _resolution], _gridSize);
                stencil.SetHorizontalCrossing(b, _dummyX);
            }
        }

        if (lastVerticalRow)
        {
            var i = _voxels.Length - _resolution + xStart;
            b = _voxels[i];
            for (var x = xStart; x <= xEnd; x++, i++) {
                a = b;
                b = _voxels[i + 1];
                stencil.SetHorizontalCrossing(a, b);
                if (crossVerticalGap) {
                    _dummyY.BecomeYDummyOf(yNeighbor._voxels[x], _gridSize);
                    stencil.SetVerticalCrossing(a, _dummyY);
                }
            }
            if (crossVerticalGap) {
                _dummyY.BecomeYDummyOf(yNeighbor._voxels[xEnd + 1], _gridSize);
                stencil.SetVerticalCrossing(b, _dummyY);
            }
            if (crossHorizontalGap) {
                _dummyX.BecomeXDummyOf(xNeighbor._voxels[_voxels.Length - _resolution], _gridSize);
                stencil.SetHorizontalCrossing(b, _dummyX);
            }
        }
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

        FillFirstRowCache();
        TriangulateCellRows();
        if (yNeighbor != null)
        {
            TriangulateGapRow();
        }

        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();
    }

    private void FillFirstRowCache()
    {
        CacheFirstCorner(_voxels[0]);
        int i;
        for (i = 0; i < _resolution - 1; i++)
        {
            CacheNextEdgeAndCorner(i * 2, _voxels[i], _voxels[i + 1]);
        }

        if (xNeighbor != null)
        {
            _dummyX.BecomeXDummyOf(xNeighbor._voxels[0], _gridSize);
            CacheNextEdgeAndCorner(i * 2, _voxels[i], _dummyX);
        }
    }

    private void CacheNextEdgeAndCorner(int i, Voxel xMin, Voxel xMax)
    {
        if (xMin.state != xMax.state)
        {
            _rowCacheMax[i + 1] = _vertices.Count;
            Vector3 p;
            p.x = xMin.xEdge;
            p.y = xMin.position.y;
            p.z = 0f;
            _vertices.Add(p);
        }

        if (xMax.state)
        {
            _rowCacheMax[i + 2] = _vertices.Count;
            _vertices.Add(xMax.position);
        }
    }

    private void CacheFirstCorner(Voxel voxel)
    {
        if (voxel.state)
        {
            _rowCacheMax[0] = _vertices.Count;
            _vertices.Add(voxel.position);
        }
    }

    private void TriangulateCellRows()
    {
        var cells = _resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++)
        {
            SwapRowCaches();
            CacheFirstCorner(_voxels[i + _resolution]);
            CacheNextMiddleEdge(_voxels[i], _voxels[i + _resolution]);
            for (var x = 0; x < cells; x++, i++)
            {
                Voxel a = _voxels[i],
                      b = _voxels[i + 1],
                      c = _voxels[i + _resolution],
                      d = _voxels[i + _resolution + 1];
                var cacheIndex = x * 2;
                CacheNextEdgeAndCorner(cacheIndex, c, d);
                CacheNextMiddleEdge(b, d);
                TriangulateCell(cacheIndex, a, b, c, d);
            }

            if (xNeighbor != null)
            {
                TriangulateGapCell(i);
            }
        }
    }

    private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
    {
        _edgeCacheMin = _edgeCacheMax;
        if (yMin.state != yMax.state)
        {
            _edgeCacheMax = _vertices.Count;
            Vector3 p;
            p.x = yMin.position.x;
            p.y = yMin.yEdge;
            p.z = 0f;
            _vertices.Add(p);
        }
    }

    private void SwapRowCaches()
    {
        (_rowCacheMin, _rowCacheMax) = (_rowCacheMax, _rowCacheMin);
    }

    private void TriangulateGapCell(int i)
    {
        var dummySwap = _dummyT;
        dummySwap.BecomeXDummyOf(xNeighbor._voxels[i + 1], _gridSize);
        _dummyT = _dummyX;
        _dummyX = dummySwap;
        var cacheIndex = (_resolution - 1) * 2;
        CacheNextEdgeAndCorner(cacheIndex, _voxels[i + _resolution], _dummyX);
        CacheNextMiddleEdge(_dummyT, _dummyX);
        TriangulateCell(cacheIndex, _voxels[i], _dummyT, _voxels[i + _resolution], _dummyX);
    }

    private void TriangulateGapRow()
    {
        _dummyY.BecomeYDummyOf(yNeighbor._voxels[0], _gridSize);
        var cells = _resolution - 1;
        var offset = cells * _resolution;
        SwapRowCaches();
        CacheFirstCorner(_dummyY);
        CacheNextMiddleEdge(_voxels[cells * _resolution], _dummyY);

        for (var x = 0; x < cells; x++)
        {
            var dummySwap = _dummyT;
            dummySwap.BecomeYDummyOf(yNeighbor._voxels[x + 1], _gridSize);
            _dummyT = _dummyY;
            _dummyY = dummySwap;
            var cacheIndex = x * 2;
            CacheNextEdgeAndCorner(cacheIndex, _dummyT, _dummyY);
            CacheNextMiddleEdge(_voxels[x + offset + 1], _dummyY);
            TriangulateCell(cacheIndex, _voxels[x + offset], _voxels[x + offset + 1], _dummyT, _dummyY);
        }

        if (xNeighbor != null)
        {
            _dummyT.BecomeXYDummyOf(xyNeighbor._voxels[0], _gridSize);
            var cacheIndex = cells * 2;
            CacheNextEdgeAndCorner(cacheIndex, _dummyY, _dummyT);
            CacheNextMiddleEdge(_dummyX, _dummyT);
            TriangulateCell(cacheIndex, _voxels[_voxels.Length - 1], _dummyX, _dummyY, _dummyT);
        }
    }

    private void TriangulateCell(int i, Voxel a, Voxel b, Voxel c, Voxel d)
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
                AddTriangle(_rowCacheMin[i], _edgeCacheMin, _rowCacheMin[i + 1]);
                break;
            case 2:
                AddTriangle(_rowCacheMin[i + 2], _rowCacheMin[i + 1], _edgeCacheMax);
                break;
            case 4:
                AddTriangle(_rowCacheMax[i], _rowCacheMax[i + 1], _edgeCacheMin);
                break;
            case 8:
                AddTriangle(_rowCacheMax[i + 2], _edgeCacheMax, _rowCacheMax[i + 1]);
                break;
            case 3:
                AddQuad(_rowCacheMin[i], _edgeCacheMin, _edgeCacheMax, _rowCacheMin[i + 2]);
                break;
            case 5:
                AddQuad(_rowCacheMin[i], _rowCacheMax[i], _rowCacheMax[i + 1], _rowCacheMin[i + 1]);
                break;
            case 10:
                AddQuad(_rowCacheMin[i + 1], _rowCacheMax[i + 1], _rowCacheMax[i + 2], _rowCacheMin[i + 2]);
                break;
            case 12:
                AddQuad(_edgeCacheMin, _rowCacheMax[i], _rowCacheMax[i + 2], _edgeCacheMax);
                break;
            case 15:
                AddQuad(_rowCacheMin[i], _rowCacheMax[i], _rowCacheMax[i + 2], _rowCacheMin[i + 2]);
                break;
            case 7:
                AddPentagon(_rowCacheMin[i], _rowCacheMax[i], _rowCacheMax[i + 1], _edgeCacheMax, _rowCacheMin[i + 2]);
                break;
            case 11:
                AddPentagon(_rowCacheMin[i + 2], _rowCacheMin[i], _edgeCacheMin, _rowCacheMax[i + 1],
                    _rowCacheMax[i + 2]);
                break;
            case 13:
                AddPentagon(_rowCacheMax[i], _rowCacheMax[i + 2], _edgeCacheMax, _rowCacheMin[i + 1], _rowCacheMin[i]);
                break;
            case 14:
                AddPentagon(_rowCacheMax[i + 2], _rowCacheMin[i + 2], _rowCacheMin[i + 1], _edgeCacheMin,
                    _rowCacheMax[i]);
                break;
            case 6:
                AddTriangle(_rowCacheMin[i + 2], _rowCacheMin[i + 1], _edgeCacheMax);
                AddTriangle(_rowCacheMax[i], _rowCacheMax[i + 1], _edgeCacheMin);
                break;
            case 9:
                AddTriangle(_rowCacheMin[i], _edgeCacheMin, _rowCacheMin[i + 1]);
                AddTriangle(_rowCacheMax[i + 2], _edgeCacheMax, _rowCacheMax[i + 1]);
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

    private void AddTriangle(int a, int b, int c)
    {
        _triangles.Add(a);
        _triangles.Add(b);
        _triangles.Add(c);
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

    private void AddQuad(int a, int b, int c, int d)
    {
        _triangles.Add(a);
        _triangles.Add(b);
        _triangles.Add(c);
        _triangles.Add(a);
        _triangles.Add(c);
        _triangles.Add(d);
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

    private void AddPentagon(int a, int b, int c, int d, int e)
    {
        _triangles.Add(a);
        _triangles.Add(b);
        _triangles.Add(c);
        _triangles.Add(a);
        _triangles.Add(c);
        _triangles.Add(d);
        _triangles.Add(a);
        _triangles.Add(d);
        _triangles.Add(e);
    }
}