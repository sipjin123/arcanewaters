using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class UsableItemDataScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for item templates
   public UsableItemDataTemplate itemTemplatePrefab;

   // Holds the tool manager reference
   public UsableItemDataToolManager toolManager;

   // The parent holding the item template
   public GameObject itemTemplateParent;

   // Holds the item data panel
   public UsableItemDataPanel UsableItemDataPanel;

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
      UsableItemData usableItemData = new UsableItemData();

      usableItemData.itemName = "Undefined";
      usableItemData.type = UsableItem.Type.None;

      UsableItemDataTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
      template.editButton.onClick.AddListener(() => {
         UsableItemDataPanel.loadUsableItemData(usableItemData);
         UsableItemDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteMonsterDataFile(usableItemData);
         toolManager.loadXMLData();
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(usableItemData);
         toolManager.loadXMLData();
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(usableItemData.itemIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadUsableItemData (Dictionary<string, UsableItemData> UsableItemDataList) {
      itemTemplateParent.gameObject.DestroyChildren();
      
      // Create a row for each monster element
      foreach (UsableItemData UsableItemData in UsableItemDataList.Values) {
         UsableItemDataTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
         template.nameText.text = UsableItemData.itemName;
         template.editButton.onClick.AddListener(() => {
            UsableItemDataPanel.loadUsableItemData(UsableItemData);
            UsableItemDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteMonsterDataFile(UsableItemData);
            toolManager.loadXMLData();
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(UsableItemData);
            toolManager.loadXMLData();
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(UsableItemData.itemIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}
