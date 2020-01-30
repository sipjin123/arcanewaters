﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using System.Linq;

public class AchievementToolScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for achievement templates
   public AchievementToolTemplate achievementTemplatePrefab;

   // Holds the tool manager reference
   public AchievementToolManager toolManager;

   // The parent holding the achievement template
   public GameObject itemTemplateParent;

   // Holds the achievement data panel
   public AchievementToolPanel achievementDataPanel;

   // Holds the empty sprite for null values
   public Sprite emptySprite;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   // Determines if ID details should be shown
   public Toggle showDetails;

   // Determines if details are being shown in the UI
   public bool isShowingDetails;

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
      showDetails.onValueChanged.AddListener(_ => {
         isShowingDetails = _;
         toolManager.loadXMLData();
      });
   }

   private void createTemplate () {
      AchievementData usableItemData = new AchievementData();
      usableItemData.achievementName = "Undefined";
      usableItemData.actionType = ActionType.None;

      AchievementToolTemplate template = GenericEntryTemplate.CreateGenericTemplate(achievementTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform).GetComponent<AchievementToolTemplate>();
      template.editButton.onClick.AddListener(() => {
         achievementDataPanel.loadData(usableItemData);
         achievementDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteAchievementDataFile(usableItemData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(usableItemData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(usableItemData.iconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadAchievementData (Dictionary<string, AchievementData> achievementDataCollection) {
      itemTemplateParent.gameObject.DestroyChildren();

      List<AchievementData> sortedList = achievementDataCollection.Values.ToList().OrderBy(w => w.actionType).ToList();

      // Create a row for each achievement element
      foreach (AchievementData achievementData in sortedList) {
         AchievementToolTemplate template = GenericEntryTemplate.CreateGenericTemplate(achievementTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform).GetComponent<AchievementToolTemplate>();
         if (isShowingDetails) {
            template.nameText.text = achievementData.achievementName + "\n[" + achievementData.tier + "] (" + achievementData.actionType + ")";
         } else {
            template.nameText.text = achievementData.achievementName;
         }

         template.editButton.onClick.AddListener(() => {
            achievementDataPanel.loadData(achievementData);
            achievementDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteAchievementDataFile(achievementData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(achievementData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(achievementData.iconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}
