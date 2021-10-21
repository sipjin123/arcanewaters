using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class WishingWell : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The shop id reference
   public int pvpShopId;

   // If within trigger distance
   public bool isWithinDistance;

   #endregion

   private void Awake () {
      _outline = GetComponentInChildren<SpriteOutline>();
   }

   public void clientClickedMe () {
      if (isWithinDistance) {
         PvpShopPanel.self.shopId = pvpShopId;
         PvpShopPanel.self.onShopButtonPressed(false);
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      PlayerBodyEntity playerBody = collision.GetComponent<PlayerBodyEntity>();
      if (Global.player != null && playerBody != null && Global.player.userId == playerBody.userId) {
         isWithinDistance = true;
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      PlayerBodyEntity playerBody = collision.GetComponent<PlayerBodyEntity>();
      if (Global.player != null && playerBody != null && Global.player.userId == playerBody.userId) {
         isWithinDistance = false;
      }
   }

   public void onHoverEnter () {
      if (_outline == null) {
         return;
      }

      _outline.setVisibility(true);
   }

   public void onHoverExit () {
      if (_outline == null) {
         return;
      }

      _outline.setVisibility(false);
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField dataField in dataFields) {
         if (dataField.k.CompareTo(DataField.SHOP_ID) == 0) {
            try {
               int intVal = int.Parse(dataField.v.Split(':')[0]);
               pvpShopId = intVal;
            } catch {
            }
         }
      }
   }

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;

   #endregion
}
