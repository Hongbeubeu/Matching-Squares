using UnityEngine;

namespace MatchingSquare
{
    [SelectionBase]
    public class VoxelGrid : MonoBehaviour
    {
        public GameObject voxelPrefab;
        public VoxelGridSurface surfacePrefab;
        public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;

        private int resolution;
        private float voxelSize, gridSize;
        private Voxel[] voxels;
        private Material[] _voxelMaterials;
        private Voxel _dummyX, _dummyY, _dummyT;
        private float _sharpFeatureLimit;
        private VoxelGridSurface surface;

        public void Initialize(int initResolution, float initSize, float maxFeatureAngle)
        {
            resolution = initResolution;
            gridSize = initSize;
            _sharpFeatureLimit = Mathf.Cos(maxFeatureAngle * Mathf.Deg2Rad);

            voxelSize = gridSize / resolution;
            var length = resolution * resolution;
            voxels = new Voxel[length];
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

            surface = Instantiate(surfacePrefab, transform, true);
            surface.transform.localPosition = Vector3.zero;
            surface.Initialize(resolution);
            Refresh();
        }

        private void CreateVoxel(int i, int x, int y)
        {
            var o = Instantiate(voxelPrefab, transform, true);
            o.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, -0.01f);
            var voxelScale = Vector3.one * voxelSize * 0.1f;
            o.transform.localScale = voxelScale;
            _voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
            voxels[i] = new Voxel(x, y, voxelSize);
        }

