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

        private void Awake()
        {
            Tools.SetDefaultValues();
            Undo.Clear();

            paletteDatas = paletteResources.GatherData(config);

            AssetSerializationMaps.Load();
        }

        private void OnEnable()
        {
            Tools.BiomeChanged += OnBiomeChanged;

            Undo.UndoPerformed += EnsurePreviewCleared;
            Undo.RedoPerformed += EnsurePreviewCleared;

            Tools.AnythingChanged += EnsurePreviewCleared;
        }

        private void OnDisable()
        {
            Tools.BiomeChanged -= OnBiomeChanged;

            Undo.UndoPerformed -= EnsurePreviewCleared;
            Undo.RedoPerformed -= EnsurePreviewCleared;

            Tools.AnythingChanged -= EnsurePreviewCleared;
        }

        private void Start()
        {
            palette.PopulatePalette(paletteDatas[Tools.Biome]);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                Undo.DoUndo();
            }
            else if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                Undo.DoRedo();
            }
        }

        public void ApplyFileData(string data)
        {
            var dt = Serializer.Deserialize(data);

            Tools.ChangeBiome(dt.biome);
            drawBoard.ApplyDeserializedData(dt);
            Undo.Clear();
        }

        private void EnsurePreviewCleared()
        {
            drawBoard.EnsurePreviewCleared();
        }
        private void OnBiomeChanged(BiomeType from, BiomeType to)
        {
            palette.PopulatePalette(paletteDatas[to]);
            drawBoard.ChangeBiome(paletteDatas[from], paletteDatas[to]);
        }
    }
}
