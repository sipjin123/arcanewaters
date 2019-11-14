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

   // Sprite dictionary
   public Dictionary<string, Sprite> genericIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipRippleSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> weaponSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> armorSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> helmSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> usableItemSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> jobClassSpriteList = new Dictionary<string, Sprite>();

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
      AchievementType = 17,
      AchievementIcon = 18,
      ItemType = 19,
      ItemCategory = 20,
      Jobclass = 21,
      JobIcons = 22
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

      string jobPath = "Assets/Sprites/Icons/Classes/";
      setupSpriteContent(jobClassSpriteList, jobPath);
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
            Sprite shipSprite = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].sprites[3];
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
      } else if (popupType == selectionType.JobIcons) {
         foreach (KeyValuePair<string, Sprite> sourceSprite in jobClassSpriteList) {
            string shortName = ImageManager.getSpritesInDirectory(sourceSprite.Key)[0].imageName;
            Sprite classIcon = ImageManager.getSprite(sourceSprite.Key);
            createImageTemplate(sourceSprite.Key, shortName, classIcon, imageIcon, textUI);
         }
      }
   }

   public void callTextSelectionPopup (selectionType popupType, Text textUI, UnityEvent changeEvent = null) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();
      previewSelectionIcon.sprite = emptySprite;
      switch (popupType) {
         case selectionType.ShipType:
            foreach (Ship.Type shipType in Enum.GetValues(typeof(Ship.Type))) {
               createTextTemplate(shipType.ToString(), textUI, changeEvent);
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
            foreach (Weapon.Type weaponType in Enum.GetValues(typeof(Weapon.Type))) {
               createTextTemplate(weaponType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.Color:
            foreach (ColorType colorType in Enum.GetValues(typeof(ColorType))) {
               createTextTemplate(colorType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ArmorType:
            foreach (Armor.Type armorType in Enum.GetValues(typeof(Armor.Type))) {
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
         case selectionType.AchievementType:
            foreach (AchievementData.ActionType actionType in Enum.GetValues(typeof(AchievementData.ActionType))) {
               createTextTemplate(actionType.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.ItemCategory:
            foreach (Item.Category category in Enum.GetValues(typeof(Item.Category))) {
               createTextTemplate(category.ToString(), textUI, changeEvent);
            }
            break;
         case selectionType.Jobclass:
            foreach (Jobs.Type category in Enum.GetValues(typeof(Jobs.Type))) {
               createTextTemplate(category.ToString(), textUI, changeEvent);
            }
            break;
      }
   }

   public void callItemTypeSelectionPopup (Item.Category category, Text textUI, Text indexUI, Image icon, UnityEvent changeEvent = null) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();
      previewSelectionIcon.sprite = emptySprite;

      switch (category) {
         case Item.Category.Armor: {
               int index = 0;
               foreach (Armor.Type itemType in Enum.GetValues(typeof(Armor.Type))) {
                  string iconPath = new Item { category = Item.Category.Armor, itemTypeId = (int) itemType }.getCastItem().getIconPath();
                  createItemTextTemplate(itemType.ToString(), index, textUI, indexUI, iconPath, icon, changeEvent);
                  index++;
               }
            }
            break;
         case Item.Category.Weapon: {
               int index = 0;
               foreach (Weapon.Type itemType in Enum.GetValues(typeof(Weapon.Type))) {
                  string iconPath = new Item { category = Item.Category.Weapon, itemTypeId = (int) itemType }.getCastItem().getIconPath();
                  createItemTextTemplate(itemType.ToString(), index, textUI, indexUI, iconPath, icon, changeEvent);
                  index++;
               }
            }
            break;
         case Item.Category.Helm: {
               int index = 0;
               foreach (Helm.Type itemType in Enum.GetValues(typeof(Helm.Type))) {
                  string iconPath = new Item { category = Item.Category.Helm, itemTypeId = (int) itemType }.getCastItem().getIconPath();
                  createItemTextTemplate(itemType.ToString(), index, textUI, indexUI, iconPath, icon, changeEvent);
                  index++;
               }
            }
            break;
         case Item.Category.CraftingIngredients: {
               int index = 0;
               foreach (CraftingIngredients.Type itemType in Enum.GetValues(typeof(CraftingIngredients.Type))) {
                  string iconPath = new Item { category = Item.Category.CraftingIngredients, itemTypeId = (int) itemType }.getCastItem().getIconPath();
                  createItemTextTemplate(itemType.ToString(), index, textUI, indexUI, iconPath, icon, changeEvent);
                  index++;
               }
            }
            break;
         case Item.Category.Usable: {
               int index = 0;
               foreach (UsableItem.Type itemType in Enum.GetValues(typeof(UsableItem.Type))) {
                  string iconPath = new Item { category = Item.Category.Usable, itemTypeId = (int) itemType }.getCastItem().getIconPath();
                  createItemTextTemplate(itemType.ToString(), index, textUI, indexUI, iconPath, icon, changeEvent);
                  index++;
               }
            }
            break;
      }
   }

   private void createItemTextTemplate (string selectionName, int index, Text textUI, Text indexUI, string imagePath = "", Image imageUI = null, UnityEvent changeEvent = null) {
      GameObject selectionObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
      ItemTypeTemplate selectionTemplate = selectionObj.GetComponent<ItemTypeTemplate>();
      selectionTemplate.itemTypeText.text = selectionName;
      selectionTemplate.selectButton.onClick.AddListener(() => {
         textUI.text = selectionName;
         indexUI.text = index.ToString();
         if (changeEvent != null) {
            changeEvent.Invoke();
         }

         if (imageUI != null && imagePath != "") {
            imageUI.sprite = ImageManager.getSprite(imagePath);
         }
         closePopup();
      });

      if (imagePath != "") {
         selectionTemplate.spriteIcon.sprite = ImageManager.getSprite(imagePath);
      }
   }

   private void createTextTemplate (string selectionName, Text textUI, UnityEvent changeEvent = null, string imagePath = "", Image imageUI = null) {
      GameObject selectionObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
      ItemTypeTemplate selectionTemplate = selectionObj.GetComponent<ItemTypeTemplate>();
      selectionTemplate.itemTypeText.text = selectionName;
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