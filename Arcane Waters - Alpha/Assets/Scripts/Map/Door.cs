using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class Door : ClientMonoBehaviour {
   #region Public Variables

   // The child objects that we manage
   public SpriteRenderer doorboard;

   // The door collider
   public GameObject doorCollider;

   // Set to true for interior doors
   public bool isInterior = false;

   // Set to true for locked doors
   public bool isLocked = false;

   #endregion

   void Start () {
      // Look up our collider
      _boxCollider = GetComponent<BoxCollider2D>();

      // Routinely check for entities nearby on the client
      InvokeRepeating("checkNearbyEntities", 0f, 1f);
   }

   private void Update () {
      doorCollider.SetActive(isLocked);
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      // If any entity comes near the door, increase the nearby count
      if (player != null) {
         _nearbyNetIds.Add(player.netId);

         // Either open or close the door now
         updateDoorForNearbyCount();

         // If it's our player, play a sound
         if (player.isLocalPlayer && isLocked) {
            SoundManager.create3dSound("door_locked", Global.player.transform.position);
         }
      }
   }

   void OnTriggerExit2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      // If any entity leaves the door, decrease the nearby count
      if (player != null && _nearbyNetIds.Contains(player.netId)) {
         _nearbyNetIds.Remove(player.netId);

         // Either open or close the door now
         updateDoorForNearbyCount();
      }
   }

   protected void checkNearbyEntities () {
      _nearbyNetIds.Clear();

      // Get an updated count of nearby players
      Collider2D[] colliders = Physics2D.OverlapAreaAll(_boxCollider.bounds.min, _boxCollider.bounds.max);

      foreach (Collider2D collider in colliders) {
         NetEntity entity = collider.GetComponent<NetEntity>();

         if (entity != null) {
            _nearbyNetIds.Add(entity.netId);
         }
      }

      // Either open or close the door now
      updateDoorForNearbyCount();
   }

   protected void updateDoorForNearbyCount () {
      if (_nearbyNetIds.Count > 0) {
         open();
      } else {
         close();
      }
   }

   protected void playDoorOpenSound () {
      Area area = GetComponentInParent<Area>();

      // Different sounds based on the area type
      if (Area.DESERT_TOWN.Equals(area.areaKey)) {
         SoundManager.create3dSound("door_cloth_open", this.transform.position);
      } else {
         SoundManager.create3dSound("door_open_", this.transform.position, 3);
      }
   }

   protected void playDoorCloseSound () {
      Area area = GetComponentInParent<Area>();

      // Different sounds based on the area type
      if (Area.DESERT_TOWN.Equals(area.areaKey)) {
         SoundManager.create3dSound("door_cloth_close", this.transform.position);
      } else {
         SoundManager.create3dSound("door_close_", this.transform.position, 3);
      }
   }

   protected void open () {
      // Don't open the door if it's locked
      if (isLocked) {
         return;
      }

      // Make the door disappear, if it's not already open
      if (doorboard != null && doorboard.enabled) {
         doorboard.enabled = false;

         // Play the door sound effect
         playDoorOpenSound();
      }
   }

   protected void close () {
      // Make the door visible
      if (doorboard != null) {
         // Interior doors have a slight delay before they actually close
         float delay = isInterior ? INTERIOR_DOOR_CLOSE_DELAY : 0f;
         StartCoroutine(CO_Close(delay));
      }
   }

   protected IEnumerator CO_Close (float delay) {
      yield return new WaitForSeconds(delay);

      // Make sure there's still no one nearby, and the door isn't already closed
      if (_nearbyNetIds.Count <= 0 && !doorboard.enabled) {
         doorboard.enabled = true;

         // Play the door sound effect
         playDoorCloseSound();
      }
   }

   #region Private Variables

   // How long we wait before closing the door
   protected static float INTERIOR_DOOR_CLOSE_DELAY = .200f;

   // Our box collider
   protected BoxCollider2D _boxCollider;

   // The number of bodies near the door
   protected HashSet<uint> _nearbyNetIds = new HashSet<uint>();

   #endregion
}
