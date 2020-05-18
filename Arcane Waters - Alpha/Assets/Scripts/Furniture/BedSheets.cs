using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class BedSheets : ClientMonoBehaviour
{
   #region Public Variables

   // The bed sheets sprite
   public SpriteRenderer sheets;

   // Sprite height when sheets are raised
   public float raisedHeight = 0.48f;

   // Sprite height when sheets are lowered
   public float loweredHeight = 0.24f;

   // Gets set to true when the sheets are lowered
   public bool sheetsAreLowered = false;

   [Tooltip("Sheet lowered state - from 0(fully lowered) to 1(fully up)")]
   public float sheetValue = 1f;

   // Sheet lower speed - how many fully animations in a second
   public float animationSpeed = 1f;

   #endregion

   private void OnValidate () {
      sheetValue = Mathf.Clamp(sheetValue, 0, 1f);

      if (sheets != null) {
         updateSheetSprite(sheets, sheetValue);
      }
   }

   private void Update () {
      // Find target sheet value
      float targetValue = sheetsAreLowered ? 0 : 1f;

      // Check if there is no change
      if (targetValue == sheetValue) return;

      // Move towards target value
      sheetValue = Mathf.MoveTowards(sheetValue, targetValue, animationSpeed * Time.deltaTime);

      updateSheetSprite(sheets, sheetValue);
   }

   private void updateSheetSprite (SpriteRenderer sheets, float value) {
      // Find target height
      float h = Mathf.Lerp(loweredHeight, raisedHeight, value);

      sheets.size = new Vector2(sheets.size.x, h);
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
