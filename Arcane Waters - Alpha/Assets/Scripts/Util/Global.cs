using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static MyNetworkManager;

public class Global {
   #region Public Variables

   // The version of the client and server
   public static int GAME_VERSION = 10038;

   // Can set this to true if we want to auto-start host mode and log in to a character to speed things up
   public static bool startAutoHost = false;

   // A reference to the Player we control, if any
   public static NetEntity player;

   // Gets set to true while we're redirecting a player to a new server
   public static bool isRedirecting = false;

   // The ID of the currently selected user for our account
   public static int currentlySelectedUserId = 0;

   // Keeps track of the last account name we provided
   public static string lastUsedAccountName;

   // Keeps track of the last account password we provided
   public static string lastUserAccountPassword;

   // Keeps track of the last account email, for Paymentwall
   public static string lastAccountEmail;

   // Keeps track of the last account creation time, for Paymentwall
   public static System.DateTime lastAccountCreationTime;

   // The facing direction we should have when our player is created
   public static Direction initialFacingDirection = Direction.South;

   // A global network instance ID that the client and server can use for communication
   public static uint netId;

   #endregion

   public static string getAddress (ServerType server) {
      switch (server) {
         case ServerType.Localhost:
            return "localhost";
         case ServerType.AmazonVPC:
            return "52.1.218.202";
         case ServerType.AmazonSydney:
            return "13.55.171.249";
         default:
            return "";
      }
   }

   public static bool isInBattle () {
      return (Global.player != null && Global.player.isInBattle()) ;
   }

   #region Private Variables

   #endregion
}
