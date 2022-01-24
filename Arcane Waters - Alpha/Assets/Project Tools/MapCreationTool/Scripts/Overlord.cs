using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MapCreationTool
{
   public class Overlord : MonoBehaviour
   {
      // The biome selected in the editor
      public Biome.Type editorBiome = Biome.Type.Forest;

      public const int TILE_PIXEL_WIDTH = 16;

      // How many open world maps are in the world
      public static Vector2Int openWorldMapCount = new Vector2Int(15, 9);

      public static Overlord instance { get; private set; }

      [Header("Accounts")]
      [SerializeField]
      private MasterToolAccountManager accountManagerPref = null;
      [SerializeField]
      private bool instantLoginWithTestPassword = true;

      [Space(5)]
      [SerializeField]
      private BiomedPaletteData areaPaletteData = null;
      [SerializeField]
      private BiomedPaletteData seaPaletteData = null;
      [SerializeField]
      private BiomedPaletteData interiorPaletteData = null;
      [SerializeField]
      private Palette palette = null;
      [SerializeField]
      private DrawBoard drawBoard = null;
      [SerializeField]
      private GameObject loadCover = null;
      [SerializeField, Tooltip("Renderers for displaying adjacent map images. Order: top, right, bottom, left")]
      private SpriteRenderer[] adjacentMapRenderers = new SpriteRenderer[0];

      [Space(5)]
      [SerializeField]
      private Keybindings.Keybinding[] defaultKeybindings = new Keybindings.Keybinding[0];

      // Remote data
      public static RemoteMaps remoteMaps { get; private set; }
      public static RemoteSpawns remoteSpawns { get; private set; }

      private void Awake () {
         CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

         instance = this;

         // Make editor pause when you minimize it
         Application.runInBackground = false;

         initializeRemoteDatas();

         Tools.setDefaultValues();
         Undo.clear();

         AssetSerializationMaps.load();

         areaPaletteData.collectInformation();
         seaPaletteData.collectInformation();
         interiorPaletteData.collectInformation();

         Settings.keybindings.defaultKeybindings = formDefaultKeybindings(defaultKeybindings);

         if (MasterToolAccountManager.self == null) {
            MasterToolAccountManager loginPanel = Instantiate(accountManagerPref);

            if (instantLoginWithTestPassword) {
               loginPanel.passwordField.text = "test";
               loginPanel.loginButton.onClick.Invoke();
            }
         }
      }

      private IEnumerator Start () {
         Settings.setDefaults();
         Settings.load();

         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllRemoteData();
      }

      private void OnEnable () {
         Tools.BiomeChanged += onBiomeChanged;

         Undo.UndoPerformed += ensurePreviewCleared;
         Undo.RedoPerformed += ensurePreviewCleared;

         Tools.AnythingChanged += ensurePreviewCleared;

         Tools.EditorTypeChanged += editorTypeChanged;

         ShopManager.OnLoaded += onRemoteDataLoaded;
         NPCManager.OnLoaded += onRemoteDataLoaded;
         MonsterManager.OnLoaded += onRemoteDataLoaded;
         MapEditorDiscoveriesManager.OnLoaded += onRemoteDataLoaded;
      }

      private void OnDisable () {
         Tools.BiomeChanged -= onBiomeChanged;

         Undo.UndoPerformed -= ensurePreviewCleared;
         Undo.RedoPerformed -= ensurePreviewCleared;

         Tools.AnythingChanged -= ensurePreviewCleared;

         Tools.EditorTypeChanged -= editorTypeChanged;
         ShopManager.OnLoaded -= onRemoteDataLoaded;
         NPCManager.OnLoaded -= onRemoteDataLoaded;
         MonsterManager.OnLoaded -= onRemoteDataLoaded;
         MapEditorDiscoveriesManager.OnLoaded -= onRemoteDataLoaded;
      }

      private Keybindings.Keybinding[] formDefaultKeybindings (Keybindings.Keybinding[] defined) {
         List<Keybindings.Keybinding> result = new List<Keybindings.Keybinding>();

         foreach (Keybindings.Command command in Enum.GetValues(typeof(Keybindings.Command))) {
            Keybindings.Keybinding bind = new Keybindings.Keybinding {
               command = command,
               primary = Key.None,
               secondary = Key.None
            };

            if (defined.Any(d => d.command == command)) {
               Keybindings.Keybinding toSet = defined.First(k => k.command == command);
               bind.primary = toSet.primary;
               bind.secondary = toSet.secondary;
            }

            result.Add(bind);
         }

         return result.ToArray();
      }

      public static bool validateMap (out string errors) {
         errors = "";

         // Make sure there's at least 1 spawn
         List<MapSpawn> mapSpawns = DrawBoard.instance.formSpawnList(null, -1);
         if (mapSpawns.Count == 0) {
            errors += "There must be at least 1 spawn in a map. Even if your map doesn't naturally require any spawns, " +
               "it may still need to be used for testing. You can place it in a convenient place and title it 'default'."
               + Environment.NewLine;
         }

         // Make sure all spawns have unique names
         if (mapSpawns.Count > 0 && mapSpawns.GroupBy(s => s.name).Max(g => g.Count()) > 1) {
            errors += "All spawn names in a map must be unique" + Environment.NewLine;
         }

         // Make sure warps and spawns don't overlap
         bool anyOverlap = false;

         Bounds[] warpBounds = DrawBoard.getPrefabComponents<WarpMapEditor>()
            .Select(w => new Bounds(w.transform.position, new Vector3(w.size.x, w.size.y, 1000f))).ToArray();
         // We add 50 units in the +Z axis to make sure these bounds intersect if one is contained in another
         Bounds[] spawnBounds = DrawBoard.getPrefabComponents<SpawnMapEditor>()
            .Select(s => new Bounds(s.transform.position + Vector3.forward * 50f, new Vector3(s.size.x, s.size.y, 1000f))).ToArray();

         for (int i = 0; i < warpBounds.Length; i++) {
            warpBounds[i].Expand(SpawnMapEditor.SPACING_FROM_WARPS * 2f);
         }

         foreach (Bounds warp in warpBounds) {
            foreach (Bounds spawn in spawnBounds) {
               if (warp.Intersects(spawn)) {
                  anyOverlap = true;
               }
            }
         }

         if (anyOverlap) {
            errors += "There must be at least 1 tile of distance between all warps and spawns" + Environment.NewLine;
         }

         return string.IsNullOrEmpty(errors);
      }

      private void initializeRemoteDatas () {
         remoteMaps = new RemoteMaps { OnLoaded = onRemoteDataLoaded };
         remoteSpawns = new RemoteSpawns { OnLoaded = onRemoteDataLoaded };
      }

      public static void loadAllRemoteData () {
         remoteSpawns.load();
         remoteMaps.load();
      }

      public static bool allRemoteDataLoaded
      {
         get
         {
            return
               ShopManager.instance.loaded &&
               NPCManager.instance.loaded &&
               MonsterManager.instance.loaded &&
               remoteMaps.loaded &&
               remoteSpawns.loaded &&
               MapEditorDiscoveriesManager.instance.loaded;
         }
      }

      private void onRemoteDataLoaded () {
         // Check if all managers are loaded
         if (allRemoteDataLoaded) {
            Destroy(loadCover);
            palette.populatePalette(currentPaletteData[Tools.biome], Tools.biome);
         }
      }


      private void Update () {
         if (KeyUtils.GetKeyDown(Key.Z) && (KeyUtils.GetKey(Key.LeftCtrl) || KeyUtils.GetKey(Key.RightCtrl))) {
            Undo.doUndo();
         } else if (KeyUtils.GetKeyDown(Key.Y) && (KeyUtils.GetKey(Key.LeftCtrl) || KeyUtils.GetKey(Key.RightCtrl))) {
            Undo.doRedo();
         }

         if (!UI.anyPanelOpen && !UI.anyInputFieldFocused) {
            if (Settings.keybindings.getAction(Keybindings.Command.PanLeft)) {
               MainCamera.pan(Vector3.left * 20f * Time.deltaTime);
            }
            if (Settings.keybindings.getAction(Keybindings.Command.PanRight)) {
               MainCamera.pan(Vector3.right * 20f * Time.deltaTime);
            }
            if (Settings.keybindings.getAction(Keybindings.Command.PanUp)) {
               MainCamera.pan(Vector3.up * 20f * Time.deltaTime);
            }
            if (Settings.keybindings.getAction(Keybindings.Command.PanDown)) {
               MainCamera.pan(Vector3.down * 20f * Time.deltaTime);
            }
            if (Settings.keybindings.getAction(Keybindings.Command.ZoomIn)) {
               MainCamera.zoom(-0.3f * Time.deltaTime);
            }
            if (Settings.keybindings.getAction(Keybindings.Command.ZoomOut)) {
               MainCamera.zoom(0.3f * Time.deltaTime);
            }
            if (Settings.keybindings.getAction(Keybindings.Command.BrushTool)) {
               if (Tools.toolType != ToolType.Brush) {
                  Tools.changeTool(ToolType.Brush);
               }
            }
            if (Settings.keybindings.getAction(Keybindings.Command.EraserTool)) {
               if (Tools.toolType != ToolType.Eraser) {
                  Tools.changeTool(ToolType.Eraser);
               }
            }
            if (Settings.keybindings.getAction(Keybindings.Command.FillTool)) {
               if (Tools.toolType != ToolType.Fill) {
                  Tools.changeTool(ToolType.Fill);
               }
            }
            if (Settings.keybindings.getAction(Keybindings.Command.SelectTool)) {
               if (Tools.toolType != ToolType.Selection) {
                  Tools.changeTool(ToolType.Selection);
               }
            }
            if (Settings.keybindings.getAction(Keybindings.Command.MoveTool)) {
               if (Tools.toolType != ToolType.Move) {
                  Tools.changeTool(ToolType.Move);
               }
            }
            if (Settings.keybindings.getAction(Keybindings.Command.CancelAction)) {
               DrawBoardEvents.CancelAction?.Invoke();
            }
         }
      }

      public void applyData (MapVersion mapVersion) {
         // Destroy adjacent map images
         foreach (SpriteRenderer ren in adjacentMapRenderers) {
            if (ren.sprite != null) {
               if (ren.sprite.texture != null) {
                  Destroy(ren.sprite.texture);
               }
               Destroy(ren.sprite);
            }
            ren.sprite = null;
         }


         var dt = Serializer.deserializeForEditor(mapVersion.editorData);

         Tools.changeBiome(dt.biome);
         Tools.changeEditorType(dt.editorType);

         if (dt.size != default) {
            Tools.changeBoardSize(dt.size);
         }

         drawBoard.applyDeserializedData(dt, mapVersion);

         Undo.clear();
      }

      public void setAdjacentMapImages ((string areaKey, Map map, Texture2D screenshot)[] adjacent) {
         for (int i = 0; i < adjacent.Length; i++) {
            if (adjacent[i].screenshot != null) {
               adjacentMapRenderers[i].sprite = Sprite.Create(
                  adjacent[i].screenshot,
                  new Rect(0, 0, adjacent[i].screenshot.width, adjacent[i].screenshot.height),
                  Vector2.one * 0.5f);
            }
         }
      }

      public void applyFileData (string data) {
         var dt = Serializer.deserializeForEditor(data);

         Tools.changeBiome(dt.biome);
         Tools.changeEditorType(dt.editorType);

         if (dt.size != default) {
            Tools.changeBoardSize(dt.size);
         }

         drawBoard.applyDeserializedData(dt, null);

         Undo.clear();
      }

      public void editorTypeChanged (EditorType from, EditorType to) {
         D.debug("Editor changed from {" + from + "} to {" + to + "}");
         switch (to) {
            case EditorType.Area:
               palette.populatePalette(areaPaletteData[Tools.biome], Tools.biome);
               break;
            case EditorType.Interior:
               palette.populatePalette(interiorPaletteData[Tools.biome], Tools.biome);
               break;
            case EditorType.Sea:
               palette.populatePalette(seaPaletteData[Tools.biome], Tools.biome);
               break;
            default:
               Debug.LogError("Unrecognized editor type.");
               break;
         }
      }

      private void ensurePreviewCleared () {
         drawBoard.ensurePreviewCleared();
      }
      private void onBiomeChanged (Biome.Type from, Biome.Type to) {
         BiomedPaletteData currentPalette = currentPaletteData;
         editorBiome = to;
         palette.populatePalette(currentPalette[to], to);
         drawBoard.changeBiome(currentPalette[from], currentPalette[to]);
      }

      /// <summary>
      /// Returns a dictionary of palettes that the current type of editor uses
      /// </summary>
      public BiomedPaletteData currentPaletteData
      {
         get
         {
            switch (Tools.editorType) {
               case EditorType.Area:
                  return areaPaletteData;
               case EditorType.Interior:
                  return interiorPaletteData;
               case EditorType.Sea:
                  return seaPaletteData;
               default:
                  throw new Exception($"Unrecognized editor type {Tools.editorType.ToString()}");
            }
         }
      }

      public static bool isOpenWorldMapKey (string key, out int mapX, out int mapY) {
         mapX = -1;
         mapY = -1;

         if (!key.StartsWith("world_map_")) {
            return false;
         }

         string[] parts = key.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
         if (parts.Length != 3) {
            return false;
         }

         if (parts[2].Length != 2) {
            return false;
         }

         // Convert the ending of ex. 'world_map_A0' to coordinates
         int x = Convert.ToInt32(parts[2][0]) - 65;
         int y = Convert.ToInt32(parts[2][1]) - 48;

         if (x < 0 || y < 00 || x >= openWorldMapCount.x || y >= openWorldMapCount.y) {
            return false;
         }

         mapX = x;
         mapY = y;

         return true;
      }

      // Creates an array, which shows what maps are adjacent to given map (open world)
      // Only fills in the area key portion
      // top, right, bottom, left
      // null if nothing is at that position
      public static (string areaKey, Map map, Texture2D screenshot)[] createAdjacentMapsArray (int mapX, int mapY) {
         (string areaKey, Map map, Texture2D screenshot)[] result = new (string areaKey, Map map, Texture2D screenshot)[4];

         if (mapY < openWorldMapCount.y - 1) {
            result[0].areaKey = "world_map_" + Convert.ToChar((mapX + 65)) + (mapY + 1).ToString();
         }
         if (mapX < openWorldMapCount.x - 1) {
            result[1].areaKey = "world_map_" + Convert.ToChar((mapX + 65 + 1)) + (mapY).ToString();
         }
         if (mapY > 0) {
            result[2].areaKey = "world_map_" + Convert.ToChar((mapX + 65)) + (mapY - 1).ToString();
         }
         if (mapX > 0) {
            result[3].areaKey = "world_map_" + Convert.ToChar((mapX + 65 - 1)) + (mapY).ToString();
         }

         return result;
      }
   }
}
