using System.Collections.Generic;
using UnityEngine;

namespace MatchingSquare
{
    public class VoxelGridWall : MonoBehaviour
    {
        public float bottom, top;
        
        private Mesh mesh;
	
        private List<Vector3> vertices;
        private List<int> triangles;
	
        private int[] xEdgesMin, xEdgesMax;
        private int yEdgeMin, yEdgeMax;

        public void Initialize (int resolution) {
            GetComponent<MeshFilter>().mesh = mesh = new Mesh();
            mesh.name = "VoxelGridWall Mesh";
            vertices = new List<Vector3>();
            triangles = new List<int>();
            xEdgesMin = new int[resolution];
            xEdgesMax = new int[resolution];
        }
	
        public void Clear () {
            vertices.Clear();
            triangles.Clear();
            mesh.Clear();
        }
	
        public void Apply () {
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
        }
    }
}