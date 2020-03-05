using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using static BackgroundTool.ImageManipulator;

namespace BackgroundTool
{
   public class ImageLoader : MonoBehaviour
   {
      #region Public Variables

      // Main Directory for the image selection
      public static string MAIN_DIRECTORY = "Assets/Sprites/BackgroundEntities";

      // Sub Directories for the image selection
      public static string SKIESANDGROUND_ELEMENTS_DIRECTORY = "/SkiesAndGround/";
      public static string BACKGROUND_ELEMENTS_DIRECTORY = "/BackgroundElements/";
      public static string MIDGROUND_ELEMENTS_DIRECTORY = "/MidgroundElements/";
      public static string FOREGROUND_ELEMENTS_DIRECTORY = "/ForegroundElements/";
      public static string FUNCTIONAL_ELEMENTS_DIRECTORY = "/FunctionalElements/";
      public static string GROUND_ELEMENTS_DIRECTORY = "/GroundElements/";
      public static string PLACEHOLDER_ELEMENTS_DIRECTORY = "/PlaceHolderElements/";
      public static string MIDGROUND_INTERACTIVE_ELEMENTS_DIRECTORY = "/MidgroundInteractiveElements/";
      public static string BACKGROUND_ANIMATED_ELEMENTS_DIRECTORY = "/BackgroundAnimatedElements/";
      public static string MIDGROUND_ANIMATED_ELEMENTS_DIRECTORY = "/MidgroundAnimatedElements/";
      public static string FOREGROUND_ANIMATED_ELEMENTS_DIRECTORY = "/ForegroundAnimatedElements/";

      // Spacing between sprite selections
      public static float spacing = .25f;

      // Empty sprite that will be addressed later
      public GameObject emptySprite;

      // Content holder of the sprites to be spawned
      public Transform spriteParent;

      // List of buttons that addresses directories
      public List<ButtonDirectory> buttonDirectories;

      // Directory button spawning
      public Button directoryButtonPrefab;
      public Transform directoryButtonParent;

      // Reference to the recently clicked button
      public Button cachedButton;

      // Self
      public static ImageLoader self;

      public enum BGContentDirectory
      {
         None = 0,
         Background = 1,
         Midground = 2,
         Foreground = 3,
         Placeholders = 4,
         SkiesAndGround = 5,
         FunctionalElements = 6,
         BackgroundAnimated = 7,
         MidgroundAnimated = 8,
         ForegroundAnimated = 9,
         MidgroundInteractive = 10,
      }

      public enum BGLayer
      {
         None = 0,
         SkiesAndGround = 1,
         Background = 2,
         Midground = 3,
         Foreground = 4,
         Overlay = 5,
         PlaceHolders = 6
      }

      public enum BGContentCategory
      {
         None = 0,
         BG = 1,
         DisplayElements = 2,
         SpawnPoints_Attackers = 3,
         SpawnPoints_Defenders = 4,
         PlaceHolders = 5,
         Interactive = 6,
         Animating = 7
      }

      public class SpriteSelectionContent
      {
         // The sprite directory
         public string spritePath;

         // The layer type of the image
         public BGLayer layerType;

         // Determines the content category
         public BGContentCategory contentCategory;
      }

      #endregion

      private void Start () {
         self = this;

         buttonDirectories = new List<ButtonDirectory>();
         foreach (BGContentDirectory contentType in Enum.GetValues(typeof(BGContentDirectory))) {
            if (contentType != BGContentDirectory.None) {
               ButtonDirectory newButtonDirectory = new ButtonDirectory();

               Button newButton = Instantiate(directoryButtonPrefab.gameObject, directoryButtonParent).GetComponent<Button>();
               newButton.GetComponentInChildren<Text>().text = contentType.ToString();
               newButton.gameObject.SetActive(true);

               newButtonDirectory.button = newButton;
               SpriteSelectionContent newContent = translateEnumDirectory(contentType);
               newButtonDirectory.directory = newContent.spritePath;
               newButton.onClick.AddListener(() => {
                  if (cachedButton != null) {
                     cachedButton.image.color = Color.white;
                  }
                  cachedButton = newButton;
                  cachedButton.image.color = Color.red;
                  setSpriteSelection(newButtonDirectory.directory, newContent.layerType, newContent.contentCategory);
               });

               buttonDirectories.Add(newButtonDirectory);
            }
         }
      }

