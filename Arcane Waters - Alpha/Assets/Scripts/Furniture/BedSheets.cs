using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BedSheets : ClientMonoBehaviour {
   #region Public Variables

   // The bed sheets sprite
   public SpriteRenderer sheets;

   // The Y offset when the sheets are raised
   public float raisedOffset = -.24f;

   // The Y offset when the sheets are lowered
   public float loweredOffset = 0f;

   // Gets set to true when the sheets are lowered
   public bool sheetsAreLowered = false;

   // The speed at which we move the sheets
   public float animationSpeed = 1f;

   #endregion

   private void Update () {
      // Find the desired offset for the bed sheets
      float desiredOffset = sheetsAreLowered ? loweredOffset : raisedOffset;

      // Check how far we are from the desired offset
      float currentOffset = sheets.transform.localPosition.y;
      float difference = desiredOffset - currentOffset;

      // Move towards the desired offset
      if (Mathf.Abs(difference) > .01f) {
         currentOffset += difference * Time.deltaTime * animationSpeed;
         Util.setLocalY(sheets.transform, currentOffset);
      }
   }

   void OnTriggerEnter2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      sheetsAreLowered = false;

      // Play a sound
      SoundManager.create3dSound("door_cloth_close", this.transform.position);
   }

   void OnTriggerExit2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      sheetsAreLowered = true;

      // Play a sound
      SoundManager.create3dSound("door_cloth_open", this.transform.position);
   }

   #region Private Variables

   #endregion
}
