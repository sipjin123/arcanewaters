using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class NewTutorialToolPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public NewTutorialToolManager toolManager;

   // Buttons for saving, canceling and adding a new step
   public Button saveButton, cancelButton, addStepButton;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      saveButton.onClick.AddListener(() => {
         NewTutorialData tutorialData = getNewTutorialData();
         toolManager.saveNewTutorial(tutorialData);
         gameObject.SetActive(false);
      });

      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
      });

      addStepButton.onClick.AddListener(() => {
         NewTutorialStepTemplate stepTemplate = Instantiate(_tutorialStepTemplate, _tutorialStepsParent);
         stepTemplate.gameObject.SetActive(true);
         stepTemplate.stepName.text = "(None)";
         stepTemplate.stepDescription.text = "(None)";
         stepTemplate.deleteButton.onClick.AddListener(() => {
            GameObject.Destroy(stepTemplate);
         });
      });

      _tutorialImgButton.onClick.AddListener(() => {
         _imageSelectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.TutorialIcon, _tutorialImage, _tutorialImgPath);
      });
   }

   private void Start () {
      toolManager.loadAreaKeys();
   }


   public void loadData (NewTutorialData data) {
      _tutorialId = data.tutorialId;
      _tutorialName.text = data.tutorialName;
      _tutorialDescription.text = data.tutorialDescription;
      IEnumerable<KeyValuePair<int, string>> areaKeyValuePair = _tutorialAreaKeyDictionary.Where(x => x.Value == data.tutorialAreaKey);
      _tutorialAreaKey.value = areaKeyValuePair.Any() ? areaKeyValuePair.First().Key : 0;

      if (!string.IsNullOrEmpty(data.tutorialImageUrl)) {
         _tutorialImage.sprite = ImageManager.getSprite(data.tutorialImageUrl);
         _tutorialImgPath.text = data.tutorialImageUrl;
      } else {
         _tutorialImage.sprite = toolManager.scene.emptySprite;
      }

      _tutorialStepsParent.gameObject.DestroyChildren();
      if (data.tutorialStepList.Any()) {
         foreach (TutorialStepData step in data.tutorialStepList) {
            NewTutorialStepTemplate stepTemplate = Instantiate(_tutorialStepTemplate, _tutorialStepsParent);
            stepTemplate.gameObject.SetActive(true);
            stepTemplate.id = step.stepId;
            stepTemplate.stepName.text = step.stepName;
            stepTemplate.stepDescription.text = step.stepDescription;
            stepTemplate.deleteButton.onClick.AddListener(() => {
               GameObject.Destroy(stepTemplate.gameObject);
            });
         }
      }
   }

   public void loadAreaKeysOptions (List<string> areaKeys) {
      _tutorialAreaKey.ClearOptions();
      List<TMPro.TMP_Dropdown.OptionData> options = new List<TMPro.TMP_Dropdown.OptionData>();
      int index = 0;

      foreach (string key in areaKeys) {
         options.Add(new TMPro.TMP_Dropdown.OptionData(key));
         _tutorialAreaKeyDictionary.Add(index, key);
         index++;
      }

      _tutorialAreaKey.AddOptions(options);
   }


   private NewTutorialData getNewTutorialData () {
      NewTutorialData data = new NewTutorialData();
      data.tutorialId = _tutorialId;
      data.tutorialName = _tutorialName.text;
      data.tutorialDescription = _tutorialDescription.text;
      data.tutorialImageUrl = _tutorialImgPath.text;
      data.tutorialIsActive = _tutorialIsActive.isOn;
      data.tutorialAreaKey = _tutorialAreaKeyDictionary[_tutorialAreaKey.value];

      data.tutorialStepList = new List<TutorialStepData>();
      foreach (Transform child in _tutorialStepsParent) {
         NewTutorialStepTemplate stepTemplate = child.GetComponent<NewTutorialStepTemplate>();
         TutorialStepData step = new TutorialStepData(stepTemplate.id, _tutorialId, stepTemplate.stepName.text, stepTemplate.stepDescription.text);

         data.tutorialStepList.Add(step);
      }

      return data;
   }

   #region Private Variables

   // The tutorial name input field
   [SerializeField]
   private InputField _tutorialName;

   // The tutorial description input field
   [SerializeField]
   private InputField _tutorialDescription;

   // The image selection popup
   [SerializeField]
   private GenericSelectionPopup _imageSelectionPopup;

   // The tutorial image
   [SerializeField]
   private Image _tutorialImage;

   // The tutorial image path
   [SerializeField]
   private Text _tutorialImgPath;

   // The tutorial is active input
   [SerializeField]
   private Toggle _tutorialIsActive;

   // The dropdown for selecting the tutorial area key
   [SerializeField]
   private TMPro.TMP_Dropdown _tutorialAreaKey;

   // The button for selecting the tutorial image
   [SerializeField]
   private Button _tutorialImgButton;

   // The parent transform for the tutorial steps
   [SerializeField]
   private Transform _tutorialStepsParent;

   // The tutorial step template
   [SerializeField]
   private NewTutorialStepTemplate _tutorialStepTemplate;

   // A dictionary holding the dropdown values along their area key string 
   private Dictionary<int, string> _tutorialAreaKeyDictionary = new Dictionary<int, string>();

   // The tutorial id
   private int _tutorialId;

   #endregion
}
