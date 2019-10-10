using UnityEngine;
using System.Collections;
using System.IO;
using System.Xml.Serialization;
using System;

public static class ToolsUtil
{
   public static T xmlLoad<T> (string filePath) {
      FileStream stream = null;
      try {
         // Create an instance of the XMLSerializer
         XmlSerializer serializer = new XmlSerializer(typeof(T));

         // Open the file
         stream = new FileStream(filePath, FileMode.Open);

         // Deserialize the object
         T obj = (T) serializer.Deserialize(stream);

         // Return the result
         return obj;

      } catch (Exception e) {
         Debug.LogError("Error when loading the file " + filePath + "\n" + e.ToString());
         return default(T);
      } finally {
         // Close the file reader
         if (stream != null) {
            stream.Close();
         }
      }
   }

   public static void xmlSave<T> (T data, string filePath) {
      FileStream stream = null;
      try {
         // Create an instance of the XMLSerializer
         XmlSerializer serializer = new XmlSerializer(data.GetType());

         // Create or overwrite the file
         stream = new FileStream(filePath, FileMode.Create);

         // Serialize the data
         serializer.Serialize(stream, data);

         // Close the writer
         stream.Close();
      } catch (Exception e) {
         Debug.LogError("Error when saving the file " + filePath + ".\n" + e.ToString());
      } finally {
         // Close the file reader
         if (stream != null) {
            stream.Close();
         }
      }
   }

   public static void deleteFile (string filePath) {
      try {
         File.Delete(filePath);
      } catch (Exception e) {
         Debug.LogError("Error when deleting the file " + filePath + ".\n" + e.ToString());
      }
   }

   public static string[] getFileNamesInFolder (string directoryPath, string searchPattern = "*.*") {
      // Get the list of files in the directory
      DirectoryInfo dir = new DirectoryInfo(directoryPath);
      FileInfo[] info = dir.GetFiles(searchPattern);

      // Get the name of each file
      string[] fileNamesArray = new string[info.Length];
      for (int i = 0; i < info.Length; i++) {
         fileNamesArray[i] = info[i].Name;
      }
      return fileNamesArray;
   }
}
