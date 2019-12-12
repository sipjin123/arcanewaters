using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class MonsterDataScene : MonoBehaviour {
   #region Public Variables

   // Reference to the ability tool manager
   public AbilityToolManager abilityToolManager;

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

   // Opens the main tool
   public Button openMainTool;

   #endregion

   private void Awake () {
      monsterPanel.gameObject.SetActive(false);
      openMainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
      createTemplateButton.onClick.AddListener(() => createNewTemplate(new BattlerData()));

      if (!hasBeenInitialized) {
         hasBeenInitialized = true;
         string spritePath = "Assets/Sprites/Enemies/";
         List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(spritePath);

         foreach (ImageManager.ImageData imgData in spriteIconFiles) {
            if (!imgData.imagePath.Contains("SeaMonsters")) {
               Sprite sourceSprite = imgData.sprite;
               monsterPanel.iconSpriteList.Add(imgData.imagePath, sourceSprite);
            }
         }

         string hitSpritePath = "Assets/Sprites/Effects/";
         List<ImageManager.ImageData> hitSpriteIconFiles = ImageManager.getSpritesInDirectory(hitSpritePath);

         foreach (ImageManager.ImageData imgData in hitSpriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            monsterPanel.hitIconSpriteList.Add(imgData.imagePath, sourceSprite);
         }

         string castSpritePath = "Assets/Sprites/Effects/";
         List<ImageManager.ImageData> castSpriteIconFiles = ImageManager.getSpritesInDirectory(castSpritePath);

         foreach (ImageManager.ImageData imgData in castSpriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            monsterPanel.castIconSpriteList.Add(imgData.imagePath, sourceSprite);
         }

         string skillIconSpritePath = "Assets/Sprites/Icons/";
         List<ImageManager.ImageData> skillIconSpriteFiles = ImageManager.getSpritesInDirectory(skillIconSpritePath);

         foreach (ImageManager.ImageData imgData in skillIconSpriteFiles) {
            Sprite sourceSprite = imgData.sprite;
            monsterPanel.skillIconSpriteList.Add(imgData.imagePath, sourceSprite);
         }

         string projectileSpritePath = "Assets/Sprites/Cannon/";
         List<ImageManager.ImageData> projectileSpriteFiles = ImageManager.getSpritesInDirectory(projectileSpritePath);

         foreach (ImageManager.ImageData imgData in projectileSpriteFiles) {
            Sprite sourceSprite = imgData.sprite;
            monsterPanel.projectileSpriteList.Add(imgData.imagePath, sourceSprite);
         }
      }
   }

   private void createNewTemplate (BattlerData monsterData) {
      string itemName = "Undefined";
      monsterData.enemyType = Enemy.Type.Coralbow;

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
         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateData(monsterData);
            toolManager.loadAllDataFiles();
         });

         template.gameObject.SetActive(true);
      }
   }

   public void refreshXML () {
      toolManager.loadAllDataFiles();
      abilityToolManager.loadAllDataFiles();
   }

   public void updatePanelWithBattlerData (Dictionary<string, BattlerData> battlerData) {
      // Clear all the rows
      monsterTemplateParent.gameObject.DestroyChildren();

      // Create a row for each monster element
      foreach (BattlerData battler in battlerData.Values) {
         EnemyDataTemplate template = Instantiate(monsterTemplate, monsterTemplateParent);
         template.updateItemDisplay(battler);
         template.editButton.onClick.AddListener(() => {
            monsterPanel.currentXMLTemplate = template;
            monsterPanel.loadData(battler);
            monsterPanel.gameObject.SetActive(true);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateData(battler);
            toolManager.loadAllDataFiles();
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);

            Enemy.Type type = battler.enemyType;
            toolManager.deleteMonsterDataFile(new BattlerData { enemyType = type, enemyName = battler.enemyName });
            toolManager.loadAllDataFiles();
            abilityToolManager.loadAllDataFiles();
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
