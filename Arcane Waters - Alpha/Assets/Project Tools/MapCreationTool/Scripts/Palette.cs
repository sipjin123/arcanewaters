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

      private void Awake () {
         tilemap = GetComponentInChildren<Tilemap>();
         eventCanvas = GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
         paletteCamera = GetComponentInChildren<Camera>();
         selectionMarker = GetComponentInChildren<SpriteRenderer>();

         Utilities.addPointerListener(eventCanvas.GetComponent<EventTrigger>(),
             EventTriggerType.PointerClick, pointerClick);
         Utilities.addPointerListener(eventCanvas.GetComponent<EventTrigger>(),
             EventTriggerType.PointerEnter, pointerEnter);
         Utilities.addPointerListener(eventCanvas.GetComponent<EventTrigger>(),
             EventTriggerType.PointerExit, pointerExit);
         Utilities.addPointerListener(eventCanvas.GetComponent<EventTrigger>(),
             EventTriggerType.Drag, pointerDrag);
         Utilities.addPointerListener(eventCanvas.GetComponent<EventTrigger>(),
             EventTriggerType.BeginDrag, pointerBeginDrag);
         Utilities.addPointerListener(eventCanvas.GetComponent<EventTrigger>(),
             EventTriggerType.EndDrag, pointerEndDrag);
      }

      private void OnEnable () {
         Undo.UndoPerformed += updateSelectionMarker;
         Undo.RedoPerformed += updateSelectionMarker;

         Tools.AnythingChanged += updateSelectionMarker;

         MainCamera.SizeChanged += mainCamSizeChanged;
      }

      private void OnDisable () {
         Undo.UndoPerformed -= updateSelectionMarker;
         Undo.RedoPerformed -= updateSelectionMarker;

         Tools.AnythingChanged -= updateSelectionMarker;

         MainCamera.SizeChanged -= mainCamSizeChanged;
      }

      private void Update () {
         if (pointerHovering && Input.mouseScrollDelta.y != 0)
            pointerScroll(Input.mouseScrollDelta.y);
      }

      public void populatePalette (PaletteData data) {
         paletteData = data;
         Tools.changeTileGroup(null, registerUndo: false);
         tilemap.ClearAllTiles();

         foreach (var pref in prefabs)
            if (pref != null)
               Destroy(pref);
         prefabs.Clear();


         for (int i = 0; i < data.tileGroups.GetLength(0); i++) {
            for (int j = 0; j < data.tileGroups.GetLength(1); j++) {
               PaletteTilesData.TileData tileData = data.getTile(i, j);
               if (tileData != null) {
                  if (data.tileGroups[i, j] is PrefabGroup)
                     tilemap.SetTile(new Vector3Int(i, j, 0), transparentTile);
                  else
                     tilemap.SetTile(new Vector3Int(i, j, 0), tileData.tile);
               }
            }
         }

         foreach (var pref in data.prefabGroups) {
            GameObject p = Instantiate(pref.refPref, transform);
            if (p.GetComponent<ZSnap>())
               p.GetComponent<ZSnap>().enabled = false;
            p.layer = LayerMask.NameToLayer("MapEditor_Palette");
            foreach (var child in p.GetComponentsInChildren<Transform>())
               child.gameObject.layer = LayerMask.NameToLayer("MapEditor_Palette");
            p.transform.localPosition = pref.centerPoint;

            prefabs.Add(p);
         }

         //Calculate bounds, where regular tiles are placed and can be selected by dragging
         Vector2 size = new Vector2(data.tileGroups.GetLength(0), 1000);
         for (int i = 0; i < data.tileGroups.GetLength(0); i++)
            for (int j = 0; j < data.tileGroups.GetLength(1); j++)
               if (data.tileGroups[i, j] != null && data.tileGroups[i, j].type != TileGroupType.Regular && size.y > j)
                  size.y = j;

         regularTileBounds = new Rect(Vector2.zero, size);

         recalculateCamBounds();
         clampCamPos();
      }

      private void mainCamSizeChanged (Vector2Int newSize) {
         recalculateCamBounds();
         clampCamPos();
      }

      private void recalculateCamBounds () {
         Vector2 tilemapSize = (Vector2Int) tilemap.size + new Vector2(1f, 4f);
         Vector2 tilemapOrigin = new Vector2(-0.5f, -2f);

         float aspect = (float) paletteCamera.pixelHeight / paletteCamera.pixelWidth;

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

      public void pointerScroll (float scroll) {
         paletteCamera.orthographicSize = paletteCamera.orthographicSize +
             scroll * -zoomSpeed.Evaluate(paletteCamera.orthographicSize) * Time.deltaTime;
         clampCamPos();
      }

      private bool inBounds (Rect bounds, Vector3Int position) {
         return
             position.x >= bounds.xMin &&
             position.x < bounds.xMax &&
             position.y >= bounds.yMin &&
             position.y < bounds.yMax;
      }

      private Vector3Int clampToBounds (Rect bounds, Vector3Int position) {
         return new Vector3Int(
             Mathf.Clamp(position.x, (int) bounds.min.x, (int) bounds.max.x - 1),
             Mathf.Clamp(position.y, (int) bounds.min.y, (int) bounds.max.y - 1),
             0);
      }

      private TileGroup formGroupBetweenPoints (Vector3Int p1, Vector3Int p2) {
         Vector3Int from = new Vector3Int(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), 0);
         Vector3Int to = new Vector3Int(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y), 0);

         var group = new TileGroup {
            type = TileGroupType.Regular,
            start = from,
            tiles = new PaletteTilesData.TileData[to.x - from.x + 1, to.y - from.y + 1]
         };

         for (int i = 0; i < group.tiles.GetLength(0); i++)
            for (int j = 0; j < group.tiles.GetLength(1); j++)
               group.tiles[i, j] = paletteData.getTile(from.x + i, from.y + j);

         return group;
      }

      public void pointerBeginDrag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            var pos = tilemap.WorldToCell(data.pointerPressRaycast.worldPosition);
            if (inBounds(regularTileBounds, pos)) {
               draggingFrom = pos;
               lastDragPos = pos;
            }
         }
      }

      public void pointerEndDrag (PointerEventData data) {
         if (draggingFrom == null)
            return;

         Vector3Int curPos = tilemap.WorldToCell(data.pointerCurrentRaycast.worldPosition);
         curPos = clampToBounds(regularTileBounds, curPos);

         var g = formGroupBetweenPoints(curPos, draggingFrom.Value);

         updateSelectionMarker(g);
         Tools.changeTileGroup(g);

         draggingFrom = null;
      }

      public void pointerDrag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left && draggingFrom != null) {
            lastDragPos = tilemap.WorldToCell(data.pointerCurrentRaycast.worldPosition);
            lastDragPos = clampToBounds(regularTileBounds, lastDragPos);

            updateSelectionMarker(formGroupBetweenPoints(lastDragPos, draggingFrom.Value));
         } else if (data.button != PointerEventData.InputButton.Left) {
            Vector3 curPos = paletteCamera.ScreenToWorldPoint(data.position);
            Vector3 prevPos = paletteCamera.ScreenToWorldPoint(data.position + data.delta);
            paletteCamera.transform.localPosition += curPos - prevPos;

            clampCamPos();
         }
      }

      public void pointerClick (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left && draggingFrom == null) {
            Vector3Int cellCoors = tilemap.WorldToCell(data.pointerPressRaycast.worldPosition);
            var group = paletteData.getGroup(cellCoors.x, cellCoors.y);

            if (group != Tools.tileGroup)
               Tools.changeTileGroup(group);
         }
      }

      private void updateSelectionMarker () {
         updateSelectionMarker(Tools.tileGroup);
      }

      private void updateSelectionMarker (TileGroup selectedGroup) {
         selectionMarker.enabled = selectedGroup != null;
         if (selectionMarker.enabled) {
            selectionMarker.size = selectedGroup.size;
            selectionMarker.transform.localPosition = selectedGroup.centerPoint;
         }
      }

      public void pointerEnter (PointerEventData data) {
         pointerHovering = true;
      }

      public void pointerExit (PointerEventData data) {
         pointerHovering = false;

         if (draggingFrom != null) {
            updateSelectionMarker();
            draggingFrom = null;
         }
      }

      private void clampCamPos () {
         var cam = paletteCamera;
         Vector2 camHalfSize = new Vector2(
             cam.orthographicSize * cam.pixelWidth / cam.pixelHeight,
             cam.orthographicSize);

         if (camHalfSize.x * 2 > camBounds.width || camHalfSize.y * 2 > camBounds.height) {
            cam.orthographicSize = Mathf.Min(
                camBounds.width * 0.5f * cam.pixelHeight / cam.pixelWidth,
                camBounds.height * 0.5f);

            camHalfSize = new Vector2(
                cam.orthographicSize * cam.pixelWidth / cam.pixelHeight,
                cam.orthographicSize);
         } else if (cam.orthographicSize < 1) {
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