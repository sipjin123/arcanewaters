using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Events;

public class TutorialToolPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public TutorialToolManager toolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public string startingName;

   // Update requirement event
   public UnityEvent requirementTypeEvent = new UnityEvent();

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         TutorialData itemData = getTutorialData();
         if (itemData != null) {
            if (itemData.tutorialName != startingName) {
               toolManager.overwriteData(itemData, startingName);
               gameObject.SetActive(false);
            } else {
               toolManager.saveXMLData(itemData);
               gameObject.SetActive(false);
            }
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _addTutorialMsgButton.onClick.AddListener(() => {
         GameObject msgObj = Instantiate(_msgTemplate.gameObject, _msgParent);
         TutorialMsgTemplate msgTemp = msgObj.GetComponent<TutorialMsgTemplate>();
         msgTemp.message.text = "(None)";
         msgTemp.index.text = _msgParent.childCount.ToString();
         msgTemp.deleteButton.onClick.AddListener(() => {
            GameObject.Destroy(msgObj);
         });
      });

      _actionButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ActionType, _actionType);
      });
      _requirementTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.RequirementType, _requirementTypeText, requirementTypeEvent);
      });
      requirementTypeEvent.AddListener(() => {
         RequirementType requirementType = (RequirementType) Enum.Parse(typeof(RequirementType), _requirementTypeText.text);
         switch (requirementType) {
            case RequirementType.Area:
               _itemOptionObj.SetActive(false);
               _areaOptionObj.SetActive(true);
               break;
            case RequirementType.Item:
               _itemOptionObj.SetActive(true);
               _areaOptionObj.SetActive(false);
               break;
            default:
               _itemOptionObj.SetActive(false);
               _areaOptionObj.SetActive(false);
               break;
         }
      });
      _imageButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.TutorialIcon, _iconSprite, _imagePath);
      });
      _tutorialIndicatorButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.TutorialIcon, _tutorialIndicatorImage, _tutorialIndicatorPath);
      });
      _tutorialIndicatorText.onValueChanged.AddListener(_ => {
         _textSample.text = _;
         _imageSample.sprite = ImageManager.getSprite(_tutorialIndicatorPath.text);
      });

      _itemCategoryButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ItemCategory, _itemCategoryText);
      });
      _itemTypeButton.onClick.AddListener(() => {
         Item.Category newCategory = (Item.Category) Enum.Parse(typeof(Item.Category), _itemCategoryText.text);
         selectionPopup.callItemTypeSelectionPopup(newCategory, _itemTypeText, _itemTypeIndexText, _imageIcon);
      });
   }

   public void loadData (TutorialData tutorialData) {
      startingName = tutorialData.tutorialName;
      _name.text = tutorialData.tutorialName;
      _actionType.text = tutorialData.actionType.ToString();
      _description.text = tutorialData.tutorialDescription;
      _imagePath.text = tutorialData.iconPath;
      _count.text = tutorialData.countRequirement.ToString();
      _requirementTypeText.text = tutorialData.requirementType.ToString();

      _itemCategoryText.text = "None";
      _itemTypeText.text = "0";
      _imageIcon.sprite = toolManager.toolScene.emptySprite;
      _areaKey.text = "None";

      RequirementType requirementType = tutorialData.requirementType;
      switch (requirementType) {
         case RequirementType.Area:
            _itemOptionObj.SetActive(false);
            _areaOptionObj.SetActive(true);

            _areaKey.text = tutorialData.rawDataJson;
            break;
         case RequirementType.Item:
            _itemOptionObj.SetActive(true);
            _areaOptionObj.SetActive(false);

            Item itemTranslate = JsonUtility.FromJson<Item>(tutorialData.rawDataJson);
            _itemCategoryText.text = itemTranslate.category.ToString();
            _itemTypeIndexText.text = itemTranslate.itemTypeId.ToString();
            _itemTypeText.text = Util.getItemName(itemTranslate.category, itemTranslate.itemTypeId);
            _imageIcon.sprite = ImageManager.getSprite(itemTranslate.getCastItem().getIconPath());
            break;
         default:
            _itemOptionObj.SetActive(false);
            _areaOptionObj.SetActive(false);
            break;
      }

      _tutorialStep.text = tutorialData.stepOrder.ToString();
      if (tutorialData.iconPath != "") {
         _iconSprite.sprite = ImageManager.getSprite(tutorialData.iconPath);
      } else {
         _iconSprite.sprite = toolManager.toolScene.emptySprite;
      }
      if (tutorialData.tutorialIndicatorImgPath != "") {
         _tutorialIndicatorImage.sprite = ImageManager.getSprite(tutorialData.tutorialIndicatorImgPath);
         _imageSample.sprite = ImageManager.getSprite(tutorialData.tutorialIndicatorImgPath);
      } else {
         _tutorialIndicatorImage.sprite = toolManager.toolScene.emptySprite;
         _imageSample.sprite = toolManager.toolScene.emptySprite;
      }

      _tutorialIndicatorText.text = tutorialData.tutorialIndicatorMessage;
      _textSample.text = tutorialData.tutorialIndicatorMessage;

      int msgCounter = 0;
      _msgParent.gameObject.DestroyChildren();
      if (tutorialData.msgList.Count > 0) {
         foreach (string msg in tutorialData.msgList) {
            GameObject msgObj = Instantiate(_msgTemplate.gameObject, _msgParent);
            TutorialMsgTemplate msgTemp = msgObj.GetComponent<TutorialMsgTemplate>();
            msgTemp.message.text = msg;
            msgTemp.index.text = msgCounter.ToString();
            msgTemp.deleteButton.onClick.AddListener(() => {
               GameObject.Destroy(msgObj);
            });
            msgCounter++;
         }
      }
   }
   private TutorialData getTutorialData () {
      TutorialData tutorialData = new TutorialData();

      tutorialData.tutorialName = _name.text;
      tutorialData.actionType = (ActionType) Enum.Parse(typeof(ActionType), _actionType.text);
      tutorialData.tutorialDescription = _description.text;
      tutorialData.iconPath = _imagePath.text;
      tutorialData.countRequirement = int.Parse(_count.text);
      tutorialData.stepOrder = int.Parse(_tutorialStep.text);
      tutorialData.requirementType = (RequirementType) Enum.Parse(typeof(RequirementType), _requirementTypeText.text);

      tutorialData.tutorialIndicatorMessage = _tutorialIndicatorText.text;
      tutorialData.tutorialIndicatorImgPath = _tutorialIndicatorPath.text;

      RequirementType requirementType = (RequirementType) Enum.Parse(typeof(RequirementType), _requirementTypeText.text);
      switch (requirementType) {
         case RequirementType.Area:
            tutorialData.rawDataJson = _areaKey.text;
            break;
         case RequirementType.Item:
            Item.Category category = (Item.Category) Enum.Parse(typeof(Item.Category), _itemCategoryText.text);
            Item newItem = new Item { category = category, itemTypeId = int.Parse(_itemTypeIndexText.text), count = int.Parse(_count.text) };
            tutorialData.rawDataJson = JsonUtility.ToJson(newItem);
            break;
      }

      tutorialData.msgList = new List<string>();
      foreach (Transform child in _msgParent) {
         tutorialData.msgList.Add(child.GetComponent<TutorialMsgTemplate>().message.text);
      }

      return tutorialData;
   }

   #region Private Variables
