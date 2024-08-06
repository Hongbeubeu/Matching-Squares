using UnityEngine;

public class VoxelMap : MonoBehaviour
{
    public float size = 2f;
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelGrid voxelGridPrefab;

    private VoxelGrid[] _chunks;
    private float _chunkSize, _voxelSize, _halfSize;
    private static string[] _fillTypeNames = { "Filled", "Empty" };
    private static string[] _radiusNames = { "0", "1", "2", "3", "4", "5" };
    private static string[] _stencilNames = { "Square", "Circle" };
    private int _fillTypeIndex, _radiusIndex, _stencilIndex;
    private VoxelStencil[] _stencils = { new(), new VoxelStencilCircle() };

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

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(4f, 4f, 150f, 500f));
        GUILayout.Label("Fill Type");
        _fillTypeIndex = GUILayout.SelectionGrid(_fillTypeIndex, _fillTypeNames, 2);
        GUILayout.Label("Radius");
        _radiusIndex = GUILayout.SelectionGrid(_radiusIndex, _radiusNames, 6);
        GUILayout.Label("Stencil");
        _stencilIndex = GUILayout.SelectionGrid(_stencilIndex, _stencilNames, 2);
        GUILayout.EndArea();
    }

    #endregion

    private void EditVoxels(Vector3 point)
    {
        var centerX = (int)((point.x + _halfSize) / _voxelSize);
        var centerY = (int)((point.y + _halfSize) / _voxelSize);

        var xStart = (centerX - _radiusIndex) / voxelResolution;
        if (xStart < 0) xStart = 0;

        var xEnd = (centerX + _radiusIndex) / voxelResolution;
        if (xEnd >= chunkResolution) xEnd = chunkResolution - 1;

        var yStart = (centerY - _radiusIndex) / voxelResolution;
        if (yStart < 0) yStart = 0;

        var yEnd = (centerY + _radiusIndex) / voxelResolution;
        if (yEnd >= chunkResolution) yEnd = chunkResolution - 1;


        var activeStencil = _stencils[_stencilIndex];
        activeStencil.Initialize(_fillTypeIndex == 0, _radiusIndex);

        var voxelYOffset = yStart * voxelResolution;
        for (var y = yStart; y <= yEnd; y++)
        {
            var i = y * chunkResolution + xStart;
            var voxelXOffset = xStart * voxelResolution;
            for (var x = xStart; x <= xEnd; x++, i++)
            {
                activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
                _chunks[i].Apply(activeStencil);
                voxelXOffset += voxelResolution;
            }

            voxelYOffset += voxelResolution;
        }
    }


    private void CreateChunk(int i, int x, int y)
    {
        var chunk = Instantiate(voxelGridPrefab, transform, true);
        chunk.Initialize(voxelResolution, _chunkSize);
        chunk.transform.localPosition = new Vector3(x * _chunkSize - _halfSize, y * _chunkSize - _halfSize);
        _chunks[i] = chunk;
    }
}