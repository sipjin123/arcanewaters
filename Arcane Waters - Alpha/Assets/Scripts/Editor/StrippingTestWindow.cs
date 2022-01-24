using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class StrippingTestWindow : EditorWindow
{
   #region Public Variables

   #endregion

   [MenuItem("Window/Analysis/Stripping Test")]
   static void init () {
      // Get existing open window or if none, make a new one:
      StrippingTestWindow window = (StrippingTestWindow) EditorWindow.GetWindow(typeof(StrippingTestWindow));
      window.Show();
   }

   void OnGUI () {
      GUILayout.Label("Script path - use absolute path in your machine");
      scriptPath = EditorGUILayout.TextField(scriptPath);

      GUILayout.Label("Result");
      var s = EditorGUILayout.TextField(stripResult);

      if (GUILayout.Button("Test strip")) {
         string source = File.ReadAllText(scriptPath);
         stripResult = stripServerCode(source);
      }
   }

   private string stripServerCode (string source) {
      // Check if we're going to strip this particular file
      bool stripMethods = PreExportMethods.containsMethodToStrip(source);
      bool stripClass = source.Contains("[StripClass]");

      // We only care about files with Server code or classes that were specifically marked for stripping
      if (stripMethods || stripClass) {

         // We'll set up a new string to store the modified file contents
         string newText = "";
         string returnLine = "";
         string previousLine = "";
         bool changeMade = false;
         bool startedStripping = false;

         // Store the file as individual lines
         string[] lines = source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

         // Loop through each of the lines in the file
         foreach (string line in lines) {
            if (PreExportMethods.isFunctionStart(line) && (PreExportMethods.containsMethodToStrip(previousLine) || stripClass) && !PreExportMethods.hasOutParameters(line)) {
               // Note that we've made at least one change to this file
               changeMade = true;

               // Note that we just started stripping a function
               startedStripping = true;

               // If it's the start of a function, append the compiler directive
               newText += line + Environment.NewLine + "#if IS_SERVER_BUILD";

               // Figure out what the return line should be
               returnLine = PreExportMethods.getReturnString(line);
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
            Debug.LogError("A class was marked for code stripping, but no changes were made");
            throw new System.ArgumentException(
               "A class was marked for code stripping, but no changes were made");
         }

         Debug.Log("File length changed from: " + source.Length + " to: " + newText.Length);

         return newText;
      }

      return source;
   }

   #region Private Variables

   // Path to file we want to test
   private string scriptPath;

   // Stripping result
   private string stripResult;

   #endregion
}
