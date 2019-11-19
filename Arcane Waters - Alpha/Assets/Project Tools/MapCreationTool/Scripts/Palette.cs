using MapCreationTool.PaletteTilesData;
using MapCreationTool.UndoSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    public class Palette : MonoBehaviour
    {
        [SerializeField]
        private AnimationCurve zoomSpeed = null;

        [SerializeField]
        private TileBase transparentTile = null;

        private RectTransform eventCanvas = null;
        private Camera paletteCamera = null;
        private Tilemap tilemap = null;
        private SpriteRenderer selectionMarker = null;
        private List<GameObject> prefabs = new List<GameObject>();

        private PaletteData paletteData;

        private bool pointerHovering = false;
        private Rect camBounds = Rect.zero;
        private Rect regularTileBounds = Rect.zero;
        private Vector3Int? draggingFrom = null;
        private Vector3Int lastDragPos = Vector3Int.zero;

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
            Utilities.AddPointerListener(eventCanvas.GetComponent<EventTrigger>(),
                EventTriggerType.BeginDrag, PointerBeginDrag);
            Utilities.AddPointerListener(eventCanvas.GetComponent<EventTrigger>(),
                EventTriggerType.EndDrag, PointerEndDrag);
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

            foreach (var pref in prefabs)
                if (pref != null)
                    Destroy(pref);
            prefabs.Clear();


            for (int i = 0; i < data.TileGroups.GetLength(0); i++)
            {
                for (int j = 0; j < data.TileGroups.GetLength(1); j++)
                {
                    PaletteTilesData.TileData tileData = data.GetTile(i, j);
                    if (tileData != null)
                    {
                        if (data.TileGroups[i, j] is PrefabGroup)
                            tilemap.SetTile(new Vector3Int(i, j, 0), transparentTile);
                        else
                            tilemap.SetTile(new Vector3Int(i, j, 0), tileData.Tile);
                    }
                }
            }

            foreach (var pref in data.PrefabGroups)
            {
                GameObject p = Instantiate(pref.RefPref, transform);
                if (p.GetComponent<ZSnap>())
                    p.GetComponent<ZSnap>().enabled = false;
                p.layer = LayerMask.NameToLayer("MapEditor_Palette");
                foreach (var child in p.GetComponentsInChildren<Transform>())
                    child.gameObject.layer = LayerMask.NameToLayer("MapEditor_Palette");
                p.transform.localPosition = pref.CenterPoint;

                prefabs.Add(p);
            }

            //Calculate bounds, where regular tiles are placed and can be selected by dragging
            Vector2 size = new Vector2(data.TileGroups.GetLength(0), 1000);
            for(int i = 0; i < data.TileGroups.GetLength(0); i++)
                for (int j = 0; j < data.TileGroups.GetLength(1); j++)
                    if (data.TileGroups[i, j] != null && data.TileGroups[i, j].Type != TileGroupType.Regular && size.y > j)
                        size.y = j;

            regularTileBounds = new Rect(Vector2.zero, size);

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
            paletteCamera.orthographicSize = paletteCamera.orthographicSize + 
                scroll * -zoomSpeed.Evaluate(paletteCamera.orthographicSize) * Time.deltaTime;
            ClampCamPos();
        }

        private bool InBounds(Rect bounds, Vector3Int position)
        {
            return 
                position.x >= bounds.xMin && 
                position.x < bounds.xMax && 
                position.y >= bounds.yMin && 
                position.y < bounds.yMax;
        }

        private Vector3Int ClampToBounds(Rect bounds, Vector3Int position)
        {
            return new Vector3Int(
                Mathf.Clamp(position.x, (int)bounds.min.x, (int)bounds.max.x - 1),
                Mathf.Clamp(position.y, (int)bounds.min.y, (int)bounds.max.y - 1),
                0);
        }

        private TileGroup FormGroupBetweenPoints(Vector3Int p1, Vector3Int p2)
        {
            Vector3Int from = new Vector3Int(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), 0);
            Vector3Int to = new Vector3Int(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y), 0);

            var group = new TileGroup
            {
                Type = TileGroupType.Regular,
                Start = from,
                Tiles = new PaletteTilesData.TileData[to.x - from.x + 1, to.y - from.y + 1]
            };

            for (int i = 0; i < group.Tiles.GetLength(0); i++)
                for (int j = 0; j < group.Tiles.GetLength(1); j++)
                    group.Tiles[i, j] = paletteData.GetTile(from.x + i, from.y + j);

            return group;
        }

        public void PointerBeginDrag(PointerEventData data)
        {
            if(data.button == PointerEventData.InputButton.Left)
            {
                var pos = tilemap.WorldToCell(data.pointerPressRaycast.worldPosition);
                if (InBounds(regularTileBounds, pos))
                {
                    draggingFrom = pos;
                    lastDragPos = pos;
                }
            }
        }

        public void PointerEndDrag(PointerEventData data)
        {
            if (draggingFrom == null)
                return;

            Vector3Int curPos = tilemap.WorldToCell(data.pointerCurrentRaycast.worldPosition);
            curPos = ClampToBounds(regularTileBounds, curPos);

            var g = FormGroupBetweenPoints(curPos, draggingFrom.Value);

            UpdateSelectionMarker(g);
            Tools.ChangeTileGroup(g);

            draggingFrom = null;
        }

        public void PointerDrag(PointerEventData data)
        {
            if(data.button == PointerEventData.InputButton.Left && draggingFrom != null)
            {
                lastDragPos = tilemap.WorldToCell(data.pointerCurrentRaycast.worldPosition);
                lastDragPos = ClampToBounds(regularTileBounds, lastDragPos);

                UpdateSelectionMarker(FormGroupBetweenPoints(lastDragPos, draggingFrom.Value));
            }
            else if(data.button != PointerEventData.InputButton.Left)
            {
                Vector3 curPos = paletteCamera.ScreenToWorldPoint(data.position);
                Vector3 prevPos = paletteCamera.ScreenToWorldPoint(data.position + data.delta);
                paletteCamera.transform.localPosition += curPos - prevPos;

                ClampCamPos();
            }
        }

        public void PointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && draggingFrom == null)
            {
                Vector3Int cellCoors = tilemap.WorldToCell(data.pointerPressRaycast.worldPosition);
                var group = paletteData.GetGroup(cellCoors.x, cellCoors.y);
            
                if (group != Tools.TileGroup)
                    Tools.ChangeTileGroup(group);
            }
        }

        private void UpdateSelectionMarker()
        {
            UpdateSelectionMarker(Tools.TileGroup);
        }

        private void UpdateSelectionMarker(TileGroup selectedGroup)
        {
            selectionMarker.enabled = selectedGroup != null;
            if (selectionMarker.enabled)
            {
                selectionMarker.size = selectedGroup.Size;
                selectionMarker.transform.localPosition = selectedGroup.CenterPoint;
            }
        }

        public void PointerEnter(PointerEventData data)
        {
            pointerHovering = true;
        }

        public void PointerExit(PointerEventData data)
        {
            pointerHovering = false;

            if(draggingFrom != null)
            {
                UpdateSelectionMarker();
                draggingFrom = null;
            }
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

            else if(cam.orthographicSize < 1)
            {
                cam.orthographicSize = 1;

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