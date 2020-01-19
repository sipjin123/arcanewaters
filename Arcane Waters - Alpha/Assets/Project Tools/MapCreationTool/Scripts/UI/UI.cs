using System;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

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
      private Dropdown editorTypeDropdown = null;
      [SerializeField]
      private Dropdown boardSizeDropdown = null;
      [SerializeField]
      private Button saveButton = null;

      [SerializeField]
      private Button undoButton = null;
      [SerializeField]
      private Button redoButton = null;

      [SerializeField]
      private DrawBoard drawBoard = null;
      [SerializeField]
      private Overlord overlord = null;

      private CanvasScaler canvasScaler = null;

      private int[] boardSizes = { 64, 128, 256 };

      public static YesNoDialog yesNoDialog { get; private set; }
      public static MapListPanel mapList { get; private set; }
      public static SaveAsPanel saveAsPanel { get; private set; }
      public static ErrorDialog errorDialog { get; private set; }

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

         editorTypeDropdown.SetValueWithoutNotify((int) Tools.editorType);

         for (int i = 0; i < boardSizes.Length; i++) {
            if (boardSizes[i] == Tools.boardSize.x)
               boardSizeDropdown.SetValueWithoutNotify(i);
         }

         updateShowedOptions();
      }
      private void Awake () {
         canvasScaler = GetComponent<CanvasScaler>();
         yesNoDialog = GetComponentInChildren<YesNoDialog>();
         mapList = GetComponentInChildren<MapListPanel>();
         saveAsPanel = GetComponentInChildren<SaveAsPanel>();
         errorDialog = GetComponentInChildren<ErrorDialog>();

         boardSizeDropdown.options = boardSizes.Select(size => new Dropdown.OptionData { text = "Size: " + size }).ToList();
      }
      private void Start () {
         biomeDropdown.options = optionsPacks.Keys.Select(k => new Dropdown.OptionData(k)).ToList();

         updateAllUI();
      }
      private void updateShowedOptions () {
         mountainLayerDropdown.gameObject.SetActive(
             Tools.toolType == ToolType.Brush &&
             Tools.tileGroup != null &&
             (Tools.tileGroup.type == TileGroupType.Mountain || Tools.tileGroup.type == TileGroupType.SeaMountain));

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

      public void editorTypeDropdown_ValueChanged () {
         if (Tools.editorType != (EditorType) editorTypeDropdown.value)
            yesNoDialog.display(
             "Editor type change",
             "Are you sure you want to change the editor type?\nAll unsaved progress will be permanently lost.",
             () => Tools.changeEditorType((EditorType) editorTypeDropdown.value),
             updateAllUI);
      }

      public void fillBoundsDropDown_ValueChanged () {
         if (Tools.fillBounds != (FillBounds) fillBoundsDropdown.value)
            Tools.changeFillBounds((FillBounds) fillBoundsDropdown.value);
      }

      public void boardSizeDropdown_ValueChanged () {
         Vector2Int dropdownSize = new Vector2Int(boardSizes[boardSizeDropdown.value], boardSizes[boardSizeDropdown.value]);

         if (Tools.boardSize != dropdownSize) {
            yesNoDialog.display(
             "Board size change",
             "Are you sure you want to change the size of the board?\nAll unsaved progress will be permanently lost.",
             () => Tools.changeBoardSize(dropdownSize),
             updateAllUI);
         }
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
             drawBoard.newMap,
             null);
      }

      public void openButton_Click () {
         string data = FileUtility.openFile();
         if (data != null) {
            yesNoDialog.display(
             "Opening a map",
             "Are you sure you want to open a map?\nAll unsaved progress will be permanently lost.",
             () => overlord.applyFileData(data, null),
             null);
         }
      }

      public void saveButton_Click () {
         #if IS_SERVER_BUILD

         if (DrawBoard.loadedMapName == null) {
            saveAs();
         } else {
            saveButton.interactable = false;
            try {
               MapDTO map = new MapDTO {
                  name = DrawBoard.loadedMapName,
                  editorData = DrawBoard.instance.formSerializedData(),
                  gameData = DrawBoard.instance.formExportData()
               };

               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  string dbError = null;
                  try {
                     DB_Main.updateMapData(map);
                  } catch (MySqlException ex) {
                     if (ex.Number == 1062) {
                        dbError = $"Map with name '{map.name}' already exists";
                     } else {
                        dbError = ex.Message;
                     }
                  }

                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     if (dbError != null) {
                        errorDialog.display(dbError);
                     }
                     saveButton.interactable = true;
                  });
               });
            } catch (Exception ex) {
               saveButton.interactable = true;
               errorDialog.display(ex.Message);
            }
         }

         #endif
      }

      public void saveAs () {
         saveAsPanel.open();
      }

      public void openMapList () {
         mapList.open();
      }

      public void masterToolButton_Click () {
         yesNoDialog.display("Exiting map editor",
             "Are you sure you want to exit the map editor?\nAll unsaved progress will be lost.",
             () => SceneManager.LoadScene(masterSceneName), null);
      }
   }
}