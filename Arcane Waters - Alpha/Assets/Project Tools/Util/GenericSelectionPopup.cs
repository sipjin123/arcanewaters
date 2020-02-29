using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Events;

public class GenericSelectionPopup : MonoBehaviour {
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
   public Dictionary<string, Sprite> shipRippleSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> weaponSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> armorSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> helmSpriteList = new Dictionary<string, Sprite>();
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

   public enum selectionType
   {
      None = 0,
      ShipType = 1,
      ShipMastType = 2,
      ShipSailType = 3,
      ShipSkinType = 4,
      ShipAvatarIcon = 5,
      ShipSprite = 6,
      ShipRippleSprite = 7,
      WeaponType = 8,
      Color = 9,
      WeaponIcon = 10,
      ArmorIcon = 11,
      HelmIcon = 12,
      ArmorType = 13,
      HelmType = 14,
      UsableItemType = 15,
      UsableItemIcon = 16,
      ActionType = 17,
      AchievementIcon = 18,
      ItemType = 19,
      ItemCategory = 20,
      PlayerClassType = 21,
      PlayerClassIcons = 22,
      PlayerFactionType = 23,
      PlayerFactionIcons = 24,
      PlayerSpecialtyType = 25,
      PlayerSpecialtyIcons = 26,
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
      WeaponActionType = 39
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
      string helmSpritePath = "Assets/Sprites/Icons/Helm/";
      setupSpriteContent(helmSpriteList, helmSpritePath);

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

      string shipsRipplePath = "Assets/Sprites/Ship/";
      setupSpriteContent(shipRippleSpriteList, shipsRipplePath);

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

      string cannonSpritePath = "Assets/Sprites/Cannon/";
      setupSpriteContent(cannonSpriteList, cannonSpritePath);

      string shopIconPath = "Assets/Sprites/Icons/";
      setupSpriteContent(shopIconSpriteList, shopIconPath); 
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
      } else if (popupType == selectionType.ShipRippleSprite) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in shipRippleSpriteList) {
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
      } else if (popupType == selectionType.HelmIcon) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in helmSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite helmSprite = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, helmSprite, imageIcon, textUI);
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
      } else if (popupType == selectionType.PlayerClassIcons) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in playerClassSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite classIcon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, classIcon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.PlayerFactionIcons) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in playerFactionSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite factionIcon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, factionIcon, imageIcon, textUI);
         }
      } else if (popupType == selectionType.PlayerSpecialtyIcons) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in playerSpecialtySpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite icon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, icon, imageIcon, textUI);
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
      }
   }

   public void callTextSelectionPopup (selectionType popupType, Text textUI, UnityEvent changeEvent = null) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();
      previewSelectionIcon.sprite = emptySprite;
      switch (popupType) {
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
            for (int weaponType = 0; weaponType < MAX_OPTIONS; weaponType++) {
               createTextTemplate(weaponType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.Color:
            foreach (ColorType colorType in Enum.GetValues(typeof(ColorType))) {
               createTextTemplate(colorType.ToString(), textUI, changeEvent);
            }
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
         case selectionType.HelmType:
            foreach (Helm.Type helmType in Enum.GetValues(typeof(Helm.Type))) {
               createTextTemplate(helmType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.UsableItemType:
            foreach (UsableItem.Type helmType in Enum.GetValues(typeof(UsableItem.Type))) {
               createTextTemplate(helmType.ToString(), textUI, changeEvent);
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
         case selectionType.PlayerClassType:
            foreach (Class.Type category in Enum.GetValues(typeof(Class.Type))) {
               createTextTemplate(category.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.PlayerFactionType:
            foreach (Faction.Type category in Enum.GetValues(typeof(Faction.Type))) {
               createTextTemplate(category.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.PlayerSpecialtyType:
            foreach (Specialty.Type category in Enum.GetValues(typeof(Specialty.Type))) {
               createTextTemplate(category.ToString(), textUI, changeEvent);
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
                     createItemTextTemplate(equipmentName, weaponData.weaponType, textUI, indexUI, iconPath, icon, changeEvent, itemIconPath);
                  }
               }
            }
            break;
         case Item.Category.Helm: {
               foreach (HelmStatData helmData in EquipmentXMLManager.self.helmStatList) {
                  if (helmData != null) {
                     string iconPath = helmData.equipmentIconPath;
                     string equipmentName = helmData.equipmentName;
                     createItemTextTemplate(equipmentName, helmData.equipmentID, textUI, indexUI, iconPath, icon, changeEvent, itemIconPath);
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

         if (changeEvent != null) {
            changeEvent.Invoke();
         }

         if (imageUI != null && imagePath != "") {
            imageUI.sprite = ImageManager.getSprite(imagePath);
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

   private void createTextTemplate (string selectionName, Text textUI, UnityEvent changeEvent = null, string imagePath = "", Image imageUI = null) {
      GameObject selectionObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
      ItemTypeTemplate selectionTemplate = selectionObj.GetComponent<ItemTypeTemplate>();
      selectionTemplate.itemTypeText.text = selectionName;
      selectionTemplate.previewButton.gameObject.SetActive(false);
      selectionTemplate.selectButton.onClick.AddListener(() => {
         textUI.text = selectionName;
         if (imageUI != null && imagePath != "") {
            imageUI.sprite = ImageManager.getSprite(imagePath);
         }
         if (changeEvent != null) {
            changeEvent.Invoke();
         }
         closePopup();
      });

      if (imagePath != "") {
         selectionTemplate.spriteIcon.sprite = ImageManager.getSprite(imagePath);
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

   private void closePopup() {
      selectionPanel.SetActive(false);
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