using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class NewTutorialToolScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for tutorial templates
   public NewTutorialElementTemplate tutorialTemplatePrefab;

   // Holds the tool manager reference
   public NewTutorialToolManager toolManager;

   // The parent holding the tutorial template
   public GameObject itemTemplateParent;

   // Holds the tutorial data panel
   public NewTutorialToolPanel tutorialPanel;

   // Holds the empty sprite for null values
   public Sprite emptySprite;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         createButton.gameObject.SetActive(false);
      }

      createButton.onClick.AddListener(() => {
         createTemplate();
      });

      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      tutorialPanel.gameObject.SetActive(false);
   }

   private void Start () {
      toolManager.loadNewTutorialList();
   }

   public void loadData (List<NewTutorialData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      foreach (NewTutorialData tutorialData in data) {
         NewTutorialElementTemplate template = GenericEntryTemplate.createGenericTemplate(tutorialTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform).GetComponent<NewTutorialElementTemplate>();

         template.nameText.text = tutorialData.tutorialName;
         template.indexText.text = "";


         template.editButton.onClick.AddListener(() => {
            tutorialPanel.loadData(tutorialData);
            tutorialPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteNewTutorial(tutorialData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateNewTutorial(tutorialData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(tutorialData.tutorialImageUrl);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         if (!Util.hasValidEntryName(template.nameText.text)) {
            template.setWarning();
         }

         template.gameObject.SetActive(true);
      }
   }

   public void loadAreaKeys (List<string> areaKeys) {
      tutorialPanel.loadAreaKeysOptions(areaKeys);
   }

   private void createTemplate () {
      NewTutorialData tutorialData = new NewTutorialData();
      tutorialData.tutorialName = "Undefined";

      NewTutorialElementTemplate template = GenericEntryTemplate.createGenericTemplate(tutorialTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform).GetComponent<NewTutorialElementTemplate>();
      template.editButton.onClick.AddListener(() => {
         tutorialPanel.loadData(tutorialData);
         tutorialPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteNewTutorial(tutorialData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateNewTutorial(tutorialData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(tutorialData.tutorialImageUrl);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   #region Private Variables

   #endregion
}
