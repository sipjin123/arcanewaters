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

   // Skill template
   public ShipSkillTemplate skillTemplate;

   // Button for adding skills
   public Button addSkillButton;

   // Skill content holder
   public Transform skillTemplateHolder, skillOptionHolder;

   // Skill Selection UI
   public GameObject skillSelectionPanel;
   public Button closeSelectionPanel;

   // Reference to current xml id
   public int currentXmlId;

   // Toggler to determine if this sql data is active in the database
   public Toggle xml_toggler;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      closeSelectionPanel.onClick.AddListener(() => {
         skillSelectionPanel.SetActive(false);
      });

      saveButton.onClick.AddListener(() => {
         ShipData newShipData = getShipData();
         if (newShipData != null) {
            shipToolManager.saveXMLData(newShipData, currentXmlId, xml_toggler.isOn);
            gameObject.SetActive(false);
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         shipToolManager.loadXMLData();
      });

      addSkillButton.onClick.AddListener(() => {
         skillSelectionPanel.SetActive(true);

         skillOptionHolder.gameObject.DestroyChildren();
         foreach (ShipAbilityPair skillOption in shipToolManager.shipSkillList) {
            if (!existingInInventory(skillOption.abilityId)) {
               ShipSkillTemplate skillOptionTemp = Instantiate(skillTemplate, skillOptionHolder.transform);
               skillOptionTemp.skillNameText.text = skillOption.abilityName;
               skillOptionTemp.skillIdText.text = skillOption.abilityId.ToString();

               skillOptionTemp.selectButton.onClick.AddListener(() => {
                  skillSelectionPanel.SetActive(false);
                  ShipSkillTemplate skillTemp = Instantiate(skillTemplate, skillTemplateHolder.transform);
                  skillTemp.skillNameText.text = skillOption.abilityName;
                  skillTemp.skillIdText.text = skillOption.abilityId.ToString();
                  skillTemp.deleteButton.onClick.AddListener(() => {
                     Destroy(skillTemp.gameObject);
                  });
               });
            }
         }
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
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShipWakeSprite, _rippleSpriteIcon, _ripplePath);
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
      newShipData.isSkillRandom = _randomSkill.isOn;

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

      newShipData.shipAbilities = new List<ShipAbilityPair>();
      foreach (Transform abilityTemplate in skillTemplateHolder.transform) {
         int skillId = int.Parse(abilityTemplate.GetComponent<ShipSkillTemplate>().skillIdText.text);
         string skillName = abilityTemplate.GetComponent<ShipSkillTemplate>().skillNameText.text;
         newShipData.shipAbilities.Add(new ShipAbilityPair { 
            abilityId = skillId,
            abilityName = skillName
         });
      }

      return newShipData;
   }

   public void loadData (ShipData loadedShipData, int xml_id, bool isActive) {
      currentXmlId = xml_id;
      xml_toggler.isOn = isActive;
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
      _randomSkill.isOn = loadedShipData.isSkillRandom;

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

      skillTemplateHolder.gameObject.DestroyChildren();
      foreach (ShipAbilityPair skill in loadedShipData.shipAbilities) {
         ShipSkillTemplate skillTemp = Instantiate(skillTemplate.gameObject, skillTemplateHolder).GetComponent<ShipSkillTemplate>();
         skillTemp.skillNameText.text = skill.abilityName;
         skillTemp.skillIdText.text = skill.abilityId.ToString();
         skillTemp.deleteButton.onClick.AddListener(() => {
            Destroy(skillTemp.gameObject);
         });
      }
   }

   private bool existingInInventory (int id) {
      List<int> cachedAbilityList = new List<int>();
      foreach (Transform child in skillTemplateHolder) { 
         int skillId = int.Parse(child.GetComponent<ShipSkillTemplate>().skillIdText.text);
         cachedAbilityList.Add(skillId);
      }
      return cachedAbilityList.Exists(_ => _ == id);
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

   [SerializeField]
   private Toggle _randomSkill;

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
