using MapCreationTool.PaletteTilesData;
using MapCreationTool.Serialization;
using MapCreationTool.UndoSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapCreationTool
{
    public class Overlord : MonoBehaviour
    {
        [SerializeField]
        private EditorConfig config;

        [Space(5)]
        [SerializeField]
        private GameObject paletteDataContainers;
        [SerializeField]
        private Palette palette;
        [SerializeField]
        private DrawBoard drawBoard;
        [SerializeField]
        private UI ui;

        private Dictionary<string, BiomeType> paletteContainerNames = new Dictionary<string, BiomeType>
        {
            { "forest_pack", BiomeType.Forest },
            { "desert_pack", BiomeType.Desert },
            { "lava_pack", BiomeType.Lava },
            { "mushroom_pack", BiomeType.Shroom },
            { "pine_pack", BiomeType.Pine },
            { "snow_pack", BiomeType.Snow }
        };

        private Dictionary<BiomeType, PaletteData> paletteDatas;

        private void Awake()
        {
            Tools.SetDefaultValues();
            Undo.Clear();

            var containers = paletteDataContainers.GetComponentsInChildren<PaletteDataContainer>(true);

            paletteDatas = containers
                .Where(pdc => pdc.transform.name != "shared")
                .Select(con => con.GatherData(config, paletteContainerNames[con.transform.name]))
                .ToDictionary(data => data.Type.Value, data => data);

            PaletteData sharedPalette = containers.First(p => p.transform.name == "shared").GatherData(config, null);
            foreach (var palette in paletteDatas.Values)
                palette.Merge(sharedPalette);

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
