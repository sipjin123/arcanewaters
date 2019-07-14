using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System;

public static class PreExportMethods {
   #region Public Variables

   #endregion

   public static void ExecuteClientPreExportMethods () {
      Debug.Log("Executing client preExport methods");

      #if CLOUD_BUILD
      Debug.Log(" CLOUD_BUILD is true");
      #endif

      ClearCSC();
      SetAmazonVPC();
      ExcludeMySQL();
      StripServerCode();

      Debug.Log("Done executing client preExport methods");
   }

   public static void ExecuteServerPreExportMethods () {
      SetAmazonVPC();
   }

   public static void SetLocalHost () {
      MyNetworkManager.cloudBuildOverride = MyNetworkManager.ServerType.Localhost;
   }

   public static void SetAmazonVPC () {
      // Right now, this isn't used
      MyNetworkManager.cloudBuildOverride = MyNetworkManager.ServerType.AmazonVPC;

      // Instead, we'll add the server define to the CSC file
      AppendCSC();
   }

   public static void ExcludeMySQL () {
      PluginImporter mysql = AssetImporter.GetAtPath("Assets/Plugins/MySQL/MySql.Data.dll") as PluginImporter;
      mysql.SetCompatibleWithAnyPlatform(false);
      mysql.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
      mysql.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
      mysql.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
      mysql.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux, false);
      mysql.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
      mysql.SetCompatibleWithPlatform(BuildTarget.StandaloneLinuxUniversal, false);
   }

   public static void ClearCSC () {
      // Look through all of our stuff in the Assets folder
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about scripts in the main script folder
         if (assetPath.EndsWith("Assets/mcs.rsp") || assetPath.EndsWith("Assets/csc.rsp")) {
            // Construct a full path to the asset
            string fullPath = Application.dataPath;
            fullPath = fullPath.Replace("Assets", "");
            fullPath += assetPath;

            // Write the new text back into the file
            File.WriteAllText(@"" + fullPath, "");

            Debug.Log("Finished clearing out the rsp file: " + assetPath);

            // Refresh the asset database because we changed the asset files
            AssetDatabase.Refresh();
         }
      }
   }

   public static void AppendCSC () {
      // Look through all of our stuff in the Assets folder
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about scripts in the main script folder
         if (assetPath.EndsWith("Assets/mcs.rsp") || assetPath.EndsWith("Assets/csc.rsp")) {
            // Construct a full path to the asset
            string fullPath = Application.dataPath;
            fullPath = fullPath.Replace("Assets", "");
            fullPath += assetPath;

            // Write the new text back into the file
            File.AppendAllText(@"" + fullPath, Environment.NewLine + "-define:FORCE_AMAZON_SERVER");

            Debug.Log("Finished appending FORCE_AMAZON_SERVER in the rsp file: " + assetPath);

            // Refresh the asset database because we changed the asset files
            AssetDatabase.Refresh();
         }
      }
   }

   public static void StripServerCode () {
      // Look through all of our stuff in the Assets folder
      foreach (string assetPath in AssetDatabase.GetAllAssetPaths()) {
         // We only care about scripts in the main script folder
         if (assetPath.Contains("Assets/Scripts/") && assetPath.EndsWith(".cs")) {
            // TESTING -- just ignore everything except the test class for now
            /*if (!assetPath.Contains("DB_Main.cs")) {
               continue;
            }*/

            // Construct a full path to the asset
            string fullPath = Application.dataPath;
            fullPath = fullPath.Replace("Assets", "");
            fullPath += assetPath;

            // Read in all of the file text
            string fileText;
            FileStream fileStream = new FileStream(@""+fullPath, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8)) {
               fileText = streamReader.ReadToEnd();
            }

            // Check if we're going to strip this particular file
            bool stripMethods = containsMethodToStrip(fileText);
            bool stripClass = fileText.Contains("[StripClass]");

            // We only care about files with Server code or classes that were specifically marked for stripping
            if (stripMethods || stripClass) {
               Debug.Log("Stripping code from Asset at path: " + fullPath);

               // We'll set up a new string to store the modified file contents
               string newText = "";
               string returnLine = "";
               string previousLine = "";
               bool changeMade = false;
               bool startedStripping = false;

               // Store the file as individual lines
               string[] lines = fileText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

               // Loop through each of the lines in the file
               foreach (string line in lines) {
                  if (isFunctionStart(line) && (containsMethodToStrip(previousLine) || stripClass)) {
                     // Note that we've made at least one change to this file
                     changeMade = true;

                     // Note that we just started stripping a function
                     startedStripping = true;

                     // If it's the start of a function, append the compiler directive
                     newText += line + Environment.NewLine + "#if IS_SERVER_BUILD";

                     // Figure out what the return line should be
                     returnLine = getReturnString(line);

                  } else if (line.StartsWith("   }") && startedStripping) {
                     // If it's the end of a function, prepend the compiler directive
                     newText += "#endif" + Environment.NewLine + returnLine + Environment.NewLine + line;

                     // Now we're done with the line
                     startedStripping = false;
                  } else {
                     // Otherwise, just keep the line how it was
                     newText += line;
                  }

                  // We need to add the new lines back to the file, since we split on those
                  newText += Environment.NewLine;

                  // Keep track of the previous line
                  previousLine = line;
               }

               // If we got through the whole file without making any changes, something is probably broken
               if (!changeMade) {
                  Debug.LogError("A class was marked for code stripping, but no changes were made: " + fullPath);
                  throw new System.ArgumentException("A class was marked for code stripping, but no changes were made: " + fullPath);
               }

               Debug.Log("File length changed from: " + fileText.Length + " to: " + newText.Length);

               // Write the new text back into the file
               File.WriteAllText(@"" + fullPath, newText);

               // Refresh the asset database because we changed the asset files
               AssetDatabase.Refresh();
            }
         }
      }
   }

   public static bool isFunctionStart (string line) {
      if (line.Contains("public") || line.Contains("protected") || line.Contains("private")) {
         if (line.Contains("(") && line.Contains(")") && line.Contains("{")) {
            return true;
         }
      }

      return false;
   }

   public static string getReturnString (string line) {
      // Only look at the stuff before the parameters
      line = line.Split('(')[0];

      if (line.Contains(" void ")) {
         return "";
      } else if (line.Contains(" bool ")) {
         return "return false;";
      } else if (line.Contains(" int ")) {
         return "return 0;";
      } else if (line.Contains(" string ")) {
         return "return \"\";";
      } else if (line.Contains(" float ")) {
         return "return 0f;";
      } else if (line.Contains(" long ")) {
         return "return 0;";
      } else if (line.Contains(" double ")) {
         return "return 0;";
      } else if (line.Contains(" Step ")) {
         return "return 0;";
      } else if (line.Contains(" IEnumerator ")) {
         return "yield break;";
      } else {
         return "return null;";
      }
   }

   public static bool containsMethodToStrip (string line) {
      return line.Contains("[Command]") || line.Contains("[Server]") || line.Contains("[ServerOnly]");
   }

   #region Private Variables

   #endregion
}
