using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

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
   public Dictionary<string, Sprite> iconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipRippleSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> weaponSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> armorSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> helmSpriteList = new Dictionary<string, Sprite>();

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
      HelmType = 14
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
      setupSpriteContent(iconSpriteList, spritePath);

      string shipsPath = "Assets/Sprites/Ships/";
      setupSpriteContent(shipSpriteList, shipsPath);

      string shipsRipplePath = "Assets/Sprites/Ship/";
      setupSpriteContent(shipRippleSpriteList, shipsRipplePath);
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
         foreach (KeyValuePair<string, Sprite> sourceSprite in iconSpriteList) {
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
      }
   }

   public void callTextSelectionPopup (selectionType popupType, Text textUI) {
      selectionPanel.SetActive(true);
      templateParent.DestroyChildren();
      previewSelectionIcon.sprite = emptySprite;
      switch (popupType) {
         case selectionType.ShipType:
            foreach (Ship.Type shipType in Enum.GetValues(typeof(Ship.Type))) {
               createTextTemplate(shipType.ToString(), textUI);
            }
            break;
         case selectionType.ShipSailType:
            foreach (Ship.SailType sailType in Enum.GetValues(typeof(Ship.SailType))) {
               createTextTemplate(sailType.ToString(), textUI);
            }
            break;
         case selectionType.ShipMastType:
            foreach (Ship.MastType mastType in Enum.GetValues(typeof(Ship.MastType))) {
               createTextTemplate(mastType.ToString(), textUI);
            }
            break;
         case selectionType.ShipSkinType:
            foreach (Ship.SkinType skinType in Enum.GetValues(typeof(Ship.SkinType))) {
               createTextTemplate(skinType.ToString(), textUI);
            }
            break;
         case selectionType.WeaponType: 
            foreach (Weapon.Type weaponType in Enum.GetValues(typeof(Weapon.Type))) {
               createTextTemplate(weaponType.ToString(), textUI);
            }
            break;
         case selectionType.Color:
            foreach (ColorType colorType in Enum.GetValues(typeof(ColorType))) {
               createTextTemplate(colorType.ToString(), textUI);
            }
            break;
         case selectionType.ArmorType:
            foreach (Armor.Type armorType in Enum.GetValues(typeof(Armor.Type))) {
               createTextTemplate(armorType.ToString(), textUI);
            }
            break;
         case selectionType.HelmType:
            foreach (Helm.Type helmType in Enum.GetValues(typeof(Helm.Type))) {
               createTextTemplate(helmType.ToString(), textUI);
            }
            break;
      }
   }

   private void createTextTemplate (string selectionName, Text textUI) {
      GameObject selectionObj = Instantiate(templatePrefab.gameObject, templateParent.transform);
      ItemTypeTemplate selectionTemplate = selectionObj.GetComponent<ItemTypeTemplate>();
      selectionTemplate.itemTypeText.text = selectionName;
      selectionTemplate.selectButton.onClick.AddListener(() => {
         textUI.text = selectionName;
         closePopup();
      });
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
