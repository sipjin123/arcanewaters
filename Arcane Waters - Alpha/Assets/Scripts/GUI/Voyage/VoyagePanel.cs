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
      Global.player.rpc.Cmd_RequestVoyageListFromServer();
   }

   public void updatePanelWithVoyageList (List<Voyage> voyageList) {
      // Clear out any old info
      mapCellsContainer.DestroyChildren();

      // Sort the list by creation date
      voyageList = voyageList.OrderByDescending(v => v.creationDate).ToList();

      // Instantiate the cells
      foreach (Voyage voyage in voyageList) {
         VoyageMapCell cell = Instantiate(mapCellPrefab, mapCellsContainer.transform, false);
         cell.setCellForVoyage(voyage);
      }
   }

   public void selectVoyageMap (Voyage voyage) {
      // Make sure the variables are captured for the click events
      int voyageId = voyage.voyageId;

      PanelManager.self.showConfirmationPanel("Please confirm your destination: " + Area.getName(voyage.areaKey) + ".",
         () => confirmJoinVoyage(voyageId), () => PanelManager.self.confirmScreen.hide(), true);
   }

   public void confirmJoinVoyage (int voyageId) {
      if (VoyageManager.isInGroup(Global.player)) {
         Global.player.rpc.Cmd_JoinVoyageWithGroup(voyageId);
      } else {
         Global.player.rpc.Cmd_JoinVoyageAlone(voyageId);
      }
   }

   #region Private Variables

   #endregion
}