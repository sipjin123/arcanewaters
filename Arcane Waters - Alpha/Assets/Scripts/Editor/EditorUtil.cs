using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Text;

public class EditorUtil : EditorWindow {
   #region Public Variables

   #endregion

   [MenuItem("Util/Set As Local Server")]
   public static void SetLocalServer () {
      #if IS_SERVER_BUILD
      DB_Main.setServer("127.0.0.1");
      Debug.Log("Set as Local Server");
      #endif
   }

   [MenuItem("Util/Set As Remote Server")]
   public static void SetRemoteServer () {
      #if IS_SERVER_BUILD
      DB_Main.setServer(DB_Main.RemoteServer);
      Debug.Log("Set as Remote Server");
      #endif
   }

   [MenuItem("Util/Clear Prefs")]
   public static void ClearPrefs () {
      PlayerPrefs.DeleteAll();
   }

   [MenuItem("Util/Launch: Main Scene (Ctrl+J) %j")]
   public static void PlayMainScene () {
      if (EditorApplication.isPlaying == true) {
         EditorApplication.isPlaying = false;
         return;
      }

      EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
      EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
      EditorApplication.isPlaying = true;
   }

   [MenuItem("Util/Launch: Master Tool (Ctrl+K) %k")]
   public static void PlayMasterToolScene () {
      if (EditorApplication.isPlaying == true) {
         EditorApplication.isPlaying = false;
         return;
      }

      EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
      EditorSceneManager.OpenScene("Assets/Project Tools/ToolScenes/MasterTool.unity");
      EditorApplication.isPlaying = true;
   }

   [MenuItem("Util/Update Image Manager (Ctrl+L) %l")]
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
      EditorUtility.SetDirty(imageManager);
      PrefabUtility.ApplyPrefabInstance(imageManager.gameObject, InteractionMode.UserAction);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
   }


   [MenuItem("Util/Update Audio Manager")]
   public static void updateAudioManager () {
      // Find the Audio Manager and clear out the stored paths
      AudioClipManager audioManager = FindObjectOfType<AudioClipManager>();

      if (audioManager == null) {
         Debug.Log("Couldn't find the Audio Manager in the scene, so not updating it.");
         return;
      }

      if (audioManager.audioDataList != null) {
         audioManager.audioDataList.Clear();
      }

      // Look through all of our stuff in the Assets folder
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about audio clips
         if (assetPath.StartsWith("Assets/Resources/Sound/Effects")) {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            if (clip == null) {
               continue;
            }

            // Find the image name
            string audioName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // Create a new Audio Clip Data instance and keep track of the name, path, and audio clips
            AudioClipManager.AudioClipData audioData = new AudioClipManager.AudioClipData();
            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            audioData.audioName = audioName;
            audioData.audioPath = assetPath;
            audioData.audioPathWithoutExtension = System.IO.Path.ChangeExtension(assetPath, null);
            audioData.audioClip= audioClip;

            // Add the new Audio Clip Data instance to the Audio Manager's list
            audioManager.audioDataList.Add(audioData);
         }
      }

      // Sort by audio clip name
      audioManager.audioDataList = audioManager.audioDataList.OrderBy(o => o.audioName).ToList();

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
         newString = newString.Insert(0, name[i].ToString());
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

   [MenuItem("Util/Find GUID")]
   static void findAssetByGUID () {
      string guid = "a99e757d831d5574cbccda70fe4dbf1e";

      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         if (AssetDatabase.AssetPathToGUID(assetPath) == guid) {
            Debug.Log("The asset with the specified guid is at path: " + assetPath);
         }
      }
   }

#region Private Variables

#endregion
}
