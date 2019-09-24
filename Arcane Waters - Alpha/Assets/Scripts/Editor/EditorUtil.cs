using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EditorUtil : EditorWindow {
   #region Public Variables

   #endregion

   [MenuItem("Util/Clear Image Manager")]
   public static void clearImagerManager () {
      // Find the Image Manager and clear out the stored paths
      ImageManager imageManager = FindObjectOfType<ImageManager>();

      if (imageManager == null) {
         Debug.Log("Couldn't find the Image Manager in the scene, so not updating it.");
         return;
      }

      imageManager.imageDataList.Clear();
   }

   [MenuItem("Util/Update Image Manager")]
   public static void updateImagerManager () {
      // Find the Image Manager and clear out the stored paths
      ImageManager imageManager = FindObjectOfType<ImageManager>();

      if (imageManager == null) {
         Debug.Log("Couldn't find the Image Manager in the scene, so not updating it.");
         return;
      }

      imageManager.imageDataList.Clear();

      // Look through all of our stuff in the Assets folder
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about textures
         if (assetPath.StartsWith("Assets/Sprites")) {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprite == null) {
               continue;
            }

            // Find the image name
            string imageName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // Create a new Image Data instance and keep track of the name, path, and sprites
            ImageManager.ImageData imageData = new ImageManager.ImageData();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            imageData.imageName = imageName;
            imageData.imagePath = assetPath;
            imageData.imagePathWithoutExtension = System.IO.Path.ChangeExtension(assetPath, null);
            imageData.texture2D = texture;
            imageData.sprite = sprite;
            imageData.sprites = new List<Sprite>();

            // We have to load the animation frames separately
            Object[] data = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            // Cast the objects into Sprites
            foreach (Object obj in data) {
               if (obj is Sprite) {
                  imageData.sprites.Add((Sprite) obj);
               }
            }

            imageData.sprites = imageData.sprites.OrderBy(_ => extractInteger(_.name)).ToList();

            // Add the new Image Data instance to the Image Manager's list
            imageManager.imageDataList.Add(imageData);
         }
      }

      // Sort by image name
      imageManager.imageDataList = imageManager.imageDataList.OrderBy(o => o.imageName).ToList();

      // Save the changes in the scene
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
   }

   public static int extractInteger (string name) {
      string newString = "";
      for (int i = name.Length - 1; i > 0; i--) {
         if (name[i] == '_') {
            break;
         }
         newString.Insert(0, name[i].ToString());
      }

      try {
         return int.Parse(newString);
      } 
      catch {
         return 0;
      }
   }

   [MenuItem("Util/Set Image Read Write")]
   public static void changeReadWrite () {
      // Loop through all of our assets
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about our sprite assets
         if (!assetPath.StartsWith("Assets/Sprites/")) {
            continue;
         }

         Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

         // We only need to enable read/write on certain folders
         List<string> folders = new List<string>() { "Ships", "Armor", "Bodies", "Weapons", "Eyes", "Hair", "NPCs" };
         bool shouldChange = false;

         foreach (string folder in folders) {
            if (assetPath.StartsWith("Assets/Sprites/" + folder)) {
               shouldChange = true;
            }
         }

         if (sprite == null || !shouldChange) {
            continue;
         }

         // Change the isReadable setting
         TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(assetPath);
         importer.isReadable = true;
         EditorUtility.SetDirty(importer);
         importer.SaveAndReimport();
      }
   }
   
   [MenuItem("Util/Test Window")]
   public static void testWindow () {
      // Show existing window instance. If one doesn't exist, make one.
      EditorWindow.GetWindow(typeof(TestWindow));

      // PreExportMethods.AppendCSC();
   }

   #region Private Variables

   #endregion
}
