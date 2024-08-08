using System.Collections.Generic;
using UnityEngine;

namespace MatchingSquare
{
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
        private float _sharpFeatureLimit;

        public void Initialize(int initResolution, float initSize, float maxFeatureAngle)
        {
            _resolution = initResolution;
            _gridSize = initSize;
            _sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.Deg2Rad);

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
                for (var x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = _voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    if (crossVerticalGap)
                    {
                        _dummyY.BecomeYDummyOf(yNeighbor._voxels[x], _gridSize);
                        stencil.SetVerticalCrossing(a, _dummyY);
                    }
                }

                if (crossVerticalGap)
                {
                    _dummyY.BecomeYDummyOf(yNeighbor._voxels[xEnd + 1], _gridSize);
                    stencil.SetVerticalCrossing(b, _dummyY);
                }

                if (crossHorizontalGap)
                {
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
                    TriangulateCase0(i, a, b, c, d);
                    break;
                case 1:
                    TriangulateCase1(i, a, b, c, d);
                    break;
                case 2:
                    TriangulateCase2(i, a, b, c, d);
                    break;
                case 3:
                    TriangulateCase3(i, a, b, c, d);
                    break;
                case 4:
                    TriangulateCase4(i, a, b, c, d);
                    break;
                case 5:
                    TriangulateCase5(i, a, b, c, d);
                    break;
                case 6:
                    TriangulateCase6(i, a, b, c, d);
                    break;
                case 7:
                    TriangulateCase7(i, a, b, c, d);
                    break;
                case 8:
                    TriangulateCase8(i, a, b, c, d);
                    break;
                case 9:
                    TriangulateCase9(i, a, b, c, d);
                    break;
                case 10:
                    TriangulateCase10(i, a, b, c, d);
                    break;
                case 11:
                    TriangulateCase11(i, a, b, c, d);
                    break;
                case 12:
                    TriangulateCase12(i, a, b, c, d);
                    break;
                case 13:
                    TriangulateCase13(i, a, b, c, d);
                    break;
                case 14:
                    TriangulateCase14(i, a, b, c, d);
                    break;
                case 15:
                    TriangulateCase15(i, a, b, c, d);
                    break;
            }
        }

        private void TriangulateCase0(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
        }

        private void TriangulateCase15(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            AddQuadABCD(i);
        }

        private void TriangulateCase1(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (ClampToCellMaxMax(ref point, a, d))
                {
                    AddQuadA(i, point);
                    return;
                }
            }

            AddTriangleA(i);
        }

        private void TriangulateCase2(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (ClampToCellMinMax(ref point, a, d))
                {
                    AddQuadB(i, point);
                    return;
                }
            }

            AddTriangleB(i);
        }

        private void TriangulateCase4(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (ClampToCellMaxMin(ref point, a, d))
                {
                    AddQuadC(i, point);
                    return;
                }
            }

            AddTriangleC(i);
        }

        private void TriangulateCase8(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (ClampToCellMinMin(ref point, a, d))
                {
                    AddQuadD(i, point);
                    return;
                }
            }

            AddTriangleD(i);
        }

        private void TriangulateCase7(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonABC(i, point);
                    return;
                }
            }

            AddPentagonABC(i);
        }

        private void TriangulateCase11(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = c.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonABD(i, point);
                    return;
                }
            }

            AddPentagonABD(i);
        }

        private void TriangulateCase13(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonACD(i, point);
                    return;
                }
            }

            AddPentagonACD(i);
        }

        private void TriangulateCase14(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddHexagonBCD(i, point);
                    return;
                }
            }

            AddPentagonBCD(i);
        }

        private void TriangulateCase3(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.yNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonAB(i, point);
                    return;
                }
            }

            AddQuadAB(i);
        }

        private void TriangulateCase5(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = c.xNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonAC(i, point);
                    return;
                }
            }

            AddQuadAC(i);
        }

        private void TriangulateCase10(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = c.xNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, c.XEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonBD(i, point);
                    return;
                }
            }

            AddQuadBD(i);
        }

        private void TriangulateCase12(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.yNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.YEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d))
                {
                    AddPentagonCD(i, point);
                    return;
                }
            }

            AddQuadCD(i);
        }

        private void TriangulateCase6(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            bool sharp1, sharp2;
            Vector2 point1, point2;

            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point1 = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                sharp1 = ClampToCellMinMax(ref point1, a, d);
            }
            else
            {
                point1.x = point1.y = 0f;
                sharp1 = false;
            }

            n1 = c.xNormal;
            n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point2 = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                sharp2 = ClampToCellMaxMin(ref point2, a, d);
            }
            else
            {
                point2.x = point2.y = 0f;
                sharp2 = false;
            }

            if (sharp1)
            {
                if (sharp2)
                {
                    if (IsBelowLine(point2, a.XEdgePoint, point1))
                    {
                        if (IsBelowLine(point2, point1, b.YEdgePoint) || IsBelowLine(point1, point2, a.YEdgePoint))
                        {
                            TriangulateCase6Connected(i, a, b, c, d);
                            return;
                        }
                    }
                    else if (IsBelowLine(point2, point1, b.YEdgePoint) && IsBelowLine(point1, c.XEdgePoint, point2))
                    {
                        TriangulateCase6Connected(i, a, b, c, d);
                        return;
                    }

                    AddQuadB(i, point1);
                    AddQuadC(i, point2);
                    return;
                }

                if (IsBelowLine(point1, c.XEdgePoint, a.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }

                AddQuadB(i, point1);
                AddTriangleC(i);
                return;
            }

            if (sharp2)
            {
                if (IsBelowLine(point2, a.XEdgePoint, b.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }

                AddTriangleB(i);
                AddQuadC(i, point2);
                return;
            }

            AddTriangleB(i);
            AddTriangleC(i);
        }

        private void TriangulateCase6Connected(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, c.position, b.position))
                {
                    AddPentagonBCToA(i, point);
                }
                else
                {
                    AddQuadBCToA(i);
                }
            }
            else
            {
                AddQuadBCToA(i);
            }

            n1 = c.xNormal;
            n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, b.position, c.position))
                {
                    AddPentagonBCToD(i, point);
                    return;
                }
            }

            AddQuadBCToD(i);
        }

        private void TriangulateCase9(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            bool sharp1, sharp2;
            Vector2 point1, point2;
            Vector2 n1 = a.xNormal;
            Vector2 n2 = a.yNormal;

            if (IsSharpFeature(n1, n2))
            {
                point1 = ComputeIntersection(a.XEdgePoint, n1, a.YEdgePoint, n2);
                sharp1 = ClampToCellMaxMax(ref point1, a, d);
            }
            else
            {
                point1.x = point1.y = 0f;
                sharp1 = false;
            }

            n1 = c.xNormal;
            n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                point2 = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                sharp2 = ClampToCellMinMin(ref point2, a, d);
            }
            else
            {
                point2.x = point2.y = 0f;
                sharp2 = false;
            }

            if (sharp1)
            {
                if (sharp2)
                {
                    if (IsBelowLine(point1, b.YEdgePoint, point2))
                    {
                        if (IsBelowLine(point1, point2, c.XEdgePoint) || IsBelowLine(point2, point1, a.XEdgePoint))
                        {
                            TriangulateCase9Connected(i, a, b, c, d);
                            return;
                        }
                    }
                    else if (IsBelowLine(point1, point2, c.XEdgePoint) && IsBelowLine(point2, a.YEdgePoint, point1))
                    {
                        TriangulateCase9Connected(i, a, b, c, d);
                        return;
                    }

                    AddQuadA(i, point1);
                    AddQuadD(i, point2);
                    return;
                }

                if (IsBelowLine(point1, b.YEdgePoint, c.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }

                AddQuadA(i, point1);
                AddTriangleD(i);
                return;
            }

            if (sharp2)
            {
                if (IsBelowLine(point2, a.YEdgePoint, a.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }

                AddTriangleA(i);
                AddQuadD(i, point2);
                return;
            }

            AddTriangleA(i);
            AddTriangleD(i);
        }

        private void TriangulateCase9Connected(int i, Voxel a, Voxel b, Voxel c, Voxel d)
        {
            Vector2 n1 = a.xNormal;
            Vector2 n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(a.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, a.position, d.position))
                {
                    AddPentagonADToB(i, point);
                }
                else
                {
                    AddQuadADToB(i);
                }
            }
            else
            {
                AddQuadADToB(i);
            }

            n1 = c.xNormal;
            n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, d.position, a.position))
                {
                    AddPentagonADToC(i, point);
                    return;
                }
            }

            AddQuadADToC(i);
        }

        private void AddQuadABCD(int i)
        {
            AddQuad(_rowCacheMin[i], _rowCacheMax[i], _rowCacheMax[i + 2], _rowCacheMin[i + 2]);
        }

        private void AddQuadBCToA(int i)
        {
            AddQuad(_edgeCacheMin, _rowCacheMax[i], _rowCacheMin[i + 2], _rowCacheMin[i + 1]);
        }

        private void AddPentagonBCToA(int i, Vector2 extraVertex)
        {
            AddPentagon(_vertices.Count, _edgeCacheMin, _rowCacheMax[i], _rowCacheMin[i + 2], _rowCacheMin[i + 1]);
            _vertices.Add(extraVertex);
        }

        private void AddQuadBCToD(int i)
        {
            AddQuad(_edgeCacheMax, _rowCacheMin[i + 2], _rowCacheMax[i], _rowCacheMax[i + 1]);
        }

        private void AddPentagonBCToD(int i, Vector2 extraVertex)
        {
            AddPentagon(_vertices.Count, _edgeCacheMax, _rowCacheMin[i + 2], _rowCacheMax[i], _rowCacheMax[i + 1]);
            _vertices.Add(extraVertex);
        }


        private void AddQuadADToB(int i)
        {
            AddQuad(_rowCacheMin[i + 1], _rowCacheMin[i], _rowCacheMax[i + 2], _edgeCacheMax);
        }

        private void AddPentagonADToB(int i, Vector2 extraVertex)
        {
            AddPentagon(_vertices.Count, _rowCacheMin[i + 1], _rowCacheMin[i], _rowCacheMax[i + 2], _edgeCacheMax);
            _vertices.Add(extraVertex);
        }

        private void AddQuadADToC(int i)
        {
            AddQuad(_rowCacheMax[i + 1], _rowCacheMax[i + 2], _rowCacheMin[i], _edgeCacheMin);
        }

        private void AddPentagonADToC(int i, Vector2 extraVertex)
        {
            AddPentagon(_vertices.Count, _rowCacheMax[i + 1], _rowCacheMax[i + 2], _rowCacheMin[i], _edgeCacheMin);
            _vertices.Add(extraVertex);
        }


        private void AddTriangleA(int i)
        {
            AddTriangle(_rowCacheMin[i], _edgeCacheMin, _rowCacheMin[i + 1]);
        }

        private void AddTriangleB(int i)
        {
            AddTriangle(_rowCacheMin[i + 2], _rowCacheMin[i + 1], _edgeCacheMax);
        }

        private void AddTriangleC(int i)
        {
            AddTriangle(_rowCacheMax[i], _rowCacheMax[i + 1], _edgeCacheMin);
        }

        private void AddTriangleD(int i)
        {
            AddTriangle(_rowCacheMax[i + 2], _edgeCacheMax, _rowCacheMax[i + 1]);
        }

        private void AddPentagonABC(int i)
        {
            AddPentagon(_rowCacheMin[i], _rowCacheMax[i], _rowCacheMax[i + 1], _edgeCacheMax, _rowCacheMin[i + 2]);
        }

        private void AddPentagonABD(int i)
        {
            AddPentagon(_rowCacheMin[i + 2], _rowCacheMin[i], _edgeCacheMin, _rowCacheMax[i + 1], _rowCacheMax[i + 2]);
        }

        private void AddPentagonACD(int i)
        {
            AddPentagon(_rowCacheMax[i], _rowCacheMax[i + 2], _edgeCacheMax, _rowCacheMin[i + 1], _rowCacheMin[i]);
        }

        private void AddPentagonBCD(int i)
        {
            AddPentagon(_rowCacheMax[i + 2], _rowCacheMin[i + 2], _rowCacheMin[i + 1], _edgeCacheMin, _rowCacheMax[i]);
        }

        private void AddPentagonAB(int i, Vector2 extraVertex)
        {
            AddPentagon(_vertices.Count, _edgeCacheMax, _rowCacheMin[i + 2], _rowCacheMin[i], _edgeCacheMin);
            _vertices.Add(extraVertex);
        }

        private void AddPentagonAC(int i, Vector2 extraVertex)
        {
            AddPentagon(_vertices.Count, _rowCacheMin[i + 1], _rowCacheMin[i], _rowCacheMax[i], _rowCacheMax[i + 1]);
            _vertices.Add(extraVertex);
        }

        private void AddPentagonBD(int i, Vector2 extraVertex)
        {
            AddPentagon(
                _vertices.Count, _rowCacheMax[i + 1], _rowCacheMax[i + 2], _rowCacheMin[i + 2], _rowCacheMin[i + 1]);
            _vertices.Add(extraVertex);
        }

        private void AddPentagonCD(int i, Vector2 extraVertex)
        {
            AddPentagon(_vertices.Count, _edgeCacheMin, _rowCacheMax[i], _rowCacheMax[i + 2], _edgeCacheMax);
            _vertices.Add(extraVertex);
        }


        private void AddQuadA(int i, Vector2 extraVertex)
        {
            AddQuad(_vertices.Count, _rowCacheMin[i + 1], _rowCacheMin[i], _edgeCacheMin);
            _vertices.Add(extraVertex);
        }

        private void AddQuadB(int i, Vector2 extraVertex)
        {
            AddQuad(_vertices.Count, _edgeCacheMax, _rowCacheMin[i + 2], _rowCacheMin[i + 1]);
            _vertices.Add(extraVertex);
        }

        private void AddQuadC(int i, Vector2 extraVertex)
        {
            AddQuad(_vertices.Count, _edgeCacheMin, _rowCacheMax[i], _rowCacheMax[i + 1]);
            _vertices.Add(extraVertex);
        }

        private void AddQuadD(int i, Vector2 extraVertex)
        {
            AddQuad(_vertices.Count, _rowCacheMax[i + 1], _rowCacheMax[i + 2], _edgeCacheMax);
            _vertices.Add(extraVertex);
        }

        private void AddQuadAB(int i)
        {
            AddQuad(_rowCacheMin[i], _edgeCacheMin, _edgeCacheMax, _rowCacheMin[i + 2]);
        }

        private void AddQuadAC(int i)
        {
            AddQuad(_rowCacheMin[i], _rowCacheMax[i], _rowCacheMax[i + 1], _rowCacheMin[i + 1]);
        }

        private void AddQuadBD(int i)
        {
            AddQuad(_rowCacheMin[i + 1], _rowCacheMax[i + 1], _rowCacheMax[i + 2], _rowCacheMin[i + 2]);
        }

        private void AddQuadCD(int i)
        {
            AddQuad(_edgeCacheMin, _rowCacheMax[i], _rowCacheMax[i + 2], _edgeCacheMax);
        }


        private void AddTriangle(int a, int b, int c)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
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

        private void AddHexagon(int a, int b, int c, int d, int e, int f)
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
            _triangles.Add(a);
            _triangles.Add(e);
            _triangles.Add(f);
        }

        private void AddHexagonABC(int i, Vector2 extraVertex)
        {
            AddHexagon(_vertices.Count, _edgeCacheMax, _rowCacheMin[i + 2], _rowCacheMin[i], _rowCacheMax[i],
                _rowCacheMax[i + 1]);
            _vertices.Add(extraVertex);
        }

        private void AddHexagonABD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                _vertices.Count, _rowCacheMax[i + 1], _rowCacheMax[i + 2],
                _rowCacheMin[i + 2], _rowCacheMin[i], _edgeCacheMin);
            _vertices.Add(extraVertex);
        }

        private void AddHexagonACD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                _vertices.Count, _rowCacheMin[i + 1], _rowCacheMin[i],
                _rowCacheMax[i], _rowCacheMax[i + 2], _edgeCacheMax);
            _vertices.Add(extraVertex);
        }

        private void AddHexagonBCD(int i, Vector2 extraVertex)
        {
            AddHexagon(
                _vertices.Count, _edgeCacheMin, _rowCacheMax[i],
                _rowCacheMax[i + 2], _rowCacheMin[i + 2], _rowCacheMin[i + 1]);
            _vertices.Add(extraVertex);
        }

        private static Vector2 ComputeIntersection(Vector2 p1, Vector2 n1, Vector2 p2, Vector2 n2)
        {
            var d2 = new Vector2(n2.y, -n2.x);
            var u2 = -Vector2.Dot(n1, p2 - p1) / Vector2.Dot(n1, d2);
            return p2 + d2 * u2;
        }

        private static bool IsInsideCell(Vector2 point, Voxel min, Voxel max)
        {
            return point.x > min.position.x
                && point.y > min.position.y
                && point.x < max.position.x
                && point.y < max.position.y;
        }

        private bool IsSharpFeature(Vector2 n1, Vector2 n2)
        {
            var dot = Vector2.Dot(n1, -n2);
            return dot >= _sharpFeatureLimit && dot < 0.9999f;
        }

        private static bool IsBelowLine(Vector2 p, Vector2 start, Vector2 end)
        {
            var determinant = (end.x - start.x) * (p.y - start.y) - (end.y - start.y) * (p.x - start.x);
            return determinant < 0f;
        }

        private static bool ClampToCellMaxMax(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x < min.position.x || point.y < min.position.y)
            {
                return false;
            }

            if (point.x > max.position.x)
            {
                point.x = max.position.x;
            }

            if (point.y > max.position.y)
            {
                point.y = max.position.y;
            }

            return true;
        }

        private static bool ClampToCellMinMin(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x > max.position.x || point.y > max.position.y)
            {
                return false;
            }

            if (point.x < min.position.x)
            {
                point.x = min.position.x;
            }

            if (point.y < min.position.y)
            {
                point.y = min.position.y;
            }

            return true;
        }

        private static bool ClampToCellMinMax(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x > max.position.x || point.y < min.position.y)
            {
                return false;
            }

            if (point.x < min.position.x)
            {
                point.x = min.position.x;
            }

            if (point.y > max.position.y)
            {
                point.y = max.position.y;
            }

            return true;
        }

        private static bool ClampToCellMaxMin(ref Vector2 point, Voxel min, Voxel max)
        {
            if (point.x > max.position.x || point.y > max.position.y)
            {
                return false;
            }

            if (point.x > max.position.x)
            {
                point.x = max.position.x;
            }

            if (point.y < min.position.y)
            {
                point.y = min.position.y;
            }

            return true;
        }
    }
}