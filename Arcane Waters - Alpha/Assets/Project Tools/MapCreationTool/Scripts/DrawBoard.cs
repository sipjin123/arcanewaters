using System.Collections.Generic;
using System.Linq;
using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.InputSystem;
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

      // The object showing the size of the eraser based on scale
      [SerializeField]
      private GameObject eraserOutline = default;

      private Preview preview = new Preview();

      public Dictionary<string, Layer> layers { get; private set; }

      private List<PlacedPrefab> placedPrefabs = new List<PlacedPrefab>();

      public Selection currentSelection { get; private set; }

      private Layer tileSelectionLayer;

      private void Awake () {
         instance = this;

         currentSelection = new Selection();
         tileSelectionLayer = new Layer(tileSelectionTilemap);
      }

      private void Start () {
         setUpLayers(Tools.editorType);
         setBoardSize(Tools.boardSize);
         changeLoadedVersion(null);
      }

      private void OnEnable () {
         Tools.AnythingChanged += toolsAnythingChanged;
         Tools.EditorTypeChanged += editorTypeChanged;
         Tools.BoardSizeChanged += boardSizeChanged;

         DrawBoardEvents.DragSecondary += dragSecondary;
         DrawBoardEvents.PointerScroll += pointerScroll;
      }

      private void OnDisable () {
         Tools.AnythingChanged -= toolsAnythingChanged;
         Tools.EditorTypeChanged -= editorTypeChanged;
         Tools.BoardSizeChanged -= boardSizeChanged;

         DrawBoardEvents.DragSecondary -= dragSecondary;
         DrawBoardEvents.PointerScroll -= pointerScroll;
      }

      private void Update () {
         updateBrushOutline();
      }

      private void boardSizeChanged (Vector2Int from, Vector2Int to) {
         // Commented out functions creates new board; Instead - just change size

         //changeBoard(BoardChange.calculateClearAllChange(layers, placedPrefabs, currentSelection));
         setBoardSize(to);
         //changeLoadedVersion(null);
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
         if (version == null) {
            PlacedPrefab.nextPrefabId = 0;
         }
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

         PlacedPrefab.nextPrefabId = data.nextPrefabId;

         changeBoard(BoardChange.calculateDeserializedDataChange(data, layers, placedPrefabs, currentSelection));

         changeLoadedVersion(mapVersion);
      }

      public void changeBoard (List<TileChange> tilesChanges) {
         changeBoard(new BoardChange() { tileChanges = tilesChanges });
      }

      public void changeBoard (BoardChange change, bool registerUndo = true) {
         if (change == null) return;

         // Give prefabs new ids if they do not have them already
         foreach (PrefabChange prefChange in change.prefabChanges) {
            if (prefChange.prefabToPlace != null) {
               if (prefChange.dataToSet == null) {
                  prefChange.dataToSet = new Dictionary<string, string>();
               }
               if (!prefChange.dataToSet.ContainsKey(DataField.PLACED_PREFAB_ID)) {
                  prefChange.dataToSet[DataField.PLACED_PREFAB_ID] = (PlacedPrefab.nextPrefabId++).ToString();
               }
            }
         }

         BoardChange undoChange = new BoardChange() { isSelectionChange = change.isSelectionChange };

         foreach (var group in change.tileChanges.GroupBy(tc => tc.layer)) {
            Vector3Int[] positions = group.Select(tc => tc.position).ToArray();
            TileBase[] tiles = group.Select(tc => tc.tile).ToArray();

            undoChange.add(group.Key.calculateUndoChange(positions, tiles));
            group.Key.setTiles(positions, tiles);
         }

         foreach (var pc in change.prefabChanges) {
            if (pc.prefabToPlace != null) {
               var pref = instantiatePlacedPrefab(pc, false);

               undoChange.prefabChanges.Add(new PrefabChange {
                  prefabToDestroy = pc.prefabToPlace,
                  positionToDestroy = new Vector3(pref.transform.position.x, pref.transform.position.y, 0)
               });

               var newPref = new PlacedPrefab {
                  placedInstance = pref,
                  original = pc.prefabToPlace
               };

               if (pc.select) {
                  currentSelection.add(newPref);
               }

               placedPrefabs.Add(newPref);

               var pdd = pref.GetComponent<PrefabDataDefinition>();
               if (pdd != null) {
                  pdd.restructureCustomFields();
                  foreach (var kv in pdd.dataFields)
                     setPrefabData(newPref, kv.name, kv.defaultValue, false);
                  foreach (var kv in pdd.selectDataFields)
                     setPrefabData(newPref, kv.name, kv.options[kv.defaultOption].value, false);
               }

               if (pc.dataToSet != null) {
                  foreach (var d in pc.dataToSet) {
                     setPrefabData(newPref, d.Key, d.Value, false);
                  }
               }
            }

            if (pc.prefabToDestroy != null) {
               var prefabToDestroy = placedPrefabs.FirstOrDefault(p =>
                   p.original == pc.prefabToDestroy &&
                   (Vector2) p.placedInstance.transform.position == (Vector2) pc.positionToDestroy);

               if (prefabToDestroy != null) {
                  undoChange.prefabChanges.Add(new PrefabChange {
                     prefabToPlace = prefabToDestroy.original,
                     positionToPlace = prefabToDestroy.placedInstance.transform.position,
                     dataToSet = prefabToDestroy.data.Clone(),
                     select = currentSelection.contains(prefabToDestroy)
                  });

                  currentSelection.remove(prefabToDestroy);

                  Destroy(prefabToDestroy.placedInstance);
                  placedPrefabs.Remove(prefabToDestroy);
               }
            }
         }

         if (!change.isSelectionChange) {
            currentSelection.remove(change.selectionToRemove.prefabs);
            currentSelection.add(change.selectionToAdd.prefabs);

            undoChange.selectionToRemove.add(change.selectionToAdd.prefabs);
            undoChange.selectionToAdd.add(change.selectionToRemove.prefabs);
         }

         currentSelection.remove(change.selectionToRemove.tiles);
         currentSelection.add(change.selectionToAdd.tiles);

         undoChange.selectionToRemove.add(change.selectionToAdd.tiles);
         undoChange.selectionToAdd.add(change.selectionToRemove.tiles);


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
         if (listener != null) {
            listener.dataFieldChanged(new DataField { k = key, v = value });
            Tools.setDefaultData(prefab.original, key, value);
         }

         PrefabDataChanged(prefab);
      }

      private GameObject instantiatePlacedPrefab (PrefabChange fromChange, bool preview) {
         var instance = Instantiate(fromChange.prefabToPlace, fromChange.positionToPlace, Quaternion.identity, prefabLayer);
         instance.name = fromChange.prefabToPlace.name;

         ZSnap snap = instance.GetComponent<ZSnap>();
         if (snap != null) {
            snap.initialize();
            snap.snapZ();
         }

         foreach (IBiomable biomable in instance.GetComponentsInChildren<IBiomable>()) {
            biomable.setBiome(Tools.biome);
         }

         if (instance.GetComponent<MapEditorPrefab>()) {
            if (preview) {
               instance.GetComponent<MapEditorPrefab>().createdForPreview();
            } else {
               instance.GetComponent<MapEditorPrefab>().placedInEditor();
            }
         }

         SpriteOutline so = instance.GetComponent<SpriteOutline>();
         if (so != null) {
            so.setNewColor(new Color(0, 0, 0, 0));
         }

         return instance;
      }

      private BoardChange getBiomeChange (PaletteData from, PaletteData to) {
         BoardChange result = new BoardChange();

         //-------------------------------------
         //Handle tiles
         foreach (var layer in nonEmptySublayers()) {
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

      public IEnumerable<Layer> nonEmptySublayers () {
         return layers.Values.SelectMany(v => v.subLayers).Where(l => l.tileCount > 0);
      }

      public string formSerializedData () {
         return Serializer.serialize(layers, placedPrefabs, Tools.biome, Tools.editorType, Tools.boardSize, false);
      }

      public string formExportData () {
         return Serializer.serializeExport(layers, placedPrefabs, Tools.biome, Tools.editorType, config, palette.formCollisionDictionary(), origin, size);
      }

      public List<MapSpawn> formSpawnList (int? mapId, int mapVersion) {
         List<MapSpawn> formList = GetComponentsInChildren<SpawnMapEditor> ().Select(s => new MapSpawn {
            mapId = mapId ?? -1,
            mapVersion = mapVersion,
            name = s.spawnName,
            posX = s.transform.position.x * 0.16f,
            posY = s.transform.position.y * 0.16f,
            spawnId = s.spawnId,
            facingDirection = (int) s.arriveFacing
         }
          ).ToList();
         return formList;
      }

      public static T[] getPrefabComponents<T> () where T : MonoBehaviour {
         return instance.prefabLayer.GetComponentsInChildren<T>();
      }

      public void setTilesModifyPreview (BoardChange change) {
         foreach (Layer layer in getLayersEnumerator())
            layer.clearAllPreviewTiles();

         tileSelectionLayer.clearAllPreviewTiles();

         if (change == null) {
            return;
         }

         foreach (var tc in change.tileChanges) {
            if (tc.tile == null)
               tc.layer.setPreviewTile(tc.position, transparentTile);
            else
               tc.layer.setPreviewTile(tc.position, tc.tile);
         }

         foreach (Vector3Int tile in change.selectionToAdd.tiles) {
            tileSelectionLayer.setPreviewTile(tile, hoveredTileHighlight);
         }

         foreach (Vector3Int tile in change.selectionToRemove.tiles) {
            tileSelectionLayer.setPreviewTile(tile, hoveredTileHighlight);
         }
      }

      public void setPrefabsModifyPreview (BoardChange change) {
         foreach (PlacedPrefab prefab in preview.prefabs) {
            Destroy(prefab.placedInstance);
         }

         preview.prefabs.Clear();

         foreach (PlacedPrefab prefab in placedPrefabs) {
            prefab.setVisible(true);

            if (!currentSelection.contains(prefab)) {
               prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setHovered(false);
               prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setSelected(false);
               prefab.setHighlight(false, false, false);
            } else {
               prefab.setHighlight(false, true, false);
               prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setSelected(true);
            }
         }

         if (change == null) {
            return;
         }

         foreach (PrefabChange prefChange in change.prefabChanges) {
            if (prefChange.prefabToPlace != null) {
               PlacedPrefab newPref = new PlacedPrefab {
                  placedInstance = instantiatePlacedPrefab(prefChange, true),
                  original = prefChange.prefabToPlace
               };

               preview.prefabs.Add(newPref);

               foreach (SpriteSwapper swapper in newPref.placedInstance.GetComponentsInChildren<SpriteSwapper>())
                  swapper.Update();

               // Set the data for the preview prefab
               var pdd = newPref.placedInstance.GetComponent<PrefabDataDefinition>();
               IPrefabDataListener listener = newPref.placedInstance.GetComponent<IPrefabDataListener>();

               if (pdd != null) {
                  pdd.restructureCustomFields();
                  if (listener != null) {
                     foreach (var kv in pdd.dataFields)
                        listener.dataFieldChanged(new DataField { k = kv.name, v = kv.defaultValue });
                     foreach (var kv in pdd.selectDataFields)
                        listener.dataFieldChanged(new DataField { k = kv.name, v = kv.options[kv.defaultOption].value });
                  }
               }

               if (prefChange.dataToSet != null && listener != null) {
                  foreach (var d in prefChange.dataToSet) {
                     listener.dataFieldChanged(new DataField { k = d.Key, v = d.Value });
                  }
               }
            } else if (prefChange.prefabToDestroy != null) {
               PlacedPrefab target = placedPrefabs.FirstOrDefault(pp => pp.isOriginalAtPosition(prefChange.prefabToDestroy, prefChange.positionToDestroy));
               if (target != null) {
                  target.setVisible(false);
               }
            }
         }

         foreach (PlacedPrefab prefab in change.selectionToAdd.prefabs.Union(change.selectionToRemove.prefabs)) {
            prefab.setHighlight(true, false, false);
            prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setHovered(true);
         }

         recalculateInheritedSorting();
      }

      public void ensurePreviewCleared () {
         setPrefabsModifyPreview(null);
         setTilesModifyPreview(null);
      }

      private void updateBrushOutline () {
         brushOutline.enabled = false;
         if (Tools.toolType == ToolType.Eraser) {
            eraserOutline.SetActive(true);
            eraserOutline.transform.localScale = new Vector3(EraserTool.size, EraserTool.size, EraserTool.size);
            placedPrefabs.ForEach(p => p.setHighlight(false, false, false));
            PlacedPrefab hoveredPrefab = getHoveredPrefab();
            if (hoveredPrefab != null) {
               hoveredPrefab.setHighlight(false, false, true);
            } else {
               brushOutline.enabled = true;
               brushOutline.transform.position = cellToWorldCenter(worldToCell(MainCamera.stwp(MouseUtils.mousePosition)));
               brushOutline.size = Vector2.one;
               brushOutline.color = Color.red;
            }
         } else if (Tools.toolType == ToolType.Selection) {
            eraserOutline.SetActive(false);
            brushOutline.enabled = DrawBoardEvents.isDragging && !SelectionTool.cancelled;

            if (!brushOutline.enabled)
               return;

            Vector3 wp = MainCamera.stwp(MouseUtils.mousePosition);
            Bounds bounds = new Bounds {
               min = new Vector3(Mathf.Min(wp.x, DrawBoardEvents.draggingFrom.Value.x), Mathf.Min(wp.y, DrawBoardEvents.draggingFrom.Value.y), 0),
               max = new Vector3(Mathf.Max(wp.x, DrawBoardEvents.draggingFrom.Value.x), Mathf.Max(wp.y, DrawBoardEvents.draggingFrom.Value.y), 0)
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
            eraserOutline.SetActive(false);
            brushOutline.enabled = (Tools.toolType == ToolType.Brush && Tools.tileGroup != null && !(Tools.tileGroup is PrefabGroup));

            if (!brushOutline.enabled)
               return;

            brushOutline.transform.position = cellToWorldCenter(worldToCell(MainCamera.stwp(MouseUtils.mousePosition)));

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

            if (Tools.toolType == ToolType.Brush && BrushTool.size > 1) {
               brushOutline.size = new Vector2(BrushTool.size, BrushTool.size);
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
         Vector2 pointerPos = mousePostion ?? MainCamera.stwp(MouseUtils.mousePosition);
         PlacedPrefab target = null;
         float minZ = float.MaxValue;

         foreach (var pp in placedPrefabs) {
            foreach (Collider2D col in pp.placedInstance.GetComponents<Collider2D>()) {
               if (col != null && col.OverlapPoint(pointerPos)) {
                  if (col.transform.position.z < minZ) {
                     minZ = col.transform.position.z;
                     target = pp;
                     break;
                  }
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

      //private void OnDrawGizmos () {
      //   foreach (PlacedPrefab prefab in placedPrefabs) {
      //      UnityEditor.Handles.Label(prefab.placedInstance.transform.position, prefab.getData("id"));
      //   }
      //}

      public static Bounds getPrefabBounds () {
         Bounds result = new Bounds(Vector3.zero, (Vector3Int) size);
         result.Expand(6f);
         return result;
      }

      public static Vector2 calculatePrefabPosition (PrefabGroup prefGroup, GameObject prefab, Vector2 inputPosition) {
         Vector2 result = inputPosition;

         PrefabCenterOffset prefOffset = prefab.GetComponent<PrefabCenterOffset>();
         PrefabDataDefinition prefData = prefab.GetComponent<PrefabDataDefinition>();

         Vector2 step = Tools.snapToGrid ? Vector2.one : prefData.positionStep;

         if (step.x > 0) {
            result.x = Mathf.FloorToInt(result.x / step.x);

            if (prefGroup.brushSize.x % 2 != 0) result.x += 0.5f;
            if ((int) (1f / step.x) % 2 == 0) result.x += 0.5f;

            result.x *= step.x;
         }

         if (step.y > 0) {
            result.y = Mathf.FloorToInt(result.y / step.y);

            if (prefGroup.brushSize.y % 2 != 0) result.y += 0.5f;
            if ((int) (1f / step.y) % 2 == 0) result.y += 0.5f;

            result.y *= step.y;
         }

         if (prefOffset != null) {
            result -= Vector2.up * prefOffset.offset;
         }

         return result;
      }

      public static BoundsInt getBoardBoundsInt () {
         // We decrease the size by 1 because we want the 'max' vector to be inclusive
         return new BoundsInt(origin.x, origin.y, 0, size.x - 1, size.y - 1, 0);
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
      private void dragSecondary (Vector3 delta) {
         MainCamera.pan(-delta);
      }

      public void pointerScroll (Vector3 at, float scroll) {
         MainCamera.zoom(-scroll * Time.deltaTime);
      }
      #endregion
   }

   public class Preview
   {
      public GameObject prefabPreviewInstance { get; set; }
      public GameObject targetPrefab { get; set; }

      public List<PlacedPrefab> prefabs { get; set; }

      public Preview () {
         prefabs = new List<PlacedPrefab>();
      }
   }

   public struct SidesInt
   {
      public int left;
      public int right;
      public int top;
      public int bot;

      public int min () {
         return Mathf.Min(left, right, top, bot);
      }

      public int max () {
         return Mathf.Max(left, right, top, bot);
      }

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
