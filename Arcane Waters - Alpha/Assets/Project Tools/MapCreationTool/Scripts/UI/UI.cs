﻿using System;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MapCreationTool.Serialization;
using UnityEngine.EventSystems;
using System.IO;

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
      private GameObject eraserSizeSlider = null;
      [SerializeField]
      private GameObject brushSizeSlider = null;
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

      private int[] boardSizes = { 64, 128, 256 };

      public static YesNoDialog yesNoDialog { get; private set; }
      public static MapListPanel mapList { get; private set; }
      public static SaveAsPanel saveAsPanel { get; private set; }
      public static MessagePanel messagePanel { get; private set; }
      public static LoadingPanel loadingPanel { get; private set; }
      public static VersionListPanel versionListPanel { get; private set; }
      public static SettingsPanel settingsPanel { get; private set; }
      public static MapDetailsPanel mapDetailsPanel { get; private set; }
      public static AdditionalInputPanel additionalInputField { get; private set; }
      public static MapObjectStateVariables.ObjectStateEditorPanel objectStateEditorPanel { get; private set; }

      // Reference to self
      public static UI self;

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
         self = this;
         yesNoDialog = GetComponentInChildren<YesNoDialog>();
         mapList = GetComponentInChildren<MapListPanel>();
         saveAsPanel = GetComponentInChildren<SaveAsPanel>();
         messagePanel = GetComponentInChildren<MessagePanel>();
         loadingPanel = GetComponentInChildren<LoadingPanel>();
         versionListPanel = GetComponentInChildren<VersionListPanel>();
         settingsPanel = GetComponentInChildren<SettingsPanel>();
         mapDetailsPanel = GetComponentInChildren<MapDetailsPanel>();
         objectStateEditorPanel = GetComponentInChildren<MapObjectStateVariables.ObjectStateEditorPanel>();
         additionalInputField = GetComponentInChildren<AdditionalInputPanel>();

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

         eraserSizeSlider.gameObject.SetActive(Tools.toolType == ToolType.Eraser);
         brushSizeSlider.gameObject.SetActive(Tools.toolType == ToolType.Brush);

         burrowedTreesToggle.gameObject.SetActive(
             Tools.toolType == ToolType.Brush &&
             Tools.tileGroup != null && Tools.tileGroup.type == TileGroupType.TreePrefab);

         fillBoundsDropdown.gameObject.SetActive(Tools.toolType == ToolType.Fill);

         snapToGridToggle.gameObject.SetActive(Tools.selectedPrefab != null || Tools.toolType == ToolType.Move);
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
            yesNoDialog.displayIfMapStateModified(
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
            // Commented out dialog; Instead - just change size without losing data until user leaves
            Tools.changeBoardSize(dropdownSize);

            //yesNoDialog.displayIfMapStateModified(
            // "Board size change",
            // "Are you sure you want to change the size of the board?\nAll unsaved progress will be permanently lost.",
            // () => Tools.changeBoardSize(dropdownSize),
            // updateAllUI);
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

      public void recordAndSaveGif () {
         ScreenRecorder.recordGif(saveGif);
      }

      private void saveGif (byte[] bytes) {
         saveScreenShot(bytes, "gif");
      }

      public void recordAndSavePng () {
         saveScreenShot(ScreenRecorder.recordPng(), "png");
      }

      private void saveScreenShot (byte[] bytes, string extension) {
         try {
            string mapName = DrawBoard.loadedVersion == null ? "Unsaved Map" : DrawBoard.loadedVersion.map.name;
            string path = getNewScreenShotPath(mapName, extension);

            File.WriteAllBytes(path, bytes);
            messagePanel.displayInfo("Image saved", "Path:\n" + path);
         } catch (Exception ex) {
            messagePanel.displayError("There was an error writing the image to your pictures folder. Exception:\n" + ex);
         }
      }

      private string getNewScreenShotPath (string mapName, string extension) {
         string pathName = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + @"\" + mapName;

         for (int i = 0; i < 1000; i++) {
            if (!File.Exists(pathName + " " + i + ".gif") && !File.Exists(pathName + " " + i + ".png")) {
               return pathName + " " + i + "." + extension;
            }
         }

         throw new Exception($"Too many saved screenshot for map { mapName }");
      }

      public void newButton_Click () {
         yesNoDialog.displayIfMapStateModified(
             "New map",
             "Are you sure you want to start a new map?\nAll unsaved progress will be lost.",
             drawBoard.newMap,
             null);
      }

      public void saveButton_Click () {
         string warnings = Overlord.instance.getWarnings();
         if (string.IsNullOrEmpty(warnings)) {
            saveWarningsConfirm();
         } else {
            yesNoDialog.display("Are you sure you want to save?", "Map has warnings:" + Environment.NewLine + warnings, saveWarningsConfirm, null, messagePanel.warningColor);
         }
      }

      private void saveWarningsConfirm () {
         if (!MasterToolAccountManager.canAlterData()) {
            messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (DrawBoard.loadedVersion == null) {
            saveAs();
         } else {
            if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin &&
               DrawBoard.loadedVersion.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
               messagePanel.displayUnauthorized("You are not the creator of this map");
               return;
            }

            additionalInputField.open(
               "Describe changes",
               "Describe the changes that were made",
               () => saveProcess_commentAdded(additionalInputField.string1Input),
               null);
         }
      }

      private void saveProcess_commentAdded (string comment) {
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

            if (!Overlord.validateMap(out string errors)) {
               throw new Exception("Failed validating a map:" + Environment.NewLine + Environment.NewLine + errors);
            }

            UnityThreading.Task dbTask = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               try {
                  DB_Main.updateMapVersion(mapVersion, Tools.biome, Tools.editorType, Overlord.authUserId, comment, true);
               } catch (Exception ex) {
                  dbError = ex.Message;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (dbError != null) {
                     messagePanel.displayError(dbError);
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
            messagePanel.displayError(ex.Message);
            Debug.Log(ex);
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
            messagePanel.displayUnauthorized("Your account type has no permissions to alter data");
            return;
         }

         if (MasterToolAccountManager.PERMISSION_LEVEL != PrivilegeType.Admin &&
               DrawBoard.loadedVersion.map.creatorID != MasterToolAccountManager.self.currentAccountID) {
            messagePanel.displayUnauthorized("You are not the creator of this map");
            return;
         }

         additionalInputField.open(
               "Describe changes",
               "Describe the changes that were made",
               () => newMapVersion_commentAdded(additionalInputField.string1Input),
               null);
      }

      private void newMapVersion_commentAdded (string comment) {
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

            if (!Overlord.validateMap(out string errors)) {
               throw new Exception("Failed validating a map:" + Environment.NewLine + Environment.NewLine + errors);
            }

            UnityThreading.Task dbTask = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               string dbError = null;
               MapVersion createdVersion = null;
               try {
                  createdVersion = DB_Main.createNewMapVersion(mapVersion, Tools.biome, Overlord.authUserId, comment);
               } catch (Exception ex) {
                  dbError = ex.Message;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (dbError != null) {
                     messagePanel.displayError(dbError);
                  } else {
                     DrawBoard.changeLoadedVersion(createdVersion);
                     Overlord.loadAllRemoteData();
                  }
               });
            });
            loadingPanel.display("Saving map version", dbTask);
         } catch (Exception ex) {
            messagePanel.displayError(ex.Message);
            Debug.Log(ex);
         }
      }

      public void masterToolButton_Click () {
         yesNoDialog.displayIfMapStateModified("Exiting map editor",
             "Are you sure you want to exit the map editor?\nAll unsaved progress will be lost.",
             () => SceneManager.LoadScene(masterSceneName), null);
      }
   }
}