#pragma warning disable 0649
   [SerializeField]
   private Button _addTutorialMsgButton;

   [SerializeField]
   private TutorialMsgTemplate _msgTemplate;

   [SerializeField]
   private Transform _msgParent;

   [SerializeField]
   private InputField _name, _description, _count, _tutorialStep;

   [SerializeField]
   private Button _requirementTypeButton;

   [SerializeField]
   private Text _requirementTypeText;

   [SerializeField]
   private Text _actionType, _imagePath;

   [SerializeField]
   private Button _actionButton, _imageButton;

   // Varies depending on requirement Type
   [SerializeField]
   private GameObject _areaOptionObj;
   [SerializeField]
   private InputField _areaKey;

   [SerializeField]
   private GameObject _itemOptionObj;
   [SerializeField]
   private Button _itemCategoryButton, _itemTypeButton;
   [SerializeField]
   private Text _itemCategoryText, _itemTypeText, _itemTypeIndexText;
   [SerializeField]
   private Image _imageIcon;

   [SerializeField]
   private Image _iconSprite;

   [SerializeField]
   private Button _tutorialIndicatorButton;
   [SerializeField]
   private Image _tutorialIndicatorImage, _imageSample;
   [SerializeField]
   private Text _tutorialIndicatorPath, _textSample;
   [SerializeField]
   private InputField _tutorialIndicatorText;
#pragma warning restore 0649
   #endregion
}
