using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class LogInUserMessage : MessageBase {
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The credentials
   public string accountName;
   public string accountPassword;
   public string steamId;

   // The client's version of the game
   public int clientGameVersion;

   // The selected user
   public int selectedUserId;

   // The client platform
   public RuntimePlatform clientPlatform;

   // Determines if the game mode is single player
   public bool isSinglePlayer;

   // Determines if the login is using steam id
   public bool isSteamLogin;

   #endregion

   public LogInUserMessage () { }

   public LogInUserMessage (uint netId) {
      this.netId = netId;
   }

   public LogInUserMessage (uint netId, string accountName, string accountPassword, string steamId, bool isSteamLogin,int clientGameVersion, int selectedUserId, RuntimePlatform clientPlatform, bool isSinglePlayer) {
      this.netId = netId;
      this.selectedUserId = selectedUserId;
      this.accountName = accountName;
      this.clientGameVersion = clientGameVersion;
      this.accountPassword = accountPassword;
      this.clientPlatform = clientPlatform;
      this.isSinglePlayer = isSinglePlayer;
      this.isSteamLogin = isSteamLogin;
      this.steamId = steamId;
   }
}