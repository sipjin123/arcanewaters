using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PlayerFactionPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public PlayerFactionToolManager toolManager;

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
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      saveButton.onClick.AddListener(() => {
         PlayerFactionData itemData = getFactionData();
         if (itemData != null) {
            if ((int)itemData.type != startingType) {
               toolManager.deleteDataFile(new PlayerFactionData { type = (Faction.Type)startingType });
            }
            toolManager.saveXMLData(itemData);
            gameObject.SetActive(false);
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _factionTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerFactionType, _factionTypeText);
      });
      _changeAvatarSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerFactionIcons, _avatarIcon, _avatarSpritePath);
      });

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private PlayerFactionData getFactionData () {
      PlayerFactionData factionData = new PlayerFactionData();

      factionData.playerStats = statHolderpanel.getStatData();
      factionData.type = (Faction.Type) Enum.Parse(typeof(Faction.Type), _factionTypeText.text);
      factionData.factionName = _factionName.text;
      factionData.description = _factionDescription.text;
      factionData.factionIconPath = _avatarSpritePath.text;

      return factionData;
   }

   public void loadPlayerFactionData (PlayerFactionData factionData) {
      startingType = (int)factionData.type;
      _factionTypeText.text = factionData.type.ToString();
      _factionName.text = factionData.factionName;
      _factionDescription.text = factionData.description;

      _avatarSpritePath.text = factionData.factionIconPath;
      if (factionData.factionIconPath != null) {
         _avatarIcon.sprite = ImageManager.getSprite(factionData.factionIconPath);
      } else {
         _avatarIcon.sprite = selectionPopup.emptySprite;
      }

      statHolderpanel.loadStatData(factionData.playerStats);
   }

   #region Private Variables
#pragma warning disable 0649

   // Item Type
   [SerializeField]
   private Button _factionTypeButton;
   [SerializeField]
   private Text _factionTypeText;

   // Item Name
   [SerializeField]
   private InputField _factionName;

   // Item Info
   [SerializeField]
   private InputField _factionDescription;

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
