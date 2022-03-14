﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;

public class CropSpot : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating Crop instances
   public Crop cropPrefab;

   // The Crop in this spot, if any
   public Crop crop;

   // The number that identifies this spot
   public int cropNumber;

   // The pickable crops
   [XmlIgnore]
   public Vector3 cropPickupLocation;

   // The area key
   public string areaKey;

   // Obj indicating the interaction of the crop spot
   public GameObject indicatorObj;

   #endregion

   private void Start () {
      // Store references
      _renderer = GetComponent<SpriteRenderer>();

      // When running the server, we don't need to do anything else
      if (Util.isBatch()) {
         this.gameObject.SetActive(false);
      }

      CropSpotManager.self.storeCropSpot(this);
   }

   private void Update() {
      // Only show the hole sprite if there's no Crop planted here yet
      float alpha = (crop == null) ? (_renderer.color.a + Time.smoothDeltaTime * .5f) : 0f;
      Util.setAlpha(_renderer, alpha);
   }

   public void tryToInteractWithCropOnClient () {
      bool hasFarmingPermissions = (AreaManager.self.isFarmOfUser(areaKey, Global.player.userId) || CustomGuildMapManager.canUserFarm(areaKey, Global.player));

      // If a player tried to plant on this spot holding a seed bag, plant a seed
      if (this.crop == null && (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.PlantCrop && hasFarmingPermissions) {
         Global.player.Cmd_PlantCrop((Crop.Type)(Global.player as PlayerBodyEntity).weaponManager.actionTypeValue, this.cropNumber, Global.player.areaKey);
         EffectManager.self.create(Effect.Type.Crop_Harvest, transform.position);
         EffectManager.self.create(Effect.Type.Crop_Dirt_Large, transform.position);

         // Play a sound
         //SoundManager.create3dSound("crop_plant_", transform.position, 5);
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.CROP_PLANT, transform.position);
      }

      // If the player tried to water this spot holding the watering pot, water the crop
      if (this.crop != null && (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.WaterCrop && !this.crop.isMaxLevel() && crop.isReadyForWater(true)) {
         D.adminLog("Client is trying to water crop", D.ADMIN_LOG_TYPE.Crop);
         Global.player.Cmd_WaterCrop(this.cropNumber);
      }
   }

   public bool isGlobalPlayerNearby () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) <= .24f);
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      tryAutoFarm(collision);
   }

   private void tryAutoFarm (Collider2D collision) {
      
      // If the local player walked over this crop spot
      PlayerBodyEntity player = collision.GetComponent<PlayerBodyEntity>();
      if (Global.autoFarm && player && Global.player && player == Global.player && !player.interactingAnimation && AreaManager.isFarmingAllowed(player.areaKey)) {
         
         bool triggeredAction = false;
         bool hasFarmingPermissions = (AreaManager.self.isFarmOfUser(player.areaKey, player.userId) || CustomGuildMapManager.canUserFarm(areaKey, player));

         // If the player is holding seeds, try plant seeds here
         if (player.weaponManager.actionType == Weapon.ActionType.PlantCrop && !crop && hasFarmingPermissions) {
            player.playFastInteractAnimation(transform.position, true);
            player.Cmd_PlantCrop((Crop.Type) player.weaponManager.actionTypeValue, cropNumber, player.areaKey);

            // Show effects on the crop spot
            ExplosionManager.createFarmingParticle(Weapon.ActionType.PlantCrop, transform.position, 1.5f, particleCount: 4);
            EffectManager.self.create(Effect.Type.Crop_Harvest, transform.position);
            EffectManager.self.create(Effect.Type.Crop_Dirt_Large, transform.position);

            // Play a sound
            //SoundManager.create3dSound("crop_plant_", transform.position, 5);
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.CROP_PLANT, transform.position);

            triggeredAction = true;
         }

         // If the player is holding a watering can, try to water this plot
         if (player.weaponManager.actionType == Weapon.ActionType.WaterCrop && crop && !crop.isMaxLevel() && crop.isReadyForWater()) {
            player.playFastInteractAnimation(transform.position, true);
            player.Cmd_WaterCrop(this.cropNumber);
            ExplosionManager.createFarmingParticle(Weapon.ActionType.WaterCrop, transform.position, 1.5f, 2, false, 30, 60);
            triggeredAction = true;
         }

         // If the player is holding a pitchfork, try to harvest this plot
         if (player.weaponManager.actionType == Weapon.ActionType.HarvestCrop && crop && crop.isMaxLevel() && !crop.hasBeenHarvested() && hasFarmingPermissions) {
            player.playFastInteractAnimation(transform.position, true);
            harvestCrop();
            triggeredAction = true;
         }

         if (triggeredAction) {
            // Play weapon SFX upon triggering animation
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(player.weaponManager.equipmentDataId);
            SoundEffectManager.self.playInteractionSfx(weaponData.actionType, weaponData.weaponClass, weaponData.sfxType, transform.position);
            //WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(player.weaponManager.equipmentDataId);
            //if (weaponData != null && weaponData.actionSfxDirectory.Length > 1) {
            //   SoundManager.create3dSoundWithPath(weaponData.actionSfxDirectory, transform.position);
            //}
         }
      }
   }

   public void harvestCrop () {
      if (!Global.player || !crop) {
         return;
      }
      
      PlayerBodyEntity player = Global.player.getPlayerBodyEntity();
      player.Cmd_BroadcastCropProjectile(crop.getCropInfo());
   }

   #region Private Variables

   // Our associated Sprite
   protected SpriteRenderer _renderer;

   #endregion
}
