using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using MapCreationTool.PaletteTilesData;
using MapCreationTool.UndoSystem;

namespace MapCreationTool 
{
    public class Palette : MonoBehaviour
    {
        [SerializeField]
        private float minZoom;
        [SerializeField]
        private float maxZoom;
        [SerializeField]
        private float zoomSpeed;
        [SerializeField]
        private UI ui;


        private RectTransform eventCanvas;
        private Camera paletteCamera;
        private Tilemap tilemap;
        private SpriteRenderer selectionMarker;

        private PaletteData paletteData;

        private bool pointerHovering = false;
        private Rect camBounds = Rect.zero;

        private void Awake()
        {
            tilemap = GetComponentInChildren<Tilemap>();
            eventCanvas = GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
            paletteCamera = GetComponentInChildren<Camera>();
            selectionMarker = GetComponentInChildren<SpriteRenderer>();

            Utilities.AddPointerListener(eventCanvas.GetComponent<EventTrigger>(),
                EventTriggerType.PointerClick, PointerClick);
            Utilities.AddPointerListener(eventCanvas.GetComponent<EventTrigger>(),
                EventTriggerType.PointerEnter, PointerEnter);
            Utilities.AddPointerListener(eventCanvas.GetComponent<EventTrigger>(),
                EventTriggerType.PointerExit, PointerExit);
            Utilities.AddPointerListener(eventCanvas.GetComponent<EventTrigger>(),
                EventTriggerType.Drag, PointerDrag);
        }

        private void OnEnable()
        {
            Undo.UndoPerformed += UpdateSelectionMarker;
            Undo.RedoPerformed += UpdateSelectionMarker;

            Tools.AnythingChanged += UpdateSelectionMarker;

            MainCamera.SizeChanged += MainCamSizeChanged;
        }

        private void OnDisable()
        {
            Undo.UndoPerformed -= UpdateSelectionMarker;
            Undo.RedoPerformed -= UpdateSelectionMarker;

            Tools.AnythingChanged -= UpdateSelectionMarker;

            MainCamera.SizeChanged -= MainCamSizeChanged;
        }

        private void Update()
        {
            if (pointerHovering && Input.mouseScrollDelta.y != 0)
                PointerScroll(Input.mouseScrollDelta.y);
        }

        public void PopulatePalette(PaletteData data)
        {
            paletteData = data;
            Tools.ChangeTileGroup(null, registerUndo: false);
            tilemap.ClearAllTiles();

            for (int i = 0; i < data.TileGroups.GetLength(0); i++)
            {
                for (int j = 0; j < data.TileGroups.GetLength(1); j++)
                {
                    PaletteTilesData.TileData tileData = data.GetTile(i, j);
                    if (tileData != null)
                        tilemap.SetTile(new Vector3Int(i, j, 0), tileData.Tile);
                }
            }

            RecalculateCamBounds();
            ClampCamPos();
        }

        private void MainCamSizeChanged(Vector2Int newSize)
        {
            RecalculateCamBounds();
            ClampCamPos();
        }

        private void RecalculateCamBounds()
        {
            Vector2 tilemapSize = (Vector2Int)tilemap.size + new Vector2(1f, 4f);
            Vector2 tilemapOrigin = new Vector2(-0.5f, -2f);

            float aspect = (float)paletteCamera.pixelHeight / paletteCamera.pixelWidth;

            float size = Mathf.Max(
                    tilemapSize.x * 0.5f * aspect,
                    tilemapSize.y * 0.5f);

            paletteCamera.orthographicSize = size;

            camBounds = new Rect(
                tilemapOrigin + new Vector2(0, tilemapSize.y - size * 2f),
                new Vector2(
                    size * 2f / aspect,
                    size * 2f));
        }

        public void PointerScroll(float scroll)
        {
            paletteCamera.orthographicSize = Mathf.Clamp(
                paletteCamera.orthographicSize + scroll * -zoomSpeed * Time.deltaTime,
                minZoom,
                maxZoom);

            ClampCamPos();
        }

        public void PointerDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left)
            {
                Vector3 curPos = paletteCamera.ScreenToWorldPoint(data.position);
                Vector3 prevPos = paletteCamera.ScreenToWorldPoint(data.position + data.delta);
                paletteCamera.transform.localPosition += curPos - prevPos;

                ClampCamPos();
            }
        }

        public void PointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                Vector3Int cellCoors = tilemap.WorldToCell(data.pointerPressRaycast.worldPosition);
                var group = paletteData.GetGroup(cellCoors.x, cellCoors.y);

                Vector2Int? tile =
                    group != null && Tools.IndividualTiles && group.Type != TileGroupType.Prefab && group.Type != TileGroupType.TreePrefab
                        ? (Vector2Int?)(cellCoors - group.Start)
                        : null;

                if (group != Tools.TileGroup || Tools.TileIndex != tile)
                {
                    Tools.ChangeTileGroup(group, tile);
                    UpdateSelectionMarker();
                }
            }
        }

        private void UpdateSelectionMarker()
        {
            selectionMarker.enabled = Tools.TileGroup != null;
            if (selectionMarker.enabled)
            {
                if (Tools.TileIndex == null)
                {
                    selectionMarker.size = Tools.TileGroup.Size;
                    selectionMarker.transform.localPosition = Tools.TileGroup.CenterPoint;
                }
                else
                {
                    selectionMarker.size = Vector2.one;
                    selectionMarker.transform.localPosition = Tools.TileGroup.Start + (Vector3Int)Tools.TileIndex.Value + new Vector3(0.5f, 0.5f, 0);
                }
            }
        }

        public void PointerEnter(PointerEventData data)
        {
            pointerHovering = true;
        }

        public void PointerExit(PointerEventData data)
        {
            pointerHovering = false;
        }

        private void ClampCamPos()
        {
            var cam = paletteCamera;
            Vector2 camHalfSize = new Vector2(
                cam.orthographicSize * cam.pixelWidth / cam.pixelHeight,
                cam.orthographicSize);

            if (camHalfSize.x * 2 > camBounds.width || camHalfSize.y * 2 > camBounds.height)
            {
                cam.orthographicSize = Mathf.Min(
                    camBounds.width * 0.5f * cam.pixelHeight / cam.pixelWidth,
                    camBounds.height * 0.5f);

                camHalfSize = new Vector2(
                cam.orthographicSize * cam.pixelWidth / cam.pixelHeight,
                cam.orthographicSize);
            }

            cam.transform.localPosition = new Vector3(
                    Mathf.Clamp(
                        cam.transform.localPosition.x,
                        camBounds.x + camHalfSize.x,
                        camBounds.x + camBounds.width - camHalfSize.x),
                    Mathf.Clamp(
                        cam.transform.localPosition.y,
                        camBounds.y + camHalfSize.y,
                        camBounds.y + camBounds.height - camHalfSize.y),
                    cam.transform.localPosition.z);
        }
    }
}