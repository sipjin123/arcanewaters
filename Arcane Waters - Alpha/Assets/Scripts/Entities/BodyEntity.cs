using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BodyEntity : NetEntity
{
   #region Public Variables

   // Our Item Managers
   public ArmorManager armorManager;
   public WeaponManager weaponManager;

   #endregion

   protected override void Awake () {
      base.Awake();

      _bodyLayer = GetComponentInChildren<BodyLayer>();
      _eyesLayer = GetComponentInChildren<EyesLayer>();
      _armorLayer = GetComponentInChildren<ArmorLayer>();
      _hairLayers = GetComponentsInChildren<HairLayer>();
      _waterChecker = GetComponentInChildren<WaterChecker>();
   }

   protected override void Start () {
      base.Start();

      // Set our sprite sheets according to our types
      StartCoroutine(CO_UpdateAllSprites());

      // Set our name to something meaningful
      this.name = "Body (user " + userId + ")";

      // Keep track in our Body Manager
      BodyManager.self.storeBody(this);
   }

   protected override void Update() {
      base.Update();

      // Note if we just finished falling
      checkFallDirection();
   }

   public override void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, ShipInfo shipInfo) {
      base.setDataFromUserInfo(userInfo, armor, weapon, shipInfo);
      
      this.armorManager.updateArmorSyncVars(Armor.castItemToArmor(armor));
      this.weaponManager.updateWeaponSyncVars(Weapon.castItemToWeapon(weapon));
   }

   public override Armor getArmorCharacteristics () {
      return new Armor(0, armorManager.armorType, armorManager.palette1, armorManager.palette2);
   }

   public override Weapon getWeaponCharacteristics () {
      return new Weapon(0, weaponManager.weaponType, weaponManager.palette1, weaponManager.palette2);
   }

   public void updateHair (HairLayer.Type newHairType, string newHairPalette1, string newHairPalette2) {
      foreach (HairLayer hairLayer in _hairLayers) {
         hairLayer.setType(newHairType);

         // Update colors
         hairLayer.recolor(newHairPalette1, newHairPalette2);
      }
   }

   public void updateBodySpriteSheets () {
      // Assign the types
      _bodyLayer.setType(bodyType);
      _eyesLayer.setType(eyesType);

      // Update both the back and front hair layers
      foreach (HairLayer hairLayer in _hairLayers) {
         hairLayer.setType(hairType);

         // Update colors
         hairLayer.recolor(hairPalette1);
      }

      // Update colors
      _eyesLayer.recolor(eyesPalette1);

      if (!Util.isEmpty(this.entityName)) {
         this.nameText.text = this.entityName;
      }
   }

   public void restartAnimations () {
      foreach (Animator animator in GetComponents<Animator>()) {
         animator.StopPlayback();
         animator.StartPlayback();
      }
   }

   protected void checkFallDirection() {
      // Check if we just finished fallin
      if (this.fallDirection == 0 && this.fallDirection != _previousFallDirection) {
         StartCoroutine(CO_ShowFallEffect());
      }

      // Note our fall direction for the next frame
      _previousFallDirection = this.fallDirection;
   }

   protected IEnumerator CO_UpdateAllSprites () {
      // Wait until we receive data
      while (Util.isEmpty(this.entityName)) {
         yield return null;
      }

      updateBodySpriteSheets();
      this.armorManager.updateSprites(this.armorManager.armorType, this.armorManager.palette1, this.armorManager.palette2);
      this.weaponManager.updateSprites(this.weaponManager.weaponType, this.weaponManager.palette1, this.weaponManager.palette2);
   }

   protected IEnumerator CO_ShowFallEffect () {
      yield return new WaitForSeconds(.1f);

      // Create a little splash of water or poof of dust
      if (_waterChecker.inWater()) {
         EffectManager.self.create(Effect.Type.Crop_Water, this.sortPoint.transform.position);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(Attack.ImpactMagnitude.None), this.sortPoint.transform.position, Quaternion.identity);
         SoundManager.create3dSound("ledge", this.sortPoint.transform.position);

         // Shake the camera
         if (isLocalPlayer) {
            CameraManager.shakeCamera(.02f);
         }
      }
   }

   #region Private Variables

   // Our Sprite Layer objects
   protected BodyLayer _bodyLayer;
   protected EyesLayer _eyesLayer;
   protected ArmorLayer _armorLayer;
   protected HairLayer[] _hairLayers;

   // Our Water Checker
   protected WaterChecker _waterChecker;

   // Our fall direction in the previous frame
   protected int _previousFallDirection;

   #endregion
}
