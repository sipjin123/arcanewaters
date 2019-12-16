using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using System.Collections.Generic;
using UnityEngine;

namespace MapCreationTool
{
   public class Overlord : MonoBehaviour
   {
      [SerializeField]
      private EditorConfig config = null;

      [Space(5)]
      [SerializeField]
      private PaletteResources areaPaletteResources = null;
      [SerializeField]
      private PaletteResources interiorPaletteResources = null;
      [SerializeField]
      private PaletteResources seaPaletteResources = null;
      [SerializeField]
      private Palette palette = null;
      [SerializeField]
      private DrawBoard drawBoard = null;

      private Dictionary<BiomeType, PaletteData> areaPaletteDatas;
      private Dictionary<BiomeType, PaletteData> interiorPaletteDatas;
      private Dictionary<BiomeType, PaletteData> seaPaletteDatas;

      private void Awake () {
         Tools.setDefaultValues();
         Undo.clear();

         AssetSerializationMaps.load();

         areaPaletteDatas = areaPaletteResources.gatherData(config);
         interiorPaletteDatas = interiorPaletteResources.gatherData(config);
         seaPaletteDatas = seaPaletteResources.gatherData(config);
      }

      private void OnEnable () {
         Tools.BiomeChanged += onBiomeChanged;

         Undo.UndoPerformed += ensurePreviewCleared;
         Undo.RedoPerformed += ensurePreviewCleared;

         Tools.AnythingChanged += ensurePreviewCleared;

         Tools.EditorTypeChanged += editorTypeChanged;
      }

      private void OnDisable () {
         Tools.BiomeChanged -= onBiomeChanged;

         Undo.UndoPerformed -= ensurePreviewCleared;
         Undo.RedoPerformed -= ensurePreviewCleared;

         Tools.AnythingChanged -= ensurePreviewCleared;

         Tools.EditorTypeChanged -= editorTypeChanged;
      }

      private void Start () {
         palette.populatePalette(currentEditorPalettes[Tools.biome]);
      }

      private void Update () {
         if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
            Undo.doUndo();
         } else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) {
            Undo.doRedo();
         }
      }

      public void applyFileData (string data) {
         var dt = Serializer.deserialize(data, true);

         Tools.changeBiome(dt.biome);
         Tools.changeEditorType(dt.editorType);

         if(dt.size != default) {
            Tools.changeBoardSize(dt.size);
         }

         drawBoard.applyDeserializedData(dt);
         Undo.clear();
      }

      private void editorTypeChanged(EditorType from, EditorType to) {
         switch(to) {
            case EditorType.Area:
               palette.populatePalette(areaPaletteDatas[Tools.biome]);
               break;
            case EditorType.Interior:
               palette.populatePalette(interiorPaletteDatas[Tools.biome]);
               break;
            case EditorType.Sea:
               palette.populatePalette(seaPaletteDatas[Tools.biome]);
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
         Dictionary<BiomeType, PaletteData> currentPalettes = currentEditorPalettes;

         palette.populatePalette(currentPalettes[to]);
         drawBoard.changeBiome(currentPalettes[from], currentPalettes[to]);
      }

      /// <summary>
      /// Returns a dictionary of palettes that the current type of editor uses
      /// </summary>
      private Dictionary<BiomeType, PaletteData> currentEditorPalettes
      {
         get {
            switch(Tools.editorType) {
               case EditorType.Area:
                  return areaPaletteDatas;
               case EditorType.Interior:
                  return interiorPaletteDatas;
               case EditorType.Sea:
                  return seaPaletteDatas;
               default:
                  throw new System.Exception($"Unrecognized editor type {Tools.editorType.ToString()}");
            }
         }
      }
   }
}
