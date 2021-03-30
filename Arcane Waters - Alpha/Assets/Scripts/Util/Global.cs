using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static MyNetworkManager;
using System;
using MLAPI.NetworkedVar;

public class Global
{
   #region Public Variables

   // The port that identifies our "master" server, which starts up first and has authority over all other server processes
   public static int MASTER_SERVER_PORT = 7777;

   // The version of the client
   public static int clientGameVersion = 0;

   // Can set this to true if we want to auto-start host mode and log in to a character to speed things up
   public static bool startAutoHost = false;

   // A reference to the Player we control, if any
   public static NetEntity player;

   // A reference to the Players user objects
   public static UserObjects userObjects;

   // A reference to the player currency for display only
   public static int lastUserGold, lastUserGems;

   // Gets set to true while we're redirecting a player to a new server
   public static bool isRedirecting = false;

   // Gets set to true while we're login in and selecting a character using the fast_login.txt config
   public static bool isFastLogin = false;

   // The character spot (not the id) that will be auto-selected with fast-login
   public static int fastLoginCharacterSpotIndex = -1;

   // The account used for the fast login
   public static string fastLoginAccountName;

   // The password used for the fast login
   public static string fastLoginAccountPassword;

   // Gets set to true when the fast login should start in host mode
   public static bool isFastLoginHostMode = false;

   // The ID of the currently selected user for our account
   public static int currentlySelectedUserId = 0;

   // If the game is set to single player mode
   public static bool isSinglePlayer = false;

   // Keeps track of the last account name we provided
   public static string lastUsedAccountName;

   // Keeps track of the last account password we provided
   public static string lastUserAccountPassword;

   // Keeps track of the last account email, for Paymentwall
   public static string lastAccountEmail;

   // Keeps track of the steam settings and info
   public static bool isSteamLogin;
   public static string lastSteamId;

   // Determine if the player should sprint without holding down sprint button
   public static bool sprintConstantly;

   // Keeps track of the last account creation time, for Paymentwall
   public static System.DateTime lastAccountCreationTime;

   // The facing direction we should have when our player is created
   public static Direction initialFacingDirection = Direction.South;

   // A global network instance ID that the client and server can use for communication
   public static uint netId;

   // The current ID of the Basic Attack ability to use as a failsafe if the player has no other abilities
   public static int BASIC_ATTACK_ID = 9;

   // Gets set to false if the player has already logged in successfully.
   public static bool isFirstLogin = true;

   // Gets set to true when the screen is transitioning, to block loading while the screen is transitioning
   public static bool isScreenTransitioning = false;

   // Gets set to false when Nubis requests are executed as Mirror requests instead
   public static bool isUsingNubis = true;

   // List of logs to show
   public static List<D.ADMIN_LOG_TYPE> logTypesToShow = new List<D.ADMIN_LOG_TYPE>();

   // Account dictionary overrides
   public static Dictionary<string, string> accountOverrides = new Dictionary<string, string>();

   // The default settings for networked vars in the server network
   public static NetworkedVarSettings defaultNetworkedVarSettings = new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly, SendChannel = "Fragmented" };

   #endregion

   public static void setUserEquipment (Item weapon, Item armor, Item hat) {
      userObjects.weapon = weapon;
      userObjects.armor = armor;
      userObjects.hat = hat;
   }

   public static void setUserObject (UserObjects newUserObj) {
      userObjects = newUserObj;
   }

   public static UserObjects getUserObjects () {
      return userObjects;
   }

   public static string getAddress (ServerType server) {
      switch (server) {
         case ServerType.Localhost:
            return "localhost";
         case ServerType.AmazonVPC:
#if FORCE_AMAZON_SERVER_PROD
            // TODO: Confirm if this is the appropriate ip address
            return "18.217.126.169";//"arcanewaters.com";
#else
            // TODO: Confirm if this is the appropriate ip address
            return "3.131.116.210";//"dev.arcanewaters.com" "3.137.185.163";
#endif
         case ServerType.AmazonSydney:
            return "13.55.171.249";
         case ServerType.AmazonPerformance:
            return "3.19.199.220";
         default:
            return "";
      }
   }

   public static bool isInBattle () {
      return (Global.player != null && Global.player.isInBattle());
   }

   public static void updateAdminLog (D.ADMIN_LOG_TYPE logType, bool isEnabled) {
      if (isEnabled) {
         if (!logTypesToShow.Contains(logType)) {
            logTypesToShow.Add(logType);
         }
      } else {
         if (logTypesToShow.Contains(logType)) {
            logTypesToShow.Remove(logType);
         }
      }
   }

   public static bool isLoggedInAsAdmin () {
      return (player && player.isAdmin());
   }

   #region Private Variables

   #endregion
}
