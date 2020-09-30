using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropPickup : MonoBehaviour {
   #region Public Variables

   // The crop spot associated with this pickable crop
   public CropSpot cropSpot;

   // The sprite renderer reference
   public SpriteRenderer spriteRender;

   // The crop collected prefab
   public CropCollected cropCollected;

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null && collision.GetComponent<PlayerBodyEntity>() == Global.player) {
         cropSpot.cropPickupLocation = transform.position;
         Global.player.Cmd_HarvestCrop(cropSpot.cropNumber);

         // Play a sound
         SoundManager.play2DClip(SoundManager.Type.Harvesting_Picking);

         // Spawn collect harvest effect
         CropCollected cropCollectedInstance = Instantiate(cropCollected, transform.position, transform.rotation);
         cropCollectedInstance.gameObject.SetActive(true);
         cropCollectedInstance.spriteRenderer.sprite = spriteRender.sprite;
         Destroy(cropCollectedInstance.gameObject, 2);

         Destroy(gameObject);
      } 
   }

   #region Private Variables

   #endregion
}
