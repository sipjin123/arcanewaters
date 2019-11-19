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
        public static Vector2Int Size { get; private set; }
        public static Vector2Int Origin { get; private set; }

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

        
        private void Awake()
        {
            Size = new Vector2Int(2000, 2000);
            Origin = new Vector2Int(-1000, -1000);

            eventCanvas = GetComponentInChildren<EventTrigger>();
            grid = GetComponentInChildren<Grid>();
            cam = GetComponentInChildren<Camera>();

            RecalculateCamBounds();
            ClampCamPos();

            SetUpLayers();

            Utilities.AddPointerListener(eventCanvas, EventTriggerType.PointerDown, PointerDown);
            Utilities.AddPointerListener(eventCanvas, EventTriggerType.Drag, Drag);
            Utilities.AddPointerListener(eventCanvas, EventTriggerType.PointerEnter, PointerEnter);
            Utilities.AddPointerListener(eventCanvas, EventTriggerType.PointerExit, PointerExit);
        }

        private void Start()
        {
            UpdateBrushOutline();
        }

        private void OnEnable()
        {
            MainCamera.SizeChanged += MainCamSizeChanged;
            Tools.AnythingChanged += ToolsAnythingChanged;
        }

        private void OnDisable()
        {
            MainCamera.SizeChanged -= MainCamSizeChanged;
            Tools.AnythingChanged -= ToolsAnythingChanged;
        }

        private void Update()
        {
            if (pointerHovering && Input.mouseScrollDelta.y != 0)
                PointerScroll(Input.mouseScrollDelta.y);

            if (pointerHovering && lastMouseHoverPos != Input.mousePosition)
            {
                PointerMove(Input.mousePosition);
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

        private void MainCamSizeChanged(Vector2Int newSize)
        {
            RecalculateCamBounds();
            ClampCamPos();
        }

        private void ToolsAnythingChanged()
        {
            UpdateBrushOutline();
        }

        private void RecalculateCamBounds()
        {
            var canvasRectT = eventCanvas.GetComponent<RectTransform>();
            camBounds = new Rect(
                new Vector2(-canvasRectT.sizeDelta.x * 0.5f, -canvasRectT.sizeDelta.y * 0.5f),
                canvasRectT.sizeDelta);
        }

        private void SetUpLayers()
        {
            layers = new Dictionary<int, Layer>();
            foreach (LayerName ln in layerDefinitions)
            {
                Tilemap[] subLayers = new Tilemap[10];
                for (int i = 0; i < 10; i++)
                {
                    Tilemap layer = Instantiate(layerPref, Vector3.zero, Quaternion.identity, layerContainer);
                    layer.gameObject.name = ln.name + " " + i;
                    layer.transform.localPosition = new Vector3(0, 0, LayerToZ(ln.layer, i));
                    subLayers[i] = layer;
                }
                layers.Add(ln.layer, new Layer(subLayers));
            }

            if (!layers.ContainsKey(0))
            {
                Tilemap layer = Instantiate(layerPref, Vector3.zero, Quaternion.identity, layerContainer);
                layer.gameObject.name = "default";
                layer.transform.localPosition = new Vector3(0, 0, LayerToZ(0));
                layers.Add(0, new Layer(layer));
            }
        }

        private float LayerToZ(int layer, int subLayer = 0)
        {
            return layer * -0.01f + subLayer * -0.001f;
        }

        private void ClampCamPos()
        {
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

        public void PerformUndoRedo(UndoRedoData undoRedoData)
        {
            BoardUndoRedoData data = undoRedoData as BoardUndoRedoData;
            ChangeBoard(data.Change, false);
        }

        public void ChangeBiome(PaletteData from, PaletteData to)
        {
            if (from != to)
                ChangeBoard(GetBiomeChange(from, to), false);
        }

        public void ClearAll()
        {
            ChangeBoard(BoardChange.CalculateClearAllChange(layers, placedPrefabs));
        }

        public void ApplyDeserializedData(DeserializedProject data)
        {
            ChangeBoard(BoardChange.CalculateDeserializedDataChange(data, layers, placedPrefabs));
        }

        private void ChangeBoard(BoardChange change, bool registerUndo = true)
        {
            BoardChange undoChange = new BoardChange();

            foreach(var group in change.TileChanges.GroupBy(tc => tc.Layer))
            {
                Vector3Int[] positions = group.Select(tc => tc.Position).ToArray();
                TileBase[] tiles = group.Select(tc => tc.Tile).ToArray();

                undoChange.Add(group.Key.CalculateUndoChange(positions, tiles));
                group.Key.SetTiles(positions, tiles);
            }

            foreach (var pc in change.PrefabChanges)
            {
                if (pc.PrefabToPlace != null)
                {
                    undoChange.PrefabChanges.Add(new PrefabChange
                    {
                        PrefabToDestroy = pc.PrefabToPlace,
                        PositionToDestroy = pc.PositionToPlace
                    });

                    placedPrefabs.Add(new PlacedPrefab
                    {
                        PlacedInstance = Instantiate(pc.PrefabToPlace, pc.PositionToPlace, Quaternion.identity, prefabLayer),
                        Original = pc.PrefabToPlace
                    });
                }

                if (pc.PrefabToDestroy != null)
                {
                    var prefabToDestroy = placedPrefabs.FirstOrDefault(p =>
                        p.Original == pc.PrefabToDestroy &&
                        (Vector2)p.PlacedInstance.transform.position == (Vector2)pc.PositionToDestroy);

                    if (prefabToDestroy != null)
                    {
                        undoChange.PrefabChanges.Add(new PrefabChange
                        {
                            PrefabToPlace = prefabToDestroy.Original,
                            PositionToPlace = prefabToDestroy.PlacedInstance.transform.position
                        });

                        Destroy(prefabToDestroy.PlacedInstance);
                        placedPrefabs.Remove(prefabToDestroy);
                    }
                }
            }

            if (registerUndo && !undoChange.Empty)
                Undo.Register(
                    PerformUndoRedo,
                    new BoardUndoRedoData { Change = undoChange },
                    new BoardUndoRedoData { Change = change });
        }

        private BoardChange GetBiomeChange(PaletteData from, PaletteData to)
        {
            EnsurePreviewCleared();
            BoardChange result = new BoardChange();

            //-------------------------------------
            //Handle tiles
            foreach (var layer in GetLayersEnumerator())
            {
                for (int i = layer.Origin.x; i < layer.Size.x; i++)
                {
                    for (int j = layer.Origin.y; j < layer.Size.y; j++)
                    {
                        TileBase tile = layer.GetTile(new Vector3Int(i, j, 0));
                        if (tile != null && tile != transparentTile)
                        {
                            Vector2Int index = from.IndexOf(tile);
                            if (index.x == -1)
                                throw new System.Exception("Unrecognized tile!");

                            result.TileChanges.Add(new TileChange(to.GetTile(index.x, index.y)?.Tile, new Vector3Int(i, j, 0), layer));
                        }
                    }
                }
            }
            //-------------------------------------
            //Handle prefabs
            foreach (var pref in placedPrefabs)
            {
                for (int i = 0; i < from.PrefabGroups.Count; i++)
                {
                    if (from.PrefabGroups[i].RefPref == pref.Original)
                    {
                        result.PrefabChanges.Add(new PrefabChange
                        {
                            PositionToPlace = pref.PlacedInstance.transform.position,
                            PrefabToPlace = to.PrefabGroups[i].RefPref,
                            PrefabToDestroy = pref.Original,
                            PositionToDestroy = pref.PlacedInstance.transform.position
                        });
                        break;
                    }
                    else
                    {
                        var treeGroup = from.PrefabGroups[i] as TreePrefabGroup;
                        if (treeGroup != null && treeGroup.BurrowedPref == pref.Original)
                        {
                            result.PrefabChanges.Add(new PrefabChange
                            {
                                PositionToPlace = pref.PlacedInstance.transform.position,
                                PrefabToPlace = (to.PrefabGroups[i] as TreePrefabGroup).BurrowedPref,
                                PrefabToDestroy = pref.Original,
                                PositionToDestroy = pref.PlacedInstance.transform.position
                            });
                            break;
                        }
                    }
                    if (i == from.PrefabGroups.Count - 1)
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
        private BoardChange GetPotentialBoardChange(Vector3 pointerWorldPosition, bool excludePrefabs = false)
        {
            BoardChange result = new BoardChange();

            if (Tools.ToolType == ToolType.Eraser)
            {
                result.Add(BoardChange.CalculateEraserChange(
                    GetLayersEnumerator(),
                    excludePrefabs ? new List<PlacedPrefab>() : placedPrefabs,
                    Tools.EraserLayerMode,
                    pointerWorldPosition));
            }
            else if (Tools.ToolType == ToolType.Brush)
            {
                if (Tools.TileGroup != null)
                {
                    if (Tools.TileGroup.Type == TileGroupType.Regular)
                    {
                        result.Add(BoardChange.CalculateRegularTileGroupChanges(Tools.TileGroup, layers, pointerWorldPosition));
                    }
                    else if (Tools.TileGroup.Type == TileGroupType.NineFour)
                    {
                        NineFourGroup group = Tools.TileGroup as NineFourGroup;
                        result.Add(BoardChange.CalculateNineFourChanges(group, layers[group.Layer], pointerWorldPosition));
                    }
                    else if (Tools.TileGroup.Type == TileGroupType.Nine)
                    {
                        NineGroup group = Tools.TileGroup as NineGroup;
                        result.Add(BoardChange.CalculateNineGroupChanges(group, layers[group.Layer].SubLayers[group.SubLayer], pointerWorldPosition));
                    }
                    else if ((Tools.TileGroup.Type == TileGroupType.Prefab ||
                        Tools.TileGroup.Type == TileGroupType.TreePrefab) && !excludePrefabs)
                    {
                        result.PrefabChanges.Add(new PrefabChange
                        {
                            PrefabToPlace = Tools.SelectedPrefab,
                            PositionToPlace = pointerWorldPosition
                        });
                    }
                    else if (Tools.TileGroup.Type == TileGroupType.NineSliceInOut)
                    {
                        NineSliceInOutGroup group = Tools.TileGroup as NineSliceInOutGroup;
                        result.Add(BoardChange.CalculateNineSliceInOutchanges(
                            group,
                            layers[group.Tiles[0, 0].Layer].SubLayers[group.Tiles[0, 0].SubLayer],
                            pointerWorldPosition));
                    }
                    else if (Tools.TileGroup.Type == TileGroupType.Mountain)
                    {
                        result.Add(BoardChange.CalculateMountainChanges(
                            Tools.TileGroup as MountainGroup,
                            pointerWorldPosition,
                            layers[PaletteData.MountainLayer].SubLayers[Tools.MountainLayer]));
                    }
                    else if (Tools.TileGroup.Type == TileGroupType.Dock)
                    {
                        var group = Tools.TileGroup as DockGroup;
                        result.Add(BoardChange.CalculateDockChanges(group, layers[group.Layer].SubLayers[group.SubLayer], pointerWorldPosition));
                    }
                    else if (Tools.TileGroup.Type == TileGroupType.Wall)
                    {
                        var group = Tools.TileGroup as WallGroup;
                        result.Add(BoardChange.CalculateWallChanges(group, layers[group.Layer].SubLayers[0], pointerWorldPosition));
                    }
                }
            }
            else if(Tools.ToolType == ToolType.Fill)
            {
                if(Tools.TileGroup != null)
                {
                    if (Tools.TileGroup.MaxTileCount == 1)
                    {
                        PaletteTilesData.TileData tile = Tools.TileGroup.Tiles[0, 0];

                        if (Tools.FillBounds == FillBounds.SingleLayer)
                            result.Add(BoardChange.CalculateFillChanges(tile.Tile, layers[tile.Layer].SubLayers[tile.SubLayer], pointerWorldPosition));
                        else
                            result.Add(BoardChange.CalculateFillChanges(tile, layers, pointerWorldPosition));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Enumerates over all the layers that have a tilemap, excluding container layers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Layer> GetLayersEnumerator()
        {
            foreach (Layer layer in layers.Values)
            {
                if (layer.HasTilemap)
                    yield return layer;
                else
                {
                    foreach (Layer sublayer in layer.SubLayers)
                    {
                        if (sublayer.HasTilemap)
                            yield return sublayer;
                    }
                }
            }
        }
        public string FormSerializedData()
        {
            return Serializer.Serialize(layers, placedPrefabs, Tools.Biome, false);
        }

        public void EnsurePreviewCleared()
        {
            if (preview.PreviewTilesSet)
            {
                foreach (Layer layer in GetLayersEnumerator())
                    layer.ClearAllPreviewTiles();

                preview.PreviewTilesSet = false;
            }

            if (preview.PrefabPreviewInstance != null)
            {
                Destroy(preview.PrefabPreviewInstance);
                preview.PrefabPreviewInstance = null;
                preview.TargetPrefab = null;
            }

            foreach (PlacedPrefab pp in placedPrefabs)
                pp.PlacedInstance.SetActive(true);
        }

        private void UpdatePreview(Vector3 pointerScreenPos)
        {
            EnsurePreviewCleared();
            Vector2 worldPos = cam.ScreenToWorldPoint(pointerScreenPos);
            BoardChange change = Tools.ToolType == ToolType.Brush
                ? GetPotentialBoardChange(worldPos)
                : new BoardChange();

            UpdateBrushOutline();

            //-------------------
            //Handle prefab to be placed
            PrefabChange prefToAdd = change.PrefabChanges.Count == 0
                ? null
                : change.PrefabChanges.FirstOrDefault(p => p.PrefabToPlace != null);

            if (prefToAdd != null)
            {
                preview.TargetPrefab = prefToAdd.PrefabToPlace;
                preview.PrefabPreviewInstance = Instantiate(preview.TargetPrefab, prefabLayer);

                var zSnap = preview.PrefabPreviewInstance.GetComponent<ZSnap>();
                if (zSnap != null)
                    zSnap.isActive = true;
            }

            if (preview.PrefabPreviewInstance != null)
                preview.PrefabPreviewInstance.transform.position = worldPos;

            //------------------------
            //Handle prefabs to destroy
            foreach (PrefabChange pChange in change.PrefabChanges)
            {
                if (pChange.PrefabToDestroy != null)
                {
                    placedPrefabs.First(p =>
                        p.Original == pChange.PrefabToDestroy &&
                        (Vector2)p.PlacedInstance.transform.position == (Vector2)pChange.PositionToDestroy)
                        .PlacedInstance.SetActive(false);
                }
            }

            foreach (var tc in change.TileChanges)
            {
                if (tc.Tile == null)
                    tc.Layer.SetPreviewTile(tc.Position, transparentTile);
                else
                    tc.Layer.SetPreviewTile(tc.Position, tc.Tile);
                preview.PreviewTilesSet = true;
            }
        }

        private void UpdateBrushOutline()
        {
            brushOutline.enabled = 
                Tools.ToolType == ToolType.Eraser || 
                (Tools.ToolType == ToolType.Brush && Tools.TileGroup != null);

            brushOutline.transform.position = CellToWorldCenter(WorldToCell(MainCamera.STWP(Input.mousePosition)));

            if (Tools.ToolType == ToolType.Eraser || Tools.TileGroup == null)
                brushOutline.size = Vector2.one;
            else
            {
                brushOutline.size = Tools.TileGroup.Size;
                //Handle cases where the outline is offset on even number size groups
                brushOutline.transform.position += new Vector3(
                    Tools.TileGroup.Size.x % 2 == 0 ? -0.5f : 0, 
                    Tools.TileGroup.Size.y % 2 == 0 ? -0.5f : 0, 
                    0);
            }

            brushOutline.color = Tools.ToolType == ToolType.Eraser ? Color.red : Color.green;
        }

        public static Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector3Int(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y), 0);
        }

        /// <summary>
        /// Given a cell index, returns the center of that cell in world coordinates
        /// </summary>
        /// <param name="cellPosition"></param>
        /// <returns></returns>
        public static Vector3 CellToWorldCenter(Vector3Int cellPosition)
        {
            return new Vector3(cellPosition.x + 0.5f, cellPosition.y + 0.5f, 0);
        }

        #region Events

        private void PointerEnter(PointerEventData data)
        {
            pointerHovering = true;
        }

        private void PointerExit(PointerEventData data)
        {
            pointerHovering = false;
            EnsurePreviewCleared();
        }

        private void PointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                ChangeBoard(GetPotentialBoardChange(data.pointerCurrentRaycast.worldPosition));
            }
            UpdatePreview(data.position);
        }

        private void Drag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
                ChangeBoard(GetPotentialBoardChange(data.pointerCurrentRaycast.worldPosition, true));
            else
            {
                Vector3 curPos = cam.ScreenToWorldPoint(data.position);
                Vector3 prevPos = cam.ScreenToWorldPoint(data.position + data.delta);
                cam.transform.localPosition += curPos - prevPos;

                ClampCamPos();
            }
            UpdatePreview(data.position);
        }

        private void PointerMove(Vector3 screenPos)
        {
            UpdatePreview(screenPos);
        }

        private void PointerScroll(float scroll)
        {
            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize + scroll * -zoomSpeed.Evaluate(cam.orthographicSize) * Time.deltaTime,
                minZoom,
                maxZoom);

            ClampCamPos();
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
        public GameObject PrefabPreviewInstance { get; set; }
        public GameObject TargetPrefab { get; set; }
        public bool PreviewTilesSet { get; set; }
    }

    public class PlacedPrefab
    {
        public GameObject PlacedInstance { get; set; }
        public GameObject Original { get; set; }
    }

    public struct SidesInt
    {
        public int left;
        public int right;
        public int top;
        public int bot;
        public static SidesInt FromOffset(Vector2Int offsetMin, Vector2Int offsetMax)
        {
            return new SidesInt
            {
                left = offsetMin.x,
                bot = offsetMin.y,
                right = offsetMax.x,
                top = offsetMax.y
            };
        }
        public override string ToString()
        {
            return $"left = {left}, right = {right}, bot = {bot}, top = {top}";
        }
    }
}
