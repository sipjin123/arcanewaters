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
   public Dictionary<string, Sprite> iconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> shipRippleSpriteList = new Dictionary<string, Sprite>();

   public enum selectionType
   {
      None = 0,
      ShipType = 1,
      ShipMastType = 2,
      ShipSailType = 3,
      ShipSkinType = 4,
      ShipAvatarIcon = 5,
      ShipSprite = 6,
      ShipRippleSprite = 7 
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
      string spritePath = "Assets/Sprites/Ships/";
      List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(spritePath);

      foreach (ImageManager.ImageData imgData in spriteIconFiles) {
         Sprite sourceSprite = imgData.sprite;
         iconSpriteList.Add(imgData.imagePath, sourceSprite);
      }

      string shipsPath = "Assets/Sprites/Ships/";
      List<ImageManager.ImageData> shipIconFiles = ImageManager.getSpritesInDirectory(shipsPath);

      foreach (ImageManager.ImageData imgData in shipIconFiles) {
         Sprite sourceSprite = imgData.sprite;
         shipSpriteList.Add(imgData.imagePath, sourceSprite);
      }

      string shipsRipplePath = "Assets/Sprites/Ship/";
      List<ImageManager.ImageData> shipRippleIconFiles = ImageManager.getSpritesInDirectory(shipsRipplePath);

      foreach (ImageManager.ImageData imgData in shipRippleIconFiles) {
         Sprite sourceSprite = imgData.sprite;
         shipRippleSpriteList.Add(imgData.imagePath, sourceSprite);
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
