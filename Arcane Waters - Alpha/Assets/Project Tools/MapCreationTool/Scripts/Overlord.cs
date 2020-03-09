using System;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using UnityEngine;

namespace MapCreationTool
{
   public class Overlord : MonoBehaviour
   {
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

      [Space(5)]
      [SerializeField]
      private Keybindings.Keybinding[] defaultKeybindings = new Keybindings.Keybinding[0];

      public Dictionary<string, List<string>> mapSpawns { get; private set; }

      private void Awake () {
         instance = this;
         mapSpawns = new Dictionary<string, List<string>>();

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

      private void Start () {
         Settings.setDefaults();
         Settings.load();
         fetchSpawns();
      }

      private void OnEnable () {
         Tools.BiomeChanged += onBiomeChanged;

         Undo.UndoPerformed += ensurePreviewCleared;
         Undo.RedoPerformed += ensurePreviewCleared;

         Tools.AnythingChanged += ensurePreviewCleared;

         Tools.EditorTypeChanged += editorTypeChanged;

         ShopManager.OnLoaded += onLoaded;
         NPCManager.OnLoaded += onLoaded;
         MonsterManager.OnLoaded += onLoaded;
      }

      private void OnDisable () {
         Tools.BiomeChanged -= onBiomeChanged;

         Undo.UndoPerformed -= ensurePreviewCleared;
         Undo.RedoPerformed -= ensurePreviewCleared;

         Tools.AnythingChanged -= ensurePreviewCleared;

         Tools.EditorTypeChanged -= editorTypeChanged;
         ShopManager.OnLoaded -= onLoaded;
         NPCManager.OnLoaded -= onLoaded;
         MonsterManager.OnLoaded -= onLoaded;
      }

      private Keybindings.Keybinding[] formDefaultKeybindings (Keybindings.Keybinding[] defined) {
         List<Keybindings.Keybinding> result = new List<Keybindings.Keybinding>();

         foreach (Keybindings.Command command in Enum.GetValues(typeof(Keybindings.Command))) {
            Keybindings.Keybinding bind = new Keybindings.Keybinding {
               command = command,
               primary = KeyCode.None,
               secondary = KeyCode.None
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

      private void fetchSpawns () {
         UnityThreading.Task task = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            string dbError = null;
            List<MapSpawn> spawns = null;
            try {
               spawns = DB_Main.getMapSpawns();
            } catch (Exception ex) {
               dbError = ex.Message;
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbError != null) {
                  UI.errorDialog.display(dbError);
               } else {
                  setSpawns(spawns);
               }
            });
         });

         UI.loadingPanel.display("Loading spawns", task);
      }

      private void onLoaded () {
         // Check if all managers are loaded
         if (ShopManager.instance.loaded && NPCManager.instance.loaded && MonsterManager.instance.loaded) {
            Destroy(loadCover);
            palette.populatePalette(currentPaletteData[Tools.biome], Tools.biome);
         }
      }

      private void Update () {
         if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
            Undo.doUndo();
         } else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
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
         }
      }

      public void applyData (MapVersion mapVersion) {
         var dt = Serializer.deserialize(mapVersion.editorData, true);

         Tools.changeBiome(dt.biome);
         Tools.changeEditorType(dt.editorType);

         if (dt.size != default) {
            Tools.changeBoardSize(dt.size);
         }

         drawBoard.applyDeserializedData(dt, mapVersion);

         Undo.clear();
      }

      public void applyFileData (string data) {
         var dt = Serializer.deserialize(data, true);

         Tools.changeBiome(dt.biome);
         Tools.changeEditorType(dt.editorType);

         if (dt.size != default) {
            Tools.changeBoardSize(dt.size);
         }

         drawBoard.applyDeserializedData(dt, null);

         Undo.clear();
      }

      private void editorTypeChanged (EditorType from, EditorType to) {
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

         palette.populatePalette(currentPalette[to], to);
         drawBoard.changeBiome(currentPalette[from], currentPalette[to]);
      }

      public void setSpawns (List<MapSpawn> spawns) {
         mapSpawns = new Dictionary<string, List<string>>();
         addSpawns(spawns);
      }

      public void addSpawns (List<MapSpawn> spawns) {
         foreach (MapSpawn spawn in spawns) {
            if (!mapSpawns.ContainsKey(spawn.mapName)) {
               mapSpawns.Add(spawn.mapName, new List<string> { spawn.name });
            } else {
               if (!mapSpawns[spawn.mapName].Contains(spawn.name)) {
                  mapSpawns[spawn.mapName].Add(spawn.name);
               }
            }
         }
      }

      /// <summary>
      /// Returns a dictionary of palettes that the current type of editor uses
      /// </summary>
      private BiomedPaletteData currentPaletteData
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
   }
}
