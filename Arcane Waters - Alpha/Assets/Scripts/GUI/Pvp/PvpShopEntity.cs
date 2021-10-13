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

   // The object that shows the radius of the collision
   public GameObject radiusGameObject;

   // The team type
   public PvpTeamType pvpTeamType;

   // The building of the shop
   public GameObject buildingObject;

   // Reference to the colliders
   public BoxCollider2D solidColliderRef;
   public CircleCollider2D triggerColliderRef;

   // If this object is active
   public bool isActive;

   // The renderer
   public SpriteRenderer currentRenderer;

   // The north facing sprite replacement
   public Sprite northSprite;

   #endregion

   public void enableShop (bool isEnabled) {
      radiusGameObject.SetActive(isEnabled);
      isActive = isEnabled;
      triggerColliderRef.enabled = isEnabled;

      if (isEnabled && !gameObject.activeInHierarchy) {
         gameObject.SetActive(true);
      }
   }

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
         
         if (dataField.k.CompareTo(DataField.HAS_SHOP_BUILDING) == 0) {
            string rawData = dataField.v.Split(':')[0];
            bool hasBuildingDisplay = rawData.ToLower() == "true" ? true : false;
            buildingObject.SetActive(hasBuildingDisplay);
            solidColliderRef.gameObject.SetActive(hasBuildingDisplay);
         }
         if (dataField.k.CompareTo(DataField.IS_FACING_NORTH) == 0) {
            string rawData = dataField.v.Split(':')[0];
            bool isFacingNorth = rawData.ToLower() == "true" ? true : false;
            if (isFacingNorth) {
               currentRenderer.sprite = northSprite;
            }
         }
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      if (!isActive) {
         return;
      }
      
      PlayerShipEntity playerEntity = collision.GetComponent<PlayerShipEntity>();
      if (Global.player != null && playerEntity != null) {
         if (Global.player.userId == playerEntity.userId && playerEntity.pvpTeam == pvpTeamType && !PvpShopPanel.self.shopButton.activeInHierarchy) {
            PvpShopPanel.self.shopId = shopId;
            PvpShopPanel.self.enableShopButton(true);
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      if (!isActive) {
         return;
      }

      PlayerShipEntity playerEntity = collision.GetComponent<PlayerShipEntity>();
      if (Global.player != null && playerEntity != null) {
         if (Global.player.userId == playerEntity.userId && playerEntity.pvpTeam == pvpTeamType && PvpShopPanel.self.shopButton.activeInHierarchy) {
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