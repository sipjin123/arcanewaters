﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InputManager : MonoBehaviour {
   #region Public Variables
      
   #endregion

   public static bool isActionKeyPressed () {
      // Don't respond to action keys while the player is dead
      if (Global.player == null || Global.player.isDead()) {
         return false;
      }

      // Can't initiate actions while typing
      if (ChatManager.isTyping()) {
         return false;
      }

      // Define the set of keys that we want to allow as "action" keys
      return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
   }

   #region Private Variables
      
   #endregion
}
