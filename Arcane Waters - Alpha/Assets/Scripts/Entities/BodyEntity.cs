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
   public HatManager hatsManager;

   #endregion

   protected override void Awake () {
      base.Awake();

      _bodyLayer = GetComponentInChildren<BodyLayer>();
      _eyesLayer = GetComponentInChildren<EyesLayer>();
      _armorLayer = GetComponentInChildren<ArmorLayer>();
      _hatLayer = GetComponentInChildren<HatLayer>();
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

   protected override void Update () {
      base.Update();

      // Note if we just finished falling
      checkFallDirection();
   }

   public override void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, Item hat, ShipInfo shipInfo, GuildInfo guildInfo, GuildRankInfo guildRankInfo) {
      base.setDataFromUserInfo(userInfo, armor, weapon, hat, shipInfo, guildInfo, guildRankInfo);
      this.armorManager.updateArmorSyncVars(armor.itemTypeId, armor.id, armor.paletteNames, armor.durability, true);
      this.weaponManager.updateWeaponSyncVars(weapon.itemTypeId, weapon.id, weapon.paletteNames, weapon.durability, weapon.count, true);
      this.hatsManager.updateHatSyncVars(hat.itemTypeId, hat.id, hat.paletteNames, true);
   }

   public override Armor getArmorCharacteristics () {
      ArmorStatData armorData = EquipmentXMLManager.self.armorStatList.Find(_ => _.armorType == armorManager.armorType);

      if (armorData == null) {
         return new Armor();
      }

      return new Armor(0, armorData.sqlId, armorManager.palettes);
   }

   public override Weapon getWeaponCharacteristics () {
      WeaponStatData weaponData = EquipmentXMLManager.self.weaponStatList.Find(_ => _.weaponType == weaponManager.weaponType);

      if (weaponData == null) {
         return new Weapon();
      }

      return new Weapon(0, weaponData.sqlId, weaponManager.palettes);
   }

   public override Hat getHatCharacteristics () {
      HatStatData hatData = EquipmentXMLManager.self.hatStatList.Find(_ => _.hatType == hatsManager.hatType);

      if (hatData == null) {
         return new Hat();
      }

      return new Hat(0, hatData.sqlId, hatsManager.palettes);
   }

   public void updateHair (HairLayer.Type newHairType, string newHairPalettes) {
      foreach (HairLayer hairLayer in _hairLayers) {
         hairLayer.setType(newHairType);

         // Update colors
         hairLayer.recolor(newHairPalettes);
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
         hairLayer.recolor(hairPalettes);
      }

      // Update colors
      _eyesLayer.recolor(eyesPalettes);

      if (nameText != null && !Util.isEmpty(this.entityName)) {
         nameText.text = this.entityName;
         recolorNameText();
      }
   }

   public virtual void recolorNameText () {
   }

   public void restartAnimations () {
      foreach (Animator animator in _animators) {
         if (!_ignoredAnimators.Contains(animator) && animator.gameObject.activeInHierarchy) {
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0.0f);
         }
      }
   }

   protected void checkFallDirection () {
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

      this.armorManager.updateSprites(this.armorManager.armorType, this.armorManager.palettes);
      this.weaponManager.updateSprites(this.weaponManager.weaponType, this.weaponManager.palettes);
      this.hatsManager.updateSprites(this.hatsManager.hatType, this.hatsManager.palettes);
      updateBodySpriteSheets();
   }

   protected IEnumerator CO_ShowFallEffect () {
      yield return new WaitForSeconds(.1f);

      // Create a little splash of water or poof of dust
      if (_waterChecker.inWater()) {
         EffectManager.self.create(Effect.Type.Crop_Water, this.sortPoint.transform.position);
      } else {
         Instantiate(PrefabsManager.self.poofPrefab, this.sortPoint.transform.position, Quaternion.identity);
         // Shake the camera
         if (isLocalPlayer) {
            CameraManager.shakeCamera(.02f);
         }
      }

      // Play jump landing sound effect
      SoundEffectManager.self.playJumpLandSfx(this.sortPoint.transform.position, this.areaKey);
   }

   #region Private Variables

   // Our Sprite Layer objects
   protected BodyLayer _bodyLayer;
   protected EyesLayer _eyesLayer;
   protected ArmorLayer _armorLayer;
   protected HatLayer _hatLayer;
   protected HairLayer[] _hairLayers;

   // Our Water Checker
   protected WaterChecker _waterChecker;

   // Our fall direction in the previous frame
   protected int _previousFallDirection;

   #endregion
}
