#define NUBIS
#if NUBIS
using UnityEngine;
using System;

public class NubisConfiguration
{
   #region "Public Variables"

   /// <summary>
   /// The name of the file where the configuration data for Nubis is saved to.
   /// </summary>
   public const string ConfigFileName = "config.json";

   /// <summary>
   /// The name of the file where the logs from Nubis are saved.
   /// </summary>
   public const string LogFileName = "log.txt";

   /// <summary>
   /// The port Nubis is listening on
   /// </summary>
   public int WebServerPort;

   #endregion

   /// <summary>
   /// The path to the folder that will contain the configuration file.
   /// </summary>
   public static string ConfigFolderPath () => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), NubisStatics.AppName, "Config");
   /// <summary>
   /// Returns the filepath to the configuration file.
   /// </summary>
   public static string ConfigFilePath () => System.IO.Path.Combine(ConfigFolderPath(), ConfigFileName);
   /// <summary>
   /// Returns the path to the folder containing the log.
   /// </summary>
   public static string LogFolderPath () => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), NubisStatics.AppName, "Log");
   /// <summary>
   /// Returns the path to the Log file.
   /// </summary>
   public static string LogFilePath () => System.IO.Path.Combine(LogFolderPath(), LogFileName);
   /// <summary>
   /// Save a ButlerConfiguration to a string.
   /// </summary>
   /// <param name="configuration">configuration to save.</param>
   /// <returns>the string representation of the configuration.</returns>
   public static string Serialize (NubisConfiguration configuration) {
      //PruneTokens(configuration);
      return JsonUtility.ToJson(configuration);
   }
   /// <summary>
   /// Load a ButlerConfiguration from a string
   /// </summary>
   public static NubisConfiguration DeSerialize (string data) {
      try {
         NubisConfiguration conf = JsonUtility.FromJson<NubisConfiguration>(data);
         //JsonConvert.DeserializeObject<ButlerConfiguration>(data);
         // conf = PruneTokens(conf);
         return conf;
      } catch {
         return null;
      }
   }
   ///// <summary>
   ///// Removes duplicates from the accepted tokens list.
   ///// </summary>
   ///// <param name="configuration"></param>
   ///// <returns></returns>
   //private static NubisConfiguration PruneTokens(NubisConfiguration configuration)
   //{
   //    configuration.AcceptedTokens = configuration.AcceptedTokens.Distinct().ToList();
   //    return configuration;
   //}
   /// <summary>
   /// Load a configuration from a JSON file.
   /// </summary>
   /// <param name="filepath">the path to the ButlerConfiguration.</param>
   /// <returns>The configuration loaded from the json file.</returns>
   public static NubisConfiguration Load (string filepath) {
      if (!System.IO.File.Exists(filepath)) return new NubisConfiguration();
      string loadedData = System.IO.File.ReadAllText(filepath);
      NubisConfiguration conf = DeSerialize(loadedData);
      if (conf == null) return new NubisConfiguration();
      return conf;
   }
   /// <summary>
   /// Loads a configuration from disk. if no valid configuration is found, returns a new configuration instance.
   /// </summary>
   /// <returns></returns>
   public static NubisConfiguration LoadSafe () {
      // try to load config from disk...
      NubisConfiguration loadedConfiguration = NubisConfiguration.Load(ConfigFilePath());
      // ... if null, create a new configuration instance.
      loadedConfiguration = loadedConfiguration ?? new NubisConfiguration();
      // save the configuration back to disk.
      NubisConfiguration.Save(loadedConfiguration, ConfigFilePath()); // this is done in order to refresh the configuration saved on disk, by adding all the other features.
                                                                      // return the loaded configuration.
      return loadedConfiguration;
   }
   /// <summary>
   /// Saves the configuration to a JSON file.
   /// </summary>
   /// <param name="configuration">The configuration to be saved.</param>
   /// <param name="filepath">The filepath to the destination JSON file.</param>
   /// <returns></returns>
   public static bool Save (NubisConfiguration configuration, string filepath) {
      try {
         System.IO.File.WriteAllText(filepath, Serialize(configuration));
         return true;
      } catch {
         return false;
      }
   }
} 
#endif