using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEditor;
using System;
using System.Linq;

public class HatClipMaskGenerationTool : MonoBehaviour
{
   #region Public Variables

   #endregion

   [MenuItem("Tools/Generate Clip Mask for Hats")]
   public static void generateClipMasks () {
      D.debug("Okay the command is reachable. Now implement it!");
      return;

      foreach (Texture2D texture in getTexturesToBeProcessed()) {
         generateClipMaskFor(texture);
      }
   }

   public static bool clipMaskExistsFor (Texture2D texture) {
      return true;
   }

   public static bool generateClipMaskFor (Texture2D texture) {
      return true;
   }

   /// <summary>
   /// Deletes all the existing clip masks
   /// </summary>
   public static void deleteClipMasks () {
      foreach (Texture2D texture in getTexturesToBeProcessed()) {
         string clipMaskPath = computePathOfClipMaskFor(texture);
         Texture2D clipMaskTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(clipMaskPath);

         if (clipMaskTexture == null) {
            continue;
         }

         bool deleted = AssetDatabase.DeleteAsset(clipMaskPath);

         if (!deleted) {
            D.debug($"The clipmask at '{clipMaskPath}' couldn't be deleted.");
         }
      }
   }

   public static string getPathOf (Texture2D texture) {
      return AssetDatabase.GetAssetPath(texture);
   }

   public static string getDirectoryOf (UnityEngine.Object asset) {
      string path = AssetDatabase.GetAssetPath(asset);

      if (string.IsNullOrWhiteSpace(path)) {
         return "";
      }

      return System.IO.Path.GetDirectoryName(path);
   }

   public static string getClipMaskNameFor (Texture2D texture) {
      if (texture == null) {
         return "";
      }

      return texture + "_clipmask";
   }

   public static string computePathOfClipMaskFor (Texture2D texture) {

      // Get the folder of the texture
      string folderPath = getDirectoryOf(texture);

      // Compute the file name for a clipmask
      string clipMaskName = getClipMaskNameFor(texture);

      // Compose the path
      return System.IO.Path.Combine(folderPath, clipMaskName);
   }

   public static List<string> getPathsOfTexturesToBeProcessed () {
      return getTexturesToBeProcessed().Select(_ => getPathOf(_)).ToList();
   }

   public static List<Texture2D> getTexturesToBeProcessed () {
      List<Texture2D> textures = new List<Texture2D>();

      D.debug("Searching for the clip mask generation settings...");
      string[] paths = AssetDatabase.FindAssets("HatClipMaskGenerationSettings.asset");

      if (paths == null || paths.Length == 0) {
         D.debug("Searching for the clip mask generation settings: failed - none found. Exiting process.");
         return textures;
      }

      D.debug($"Searching for the clip mask generation settings: ok - found {paths.Length} paths.");

      foreach (string path in paths) {
         D.debug($"Found valid clipmask generation settings at {path}.");
         HatClipMaskGenerationSettings settings = AssetDatabase.LoadAssetAtPath<HatClipMaskGenerationSettings>(path);

         if (settings == null) {
            D.debug($"The settings object found at {path} wasn't valid after all.");
            continue;
         }

         if (settings.textures == null) {
            D.debug($"The settings object found at {path} is valid but there are no textures defined.");
            continue;
         }

         foreach (Texture2D texture in settings.textures) {
            if (texture == null) {
               continue;
            }

            textures.Add(texture);
         }
      }

      return textures;
   }

   #region Private Variables

   #endregion
}
