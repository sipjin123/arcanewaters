using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerFactionScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for selection templates
   public PlayerClassTemplate itemTemplatePrefab;

   // Holds the tool manager reference
   public PlayerFactionToolManager toolManager;

   // The parent holding the item template
   public GameObject itemTemplateParent;

   // Holds the data panel
   public PlayerFactionPanel playerFactionPanel;

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

      playerFactionPanel.gameObject.SetActive(false);
   }

   private void createTemplate () {
      PlayerFactionData factionData = new PlayerFactionData();

      factionData.factionName = "Undefined";
      factionData.type = Faction.Type.None;

      PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
      template.editButton.onClick.AddListener(() => {
         playerFactionPanel.loadPlayerFactionData(factionData);
         playerFactionPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(factionData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(factionData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(factionData.factionIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadPlayerFaction (Dictionary<Faction.Type, PlayerFactionData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      // Create a row for each player faction
      foreach (PlayerFactionData faction in data.Values) {
         PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
         template.nameText.text = faction.factionName;
         template.editButton.onClick.AddListener(() => {
            playerFactionPanel.loadPlayerFactionData(faction);
            playerFactionPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(faction);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(faction);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(faction.factionIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}
