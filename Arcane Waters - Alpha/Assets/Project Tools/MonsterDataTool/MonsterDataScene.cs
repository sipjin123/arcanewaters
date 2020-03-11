using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.SceneManagement;
using static MonsterToolManager;

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

   // Quick Access to ability Tool
   public Button[] openAbilityTool;

   #endregion

   private void Awake () {
      foreach (Button button in openAbilityTool) {
         button.onClick.AddListener(() => {
            SceneManager.LoadScene(MasterToolScene.abilityScene);
         });
      }

      if (!MasterToolAccountManager.canAlterData()) {
         createTemplateButton.gameObject.SetActive(false);
      }

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
      monsterData.enemyType = Enemy.Type.None;

      EnemyDataTemplate template = GenericEntryTemplate.createGenericTemplate(monsterTemplate.gameObject, toolManager, monsterTemplateParent.transform).GetComponent<EnemyDataTemplate>();
      template.editButton.onClick.AddListener(() => {
         monsterPanel.currentXMLTemplate = template;
         monsterPanel.loadData(monsterData, -1, false);
         monsterPanel.gameObject.SetActive(true);
      });
      template.deleteButton.onClick.AddListener(() => {
            toolManager.deleteMonsterDataFile(template.xmlId);
      });
      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateData(monsterData);
      });

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void refreshXML () {
      abilityToolManager.loadXML();
   }

   public void updatePanelWithBattlerData (List<BattlerXMLContent> battlerData) {
      // Clear all the rows
      monsterTemplateParent.gameObject.DestroyChildren();

      // Create a row for each monster element
      foreach (BattlerXMLContent rawData in battlerData) {
         BattlerData battler = rawData.battler;

         EnemyDataTemplate template = GenericEntryTemplate.createGenericTemplate(monsterTemplate.gameObject, toolManager, monsterTemplateParent.transform).GetComponent<EnemyDataTemplate>();
         template.updateItemDisplay(battler, rawData.isEnabled, rawData.xmlId);
         template.editButton.onClick.AddListener(() => {
            monsterPanel.currentXMLTemplate = template;
            monsterPanel.loadData(battler, rawData.xmlId, rawData.isEnabled);
            monsterPanel.gameObject.SetActive(true);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateData(battler);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);

            Enemy.Type type = battler.enemyType;
            toolManager.deleteMonsterDataFile(template.xmlId);
         });

         try {
            template.itemIcon.sprite = ImageManager.getSprite(battler.imagePath);
         } catch {
            template.itemIcon.sprite = monsterPanel.emptySprite;
         }

         if (!Util.hasValidEntryName(battler.enemyName)) {
            template.setWarning();
         }
         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
