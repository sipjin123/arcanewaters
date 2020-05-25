﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   // The hair layers
   public List<HairLayer> hairLayers;

   // The weapon layers
   public List<WeaponLayer> weaponLayers;

   // The name text
   public Text nameText;

   // The level text
   public Text levelText;

   // Whether or not this character is in creation mode
   public bool creationMode = false;

   // Our associated Character creation canvas group
   public CanvasGroup creationCanvasGroup;

   // The creation name input field
   public InputField nameInputField;

   // The sort point
   public GameObject sortPoint;

   // The Class we've chosen
   public Class.Type classType = Class.Type.Fighter;

   // The Specialty we've chosen
   public Specialty.Type specialty = Specialty.Type.Adventurer;

   // The Faction we've chosen
   public Faction.Type faction = Faction.Type.Neutral;

   // The spot this character is in
   public CharacterSpot spot;

   #endregion

   private void Start () {
      // If we just started creating a new character, then update the panel to reflect our gender
      if (this.creationMode) {
         CharacterCreationPanel.self.setCharacterBeingCreated(this);
      }
   }

   private void Update () {
      creationCanvasGroup.alpha = creationMode ? 1f : 0f;
      creationCanvasGroup.blocksRaycasts = creationMode;
   }

   public void setDataAndLayers (UserInfo userInfo, Item weapon, Item armor, string armorPalette1, string armorPalette2) {
      this.userId = userInfo.userId;

      setBodyLayers(userInfo);

      ArmorStatData armorData = ArmorStatData.getDefaultData();
      if (armor.data != "") {
         armorData = Util.xmlLoad<ArmorStatData>(armor.data);
         armorData.palette1 = armorPalette1;
         armorData.palette2 = armorPalette2;
      }

      setArmor(armorData.armorType, armorData.palette1, armorData.palette2);
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
         hairLayer.recolor(userInfo.hairPalette1);
      }

      // Update colors
      eyes.recolor(userInfo.eyesPalette1);
   }

   public void setWeapon (UserInfo userInfo, Item weapon) {
      WeaponStatData weaponData = WeaponStatData.getDefaultData();
      if (weapon.data != "") {
         weaponData = Util.xmlLoad<WeaponStatData>(weapon.data);
      }

      // Update our Material
      foreach (WeaponLayer weaponLayer in weaponLayers) {
         weaponLayer.setType(userInfo.gender, weaponData.weaponType);
         weaponLayer.recolor(weaponData.palette1, weaponData.palette2);
      }
   }

   public void setArmor (int armorType, string paletteName1, string paletteName2) {
      // Set the correct sheet for our gender and armor type
      armor.setType(this.genderType, armorType, true);

      // Update our Material
      armor.recolor(paletteName1, paletteName2);
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
      info.hairPalette1 = this.hairFront.getPalette1();
      info.hairPalette2 = this.hairFront.getPalette2();
      info.eyesType = this.eyes.getType();
      info.eyesPalette1 = this.eyes.getPalette1();
      info.eyesPalette2 = this.eyes.getPalette2();
      info.bodyType = this.body.getType();
      info.classType = this.classType;
      info.specialty = this.specialty;
      info.faction = this.faction;

      return info;
   }

   public Armor getArmor() {
      Armor armor = new Armor();
      armor.itemTypeId = this.armor.getType();
      armor.paletteName1 = this.armor.getPalette1();
      armor.paletteName2 = this.armor.getPalette2();

      return armor;
   }

   #region Private Variables

   #endregion
}
