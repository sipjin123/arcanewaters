using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class MonsterDataScene : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public MonsterToolManager toolManager;

   // Reference to monster ingredient panel
   public MonsterDataPanel monsterPanel;

   // Parent holder of the monster templates
   public Transform monsterTemplateParent;

   // Monster template
   public EnemyDataTemplate monsterTemplate;

   // Button that generates a new monster template
   public Button createTemplateButton;

   // Refreshes the file that is loaded in XML
   public Button refreshButton;

   // Determines if the sprites have been initialized
   public bool hasBeenInitialized;

   #endregion

   private void Awake () {
      monsterPanel.gameObject.SetActive(false);
      createTemplateButton.onClick.AddListener(() => createNewTemplate(new MonsterRawData()));

      if (!hasBeenInitialized) {
         hasBeenInitialized = true;
         string spritePath = "Assets/Sprites/Enemies/";
         List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(spritePath);

         foreach (ImageManager.ImageData imgData in spriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            monsterPanel.iconSpriteList.Add(imgData.imagePath, sourceSprite);
         }
      }
   }

   private void createNewTemplate (MonsterRawData monsterData) {
      string itemName = "Undefined";

      if (!toolManager.ifExists(itemName)) {
         EnemyDataTemplate template = Instantiate(monsterTemplate, monsterTemplateParent);
         template.editButton.onClick.AddListener(() => {
            monsterPanel.currentXMLTemplate = template;
            monsterPanel.loadData(monsterData);
            monsterPanel.gameObject.SetActive(true);
         });
         template.deleteButton.onClick.AddListener(() => {
             toolManager.deleteMonsterDataFile(monsterData);
         });

         template.gameObject.SetActive(true);
      }
   }

   public void refreshXML () {
      toolManager.loadAllDataFiles();
   }

   public void updatePanelWithMonsterRawData (Dictionary<string, MonsterRawData> _MonsterRawData) {
      // Clear all the rows
      monsterTemplateParent.gameObject.DestroyChildren();

      // Create a row for each monster element
      foreach (MonsterRawData battler in _MonsterRawData.Values) {
         EnemyDataTemplate template = Instantiate(monsterTemplate, monsterTemplateParent);
         template.updateItemDisplay(battler);
         template.editButton.onClick.AddListener(() => {
            monsterPanel.currentXMLTemplate = template;
            monsterPanel.loadData(battler);
            monsterPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);

            Enemy.Type type = (Enemy.Type) Enum.Parse(typeof(Enemy.Type), template.nameText.text);
            toolManager.deleteMonsterDataFile(new MonsterRawData { battlerID = type });
            toolManager.loadAllDataFiles();
         });

         try {
            template.itemIcon.sprite = ImageManager.getSprite(battler.imagePath);
         } catch {
            template.itemIcon.sprite = monsterPanel.emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
