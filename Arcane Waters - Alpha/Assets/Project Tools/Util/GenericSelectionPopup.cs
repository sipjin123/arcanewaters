using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Events;

public class GenericSelectionPopup : MonoBehaviour
{
   #region Public Variables

   // Parent of the template
   public GameObject templateParent;

   // Prefab of the template
   public GameObject templatePrefab;

   // Holds the UI Panel
   public GameObject selectionPanel;

   // Holds the preview Icon for selected templates
   public Image previewSelectionIcon;

   // Holds empty sprite as default value for images
   public Sprite emptySprite;

   // Buttons for selection and exit
   public Button confirmButton;
   public Button exitButton;

   // Audio source
   public AudioSource audioSource;

   // Maximum item entry for integer types
   public static int MAX_OPTIONS = 100;

   // Sprite dictionary
   public Dictionary<string, Sprite> genericIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipWakeSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> weaponSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> armorSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> hatSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> usableItemSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> playerClassSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> playerFactionSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> playerSpecialtySpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> playerJobSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> tutorialSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipAbilitySpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipAbilityEffectSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> cannonSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shopIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> cropIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> discoveriesSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> perkIconSpriteList = new Dictionary<string, Sprite>();

   public enum selectionType
   {
      None = 0,
      ShipType = 1,
      ShipMastType = 2,
      ShipSailType = 3,
      ShipSkinType = 4,
      ShipAvatarIcon = 5,
      ShipSprite = 6,
      ShipWakeSprite = 7,
      WeaponType = 8,
      Color = 9,
      WeaponIcon = 10,
      ArmorIcon = 11,
      HatIcon = 12,
      ArmorType = 13,
      HatType = 14,
      UsableItemType = 15,
      UsableItemIcon = 16,
      ActionType = 17,
      AchievementIcon = 18,
      ItemType = 19,
      ItemCategory = 20,
      PlayerJobType = 27,
      PlayerJobIcons = 28,
      MonsterType = 29,
      AbilityType = 30,
      TutorialIcon = 31,
      RequirementType = 32,
      ShipAbilityIcon = 33,
      ShipAbilityEffect = 34,
      CannonSprites = 35,
      ShopIcon = 36,
      CropType = 37,
      ArmorTypeSprites = 38,
      WeaponActionType = 39,
      CropsIcon = 40,
      DiscoverySprites = 41,
      AchievementItemCategory = 42,
      PerkIcon = 43,
      BiomeType = 44,
      LootGroupsLandMonsters = 45,
      LootGroupsSeaMonsters = 46
   }

   #endregion

   private void Start () {
      initializeSpriteDictionary();
      confirmButton.onClick.AddListener(() => {
      });
      exitButton.onClick.AddListener(() => {
         closePopup();
      });
   }

