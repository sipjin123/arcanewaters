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
      private PaletteResources paletteResources = null;
      [SerializeField]
      private Palette palette = null;
      [SerializeField]
      private DrawBoard drawBoard = null;

      private Dictionary<BiomeType, PaletteData> paletteDatas;

      private void Awake () {
         Tools.setDefaultValues();
         Undo.clear();

         AssetSerializationMaps.load();

         paletteDatas = paletteResources.gatherData(config);
      }

      private void OnEnable () {
         Tools.BiomeChanged += onBiomeChanged;

         Undo.UndoPerformed += ensurePreviewCleared;
         Undo.RedoPerformed += ensurePreviewCleared;

         Tools.AnythingChanged += ensurePreviewCleared;
      }

      private void OnDisable () {
         Tools.BiomeChanged -= onBiomeChanged;

         Undo.UndoPerformed -= ensurePreviewCleared;
         Undo.RedoPerformed -= ensurePreviewCleared;

         Tools.AnythingChanged -= ensurePreviewCleared;
      }

      private void Start () {
         palette.populatePalette(paletteDatas[Tools.biome]);
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
         drawBoard.applyDeserializedData(dt);
         Undo.clear();
      }

      private void ensurePreviewCleared () {
         drawBoard.ensurePreviewCleared();
      }
      private void onBiomeChanged (BiomeType from, BiomeType to) {
         palette.populatePalette(paletteDatas[to]);
         drawBoard.changeBiome(paletteDatas[from], paletteDatas[to]);
      }
   }
}