        public void Apply(VoxelStencil stencil)
        {
            var xStart = (int)(stencil.XStart / voxelSize);
            if (xStart < 0) xStart = 0;

            var xEnd = (int)(stencil.XEnd / voxelSize);
            if (xEnd >= resolution) xEnd = resolution - 1;

            var yStart = (int)(stencil.YStart / voxelSize);
            if (yStart < 0) yStart = 0;

            var yEnd = (int)(stencil.YEnd / voxelSize);
            if (yEnd >= resolution) yEnd = resolution - 1;

            for (var y = yStart; y <= yEnd; y++)
            {
                var i = y * resolution + xStart;
                for (var x = xStart; x <= xEnd; x++, i++)
                {
                    stencil.Apply(voxels[i]);
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
            if (xEnd == resolution - 1)
            {
                xEnd -= 1;
                crossHorizontalGap = xNeighbor != null;
            }

            if (yStart > 0) yStart -= 1;
            if (yEnd == resolution - 1)
            {
                yEnd -= 1;
                lastVerticalRow = true;
                crossVerticalGap = yNeighbor != null;
            }

            Voxel a, b;
            for (var y = yStart; y <= yEnd; y++)
            {
                var i = y * resolution + xStart;
                b = voxels[i];
                for (var x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    stencil.SetVerticalCrossing(a, voxels[i + resolution]);
                }

                stencil.SetVerticalCrossing(b, voxels[i + resolution]);
                if (crossHorizontalGap)
                {
                    _dummyX.BecomeXDummyOf(xNeighbor.voxels[y * resolution], gridSize);
                    stencil.SetHorizontalCrossing(b, _dummyX);
                }
            }

            if (lastVerticalRow)
            {
                var i = voxels.Length - resolution + xStart;
                b = voxels[i];
                for (var x = xStart; x <= xEnd; x++, i++)
                {
                    a = b;
                    b = voxels[i + 1];
                    stencil.SetHorizontalCrossing(a, b);
                    if (crossVerticalGap)
                    {
                        _dummyY.BecomeYDummyOf(yNeighbor.voxels[x], gridSize);
                        stencil.SetVerticalCrossing(a, _dummyY);
                    }
                }

                if (crossVerticalGap)
                {
                    _dummyY.BecomeYDummyOf(yNeighbor.voxels[xEnd + 1], gridSize);
                    stencil.SetVerticalCrossing(b, _dummyY);
                }

                if (crossHorizontalGap)
                {
                    _dummyX.BecomeXDummyOf(xNeighbor.voxels[voxels.Length - resolution], gridSize);
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
            for (var i = 0; i < voxels.Length; i++)
            {
                _voxelMaterials[i].color = voxels[i].state ? Color.black : Color.white;
            }
        }

        private void Triangulate()
        {
            surface.Clear();
            FillFirstRowCache();
            TriangulateCellRows();
            if (yNeighbor != null)
            {
                TriangulateGapRow();
            }

            surface.Apply();
        }

        private void FillFirstRowCache()
        {
            CacheFirstCorner(voxels[0]);
            int i;
            for (i = 0; i < resolution - 1; i++)
            {
                CacheNextEdgeAndCorner(i, voxels[i], voxels[i + 1]);
            }

            if (xNeighbor != null)
            {
                _dummyX.BecomeXDummyOf(xNeighbor.voxels[0], gridSize);
                CacheNextEdgeAndCorner(i, voxels[i], _dummyX);
            }
        }

        private void CacheNextEdgeAndCorner(int i, Voxel xMin, Voxel xMax)
        {
            if (xMin.state != xMax.state)
            {
                surface.CacheXEdge(i, xMin);
            }

            if (xMax.state)
            {
                surface.CacheNextCorner(i, xMax);
            }
        }

        private void CacheFirstCorner(Voxel voxel)
        {
            if (voxel.state)
            {
                surface.CacheFirstCorner(voxel);
            }
        }

        private void TriangulateCellRows()
        {
            var cells = resolution - 1;
            for (int i = 0, y = 0; y < cells; y++, i++)
            {
                SwapRowCaches();
                CacheFirstCorner(voxels[i + resolution]);
                CacheNextMiddleEdge(voxels[i], voxels[i + resolution]);
                for (var x = 0; x < cells; x++, i++)
                {
                    Voxel a = voxels[i],
                          b = voxels[i + 1],
                          c = voxels[i + resolution],
                          d = voxels[i + resolution + 1];
                    CacheNextEdgeAndCorner(x, c, d);
                    CacheNextMiddleEdge(b, d);
                    TriangulateCell(x, a, b, c, d);
                }

                if (xNeighbor != null)
                {
                    TriangulateGapCell(i);
                }
            }
        }

        private void CacheNextMiddleEdge(Voxel yMin, Voxel yMax)
        {
            surface.PrepareCacheForNextCell();
            if (yMin.state != yMax.state)
            {
                surface.CacheYEdge(yMin);
            }
        }

        private void SwapRowCaches()
        {
            surface.PrepareCacheForNextRow();
        }

        private void TriangulateGapCell(int i)
        {
            var dummySwap = _dummyT;
            dummySwap.BecomeXDummyOf(xNeighbor.voxels[i + 1], gridSize);
            _dummyT = _dummyX;
            _dummyX = dummySwap;
            var cacheIndex = resolution - 1;
            CacheNextEdgeAndCorner(cacheIndex, voxels[i + resolution], _dummyX);
            CacheNextMiddleEdge(_dummyT, _dummyX);
            TriangulateCell(cacheIndex, voxels[i], _dummyT, voxels[i + resolution], _dummyX);
        }

        private void TriangulateGapRow()
        {
            _dummyY.BecomeYDummyOf(yNeighbor.voxels[0], gridSize);
            var cells = resolution - 1;
            var offset = cells * resolution;
            SwapRowCaches();
            CacheFirstCorner(_dummyY);
            CacheNextMiddleEdge(voxels[cells * resolution], _dummyY);

            for (var x = 0; x < cells; x++)
            {
                var dummySwap = _dummyT;
                dummySwap.BecomeYDummyOf(yNeighbor.voxels[x + 1], gridSize);
                _dummyT = _dummyY;
                _dummyY = dummySwap;
                CacheNextEdgeAndCorner(x, _dummyT, _dummyY);
                CacheNextMiddleEdge(voxels[x + offset + 1], _dummyY);
                TriangulateCell(x, voxels[x + offset], voxels[x + offset + 1], _dummyT, _dummyY);
            }

            if (xNeighbor != null)
            {
                _dummyT.BecomeXYDummyOf(xyNeighbor.voxels[0], gridSize);
                CacheNextEdgeAndCorner(cells, _dummyY, _dummyT);
                CacheNextMiddleEdge(_dummyX, _dummyT);
                TriangulateCell(cells, voxels[voxels.Length - 1], _dummyX, _dummyY, _dummyT);
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
            surface.AddQuadABCD(i);
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
                    surface.AddQuadA(i, point);
                    return;
                }
            }

            surface.AddTriangleA(i);
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
                    surface.AddQuadB(i, point);
                    return;
                }
            }

            surface.AddTriangleB(i);
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
                    surface.AddQuadC(i, point);
                    return;
                }
            }

