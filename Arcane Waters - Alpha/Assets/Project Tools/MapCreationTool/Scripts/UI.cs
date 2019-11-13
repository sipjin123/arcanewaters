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
        const string masterSceneName = "MasterTool";

        [SerializeField]
        private Dropdown toolDropdown = null;
        [SerializeField]
        private Dropdown biomeDropdown = null;
        [SerializeField]
        private Toggle burrowedTreesToggle = null;
        [SerializeField]
        private Dropdown mountainLayerDropdown = null;
        [SerializeField]
        private Dropdown fillBoundsDropdown = null;

        [SerializeField]
        private Button undoButton = null;
        [SerializeField]
        private Button redoButton = null;

        [SerializeField]
        private DrawBoard drawBoard = null;
        [SerializeField]
        private Overlord overlord = null;

        private CanvasScaler canvasScaler = null;

        public YesNoDialog YesNoDialog { get; private set; }

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
            fillBoundsDropdown.value = (int)Tools.FillBounds;
            for (int i = 0; i < biomeDropdown.options.Count; i++)
                if (optionsPacks[biomeDropdown.options[i].text] == Tools.Biome)
                    biomeDropdown.value = i;

            UpdateShowedOptions();
        }
        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            YesNoDialog = GetComponentInChildren<YesNoDialog>();
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

            fillBoundsDropdown.gameObject.SetActive(Tools.ToolType == ToolType.Fill);
        }
        public void BiomeDropdown_Changes()
        {
            if (Tools.Biome != optionsPacks[biomeDropdown.options[biomeDropdown.value].text])
                Tools.ChangeBiome(optionsPacks[biomeDropdown.options[biomeDropdown.value].text]);
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
        public void FillBoundsDropDown_ValueChanged()
        {
            if (Tools.FillBounds != (FillBounds)fillBoundsDropdown.value)
                Tools.ChangeFillBounds((FillBounds)fillBoundsDropdown.value);
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
            YesNoDialog.Display(
                "New map", 
                "Are you sure you want to start a new map?\nAll unsaved progress will be lost.", 
                drawBoard.ClearAll, 
                null);
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

        public void MasterToolButton_Click()
        {
            YesNoDialog.Display("Exiting map editor",
                "Are you sure you want to exit the map editor?\nAll unsaved progress will be lost.",
                () => SceneManager.LoadScene(masterSceneName), null);
        }
    }
}