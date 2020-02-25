using UnityEngine;
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
   }

   void OnTriggerEnter2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null || !player.isLocalPlayer) {
         return;
      }

      // If the player entered this spot holding the watering pot, maybe water something
      if (this.crop != null && player.weaponManager.actionType == Weapon.ActionType.WaterCrop && !this.crop.isMaxLevel() && crop.isReadyForWater()) {
         player.Cmd_WaterCrop(this.cropNumber);
      }

      // If the player entered this spot holding the pitchfork, maybe harvest something
      if (this.crop != null && player.weaponManager.actionType == Weapon.ActionType.HarvestCrop && this.crop.isMaxLevel()) {
         player.Cmd_HarvestCrop(this.cropNumber);
      }

      // If a player entered this spot holding a seed bag, maybe plant something
      if (this.crop == null && player.weaponManager.actionType == Weapon.ActionType.PlantCrop) {
         player.Cmd_PlantCrop(CropManager.STARTING_CROP, this.cropNumber);
      }
   }

   #region Private Variables

   // Our associated Sprite
   protected SpriteRenderer _renderer;

   #endregion
}
