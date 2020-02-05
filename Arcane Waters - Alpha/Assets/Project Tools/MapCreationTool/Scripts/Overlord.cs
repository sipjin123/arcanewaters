using System;
using System.Collections.Generic;
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

         if (MasterToolAccountManager.self == null) {
            MasterToolAccountManager loginPanel = Instantiate(accountManagerPref);

            if (instantLoginWithTestPassword) {
               loginPanel.passwordField.text = "test";
               loginPanel.loginButton.onClick.Invoke();
            }
         }
      }

      private void Start () {
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
      }

      private void OnDisable () {
         Tools.BiomeChanged -= onBiomeChanged;

         Undo.UndoPerformed -= ensurePreviewCleared;
         Undo.RedoPerformed -= ensurePreviewCleared;

         Tools.AnythingChanged -= ensurePreviewCleared;

         Tools.EditorTypeChanged -= editorTypeChanged;
         ShopManager.OnLoaded -= onLoaded;
         NPCManager.OnLoaded -= onLoaded;
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
         Destroy(loadCover);
         palette.populatePalette(currentPaletteData[Tools.biome], Tools.biome);
      }

      private void Update () {
         if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
            Undo.doUndo();
         } else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
            Undo.doRedo();
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
