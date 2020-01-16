using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BattlerTemplate : MonoBehaviour {
   #region Public Variables

   // Icon of the battler
   public Image battlerIcon;

   // Name of the battler
   public Text battlerName;

   // Enemy Type of the battler
   public Text battlerType;

   // Cached data 
   public BattlerData battlerDataCache;

   // User name text for player battlers
   public InputField userNameText;

   #endregion

   #region Private Variables
      
   #endregion
}
