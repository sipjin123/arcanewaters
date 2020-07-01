﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class CharacterScreen : MonoBehaviour
{
   #region Public Variables

   // Our virtual camera
   public CinemachineVirtualCamera virtualCam;

   // For some reason the static lookup of this object reference is null in test builds
   public MessageManager messageManager;

   // The prefab we use for creating offline characters
   public OfflineCharacter offlineCharacterPrefab;

   // The Canvas Group
   [HideInInspector]
   public CanvasGroup canvasGroup;

   // Self
   public static CharacterScreen self;

   // The character creation panel
   public CharacterCreationPanel characterCreationPanel;

   // List of armor data
   public List<StartingArmorData> startingArmorData;

   // The MyCamera component
   public MyCamera myCamera;

   public class StartingArmorData {
      // The sql id 
      public int equipmentId;

      // The sprite index 
      public int spriteId;
   }

   #endregion

   void Awake () {
      self = this;

      // Look up components
      this.canvasGroup = GetComponent<CanvasGroup>();

      // Look up our Character Spots
      foreach (CharacterSpot spot in GetComponentsInChildren<CharacterSpot>()) {
         _spots[spot.number] = spot;
      }

      myCamera = virtualCam.GetComponent<MyCamera>();
   }

   public StartingArmorData getStartingArmor (int index) {
      StartingArmorData armorData = startingArmorData.Find(_ => _.equipmentId == index);
      return armorData;
   }

   public bool isShowing () {
      return Vector2.Distance(Camera.main.transform.position, virtualCam.transform.position) < .5f;
   }

   public bool isCreatingCharacter () {
      foreach (OfflineCharacter offlineChar in GetComponentsInChildren<OfflineCharacter>()) {
         if (offlineChar.creationMode) {
            return true;
         }
      }

      return false;
   }

   public void initializeScreen (UserInfo[] userArray, Item[] armorArray, Item[] weaponArray, Item[] hatArray, string[] armorPalettes1, string[] armorPalettes2, int[] equipmentIds, int[] spriteIds) {
      // Cache the starting armor info
      startingArmorData = new List<StartingArmorData>();
      for (int i = 0; i < spriteIds.Length; i++) {
         StartingArmorData newData = new StartingArmorData {
            equipmentId = equipmentIds[i],
            spriteId = spriteIds[i]
         };
         startingArmorData.Add(newData);
      }

      // Store the data we receive for later reference
      _userArray = userArray;

      // Armor Setup
      _armorArray = new Armor[armorArray.Length];
      for (int i = 0; i < armorArray.Length; i++) {
         _armorArray[i] = Armor.castItemToArmor(armorArray[i]);
         _armorArray[i].paletteName1 = armorPalettes1[i];
         _armorArray[i].paletteName2 = armorPalettes2[i];
      }

      if (_armorArray.Length == 0) {
         Armor emptyArmor = new Armor { category = Item.Category.Armor, id = 0, itemTypeId = 0 };
         _armorArray = new Armor[3] { emptyArmor, emptyArmor, emptyArmor };
         armorArray = _armorArray;
      }

      // Hat Setup
      _hatArray = new Hat[hatArray.Length];
      for (int i = 0; i < hatArray.Length; i++) {
         _hatArray[i] = Hat.castItemToHat(armorArray[i]);

         HatStatData hatData = EquipmentXMLManager.self.getHatData(_hatArray[i].itemTypeId);
         if (hatData != null) {
            _hatArray[i].paletteName1 = hatData.palette1;
            _hatArray[i].paletteName2 = hatData.palette2;
         }
      }
      if (_hatArray.Length == 0) {
         Hat emptyHat = new Hat { category = Item.Category.Hats, id = 0, itemTypeId = 0 };
         _hatArray = new Hat[3] { emptyHat, emptyHat, emptyHat };
         hatArray = _hatArray;
      }

      _weaponArray = new Weapon[weaponArray.Length];
      for (int i = 0; i < weaponArray.Length; i++) {
         _weaponArray[i] = Weapon.castItemToWeapon(weaponArray[i]);
      }

      // Clear out any existing offline characters
      foreach (OfflineCharacter offlineChar in GetComponentsInChildren<OfflineCharacter>(true)) {
         Destroy(offlineChar.gameObject);
      }

      for (int i = 0; i < 3; i++) {
         // If they don't have a character in that spot, move on
         if (i > userArray.Length - 1 || userArray[i] == null) {
            continue;
         }

         int charSpotNumber = userArray[i].charSpot;

         // Create the offline character object
         if (_spots.ContainsKey(charSpotNumber)) {
            CharacterSpot spot = _spots[charSpotNumber];
            OfflineCharacter offlineChar = Instantiate(offlineCharacterPrefab, spot.transform.position, Quaternion.identity);
            Global.lastUserGold = userArray[i].gold;
            Global.lastUserGems = userArray[i].gems;
            offlineChar.setDataAndLayers(userArray[i], weaponArray[i], armorArray[i], hatArray[i], armorPalettes1[i], armorPalettes2[i]);
            spot.assignCharacter(offlineChar);
         }
      }

      // Sometimes we just want to auto-select a character when debugging
      if (Util.isAutoStarting()) {
         _spots[1].selectButtonWasPressed();
      } else if (Global.isFastLogin && Global.fastLoginCharacterSpotIndex != -1) {
         _spots[Global.fastLoginCharacterSpotIndex].selectButtonWasPressed();
      }

      // Enable character buttons
      Util.enableCanvasGroup(canvasGroup);
   }

   #region Private Variables

   // The array of UserInfo we received
   protected UserInfo[] _userArray;

   // The array of Armor we received
   protected Armor[] _armorArray;

   // The array of hat we received
   protected Hat[] _hatArray;

   // The array of Weapons we received
   protected Weapon[] _weaponArray;

   // Our various character spots, indexed by their assigned number
   protected Dictionary<int, CharacterSpot> _spots = new Dictionary<int, CharacterSpot>();

   #endregion
}
