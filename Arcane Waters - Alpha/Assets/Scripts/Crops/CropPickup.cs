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

   // The shadow rendered underneath crop
   public GameObject shadow;

   #endregion

   private void Start () {
      _creationTime = NetworkTime.time;
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      if (collision.GetComponent<PlayerBodyEntity>() != null && collision.GetComponent<PlayerBodyEntity>() == Global.player) {
         cropSpot.cropPickupLocation = transform.position;
         Global.player.Cmd_HarvestCrop(cropSpot.cropNumber);

         // Play a sound
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.PICKUP_CROP, transform);
         //SoundManager.play2DClip(SoundManager.Type.Harvesting_Picking);

         // Spawn collect harvest effect
         CropCollected cropCollectedInstance = Instantiate(cropCollected, transform.position, transform.rotation);
         cropCollectedInstance.gameObject.SetActive(true);
         cropCollectedInstance.spriteRenderer.sprite = spriteRender.sprite;
         Destroy(cropCollectedInstance.gameObject, 2);

         Destroy(gameObject);
      } 
   }

   private void Update () {
      // If the pickup has been around long enough, move it towards the player
      if (Global.player != null && (NetworkTime.time - _creationTime) > GRAVITATE_DELAY) {
         float distanceToPlayer = Vector2.Distance(Global.player.transform.position, transform.position);

         // Only move it towards the player if it's within a certain radius
         if (distanceToPlayer <= MAX_GRAVITATE_DISTANCE) {
            float speed = MAX_GRAVITATE_DISTANCE / distanceToPlayer;
            Vector2 newPosition = Vector2.Lerp(transform.position, Global.player.transform.position, Time.deltaTime * speed);
            Util.setXY(transform, newPosition);
         }
      }
   }

   #region Private Variables

   // When this pickup was created
   private double _creationTime;

   // The max distance at which this pickup will gravitate towards the player
   private const float MAX_GRAVITATE_DISTANCE = 1f;

   // How long this pickup must have existed for, before it begins gravitating to the player
   private const float GRAVITATE_DELAY = 1.0f;

   #endregion
}
