using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using static ShipAbilityToolManager;

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

      ShipAbilityTemplate template = GenericEntryTemplate.createGenericTemplate(itemTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform).GetComponent<ShipAbilityTemplate>();
      template.xmlId = -1;
      template.editButton.onClick.AddListener(() => {
         shipAbilityPanel.loadData(shipAbilityData, template.xmlId);
         shipAbilityPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(template.xmlId);
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

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void loadData (Dictionary<int, ShipAbilityGroup> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      // Create a row for each template
      foreach (ShipAbilityGroup shipAbilityGroup in data.Values) {
         ShipAbilityTemplate template = GenericEntryTemplate.createGenericTemplate(itemTemplatePrefab.gameObject, toolManager, itemTemplateParent.transform).GetComponent<ShipAbilityTemplate>();
         template.xmlId = shipAbilityGroup.xmlId;
         template.nameText.text = shipAbilityGroup.shipAbility.abilityName;
         template.editButton.onClick.AddListener(() => {
            shipAbilityPanel.loadData(shipAbilityGroup.shipAbility, template.xmlId);
            shipAbilityPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(template.xmlId);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(shipAbilityGroup.shipAbility);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(shipAbilityGroup.shipAbility.skillIconPath);
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

   #region Private Variables

   #endregion
}
