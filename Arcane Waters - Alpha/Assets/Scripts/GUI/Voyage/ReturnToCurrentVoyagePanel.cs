using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;

public class ReturnToCurrentVoyagePanel : Panel
{
   #region Public Variables

   // The container for the map cell
   public GameObject mapCellContainer;

   // The prefab we use for creating map cells
   public VoyageMapCell mapCellPrefab;

   // Self
   public static ReturnToCurrentVoyagePanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void updatePanelWithCurrentVoyage (Voyage voyage) {
      // Clear out any old info
      mapCellContainer.DestroyChildren();

      // Instantiate the cell
      VoyageMapCell cell = Instantiate(mapCellPrefab, mapCellContainer.transform, false);
      cell.setCellForVoyage(voyage, () => warpToCurrentVoyageMap());
   }

   public void warpToCurrentVoyageMap () {
      Global.player.rpc.Cmd_WarpToCurrentVoyageMap();
      PanelManager.self.unlinkPanel();
   }

   public void onLeaveVoyageButtonPressed () {
      VoyageGroupPanel.self.OnLeaveGroupButtonClickedOn();
   }

   #region Private Variables

   #endregion
}