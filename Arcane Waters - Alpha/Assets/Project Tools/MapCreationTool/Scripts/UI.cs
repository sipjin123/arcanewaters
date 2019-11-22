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

      public YesNoDialog yesNoDialog { get; private set; }

      private Dictionary<string, BiomeType> optionsPacks = new Dictionary<string, BiomeType>
      {
            {"Forest", BiomeType.Forest },
            {"Desert", BiomeType.Desert },
            {"Lava", BiomeType.Lava },
            {"Shroom", BiomeType.Shroom },
            {"Pine", BiomeType.Pine },
            {"Snow", BiomeType.Snow }
        };

      private void OnEnable () {
         Tools.AnythingChanged += updateAllUI;

         Undo.UndoRegisterd += updateAllUI;
         Undo.UndoPerformed += updateAllUI;
         Undo.RedoPerformed += updateAllUI;
         Undo.LogCleared += updateAllUI;
      }

      private void OnDisable () {
         Tools.AnythingChanged -= updateAllUI;

         Undo.UndoRegisterd -= updateAllUI;
         Undo.UndoPerformed -= updateAllUI;
         Undo.RedoPerformed -= updateAllUI;
         Undo.LogCleared -= updateAllUI;
      }
      private void updateAllUI () {
         undoButton.interactable = Undo.undoCount > 0;
         redoButton.interactable = Undo.redoCount > 0;

         burrowedTreesToggle.isOn = Tools.burrowedTrees;
         toolDropdown.value = (int) Tools.toolType;
         mountainLayerDropdown.value = Tools.mountainLayer;
         fillBoundsDropdown.value = (int) Tools.fillBounds;
         for (int i = 0; i < biomeDropdown.options.Count; i++)
            if (optionsPacks[biomeDropdown.options[i].text] == Tools.biome)
               biomeDropdown.value = i;

         updateShowedOptions();
      }
      private void Awake () {
         canvasScaler = GetComponent<CanvasScaler>();
         yesNoDialog = GetComponentInChildren<YesNoDialog>();
      }
      private void Start () {
         biomeDropdown.options = optionsPacks.Keys.Select(k => new Dropdown.OptionData(k)).ToList();

         updateAllUI();
      }
      private void updateShowedOptions () {
         mountainLayerDropdown.gameObject.SetActive(
             Tools.toolType == ToolType.Brush &&
             Tools.tileGroup != null && Tools.tileGroup.type == TileGroupType.Mountain);

         burrowedTreesToggle.gameObject.SetActive(
             Tools.toolType == ToolType.Brush &&
             Tools.tileGroup != null && Tools.tileGroup.type == TileGroupType.TreePrefab);

         fillBoundsDropdown.gameObject.SetActive(Tools.toolType == ToolType.Fill);
      }
      public void biomeDropdown_Changes () {
         if (Tools.biome != optionsPacks[biomeDropdown.options[biomeDropdown.value].text])
            Tools.changeBiome(optionsPacks[biomeDropdown.options[biomeDropdown.value].text]);
      }
      public void burrowedTreesToggle_Changes () {
         if (Tools.burrowedTrees != burrowedTreesToggle.isOn)
            Tools.changeBurrowedTrees(burrowedTreesToggle.isOn);
      }
      public void mountainLayerDropdown_Changes () {
         if (Tools.mountainLayer != mountainLayerDropdown.value)
            Tools.changeMountainLayer(mountainLayerDropdown.value);
      }
      public void toolDropdown_ValueChanged () {
         if (Tools.toolType != (ToolType) toolDropdown.value)
            Tools.changeTool((ToolType) toolDropdown.value);
      }
      public void fillBoundsDropDown_ValueChanged () {
         if (Tools.fillBounds != (FillBounds) fillBoundsDropdown.value)
            Tools.changeFillBounds((FillBounds) fillBoundsDropdown.value);
      }
      public void undoButton_Click () {
         Undo.doUndo();
      }
      public void redoButton_Click () {
         Undo.doRedo();
      }
      public void newButton_Click () {
         yesNoDialog.display(
             "New map",
             "Are you sure you want to start a new map?\nAll unsaved progress will be lost.",
             drawBoard.clearAll,
             null);
      }
      public void openButton_Click () {
         string data = FileUtility.openFile();
         if (data != null) {
            overlord.applyFileData(data);
         }
      }
      public void saveButton_Click () {
         FileUtility.saveFile(drawBoard.formSerializedData());
      }

      public void masterToolButton_Click () {
         yesNoDialog.display("Exiting map editor",
             "Are you sure you want to exit the map editor?\nAll unsaved progress will be lost.",
             () => SceneManager.LoadScene(masterSceneName), null);
      }
   }
}