using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class LogInUserMessage : NetworkMessage
{
   #region Public Variables

   // The credentials
   public string accountName;
   public string accountPassword;

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

   // The login token generated on the client side
   public byte[] steamAuthTicket;

   // The data size of the ticket
   public uint steamTicketSize;

   // The client's machine identifier
   public string machineIdentifier;

   // False if the player has already logged in successfully.
   public bool isFirstLogin;

   // The app id of the steam game used {game app / play test app}
   public string steamAppId;

   // The steam user id of the player
   public string steamUserId = "";

   // Flag for client being redirected to another server
   public bool isRedirecting;

   // The client's deploymentId
   public int deploymentId;

   #endregion

   public LogInUserMessage () { }

   public LogInUserMessage (string accountName, string accountPassword, bool isSteamLogin,
      int clientGameVersion, int selectedUserId, RuntimePlatform clientPlatform, bool isSinglePlayer, 
      byte[] steamAuthTicket, uint steamTicketSize, string machineIdentifier, bool isFirstLogin, string steamAppId, string steamUserId, bool isRedirecting, int deploymentId) {
      this.selectedUserId = selectedUserId;
      this.accountName = accountName;
      this.clientGameVersion = clientGameVersion;
      this.accountPassword = accountPassword;
      this.clientPlatform = clientPlatform;
      this.isSinglePlayer = isSinglePlayer;
      this.isSteamLogin = isSteamLogin;
      this.steamAuthTicket = steamAuthTicket;
      this.steamTicketSize = steamTicketSize;
      this.machineIdentifier = machineIdentifier;
      this.isFirstLogin = isFirstLogin;
      this.steamAppId = steamAppId;
      this.steamUserId = steamUserId;
      this.isRedirecting = isRedirecting;
      this.deploymentId = deploymentId;
   }
}