using System;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MapCreationTool.Serialization;
using UnityEngine.EventSystems;

namespace MapCreationTool
{
   public class UI : MonoBehaviour
   {
      const string masterSceneName = "MasterTool";

      [SerializeField]
      private ToolSelect toolSelect = null;
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
      private Toggle snapToGridToggle = null;
      [SerializeField]
      private Dropdown selectionTargetDropdown = null;
      [SerializeField]
      private Button saveButton = null;
      [SerializeField]
      private Text loadedMapText = null;
      [SerializeField]
      private Button newVersionButton = null;
      [SerializeField]
      private Text toolTipText = null;

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
      public static LoadingPanel loadingPanel { get; private set; }
      public static VersionListPanel versionListPanel { get; private set; }
      public static SettingsPanel settingsPanel { get; private set; }
      public static MapDetailsPanel mapDetailsPanel { get; private set; }

      private static UIPanel[] uiPanels;

      private Dictionary<string, Biome.Type> optionsPacks = new Dictionary<string, Biome.Type>
      {
            {"Forest", Biome.Type.Forest },
            {"Desert", Biome.Type.Desert },
            {"Lava", Biome.Type.Lava },
            {"Shroom", Biome.Type.Mushroom },
            {"Pine", Biome.Type.Pine },
            {"Snow", Biome.Type.Snow }
      };

      private void OnEnable () {
         Tools.AnythingChanged += updateAllUI;

         Undo.UndoRegisterd += updateAllUI;
         Undo.UndoPerformed += updateAllUI;
         Undo.RedoPerformed += updateAllUI;
         Undo.LogCleared += updateAllUI;

         DrawBoard.loadedVersionChanged += changeLoadedMapUI;
         toolSelect.OnValueChanged += toolSelect_ValueChanged;
      }

      private void OnDisable () {
         Tools.AnythingChanged -= updateAllUI;

         Undo.UndoRegisterd -= updateAllUI;
         Undo.UndoPerformed -= updateAllUI;
         Undo.RedoPerformed -= updateAllUI;
         Undo.LogCleared -= updateAllUI;

         DrawBoard.loadedVersionChanged -= changeLoadedMapUI;
         toolSelect.OnValueChanged -= toolSelect_ValueChanged;
      }

      private void updateAllUI () {
         undoButton.interactable = Undo.undoCount > 0;
         redoButton.interactable = Undo.redoCount > 0;

         burrowedTreesToggle.isOn = Tools.burrowedTrees;
         toolSelect.setValueNoNotify(Tools.toolType);
         mountainLayerDropdown.value = Tools.mountainLayer;
         fillBoundsDropdown.value = (int) Tools.fillBounds;
         for (int i = 0; i < biomeDropdown.options.Count; i++)
            if (optionsPacks[biomeDropdown.options[i].text] == Tools.biome)
               biomeDropdown.value = i;

         editorTypeDropdown.SetValueWithoutNotify((int) Tools.editorType);

         selectionTargetDropdown.SetValueWithoutNotify((int) Tools.selectionTarget - 1);

         for (int i = 0; i < boardSizes.Length; i++) {
            if (boardSizes[i] == Tools.boardSize.x)
               boardSizeDropdown.SetValueWithoutNotify(i);
         }

         snapToGridToggle.SetIsOnWithoutNotify(Tools.snapToGrid);

         updateShowedOptions();
      }

      private void Awake () {
         canvasScaler = GetComponent<CanvasScaler>();
         yesNoDialog = GetComponentInChildren<YesNoDialog>();
         mapList = GetComponentInChildren<MapListPanel>();
         saveAsPanel = GetComponentInChildren<SaveAsPanel>();
         errorDialog = GetComponentInChildren<ErrorDialog>();
         loadingPanel = GetComponentInChildren<LoadingPanel>();
         versionListPanel = GetComponentInChildren<VersionListPanel>();
         settingsPanel = GetComponentInChildren<SettingsPanel>();
         mapDetailsPanel = GetComponentInChildren<MapDetailsPanel>();

         uiPanels = GetComponentsInChildren<UIPanel>();

         boardSizeDropdown.options = boardSizes.Select(size => new Dropdown.OptionData { text = "Size: " + size }).ToList();
      }

      private void Start () {
         biomeDropdown.options = optionsPacks.Keys.Select(k => new Dropdown.OptionData(k)).ToList();

         updateAllUI();
      }

      private void Update () {
         toolTipText.text = ToolTipManager.currentMessage;
      }

      private void changeLoadedMapUI (MapVersion mapVersion) {
         if (mapVersion == null) {
            newVersionButton.interactable = false;
            loadedMapText.text = "New map";
         } else {
            newVersionButton.interactable = true;
            loadedMapText.text = $"Name: {mapVersion.map.name}, version: {mapVersion.version}";
         }
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

         snapToGridToggle.gameObject.SetActive(Tools.selectedPrefab != null);
         selectionTargetDropdown.gameObject.SetActive(Tools.toolType == ToolType.Selection);
      }

