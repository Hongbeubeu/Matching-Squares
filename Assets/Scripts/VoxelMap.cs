using UnityEngine;

public class VoxelMap : MonoBehaviour
{
    public float size = 2f;
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelGrid voxelGridPrefab;

    private VoxelGrid[] _chunks;
    private float _chunkSize, _voxelSize, _halfSize;
    private static readonly string[] FillTypeNames = { "Filled", "Empty" };
    private static readonly string[] RadiusNames = { "0", "1", "2", "3", "4", "5" };
    private static readonly string[] StencilNames = { "Square", "Circle" };
    private int _fillTypeIndex, _radiusIndex, _stencilIndex;
    private readonly VoxelStencil[] _stencils = { new(), new VoxelStencilCircle() };
    private Camera _camera;

    #region Unity Event Functions

    private void Awake()
    {
        _camera = Camera.main;
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
        if (!Input.GetMouseButton(0)) return;
        if (!Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hitInfo)) return;
        if (hitInfo.collider.gameObject == gameObject)
        {
            EditVoxels(transform.InverseTransformPoint(hitInfo.point));
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(4f, 4f, 150f, 500f));
        GUILayout.Label("Fill Type");
        _fillTypeIndex = GUILayout.SelectionGrid(_fillTypeIndex, FillTypeNames, 2);
        GUILayout.Label("Radius");
        _radiusIndex = GUILayout.SelectionGrid(_radiusIndex, RadiusNames, 6);
        GUILayout.Label("Stencil");
        _stencilIndex = GUILayout.SelectionGrid(_stencilIndex, StencilNames, 2);
        GUILayout.EndArea();
    }

    #endregion

    private void EditVoxels(Vector3 point)
    {
        var centerX = (int)((point.x + _halfSize) / _voxelSize);
        var centerY = (int)((point.y + _halfSize) / _voxelSize);

        var xStart = (centerX - _radiusIndex - 1) / voxelResolution;
        if (xStart < 0) xStart = 0;

        var xEnd = (centerX + _radiusIndex) / voxelResolution;
        if (xEnd >= chunkResolution) xEnd = chunkResolution - 1;

        var yStart = (centerY - _radiusIndex - 1) / voxelResolution;
        if (yStart < 0) yStart = 0;

        var yEnd = (centerY + _radiusIndex) / voxelResolution;
        if (yEnd >= chunkResolution) yEnd = chunkResolution - 1;


        var activeStencil = _stencils[_stencilIndex];
        activeStencil.Initialize(_fillTypeIndex == 0, _radiusIndex);

        var voxelYOffset = yEnd * voxelResolution;
        for (var y = yEnd; y >= yStart; y--)
        {
            var i = y * chunkResolution + xEnd;
            var voxelXOffset = xEnd * voxelResolution;
            for (var x = xEnd; x >= xStart; x--, i--)
            {
                activeStencil.SetCenter(centerX - voxelXOffset, centerY - voxelYOffset);
                _chunks[i].Apply(activeStencil);
                voxelXOffset -= voxelResolution;
            }

            voxelYOffset -= voxelResolution;
        }
    }


    private void CreateChunk(int i, int x, int y)
    {
        var chunk = Instantiate(voxelGridPrefab, transform, true);
        chunk.Initialize(voxelResolution, _chunkSize);
        chunk.transform.localPosition = new Vector3(x * _chunkSize - _halfSize, y * _chunkSize - _halfSize);
        _chunks[i] = chunk;
        if (x > 0)
        {
            _chunks[i - 1].xNeighbor = _chunks[i];
        }

        if (y > 0)
        {
            _chunks[i - chunkResolution].yNeighbor = chunk;
            if (x > 0)
            {
                _chunks[i - chunkResolution - 1].xyNeighbor = chunk;
            }
        }
    }
}