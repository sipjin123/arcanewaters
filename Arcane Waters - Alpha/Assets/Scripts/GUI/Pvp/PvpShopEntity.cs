using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class PvpShopEntity : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The shop id
   public int shopId;

   // The object that enables when highlighted
   public GameObject highlightGameObject;

   // The team type
   public PvpTeamType pvpTeamType;

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField dataField in dataFields) {
         if (dataField.k.CompareTo(DataField.SHOP_ID) == 0) {
            try {
               int intVal = int.Parse(dataField.v.Split(':')[0]);
               shopId = intVal;
            } catch {
            }
         }
         if (dataField.k.CompareTo(DataField.PVP_TEAM_TYPE) == 0) {
            try {
               PvpTeamType pvpVal = (PvpTeamType) System.Enum.Parse(typeof(PvpTeamType), dataField.v.Split(':')[0]);
               pvpTeamType = pvpVal;
            } catch {

            }
         }
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      PlayerShipEntity playerEntity = collision.GetComponent<PlayerShipEntity>();
      if (Global.player != null && playerEntity != null) {
         if (Global.player.userId == playerEntity.userId && playerEntity.pvpTeam == pvpTeamType) {
            PvpShopPanel.self.shopId = shopId;
            PvpShopPanel.self.enableShopButton(true);
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      PlayerShipEntity playerEntity = collision.GetComponent<PlayerShipEntity>();
      if (Global.player != null && playerEntity != null) {
         if (Global.player.userId == playerEntity.userId && playerEntity.pvpTeam == pvpTeamType) {
            PvpShopPanel.self.enableShopButton(false);
         }
      }
   }

   public void onPointerEnter () {
      highlightGameObject.SetActive(true);
   }

   public void onPointerExit () {
      highlightGameObject.SetActive(false);
   }

   #region Private Variables

   #endregion
}