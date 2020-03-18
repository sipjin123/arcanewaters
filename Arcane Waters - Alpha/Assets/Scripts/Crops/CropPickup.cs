using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropPickup : MonoBehaviour {
   #region Public Variables

   // The crop spot associated with this pickable crop
   public CropSpot cropSpot;

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null && collision.GetComponent<PlayerBodyEntity>() == Global.player) {
         cropSpot.cropPickupLocation = transform.position;
         Global.player.Cmd_HarvestCrop(cropSpot.cropNumber);
         Destroy(gameObject);
      } 
   }

   #region Private Variables

   #endregion
}
