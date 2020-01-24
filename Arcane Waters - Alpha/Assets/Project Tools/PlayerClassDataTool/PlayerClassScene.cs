using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerClassScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for selection templates
   public PlayerClassTemplate itemTemplatePrefab;

   // Holds the tool manager reference
   public PlayerClassTool toolManager;

   // The parent holding the item template
   public GameObject itemTemplateParent;

   // Holds the player data panel
   public PlayerClassPanel playerClassPanel;
   
   // Main menu Buttons
   public Button createButton, mainMenuButton;

   // Reference to the empty sprite
   public Sprite emptySprite;

   #endregion

   private void Awake () {
      createButton.onClick.AddListener(() => {
         createTemplate();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      playerClassPanel.gameObject.SetActive(false);

      if (!MasterToolAccountManager.canAlterData()) {
         createButton.gameObject.SetActive(false);
      }
   }

   private void createTemplate () {
      PlayerClassData classData = new PlayerClassData();

      classData.className = "Undefined";
      classData.type = Class.Type.None;

      PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
      template.indexText.text = classData.type.ToString();

      template.editButton.onClick.AddListener(() => {
         playerClassPanel.loadPlayerClassData(classData);
         playerClassPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(classData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(classData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(classData.itemIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadPlayerClass (Dictionary<string, PlayerClassData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      // Create a row for each player class
      foreach (PlayerClassData playerClass in data.Values) {
         PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
         template.nameText.text = playerClass.className;
         template.indexText.text = playerClass.type.ToString();

         template.editButton.onClick.AddListener(() => {
            playerClassPanel.loadPlayerClassData(playerClass);
            playerClassPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(playerClass);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(playerClass);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(playerClass.itemIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}
