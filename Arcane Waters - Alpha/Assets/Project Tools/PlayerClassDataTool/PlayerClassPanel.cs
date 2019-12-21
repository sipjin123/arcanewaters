using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PlayerClassPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public PlayerClassTool toolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public int startingType;

   // List for toggle able tabs
   public List<TogglerClass> togglerList;

   // Holds the reference to the stats UI
   public StatHolderPanel statHolderpanel;

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         PlayerClassData itemData = getClassData();
         if (itemData != null) {
            if ((int)itemData.type != startingType) {
               toolManager.deleteDataFile(new PlayerClassData { type = (Class.Type)startingType });
            }
            toolManager.saveXMLData(itemData);
            gameObject.SetActive(false);
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _classTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerClassType, _classTypeText);
      });
      _changeAvatarSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerClassIcons, _avatarIcon, _avatarSpritePath);
      });

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private PlayerClassData getClassData () {
      PlayerClassData classData = new PlayerClassData();

      classData.playerStats = statHolderpanel.getStatData();
      classData.type = (Class.Type) Enum.Parse(typeof(Class.Type), _classTypeText.text);
      classData.className = _className.text;
      classData.description = _classDescription.text;
      classData.itemIconPath = _avatarSpritePath.text;

      return classData;
   }

   public void loadPlayerClassData (PlayerClassData classData) {
      startingType = (int)classData.type;
      _classTypeText.text = classData.type.ToString();
      _className.text = classData.className;
      _classDescription.text = classData.description;

      _avatarSpritePath.text = classData.itemIconPath;
      if (classData.itemIconPath != null) {
         _avatarIcon.sprite = ImageManager.getSprite(classData.itemIconPath);
      } else {
         _avatarIcon.sprite = selectionPopup.emptySprite;
      }

      statHolderpanel.loadStatData(classData.playerStats);
   }

   #region Private Variables
#pragma warning disable 0649

   // Item Type
   [SerializeField]
   private Button _classTypeButton;
   [SerializeField]
   private Text _classTypeText;

   // Item Name
   [SerializeField]
   private InputField _className;

   // Item Info
   [SerializeField]
   private InputField _classDescription;

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
