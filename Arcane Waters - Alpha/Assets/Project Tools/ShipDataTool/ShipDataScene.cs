using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class ShipDataScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for ship templates
   public ShipDataTemplate shipTemplatePrefab;

   // Holds the tool manager reference
   public ShipDataToolManager shipToolManager;

   // The parent holding the ship template
   public GameObject shipTemplateParent;

   // Holds the ship data panel
   public ShipDataPanel shipDataPanel;

   // Holds the empty sprite for null values
   public Sprite emptySprite;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   #endregion

   private void Awake () {
      shipDataPanel.gameObject.SetActive(false);
      createButton.onClick.AddListener(() => {
         createTemplate();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   private void createTemplate () {
      ShipData shipData = new ShipData();
      shipData.shipName = "Undefined";
      shipData.shipType = Ship.Type.Barge;
      shipData.mastType = Ship.MastType.Caravel_1;
      shipData.sailType = Ship.SailType.Caravel_1;
      shipData.skinType = Ship.SkinType.Barge_Dragon;

      ShipDataTemplate template = Instantiate(shipTemplatePrefab, shipTemplateParent.transform);
      template.updateItemDisplay(shipData);
      template.editButton.onClick.AddListener(() => {
         shipDataPanel.loadData(shipData);
         shipDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         shipToolManager.deleteDataFile(shipData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         shipToolManager.duplicateXMLData(shipData);
      });

      try {
         Sprite shipSprite = ImageManager.getSpritesInDirectory(shipData.avatarIconPath)[0].sprites[3];
         template.itemIcon.sprite = shipSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadShipData (Dictionary<string, ShipData> shipDataList) {
      shipTemplateParent.gameObject.DestroyChildren();

      // Create a row for each monster element
      foreach (ShipData shipData in shipDataList.Values) {
         ShipDataTemplate template = Instantiate(shipTemplatePrefab, shipTemplateParent.transform);
         template.updateItemDisplay(shipData);
         template.editButton.onClick.AddListener(() => {
            shipDataPanel.loadData(shipData);
            shipDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            shipToolManager.deleteDataFile(shipData);
         });

         template.duplicateButton.onClick.AddListener(() => {
            shipToolManager.duplicateXMLData(shipData);
         });

         try {
            Sprite shipSprite = ImageManager.getSpritesInDirectory(shipData.avatarIconPath)[0].sprites[3];
            template.itemIcon.sprite = shipSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}
