using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

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

      _actionButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ActionType, _actionType);
      });
      _stepButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Step, _step);
      });
      _imageButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.TutorialIcon, _iconSprite, _imagePath);
      });
   }

   public void loadData (TutorialData tutorialData) {
      startingName = tutorialData.tutorialName;
      _name.text = tutorialData.tutorialName;
      _step.text = tutorialData.tutorialStep.ToString();
      _actionType.text = tutorialData.actionType.ToString();
      _description.text = tutorialData.tutorialDescription;
      _imagePath.text = tutorialData.iconPath;
      _count.text = tutorialData.countRequirement.ToString();

      _tutorialStep.text = tutorialData.stepOrder.ToString();
      if (tutorialData.iconPath != "") {
         _iconSprite.sprite = ImageManager.getSprite(tutorialData.iconPath);
      } else {
         _iconSprite.sprite = toolManager.toolScene.emptySprite;
      }
   }
   private TutorialData getTutorialData () {
      TutorialData tutorialData = new TutorialData();

      tutorialData.tutorialName = _name.text;
      tutorialData.tutorialStep = (Step) Enum.Parse(typeof(Step), _step.text);
      tutorialData.actionType = (ActionType) Enum.Parse(typeof(ActionType), _actionType.text);
      tutorialData.tutorialDescription = _description.text;
      tutorialData.iconPath = _imagePath.text;
      tutorialData.countRequirement = int.Parse(_count.text);
      tutorialData.stepOrder = int.Parse(_tutorialStep.text);

      return tutorialData;
   }

   #region Private Variables
#pragma warning disable 0649
   [SerializeField]
   private InputField _name, _description, _count, _tutorialStep;

   [SerializeField]
   private Text _step, _actionType, _imagePath;

   [SerializeField]
   private Button _stepButton, _actionButton, _imageButton;

   [SerializeField]
   private Image _iconSprite;
#pragma warning restore 0649
   #endregion
}
