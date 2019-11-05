using MapCreationTool.PaletteTilesData;
using MapCreationTool.UndoSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MapCreationTool
{
    public class UI : MonoBehaviour
    {
        [SerializeField]
        private Dropdown toolDropdown;
        [SerializeField]
        private Dropdown biomeDropdown;
        [SerializeField]
        private Toggle burrowedTreesToggle;
        [SerializeField]
        private Toggle tileModeToggle;
        [SerializeField]
        private Dropdown mountainLayerDropdown;

        [SerializeField]
        private Button undoButton;
        [SerializeField]
        private Button redoButton;

        [SerializeField]
        private DrawBoard drawBoard;
        [SerializeField]
        private Overlord overlord;

        // Opens the main tool
        public Button openMainTool;

        private CanvasScaler canvasScaler;

        private Dictionary<string, BiomeType> optionsPacks = new Dictionary<string, BiomeType>
    {
        {"Forest", BiomeType.Forest },
        {"Desert", BiomeType.Desert },
        {"Lava", BiomeType.Lava },
        {"Shroom", BiomeType.Shroom },
        {"Pine", BiomeType.Pine },
        {"Snow", BiomeType.Snow }
    };

        private void OnEnable()
        {
            Tools.AnythingChanged += UpdateAllUI;

            Undo.UndoRegisterd += UpdateAllUI;
            Undo.UndoPerformed += UpdateAllUI;
            Undo.RedoPerformed += UpdateAllUI;
            Undo.LogCleared += UpdateAllUI;
        }

        private void OnDisable()
        {
            Tools.AnythingChanged -= UpdateAllUI;

            Undo.UndoRegisterd -= UpdateAllUI;
            Undo.UndoPerformed -= UpdateAllUI;
            Undo.RedoPerformed -= UpdateAllUI;
            Undo.LogCleared -= UpdateAllUI;
        }
        private void UpdateAllUI()
        {
            undoButton.interactable = Undo.UndoCount > 0;
            redoButton.interactable = Undo.RedoCount > 0;

            burrowedTreesToggle.isOn = Tools.BurrowedTrees;
            toolDropdown.value = (int)Tools.ToolType;
            mountainLayerDropdown.value = Tools.MountainLayer;
            tileModeToggle.isOn = Tools.IndividualTiles;
            for (int i = 0; i < biomeDropdown.options.Count; i++)
                if (optionsPacks[biomeDropdown.options[i].text] == Tools.Biome)
                    biomeDropdown.value = i;

            UpdateShowedOptions();
        }
        private void Awake()
        {
            openMainTool.onClick.AddListener(() => {
               SceneManager.LoadScene(MasterToolScene.masterScene);
            });
            canvasScaler = GetComponent<CanvasScaler>();
        }
        private void Start()
        {
            biomeDropdown.options = optionsPacks.Keys.Select(k => new Dropdown.OptionData(k)).ToList();

            UpdateAllUI();
        }
        private void UpdateShowedOptions()
        {
            mountainLayerDropdown.gameObject.SetActive(
                Tools.ToolType == ToolType.Brush &&
                Tools.TileGroup != null && Tools.TileGroup.Type == TileGroupType.Mountain);

            burrowedTreesToggle.gameObject.SetActive(
                Tools.ToolType == ToolType.Brush &&
                Tools.TileGroup != null && Tools.TileGroup.Type == TileGroupType.TreePrefab);
        }
        public void BiomeDropdown_Changes()
        {
            if (Tools.Biome != optionsPacks[biomeDropdown.options[biomeDropdown.value].text])
                Tools.ChangeBiome(optionsPacks[biomeDropdown.options[biomeDropdown.value].text]);
        }
        public void TileModeToggle_Changes()
        {
            if (Tools.IndividualTiles != tileModeToggle.isOn)
                Tools.ChangeIndividualTiles(tileModeToggle.isOn);
        }
        public void BurrowedTreesToggle_Changes()
        {
            if (Tools.BurrowedTrees != burrowedTreesToggle.isOn)
                Tools.ChangeBurrowedTrees(burrowedTreesToggle.isOn);
        }
        public void MountainLayerDropdown_Changes()
        {
            if (Tools.MountainLayer != mountainLayerDropdown.value)
                Tools.ChangeMountainLayer(mountainLayerDropdown.value);
        }
        public void ToolDropdown_ValueChanged()
        {
            if (Tools.ToolType != (ToolType)toolDropdown.value)
                Tools.ChangeTool((ToolType)toolDropdown.value);
        }
        public void UndoButton_Click()
        {
            Undo.DoUndo();
        }
        public void RedoButton_Click()
        {
            Undo.DoRedo();
        }
        public void NewButton_Click()
        {
            drawBoard.ClearAll();
        }
        public void OpenButton_Click()
        {
            string data = FileUtility.OpenFile();
            if (data != null)
            {
                overlord.ApplyFileData(data);
            }
        }
        public void SaveButton_Click()
        {
            FileUtility.SaveFile(drawBoard.FormSerializedData());
        }
    }
}