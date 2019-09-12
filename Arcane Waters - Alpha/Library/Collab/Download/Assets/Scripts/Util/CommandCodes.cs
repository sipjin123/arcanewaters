using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CommandCodes : MonoBehaviour {
   #region Public Variables

   public enum Type {
      AUTO_HOST = 1,   // Automatically start up host mode
      AUTO_CLIENT = 2, // Automatically start up a client
      AUTO_SERVER = 3, // Automatically start up a server
      AUTO_TEST = 4,   // Automatically log in to the character in the first slot
      AUTO_MOVE = 5,   // Automatically move around the scene
      PHOTON_CLOUD = 6,// Force connection to Photon cloud (rather than self-hosted)
   }

   #endregion

   public static bool get (Type commandType) {
      return System.Environment.CommandLine.Contains(commandType+"");
   }

   #region Private Variables

   #endregion
}
