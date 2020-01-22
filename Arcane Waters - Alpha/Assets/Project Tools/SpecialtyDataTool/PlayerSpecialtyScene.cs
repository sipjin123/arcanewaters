using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerSpecialtyScene : MonoBehaviour
{
   #region Public Variables

   // Holds the prefab for selection templates
   public PlayerClassTemplate itemTemplatePrefab;

   // Holds the tool manager reference
   public PlayerSpecialtyToolManager toolManager;

   // The parent holding the item template
   public GameObject itemTemplateParent;

   // Holds the data panel
   public PlayerSpecialtyPanel playerSpecialtyPanel;

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

      playerSpecialtyPanel.gameObject.SetActive(false);
   }

   private void createTemplate () {
      PlayerSpecialtyData specialtyData = new PlayerSpecialtyData();

      specialtyData.specialtyName = "Undefined";
      specialtyData.type = Specialty.Type.None;

      PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
      template.editButton.onClick.AddListener(() => {
         playerSpecialtyPanel.loadPlayerSpecialtyData(specialtyData);
         playerSpecialtyPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(specialtyData);
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(specialtyData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(specialtyData.specialtyIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadPlayerSpecialtyData (Dictionary<Specialty.Type, PlayerSpecialtyData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      // Create a row for each player specialty
      foreach (PlayerSpecialtyData specialty in data.Values) {
         PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
         template.nameText.text = specialty.specialtyName;
         template.editButton.onClick.AddListener(() => {
            playerSpecialtyPanel.loadPlayerSpecialtyData(specialty);
            playerSpecialtyPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(specialty);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(specialty);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(specialty.specialtyIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}
