using System.Collections.Generic;
using System.Linq;
using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class DrawBoard : MonoBehaviour
   {
      public static event System.Action<Selection> SelectionChanged;
      public static event System.Action<PlacedPrefab> PrefabDataChanged;
      public static event System.Action<MapVersion> loadedVersionChanged;

      public static DrawBoard instance { get; private set; }
      public static Vector2Int size { get; private set; }
      public static Vector2Int origin { get; private set; }
      public static MapVersion loadedVersion { get; private set; }

      [SerializeField]
      private EditorConfig config = null;
      [SerializeField]
      private Palette palette = null;
      [SerializeField]
      private Tilemap layerPref = null;
      [SerializeField]
      private Transform prefabLayer = null;
      [SerializeField]
      private Transform layerContainer = null;
      [SerializeField]
      private SpriteRenderer brushOutline = null;
      [SerializeField]
      private TileBase transparentTile = null;
      [SerializeField]
      private SpriteRenderer sizeCover = null;
      [SerializeField]
      private Tilemap tileSelectionTilemap = null;
      [SerializeField]
      private TileBase selectedTileHighlight = null;
      [SerializeField]
      private TileBase hoveredTileHighlight = null;

      private EventTrigger eventCanvas;
      private Camera cam;

      private bool pointerHovering = false;
      private Vector3 lastMouseHoverPos = Vector2.zero;

      private Preview preview = new Preview();

      public Dictionary<string, Layer> layers { get; private set; }

      private List<PlacedPrefab> placedPrefabs = new List<PlacedPrefab>();

      public Selection currentSelection { get; private set; }

      private Vector3? draggingFrom = null;

      private Layer tileSelectionLayer;

      private void Awake () {
         instance = this;

         currentSelection = new Selection();

         eventCanvas = GetComponentInChildren<EventTrigger>();
         cam = GetComponentInChildren<Camera>();

         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerDown, pointerDown);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.Drag, drag);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerEnter, pointerEnter);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerExit, pointerExit);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.BeginDrag, pointerBeginDrag);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.EndDrag, pointerEndDrag);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerUp, pointerUp);

         DrawBoardEvents.PointerScroll += pointerScroll;

         tileSelectionLayer = new Layer(tileSelectionTilemap);
      }

      private void Start () {
         setUpLayers(Tools.editorType);
         setBoardSize(Tools.boardSize);
         changeLoadedVersion(null);
         updateBrushOutline();
      }

      private void OnEnable () {
         Tools.AnythingChanged += toolsAnythingChanged;
         Tools.EditorTypeChanged += editorTypeChanged;
         Tools.BoardSizeChanged += boardSizeChanged;
      }

      private void OnDisable () {
         Tools.AnythingChanged -= toolsAnythingChanged;
         Tools.EditorTypeChanged -= editorTypeChanged;
         Tools.BoardSizeChanged -= boardSizeChanged;
      }

      private void Update () {
         if (pointerHovering && lastMouseHoverPos != Input.mousePosition) {
            pointerMove(Input.mousePosition);
            lastMouseHoverPos = Input.mousePosition;
         }
      }

      private void boardSizeChanged (Vector2Int from, Vector2Int to) {
         changeBoard(BoardChange.calculateClearAllChange(layers, placedPrefabs, currentSelection));
         setBoardSize(to);
         changeLoadedVersion(null);
      }

      private void editorTypeChanged (EditorType from, EditorType to) {
         changeBoard(BoardChange.calculateClearAllChange(layers, placedPrefabs, currentSelection));
         setUpLayers(to);
         changeLoadedVersion(null);
      }

      private void toolsAnythingChanged () {
         updateBrushOutline();
      }

      public static void changeLoadedVersion (MapVersion version) {
         loadedVersion = version;
         loadedVersionChanged?.Invoke(loadedVersion);
      }

      private void setBoardSize (Vector2Int size) {
         DrawBoard.size = size;
         origin = new Vector2Int(-size.x / 2, -size.y / 2);

         sizeCover.transform.localScale = new Vector3(size.x, size.y, 1);
         sizeCover.transform.position = new Vector3(
            size.x % 2 == 0 ? 0 : 0.5f,
            size.y % 2 == 0 ? 0 : 0.5f,
            sizeCover.transform.position.z);
      }

      private void setUpLayers (EditorType editorType) {
         if (layers != null) {
            foreach (Layer layer in layers.Values)
               layer.Destroy();
         }

         layers = new Dictionary<string, Layer>();

         foreach (var l in config.getLayers(editorType).OrderBy(l => l.index)) {
            Tilemap[] subLayers = new Tilemap[10];
            for (int i = 0; i < 10; i++) {
               Tilemap layer = Instantiate(layerPref, Vector3.zero, Quaternion.identity, layerContainer);
               layer.gameObject.name = l.layer + " " + i;
               layer.transform.localPosition = new Vector3(0, 0, layerToZ(l.index, i) + l.zOffset);
               subLayers[i] = layer;
            }
            layers.Add(l.layer, new Layer(subLayers));
         }
      }

      private float layerToZ (int layer, int subLayer = 0) {
         return layer * -0.01f + subLayer * -0.001f;
      }

      public void performUndoRedo (UndoRedoData undoRedoData) {
         BoardUndoRedoData data = undoRedoData as BoardUndoRedoData;
         changeBoard(data.change, false);
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
         changeBoard(BoardChange.calculateClearAllChange(layers, placedPrefabs, currentSelection));
      }

      public void newMap () {
         clearAll();
         changeLoadedVersion(null);
         Undo.clear();
      }

      public void applyDeserializedData (DeserializedProject data, MapVersion mapVersion) {
         changeLoadedVersion(null);

         changeBoard(BoardChange.calculateDeserializedDataChange(data, layers, placedPrefabs, currentSelection));
         foreach (var pref in data.prefabs) {
            PlacedPrefab pp = placedPrefabs.FirstOrDefault(p => p.original == pref.prefab &&
                     (Vector2) p.placedInstance.transform.position == (Vector2) pref.position);

            if (pp != null && pref.dataFields != null) {
               foreach (var df in pref.dataFields) {
                  setPrefabData(pp, df.k, df.v, false);
               }
            }
         }

         changeLoadedVersion(mapVersion);
      }

      public void changeBoard (BoardChange change, bool registerUndo = true) {
         if (change == null) return;

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
                     pdd.restructureCustomFields();
                     foreach (var kv in pdd.dataFields)
                        setPrefabData(newPref, kv.name, kv.defaultValue, false);
                     foreach (var kv in pdd.selectDataFields)
                        setPrefabData(newPref, kv.name, kv.options[kv.defaultOption].value, false);
                  }
               }
            }

            if (pc.prefabToDestroy != null) {
               var prefabToDestroy = placedPrefabs.FirstOrDefault(p =>
                   p.original == pc.prefabToDestroy &&
                   (Vector2) p.placedInstance.transform.position == (Vector2) pc.positionToDestroy);

               if (prefabToDestroy != null) {
                  currentSelection.remove(prefabToDestroy);

                  undoChange.prefabChanges.Add(new PrefabChange {
                     prefabToPlace = prefabToDestroy.original,
                     positionToPlace = prefabToDestroy.placedInstance.transform.position,
                     dataToSet = prefabToDestroy.data.Clone()
                  });

                  Destroy(prefabToDestroy.placedInstance);
                  placedPrefabs.Remove(prefabToDestroy);
               }
            } else if (pc.prefabToTranslate != null) {
               PlacedPrefab toTranslate = placedPrefabs.FirstOrDefault(p =>
                   p.original == pc.prefabToTranslate &&
                   (Vector2) p.placedInstance.transform.position == (Vector2) pc.positionToPlace);

               if (toTranslate != null) {
                  undoChange.prefabChanges.Add(new PrefabChange {
                     prefabToTranslate = toTranslate.original,
                     translation = -pc.translation
                  });

                  toTranslate.placedInstance.transform.position += pc.translation;
               }
            }
         }

         currentSelection.remove(change.selectionToRemove.tiles);
         currentSelection.remove(change.selectionToRemove.prefabs);
         currentSelection.add(change.selectionToAdd.tiles);
         currentSelection.add(change.selectionToAdd.prefabs);

         undoChange.selectionToRemove.add(change.selectionToAdd.tiles);
         undoChange.selectionToRemove.add(change.selectionToAdd.prefabs);
         undoChange.selectionToAdd.add(change.selectionToRemove.tiles);
         undoChange.selectionToAdd.add(change.selectionToRemove.prefabs);

         foreach (PlacedPrefab prefab in change.selectionToAdd.prefabs) {
            if (prefab.placedInstance != null) {
               prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setSelected(true);
            }
         }

         foreach (PlacedPrefab prefab in change.selectionToRemove.prefabs) {
            if (prefab.placedInstance != null) {
               prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setSelected(false);
            }
         }

         tileSelectionLayer.setTiles(
            change.selectionToRemove.tiles.ToArray(),
            Enumerable.Repeat<TileBase>(null, change.selectionToRemove.tiles.Count).ToArray());
         tileSelectionLayer.setTiles(
            change.selectionToAdd.tiles.ToArray(),
            Enumerable.Repeat<TileBase>(selectedTileHighlight, change.selectionToAdd.tiles.Count).ToArray());

         recalculateInheritedSorting();

         if (registerUndo && !undoChange.empty)
            Undo.register(
                performUndoRedo,
                new BoardUndoRedoData { change = undoChange },
                new BoardUndoRedoData { change = change });

         if (!change.selectionToAdd.empty || !change.selectionToRemove.empty) {
            SelectionChanged?.Invoke(currentSelection);
         }
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
            listener.dataFieldChanged(new DataField { k = key, v = value });

         PrefabDataChanged(prefab);
      }

      private GameObject instantiatePlacedPrefab (GameObject original, Vector3 position) {
         var instance = Instantiate(original, position, Quaternion.identity, prefabLayer);
         instance.name = original.name;

         ZSnap snap = instance.GetComponent<ZSnap>();
         if (snap != null) {
            snap.initialize();
         }

         foreach (IBiomable biomable in instance.GetComponentsInChildren<IBiomable>()) {
            biomable.setBiome(Tools.biome);
         }

         if (instance.GetComponent<MapEditorPrefab>())
            instance.GetComponent<MapEditorPrefab>().placedInEditor();

         instance.GetComponent<SpriteOutline>()?.setNewColor(new Color(0, 0, 0, 0));

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
                  if (tile == null || tile == transparentTile) {
                     continue;
                  }
                  Vector2Int index = AssetSerializationMaps.getIndex(tile, from.type.Value);
                  TileBase newTile = AssetSerializationMaps.getTile(index, to.type.Value);

                  result.tileChanges.Add(new TileChange(newTile, new Vector3Int(i, j, 0), layer));
               }
            }
         }
         //-------------------------------------
         //Handle prefabs
         foreach (var pref in placedPrefabs) {
            int index = AssetSerializationMaps.getIndex(pref.original, from.type.Value);
            GameObject newPref = AssetSerializationMaps.getPrefab(index, to.type.Value, true);
            result.prefabChanges.Add(new PrefabChange {
               positionToPlace = pref.placedInstance.transform.position,
               prefabToPlace = newPref,
               prefabToDestroy = pref.original,
               positionToDestroy = pref.placedInstance.transform.position,
               dataToSet = pref.data.Clone()
            });
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

         if (Tools.toolType == ToolType.Move && draggingFrom != null) {
            if (!currentSelection.empty) {
               result.add(BoardChange.calculateMoveChange(draggingFrom.Value, pointerWorldPosition, getLayersEnumerator(), placedPrefabs, currentSelection));
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
         return Serializer.serialize(layers, placedPrefabs, Tools.biome, Tools.editorType, Tools.boardSize, false);
      }

      public string formExportData () {
         return Serializer.serializeExport(layers, placedPrefabs, Tools.biome, Tools.editorType, config, palette.formCollisionDictionary(), origin, size);
      }

      public List<MapSpawn> formSpawnList (int? mapId, int mapVersion) {
         return GetComponentsInChildren<SpawnMapEditor>().Select(s => new MapSpawn {
            mapId = mapId ?? -1,
            mapVersion = mapVersion,
            name = s.spawnName,
            posX = s.transform.position.x * 0.16f,
            posY = s.transform.position.y * 0.16f
         }
         ).ToList();
      }

      public void setTilesModifyPreview (BoardChange change) {
         foreach (Layer layer in getLayersEnumerator())
            layer.clearAllPreviewTiles();

         if (change == null) {
            return;
         }

         foreach (var tc in change.tileChanges) {
            if (tc.tile == null)
               tc.layer.setPreviewTile(tc.position, transparentTile);
            else
               tc.layer.setPreviewTile(tc.position, tc.tile);
         }
      }

      public void setPrefabsModifyPreview (BoardChange change) {
         foreach (PlacedPrefab prefab in preview.prefabs) {
            Destroy(prefab.placedInstance);
         }

         preview.prefabs.Clear();

         foreach (PlacedPrefab prefab in placedPrefabs) {
            prefab.setVisible(true);
         }

         if (change == null) {
            return;
         }

         foreach (PrefabChange prefChange in change.prefabChanges) {
            if (prefChange.prefabToPlace != null) {
               preview.prefabs.Add(new PlacedPrefab {
                  placedInstance = instantiatePlacedPrefab(prefChange.prefabToPlace, prefChange.positionToPlace),
                  original = prefChange.prefabToPlace
               });
            } else if (prefChange.prefabToDestroy != null) {
               PlacedPrefab target = placedPrefabs.FirstOrDefault(pp => pp.isOriginalAtPosition(prefChange.prefabToDestroy, prefChange.positionToDestroy));
               if (target != null) {
                  target.setVisible(false);
               }
            }
         }

         recalculateInheritedSorting();
      }

      public void setTilesSelectionModifyPreview (BoardChange change) {
         tileSelectionLayer.clearAllPreviewTiles();

         if (change == null) return;

         foreach (Vector3Int tile in change.selectionToAdd.tiles) {
            tileSelectionLayer.setPreviewTile(tile, hoveredTileHighlight);
         }

         foreach (Vector3Int tile in change.selectionToRemove.tiles) {
            tileSelectionLayer.setPreviewTile(tile, hoveredTileHighlight);
         }
      }

      public void setPrefabsSelectionModifyPreview (BoardChange change) {
         foreach (PlacedPrefab pp in placedPrefabs) {
            if (!currentSelection.contains(pp)) {
               pp.setHighlight(false, false, false);
            } else {
               pp.setHighlight(false, true, false);
            }
         }

         if (change == null) return;

         foreach (PlacedPrefab prefab in change.selectionToAdd.prefabs.Union(change.selectionToRemove.prefabs)) {
            prefab.setHighlight(true, false, false);
            prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setHovered(true);
         }
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
            pp.placedInstance.GetComponent<MapEditorPrefab>()?.setHovered(false);
         }
      }

      private void updatePreview (Vector3 pointerScreenPos, bool isDrag) {
         ensurePreviewCleared();
         Vector2 worldPos = cam.ScreenToWorldPoint(pointerScreenPos);
         BoardChange change = Tools.toolType == ToolType.Brush
             ? getPotentialBoardChange(worldPos)
             : new BoardChange();

         if (Tools.toolType == ToolType.Move) {
            if (draggingFrom != null) {
               change = BoardChange.calculateMoveChange(draggingFrom.Value, worldPos, getLayersEnumerator(), placedPrefabs, currentSelection);
            }
         }

         updateBrushOutline();

         //-------------------
         //Handle prefab to be placed
         PrefabChange prefToAdd = change.prefabChanges.Count == 0
             ? null
             : change.prefabChanges.FirstOrDefault(p => p.prefabToPlace != null);

         if (prefToAdd != null) {
            preview.targetPrefab = prefToAdd.prefabToPlace;
            preview.prefabPreviewInstance = Instantiate(preview.targetPrefab, prefabLayer);
            preview.prefabPreviewInstance.name = preview.targetPrefab.name;
            preview.prefabPreviewInstance.transform.position = prefToAdd.positionToPlace;

            foreach (SpriteSwapper swapper in preview.prefabPreviewInstance.GetComponentsInChildren<SpriteSwapper>())
               swapper.Update();

            foreach (IBiomable biomable in preview.prefabPreviewInstance.GetComponentsInChildren<IBiomable>()) {
               biomable.setBiome(Tools.biome);
            }

            var zSnap = preview.prefabPreviewInstance.GetComponent<ZSnap>();
            if (zSnap != null)
               zSnap.isActive = true;

            preview.prefabPreviewInstance.GetComponent<MapEditorPrefab>()?.createdForPreview();

            preview.prefabPreviewInstance.GetComponent<SpriteOutline>()?.setNewColor(new Color(0, 0, 0, 0));
         }

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
      }

      private void updateBrushOutline () {
         brushOutline.enabled = false;
         if (Tools.toolType == ToolType.Eraser) {
            PlacedPrefab hoveredPrefab = getHoveredPrefab();
            if (hoveredPrefab != null) {
               hoveredPrefab.setHighlight(false, false, true);
            } else {
               brushOutline.enabled = true;
               brushOutline.transform.position = cellToWorldCenter(worldToCell(MainCamera.stwp(Input.mousePosition)));
               brushOutline.size = Vector2.one;
               brushOutline.color = Color.red;
            }
         } else if (Tools.toolType == ToolType.Selection) {
            brushOutline.enabled = draggingFrom != null;

            if (!brushOutline.enabled)
               return;

            Vector3 wp = MainCamera.stwp(Input.mousePosition);
            Bounds bounds = new Bounds {
               min = new Vector3(Mathf.Min(wp.x, draggingFrom.Value.x), Mathf.Min(wp.y, draggingFrom.Value.y), 0),
               max = new Vector3(Mathf.Max(wp.x, draggingFrom.Value.x), Mathf.Max(wp.y, draggingFrom.Value.y), 0)
            };

            brushOutline.transform.position = bounds.center;
            brushOutline.size = bounds.size;

            if (Settings.keybindings.getAction(Keybindings.Command.SelectionAdd)) {
               brushOutline.color = Color.green;
            } else if (Settings.keybindings.getAction(Keybindings.Command.SelectionRemove)) {
               brushOutline.color = Color.red;
            } else {
               brushOutline.color = Color.blue;
            }
         } else {
            brushOutline.enabled = (Tools.toolType == ToolType.Brush && Tools.tileGroup != null && !(Tools.tileGroup is PrefabGroup));

            if (!brushOutline.enabled)
               return;

            brushOutline.transform.position = cellToWorldCenter(worldToCell(MainCamera.stwp(Input.mousePosition)));

            if (Tools.tileGroup == null)
               brushOutline.size = Vector2.one;
            else {
               brushOutline.size = Tools.tileGroup.brushSize;
               //Handle cases where the outline is offset on even number size groups
               brushOutline.transform.position += new Vector3(
                   Tools.tileGroup.brushSize.x % 2 == 0 ? -0.5f : 0,
                   Tools.tileGroup.brushSize.y % 2 == 0 ? -0.5f : 0,
                   0);
            }

            brushOutline.color = Color.green;
         }
      }

      private void recalculateInheritedSorting () {
         (PrefabDataDefinition data, Collider2D col, ZSnap snap)[] prefs = placedPrefabs.Union(preview.prefabs)
            .Select(p => (p.placedInstance.GetComponent<PrefabDataDefinition>(), p.placedInstance.GetComponent<Collider2D>(), p.placedInstance.GetComponent<ZSnap>()))
            .Where(p => p.Item1 != null && p.Item2 != null && p.Item3 != null)
            .Where(p => p.Item1.canInheritPosition || p.Item1.canControlPosition)
            .ToArray();

         foreach (var pref in prefs) {
            if (pref.data.canInheritPosition) {
               float minZ = float.MaxValue;
               Vector2 origin = pref.snap.sortPoint == null ? pref.snap.transform.position : pref.snap.sortPoint.transform.position;
               (PrefabDataDefinition data, Collider2D col, ZSnap snap) bestController = (null, null, null);
               foreach (var controller in prefs) {
                  if (controller.data.canControlPosition && controller.col.OverlapPoint(origin) && controller.data.transform.position.z < minZ) {
                     minZ = controller.data.transform.position.z;
                     bestController = controller;
                  }
               }

               if (bestController.data == null) {
                  pref.snap.inheritedOffsetZ = 0;
                  continue;
               }

               Vector2 bestOrigin = bestController.snap.sortPoint == null ? bestController.snap.transform.position : bestController.snap.sortPoint.transform.position;
               float diff = origin.y - bestOrigin.y;
               float maxDiff = (bestController.col.bounds.max.y - bestOrigin.y);

               pref.snap.inheritedOffsetZ = -diff - 0.1f - maxDiff * 0.2f + Mathf.Lerp(0, maxDiff * 0.2f, Mathf.InverseLerp(0, maxDiff, diff));
            }
         }
      }

      public PlacedPrefab getHoveredPrefab (Vector3? mousePostion = null) {
         Vector2 pointerPos = mousePostion ?? MainCamera.stwp(Input.mousePosition);
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

      public IEnumerable<PlacedPrefab> prefabsBetween (Vector3 p1, Vector3 p2) {
         Bounds bounds = new Bounds();
         bounds.min = new Vector3(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), -1000f);
         bounds.max = new Vector3(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y), 1000f);

         foreach (var pp in placedPrefabs) {
            var col = pp.placedInstance.GetComponent<Collider2D>();
            if (col != null && col.bounds.Intersects(bounds)) {
               yield return pp;
            }
         }
      }

      /// <summary>
      /// Returns the top-most layer, that has a tile placed at a given position
      /// </summary>
      /// <param name="position"></param>
      /// <returns></returns>
      public Layer getTopLayerWithTile (Vector3Int position) {
         foreach (Layer layer in getLayersEnumerator().Reverse()) {
            if (layer.hasTile(position)) {
               return layer;
            }
         }

         return null;
      }

      public static Bounds getPrefabBounds () {
         Bounds result = new Bounds(Vector3.zero, (Vector3Int) size);
         result.Expand(6f);
         return result;
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
         if (Tools.toolType == ToolType.Brush || Tools.toolType == ToolType.Eraser || Tools.toolType == ToolType.Fill)
            return;
         if (data.button == PointerEventData.InputButton.Left) {
            if (Tools.toolType == ToolType.Brush || Tools.toolType == ToolType.Eraser || Tools.toolType == ToolType.Fill || Tools.toolType == ToolType.Selection) {
               changeBoard(getPotentialBoardChange(data.pointerCurrentRaycast.worldPosition));
            }
         }
         updatePreview(data.position, false);
      }

      public void pointerBeginDrag (PointerEventData data) {
         if (Tools.toolType == ToolType.Brush || Tools.toolType == ToolType.Eraser || Tools.toolType == ToolType.Fill)
            return;
         if (data.button == PointerEventData.InputButton.Left) {
            if ((Tools.tileGroup != null && Tools.tileGroup.type == TileGroupType.Rect) || Tools.toolType == ToolType.Selection || Tools.toolType == ToolType.Move) {
               draggingFrom = data.pointerPressRaycast.worldPosition;
            }
         }
      }

      public void pointerEndDrag (PointerEventData data) {
         if (draggingFrom == null)
            return;

         if (data.button == PointerEventData.InputButton.Left) {
            if (Tools.toolType == ToolType.Move) {
               changeBoard(getPotentialBoardChange(data.pointerCurrentRaycast.worldPosition, true));
            }
         }

         draggingFrom = null;
         updatePreview(data.position, false);
      }

      private void drag (PointerEventData data) {
         if (data.button != PointerEventData.InputButton.Left) {
            Vector3 curPos = cam.ScreenToWorldPoint(data.position);
            Vector3 prevPos = cam.ScreenToWorldPoint(data.position + data.delta);

            MainCamera.pan(curPos - prevPos);
         }
         updatePreview(data.position, true);
      }

      private void pointerMove (Vector3 screenPos) {
         updatePreview(screenPos, draggingFrom != null);
      }

      private void pointerUp (PointerEventData data) {
         updatePreview(data.position, false);
      }

      private void pointerScroll (Vector3 at, float scroll) {
         MainCamera.zoom(-scroll * Time.deltaTime);
      }
      #endregion
   }

   public class Preview
   {
      public GameObject prefabPreviewInstance { get; set; }
      public GameObject targetPrefab { get; set; }
      public bool previewTilesSet { get; set; }

      public List<PlacedPrefab> prefabs { get; set; }

      public Preview () {
         prefabs = new List<PlacedPrefab>();
      }
   }

   public class PlacedPrefab
   {
      public GameObject placedInstance { get; set; }

      public GameObject original { get; set; }

      public Dictionary<string, string> data { get; private set; }

      public PlacedPrefab () {
         data = new Dictionary<string, string>();
      }

      public bool isOriginalAtPosition (GameObject original, Vector2 position) {
         return this.original == original && (Vector2) placedInstance.transform.position == position;
      }

      public void setVisible (bool visible) {
         foreach (Renderer ren in placedInstance.GetComponentsInChildren<Renderer>(true)) {
            ren.enabled = visible;
         }

         foreach (Canvas canvas in placedInstance.GetComponentsInChildren<Canvas>(true)) {
            canvas.enabled = visible;
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         var outline = placedInstance.GetComponent<SpriteOutline>();
         var highlight = placedInstance.GetComponent<PrefabHighlight>();
         var highlightable = placedInstance.GetComponent<IHighlightable>();

         if (outline != null) {
            if (deleting) {
               outline.setVisibility(true);
               outline.setNewColor(MapEditorPrefab.DELETING_HIGHLIGHT_COLOR);
               SpriteRenderer sr = outline.transform.Find("Outline")?.GetComponent<SpriteRenderer>();
               if (sr != null) {
                  sr.color = MapEditorPrefab.DELETING_HIGHLIGHT_COLOR;
               }
               outline.Regenerate();
            } else if (!hovered && !selected) {
               outline.setVisibility(false);
            } else if (hovered) {
               outline.setVisibility(true);
               outline.setNewColor(MapEditorPrefab.HOVERED_HIGHLIGHT_COLOR);
               SpriteRenderer sr = outline.transform.Find("Outline")?.GetComponent<SpriteRenderer>();
               if (sr != null) {
                  sr.color = MapEditorPrefab.HOVERED_HIGHLIGHT_COLOR;
               }
               outline.Regenerate();
            } else if (selected) {
               outline.setVisibility(true);
               outline.setNewColor(MapEditorPrefab.SELECTED_HIGHLIGHT_COLOR);
               outline.color = MapEditorPrefab.SELECTED_HIGHLIGHT_COLOR;
               SpriteRenderer sr = outline.transform.Find("Outline")?.GetComponent<SpriteRenderer>();
               if (sr != null) {
                  sr.color = MapEditorPrefab.SELECTED_HIGHLIGHT_COLOR;
               }
               outline.Regenerate();
            }
         }

         if (highlight != null)
            highlight.setHighlight(hovered, selected, deleting);

         if (highlightable != null)
            highlightable.setHighlight(hovered, selected, deleting);
      }

      public void setData (string key, string value) {
         if (data.ContainsKey(key)) {
            data[key] = value;
         } else {
            data.Add(key, value);
         }
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

      public static SidesInt uniform (int n) {
         return new SidesInt {
            left = n,
            bot = n,
            right = n,
            top = n
         };
      }

      public override string ToString () {
         return $"left = {left}, right = {right}, bot = {bot}, top = {top}";
      }
   }
}
