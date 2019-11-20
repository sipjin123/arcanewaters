﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerJobScene : MonoBehaviour
{
   #region Public Variables

   // Holds the prefab for selection templates
   public PlayerClassTemplate itemTemplatePrefab;

   // Holds the tool manager reference
   public PlayerJobToolManager toolManager;

   // The parent holding the item template
   public GameObject itemTemplateParent;

   // Holds the data panel
   public PlayerJobPanel playerJobPanel;

   // Main menu Buttons
   public Button createButton, mainMenuButton;

   // Reference to the empty sprite
   public Sprite emptySprite;

   #endregion

   private void Awake () {
      createButton.onClick.AddListener(() => {
         createTemplate();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      playerJobPanel.gameObject.SetActive(false);
   }

   private void createTemplate () {
      PlayerJobData jobData = new PlayerJobData();

      jobData.jobName = "Undefined";
      jobData.type = Jobs.Type.None;

      PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
      template.editButton.onClick.AddListener(() => {
         playerJobPanel.loadPlayerJobData(jobData);
         playerJobPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         toolManager.deleteDataFile(jobData);
         toolManager.loadXMLData();
      });

      template.duplicateButton.onClick.AddListener(() => {
         toolManager.duplicateXMLData(jobData);
         toolManager.loadXMLData();
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(jobData.jobIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadPlayerJobData (Dictionary<string, PlayerJobData> data) {
      itemTemplateParent.gameObject.DestroyChildren();

      // Create a row for each player job
      foreach (PlayerJobData job in data.Values) {
         PlayerClassTemplate template = Instantiate(itemTemplatePrefab, itemTemplateParent.transform);
         template.nameText.text = job.jobName;
         template.editButton.onClick.AddListener(() => {
            playerJobPanel.loadPlayerJobData(job);
            playerJobPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteDataFile(job);
            toolManager.loadXMLData();
         });

         template.duplicateButton.onClick.AddListener(() => {
            toolManager.duplicateXMLData(job);
            toolManager.loadXMLData();
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(job.jobIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }
}