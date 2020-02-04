using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

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

      public static float spacing = .5f;

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
         TreesDirectory = 9
      }

      #endregion

      private void Start () {
         buttonDirectories = new List<ButtonDirectory>();
         foreach (BGContentType contentType in Enum.GetValues(typeof(BGContentType))) {
            ButtonDirectory newButtonDirectory = new ButtonDirectory();

            Button newButton = Instantiate(directoryButtonPrefab.gameObject, directoryButtonParent).GetComponent<Button>();
            newButton.GetComponentInChildren<Text>().text = contentType.ToString();
            newButton.gameObject.SetActive(true);

            newButtonDirectory.button = newButton;
            newButtonDirectory.directory = translateEnumDirectory(contentType);
            newButton.onClick.AddListener(() => {
               if (cachedButton != null) {
                  cachedButton.image.color = Color.white;
               }
               cachedButton = newButton;
               cachedButton.image.color = Color.red;
               setSpriteSelection(newButtonDirectory.directory);
            });

            buttonDirectories.Add(newButtonDirectory);
         }
      }

      private void setSpriteSelection (string buttonDirectory) {
         spriteParent.gameObject.DestroyChildren();
         List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(MAIN_DIRECTORY + buttonDirectory);

         float previousBounds = 0;
         foreach (ImageManager.ImageData imgData in spriteIconFiles) {
            GameObject prefab = Instantiate(emptySprite, spriteParent);

            float newXValue = previousBounds ;
            prefab.transform.localPosition += new Vector3(newXValue, 0, 0);
            previousBounds = newXValue + spacing +imgData.sprite.bounds.size.x;

            SpriteSelectionTemplate spriteTemplate = prefab.GetComponent<SpriteSelectionTemplate>();
            spriteTemplate.imageIcon.sprite = imgData.sprite;
            spriteTemplate.spritePath = imgData.imagePath;
            spriteTemplate.gameObject.AddComponent<BoxCollider2D>();
            prefab.SetActive(true);
         }
      }

      private string translateEnumDirectory (BGContentType contentType) {
         string returnString = "";
         switch (contentType) {
            case BGContentType.BGEelements:
               returnString = BGELEMENTS_DIRECTORY;
               break;
            case BGContentType.Bush:
               returnString = BUSH_DIRECTORY;
               break;
            case BGContentType.Foliage:
               returnString = FOLIAGE_DIRECTORY;
               break;
            case BGContentType.Grass:
               returnString = GRASS_DIRECTORY;
               break;
            case BGContentType.GroundDetails:
               returnString = GROUND_DETAILS_DIRECTORY;
               break;
            case BGContentType.RocksDirectory:
               returnString = ROCKS_DIRECTORY;
               break;
            case BGContentType.SkiesDirectory:
               returnString = SKIES_DIRECTORY;
               break;
            case BGContentType.TerrainDirectory:
               returnString = TERRAIN_DIRECTORY;
               break;
            case BGContentType.TreesDirectory:
               returnString = TREES_DIRECTORY;
               break;
         }
         return returnString;
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