            surface.AddTriangleC(i);
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
                    surface.AddQuadD(i, point);
                    return;
                }
            }

            surface.AddTriangleD(i);
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
                    surface.AddHexagonABC(i, point);
                    return;
                }
            }

            surface.AddPentagonABC(i);
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
                    surface.AddHexagonABD(i, point);
                    return;
                }
            }

            surface.AddPentagonABD(i);
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
                    surface.AddHexagonACD(i, point);
                    return;
                }
            }

            surface.AddPentagonACD(i);
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
                    surface.AddHexagonBCD(i, point);
                    return;
                }
            }

            surface.AddPentagonBCD(i);
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
                    surface.AddPentagonAB(i, point);
                    return;
                }
            }

            surface.AddQuadAB(i);
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
                    surface.AddPentagonAC(i, point);
                    return;
                }
            }

            surface.AddQuadAC(i);
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
                    surface.AddPentagonBD(i, point);
                    return;
                }
            }

            surface.AddQuadBD(i);
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
                    surface.AddPentagonCD(i, point);
                    return;
                }
            }

            surface.AddQuadCD(i);
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

                    surface.AddQuadB(i, point1);
                    surface.AddQuadC(i, point2);
                    return;
                }

                if (IsBelowLine(point1, c.XEdgePoint, a.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }

                surface.AddQuadB(i, point1);
                surface.AddTriangleC(i);
                return;
            }

            if (sharp2)
            {
                if (IsBelowLine(point2, a.XEdgePoint, b.YEdgePoint))
                {
                    TriangulateCase6Connected(i, a, b, c, d);
                    return;
                }

                surface.AddTriangleB(i);
                surface.AddQuadC(i, point2);
                return;
            }

            surface.AddTriangleB(i);
            surface.AddTriangleC(i);
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
                    surface.AddPentagonBCToA(i, point);
                }
                else
                {
                    surface.AddQuadBCToA(i);
                }
            }
            else
            {
                surface.AddQuadBCToA(i);
            }

            n1 = c.xNormal;
            n2 = b.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, b.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, b.position, c.position))
                {
                    surface.AddPentagonBCToD(i, point);
                    return;
                }
            }

            surface.AddQuadBCToD(i);
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

                    surface.AddQuadA(i, point1);
                    surface.AddQuadD(i, point2);
                    return;
                }

                if (IsBelowLine(point1, b.YEdgePoint, c.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }

                surface.AddQuadA(i, point1);
                surface.AddTriangleD(i);
                return;
            }

            if (sharp2)
            {
                if (IsBelowLine(point2, a.YEdgePoint, a.XEdgePoint))
                {
                    TriangulateCase9Connected(i, a, b, c, d);
                    return;
                }

                surface.AddTriangleA(i);
                surface.AddQuadD(i, point2);
                return;
            }

            surface.AddTriangleA(i);
            surface.AddTriangleD(i);
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
                    surface.AddPentagonADToB(i, point);
                }
                else
                {
                    surface.AddQuadADToB(i);
                }
            }
            else
            {
                surface.AddQuadADToB(i);
            }

            n1 = c.xNormal;
            n2 = a.yNormal;
            if (IsSharpFeature(n1, n2))
            {
                Vector2 point = ComputeIntersection(c.XEdgePoint, n1, a.YEdgePoint, n2);
                if (IsInsideCell(point, a, d) && IsBelowLine(point, d.position, a.position))
                {
                    surface.AddPentagonADToC(i, point);
                    return;
                }
            }

            surface.AddQuadADToC(i);
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