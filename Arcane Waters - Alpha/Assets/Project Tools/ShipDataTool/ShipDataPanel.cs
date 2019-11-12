using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class ShipDataPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public ShipDataToolManager shipToolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Reference to the ship preview panel
   public ShipDataPreview shipPreview;

   // Shows the preview of the ship sprite
   public Button showPreviewButton;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public string startingName;

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         ShipData newShipData = getShipData();
         if (newShipData != null) {
            if (newShipData.shipName != startingName) {
               shipToolManager.deleteMonsterDataFile(new ShipData { shipName = startingName });
            }
            shipToolManager.saveXMLData(newShipData);
            gameObject.SetActive(false);
            shipToolManager.loadXMLData();
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         shipToolManager.loadXMLData();
      });

      _shipTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ShipType, _shipTypeText);
      });
      _sailTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ShipSailType, _sailTypeText);
      });
      _mastTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ShipMastType, _mastTypeText);
      });
      _skinTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ShipSkinType, _skinTypeText);
      });

      _changeSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShipSprite, _spriteIcon, _spritePath);
      });
      _changeRippleSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShipRippleSprite, _rippleSpriteIcon, _ripplePath);
      });
      _changeAvatarSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShipAvatarIcon, _avatarIcon, _avatarPath);
      });

      showPreviewButton.onClick.AddListener(() => {
         ShipData newShipData = getShipData();
         if (newShipData != null) {
            shipPreview.gameObject.SetActive(true);
            shipPreview.setpreview(getShipData());
         }
      });
   }

   public ShipData getShipData () {
      ShipData newShipData = new ShipData();

      newShipData.shipName = _shipName.text;

      newShipData.baseHealth = int.Parse(_baseHealth.text);
      newShipData.baseDamage = int.Parse(_baseDamage.text);
      newShipData.baseRange = int.Parse(_baseRange.text);
      newShipData.baseSpeed = int.Parse(_baseSpeed.text);

      newShipData.baseSailors = int.Parse(_baseSailors.text);
      newShipData.baseCargoRoom = int.Parse(_baseCargoRoom.text);
      newShipData.baseSupplyRoom = int.Parse(_baseSupplyRoom.text);
      newShipData.basePrice = int.Parse(_basePrice.text);

      try {
         newShipData.skinType = (Ship.SkinType) Enum.Parse(typeof(Ship.SkinType), _skinTypeText.text);
         newShipData.shipType = (Ship.Type) Enum.Parse(typeof(Ship.Type), _shipTypeText.text);
         newShipData.mastType = (Ship.MastType) Enum.Parse(typeof(Ship.MastType), _mastTypeText.text);
         newShipData.sailType = (Ship.SailType) Enum.Parse(typeof(Ship.SailType), _sailTypeText.text);
      }
      catch {
         return null;
      }

      newShipData.avatarIconPath = _avatarPath.text;
      newShipData.spritePath = _spritePath.text;
      newShipData.rippleSpritePath = _ripplePath.text;

      return newShipData;
   }

   public void loadData (ShipData loadedShipData) {
      _shipName.text = loadedShipData.shipName;
      startingName = _shipName.text;
      _baseHealth.text = loadedShipData.baseHealth.ToString();
      _baseDamage.text = loadedShipData.baseDamage.ToString();
      _baseRange.text = loadedShipData.baseRange.ToString();
      _baseSpeed.text = loadedShipData.baseSpeed.ToString();

      _basePrice.text = loadedShipData.basePrice.ToString();
      _baseSailors.text = loadedShipData.baseSailors.ToString();
      _baseCargoRoom.text = loadedShipData.baseCargoRoom.ToString();
      _baseSupplyRoom.text = loadedShipData.baseSupplyRoom.ToString();

      _shipTypeText.text = loadedShipData.shipType.ToString();
      _mastTypeText.text = loadedShipData.mastType.ToString();
      _sailTypeText.text = loadedShipData.sailType.ToString();
      _skinTypeText.text = loadedShipData.skinType.ToString();

      _spritePath.text = loadedShipData.spritePath;
      _ripplePath.text = loadedShipData.rippleSpritePath;
      _avatarPath.text = loadedShipData.avatarIconPath;

      _avatarIcon.sprite = selectionPopup.emptySprite;
      _spriteIcon.sprite = selectionPopup.emptySprite;
      _rippleSpriteIcon.sprite = selectionPopup.emptySprite;

      if (loadedShipData.avatarIconPath != null && loadedShipData.avatarIconPath != "") {
         Sprite shipSprite = ImageManager.getSpritesInDirectory(loadedShipData.avatarIconPath)[0].sprites[3];
         _avatarIcon.sprite = shipSprite;
      }
      if (loadedShipData.spritePath != null && loadedShipData.spritePath != "") {
         Sprite shipSprite = ImageManager.getSpritesInDirectory(loadedShipData.spritePath)[0].sprites[3];
         _spriteIcon.sprite = shipSprite;
      }
      if (loadedShipData.rippleSpritePath != null && loadedShipData.rippleSpritePath != "") {
         _rippleSpriteIcon.sprite = ImageManager.getSprite(loadedShipData.rippleSpritePath);
      }
   }

   #region Private Variables
#pragma warning disable 0649
   // Name of the ship
   [SerializeField]
   private InputField _shipName;

   // Input field variables for float and int values
   [SerializeField]
   private InputField _baseHealth;
   [SerializeField]
   private InputField _baseRange;
   [SerializeField]
   private InputField _baseDamage;
   [SerializeField]
   private InputField _baseSpeed;
   [SerializeField]
   private InputField _basePrice;
   [SerializeField]
   private InputField _baseSailors;
   [SerializeField]
   private InputField _baseCargoRoom;
   [SerializeField]
   private InputField _baseSupplyRoom;

   // Slider input fields
   [SerializeField]
   private Button _shipTypeButton, _sailTypeButton, _mastTypeButton, _skinTypeButton;
   [SerializeField]
   private Text _shipTypeText, _sailTypeText, _mastTypeText, _skinTypeText;

   // Icon Change fields
   [SerializeField]
   private Button _changeSpriteButton, _changeRippleSpriteButton, _changeAvatarSpriteButton;
   [SerializeField]
   private Image _spriteIcon, _rippleSpriteIcon, _avatarIcon;
   [SerializeField]
   private Text _spritePath, _ripplePath, _avatarPath;
#pragma warning restore 0649
   #endregion
}
