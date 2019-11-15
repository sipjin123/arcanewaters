using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

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

   #endregion

   private void Awake () {
      createButton.onClick.AddListener(() => {
         createTemplate();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   private void createTemplate () {
      AchievementData usableItemData = new AchievementData();
      usableItemData.achievementName = "Undefined";
      usableItemData.actionType = AchievementData.ActionType.None;

      AchievementToolTemplate template = Instantiate(achievementTemplatePrefab, itemTemplateParent.transform);
      template.editButton.onClick.AddListener(() => {
         achievementDataPanel.loadData(usableItemData);
         achievementDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteAchievementDataFile(usableItemData);
         toolManager.loadXMLData();
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(usableItemData);
         toolManager.loadXMLData();
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(usableItemData.iconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadAchievementData (Dictionary<string, AchievementData> achievementDataList) {
      itemTemplateParent.gameObject.DestroyChildren();
      
      // Create a row for each achievement element
      foreach (AchievementData achievementData in achievementDataList.Values) {
         AchievementToolTemplate template = Instantiate(achievementTemplatePrefab, itemTemplateParent.transform);
         template.nameText.text = achievementData.achievementName;
         template.editButton.onClick.AddListener(() => {
            achievementDataPanel.loadData(achievementData);
            achievementDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteAchievementDataFile(achievementData);
            toolManager.loadXMLData();
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(achievementData);
            toolManager.loadXMLData();
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
