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
         toolManager.loadAreaKeys();
         toolManager.loadTutorialStepActionOptions();
         gameObject.SetActive(false);
      });

      cancelButton.onClick.AddListener(() => {
         toolManager.loadAreaKeys();
         toolManager.loadTutorialStepActionOptions();
         gameObject.SetActive(false);
      });

      addStepButton.onClick.AddListener(() => {
         NewTutorialStepTemplate stepTemplate = Instantiate(_tutorialStepTemplate, _tutorialStepsParent);
         stepTemplate.gameObject.SetActive(true);
         stepTemplate.stepName.text = "(None)";
         stepTemplate.stepDescription.text = "(None)";
         stepTemplate.stepAction.ClearOptions();
         stepTemplate.stepAction.AddOptions(_tutorialStepActionOptions);
         stepTemplate.deleteButton.onClick.AddListener(() => {
            GameObject.Destroy(stepTemplate);
         });
      });

      _tutorialImgButton.onClick.AddListener(() => {
         _imageSelectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.TutorialIcon, _tutorialImage, _tutorialImgPath);
      });
   }

   public void loadData (NewTutorialData data) {
      _tutorialId = data.tutorialId;
      _tutorialName.text = data.tutorialName;
      _tutorialDescription.text = data.tutorialDescription;
      IEnumerable<KeyValuePair<int, string>> areaKeyValuePair = _tutorialAreaKeyDictionary.Where(x => x.Value == data.tutorialAreaKey);

      if (areaKeyValuePair.Any()) {
         _tutorialAreaKey.value = areaKeyValuePair.First().Key;
      } else {
         // We are editing a tutorial that already has an area key value. Available area keys are not sufficient. 
         _tutorialAreaKeyDictionary.Add(_tutorialAreaKeyDictionary.Keys.Count, data.tutorialAreaKey);
         _tutorialAreaKey.options.Add(new TMPro.TMP_Dropdown.OptionData(data.tutorialAreaKey));
         _tutorialAreaKey.value = _tutorialAreaKeyDictionary.Where(x => x.Value == data.tutorialAreaKey).First().Key;
      }

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
            stepTemplate.stepAction.ClearOptions();
            stepTemplate.stepAction.AddOptions(_tutorialStepActionOptions);
            IEnumerable<KeyValuePair<int, TutorialStepAction>> actionKeyValuePair = _tutorialStepActionDictionary.Where(x => x.Value.stepActionId == step.stepAction.stepActionId);
            stepTemplate.stepAction.value = actionKeyValuePair.Any() ? actionKeyValuePair.First().Key : 0;

            stepTemplate.deleteButton.onClick.AddListener(() => {
               GameObject.Destroy(stepTemplate.gameObject);
            });
         }
      }
   }

   public void loadAreaKeysOptions (List<string> areaKeys) {
      _tutorialAreaKeyDictionary.Clear();
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

   public void loadTutorialStepActionOptions (List<TutorialStepAction> actions) {
      _tutorialStepActionDictionary.Clear();
      int index = 0;

      foreach (TutorialStepAction action in actions) {
         _tutorialStepActionOptions.Add(new TMPro.TMP_Dropdown.OptionData(action.displayName));
         _tutorialStepActionDictionary.Add(index, action);
         index++;
      }
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
         TutorialStepData step = new TutorialStepData(stepTemplate.id, _tutorialId, stepTemplate.stepName.text, stepTemplate.stepDescription.text, _tutorialStepActionDictionary[stepTemplate.stepAction.value]);

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

   // A dictionary holding the dropdown values along their TutorialStepAction reference
   private Dictionary<int, TutorialStepAction> _tutorialStepActionDictionary = new Dictionary<int, TutorialStepAction>();

   // A dropdown template for the selection of TutorialStepAction in steps
   private List<TMPro.TMP_Dropdown.OptionData> _tutorialStepActionOptions = new List<TMPro.TMP_Dropdown.OptionData>();

   // The tutorial id
   private int _tutorialId;

   #endregion
}
