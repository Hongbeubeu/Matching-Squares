using UnityEngine;

public class VoxelMap : MonoBehaviour
{
    public float size = 2f;
    public int voxelResolution = 8;
    public int chunkResolution = 2;
    public VoxelGrid voxelGridPrefab;
    public Transform[] stencilVisualizations;
    public bool snapToGrid;

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
        var visualization = stencilVisualizations[_stencilIndex];
        if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hitInfo) &&
            hitInfo.collider.gameObject == gameObject)
        {
            var center = transform.InverseTransformPoint(hitInfo.point);
            center.x += _halfSize;
            center.y += _halfSize;
            if (snapToGrid)
            {
                center.x = ((int)(center.x / _voxelSize) + 0.5f) * _voxelSize;
                center.y = ((int)(center.y / _voxelSize) + 0.5f) * _voxelSize;
            }

            if (Input.GetMouseButton(0))
            {
                EditVoxels(center);
            }

            center.x -= _halfSize;
            center.y -= _halfSize;
            visualization.localPosition = center;
            visualization.localScale = Vector3.one * ((_radiusIndex + 0.5f) * _voxelSize * 2f);
            visualization.gameObject.SetActive(true);
        }
        else
        {
            visualization.gameObject.SetActive(false);
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

    private void EditVoxels(Vector2 center)
    {
        var activeStencil = _stencils[_stencilIndex];
        activeStencil.Initialize(_fillTypeIndex == 0, (_radiusIndex + 0.5f) * _voxelSize);
        activeStencil.SetCenter(center.x, center.y);

        var xStart = (int)((activeStencil.XStart - _voxelSize) / _chunkSize);
        if (xStart < 0) xStart = 0;

        var xEnd = (int)((activeStencil.XEnd + _voxelSize) / _chunkSize);
        if (xEnd >= chunkResolution) xEnd = chunkResolution - 1;

        var yStart = (int)((activeStencil.YStart - _voxelSize) / _chunkSize);
        if (yStart < 0) yStart = 0;

        var yEnd = (int)((activeStencil.YEnd + _voxelSize) / _chunkSize);
        if (yEnd >= chunkResolution) yEnd = chunkResolution - 1;


        var voxelYOffset = yEnd * voxelResolution;
        for (var y = yEnd; y >= yStart; y--)
        {
            var i = y * chunkResolution + xEnd;
            var voxelXOffset = xEnd * voxelResolution;
            for (var x = xEnd; x >= xStart; x--, i--)
            {
                activeStencil.SetCenter(center.x - x * _chunkSize, center.y - y * _chunkSize);
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