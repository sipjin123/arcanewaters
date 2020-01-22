using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class ShipAbilityScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for selection templates
   public ShipAbilityTemplate itemTemplatePrefab;

   // Holds the tool manager reference
   public ShipAbilityToolManager toolManager;

   // The parent holding the item template
   public GameObject itemTemplateParent;

   // Holds the data panel
   public ShipAbilityPanel shipAbilityPanel;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   // Reference to the empty sprite
   public Sprite emptySprite;

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

      shipAbilityPanel.gameObject.SetActive(false);
   }

   private void createTemplate () {
      ShipAbilityData shipAbilityData = new ShipAbilityData();

      shipAbilityData.abilityName = "Undefined";

      ShipAbilityTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
      template.editButton.onClick.AddListener(() => {
         shipAbilityPanel.loadData(shipAbilityData);
         shipAbilityPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(shipAbilityData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(shipAbilityData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(shipAbilityData.skillIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadData (Dictionary<string, ShipAbilityData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      // Create a row for each template
      foreach (ShipAbilityData shipData in data.Values) {
         ShipAbilityTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
         template.nameText.text = shipData.abilityName;
         template.editButton.onClick.AddListener(() => {
            shipAbilityPanel.loadData(shipData);
            shipAbilityPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(shipData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(shipData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(shipData.skillIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
