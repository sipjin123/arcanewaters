using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

public class HatClipMaskGenerationTool : MonoBehaviour
{
   #region Public Variables

   #endregion

   public static HatClipMaskGenerationSettings findHatClipMaskGenerationSettings () {
      D.debug("Searching for the clip mask generation settings...");
      string[] guids = AssetDatabase.FindAssets("HatClipMaskGenerationSettings");

      if (guids == null || guids.Length == 0) {
         D.error("Searching for the clip mask generation settings: failed - none found. Exiting process.");
         return null;
      }

      D.debug($"Searching for the clip mask generation settings: ok - found {guids.Length} GUIDs.");

      foreach (string guid in guids) {
         string path = AssetDatabase.GUIDToAssetPath(guid);

         D.debug($"Found valid clipmask generation settings at {path}.");
         HatClipMaskGenerationSettings settings = AssetDatabase.LoadAssetAtPath<HatClipMaskGenerationSettings>(path);

         if (settings == null) {
            D.error($"The settings object found at {path} wasn't valid after all.");
            continue;
         }

         if (settings.values == null) {
            D.error($"The settings object found at {path} is valid but there are no textures defined.");
            continue;
         }

         return settings;
      }

      return null;
   }

   #region ClipMasks

   [MenuItem("Util/Generate Clip Masks for Hats")]
   public static void generateClipMasks () {
      D.debug($"HatClipMaskGenerationTool: Task started...");

      rep("Searching for Generation Settings...", 1.0f);
      HatClipMaskGenerationSettings settings = findHatClipMaskGenerationSettings();

      if (settings == null) {
         EditorUtility.ClearProgressBar();
      }

      List<Texture2D> textures = getTexturesToBeProcessed(settings);

      rep("Deleting Old Clip Masks...", 0.0f);

      int counter = 0;
      int total = textures.Count;

      foreach (Texture2D texture in textures) {
         rep($"Deleting Clip Mask for '{texture.name}'... ({counter + 1}/{total})", (float) (counter + 1) / textures.Count);
         deleteClipMaskOf(texture);
         counter++;
      }

      rep("Generating Clip Masks...", 0f);

      counter = 0;
      total = textures.Count;

      foreach (Texture2D texture in textures) {
         rep($"Generating Clip Masks for '{texture.name}'... ({counter + 1}/{total})", (float) (counter + 1) / total);
         Texture2D clipMaskTexture = generateClipMaskFor(texture);
         applyExtraClipMaskTo(clipMaskTexture, getExtraClipMaskFor(texture, settings));
         applyPaintedClipMaskTo(clipMaskTexture, getPaintedClipMaskFor(texture, settings));
         addTextureToProject(clipMaskTexture, computePathOfClipMaskFor(texture));
         counter++;
      }

      rep("Importing Generated Clip Masks...", 1.0f);

      foreach (Texture2D texture in textures) {
         importAsset(computePathOfClipMaskFor(texture));
      }

      EditorUtility.ClearProgressBar();

      D.debug($"HatClipMaskGenerationTool: Task completed.");
   }

   public static Texture2D getClipMaskFor (Texture2D texture) {
      string clipMaskPath = computePathOfClipMaskFor(texture);
      return AssetDatabase.LoadAssetAtPath<Texture2D>(clipMaskPath);
   }

   public static Texture2D getExtraClipMaskFor (Texture2D texture, HatClipMaskGenerationSettings settings) {
      return getSettingFor(texture, settings).extraClipMaskTexture;
   }
   
   public static Texture2D getPaintedClipMaskFor (Texture2D texture, HatClipMaskGenerationSettings settings) {
      return getSettingFor(texture, settings).paintedClipMaskTexture;
   }

   public static bool clipMaskExistsFor (Texture2D texture) {
      return getClipMaskFor(texture) != null;
   }

   public static Texture2D generateClipMaskFor (Texture2D texture) {

      D.debug($"Creating a Clip mask for the texture '{texture.name}'...");

      // Create the clipmask texture
      Texture2D clipMaskTexture = new Texture2D(texture.width, texture.height, TextureFormat.R8, false);

      for (int row = 0; row < clipMaskTexture.height; row++) {
         for (int column = 0; column < clipMaskTexture.width; column++) {
            bool isPixelTransparent = texture.GetPixel(column, row).a < 0.999f;
            clipMaskTexture.SetPixel(column, row, isPixelTransparent ? Color.red : Color.black);
         }
      }

      clipMaskTexture.name = getClipMaskNameFor(texture);

      D.debug($"Creating a Clip mask for the texture '{texture.name}': DONE");

      return clipMaskTexture;
   }

   public static bool applyExtraClipMaskTo (Texture2D clipMaskTexture, Texture2D extraClipMaskTexture) {
      if (clipMaskTexture == null) {
         D.debug($"Invalid ClipMask Texture");
         return false;
      }

      if (extraClipMaskTexture == null) {
         D.debug($"No ExtraClipMask Texture specified for ClipMask '{clipMaskTexture.name}'. Skipping...");
         return false;
      }

      D.debug($"Applying extra clipping to the clipmask texture '{clipMaskTexture.name}'...");

      List<Sprite> sprites = getSpritesOf(extraClipMaskTexture);

      foreach (Sprite sprite in sprites) {
         for (int row = 0; row < sprite.rect.height; row++) {
            for (int column = 0; column < sprite.rect.width; column++) {
               int x = Mathf.CeilToInt(sprite.rect.x + column);
               int y = Mathf.CeilToInt(sprite.rect.y + row);
               clipMaskTexture.SetPixel(x, y, Color.cyan);
            }
         }
      }

      D.debug($"Applying extra clipping to the clipmask texture '{clipMaskTexture.name}': DONE");

      return true;
   }

