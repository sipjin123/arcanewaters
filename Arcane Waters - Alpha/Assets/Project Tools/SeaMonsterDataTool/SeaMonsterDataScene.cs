﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.SceneManagement;
using static SeaMonsterToolManager;

public class SeaMonsterDataScene : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool manager
   public SeaMonsterToolManager toolManager;

   // Reference to monster ingredient panel
   public SeaMonsterDataPanel monsterPanel;

   // Parent holder of the monster templates
   public Transform monsterTemplateParent;

   // Monster template
   public SeaMonsterDataTemplate monsterTemplate;

   // Button that generates a new monster template
   public Button createTemplateButton;

   // Determines if the sprites have been initialized
   public bool hasBeenInitialized;

   // Opens the main tool
   public Button openMainTool;

   #endregion

   private void Start () {
      if (!MasterToolAccountManager.canAlterData()) {
         createTemplateButton.gameObject.SetActive(false);
      }

      monsterPanel.gameObject.SetActive(false);
      openMainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
      createTemplateButton.onClick.AddListener(() => createNewTemplate(new SeaMonsterEntityData()));

      if (!hasBeenInitialized) {
         hasBeenInitialized = true;
         string spritePath = "Assets/Sprites/Enemies/";
         List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(spritePath);

         foreach (ImageManager.ImageData imgData in spriteIconFiles) {
            if (imgData.imagePath.Contains("SeaMonsters")) {
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
      }
   }

   private void createNewTemplate (SeaMonsterEntityData monsterData) {
      monsterData.seaMonsterType = SeaMonsterEntity.Type.None;

      SeaMonsterDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(monsterTemplate.gameObject, toolManager, monsterTemplateParent.transform).GetComponent<SeaMonsterDataTemplate>();
      template.xml_id = -1;
      template.editButton.onClick.AddListener(() => {
         monsterPanel.currentXMLTemplate = template;
         monsterPanel.loadData(monsterData, -1, false);
         monsterPanel.gameObject.SetActive(true);
      });
      template.deleteButton.onClick.AddListener(() => {
         toolManager.deleteMonsterDataFile(template.xml_id);
      });
      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateFile(monsterData);
      });

      template.gameObject.SetActive(true);
   }

   public void updatePanelWithData (List<SeaMonsterXMLContent> monsterData) {
      // Clear all the rows
      monsterTemplateParent.gameObject.DestroyChildren();

      // Create a row for each monster element
      foreach (SeaMonsterXMLContent rawData in monsterData) {
         SeaMonsterEntityData seaMonsterData = rawData.seaMonsterData;

         SeaMonsterDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(monsterTemplate.gameObject, toolManager, monsterTemplateParent.transform).GetComponent<SeaMonsterDataTemplate>();
         template.xml_id = rawData.xmlId;
         template.updateItemDisplay(seaMonsterData, rawData.isEnabled);
         template.editButton.onClick.AddListener(() => {
            monsterPanel.currentXMLTemplate = template;
            monsterPanel.loadData(seaMonsterData, rawData.xmlId, rawData.isEnabled);
            monsterPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            
            toolManager.deleteMonsterDataFile(template.xml_id);
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateFile(seaMonsterData);
         });

         try {
            template.itemIcon.sprite = ImageManager.getSprite(seaMonsterData.avatarSpritePath);
         } catch {
            template.itemIcon.sprite = monsterPanel.emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
