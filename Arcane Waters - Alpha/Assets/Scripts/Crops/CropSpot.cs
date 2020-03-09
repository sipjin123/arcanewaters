﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropSpot : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating Crop instances
   public Crop cropPrefab;

   // The Crop in this spot, if any
   public Crop crop;

   // The number that identifies this spot
   public int cropNumber;

   #endregion

   private void Start () {
      // Store references
      _renderer = GetComponent<SpriteRenderer>();

      // When running the server, we don't need to do anything else
      if (Application.isBatchMode) {
         this.gameObject.SetActive(false);
      }
   }

   private void Update() {
      // Only show the hole sprite if there's no Crop planted here yet
      float alpha = (crop == null) ? (_renderer.color.a + Time.smoothDeltaTime * .5f) : 0f;
      Util.setAlpha(_renderer, alpha);

      // Allow pressing keyboard to interact with the crop
      if (InputManager.isActionKeyPressed() && isGlobalPlayerNearby()) {
         tryToInteractWithCropOnClient();
      }
   }

   public void tryToInteractWithCropOnClient () {
      // The player has to be close enough
      if (!isGlobalPlayerNearby()) {
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      // If a player tried to plant on this spot holding a seed bag, maybe plant something
      if (this.crop == null && (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.PlantCrop) {
         Global.player.Cmd_PlantCrop(CropManager.STARTING_CROP, this.cropNumber);
      }

      // If the player tried to water this spot holding the watering pot, maybe water something
      if (this.crop != null && (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.WaterCrop && !this.crop.isMaxLevel() && crop.isReadyForWater()) {
         Global.player.Cmd_WaterCrop(this.cropNumber);
      }

      // If the player tried to harvest this spot holding the pitchfork, maybe harvest something
      if (this.crop != null && (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.HarvestCrop && this.crop.isMaxLevel()) {
         Global.player.Cmd_HarvestCrop(this.cropNumber);
      }
   }

   public bool isGlobalPlayerNearby () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) <= .24f);
   }

   #region Private Variables

   // Our associated Sprite
   protected SpriteRenderer _renderer;

   #endregion
}
