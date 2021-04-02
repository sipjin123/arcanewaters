using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class CharacterScreen : GenericGameManager {
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

   // The battleboard reference for weather simulation
   public BattleBoard battleBoard;

   // The number of starting armor options
   public const int STARTING_ARMOR_COUNT = 3;

   public class StartingArmorData {
      // The sql id 
      public int equipmentId;

      // The sprite index 
      public int spriteId;
   }

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      // Look up components
      this.canvasGroup = GetComponent<CanvasGroup>();

      // Look up our Character Spots
      foreach (CharacterSpot spot in GetComponentsInChildren<CharacterSpot>()) {
         _spots[spot.number] = spot;
      }

      myCamera = virtualCam.GetComponent<MyCamera>();
   }

   private void Start () {
      battleBoard.setWeather(WeatherEffectType.Cloud, battleBoard.biomeType);
   }

   public StartingArmorData getStartingArmor (int index) {
      StartingArmorData armorData = startingArmorData.Find(_ => _.equipmentId == index);
      return armorData;
   }

   public bool isShowing () {
      return Camera.main != null && virtualCam != null && Vector2.Distance(Camera.main.transform.position, virtualCam.transform.position) < .5f;
   }

   public bool isCreatingCharacter () {
      foreach (OfflineCharacter offlineChar in GetComponentsInChildren<OfflineCharacter>()) {
         if (offlineChar.creationMode) {
            return true;
         }
      }

      return false;
   }

   public void initializeScreen (UserInfo[] userArray, Item[] armorArray, Item[] weaponArray, Item[] hatArray, string[] armorPalettes, int[] equipmentIds, int[] spriteIds) {
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
         _armorArray[i].paletteNames = armorPalettes[i];
      }

      if (armorArray.Length < STARTING_ARMOR_COUNT) {
         List<Armor> newArmorList = new List<Armor>();

         // Register the valid armor to the new list
         foreach (Item armorFetched in armorArray) {
            newArmorList.Add(Armor.castItemToArmor(armorFetched));
         }

         // Generate a blank armor for the other character that the server failed to provide an armor to
         while (newArmorList.Count < STARTING_ARMOR_COUNT) {
            Armor emptyArmor = new Armor { category = Item.Category.Armor, id = 0, itemTypeId = 0 };
            newArmorList.Add(emptyArmor);
         }
         _armorArray = newArmorList.ToArray();
         armorArray = _armorArray;
      }

      // Hat Setup
      _hatArray = new Hat[hatArray.Length];
      for (int i = 0; i < hatArray.Length; i++) {
         _hatArray[i] = Hat.castItemToHat(hatArray[i]);

         HatStatData hatData = EquipmentXMLManager.self.getHatData(_hatArray[i].itemTypeId);
         if (hatData != null) {
            _hatArray[i].paletteNames = hatData.palettes;
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

      // Make sure that the palette swap manager is setup before revealing the character in the scene to prevent rendering a blank or incomplete character sprite
      if (PaletteSwapManager.self.hasInitialized) {
         setupCharacterSpots(userArray, armorArray, weaponArray, hatArray, armorPalettes);
      } else {
         PaletteSwapManager.self.paletteCompleteEvent.AddListener(() => {
            setupCharacterSpots(userArray, armorArray, weaponArray, hatArray, armorPalettes);
         });
      }
   }

   private void setupCharacterSpots (UserInfo[] userArray, Item[] armorArray, Item[] weaponArray, Item[] hatArray, string[] armorPalettes) {
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
            try {
               offlineChar.setDataAndLayers(userArray[i], weaponArray[i], armorArray[i], hatArray[i], armorPalettes[i]);
            } catch {
               D.debug("Investigate Here! Failed to assign data to offline character and character spot! " +
                  "Weapon Count: {" + weaponArray.Length + "/3} " +
                  "Armor Count: {" + armorArray.Length + "/3} " +
                  "Hat Count: {" + hatArray.Length + "/3 } " +
                  "ArmorPalette Count: {" + armorPalettes.Length + "/3} :: INDEX:{" + i + "}");

               offlineChar.setDataAndLayers(userArray[i], weaponArray[i], armorArray[i], hatArray[i], armorPalettes[i]);
            }
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

      // Hide loading screen
      PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.Login);
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