      private void setSpriteSelection (string buttonDirectory, BGLayer layerType, BGContentCategory contentCategory) {
         spriteParent.gameObject.DestroyChildren();

         if (buttonDirectory != "") {
            List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(buttonDirectory);
            float previousBounds = 0;
            foreach (ImageManager.ImageData imgData in spriteIconFiles) {
               GameObject prefab = Instantiate(emptySprite, spriteParent);

               if (imgData.imageName.ToLower().Contains("tree")) {
                  prefab.transform.localScale = new Vector3(.5f, .5f, 1);
               }

               float newYValue = previousBounds;
               float spriteWidth = imgData.sprite.bounds.size.x;

               if (spriteWidth > 6) {
                  // Get corner of the sprite if the sprite is landscape/background
                  prefab.transform.localPosition = new Vector3((spriteWidth / 2) - 1, newYValue, 0);
               } else {
                  // Set sprite as is if it is a background element
                  prefab.transform.localPosition = new Vector3(0, newYValue, 0);
               }

               previousBounds = newYValue - spacing - imgData.sprite.bounds.size.y;

               SpriteSelectionTemplate spriteTemplate = prefab.GetComponent<SpriteSelectionTemplate>();
               spriteTemplate.spriteIcon.sprite = imgData.sprite;
               spriteTemplate.spritePath = imgData.imagePath;
               spriteTemplate.layerType = layerType;
               spriteTemplate.contentCategory = contentCategory;
               spriteTemplate.gameObject.AddComponent<BoxCollider2D>();
               prefab.SetActive(true);
            }
         }
      }

      private SpriteSelectionContent translateEnumDirectory (BGContentDirectory contentType) {
         string newPath = "";
         string extendedPath = "";
         BGLayer layerType = BGLayer.None;
         BGContentCategory newContentCategory = BGContentCategory.None;
         newPath = MAIN_DIRECTORY;

         switch (contentType) {
            case BGContentDirectory.SkiesAndGround:
               layerType = BGLayer.SkiesAndGround;
               newContentCategory = BGContentCategory.BG;
               extendedPath = SKIESANDGROUND_ELEMENTS_DIRECTORY;
               break;
            case BGContentDirectory.Placeholders:
               layerType = BGLayer.PlaceHolders;
               newContentCategory = BGContentCategory.PlaceHolders;
               extendedPath = PLACEHOLDER_ELEMENTS_DIRECTORY;
               break;
            case BGContentDirectory.FunctionalElements:
               layerType = BGLayer.Overlay;
               newContentCategory = BGContentCategory.SpawnPoints_Attackers;
               extendedPath = FUNCTIONAL_ELEMENTS_DIRECTORY;
               break;

            case BGContentDirectory.Background:
               layerType = BGLayer.Background;
               newContentCategory = BGContentCategory.DisplayElements;
               extendedPath = BACKGROUND_ELEMENTS_DIRECTORY;
               break;
            case BGContentDirectory.BackgroundAnimated:
               layerType = BGLayer.Background;
               newContentCategory = BGContentCategory.Animating;
               extendedPath = BACKGROUND_ANIMATED_ELEMENTS_DIRECTORY;
               break;

            case BGContentDirectory.Foreground:
               layerType = BGLayer.Foreground;
               newContentCategory = BGContentCategory.DisplayElements;
               extendedPath = FOREGROUND_ELEMENTS_DIRECTORY;
               break;
            case BGContentDirectory.ForegroundAnimated:
               layerType = BGLayer.Foreground;
               newContentCategory = BGContentCategory.Animating;
               extendedPath = FOREGROUND_ANIMATED_ELEMENTS_DIRECTORY;
               break;

            case BGContentDirectory.Midground:
               layerType = BGLayer.Midground;
               newContentCategory = BGContentCategory.DisplayElements;
               extendedPath = MIDGROUND_ELEMENTS_DIRECTORY;
               break;
            case BGContentDirectory.MidgroundAnimated:
               layerType = BGLayer.Midground;
               newContentCategory = BGContentCategory.Animating;
               extendedPath = MIDGROUND_ANIMATED_ELEMENTS_DIRECTORY;
               break;
            case BGContentDirectory.MidgroundInteractive:
               layerType = BGLayer.Midground;
               newContentCategory = BGContentCategory.Interactive;
               extendedPath = MIDGROUND_INTERACTIVE_ELEMENTS_DIRECTORY;
               break;
         }
         newPath += extendedPath;
         return new SpriteSelectionContent { layerType = layerType, spritePath = newPath, contentCategory = newContentCategory };
      }

      #region Private Variables

      #endregion
   }
}

[Serializable]
public class ButtonDirectory
{
   // Button to be clicked that is paired to directory
   public Button button;

   // Directory of the sprite
   public string directory;
}