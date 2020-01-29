using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

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

      ShopToolTemplate template = Instantiate(shopTemplatePrefab, templateParent.transform);
      template.xmlToolReference = toolManager;
      template.nameText.text = shopData.shopName;
      template.editButton.onClick.AddListener(() => {
         shopPanel.loadData(shopData);
         shopPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(shopData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(shopData);
      });

      template.gameObject.SetActive(true);
   }

   public void loadShopData (Dictionary<string, ShopData> data) {
      templateParent.gameObject.DestroyChildren();

      // Create a row for each monster element
      foreach (ShopData shopData in data.Values) {
         ShopToolTemplate template = Instantiate(shopTemplatePrefab, templateParent.transform);
         template.xmlToolReference = toolManager;
         template.nameText.text = shopData.shopName;

         template.editButton.onClick.AddListener(() => {
            shopPanel.loadData(shopData);
            shopPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(shopData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(shopData);
         });

         try {
            Sprite shipSprite = ImageManager.getSpritesInDirectory(shopData.shopIconPath)[0].sprites[0];
            template.itemIcon.sprite = shipSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables
      
   #endregion
}
