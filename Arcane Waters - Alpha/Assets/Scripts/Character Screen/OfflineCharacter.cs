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
   public TextMeshPro nameText;

   // The level text
   public TextMeshPro levelText;

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

   // The guild icon of the user
   public GuildIcon guildIcon;

   // Guild Icon GameObject
   public GameObject guildIconGameObject;

   // The left rotate button
   public Button leftRotateButton;

   // The right rotate button
   public Button rightRotateButton;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Turn off guild icon to start
      guildIconGameObject.SetActive(false);
   }

   private void Start () {
      // If we just started creating a new character, then update the panel to reflect our gender
      if (this.creationMode) {
         setTextsVisible(false);
      }

      // Get a reference to the buttons that rotate the character
      foreach (Button button in this.gameObject.transform.parent.GetComponentsInChildren<Button>()) {
         if (button.name.ToLower().Contains("left")) {
            leftRotateButton = button;
            leftRotateButton.onClick.AddListener(this.GetComponentInChildren<CharacterStack>().rotateDirectionClockWise);
         } else {
            if (button.name.ToLower().Contains("right")) {
               rightRotateButton = button;
               rightRotateButton.onClick.AddListener(this.GetComponentInChildren<CharacterStack>().rotateDirectionCounterClockWise);
            }
         }
      }
   }

   private void Update () {
      creationCanvasGroup.alpha = creationMode ? 1f : 0f;
      creationCanvasGroup.blocksRaycasts = creationMode;
   }

   public void setDataAndLayers (UserInfo userInfo, Item weapon, Item armor, Item hat, string armorPalettes) {
      setGuildIcon(userInfo);
      setInternalDataAndLayers(userInfo, weapon, armor, hat, armorPalettes);
   }

   public void setGuildIcon (UserInfo userInfo) {
      // Setup Guild Icon
      if (userInfo.guildId > 0) {
         guildIconGameObject.SetActive(true);
         guildIcon.setBackground(userInfo.iconBackground, userInfo.iconBackPalettes);
         guildIcon.setBorder(userInfo.iconBorder);
         guildIcon.setSigil(userInfo.iconSigil, userInfo.iconSigilPalettes);
      } else {
         guildIconGameObject.SetActive(false);
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
      if (armorData != null) {
         setArmor(armorData.armorType, armorData.palettes, ArmorStatData.serializeArmorStatData(armorData));
      } else {
         D.debug("Armor data is null: {" + armor.itemTypeId + "} ArmorContentCount: {" + EquipmentXMLManager.self.armorStatList.Count + "}");
      }

      HatStatData hatData = EquipmentXMLManager.self.getHatData(hat.itemTypeId);
      if (hatData == null) {
         hatData = HatStatData.getDefaultData();
         hatData.palettes = hat.paletteNames;
      }
      setHat(hatData.hatType, hatData.palettes, HatStatData.serializeHatStatData(hatData));

      setWeapon(userInfo, weapon);

      Item newWeapon = new Weapon { itemTypeId = weapon.itemTypeId, id = weapon.id, paletteNames = weapon.paletteNames };
      Item newArmor = new Armor { itemTypeId = armor.itemTypeId, id = armor.id, paletteNames = armor.paletteNames };
      Item newHat = new Hat { itemTypeId = hat.itemTypeId, id = hat.id, paletteNames = hat.paletteNames };
      Global.setUserObject(new UserObjects {
         userInfo = userInfo,
         weapon = newWeapon,
         armor = newArmor,
         hat = newHat,
      });

      // Depending on the hat chosen, update the hair again
      setHairLayer(userInfo);
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

      setHairLayer(userInfo);
   }

   public void setHairLayer (UserInfo userInfo) {
      // Update both the back and front hair layers
      foreach (HairLayer hairLayer in hairLayers) {
         hairLayer.setType(userInfo.hairType);

         // Update colors
         hairLayer.recolor(userInfo.hairPalettes);

         // Apply clip mask
         if (hairLayer.isFront) {
            hairLayer.setClipMaskForHat(hatLayer.getType());
         }
      }

      // Update colors
      eyes.recolor(userInfo.eyesPalettes);
      if (Global.getUserObjects() != null) {
         Global.userObjects.userInfo = userInfo;
      } else {
         Global.setUserObject(new UserObjects {
            userInfo = userInfo,
         });
      }
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

      if (Global.getUserObjects() != null) {
         Global.userObjects.armor = getArmor();
      }
   }

   public void cancelCreating () {
      Destroy(this.gameObject);
   }

   public UserInfo getUserInfo () {
      CharacterSpot spot = this.spot != null ? this.spot : GetComponentInParent<CharacterSpot>();

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
      if (_armorData == null) {
         _armorData = "";
      }

      Armor armor = new Armor();
      armor.itemTypeId = this.armor.getType();
      armor.paletteNames = this.armor.getPalettes();
      armor.category = Item.Category.Armor;
      armor.count = 1;
      armor.data = _armorData;
      if (_armorData.Length < 1 && armor.itemTypeId != 0) {
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(armor.itemTypeId);
         if (armorData != null) {
            armor.data = ArmorStatData.serializeArmorStatData(armorData);
         } else {
            D.debug("Armor data is null: {" + armor.itemTypeId + "}");
         }
      }

      return armor;
   }

   public Weapon getWeapon () {
      if (_weaponData == null) {
         _weaponData = WeaponStatData.serializeWeaponStatData(WeaponStatData.getDefaultData());
      }

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
      if (_hatData == null) {
         _hatData = "";
      }

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
