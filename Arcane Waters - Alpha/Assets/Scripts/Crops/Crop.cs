using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class Crop : ClientMonoBehaviour {
   #region Public Variables

   // The type of Crop
   public enum Type { None = 0,
      Potatoes = 1, Onions = 2, Carrots = 3, Corn = 4, Garlic = 5,
      Lettuce = 6, Peas = 7, Chilies = 8, Radishes = 9, Tomatoes = 10,
   }

   // The type of Crop
   public Type cropType;

   // The user who owns the crop
   public int userId;

   // The spot number for this crop
   public int cropNumber;

   // The time the crop was planted
   public long creationTime;

   // The time the crop was last watered
   public long lastWaterTimestamp;

   // The amount of time we have to wait between watering
   public float waterInterval;

   // The current growth level of this crop
   public int growthLevel;

   // Our floating water icon
   public SpriteRenderer floatingWaterIcon;

   // Our floating harvest icon
   public SpriteRenderer floatingHarvestIcon;

   // Our floating water level container
   public GameObject waterLevelContainer;

   // The bar that shows when this crop is ready for water
   public Image waterBar;

   // Our simple animation component
   [HideInInspector]
   public SimpleAnimation anim;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      anim = GetComponent<SimpleAnimation>();
   }

   private void Update () {
      float currentWaterAlpha = floatingWaterIcon.color.a;
      float currentHarvestAlpha = floatingHarvestIcon.color.a;

      // If we're ready to be watered, fade in a water icon
      Util.setAlpha(floatingWaterIcon, isReadyForWater() ?
         currentWaterAlpha + Time.smoothDeltaTime : currentWaterAlpha - Time.smoothDeltaTime);

      // If we're ready to be harvested, fade in a harvest icon
      Util.setAlpha(floatingHarvestIcon, isMaxLevel() ?
         currentHarvestAlpha + Time.smoothDeltaTime : currentHarvestAlpha - Time.smoothDeltaTime);

      // Update our water level display
      updateWaterLevelDisplay();
   }

   public override bool Equals (object rhs) {
      if (rhs is Crop) {
         var other = rhs as Crop;
         return cropType == other.cropType && userId == other.userId && cropNumber == other.cropNumber && creationTime == other.creationTime;
      }
      return false;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + cropType.GetHashCode();
         hash = hash * 23 + userId.GetHashCode();
         hash = hash * 23 + cropNumber.GetHashCode();
         hash = hash * 23 + creationTime.GetHashCode();
         return hash;
      }
   }

   public int getMaxGrowthLevel () {
      return getMaxGrowthLevel(this.cropType);
   }

   public static int getMaxGrowthLevel (Crop.Type cropType) {
      switch (cropType) {
         case Type.Carrots:
            return 3;
         default:
            return 3;
      }
   }

   public bool isMaxLevel () {
      return (this.growthLevel >= getMaxGrowthLevel());
   }

   public bool isReadyForWater () {
      if (isMaxLevel()) {
         return false;
      }

      return (TimeManager.self.getLastServerUnixTimestamp() - lastWaterTimestamp) > this.waterInterval;
   }

   public static int getXP (Crop.Type cropType) {
      // For now, we'll have all crops be worth the same XP, and have the varying supply/demand be what creates variety
      return 25;

      /*switch (cropType) {
         case Type.Carrots:
            return 25;
         case Type.Onions:
            return 50;
         case Type.Potatoes:
            return 100;
      }

      return 25;*/
   }

   protected void updateWaterLevelDisplay () {
      // There are several situations where we don't need to show the water display at all
      if (Global.player == null || this.isMaxLevel() || this.isReadyForWater() || !isPlayerNear() || !isPlayerHoldingWaterCan()) {
         waterLevelContainer.SetActive(false);
         return;
      }

      // Make sure the container shows
      waterLevelContainer.SetActive(true);

      // Check how close we are to being ready for water
      float waterReadiness = (TimeManager.self.getLastServerUnixTimestamp() - lastWaterTimestamp) / this.waterInterval;

      // Update the water bar size
      waterBar.fillAmount = waterReadiness;
   }

   protected bool isPlayerNear () {
      if (Global.player == null) {
         return false;
      }

      return Vector2.Distance(Global.player.transform.position, this.transform.position) < 3f;
   }

   protected bool isPlayerHoldingWaterCan () {
      if (Global.player != null && Global.player is BodyEntity) {
         BodyEntity playerBody = (BodyEntity) Global.player;

         return (playerBody.weaponManager.weaponType == Weapon.Type.WateringPot) ;
      }

      return false;
   }

   #region Private Variables

   #endregion
}
