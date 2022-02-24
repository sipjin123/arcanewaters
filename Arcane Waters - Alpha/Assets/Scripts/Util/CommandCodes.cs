using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CommandCodes : MonoBehaviour {
   #region Public Variables

   public enum Type {
      AUTO_HOST = 1,             // Automatically start up host mode
      AUTO_CLIENT = 2,           // Automatically start up a client
      AUTO_SERVER = 3,           // Automatically start up a server
      AUTO_TEST = 4,             // Automatically log in to the character in the first slot
      AUTO_MOVE = 5,             // Automatically move around the scene
      PHOTON_CLOUD = 6,          // Force connection to Photon cloud (rather than self-hosted)
      AUTO_DBCONFIG = 7,         // Automatically start up a server and read DB server configuration from json config file
      MAX_INSTANCE_PLAYERS = 8,  // Sets the maximum number of players allowed in an instance
      NPC_DISABLE = 9,           // Disables NPCs for area (Area.npcDatafields)
      SERVER_DISABLE_COMMUNICATION = 10, // Disables ServerCommunicationHandler
      SERVER_DISABLE_DATA_HANDLER = 11, // Disables SharedServerDataHandler
      SERVER_DB_DEBUG = 12, // Enables server DB debug 
      AUTO_WARP = 13,             // Regularly request a random warp
      IS_STRESS_TEST = 14, // Is current launch is stresstesting
      CLIENT_DISABLE_NUBIS = 15 // On clients, disable Nubis and use Mirror requests instead
   }

   public static Dictionary<Type, bool> cache;
   #endregion

   public static bool get (Type commandType) {
      if (cache == null) {
         cache = new Dictionary<Type, bool>();
      }
      
      if (!cache.ContainsKey(commandType)) {
         cache[commandType] = System.Environment.CommandLine.Contains(commandType + "");
      }
      return cache[commandType];
   }
   
   #region Private Variables
   #endregion
}