   private void initializeSpriteDictionary () {
      string hatSpritePath = "Assets/Sprites/Icons/Hat/";
      setupSpriteContent(hatSpriteList, hatSpritePath);

      string armorSpritePath = "Assets/Sprites/Icons/Armor/";
      setupSpriteContent(armorSpriteList, armorSpritePath);

      string weaponSpritePath = "Assets/Sprites/Icons/Weapons/";
      setupSpriteContent(weaponSpriteList, weaponSpritePath);

      string genericspritePath = "Assets/Sprites/Icons/";
      setupSpriteContent(genericIconSpriteList, genericspritePath);

      string spritePath = "Assets/Sprites/Ships/";
      setupSpriteContent(shipIconSpriteList, spritePath);

      string shipsPath = "Assets/Sprites/Ships/";
      setupSpriteContent(shipSpriteList, shipsPath);

      string shipsWakePath = "Assets/Sprites/ShipWakes/";
      setupSpriteContent(shipWakeSpriteList, shipsWakePath);

      string usableItemPath = "Assets/Sprites/Icons/UsableItems/";
      setupSpriteContent(usableItemSpriteList, usableItemPath);

      string classPath = "Assets/Sprites/Icons/Classes/";
      setupSpriteContent(playerClassSpriteList, classPath);

      string factionPath = "Assets/Sprites/Icons/Factions/";
      setupSpriteContent(playerFactionSpriteList, factionPath);

      string specialtyPath = "Assets/Sprites/Icons/Specialties/";
      setupSpriteContent(playerSpecialtySpriteList, specialtyPath);

      string jobPath = "Assets/Sprites/Icons/Jobs/";
      setupSpriteContent(playerJobSpriteList, jobPath);

      string tutorialPath = "Assets/Sprites/Icons/";
      setupSpriteContent(tutorialSpriteList, tutorialPath);

      string shipAbilityPath = "Assets/Sprites/Icons/";
      setupSpriteContent(shipAbilitySpriteList, shipAbilityPath);

      string shipAbilityEffectPath = "Assets/Sprites/Effects/";
      setupSpriteContent(shipAbilityEffectSpriteList, shipAbilityEffectPath);

      string cannonSpritePath = "Assets/Sprites/Projectiles/";
      setupSpriteContent(cannonSpriteList, cannonSpritePath);

      string shopIconPath = "Assets/Sprites/Icons/";
      setupSpriteContent(shopIconSpriteList, shopIconPath);

      string cropIconPath = "Assets/Sprites/Crops/";
      setupSpriteContent(cropIconSpriteList, cropIconPath);

      string discoverySpritesPath = "Assets/Sprites/Discoveries/";
      setupSpriteContent(discoveriesSpriteList, discoverySpritesPath);

      string perkSpritePath = "Assets/Sprites/Icons/Perks/";
      setupSpriteContent(perkIconSpriteList, perkSpritePath);
   }

   private void setupSpriteContent (Dictionary<string, Sprite> spriteCollection, string spritePath) {
      List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(spritePath);

      foreach (ImageManager.ImageData imgData in spriteIconFiles) {
         Sprite sourceSprite = imgData.sprite;
         spriteCollection.Add(imgData.imagePath, sourceSprite);
      }
   }

   public void callImageTextSelectionPopup (selectionType popupType, Image imageIcon, Text textUI = null) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();
      previewSelectionIcon.sprite = emptySprite;

