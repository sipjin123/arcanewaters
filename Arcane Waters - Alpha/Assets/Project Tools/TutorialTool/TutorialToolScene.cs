using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using System.Linq;

public class TutorialToolScene : MonoBehaviour
{
   #region Public Variables

   // Holds the prefab for tutorial templates
   public TutorialToolTemplate tutorialTemplatePrefab;

   // Holds the tool manager reference
   public TutorialToolManager toolManager;

   // The parent holding the tutorial template
   public GameObject itemTemplateParent;

   // Holds the tutorial data panel
   public TutorialToolPanel tutorialPanel;

   // Holds the empty sprite for null values
   public Sprite emptySprite;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   // Determines if ID details should be shown
   public Toggle showDetails;

   // Determines if details are being shown in the UI
   public bool isShowingDetails = true;

   #endregion

   private void Awake () {
      createButton.onClick.AddListener(() => {
         createTemplate();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
      showDetails.onValueChanged.AddListener(_ => {
         isShowingDetails = _;
         toolManager.loadXMLData();
      });

      tutorialPanel.gameObject.SetActive(false);
   }

   private void createTemplate () {
      TutorialData tutorialData = new TutorialData();
      tutorialData.tutorialName = "Undefined";

      TutorialToolTemplate template = Instantiate(tutorialTemplatePrefab, itemTemplateParent.transform);
      template.editButton.onClick.AddListener(() => {
         tutorialPanel.loadData(tutorialData);
         tutorialPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteTutorialDataFile(tutorialData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(tutorialData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(tutorialData.iconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadData (Dictionary<string, TutorialData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      List<TutorialData> sortedList = data.Values.ToList().OrderBy(w => w.stepOrder).ToList();

      // Create a row for each tutorial element
      foreach (TutorialData tutorialData in sortedList) {
         TutorialToolTemplate template = Instantiate(tutorialTemplatePrefab, itemTemplateParent.transform);
         if (isShowingDetails) {
            template.nameText.text = tutorialData.tutorialName + "\n[" + tutorialData.stepOrder + "]";
         } else {
            template.nameText.text = tutorialData.tutorialName;
         }

         template.editButton.onClick.AddListener(() => {
            tutorialPanel.loadData(tutorialData);
            tutorialPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteTutorialDataFile(tutorialData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(tutorialData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(tutorialData.iconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}