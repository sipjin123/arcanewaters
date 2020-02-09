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
      public static string BGELEMENTS_DIRECTORY = "/BGElements/";
      public static string BUSH_DIRECTORY = "/Bush/";
      public static string FOLIAGE_DIRECTORY = "/Foliage/";
      public static string GRASS_DIRECTORY = "/Grass/";
      public static string GROUND_DETAILS_DIRECTORY = "/GroundDetails/";
      public static string ROCKS_DIRECTORY = "/Rocks/";
      public static string SKIES_DIRECTORY = "/Skies/";
      public static string TERRAIN_DIRECTORY = "/Terrain/";
      public static string TREES_DIRECTORY = "/Trees/";
      public static string FUNCTIONAL_DIRECTORY = "/FunctionalElements/";
      public static string PLACEHOLDER_DIRECTORY = "/PlaceHolderBattlers/";
      
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

      public enum BGContentType
      {
         None = 0,
         BGEelements = 1,
         Bush = 2,
         Foliage = 3,
         Grass = 4,
         GroundDetails = 5,
         RocksDirectory = 6,
         SkiesDirectory = 7,
         TerrainDirectory = 8,
         TreesDirectory = 9,
         FunctionalElements = 10,
         PlaceholderElements = 11
      }

      public enum BGContentCategory
      {
         None = 0,
         BG = 1,
         DisplayElements = 2,
         SpawnPoints_Attackers = 3,
         SpawnPoints_Defenders = 4,
         PlaceHolders = 5,
         Animated = 6
      }

      public class SpriteSelectionContent
      {
         // The sprite directory
         public string spritePath;

         // The layer type of the image
         public LayerType layerType;

         // Determines the content category
         public BGContentCategory contentCategory;
      }

      #endregion

      private void Start () {
         self = this;

         buttonDirectories = new List<ButtonDirectory>();
         foreach (BGContentType contentType in Enum.GetValues(typeof(BGContentType))) {
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

      private void setSpriteSelection (string buttonDirectory, LayerType layerType, BGContentCategory contentCategory) {
         spriteParent.gameObject.DestroyChildren();

         if (buttonDirectory != "") {
            List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(MAIN_DIRECTORY + buttonDirectory);

            float previousBounds = 0;
            foreach (ImageManager.ImageData imgData in spriteIconFiles) {
               GameObject prefab = Instantiate(emptySprite, spriteParent);

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

      private SpriteSelectionContent translateEnumDirectory (BGContentType contentType) {
         string newPath = "";
         ImageManipulator.LayerType layerType = ImageManipulator.LayerType.None;
         BGContentCategory newContentCategory = BGContentCategory.None;

         switch (contentType) {
            case BGContentType.BGEelements:
               newPath = BGELEMENTS_DIRECTORY;
               layerType = ImageManipulator.LayerType.Background;
               newContentCategory = BGContentCategory.BG;
               break;
            case BGContentType.Bush:
               newPath = BUSH_DIRECTORY;
               layerType = ImageManipulator.LayerType.Foreground;
               newContentCategory = BGContentCategory.DisplayElements;
               break;
            case BGContentType.Foliage:
               newPath = FOLIAGE_DIRECTORY;
               layerType = ImageManipulator.LayerType.Foreground;
               newContentCategory = BGContentCategory.Animated;
               break;
            case BGContentType.Grass:
               newPath = GRASS_DIRECTORY;
               layerType = ImageManipulator.LayerType.Foreground;
               newContentCategory = BGContentCategory.Animated;
               break;
            case BGContentType.GroundDetails:
               newPath = GROUND_DETAILS_DIRECTORY;
               layerType = ImageManipulator.LayerType.Foreground;
               newContentCategory = BGContentCategory.DisplayElements;
               break;
            case BGContentType.RocksDirectory:
               newPath = ROCKS_DIRECTORY;
               layerType = ImageManipulator.LayerType.Foreground;
               newContentCategory = BGContentCategory.DisplayElements;
               break;
            case BGContentType.SkiesDirectory:
               newPath = SKIES_DIRECTORY;
               layerType = ImageManipulator.LayerType.Background;
               newContentCategory = BGContentCategory.BG;
               break;
            case BGContentType.TerrainDirectory:
               newPath = TERRAIN_DIRECTORY;
               layerType = ImageManipulator.LayerType.Midground;
               newContentCategory = BGContentCategory.DisplayElements;
               break;
            case BGContentType.TreesDirectory:
               newPath = TREES_DIRECTORY;
               layerType = ImageManipulator.LayerType.Foreground;
               newContentCategory = BGContentCategory.DisplayElements;
               break;
            case BGContentType.FunctionalElements:
               newPath = FUNCTIONAL_DIRECTORY;
               layerType = ImageManipulator.LayerType.Overlay;
               newContentCategory = BGContentCategory.SpawnPoints_Attackers;
               break;
            case BGContentType.PlaceholderElements:
               newPath = PLACEHOLDER_DIRECTORY;
               layerType = ImageManipulator.LayerType.PlaceHolders;
               newContentCategory = BGContentCategory.PlaceHolders;
               break;
         }
         return new SpriteSelectionContent { layerType = layerType, spritePath = newPath, contentCategory = newContentCategory};
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