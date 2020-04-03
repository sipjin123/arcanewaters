using UnityEngine;
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
      // If a player tried to plant on this spot holding a seed bag, maybe plant something
      if (this.crop == null && (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.PlantCrop) {
         Global.player.Cmd_PlantCrop((Crop.Type)(Global.player as PlayerBodyEntity).weaponManager.actionTypeValue, this.cropNumber);
      }

      // If the player tried to water this spot holding the watering pot, maybe water something
      if (this.crop != null && (Global.player as PlayerBodyEntity).weaponManager.actionType == Weapon.ActionType.WaterCrop && !this.crop.isMaxLevel() && crop.isReadyForWater()) {
         Global.player.Cmd_WaterCrop(this.cropNumber);
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
