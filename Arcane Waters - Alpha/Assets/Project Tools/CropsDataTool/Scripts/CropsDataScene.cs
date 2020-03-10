using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using static CropsDataToolManager;

public class CropsDataScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for templates
   public CropsDataTemplate cropsTemplatePrefab;

   // Holds the tool manager reference
   public CropsDataToolManager cropsToolManager;

   // The parent holding the template
   public GameObject cropsTemplateParent;

   // Holds the data panel
   public CropsDataPanel cropsPanel;

   // Holds the empty sprite for null values
   public Sprite emptySprite;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   #endregion

   private void Awake () {
      cropsPanel.gameObject.SetActive(false);
      createButton.onClick.AddListener(() => {
         createTemplate();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      if (!MasterToolAccountManager.canAlterData()) {
         createButton.gameObject.SetActive(false);
      }
   }

   private void createTemplate () {
      CropsData cropData = new CropsData();
      cropData.xmlName = MasterToolScene.UNDEFINED;
      cropData.cropsType = 0;
      cropData.isEnabled = false;
      cropData.xmlId = -1;

      CropsDataTemplate template = GenericEntryTemplate.createGenericTemplate(cropsTemplatePrefab.gameObject, cropsToolManager, cropsTemplateParent.transform).GetComponent<CropsDataTemplate>();
      template.updateItemDisplay(cropData, false);
      template.xml_id = -1;
      template.editButton.onClick.AddListener(() => {
         cropsPanel.loadData(cropData, -1);
         cropsPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         cropsToolManager.deleteDataFile(template.xml_id);
      });

      template.duplicateButton.onClick.AddListener(() => {
         cropsToolManager.duplicateXMLData(cropData);
      });

      try {
         Sprite newSprite = ImageManager.getSpritesInDirectory(cropData.iconPath)[0].sprites[5];
         template.itemIcon.sprite = newSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void loadData (List<CropsDataGroup> cropDataList) {
      cropsTemplateParent.gameObject.DestroyChildren();

      // Create a row for each crop element
      foreach (CropsDataGroup rawXmlGroup in cropDataList) {
         CropsData cropData = rawXmlGroup.cropsData;
         CropsDataTemplate template = GenericEntryTemplate.createGenericTemplate(cropsTemplatePrefab.gameObject, cropsToolManager, cropsTemplateParent.transform).GetComponent<CropsDataTemplate>();
         template.xml_id = rawXmlGroup.xmlId;
         template.updateItemDisplay(cropData, rawXmlGroup.isEnabled);
         template.editButton.onClick.AddListener(() => {
            cropsPanel.loadData(cropData, rawXmlGroup.xmlId);
            cropsPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            cropsToolManager.deleteDataFile(template.xml_id);
         });

         template.duplicateButton.onClick.AddListener(() => {
            cropsToolManager.duplicateXMLData(cropData);
         });

         try {
            Sprite newSprite = ImageManager.getSprite(cropData.iconPath);
            template.itemIcon.sprite = newSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         if (!Util.hasValidEntryName(cropData.xmlName)) {
            template.setWarning();
         }
         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
