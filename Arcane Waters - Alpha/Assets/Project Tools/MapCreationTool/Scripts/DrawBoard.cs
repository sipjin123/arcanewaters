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
      private Grid grid;

      private bool pointerHovering = false;
      private Vector3 lastMouseHoverPos = Vector2.zero;
      private Vector3Int lastHoverCellPos = new Vector3Int(0, 0, -1);

      private Preview preview = new Preview();

      private Dictionary<string, Layer> layers;

      private List<PlacedPrefab> placedPrefabs = new List<PlacedPrefab>();

      private Selection currentSelection = new Selection();

      private Vector3? draggingFrom = null;

      private Layer deletingFrom = null;

      private Layer tileSelectionLayer;

      private void Awake () {
         instance = this;

         eventCanvas = GetComponentInChildren<EventTrigger>();
         grid = GetComponentInChildren<Grid>();
         cam = GetComponentInChildren<Camera>();

         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerDown, pointerDown);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.Drag, drag);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerEnter, pointerEnter);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerExit, pointerExit);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.BeginDrag, pointerBeginDrag);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.EndDrag, pointerEndDrag);
         Utilities.addPointerListener(eventCanvas, EventTriggerType.PointerClick, pointerClick);

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
         Tools.ToolChanged += toolChanged;
         Tools.EditorTypeChanged += editorTypeChanged;
         Tools.BoardSizeChanged += boardSizeChanged;
      }

      private void OnDisable () {
         Tools.AnythingChanged -= toolsAnythingChanged;
         Tools.ToolChanged -= toolChanged;
         Tools.EditorTypeChanged -= editorTypeChanged;
         Tools.BoardSizeChanged -= boardSizeChanged;
      }

      private void Update () {
         if (pointerHovering && Input.mouseScrollDelta.y != 0)
            pointerScroll(Input.mouseScrollDelta.y);

         if (pointerHovering && lastMouseHoverPos != Input.mousePosition) {
            pointerMove(Input.mousePosition);
            lastMouseHoverPos = Input.mousePosition;
         }

         //Generates a random mountainy area
         //if (Input.GetKeyDown(KeyCode.P)) {
         //   for (int i = 0; i < 1000; i++) {
         //      changeBoard(BoardChange.calculateSeaMountainChanges(
         //          Tools.tileGroup as SeaMountainGroup,
         //          new Vector3(Random.Range(-126, 126), Random.Range(-126, 126), 0),
         //          layers[PaletteData.MountainLayer].defaultLayer));
         //   }
         //}
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

      private void toolChanged (ToolType from, ToolType to) {

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

      private void changeBoard (BoardChange change, bool registerUndo = true, bool isDrag = false) {
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
                  pdd.restructureCustomFields();

                  if (pdd != null) {
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

         if (registerUndo && !undoChange.empty)
            Undo.register(
                performUndoRedo,
                new BoardUndoRedoData { change = undoChange, cascade = isDrag },
                new BoardUndoRedoData { change = change, cascade = isDrag });

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
            listener.dataFieldChanged(key, value);

         PrefabDataChanged(prefab);
      }

      private GameObject instantiatePlacedPrefab (GameObject original, Vector3 position) {
         var instance = Instantiate(original, position, Quaternion.identity, prefabLayer);
         instance.name = original.name;

         if (instance.GetComponent<ZSnap>())
            instance.GetComponent<ZSnap>().roundoutPosition();

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
                pointerWorldPosition,
                deletingFrom));
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
               } else if ((Tools.tileGroup.type == TileGroupType.Prefab || Tools.tileGroup.type == TileGroupType.TreePrefab) && !excludePrefabs) {
                  Vector3 position = getPrefabPosition(Tools.tileGroup as PrefabGroup, Tools.selectedPrefab, pointerWorldPosition);
                  result.add(BoardChange.calculatePrefabChange(Tools.selectedPrefab, position));
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
               } else if (Tools.tileGroup.type == TileGroupType.SeaMountain) {
                  var group = Tools.tileGroup as SeaMountainGroup;
                  result.add(BoardChange.calculateSeaMountainChanges(group, pointerWorldPosition, layers[PaletteData.MountainLayer].subLayers[Tools.mountainLayer]));
               } else if (Tools.tileGroup.type == TileGroupType.River) {
                  var group = Tools.tileGroup as RiverGroup;
                  result.add(BoardChange.calculateRiverChanges(group, pointerWorldPosition, layers[group.layer].subLayers[group.subLayer]));
               } else if (Tools.tileGroup.type == TileGroupType.InteriorWall) {
                  var group = Tools.tileGroup as InteriorWallGroup;
                  result.add(BoardChange.calculateInteriorWallChanges(group, pointerWorldPosition, layers[group.layer].subLayers[group.subLayer]));
               } else if (Tools.tileGroup.type == TileGroupType.Rect && draggingFrom != null) {
                  var group = Tools.tileGroup as RectTileGroup;
                  Vector3Int to = worldToCell(pointerWorldPosition);
                  result.add(BoardChange.calculateRectChanges(group, layers[group.layer].subLayers[group.subLayer], worldToCell(draggingFrom.Value), to));
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
         } else if (Tools.toolType == ToolType.Move && draggingFrom != null) {
            if (!currentSelection.empty) {
               result.add(BoardChange.calculateMoveChange(draggingFrom.Value, pointerWorldPosition, getLayersEnumerator(), placedPrefabs, currentSelection));
            }
         }
         return result;
      }

      private BoardChange getSelectionBoardChange (Vector3 p1, Vector3 p2, bool add, bool rem, bool selectTiles, bool selectPrefabs, bool topMostPref = false) {
         BoardChange result = new BoardChange();

         if (selectTiles) {
            Vector3Int cell1 = worldToCell(p1);
            Vector3Int cell2 = worldToCell(p2);
            if (add) {
               foreach (Vector3Int tile in BoardChange.getRectTilePositions(cell1, cell2)) {
                  if (!currentSelection.contains(tile)) {
                     result.selectionToAdd.add(tile);
                  }
               }
            } else if (rem) {
               foreach (Vector3Int tile in BoardChange.getRectTilePositions(cell1, cell2)) {
                  if (currentSelection.contains(tile)) {
                     result.selectionToRemove.add(tile);
                  }
               }
            } else {
               HashSet<Vector3Int> rect = new HashSet<Vector3Int>(BoardChange.getRectTilePositions(cell1, cell2));
               foreach (Vector3Int tile in currentSelection.tiles) {
                  if (!rect.Contains(tile)) {
                     result.selectionToRemove.add(tile);
                  }
               }
               foreach (Vector3Int tile in rect) {
                  if (!currentSelection.contains(tile)) {
                     result.selectionToAdd.add(tile);
                  }
               }

               if (!selectPrefabs) {
                  result.selectionToRemove.add(currentSelection.prefabs);
               }
            }
         }

         if (selectPrefabs) {
            List<PlacedPrefab> prefs = null;
            if (topMostPref) {
               prefs = new List<PlacedPrefab>();
               PlacedPrefab hovered = getHoveredPrefab(p2);
               if (hovered != null) {
                  prefs.Add(hovered);
               }
            } else {
               prefs = prefabsBetween(p1, p2);
            }
            if (add) {
               foreach (PlacedPrefab pref in prefs) {
                  if (!currentSelection.contains(pref)) {
                     result.selectionToAdd.add(pref);
                  }
               }
            } else if (rem) {
               foreach (PlacedPrefab pref in prefs) {
                  if (currentSelection.contains(pref)) {
                     result.selectionToRemove.add(pref);
                  }
               }
            } else {
               HashSet<PlacedPrefab> rect = new HashSet<PlacedPrefab>(prefs);
               foreach (PlacedPrefab pref in currentSelection.prefabs) {
                  if (!rect.Contains(pref)) {
                     result.selectionToRemove.add(pref);
                  }
               }
               foreach (PlacedPrefab pref in rect) {
                  if (!currentSelection.contains(pref)) {
                     result.selectionToAdd.add(pref);
                  }
               }

               if (!selectTiles) {
                  result.selectionToRemove.add(currentSelection.tiles);
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
            if (!currentSelection.contains(pp)) {
               pp.setHighlight(false, false, false);
            } else {
               pp.setHighlight(false, true, false);
            }
            pp.placedInstance.GetComponent<MapEditorPrefab>()?.setHovered(false);
         }

         tileSelectionLayer.clearAllPreviewTiles();
      }

      private void updatePreview (Vector3 pointerScreenPos, bool isDrag) {
         ensurePreviewCleared();
         Vector2 worldPos = cam.ScreenToWorldPoint(pointerScreenPos);
         BoardChange change = Tools.toolType == ToolType.Brush
             ? getPotentialBoardChange(worldPos)
             : new BoardChange();

         if (Tools.toolType == ToolType.Selection) {
            bool selectTiles = Tools.selectionTarget == SelectionTarget.Both || Tools.selectionTarget == SelectionTarget.Tiles;
            bool selectPrefabs = Tools.selectionTarget == SelectionTarget.Both || Tools.selectionTarget == SelectionTarget.Prefabs;

            bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
            bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);

            if (!add && !rem)
               add = true;

            if (draggingFrom != null) {
               change = getSelectionBoardChange(draggingFrom.Value, worldPos, add, rem, selectTiles, selectPrefabs);
            } else {
               change = getSelectionBoardChange(worldPos, worldPos, add, rem, false, selectPrefabs);
            }
         } else if (Tools.toolType == ToolType.Move) {
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

         foreach (Vector3Int tile in change.selectionToAdd.tiles) {
            tileSelectionLayer.setPreviewTile(tile, hoveredTileHighlight);
         }

         foreach (Vector3Int tile in change.selectionToRemove.tiles) {
            tileSelectionLayer.setPreviewTile(tile, hoveredTileHighlight);
         }

         foreach (PlacedPrefab prefab in change.selectionToAdd.prefabs.Union(change.selectionToRemove.prefabs)) {
            prefab.setHighlight(true, false, false);
            prefab.placedInstance.GetComponent<MapEditorPrefab>()?.setHovered(true);
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

      private Vector3 getPrefabPosition (PrefabGroup group, GameObject prefabToPlace, Vector3 targetPosition) {
         if (Tools.snapToGrid) {
            targetPosition = cellToWorldCenter(worldToCell(targetPosition));
            targetPosition += new Vector3(
                group.brushSize.x % 2 == 0 ? -0.5f : 0,
                group.brushSize.y % 2 == 0 ? -0.5f : 0,
                0);
         }

         PrefabCenterOffset prefOffset = prefabToPlace.GetComponent<PrefabCenterOffset>();
         if (prefOffset != null) {
            targetPosition -= Vector3.up * prefOffset.offset;
         }

         return targetPosition;
      }

      private PlacedPrefab getHoveredPrefab (Vector3? mousePostion = null) {
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

      private List<PlacedPrefab> prefabsBetween (Vector3 p1, Vector3 p2) {
         List<PlacedPrefab> result = new List<PlacedPrefab>();
         Bounds bounds = new Bounds();
         bounds.min = new Vector3(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), -1000f);
         bounds.max = new Vector3(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y), 1000f);

         foreach (var pp in placedPrefabs) {
            var col = pp.placedInstance.GetComponent<Collider2D>();
            if (col != null && col.bounds.Intersects(bounds)) {
               result.Add(pp);
            }
         }

         return result;
      }

      /// <summary>
      /// Returns the top-most layer, that has a tile placed at a given position
      /// </summary>
      /// <param name="position"></param>
      /// <returns></returns>
      private Layer getTopLayerWithTile (Vector3Int position) {
         foreach (Layer layer in getLayersEnumerator().Reverse()) {
            if (layer.hasTile(position)) {
               return layer;
            }
         }

         return null;
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
            if (Tools.toolType == ToolType.Brush || Tools.toolType == ToolType.Eraser || Tools.toolType == ToolType.Fill || Tools.toolType == ToolType.Selection) {
               if (Tools.toolType == ToolType.Eraser) {
                  // Ensure following deletion on drag event is only at the current layer
                  if (getHoveredPrefab() != null) {
                     deletingFrom = null;
                  } else {
                     deletingFrom = getTopLayerWithTile(worldToCell(data.pointerCurrentRaycast.worldPosition));
                  }
               }
               changeBoard(getPotentialBoardChange(data.pointerCurrentRaycast.worldPosition));
            }
         }
         updatePreview(data.position, false);
      }

      public void pointerBeginDrag (PointerEventData data) {
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
            if (Tools.toolType == ToolType.Selection) {
               bool selectTiles = Tools.selectionTarget == SelectionTarget.Both || Tools.selectionTarget == SelectionTarget.Tiles;
               bool selectPrefabs = Tools.selectionTarget == SelectionTarget.Both || Tools.selectionTarget == SelectionTarget.Prefabs;

               bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
               bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);
               changeBoard(getSelectionBoardChange(
                  draggingFrom.Value,
                  data.pointerCurrentRaycast.worldPosition,
                  add, rem, selectTiles, selectPrefabs));
            } else {
               changeBoard(getPotentialBoardChange(data.pointerCurrentRaycast.worldPosition, true));
            }
         }

         draggingFrom = null;
      }

      private void drag (PointerEventData data) {
         if (data.button == PointerEventData.InputButton.Left) {
            if ((Tools.tileGroup == null || Tools.tileGroup.type != TileGroupType.Rect) && Tools.toolType != ToolType.Move) {
               changeBoard(getPotentialBoardChange(data.pointerCurrentRaycast.worldPosition, true), isDrag: true);
            }
         } else {
            Vector3 curPos = cam.ScreenToWorldPoint(data.position);
            Vector3 prevPos = cam.ScreenToWorldPoint(data.position + data.delta);

            MainCamera.pan(curPos - prevPos);
         }
         updatePreview(data.position, true);
      }

      private void pointerClick (PointerEventData data) {
         if (Tools.toolType == ToolType.Selection && draggingFrom == null && data.button == PointerEventData.InputButton.Left) {
            Vector3 pos = data.pointerPressRaycast.worldPosition;
            bool selectPrefabs = Tools.selectionTarget == SelectionTarget.Both || Tools.selectionTarget == SelectionTarget.Prefabs;
            bool add = Settings.keybindings.getAction(Keybindings.Command.SelectionAdd);
            bool rem = Settings.keybindings.getAction(Keybindings.Command.SelectionRemove);
            changeBoard(getSelectionBoardChange(pos, pos, add, rem, false, selectPrefabs, true));
         }

         updatePreview(data.position, false);
      }

      private void pointerMove (Vector3 screenPos) {
         updatePreview(screenPos, draggingFrom != null);
      }

      private void pointerScroll (float scroll) {
         MainCamera.zoom(-scroll * Time.deltaTime);
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

      public Preview () {

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
