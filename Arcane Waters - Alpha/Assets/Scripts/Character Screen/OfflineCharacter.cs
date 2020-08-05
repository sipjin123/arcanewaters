using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class OfflineCharacter : ClientMonoBehaviour {
   #region Public Variables

   // The user ID associated with this character, if any
   public int userId;

   // The gender
   public Gender.Type genderType = Gender.Type.Male;

   // The various layers
   public HairLayer hairBack;
   public WeaponLayer weaponBack;
   public BodyLayer body;
   public EyesLayer eyes;
   public ArmorLayer armor;
   public HairLayer hairFront;
   public WeaponLayer weaponFront;
   public HatLayer hatLayer;

   // The hair layers
   public List<HairLayer> hairLayers;

   // The weapon layers
   public List<WeaponLayer> weaponLayers;

   // The name text
   public TextMeshProUGUI nameText;

   // The level text
   public TextMeshProUGUI levelText;

   // Whether or not this character is in creation mode
   public bool creationMode = false;

   // Our associated Character creation canvas group
   public CanvasGroup creationCanvasGroup;

   // The creation name input field
   public InputField nameInputField;

   // The sort point
   public GameObject sortPoint;

   // The spot this character is in
   public CharacterSpot spot;

   // The content holder showing the character and the loading indicator
   public GameObject contentHolder, contentLoader;

   #endregion

   private void Start () {
      // If we just started creating a new character, then update the panel to reflect our gender
      if (this.creationMode) {
         CharacterCreationPanel.self.setCharacterBeingCreated(this);
         setTextsVisible(false);
      }
   }

   private void Update () {
      creationCanvasGroup.alpha = creationMode ? 1f : 0f;
      creationCanvasGroup.blocksRaycasts = creationMode;
   }

   public void setDataAndLayers (UserInfo userInfo, Item weapon, Item armor, Item hat, string armorPalettes) {
      if (PaletteSwapManager.self.hasInitialized) {
         setInternalDataAndLayers(userInfo, weapon, armor, hat, armorPalettes);
      } else {
         PaletteSwapManager.self.paletteCompleteEvent.AddListener(() => {
            setInternalDataAndLayers(userInfo, weapon, armor, hat, armorPalettes);
         });
      }
   }

   private void OnDestroy () {
      PaletteSwapManager.self.paletteCompleteEvent.RemoveAllListeners();
   }

   public void setTextsVisible (bool isVisible) {
      levelText.gameObject.SetActive(isVisible);
      nameText.gameObject.SetActive(isVisible);
   }

   private void setInternalDataAndLayers (UserInfo userInfo, Item weapon, Item armor, Item hat, string armorPalettes) {
      contentHolder.SetActive(true);
      contentLoader.SetActive(false);
    
      this.userId = userInfo.userId;
      setBodyLayers(userInfo);

      ArmorStatData armorData = ArmorStatData.getDefaultData();
      if (armor.data != "") {
         armorData = Util.xmlLoad<ArmorStatData>(armor.data);
         armorData.palettes = armor.paletteNames;
      }

      HatStatData hatData = EquipmentXMLManager.self.getHatData(hat.itemTypeId);
      if (hatData == null) {
         hatData = HatStatData.getDefaultData();
         hatData.palettes = hat.paletteNames;
      }

      setArmor(armorData.armorType, armorData.palettes, ArmorStatData.serializeArmorStatData(armorData));
      setHat(hatData.hatType, hatData.palettes, HatStatData.serializeHatStatData(hatData));
      setWeapon(userInfo, weapon);
   }

   public void setBodyLayers (UserInfo userInfo) {
      this.genderType = userInfo.gender;

      this.nameText.text = userInfo.username;
      this.levelText.text = "Level " + LevelUtil.levelForXp(userInfo.XP);

      // Assign the types
      body.setType(userInfo.bodyType);
      eyes.setType(userInfo.eyesType);

      // Set the colors we're going to allow
      CharacterCreationPanel.self.updateColorBoxes(userInfo.gender);

      // Update both the back and front hair layers
      foreach (HairLayer hairLayer in hairLayers) {
         hairLayer.setType(userInfo.hairType);

         // Update colors
         hairLayer.recolor(userInfo.hairPalettes);
      }

      // Update colors
      eyes.recolor(userInfo.eyesPalettes);
   }

   public void setWeapon (UserInfo userInfo, Item weapon) {
      WeaponStatData weaponData = WeaponStatData.getDefaultData();
      if (weapon.data != "") {
         weaponData = Util.xmlLoad<WeaponStatData>(weapon.data);
      }

      // Cache string data
      _weaponData = WeaponStatData.serializeWeaponStatData(weaponData);

      // Update our Material
      foreach (WeaponLayer weaponLayer in weaponLayers) {
         weaponLayer.setType(userInfo.gender, weaponData.weaponType);
         weaponLayer.recolor(weapon.paletteNames);
      }
   }

   public void setHat (int hatType, string paletteNames, string data = "") {
      // Set the correct sheet for our gender and hat type
      hatLayer.setType(this.genderType, hatType, true);

      // Cache string data
      _hatData = data;

      // Update our Material
      hatLayer.recolor(paletteNames);
   }

   public void setArmor (int armorType, string paletteNames, string data = "") {
      // Set the correct sheet for our gender and armor type
      armor.setType(this.genderType, armorType, true);

      // Cache string data
      _armorData = data;

      // Update our Material
      armor.recolor(paletteNames);
   }

   public void cancelCreating () {
      Destroy(this.gameObject);
   }

   public UserInfo getUserInfo () {
      CharacterSpot spot = GetComponentInParent<CharacterSpot>();

      UserInfo info = new UserInfo();
      info.gender = this.genderType;
      info.userId = this.userId;
      info.username = CharacterCreationPanel.self.nameText.text;
      info.charSpot = spot.number;
      info.hairType = this.hairFront.getType();
      info.hairPalettes = this.hairFront.getPalettes();
      info.eyesType = this.eyes.getType();
      info.eyesPalettes = this.eyes.getPalettes();
      info.bodyType = this.body.getType();

      return info;
   }

   public Armor getArmor() {
      Armor armor = new Armor();
      armor.itemTypeId = this.armor.getType();
      armor.paletteNames = this.armor.getPalettes();
      armor.category = Item.Category.Armor;
      armor.count = 1;
      armor.data = _armorData;
      if (_armorData.Length < 1 && armor.itemTypeId != 0) {
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armor.itemTypeId);
         armor.data = ArmorStatData.serializeArmorStatData(armorData);
      }

      return armor;
   }

   public Weapon getWeapon () {
      Weapon weapon = new Weapon();
      weapon.itemTypeId = this.weaponFront.getType();
      weapon.paletteNames = this.weaponFront.getPalettes();
      weapon.category = Item.Category.Weapon;
      weapon.count = 1;
      weapon.data = _weaponData;
      if (_weaponData.Length < 1 && weapon.itemTypeId != 0) {
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);
         weapon.data = WeaponStatData.serializeWeaponStatData(weaponData);
      }

      return weapon;
   }

   public Hat getHat () {
      Hat hat = new Hat();
      hat.itemTypeId = this.hatLayer.getType();
      hat.paletteNames = this.hatLayer.getPalettes();
      hat.category = Item.Category.Hats;
      hat.count = 1;
      hat.data = _hatData;
      if (_hatData.Length < 1 && hat.itemTypeId != 0) {
         HatStatData hatData = EquipmentXMLManager.self.getHatData(hat.itemTypeId);
         hat.data = HatStatData.serializeHatStatData(hatData);
      }

      return hat;
   }

   #region Private Variables

   // The cached string data of each equipment
   private string _armorData, _weaponData, _hatData;

   #endregion
}
