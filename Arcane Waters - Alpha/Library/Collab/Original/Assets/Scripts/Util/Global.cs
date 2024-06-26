﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static MyNetworkManager;

public class Global {
   #region Public Variables

   // The version of the client and server
   public static int GAME_VERSION = 10041;

   // Can set this to true if we want to auto-start host mode and log in to a character to speed things up
   public static bool startAutoHost = false;

   // A reference to the Player we control, if any
   public static NetEntity player;

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
