using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;

public class VoyagePanel : Panel
{
   #region Public Variables

   // The mode of the panel
   public enum Mode { None = 0, MapSelection = 1, ModeSelection = 2 }

   // The container for the map cells
   public GameObject mapCellsContainer;

   // The container for the selected map
   public GameObject selectedMapContainer;

   // The prefab we use for creating map cells
   public VoyageMapCell mapCellPrefab;

   // The section displayed when selecting a map
   public GameObject mapSection;

   // The section displayed when selecting the voyage mode
   public GameObject voyageModeSection;

   // The toggles for the different voyage modes
   public Toggle quickmatchToggle;
   public Toggle privateToggle;

   // Self
   public static VoyagePanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void displayVoyagesSelection () {
      Global.player.rpc.Cmd_RequestVoyageListFromServer();
   }

   public void updatePanelWithVoyageList (List<Voyage> voyageList) {
      // Clear out any old info
      mapCellsContainer.DestroyChildren();

      // Instantiate the cells
      foreach (Voyage voyage in voyageList) {
         VoyageMapCell cell = Instantiate(mapCellPrefab, mapCellsContainer.transform, false);
         cell.setCellForVoyage(voyage);
      }

      // Set the panel mode if it has not been initialized yet
      configurePanelForMode(Mode.MapSelection);
   }

   public void selectVoyageMap (Voyage voyage) {
      _selectedVoyage = voyage;

      // Switch to voyage mode selection
      configurePanelForMode(Mode.ModeSelection);

      // Clear the displayed map
      selectedMapContainer.DestroyChildren();

      // Set the selected map
      VoyageMapCell cell = Instantiate(mapCellPrefab, selectedMapContainer.transform, false);
      cell.setCellForVoyage(voyage);
      cell.disablePointerEvents();

      // Selects the voyage mode
      switch (_selectedVoyageMode) {
         case Voyage.Mode.Quickmatch:
            OnQuickmatchModeButtonClickedOn();
            break;
         case Voyage.Mode.Private:
            OnPrivateModeButtonClickedOn();
            break;
         default:
            break;
      }
   }

   public void OnQuickmatchModeButtonClickedOn () {
      quickmatchToggle.isOn = true;
      privateToggle.isOn = false;
      _selectedVoyageMode = Voyage.Mode.Quickmatch;
   }

   public void OnPrivateModeButtonClickedOn () {
      quickmatchToggle.isOn = false;
      privateToggle.isOn = true;
      _selectedVoyageMode = Voyage.Mode.Private;
   }

   public void OnStartButtonClickedOn () {
      if (VoyageGroupPanel.self.isShowing()) {
         // Associate a new function with the confirmation button
         PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
         PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmStartVoyage());

         // Show a confirmation panel
         PanelManager.self.confirmScreen.show("You will automatically leave your current voyage group. Are you sure?");
      } else {
         confirmStartVoyage();
      }
   }

   public void confirmStartVoyage () {
      // Hide the confirm panel
      PanelManager.self.confirmScreen.hide();

      // Join a group with the selected mode
      switch (_selectedVoyageMode) {
         case Voyage.Mode.Quickmatch:
            Global.player.rpc.Cmd_AddUserToQuickmatchVoyageGroup(_selectedVoyage.areaKey);
            break;
         case Voyage.Mode.Private:
            Global.player.rpc.Cmd_CreatePrivateVoyageGroup(_selectedVoyage.areaKey);
            break;
         default:
            break;
      }
   }

   private void configurePanelForMode (Mode mode) {
      switch (mode) {
         case Mode.MapSelection:
            mapSection.SetActive(true);
            voyageModeSection.SetActive(false);
            break;
         case Mode.ModeSelection:
            mapSection.SetActive(false);
            voyageModeSection.SetActive(true);
            break;
         default:
            break;
      }
      _currentMode = mode;
   }

   #region Private Variables

   // The data of the selected map
   private Voyage _selectedVoyage = null;

   // The selected voyage mode
   private Voyage.Mode _selectedVoyageMode = Voyage.Mode.Quickmatch;

   // The current panel mode
   private Mode _currentMode = Mode.None;

   #endregion
}