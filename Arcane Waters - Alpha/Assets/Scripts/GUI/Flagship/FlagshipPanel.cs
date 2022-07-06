using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class FlagshipPanel : Panel {
   #region Public Variables

   // The prefab we use for creating rows
   public FlagshipRow rowPrefab;

   // The container for our rows 
   public GameObject scrollViewContent;

   // This user's flagship ID
   public static int playerFlagshipId;

   // Reference to the ship ability tooltip
   public ShipAbilityTooltip shipAbilityTooltip;

   // Blocks panel during server communication
   public GameObject loadBlocker;

   #endregion

   public override void show () {
      base.show();
   }

   public void updatePanelWithShips (List<ShipInfo> shipList, int flagshipId, int level, int sailorLevel, List<ShipRefundData> refundableShips) {
      playerFlagshipId = flagshipId;

      // Clear out any old info
      scrollViewContent.DestroyChildren();

      for (int i = 0; i < shipList.Count; i++) {
         // Look up the info for this row
         ShipInfo shipInfo = shipList[i];

         D.adminLog("----> Loaded ship with id:{" + shipInfo.shipId + "} Stats:" +
            "{Dmg:" + (shipInfo.damage * 100).ToString("f1") + "% HP:" + shipInfo.maxHealth.ToString("f1") + "}" +
            "{T:" + shipInfo.shipType + " S:" + shipInfo.speed + "}", D.ADMIN_LOG_TYPE.ShipPurchase);

         // Create a new row
         FlagshipRow row = Instantiate(rowPrefab, scrollViewContent.transform, false);
         row.transform.SetParent(scrollViewContent.transform, false);

         int refundAmount = 0;
         bool canBeRefunded = false;
         ShipRefundData refundData = refundableShips.Find(_ => _.shipId == shipInfo.shipId);
         if (refundData != null) {
            canBeRefunded = true;
            refundAmount = refundData.shipRefundAmount;
         }
         row.setRowForItem(shipInfo, level, sailorLevel, canBeRefunded, refundAmount);
      }
      loadBlocker.SetActive(false);
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
