using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BodyEntity : NetEntity
{
   #region Public Variables

   // The types associated with our sprite layers
   [SyncVar]
   public BodyLayer.Type bodyType;
   [SyncVar]
   public EyesLayer.Type eyesType;
   [SyncVar]
   public HairLayer.Type hairType;

   // Our colors
   [SyncVar]
   public ColorType eyesColor1;
   [SyncVar]
   public ColorType hairColor1;
   [SyncVar]
   public ColorType hairColor2;

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

   public void setDataFromUserInfo (UserInfo userInfo, Armor armor, Weapon weapon) {
      this.entityName = userInfo.username;
      this.adminFlag = userInfo.adminFlag;
      this.classType = userInfo.classType;
      this.specialty = userInfo.specialty;
      this.faction = userInfo.faction;
      this.guildId = this.guildId = userInfo.guildId;

      // Body
      this.gender = userInfo.gender;
      this.hairColor1 = userInfo.hairColor1;
      this.hairColor2 = userInfo.hairColor2;
      this.hairType = userInfo.hairType;
      this.eyesType = userInfo.eyesType;
      this.eyesColor1 = userInfo.eyesColor1;
      this.bodyType = userInfo.bodyType;
      this.armorManager.updateArmorSyncVars(armor);
      
      this.weaponManager.updateWeaponSyncVars(weapon);
   }

   public void updateHair (HairLayer.Type newHairType, ColorType newHairColor1, ColorType newHairColor2) {
      foreach (HairLayer hairLayer in _hairLayers) {
         hairLayer.setType(newHairType);

         // Update colors
         ColorKey hairColorKey = new ColorKey(gender, Layer.Hair);
         hairLayer.recolor(hairColorKey, newHairColor1, newHairColor2);
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
         ColorKey hairColorKey = new ColorKey(gender, Layer.Hair);
         hairLayer.recolor(hairColorKey, hairColor1, hairColor2);
      }

      // Update colors
      ColorKey eyeColorKey = new ColorKey(gender, Layer.Eyes);
      _eyesLayer.recolor(eyeColorKey, eyesColor1, 0);

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
      this.armorManager.updateSprites(this.armorManager.armorType, this.armorManager.color1, this.armorManager.color2);
      this.weaponManager.updateSprites(this.weaponManager.weaponType, this.weaponManager.color1, this.weaponManager.color2);
   }

   protected IEnumerator CO_ShowFallEffect () {
      yield return new WaitForSeconds(.1f);

      // Create a little splash of water or poof of dust
      if (_waterChecker.inWater()) {
         EffectManager.self.create(Effect.Type.Crop_Water, this.sortPoint.transform.position);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(Attack.ImpactMagnitude.Weak), this.sortPoint.transform.position, Quaternion.identity);
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
