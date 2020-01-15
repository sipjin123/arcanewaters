using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using System.Collections.Generic;
using UnityEngine;

namespace MapCreationTool
{
   public class Overlord : MonoBehaviour
   {
      public static Overlord instance { get; private set; }

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

      private void Awake () {
         instance = this;

         Tools.setDefaultValues();
         Undo.clear();

         AssetSerializationMaps.load();

         areaPaletteData.collectInformation();
         seaPaletteData.collectInformation();
         interiorPaletteData.collectInformation();
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

      private void onLoaded () {
         Destroy(loadCover);
         palette.populatePalette(currentPaletteData[Tools.biome]);
      }

      private void Update () {
         if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
            Undo.doUndo();
         } else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
            Undo.doRedo();
         }
      }

      public void applyFileData (string data, string mapName) {
         var dt = Serializer.deserialize(data, true);

         Tools.changeBiome(dt.biome);
         Tools.changeEditorType(dt.editorType);

         if (dt.size != default) {
            Tools.changeBoardSize(dt.size);
         }

         drawBoard.applyDeserializedData(dt, mapName);

         Undo.clear();
      }

      private void editorTypeChanged (EditorType from, EditorType to) {
         switch (to) {
            case EditorType.Area:
               palette.populatePalette(areaPaletteData[Tools.biome]);
               break;
            case EditorType.Interior:
               palette.populatePalette(interiorPaletteData[Tools.biome]);
               break;
            case EditorType.Sea:
               palette.populatePalette(seaPaletteData[Tools.biome]);
               break;
            default:
               Debug.LogError("Unrecognized editor type.");
               break;
         }
      }

      private void ensurePreviewCleared () {
         drawBoard.ensurePreviewCleared();
      }
      private void onBiomeChanged (BiomeType from, BiomeType to) {
         BiomedPaletteData currentPalette = currentPaletteData;

         palette.populatePalette(currentPalette[to]);
         drawBoard.changeBiome(currentPalette[from], currentPalette[to]);
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
                  throw new System.Exception($"Unrecognized editor type {Tools.editorType.ToString()}");
            }
         }
      }
   }
}
