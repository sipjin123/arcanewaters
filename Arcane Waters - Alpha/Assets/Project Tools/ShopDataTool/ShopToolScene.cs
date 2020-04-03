using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using static ShopDataToolManager;

public class ShopToolScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for templates
   public ShopToolTemplate shopTemplatePrefab;

   // Holds the tool manager reference
   public ShopDataToolManager toolManager;

   // The parent holding the template
   public GameObject templateParent;

   // Holds the data panel
   public ShopToolPanel shopPanel;

   // Holds the empty sprite for null values
   public Sprite emptySprite;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         createButton.gameObject.SetActive(false);
      }

      shopPanel.gameObject.SetActive(false);
      createButton.onClick.AddListener(() => {
         createTemplate();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   private void createTemplate () {
      ShopData shopData = new ShopData();
      shopData.shopName = "Undefined";
      shopData.shopIconPath = "";
      shopData.shopGreetingText = "";

      ShopToolTemplate template = GenericEntryTemplate.createGenericTemplate(shopTemplatePrefab.gameObject, toolManager, templateParent.transform).GetComponent<ShopToolTemplate>();
      template.nameText.text = shopData.shopName;
      template.xmlId = -1;

      template.editButton.onClick.AddListener(() => {
         shopPanel.loadData(shopData, template.xmlId);
         shopPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(template.xmlId);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(shopData);
      });


      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void loadShopData (Dictionary<int, ShopDataGroup> data) {
      templateParent.gameObject.DestroyChildren();

      // Create a row for each monster element
      foreach (ShopDataGroup shopGroup in data.Values) {
         ShopToolTemplate template = GenericEntryTemplate.createGenericTemplate(shopTemplatePrefab.gameObject, toolManager, templateParent.transform).GetComponent<ShopToolTemplate>();
         template.nameText.text = shopGroup.shopData.shopName;
         template.xmlId = shopGroup.xmlId;

         template.editButton.onClick.AddListener(() => {
            shopPanel.loadData(shopGroup.shopData, shopGroup.xmlId);
            shopPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(template.xmlId);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(shopGroup.shopData);
         });

         try {
            Sprite shipSprite = ImageManager.getSpritesInDirectory(shopGroup.shopData.shopIconPath)[0].sprites[0];
            template.itemIcon.sprite = shipSprite;
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