      if (popupType == selectionType.ShipSprite) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in shipSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite shipSprite = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].sprites[3];
            createImageTemplate(sourceSprite.Key, shortName, shipSprite, imageIcon, textUI);
         }
      } else if (popupType == selectionType.ShipWakeSprite) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in shipWakeSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            createImageTemplate(sourceSprite.Key, shortName, sourceSprite.Value, imageIcon, textUI);
         }
      } else if (popupType == selectionType.ShipAvatarIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in shipIconSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite shipSprite = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].sprites[5];
            createImageTemplate(sourceSprite.Key, shortName, shipSprite, imageIcon, textUI);
         }
      } else if (popupType == selectionType.WeaponIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in weaponSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite weaponSprite = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, weaponSprite, imageIcon, textUI);
         }
      } else if (popupType == selectionType.ArmorIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in armorSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite armorSprite = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, armorSprite, imageIcon, textUI);
         }
      } else if (popupType == selectionType.HatIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in hatSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite hatSprite = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, hatSprite, imageIcon, textUI);
         }
      } else if (popupType == selectionType.UsableItemIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in usableItemSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite itemSprite = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, itemSprite, imageIcon, textUI);
         }
      } else if (popupType == selectionType.AchievementIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in genericIconSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite achievementSprite = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, achievementSprite, imageIcon, textUI);
         }
      } else if (popupType == selectionType.PlayerJobIcons) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in playerJobSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.TutorialIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in tutorialSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.ShipAbilityIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in shipAbilitySpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.ShipAbilityEffect) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in shipAbilityEffectSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.CannonSprites) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in cannonSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.ShopIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in shopIconSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.CropsIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in cropIconSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.DiscoverySprites) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in discoveriesSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.PerkIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in perkIconSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
         }
      }
   }

   public void callTextSelectionPopup (selectionType popupType, Text textUI, UnityEvent changeEvent = null) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();
      previewSelectionIcon.sprite = emptySprite;
      switch (popupType) {
         case selectionType.LootGroupsLandMonsters:
            foreach (KeyValuePair<int, LootGroupData> lootGroupData in MonsterToolManager.instance.lootGroupDataCollection) {
               createTextTemplate(lootGroupData.Value.lootGroupName, textUI, changeEvent, "", null, lootGroupData.Key, true);
            }
            break;
         case selectionType.LootGroupsSeaMonsters:
            foreach (KeyValuePair<int, LootGroupData> lootGroupData in SeaMonsterToolManager.instance.lootGroupDataCollection) {
               createTextTemplate(lootGroupData.Value.lootGroupName, textUI, changeEvent, "", null, lootGroupData.Key, true);
            }
            break;
         case selectionType.WeaponActionType:
            foreach (Weapon.ActionType weaponActionType in Enum.GetValues(typeof(Weapon.ActionType))) {
               createTextTemplate(weaponActionType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ShipType:
            foreach (Ship.Type shipType in Enum.GetValues(typeof(Ship.Type))) {
               createTextTemplate(shipType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.RequirementType:
            foreach (RequirementType requirementType in Enum.GetValues(typeof(RequirementType))) {
               createTextTemplate(requirementType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.BiomeType:
            foreach (Biome.Type biomeType in Enum.GetValues(typeof(Biome.Type))) {
               createTextTemplate(biomeType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ShipSailType:
            foreach (Ship.SailType sailType in Enum.GetValues(typeof(Ship.SailType))) {
               createTextTemplate(sailType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ShipMastType:
            foreach (Ship.MastType mastType in Enum.GetValues(typeof(Ship.MastType))) {
               createTextTemplate(mastType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ShipSkinType:
            foreach (Ship.SkinType skinType in Enum.GetValues(typeof(Ship.SkinType))) {
               createTextTemplate(skinType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.WeaponType:
            for (int weaponType = 1; weaponType < MAX_OPTIONS; weaponType++) {
               string spritePath = "Assets/Sprites/Weapons/" + Gender.Type.Female + "/" + "weapon_" + weaponType + "_front";
               createTextTemplate(weaponType.ToString(), textUI, changeEvent, spritePath, null, EquipmentToolPanel.EQUIPMENT_SPRITE_INDEX);
            }
            break;
         case selectionType.Color:
            createTextTemplate(PaletteDef.Armor1.Brown.ToString(), textUI, changeEvent);
            createTextTemplate(PaletteDef.Armor1.White.ToString(), textUI, changeEvent);
            createTextTemplate(PaletteDef.Armor1.Blue.ToString(), textUI, changeEvent);
            createTextTemplate(PaletteDef.Armor1.Red.ToString(), textUI, changeEvent);
            createTextTemplate(PaletteDef.Armor1.Green.ToString(), textUI, changeEvent);
            createTextTemplate(PaletteDef.Armor1.Yellow.ToString(), textUI, changeEvent);
            createTextTemplate(PaletteDef.Armor1.Teal.ToString(), textUI, changeEvent);
            break;
         case selectionType.ArmorType:
            foreach (ArmorStatData armorType in EquipmentXMLManager.self.armorStatList) {
               createTextTemplate(armorType.equipmentID.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ArmorTypeSprites:
            for (int armorType = 0; armorType < MAX_OPTIONS; armorType++) {
               createTextTemplate(armorType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.HatType:
            for (int hatType = 1; hatType < MAX_OPTIONS; hatType++) {
               string spritePath = "Assets/Sprites/Hats/" + Gender.Type.Female + "/" + Gender.Type.Female.ToString().ToLower() + "_hat_" + hatType;
               createTextTemplate(hatType.ToString(), textUI, changeEvent, spritePath, null, EquipmentToolPanel.EQUIPMENT_SPRITE_INDEX);
            }
            break;
         case selectionType.UsableItemType:
            foreach (UsableItem.Type hatType in Enum.GetValues(typeof(UsableItem.Type))) {
               createTextTemplate(hatType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ActionType:
            foreach (ActionType actionType in Enum.GetValues(typeof(ActionType))) {
               createTextTemplate(actionType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ItemCategory:
            foreach (Item.Category category in Enum.GetValues(typeof(Item.Category))) {
               createTextTemplate(category.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.AchievementItemCategory:
            foreach (Item.Category category in Enum.GetValues(typeof(Item.Category))) {
               if (category == Item.Category.None || category == Item.Category.Weapon || category == Item.Category.Armor || category == Item.Category.CraftingIngredients) {
                  createTextTemplate(category.ToString(), textUI, changeEvent);
               }
            }
            break;
         case selectionType.PlayerJobType:
            foreach (Jobs.Type category in Enum.GetValues(typeof(Jobs.Type))) {
               createTextTemplate(category.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.MonsterType:
            foreach (Enemy.Type enemyType in Enum.GetValues(typeof(Enemy.Type))) {
               createTextTemplate(enemyType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.AbilityType:
            if (AbilityManager.self != null) {
               foreach (BasicAbilityData battleData in AbilityManager.self.allGameAbilities) {
                  createTextTemplate(battleData.itemName.ToString(), textUI, changeEvent);
               }
            } else {
               selectionPanel.SetActive(false);
            }
            break;
         case selectionType.CropType:
            foreach (Crop.Type cropType in Enum.GetValues(typeof(Crop.Type))) {
               createTextTemplate(cropType.ToString(), textUI, changeEvent);
            }
            break;
      }
   }

   public void callItemTypeSelectionPopup (Item.Category category, Text textUI, Text indexUI, Image icon, UnityEvent changeEvent = null, Text itemIconPath = null) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();
      previewSelectionIcon.sprite = emptySprite;

      switch (category) {
         case Item.Category.Armor: {
               foreach (ArmorStatData armorData in EquipmentXMLManager.self.armorStatList) {
                  if (armorData != null) {
                     string iconPath = armorData.equipmentIconPath;
                     string armorName = armorData.equipmentName;
                     createItemTextTemplate(armorName, armorData.equipmentID, textUI, indexUI, iconPath, icon, changeEvent, itemIconPath);
                  }
               }
            }
            break;
         case Item.Category.Weapon: {
               foreach (WeaponStatData weaponData in EquipmentXMLManager.self.weaponStatList) {
                  if (weaponData != null) {
                     string iconPath = weaponData.equipmentIconPath;
                     string equipmentName = weaponData.equipmentName;
                     createItemTextTemplate(equipmentName, weaponData.equipmentID, textUI, indexUI, iconPath, icon, changeEvent, itemIconPath);
                  }
               }
            }
            break;
         case Item.Category.Hats: {
               foreach (HatStatData hatData in EquipmentXMLManager.self.hatStatList) {
                  if (hatData != null) {
                     string iconPath = hatData.equipmentIconPath;
                     string equipmentName = hatData.equipmentName;
                     createItemTextTemplate(equipmentName, hatData.equipmentID, textUI, indexUI, iconPath, icon, changeEvent, itemIconPath);
                  }
               }
            }
            break;
         case Item.Category.CraftingIngredients: {
               foreach (CraftingIngredients.Type itemType in Enum.GetValues(typeof(CraftingIngredients.Type))) {
                  string iconPath = new Item { category = Item.Category.CraftingIngredients, itemTypeId = (int) itemType }.getCastItem().getIconPath();
                  createItemTextTemplate(itemType.ToString(), (int) itemType, textUI, indexUI, iconPath, icon, changeEvent, itemIconPath);
               }
            }
            break;
         case Item.Category.Usable: {
               foreach (UsableItem.Type itemType in Enum.GetValues(typeof(UsableItem.Type))) {
                  string iconPath = new Item { category = Item.Category.Usable, itemTypeId = (int) itemType }.getCastItem().getIconPath();
                  createItemTextTemplate(itemType.ToString(), (int) itemType, textUI, indexUI, iconPath, icon, changeEvent, itemIconPath);
               }
            }
            break;
      }
   }

   private void createItemTextTemplate (string selectionName, int index, Text textUI, Text indexUI, string imagePath = "", Image imageUI = null, UnityEvent changeEvent = null, Text itemIconPath = null) {
      GameObject selectionObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
      ItemTypeTemplate selectionTemplate = selectionObj.GetComponent<ItemTypeTemplate>();
      selectionTemplate.itemTypeText.text = selectionName;
      selectionTemplate.selectButton.onClick.AddListener(() => {
         textUI.text = selectionName;
         indexUI.text = index.ToString();

         if (itemIconPath != null) {
            itemIconPath.text = imagePath;
         }

         if (imageUI != null && imagePath != "") {
            imageUI.sprite = ImageManager.getSprite(imagePath);
         }

         if (changeEvent != null) {
            changeEvent.Invoke();
         }
         closePopup();
      });

      if (imageUI != null && imagePath != "") {
         selectionTemplate.previewButton.onClick.AddListener(() => {
            previewSelectionIcon.sprite = ImageManager.getSprite(imagePath);
         });
      } else {
         selectionTemplate.previewButton.gameObject.SetActive(false);
      }

      if (imagePath != "") {
         selectionTemplate.spriteIcon.sprite = ImageManager.getSprite(imagePath);
      }
   }

   private void createTextTemplate (string selectionName, Text textUI, UnityEvent changeEvent = null, string imagePath = "", Image imageUI = null, int spriteIndex = 0, bool useIndexAsName = false) {
      GameObject selectionObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
      ItemTypeTemplate selectionTemplate = selectionObj.GetComponent<ItemTypeTemplate>();
      selectionTemplate.itemTypeText.text = selectionName;
      selectionTemplate.previewButton.gameObject.SetActive(false);
      selectionTemplate.selectButton.onClick.AddListener(() => {
         textUI.text = useIndexAsName ? spriteIndex.ToString() : selectionName;
         if (imageUI != null && imagePath != "") {
            imageUI.sprite = ImageManager.getSprite(imagePath);
         }
         if (changeEvent != null) {
            changeEvent.Invoke();
         }
         closePopup();
      });

      if (imagePath != "") {
         if (spriteIndex > 0) {
            selectionTemplate.previewButton.gameObject.SetActive(true);
            selectionTemplate.previewButton.onClick.AddListener(() => {
               previewSelectionIcon.sprite = selectionTemplate.spriteIcon.sprite;
            });
            Sprite[] sprites = ImageManager.getSprites(imagePath);
            if (sprites.Length > 0) {
               if (spriteIndex > sprites.Length) {
                  spriteIndex = sprites.Length - 1;
               }
               selectionTemplate.spriteIcon.sprite = sprites[spriteIndex];
            }
         } else {
            selectionTemplate.spriteIcon.sprite = ImageManager.getSprite(imagePath);
         }
      }
   }

   private void createImageTemplate (string selectionName, string shortName, Sprite selectionIcon, Image imageIcon, Text textUI = null) {
      GameObject selectionObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
      ItemTypeTemplate selectionTemplate = selectionObj.GetComponent<ItemTypeTemplate>();
      selectionTemplate.itemTypeText.text = shortName;
      selectionTemplate.spriteIcon.sprite = selectionIcon;
      selectionTemplate.selectButton.onClick.AddListener(() => {
         textUI.text = selectionName;
         imageIcon.sprite = selectionIcon;
         closePopup();
      });
      selectionTemplate.previewButton.onClick.AddListener(() => {
         previewSelectionIcon.sprite = selectionIcon;
      });
   }

   public void toggleAudioSelection (AudioClip clip, Text textUsed) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();

      foreach (AudioClipManager.AudioClipData sourceClip in AudioClipManager.self.audioDataList) {
         GameObject iconTempObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.itemTypeText.text = sourceClip.audioName;

         iconTemp.previewButton.onClick.AddListener(() => {
            if (sourceClip.audioClip != null) {
               audioSource.clip = sourceClip.audioClip;
               audioSource.Play();
            }
         });

         iconTemp.selectButton.onClick.AddListener(() => {
            textUsed.text = sourceClip.audioPath;
            clip = sourceClip.audioClip;
            closePopup();
         });
      }
   }

   private void closePopup () {
      selectionPanel.SetActive(false);
   }

   public static Dictionary<int, Item> getItemCollection (Item.Category selectedCategory, List<CraftableItemRequirements> craftingRequirementList) {
      Dictionary<int, Item> itemNameList = new Dictionary<int, Item>();
      switch (selectedCategory) {
         case Item.Category.Blueprint:
            foreach (CraftableItemRequirements item in craftingRequirementList) {
               string itemData = "";
               string itemName = "";
               string prefix = "";

               if (item.resultItem.category == Item.Category.Weapon) {
                  WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.resultItem.itemTypeId);
                  itemData = Blueprint.createData(Item.Category.Weapon, item.resultItem.itemTypeId);
                  itemName = weaponData.equipmentName;
                  prefix = Blueprint.WEAPON_ID_PREFIX;
               } else if (item.resultItem.category == Item.Category.Armor) {
                  ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.resultItem.itemTypeId);
                  itemData = Blueprint.createData(Item.Category.Armor, item.resultItem.itemTypeId);
                  itemName = armorData.equipmentName;
                  prefix = Blueprint.ARMOR_ID_PREFIX;
               }

               prefix = prefix + item.resultItem.itemTypeId;
               if (!itemNameList.ContainsKey(int.Parse(prefix))) {
                  itemNameList.Add(int.Parse(prefix), new Item { itemName = itemName, data = itemData, category = Item.Category.Blueprint, itemTypeId = item.resultItem.itemTypeId });
               }
            }
            break;
         case Item.Category.Hats:
            foreach (HatStatData hatData in EquipmentXMLManager.self.hatStatList) {
               itemNameList.Add((int) hatData.hatType, new Item { itemName = hatData.equipmentName });
            }
            break;
         case Item.Category.Armor:
            foreach (ArmorStatData armorStatData in EquipmentXMLManager.self.armorStatList) {
               itemNameList.Add(armorStatData.equipmentID, new Item { itemName = armorStatData.equipmentName });
            }
            break;
         case Item.Category.Weapon:
            foreach (WeaponStatData weaponData in EquipmentXMLManager.self.weaponStatList) {
               itemNameList.Add(weaponData.equipmentID, new Item { itemName = weaponData.equipmentName });
            }
            break;
         default:
            Type itemType = Util.getItemType(selectedCategory);

            if (itemType != null) {
               foreach (object item in Enum.GetValues(itemType)) {
                  int newVal = (int) item;
                  itemNameList.Add(newVal, new Item { itemName = item.ToString() });
               }
            }
            break;
      }
      return itemNameList;
   }

   #region Private Variables

   #endregion
}

[Serializable]
public class TogglerClass
{
   // Button to trigger the toggle
   public Button buttonToggle;

   // Items to hide
   public GameObject gameObjToHide;

   // Drop down icon for indication
   public GameObject dropDownIcon;

   public void initListeners () {
      buttonToggle.onClick.AddListener(() => {
         gameObjToHide.SetActive(!gameObjToHide.activeSelf);
         dropDownIcon.SetActive(!gameObjToHide.activeSelf);
      });
   }
}
