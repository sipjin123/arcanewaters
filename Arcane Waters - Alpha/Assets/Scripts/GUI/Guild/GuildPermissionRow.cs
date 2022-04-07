using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GuildPermissionRow : MonoBehaviour {
   #region Public Variables
   
   // Permission name text
   public Text nameText;
   
   // Permission toggle
   public Toggle statusToggle;
   
   #endregion

   public void init (string permissionName) {
      nameText.text = permissionName;
   }
   
   #region Private Variables
      
   #endregion
}
