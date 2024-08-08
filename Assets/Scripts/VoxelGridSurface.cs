using UnityEngine;
using System.Collections.Generic;

namespace MatchingSquare
{
	public class VoxelGridSurface : MonoBehaviour
	{

		private Mesh mesh;

		private List<Vector3> vertices;
		private List<int> triangles;

		private Voxel dummyX, dummyY, dummyT;

		private int[] cornersMin, cornersMax;
		private int[] xEdgesMin, xEdgesMax;
		private int yEdgeMin, yEdgeMax;

		public void Initialize(int resolution)
		{
			GetComponent<MeshFilter>().mesh = mesh = new Mesh();
			mesh.name = "VoxelGridSurface Mesh";
			vertices = new List<Vector3>();
			triangles = new List<int>();
			cornersMax = new int[resolution + 1];
			cornersMin = new int[resolution + 1];
			xEdgesMin = new int[resolution];
			xEdgesMax = new int[resolution];
		}

		public void Clear()
		{
			vertices.Clear();
			triangles.Clear();
			mesh.Clear();
		}

		public void Apply()
		{
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
		}

		public void CacheFirstCorner(Voxel voxel)
		{
			cornersMax[0] = vertices.Count;
			vertices.Add(voxel.position);
		}

		public void CacheNextCorner(int i, Voxel voxel)
		{
			cornersMax[i + 1] = vertices.Count;
			vertices.Add(voxel.position);
		}

		public void CacheXEdge(int i, Voxel voxel)
		{
			xEdgesMax[i] = vertices.Count;
			vertices.Add(voxel.XEdgePoint);
		}

		public void CacheYEdge(Voxel voxel)
		{
			yEdgeMax = vertices.Count;
			vertices.Add(voxel.YEdgePoint);
		}

		public void PrepareCacheForNextCell()
		{
			yEdgeMin = yEdgeMax;
		}

		public void PrepareCacheForNextRow()
		{
			int[] rowSwap = cornersMin;
			cornersMin = cornersMax;
			cornersMax = rowSwap;
			rowSwap = xEdgesMin;
			xEdgesMin = xEdgesMax;
			xEdgesMax = rowSwap;
		}

		public void AddQuadABCD(int i)
		{
			AddQuad(
				cornersMin[i], cornersMax[i], cornersMax[i + 1], cornersMin[i + 1]);
		}

		public void AddTriangleA(int i)
		{
			AddTriangle(cornersMin[i], yEdgeMin, xEdgesMin[i]);
		}

		public void AddQuadA(int i, Vector2 extraVertex)
		{
			AddQuad(vertices.Count, xEdgesMin[i], cornersMin[i], yEdgeMin);
			vertices.Add(extraVertex);
		}

		public void AddTriangleB(int i)
		{
			AddTriangle(cornersMin[i + 1], xEdgesMin[i], yEdgeMax);
		}

		public void AddQuadB(int i, Vector2 extraVertex)
		{
			AddQuad(vertices.Count, yEdgeMax, cornersMin[i + 1], xEdgesMin[i]);
			vertices.Add(extraVertex);
		}

		public void AddTriangleC(int i)
		{
			AddTriangle(cornersMax[i], xEdgesMax[i], yEdgeMin);
		}

		public void AddQuadC(int i, Vector2 extraVertex)
		{
			AddQuad(vertices.Count, yEdgeMin, cornersMax[i], xEdgesMax[i]);
			vertices.Add(extraVertex);
		}

		public void AddTriangleD(int i)
		{
			AddTriangle(cornersMax[i + 1], yEdgeMax, xEdgesMax[i]);
		}

		public void AddQuadD(int i, Vector2 extraVertex)
		{
			AddQuad(vertices.Count, xEdgesMax[i], cornersMax[i + 1], yEdgeMax);
			vertices.Add(extraVertex);
		}

		public void AddPentagonABC(int i)
		{
			AddPentagon(
				cornersMin[i], cornersMax[i], xEdgesMax[i],
				yEdgeMax, cornersMin[i + 1]);
		}

		public void AddHexagonABC(int i, Vector2 extraVertex)
		{
			AddHexagon(
				vertices.Count, yEdgeMax, cornersMin[i + 1],
				cornersMin[i], cornersMax[i], xEdgesMax[i]);
			vertices.Add(extraVertex);
		}

		public void AddPentagonABD(int i)
		{
			AddPentagon(
				cornersMin[i + 1], cornersMin[i], yEdgeMin,
				xEdgesMax[i], cornersMax[i + 1]);
		}

		public void AddHexagonABD(int i, Vector2 extraVertex)
		{
			AddHexagon(
				vertices.Count, xEdgesMax[i], cornersMax[i + 1],
				cornersMin[i + 1], cornersMin[i], yEdgeMin);
			vertices.Add(extraVertex);
		}