      public static bool anyPanelOpen
      {
         get
         {
            foreach (UIPanel panel in uiPanels) {
               if (panel.showing) {
                  return true;
               }
            }
            return false;
         }
      }

      public static bool anyInputFieldFocused
      {
         get
         {
            if (EventSystem.current.currentSelectedGameObject == null) {
               return false;
            }
            return EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null;
         }
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

      public void toolSelect_ValueChanged (ToolType value) {
         if (Tools.toolType != value)
            Tools.changeTool(value);
      }

      public void snapToGridToggle_ValueChanged () {
         if (Tools.snapToGrid != snapToGridToggle.isOn) {
            Tools.changeSnapToGrid(snapToGridToggle.isOn);
         }
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

      public void selectionTargetDropdown_ValueChanged () {
         if (Tools.selectionTarget != (SelectionTarget) (selectionTargetDropdown.value + 1))
            Tools.changeSelectionTarget((SelectionTarget) (selectionTargetDropdown.value + 1));
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

      public void saveButton_Click () {
         if (!MasterToolAccountManager.canAlterData()) {
            errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (DrawBoard.loadedVersion == null) {
            saveAs();
         } else {
            if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin &&
               DrawBoard.loadedVersion.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
               errorDialog.displayUnauthorized("You are not the creator of this map");
               return;
            }
            saveButton.interactable = false;
            try {
               MapVersion mapVersion = new MapVersion {
                  mapId = DrawBoard.loadedVersion.map.id,
                  version = DrawBoard.loadedVersion.version,
                  createdAt = DrawBoard.loadedVersion.createdAt,
                  updatedAt = DateTime.UtcNow,
                  editorData = DrawBoard.instance.formSerializedData(),
                  gameData = DrawBoard.instance.formExportData(),
                  map = DrawBoard.loadedVersion.map,
                  spawns = DrawBoard.instance.formSpawnList(DrawBoard.loadedVersion.mapId, DrawBoard.loadedVersion.version)
               };

               mapVersion.map.editorType = Tools.editorType;
               mapVersion.map.biome = Tools.biome;

               // Make sure all spawns have unique names
               if (mapVersion.spawns.Count > 0 && mapVersion.spawns.GroupBy(s => s.name).Max(g => g.Count()) > 1) {
                  throw new Exception("Not all spawn names are unique");
               }

               UnityThreading.Task dbTask = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  string dbError = null;
                  try {
                     DB_Main.updateMapVersion(mapVersion);
                  } catch (Exception ex) {
                     dbError = ex.Message;
                  }

                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     if (dbError != null) {
                        errorDialog.display(dbError);
                     } else {
                        DrawBoard.changeLoadedVersion(mapVersion);
                        Overlord.loadAllRemoteData();
                     }
                     saveButton.interactable = true;
                  });
               });
               loadingPanel.display("Saving map version", dbTask);
            } catch (Exception ex) {
               saveButton.interactable = true;
               errorDialog.display(ex.Message);
            }
         }
      }

      public void saveAs () {
         saveAsPanel.open();
      }

      public void openMapList () {
         mapList.open();
      }

      public void openSettings () {
         settingsPanel.open();
      }

      public void newVersion () {
         if (DrawBoard.loadedVersion == null) {
            return;
         }

         if (!MasterToolAccountManager.canAlterData()) {
            errorDialog.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != AdminManager.Type.Admin &&
               DrawBoard.loadedVersion.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            errorDialog.displayUnauthorized("You are not the creator of this map");
            return;
         }

         try {
            MapVersion mapVersion = new MapVersion {
               mapId = DrawBoard.loadedVersion.map.id,
               version = -1,
               createdAt = DateTime.UtcNow,
               updatedAt = DateTime.UtcNow,
               editorData = DrawBoard.instance.formSerializedData(),
               gameData = DrawBoard.instance.formExportData(),
               map = DrawBoard.loadedVersion.map,
               spawns = DrawBoard.instance.formSpawnList(DrawBoard.loadedVersion.mapId, DrawBoard.loadedVersion.version)
            };

            // Make sure all spawns have unique names
            if (mapVersion.spawns.Count > 0 && mapVersion.spawns.GroupBy(s => s.name).Max(g => g.Count()) > 1) {
               throw new Exception("Not all spawn names are unique");
            }

            UnityThreading.Task dbTask = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               MapVersion createdVersion = null;
               try {
                  createdVersion = DB_Main.createNewMapVersion(mapVersion);
               } catch (Exception ex) {
                  dbError = ex.Message;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (dbError != null) {
                     errorDialog.display(dbError);
                  } else {
                     DrawBoard.changeLoadedVersion(createdVersion);
                     Overlord.loadAllRemoteData();
                  }
               });
            });
            loadingPanel.display("Saving map version", dbTask);
         } catch (Exception ex) {
            errorDialog.display(ex.Message);
         }
      }

      public void masterToolButton_Click () {
         yesNoDialog.display("Exiting map editor",
             "Are you sure you want to exit the map editor?\nAll unsaved progress will be lost.",
             () => SceneManager.LoadScene(masterSceneName), null);
      }
   }
}