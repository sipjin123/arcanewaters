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

      if (cropSpot.crop != null) {
         _targetEntity = EntityManager.self.getEntity(cropSpot.crop.userId);
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      
      // Only the player who owns this farm is able to pick up this crop
      PlayerBodyEntity player = collision.GetComponent<PlayerBodyEntity>();
      if (player != null && cropSpot.crop != null && player.userId == cropSpot.crop.userId) {
         cropSpot.cropPickupLocation = transform.position;

         // We will only notify the server to harvest the crop from the client who harvested it
         if (player.userId == Global.player.userId) {
            Global.player.Cmd_HarvestCrop(cropSpot.cropNumber);
         }

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
      if (_targetEntity == null) {
         gameObject.SetActive(false);
         return;
      }

      // If the pickup has been around long enough, move it towards the player
      if (_targetEntity != null && (NetworkTime.time - _creationTime) > GRAVITATE_DELAY) {
         float distanceToPlayer = Vector2.Distance(_targetEntity.transform.position, transform.position);

         // Only move it towards the player if it's within a certain radius
         if (distanceToPlayer <= MAX_GRAVITATE_DISTANCE) {
            float speed = MAX_GRAVITATE_DISTANCE / distanceToPlayer;
            Vector2 newPosition = Vector2.Lerp(transform.position, _targetEntity.transform.position, Time.deltaTime * speed);
            Util.setXY(transform, newPosition);
         }
      }
   }

   #region Private Variables

   // When this pickup was created
   private double _creationTime;

   // A reference to the entity we are gravitating toward
   private NetEntity _targetEntity;

   // The max distance at which this pickup will gravitate towards the player
   private const float MAX_GRAVITATE_DISTANCE = 1f;

   // How long this pickup must have existed for, before it begins gravitating to the player
   private const float GRAVITATE_DELAY = 1.0f;

   #endregion
}