		public void AddPentagonACD(int i)
		{
			AddPentagon(
				cornersMax[i], cornersMax[i + 1], yEdgeMax,
				xEdgesMin[i], cornersMin[i]);
		}

		public void AddHexagonACD(int i, Vector2 extraVertex)
		{
			AddHexagon(
				vertices.Count, xEdgesMin[i], cornersMin[i],
				cornersMax[i], cornersMax[i + 1], yEdgeMax);
			vertices.Add(extraVertex);
		}

		public void AddPentagonBCD(int i)
		{
			AddPentagon(
				cornersMax[i + 1], cornersMin[i + 1], xEdgesMin[i],
				yEdgeMin, cornersMax[i]);
		}

		public void AddHexagonBCD(int i, Vector2 extraVertex)
		{
			AddHexagon(
				vertices.Count, yEdgeMin, cornersMax[i],
				cornersMax[i + 1], cornersMin[i + 1], xEdgesMin[i]);
			vertices.Add(extraVertex);
		}

		public void AddQuadAB(int i)
		{
			AddQuad(cornersMin[i], yEdgeMin, yEdgeMax, cornersMin[i + 1]);
		}

		public void AddPentagonAB(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, yEdgeMax, cornersMin[i + 1],
				cornersMin[i], yEdgeMin);
			vertices.Add(extraVertex);
		}

		public void AddQuadAC(int i)
		{
			AddQuad(cornersMin[i], cornersMax[i], xEdgesMax[i], xEdgesMin[i]);
		}

		public void AddPentagonAC(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, xEdgesMin[i], cornersMin[i],
				cornersMax[i], xEdgesMax[i]);
			vertices.Add(extraVertex);
		}

		public void AddQuadBD(int i)
		{
			AddQuad(
				xEdgesMin[i], xEdgesMax[i], cornersMax[i + 1], cornersMin[i + 1]);
		}

		public void AddPentagonBD(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, xEdgesMax[i], cornersMax[i + 1],
				cornersMin[i + 1], xEdgesMin[i]);
			vertices.Add(extraVertex);
		}

		public void AddQuadCD(int i)
		{
			AddQuad(yEdgeMin, cornersMax[i], cornersMax[i + 1], yEdgeMax);
		}

		public void AddPentagonCD(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, yEdgeMin, cornersMax[i],
				cornersMax[i + 1], yEdgeMax);
			vertices.Add(extraVertex);
		}

		public void AddQuadBCToA(int i)
		{
			AddQuad(yEdgeMin, cornersMax[i], cornersMin[i + 1], xEdgesMin[i]);
		}

		public void AddPentagonBCToA(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, yEdgeMin, cornersMax[i],
				cornersMin[i + 1], xEdgesMin[i]);
			vertices.Add(extraVertex);
		}

		public void AddQuadBCToD(int i)
		{
			AddQuad(yEdgeMax, cornersMin[i + 1], cornersMax[i], xEdgesMax[i]);
		}

		public void AddPentagonBCToD(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, yEdgeMax, cornersMin[i + 1],
				cornersMax[i], xEdgesMax[i]);
			vertices.Add(extraVertex);
		}

		public void AddQuadADToB(int i)
		{
			AddQuad(xEdgesMin[i], cornersMin[i], cornersMax[i + 1], yEdgeMax);
		}

		public void AddPentagonADToB(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, xEdgesMin[i], cornersMin[i],
				cornersMax[i + 1], yEdgeMax);
			vertices.Add(extraVertex);
		}

		public void AddQuadADToC(int i)
		{
			AddQuad(xEdgesMax[i], cornersMax[i + 1], cornersMin[i], yEdgeMin);
		}

		public void AddPentagonADToC(int i, Vector2 extraVertex)
		{
			AddPentagon(
				vertices.Count, xEdgesMax[i], cornersMax[i + 1],
				cornersMin[i], yEdgeMin);
			vertices.Add(extraVertex);
		}

		private void AddTriangle(int a, int b, int c)
		{
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
		}

		private void AddQuad(int a, int b, int c, int d)
		{
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
			triangles.Add(a);
			triangles.Add(c);
			triangles.Add(d);
		}

		private void AddPentagon(int a, int b, int c, int d, int e)
		{
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
			triangles.Add(a);
			triangles.Add(c);
			triangles.Add(d);
			triangles.Add(a);
			triangles.Add(d);
			triangles.Add(e);
		}

		private void AddHexagon(int a, int b, int c, int d, int e, int f)
		{
			triangles.Add(a);
			triangles.Add(b);
			triangles.Add(c);
			triangles.Add(a);
			triangles.Add(c);
			triangles.Add(d);
			triangles.Add(a);
			triangles.Add(d);
			triangles.Add(e);
			triangles.Add(a);
			triangles.Add(e);
			triangles.Add(f);
		}
	}
}