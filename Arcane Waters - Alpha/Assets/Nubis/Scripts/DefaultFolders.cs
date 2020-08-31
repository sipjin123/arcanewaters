#if NUBIS
using System.IO;
using System;

public class DefaultFolders
{
   /// <summary>
   /// Creates a file if it doesnt' exist.
   /// </summary>
   /// <param name="filepath">Path to the new file.</param>
   /// <returns>TRUE if the file was created successflly (or was already available), FALSE otherwise.</returns>
   private static bool CreateFileSafe (string filepath) {
      // check if file already exists. if yes, do nothing.
      if (File.Exists(filepath)) {
         return true;
      }
      // the folder doesn't exist yet. create it.
      try {
         File.WriteAllText(filepath, string.Empty);
         return true;
      } catch {
         return false;
      }
   }

   private static bool CreateFolderSafe (String dirpath) {
      // check if folder already exists. if yes, do nothing.
      bool exists = Directory.Exists(dirpath);
      if (exists) {
         return true;
      }
      // the folder doesn't exist yet. create it.
      DirectoryInfo result = Directory.CreateDirectory(dirpath);
      return result.Exists;
   }

   public static bool CreateLogFolder () {
      return CreateFolderSafe(NubisConfiguration.LogFolderPath());
   }

   public static bool CreateLogFile () {
      return CreateFileSafe(NubisConfiguration.LogFilePath());
   }

   private static bool CreateConfigFolder () {
      return CreateFolderSafe(NubisConfiguration.ConfigFolderPath());

   }

   private static bool CreateConfigFile () {
      return CreateFileSafe(NubisConfiguration.ConfigFilePath());
   }

   private static void ReportConfigurationCreationFailed () {
      NubisLogger.i("Configuration File Creation - Status: Failed.");
   }

   public static void Initialize () {

      if (CreateLogFolder()) {
         CreateLogFile();
      }

      if (CreateConfigFolder()) {
         CreateConfigFile();
      }

   }
}
#endif