   public static bool applyPaintedClipMaskTo (Texture2D clipMaskTexture, Texture2D paintedClipMaskTexture) {
      // Black pixels in the painted clip mask texture will be merged to the mask
      if (clipMaskTexture == null) {
         D.debug($"Invalid ClipMask Texture");
         return false;
      }

      if (paintedClipMaskTexture == null) {
         D.debug($"No Painted Clipmask Texture specified for ClipMask '{clipMaskTexture.name}'. Skipping...");
         return false;
      }

      D.debug($"Applying painted clipping to the clipmask texture '{clipMaskTexture.name}'...");

      for (int row = 0; row < paintedClipMaskTexture.height; row++) {
         for (int column = 0; column < paintedClipMaskTexture.width; column++) {
            bool isBlack = paintedClipMaskTexture.GetPixel(column, row).r < 0.5f;
            Color originalColor = clipMaskTexture.GetPixel(column, row);
            clipMaskTexture.SetPixel(column, row, isBlack ? Color.cyan : originalColor);
         }
      }

      D.debug($"Applying painted clipping to the clipmask texture '{clipMaskTexture.name}': DONE");

      return true;
   }

   public static List<Texture2D> getAllClipMasks (HatClipMaskGenerationSettings settings) {
      List<Texture2D> clipMasks = new List<Texture2D>();

      foreach (Texture2D texture in getTexturesToBeProcessed(settings)) {
         if (!clipMaskExistsFor(texture)) {
            continue;
         }

         clipMasks.Add(getClipMaskFor(texture));
      }

      return clipMasks;
   }

   public static bool deleteClipMaskOf (Texture2D texture) {
      string clipMaskPath = computePathOfClipMaskFor(texture);
      bool deleted = AssetDatabase.DeleteAsset(clipMaskPath);

      if (!deleted) {
         D.debug($"The clipmask at '{clipMaskPath}' couldn't be deleted.");
      }

      return deleted;
   }

   public static string getClipMaskNameFor (Texture2D texture) {
      if (texture == null) {
         return "";
      }

      return texture.name + "_clipmask.png";
   }

   #endregion

   #region Utils

   private static void rep (string message, float progress) {
      EditorUtility.DisplayProgressBar(_toolName, message, progress);
   }

   public static List<Sprite> getSpritesOf (Texture2D texture) {
      List<Sprite> sprites = new List<Sprite>();
      string path = AssetDatabase.GetAssetPath(texture);

      if (string.IsNullOrEmpty(path)) {
         return sprites;
      }

      UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

      if (assets == null) {
         return sprites;
      }

      foreach (UnityEngine.Object asset in assets) {
         if (asset is Sprite) {
            sprites.Add((Sprite) asset);
         }
      }

      return sprites;
   }

   public static string getPathOf (UnityEngine.Object asset) {
      return AssetDatabase.GetAssetPath(asset);
   }

   public static string getDirectoryOf (UnityEngine.Object asset) {
      string path = getPathOf(asset);

      if (string.IsNullOrWhiteSpace(path)) {
         return "";
      }

      return System.IO.Path.GetDirectoryName(path);
   }

   public static string computePathOfClipMaskFor (Texture2D texture) {

      // Get the folder of the texture
      string folderPath = getDirectoryOf(texture);

      // Compute the file name for a clipmask
      string clipMaskName = getClipMaskNameFor(texture);

      // Compose the path
      return System.IO.Path.Combine(folderPath, clipMaskName);
   }

   public static List<Texture2D> getTexturesToBeProcessed (HatClipMaskGenerationSettings settings) {
      List<Texture2D> textures = new List<Texture2D>();

      if (settings == null || settings.values == null) {
         return textures;
      }

      foreach (HatClipMaskGenerationSetting setting in settings.values) {
         if (setting == null || setting.texture == null || !setting.isEnabled) {
            continue;
         }

         textures.Add(setting.texture);
      }

      return textures;
   }

   public static bool addTextureToProject (Texture2D texture, string path) {
      try {
         byte[] bytes = texture.EncodeToPNG();
         string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
         File.WriteAllBytes(outputPath, bytes);
         return true;
      } catch {
         return false;
      }
   }

   public static void importAsset (string path) {
      AssetDatabase.ImportAsset(path);
   }

   private static HatClipMaskGenerationSetting getSettingFor (Texture2D texture, HatClipMaskGenerationSettings settings) {
      if (texture == null || settings == null || settings.values == null) {
         return null;
      }

      return settings.values.FirstOrDefault(_ => _.texture == texture);
   }

   #endregion

   #region Private Variables

   // The name of the tool
   private static string _toolName = "Hat Clip Mask Generation";

   #endregion
}