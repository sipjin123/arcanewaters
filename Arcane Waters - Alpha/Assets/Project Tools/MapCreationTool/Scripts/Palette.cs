﻿using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class Palette : MonoBehaviour
   {
      public static Palette instance { get; private set; }

      [SerializeField]
      private AnimationCurve zoomSpeed = null;

      [SerializeField]
      private TileBase transparentTile = null;

      private RectTransform eventCanvas = null;
      private Camera paletteCamera = null;
      private Tilemap tilemap = null;
      private SpriteRenderer selectionMarker = null;

      // List of prefabs in the palette currently
      private List<PlacedPrefab> prefabs = new List<PlacedPrefab>();

      public PaletteData paletteData { get; private set; }

      private bool pointerHovering = false;
      private Rect camBounds = Rect.zero;
      private Rect regularTileBounds = Rect.zero;
      private Vector3Int? draggingFrom = null;
      private Vector3Int lastDragPos = Vector3Int.zero;

      // ToolTip component for the palette
      private PaletteToolTip _toolTip;

      private Vector2 paletteSize;

      private void Awake () {
         if (instance == null) {
            instance = this;
         }

         tilemap = GetComponentInChildren<Tilemap>();
         eventCanvas = GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
         paletteCamera = GetComponentInChildren<Camera>();
         selectionMarker = GetComponentInChildren<SpriteRenderer>();
         _toolTip = GetComponent<PaletteToolTip>();

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
         if (pointerHovering) {
            if (MouseUtils.mouseScrollY != 0) {
               pointerScroll(MouseUtils.mouseScrollY * .5f);
            }

            updateToolTip(MouseUtils.mousePosition);
         }
      }

      private void updateToolTip (Vector2 screenPos) {
         Vector3Int cellCoors = tilemap.WorldToCell(paletteCamera.ScreenToWorldPoint(screenPos));
         TileGroup group = paletteData.getGroup(cellCoors.x, cellCoors.y);

         if (group != Tools.tileGroup) {
            if (group is PrefabGroup) {
               _toolTip.setToolTip("Prefab: " + (group as PrefabGroup).getPrefab().name);
               return;
            } else {
               PaletteTilesData.TileData tileData = paletteData.getTile(cellCoors.x, cellCoors.y);
               if (tileData != null) {
                  string tileName = "_";
                  if (tileData.tile != null) {
                     tileName = tileData.tile.name;
                  }

                  _toolTip.setToolTip($"{ tileName }, layer: { tileData.layer }_{ tileData.subLayer }");
                  return;
               }
            }
         }

         _toolTip.hideToolTip();
      }

      public void updatePrefabData (GameObject prefab, string key, string value) {
         foreach (PlacedPrefab placed in prefabs) {
            if (placed.original == prefab) {
               IPrefabDataListener listener = placed.placedInstance.GetComponent<IPrefabDataListener>();
               listener.dataFieldChanged(new DataField { k = key, v = value });
            }
         }
      }

      public Dictionary<TileBase, TileCollisionType> formCollisionDictionary () {
         return paletteData.formCollisionDictionary();
      }

      public void populatePalette (PaletteData data, Biome.Type biome) {
         paletteData = data;
         Tools.changeTileGroup(null, registerUndo: false);
         tilemap.ClearAllTiles();

         foreach (PlacedPrefab pref in prefabs) {
            if (pref != null) {
               Destroy(pref.placedInstance);
            }
         }
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

            foreach (IBiomable biomable in p.GetComponentsInChildren<IBiomable>()) {
               biomable.setBiome(biome);
            }

            p.layer = LayerMask.NameToLayer("MapEditor_Palette");
            foreach (var child in p.GetComponentsInChildren<Transform>())
               child.gameObject.layer = LayerMask.NameToLayer("MapEditor_Palette");

            float centerOffset = p.GetComponent<PrefabCenterOffset>() ? p.GetComponent<PrefabCenterOffset>().offset : 0;
            p.transform.localPosition = pref.centerPoint - Vector2.up * centerOffset;

            p.GetComponent<MapEditorPrefab>()?.createdInPalette();

            p.GetComponent<SpriteOutline>()?.setNewColor(new Color(0, 0, 0, 0));

            prefabs.Add(new PlacedPrefab { original = pref.refPref, placedInstance = p });

            // Set the data for the prefab
            var pdd = p.GetComponent<PrefabDataDefinition>();
            IPrefabDataListener listener = p.GetComponent<IPrefabDataListener>();

            if (pdd != null && listener != null) {
               pdd.restructureCustomFields();
               foreach (var kv in pdd.dataFields)
                  listener.dataFieldChanged(new DataField { k = kv.name, v = kv.defaultValue });
               foreach (var kv in pdd.selectDataFields)
                  listener.dataFieldChanged(new DataField { k = kv.name, v = kv.options[kv.defaultOption].value });

               Dictionary<string, string> defaultData = Tools.getDefaultData(pref);
               if (defaultData != null) {
                  foreach (KeyValuePair<string, string> d in defaultData) {
                     listener.dataFieldChanged(new DataField { k = d.Key, v = d.Value });
                  }
               }
            }
         }

         //Calculate bounds, where regular tiles are placed and can be selected by dragging
         Vector2 size = new Vector2(data.tileGroups.GetLength(0), 1000);
         for (int i = 0; i < data.tileGroups.GetLength(0); i++)
            for (int j = 0; j < data.tileGroups.GetLength(1); j++)
               if (data.tileGroups[i, j] != null && data.tileGroups[i, j].type != TileGroupType.Regular && size.y > j)
                  size.y = j;

         regularTileBounds = new Rect(Vector2.zero, size);

         paletteSize = new Vector2(paletteData.tileGroups.GetLength(0), paletteData.tileGroups.GetLength(1));

         recalculateCamBounds();
         clampCamPos();
      }

      private void mainCamSizeChanged (Vector2Int newSize) {
         recalculateCamBounds();
         clampCamPos();
      }

      private void recalculateCamBounds () {
         Vector2 extendedSize = paletteSize + new Vector2(2f, 10f);
         Vector2 tilemapOrigin = new Vector2(-1f, -4f);

         float aspect = (float) paletteCamera.pixelHeight / paletteCamera.pixelWidth;

         float size = Mathf.Max(
                 extendedSize.x * 0.5f * aspect,
                 extendedSize.y * 0.5f);

         paletteCamera.orthographicSize = size;

         camBounds = new Rect(
             tilemapOrigin + new Vector2(0, extendedSize.y - size * 2f),
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

         _toolTip.hideToolTip();
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