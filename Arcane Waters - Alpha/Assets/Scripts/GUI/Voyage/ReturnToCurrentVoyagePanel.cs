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

   // The prefab we use for creating voyage map cells
   public VoyageMapCell mapCellPrefab;

   // The prefab we use for creating pvp map cells
   public PvpArenaCell pvpArenaCellPrefab;

   // Self
   public static ReturnToCurrentVoyagePanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void updatePanelWithCurrentVoyage (GroupInstance groupInstance) {
      // Clear out any old info
      mapCellContainer.DestroyChildren();
      VoyageTriggerPopup.self.disableAllPanels();

      // Instantiate the cell
      if (groupInstance.isPvP) {
         PvpArenaCell cell = Instantiate(pvpArenaCellPrefab, mapCellContainer.transform, false);
         cell.setCellForPvpArena(groupInstance, () => warpToCurrentVoyageMap());
      } else {
         VoyageMapCell cell = Instantiate(mapCellPrefab, mapCellContainer.transform, false);
         cell.setCellForVoyage(groupInstance, () => warpToCurrentVoyageMap());
      }
   }

   public void warpToCurrentVoyageMap () {
      Global.player.rpc.Cmd_WarpToCurrentVoyageMap();
      PanelManager.self.hideCurrentPanel();
   }

   public void onLeaveVoyageButtonPressed () {
      GroupPanel.self.OnLeaveGroupButtonClickedOn();
   }

   #region Private Variables

   #endregion
}