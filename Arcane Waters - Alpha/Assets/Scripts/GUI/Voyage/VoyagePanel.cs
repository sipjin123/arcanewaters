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

   // The container for the map cells
   public GameObject mapCellsContainer;

   // The prefab we use for creating map cells
   public VoyageMapCell mapCellPrefab;

   // Self
   public static VoyagePanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void displayVoyagesSelection () {
      Global.player.rpc.Cmd_RequestGroupInstanceListFromServer();
   }

   public void updatePanelWithVoyageList (List<GroupInstance> voyageList) {
      // Clear out any old info
      mapCellsContainer.DestroyChildren();

      // Sort the list by creation date
      voyageList = voyageList.OrderByDescending(v => v.creationDate).ToList();

      // Instantiate the cells
      foreach (GroupInstance voyage in voyageList) {
         VoyageMapCell cell = Instantiate(mapCellPrefab, mapCellsContainer.transform, false);
         cell.setCellForVoyage(voyage, () => selectVoyageMap(voyage));
      }

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenVoyagePanel);
   }

   public void selectVoyageMap (GroupInstance groupInstance) {
      PanelManager.self.showConfirmationPanel("Please confirm your destination: " + groupInstance.areaName + ".",
         () => confirmJoinVoyage(groupInstance));
   }

   public void confirmJoinVoyage (GroupInstance groupInstance) {
      if (GroupManager.isInGroup(Global.player)) {
         Global.player.rpc.Cmd_JoinVoyageWithGroup(groupInstance.groupInstanceId);
      } else {
         Global.player.rpc.Cmd_JoinVoyageAlone(groupInstance.groupInstanceId);
      }

      // Close the panel
      PanelManager.self.hideCurrentPanel();
   }

   #region Private Variables

   #endregion
}