using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PlayerSpecialtyPanel : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool manager
   public PlayerSpecialtyToolManager toolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public string startingName;

   // List for toggle able tabs
   public List<TogglerClass> togglerList;

   // Holds the reference to the stats UI
   public StatHolderPanel statHolderpanel;

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         PlayerSpecialtyData itemData = getSpecialtyData();
         if (itemData != null) {
            if (itemData.specialtyName != startingName) {
               toolManager.deleteDataFile(new PlayerSpecialtyData { specialtyName = startingName });
            }
            toolManager.saveXMLData(itemData);
            gameObject.SetActive(false);
            toolManager.loadXMLData();
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _specialtyTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerSpecialtyType, _specialtyTypeText);
      });
      _changeAvatarSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerSpecialtyIcons, _avatarIcon, _avatarSpritePath);
      });

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private PlayerSpecialtyData getSpecialtyData () {
      PlayerSpecialtyData specialtyData = new PlayerSpecialtyData();

      specialtyData.playerStats = statHolderpanel.getStatData();
      specialtyData.type = (Specialty.Type) Enum.Parse(typeof(Specialty.Type), _specialtyTypeText.text);
      specialtyData.specialtyName = _specialtyName.text;
      specialtyData.description = _specialtyDescription.text;
      specialtyData.specialtyIconPath = _avatarSpritePath.text;

      return specialtyData;
   }

   public void loadPlayerSpecialtyData (PlayerSpecialtyData specialtyData) {
      startingName = specialtyData.specialtyName;
      _specialtyTypeText.text = specialtyData.type.ToString();
      _specialtyName.text = specialtyData.specialtyName;
      _specialtyDescription.text = specialtyData.description;

      _avatarSpritePath.text = specialtyData.specialtyIconPath;
      if (specialtyData.specialtyIconPath != null) {
         _avatarIcon.sprite = ImageManager.getSprite(specialtyData.specialtyIconPath);
      } else {
         _avatarIcon.sprite = selectionPopup.emptySprite;
      }

      statHolderpanel.loadStatData(specialtyData.playerStats);
   }

   #region Private Variables
#pragma warning disable 0649

   // Item Type
   [SerializeField]
   private Button _specialtyTypeButton;
   [SerializeField]
   private Text _specialtyTypeText;

   // Item Name
   [SerializeField]
   private InputField _specialtyName;

   // Item Info
   [SerializeField]
   private InputField _specialtyDescription;

   // Icon
   [SerializeField]
   private Button _changeAvatarSpriteButton;
   [SerializeField]
   private Text _avatarSpritePath;
   [SerializeField]
   private Image _avatarIcon;

#pragma warning restore 0649
   #endregion
}
