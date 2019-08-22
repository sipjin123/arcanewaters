using UnityEngine;
using System.IO;

//Chris Palacios.

public static class ItemEditorUtilities
{
   private static readonly string PROJECT_ROOT_PATH;

   static ItemEditorUtilities () {
      PROJECT_ROOT_PATH = Application.dataPath;
   }

   public static string[] getFiles (string fileExtension, bool recursive) {
      return Directory.GetFiles(PROJECT_ROOT_PATH, fileExtension,
          recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
   }

   public static string absolutePathToAssetsRelative (string filePath) {
      return filePath.Replace(PROJECT_ROOT_PATH, "Assets");
   }

   public static string limitStringTo (int maxChars, string value) {
      if (value.Length <= maxChars) return value;
      return value.Remove(maxChars) + "...";
   }
}
