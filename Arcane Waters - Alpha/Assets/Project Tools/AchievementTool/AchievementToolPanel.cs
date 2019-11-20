using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Events;

public class AchievementToolPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public AchievementToolManager toolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public string startingName;

   // List for toggle able tabs
   public List<TogglerClass> togglerList;

   // Selection Event
   public UnityEvent selectionChangedEvent = new UnityEvent();

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         AchievementData itemData = getAchievementData();
         if (itemData != null) {
            if (itemData.achievementName != startingName) {
               toolManager.deleteAchievementDataFile(new AchievementData { achievementName = startingName });
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

      _achievementTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.AchievementType, _achievementTypeText);
      });
      _changeIconButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.AchievementIcon, _achievementIcon, _achievementIconPath);
      });
      _itemCategoryButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ItemCategory, _itemCategoryText, selectionChangedEvent);
      });
      _itemTypeButton.onClick.AddListener(() => {
         Item.Category newCategory = (Item.Category) Enum.Parse(typeof(Item.Category), _itemCategoryText.text);
         selectionPopup.callItemTypeSelectionPopup(newCategory, _itemTypeText, _itemIndexText, _itemIcon);
      });

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   public void loadData (AchievementData achievementData) {
      startingName = achievementData.achievementName;
      Item.Category category = (Item.Category) achievementData.itemCategory;
      string itemName = Util.getItemName(category, achievementData.itemType);

      _achievementTypeText.text = achievementData.actionType.ToString();
      _itemTypeText.text = itemName;
      _itemIndexText.text = achievementData.itemType.ToString();
      _itemCategoryText.text = category.ToString();

      _achievementIconPath.text = achievementData.iconPath;
      _achievementName.text = achievementData.achievementName;
      _achievementDescription.text = achievementData.achievementDescription;
      _achievementKey.text = achievementData.achievementUniqueID;
      _achievementValue.text = achievementData.count.ToString();
      _achievementTier.text = achievementData.tier.ToString();

      if (achievementData.iconPath != null) {
         _achievementIcon.sprite = ImageManager.getSprite(achievementData.iconPath);
      } else {
         _achievementIcon.sprite = selectionPopup.emptySprite;
      }

      try {
         _itemIcon.sprite = ImageManager.getSprite(new Item { category = category, itemTypeId = achievementData.itemType }.getCastItem().getIconPath());
      } catch {
         _itemIcon.sprite = selectionPopup.emptySprite;
      }
      selectionChangedEvent.RemoveAllListeners();
      selectionChangedEvent.AddListener(() => {
         categoryChanged();
      });
   }

   public void categoryChanged () {
      _itemTypeText.text = "None";
      _itemIndexText.text = "0";
      _itemIcon.sprite = selectionPopup.emptySprite;
   }

   private AchievementData getAchievementData () {
      AchievementData achievementData = new AchievementData();

      achievementData.achievementName = _achievementName.text;
      achievementData.achievementDescription = _achievementDescription.text;
      achievementData.achievementUniqueID = _achievementKey.text;
      achievementData.iconPath = _achievementIconPath.text;
      achievementData.tier = int.Parse(_achievementTier.text);

      achievementData.count = int.Parse(_achievementValue.text);
      achievementData.itemCategory = (int)(Item.Category) Enum.Parse(typeof(Item.Category), _itemCategoryText.text);
      achievementData.itemType = int.Parse(_itemIndexText.text); 
      achievementData.actionType = (ActionType) Enum.Parse(typeof(ActionType), _achievementTypeText.text);

      return achievementData;
   }

   #region Private Variables
#pragma warning disable 0649
   // UI for achievement type
   [SerializeField]
   private Button _achievementTypeButton;
   [SerializeField]
   private Text _achievementTypeText;

   // UI for achievement Item type
   [SerializeField]
   private Button _itemTypeButton;
   [SerializeField]
   private Text _itemTypeText;
   [SerializeField]
   private Image _itemIcon;
   [SerializeField]
   private Text _itemIndexText;

   // UI for achievement Item category
   [SerializeField]
   private Button _itemCategoryButton;
   [SerializeField]
   private Text _itemCategoryText;

   [SerializeField]
   private InputField _achievementTier;

   // UI for Icon 
   [SerializeField]
   private Button _changeIconButton;
   [SerializeField]
   private Image _achievementIcon;
   [SerializeField]
   private Text _achievementIconPath;

   // Name of the achievement
   [SerializeField]
   private InputField _achievementName;

   // Description of the achievement
   [SerializeField]
   private InputField _achievementDescription;

   // Key element for the achievement
   [SerializeField]
   private InputField _achievementKey;

   // Key element for the achievement count value
   [SerializeField]
   private InputField _achievementValue;
#pragma warning restore 0649
   #endregion
}