using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class FlagshipPanel : Panel, IPointerClickHandler {
   #region Public Variables

   // The prefab we use for creating rows
   public FlagshipRow rowPrefab;

   // The container for our rows 
   public GameObject scrollViewContent;

   // This user's flagship ID
   public static int playerFlagshipId;

   #endregion

   public override void show () {
      base.show();

      // Show the list of ships we own
      Global.player.rpc.Cmd_RequestShipsFromServer();
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   public void updatePanelWithShips (List<ShipInfo> shipList, int flagshipId) {
      playerFlagshipId = flagshipId;

      // Clear out any old info
      scrollViewContent.DestroyChildren();

      for (int i = 0; i < shipList.Count; i++) {
         // Look up the info for this row
         ShipInfo shipInfo = shipList[i];

         // Create a new row
         FlagshipRow row = Instantiate(rowPrefab, scrollViewContent.transform, false);
         row.transform.SetParent(scrollViewContent.transform, false);
         row.setRowForItem(shipInfo);
      }
   }

   protected ShipInfo getShipInfo (int shipId) {
      foreach (FlagshipRow row in scrollViewContent.GetComponentsInChildren<FlagshipRow>()) {
         if (row.shipInfo.shipId == shipId) {
            return row.shipInfo;
         }
      }

      return null;
   }

   #region Private Variables

   #endregion
}
