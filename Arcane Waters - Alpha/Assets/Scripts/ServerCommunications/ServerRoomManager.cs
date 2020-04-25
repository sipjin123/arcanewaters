using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ServerRoomManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ServerRoomManager self;

   // If the servers have initialized
   public bool hasInitialized = false;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeServer () {
      Debug.Log("Connected to Master!");
      if (!hasInitialized) {
         hasInitialized = true;
         ServerCommunicationHandler.self.initializeServers();
      }
   }

   protected string getServerRoomName () {
      string name = "ServerRoom";

      // Make sure multiple devs can launch servers locally without having the names be the same
      name += "-" + SystemInfo.deviceName;

      Debug.Log("Using server room name: " + name);

      return name;
   }

   #region Private Variables

   #endregion
}
