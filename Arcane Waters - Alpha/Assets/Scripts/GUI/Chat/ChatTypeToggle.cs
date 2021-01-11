using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ChatTypeToggle : MonoBehaviour {
   #region Public Variables

   // Background of an active chat type tab
   public GameObject activeBackground;

   // Crossed name of an inactive chat type tab name
   public GameObject crossedName;

   #endregion

   public void onToggleValueChanged () {
      Toggle toggle = GetComponentInChildren<Toggle>();
      if (toggle) {
         activeBackground.SetActive(toggle.isOn);
         crossedName.SetActive(!toggle.isOn);
         GetComponentInChildren<Text>().color = toggle.isOn ? Color.white : Color.grey;
      }
   }

   #region Private Variables
      
   #endregion
}
