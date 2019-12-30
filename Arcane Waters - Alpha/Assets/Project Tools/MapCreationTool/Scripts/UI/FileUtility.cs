using SFB;
using System.IO;

namespace MapCreationTool
{
   public class FileUtility
   {
      public static void saveFile (string data) {
         string path = StandaloneFileBrowser.SaveFilePanel("Save file", "", "new file", "arcane");
         if (!string.IsNullOrEmpty(path))
            File.WriteAllText(path, data);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns>Data of the selected file. Null if cancelled.</returns>
      public static string openFile () {
         string[] path = StandaloneFileBrowser.OpenFilePanel("Open file", "", "arcane", false);
         if (path.Length == 1)
            return File.ReadAllText(path[0]);
         return null;
      }

      public static void exportFile(string data) {
         string path = StandaloneFileBrowser.SaveFilePanel("Export file", "", "new file", "json");
         if (!string.IsNullOrEmpty(path))
            File.WriteAllText(path, data);
      }
   }
}
