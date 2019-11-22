using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class DrawBoard : MonoBehaviour
   {
      public static event System.Action<PrefabDataDefinition, PlacedPrefab> SelectedPrefabChanged;
      public static event System.Action<PlacedPrefab> SelectedPrefabDataChanged;

      public static Vector2Int size { get; private set; }
      public static Vector2Int origin { get; private set; }

      [SerializeField]
      private Tilemap layerPref = null;
      [SerializeField]
      private Transform prefabLayer = null;
      [SerializeField]
      private LayerName[] layerDefinitions = new LayerName[0];
      [SerializeField]
      private Transform layerContainer = null;
      [SerializeField]
      private SpriteRenderer brushOutline = null;
      [SerializeField]
      private float minZoom = 0;
      [SerializeField]
      private float maxZoom = 0;
      [SerializeField]
      private AnimationCurve zoomSpeed = null;
      [SerializeField]
      private TileBase transparentTile = null;

      private EventTrigger eventCanvas;
      private Camera cam;
      private Grid grid;

      private bool pointerHovering = false;
      private Vector3 lastMouseHoverPos = Vector2.zero;
      private Vector3Int lastHoverCellPos = new Vector3Int(0, 0, -1);

      private Rect camBounds;
      private Preview preview = new Preview();

      private Dictionary<int, Layer> layers;

      private List<PlacedPrefab> placedPrefabs = new List<PlacedPrefab>();

      private PlacedPrefab selectedPrefab;


      private void Awake () {
         size = new Vector2Int(2000, 2000);
         origin = new Vector2Int(-1000, -1000);

         eventCanvas = GetComponentInChildren<EventTrigger>();
         grid = GetComponentInChildren<Grid>();
         cam = GetComponentInChildren<Camera>();

         recalculateCamBounds();
         clampCamPos();

         setUpLayers();

         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerDown, pointerDown);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.Drag, drag);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerEnter, pointerEnter);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerExit, pointerExit);
      }

      private void Start () {
         updateBrushOutline();
      }

      private void OnEnable () {
         MainCamera.SizeChanged += mainCamSizeChanged;
         Tools.AnythingChanged += toolsAnythingChanged;
         Tools.ToolChanged += toolChanged;
      }

      private void OnDisable () {
         MainCamera.SizeChanged -= mainCamSizeChanged;
         Tools.AnythingChanged -= toolsAnythingChanged;
         Tools.ToolChanged -= toolChanged;
      }

      private void Update () {
         if (pointerHovering && Input.mouseScrollDelta.y != 0)
            pointerScroll(Input.mouseScrollDelta.y);

         if (pointerHovering && lastMouseHoverPos != Input.mousePosition) {
            pointerMove(Input.mousePosition);
            lastMouseHoverPos = Input.mousePosition;
         }

         //Generates a random mountainy area
         //if(Input.GetKeyDown(KeyCode.P))
         //{
         //    layers[PaletteData.MountainLayer].Default.ClearAllTiles();
         //
         //    for(int i = 0; i < 1000; i++)
         //    {
         //        ChangeBoard(BoardChange.CalculateMountainChanges(
         //            palette.SelectedGroup as MountainGroup, 
         //            new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), 0), 
         //            layers[PaletteData.MountainLayer].Default));
         //    }
         //    
         //}
      }

      private void toolChanged(ToolType from, ToolType to) {
         if (from == ToolType.Selection && selectedPrefab != null)
            selectPrefab(null, false);
      }

      private void mainCamSizeChanged (Vector2Int newSize) {
         recalculateCamBounds();
         clampCamPos();
      }

      private void toolsAnythingChanged () {
         updateBrushOutline();
      }

      private void recalculateCamBounds () {
         var canvasRectT = eventCanvas.GetComponent<RectTransform>();
         camBounds = new Rect(
             new Vector2(-canvasRectT.sizeDelta.x * 0.5f, -canvasRectT.sizeDelta.y * 0.5f),
             canvasRectT.sizeDelta);
      }

      private void setUpLayers () {
         layers = new Dictionary<int, Layer>();
         foreach (LayerName ln in layerDefinitions) {
            Tilemap[] subLayers = new Tilemap[10];
            for (int i = 0; i < 10; i++) {
               Tilemap layer = Instantiate(layerPref, Vector3.zero, Quaternion.identity, layerContainer);
               layer.gameObject.name = ln.name + " " + i;
               layer.transform.localPosition = new Vector3(0, 0, layerToZ(ln.layer, i));
               subLayers[i] = layer;
            }
            layers.Add(ln.layer, new Layer(subLayers));
         }

         if (!layers.ContainsKey(0)) {
            Tilemap layer = Instantiate(layerPref, Vector3.zero, Quaternion.identity, layerContainer);
            layer.gameObject.name = "default";
            layer.transform.localPosition = new Vector3(0, 0, layerToZ(0));
            layers.Add(0, new Layer(layer));
         }
      }

      private float layerToZ (int layer, int subLayer = 0) {
         return layer * -0.01f + subLayer * -0.001f;
      }

      private void clampCamPos () {
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

      public void performUndoRedo (UndoRedoData undoRedoData) {
         BoardUndoRedoData data = undoRedoData as BoardUndoRedoData;
         changeBoard(data.change, false);
      }

      public void performUndoRedoPrefabSelection (UndoRedoData undoRedoData) {
         SelectedPrefabUndoRedoData data = undoRedoData as SelectedPrefabUndoRedoData;
         selectPrefab(data.prefab, data.position, false);
      }

      public void performUndoRedoPrefabData (UndoRedoData undoRedoData) {
         PrefabDataUndoRedoData data = undoRedoData as PrefabDataUndoRedoData;

         var pref = placedPrefabs.FirstOrDefault(p => p.original == data.prefab &&
                     (Vector2) p.placedInstance.transform.position == (Vector2) data.position);

         setPrefabData(pref, data.key, data.value, false);
      }

      public void changeBiome (PaletteData from, PaletteData to) {
         if (from != to)
            changeBoard(getBiomeChange(from, to), false);
      }

      public void clearAll () {

         changeBoard(BoardChange.calculateClearAllChange(layers, placedPrefabs));
      }

      public void applyDeserializedData (DeserializedProject data) {
         changeBoard(BoardChange.calculateDeserializedDataChange(data, layers, placedPrefabs));
         foreach (var pref in data.prefabs) {
            PlacedPrefab pp = placedPrefabs.FirstOrDefault(p => p.original == pref.prefab &&
                     (Vector2) p.placedInstance.transform.position == (Vector2) pref.position);

            if (pp != null && pref.dataFields != null) {
               foreach (var df in pref.dataFields) {
                  setPrefabData(pp, df.k, df.v, false);
               }
            }
         }
      }

      private void changeBoard (BoardChange change, bool registerUndo = true) {
         BoardChange undoChange = new BoardChange();

         foreach (var group in change.tileChanges.GroupBy(tc => tc.layer)) {
            Vector3Int[] positions = group.Select(tc => tc.position).ToArray();
            TileBase[] tiles = group.Select(tc => tc.tile).ToArray();

            undoChange.add(group.Key.calculateUndoChange(positions, tiles));
            group.Key.setTiles(positions, tiles);
         }

         foreach (var pc in change.prefabChanges) {
            if (pc.prefabToPlace != null) {
               var pref = instantiatePlacedPrefab(pc.prefabToPlace, pc.positionToPlace);

               undoChange.prefabChanges.Add(new PrefabChange {
                  prefabToDestroy = pc.prefabToPlace,
                  positionToDestroy = new Vector3(pref.transform.position.x, pref.transform.position.y, 0)
               });

               var newPref = new PlacedPrefab {
                  placedInstance = pref,
                  original = pc.prefabToPlace
               };

               placedPrefabs.Add(newPref);

               if (pc.dataToSet != null) {
                  foreach (var d in pc.dataToSet)
                     setPrefabData(newPref, d.Key, d.Value, false);
               } else {
                  var pdd = pref.GetComponent<PrefabDataDefinition>();
                  if (pdd != null) {
                     foreach (var kv in pdd.dataFields)
                        setPrefabData(newPref, kv.name, kv.defaultValue, false);
                     foreach (var kv in pdd.selectDataFields)
                        setPrefabData(newPref, kv.name, kv.options[kv.defaultOption], false);
                  }
               }
            }

            if (pc.prefabToDestroy != null) {
               var prefabToDestroy = placedPrefabs.FirstOrDefault(p =>
                   p.original == pc.prefabToDestroy &&
                   (Vector2) p.placedInstance.transform.position == (Vector2) pc.positionToDestroy);

               if (prefabToDestroy != null) {
                  if (prefabToDestroy == selectedPrefab)
                     selectPrefab(null);

                  undoChange.prefabChanges.Add(new PrefabChange {
                     prefabToPlace = prefabToDestroy.original,
                     positionToPlace = prefabToDestroy.placedInstance.transform.position,
                     dataToSet = prefabToDestroy.data.Clone()
                  });

                  Destroy(prefabToDestroy.placedInstance);
                  placedPrefabs.Remove(prefabToDestroy);
               }
            }
         }

         if (registerUndo && !undoChange.empty)
            Undo.register(
                performUndoRedo,
                new BoardUndoRedoData { change = undoChange },
                new BoardUndoRedoData { change = change });
      }

      public void setSelectedPrefabData (string key, string value, bool recordUndo = true) {
         setPrefabData(selectedPrefab, key, value, recordUndo);
      }

      public void setPrefabData (PlacedPrefab prefab, string key, string value, bool recordUndo = true) {
         if (recordUndo) {
            Undo.register(
               performUndoRedoPrefabData,
               new PrefabDataUndoRedoData {
                  prefab = prefab.original,
                  position = prefab.placedInstance.transform.position,
                  key = key,
                  value = prefab.getData(key)
               },
               new PrefabDataUndoRedoData {
                  prefab = prefab.original,
                  position = prefab.placedInstance.transform.position,
                  key = key,
                  value = value
               });
         }
         prefab.setData(key, value);

         var listener = prefab.placedInstance.GetComponent<IPrefabDataListener>();
         if (listener != null)
            listener.dataFieldChanged(key, value);

         if (prefab == selectedPrefab)
            SelectedPrefabDataChanged(prefab);
      }

      private GameObject instantiatePlacedPrefab (GameObject original, Vector3 position) {
         var instance = Instantiate(original, position, Quaternion.identity, prefabLayer);

         if (instance.GetComponent<ZSnap>())
            instance.GetComponent<ZSnap>().roundoutPosition();

         return instance;
      }

      private BoardChange getBiomeChange (PaletteData from, PaletteData to) {
         ensurePreviewCleared();
         BoardChange result = new BoardChange();

         //-------------------------------------
         //Handle tiles
         foreach (var layer in getLayersEnumerator()) {
            for (int i = layer.origin.x; i < layer.size.x; i++) {
               for (int j = layer.origin.y; j < layer.size.y; j++) {
                  TileBase tile = layer.getTile(new Vector3Int(i, j, 0));
                  if (tile != null && tile != transparentTile) {
                     Vector2Int index = from.indexOf(tile);
                     if (index.x == -1)
                        throw new System.Exception("Unrecognized tile!");

                     result.tileChanges.Add(new TileChange(to.getTile(index.x, index.y)?.tile, new Vector3Int(i, j, 0), layer));
                  }
               }
            }
         }
         //-------------------------------------
         //Handle prefabs
         foreach (var pref in placedPrefabs) {
            for (int i = 0; i < from.prefabGroups.Count; i++) {
               if (from.prefabGroups[i].refPref == pref.original) {
                  result.prefabChanges.Add(new PrefabChange {
                     positionToPlace = pref.placedInstance.transform.position,
                     prefabToPlace = to.prefabGroups[i].refPref,
                     prefabToDestroy = pref.original,
                     positionToDestroy = pref.placedInstance.transform.position,
                     dataToSet = pref.data.Clone()
                  });
                  break;
               } else {
                  var treeGroup = from.prefabGroups[i] as TreePrefabGroup;
                  if (treeGroup != null && treeGroup.burrowedPref == pref.original) {
                     result.prefabChanges.Add(new PrefabChange {
                        positionToPlace = pref.placedInstance.transform.position,
                        prefabToPlace = (to.prefabGroups[i] as TreePrefabGroup).burrowedPref,
                        prefabToDestroy = pref.original,
                        positionToDestroy = pref.placedInstance.transform.position,
                        dataToSet = pref.data.Clone()
                     });
                     break;
                  }
               }
               if (i == from.prefabGroups.Count - 1)
                  throw new System.Exception("Unrecognized prefab!");
            }
         }

         return result;
      }

      /// <summary>
      /// Forms the potential change, that would apply, if the users made an input at a given mouse position
      /// </summary>
      /// <param name="pointerWorldPosition"></param>
      /// <returns></returns>
      private BoardChange getPotentialBoardChange (Vector3 pointerWorldPosition, bool excludePrefabs = false) {
         BoardChange result = new BoardChange();

         if (Tools.toolType == ToolType.Eraser) {
            result.add(BoardChange.calculateEraserChange(
                getLayersEnumerator(),
                excludePrefabs ? new List<PlacedPrefab>() : placedPrefabs,
                Tools.eraserLayerMode,
                pointerWorldPosition));
         } else if (Tools.toolType == ToolType.Brush) {
            if (Tools.tileGroup != null) {
               if (Tools.tileGroup.type == TileGroupType.Regular) {
                  result.add(BoardChange.calculateRegularTileGroupChanges(Tools.tileGroup, layers, pointerWorldPosition));
               } else if (Tools.tileGroup.type == TileGroupType.NineFour) {
                  NineFourGroup group = Tools.tileGroup as NineFourGroup;
                  result.add(BoardChange.calculateNineFourChanges(group, layers[group.layer], pointerWorldPosition));
               } else if (Tools.tileGroup.type == TileGroupType.Nine) {
                  NineGroup group = Tools.tileGroup as NineGroup;
                  result.add(BoardChange.calculateNineGroupChanges(group, layers[group.layer].subLayers[group.subLayer], pointerWorldPosition));
               } else if ((Tools.tileGroup.type == TileGroupType.Prefab ||
                     Tools.tileGroup.type == TileGroupType.TreePrefab) && !excludePrefabs) {
                  result.prefabChanges.Add(new PrefabChange {
                     prefabToPlace = Tools.selectedPrefab,
                     positionToPlace = pointerWorldPosition
                  });
               } else if (Tools.tileGroup.type == TileGroupType.NineSliceInOut) {
                  NineSliceInOutGroup group = Tools.tileGroup as NineSliceInOutGroup;
                  result.add(BoardChange.calculateNineSliceInOutchanges(
                      group,
                      layers[group.tiles[0, 0].layer].subLayers[group.tiles[0, 0].subLayer],
                      pointerWorldPosition));
               } else if (Tools.tileGroup.type == TileGroupType.Mountain) {
                  result.add(BoardChange.calculateMountainChanges(
                      Tools.tileGroup as MountainGroup,
                      pointerWorldPosition,
                      layers[PaletteData.MountainLayer].subLayers[Tools.mountainLayer]));
               } else if (Tools.tileGroup.type == TileGroupType.Dock) {
                  var group = Tools.tileGroup as DockGroup;
                  result.add(BoardChange.calculateDockChanges(group, layers[group.layer].subLayers[group.subLayer], pointerWorldPosition));
               } else if (Tools.tileGroup.type == TileGroupType.Wall) {
                  var group = Tools.tileGroup as WallGroup;
                  result.add(BoardChange.calculateWallChanges(group, layers[group.layer].subLayers[0], pointerWorldPosition));
               }
            }
         } else if (Tools.toolType == ToolType.Fill) {
            if (Tools.tileGroup != null) {
               if (Tools.tileGroup.maxTileCount == 1) {
                  PaletteTilesData.TileData tile = Tools.tileGroup.tiles[0, 0];

                  if (Tools.fillBounds == FillBounds.SingleLayer)
                     result.add(BoardChange.calculateFillChanges(tile.tile, layers[tile.layer].subLayers[tile.subLayer], pointerWorldPosition));
                  else
                     result.add(BoardChange.calculateFillChanges(tile, layers, pointerWorldPosition));
               }
            }
         }
         return result;
      }

      /// <summary>
      /// Enumerates over all the layers that have a tilemap, excluding container layers
      /// </summary>
      /// <returns></returns>
      public IEnumerable<Layer> getLayersEnumerator () {
         foreach (Layer layer in layers.Values) {
            if (layer.hasTilemap)
               yield return layer;
            else {
               foreach (Layer sublayer in layer.subLayers) {
                  if (sublayer.hasTilemap)
                     yield return sublayer;
               }
            }
         }
      }
      public string formSerializedData () {
         return Serializer.serialize(layers, placedPrefabs, Tools.biome, false);
      }

      public void ensurePreviewCleared () {
         if (preview.previewTilesSet) {
            foreach (Layer layer in getLayersEnumerator())
               layer.clearAllPreviewTiles();

            preview.previewTilesSet = false;
         }

         if (preview.prefabPreviewInstance != null) {
            Destroy(preview.prefabPreviewInstance);
            preview.prefabPreviewInstance = null;
            preview.targetPrefab = null;
         }
         foreach (PlacedPrefab pp in placedPrefabs) {
            pp.placedInstance.SetActive(true);
            pp.setHighlight(false, false);
         }

         if (selectedPrefab != null) {
            selectedPrefab.setHighlight(false, true);
         }
      }

      private void updatePreview (Vector3 pointerScreenPos) {
         ensurePreviewCleared();
         Vector2 worldPos = cam.ScreenToWorldPoint(pointerScreenPos);
         BoardChange change = Tools.toolType == ToolType.Brush
             ? getPotentialBoardChange(worldPos)
             : new BoardChange();

         updateBrushOutline();

         //-------------------
         //Handle prefab to be placed
         PrefabChange prefToAdd = change.prefabChanges.Count == 0
             ? null
             : change.prefabChanges.FirstOrDefault(p => p.prefabToPlace != null);

         if (prefToAdd != null) {
            preview.targetPrefab = prefToAdd.prefabToPlace;
            preview.prefabPreviewInstance = Instantiate(preview.targetPrefab, prefabLayer);

            var zSnap = preview.prefabPreviewInstance.GetComponent<ZSnap>();
            if (zSnap != null)
               zSnap.isActive = true;
         }

         if (preview.prefabPreviewInstance != null)
            preview.prefabPreviewInstance.transform.position = worldPos;

         //------------------------
         //Handle prefabs to destroy
         foreach (PrefabChange pChange in change.prefabChanges) {
            if (pChange.prefabToDestroy != null) {
               placedPrefabs.First(p =>
                   p.original == pChange.prefabToDestroy &&
                   (Vector2) p.placedInstance.transform.position == (Vector2) pChange.positionToDestroy)
                   .placedInstance.SetActive(false);
            }
         }

         foreach (var tc in change.tileChanges) {
            if (tc.tile == null)
               tc.layer.setPreviewTile(tc.position, transparentTile);
            else
               tc.layer.setPreviewTile(tc.position, tc.tile);
            preview.previewTilesSet = true;
         }

         //--------------------------------
         //Handle prefab selection
         if (Tools.toolType == ToolType.Selection) {
            var target = getHoveredPrefab();

            if (target != null && target != selectedPrefab) {
               target.setHighlight(true, false);
            }
         }
      }

      private void updateBrushOutline () {
         brushOutline.enabled =
             Tools.toolType == ToolType.Eraser ||
             (Tools.toolType == ToolType.Brush && Tools.tileGroup != null && !(Tools.tileGroup is PrefabGroup));

         if (!brushOutline.enabled)
            return;

         brushOutline.transform.position = cellToWorldCenter(worldToCell(MainCamera.stwp(Input.mousePosition)));

         if (Tools.toolType == ToolType.Eraser || Tools.tileGroup == null)
            brushOutline.size = Vector2.one;
         else {
            brushOutline.size = Tools.tileGroup.brushSize;
            //Handle cases where the outline is offset on even number size groups
            brushOutline.transform.position += new Vector3(
                Tools.tileGroup.brushSize.x % 2 == 0 ? -0.5f : 0,
                Tools.tileGroup.brushSize.y % 2 == 0 ? -0.5f : 0,
                0);
         }

         brushOutline.color = Tools.toolType == ToolType.Eraser ? Color.red : Color.green;
      }

      private void selectPrefab (PlacedPrefab prefab, bool recordUndo = true) {
         if (recordUndo) {
            Undo.register(
                performUndoRedoPrefabSelection,
                new SelectedPrefabUndoRedoData {
                   prefab = selectedPrefab?.original,
                   position = selectedPrefab == null ? Vector3.zero : selectedPrefab.placedInstance.transform.position
                },
                new SelectedPrefabUndoRedoData {
                   prefab = prefab?.original,
                   position = prefab == null ? Vector3.zero : prefab.placedInstance.transform.position
                });
         }

         selectedPrefab = prefab;
         SelectedPrefabChanged?.Invoke(selectedPrefab?.placedInstance.GetComponent<PrefabDataDefinition>(), selectedPrefab);
      }

      private void selectPrefab (GameObject prefab, Vector3 position, bool recordUndo = true) {
         selectPrefab(placedPrefabs.FirstOrDefault(p => p.original == prefab &&
                     (Vector2) p.placedInstance.transform.position == (Vector2) position), recordUndo);
      }

      private PlacedPrefab getHoveredPrefab () {
         Vector2 pointerPos = MainCamera.stwp(Input.mousePosition);
         PlacedPrefab target = null;
         float minZ = float.MaxValue;

         foreach (var pp in placedPrefabs) {
            var col = pp.placedInstance.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(pointerPos)) {
               if (col.transform.position.z < minZ) {
                  minZ = col.transform.position.z;
                  target = pp;
               }
            }
         }

         return target;
      }

      public static Vector3Int worldToCell (Vector3 worldPosition) {
         return new Vector3Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y), 0);
      }

      /// <summary>
      /// Given a cell index, returns the center of that cell in world coordinates
      /// </summary>
      /// <param name="cellPosition"></param>
      /// <returns></returns>
      public static Vector3 cellToWorldCenter (Vector3Int cellPosition) {
         return new Vector3(cellPosition.x + 0.5f, cellPosition.y + 0.5f, 0);
      }

      #region Events

      private void pointerEnter (PointerEventData data) {
         pointerHovering = true;
      }

      private void pointerExit (PointerEventData data) {
         pointerHovering = false;
         ensurePreviewCleared();
      }

      private void pointerDown (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            if (Tools.toolType == ToolType.Brush || Tools.toolType == ToolType.Eraser || Tools.toolType == ToolType.Fill)
               changeBoard(getPotentialBoardChange(data.pointerCurrentRaycast.worldPosition));
            else if (Tools.toolType == ToolType.Selection)
               selectPrefab(getHoveredPrefab());
         }
         updatePreview(data.position);
      }

      private void drag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left)
            changeBoard(getPotentialBoardChange(data.pointerCurrentRaycast.worldPosition, true));
         else {
            Vector3 curPos = cam.ScreenToWorldPoint(data.position);
            Vector3 prevPos = cam.ScreenToWorldPoint(data.position + data.delta);
            cam.transform.localPosition += curPos - prevPos;

            clampCamPos();
         }
         updatePreview(data.position);
      }

      private void pointerMove (Vector3 screenPos) {
         updatePreview(screenPos);
      }

      private void pointerScroll (float scroll) {
         cam.orthographicSize = Mathf.Clamp(
             cam.orthographicSize + scroll * -zoomSpeed.Evaluate(cam.orthographicSize) * Time.deltaTime,
             minZoom,
             maxZoom);

         clampCamPos();
      }
      #endregion
   }
   [System.Serializable]
   public class LayerName
   {
      public string name;
      public int layer;
   }

   public class Preview
   {
      public GameObject prefabPreviewInstance { get; set; }
      public GameObject targetPrefab { get; set; }
      public bool previewTilesSet { get; set; }
   }

   public class PlacedPrefab
   {
      public GameObject placedInstance { get; set; }
      public GameObject original { get; set; }

      public Dictionary<string, string> data { get; private set; }

      public PlacedPrefab () {
         data = new Dictionary<string, string>();
      }

      public void setHighlight (bool hovered, bool selected) {
         var outline = placedInstance.GetComponent<SpriteOutline>();
         var highlight = placedInstance.GetComponent<PrefabHighlight>();

         if (outline != null) {
            if (!hovered && !selected) {
               outline.setVisibility(false);
            } else if (hovered) {
               outline.setVisibility(true);
               outline.setNewColor(Color.white);
               outline.Regenerate();
            } else if (selected) {
               outline.setVisibility(true);
               outline.setNewColor(Color.green);
               outline.Regenerate();
            }
         }

         if (highlight != null)
            highlight.setHighlight(hovered, selected);
      }

      public void setData (string key, string value) {
         if (data.ContainsKey(key))
            data[key] = value;
         else
            data.Add(key, value);
      }

      public string getData (string key) {
         if (data.TryGetValue(key, out string value))
            return value;
         return string.Empty;
      }
   }

   public struct SidesInt
   {
      public int left;
      public int right;
      public int top;
      public int bot;
      public static SidesInt fromOffset (Vector2Int offsetMin, Vector2Int offsetMax) {
         return new SidesInt {
            left = offsetMin.x,
            bot = offsetMin.y,
            right = offsetMax.x,
            top = offsetMax.y
         };
      }
      public override string ToString () {
         return $"left = {left}, right = {right}, bot = {bot}, top = {top}";
      }
   }
}
