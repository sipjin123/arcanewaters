using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AdminPanelTabSwitcher : MonoBehaviour
{
   #region Public Variables

   // The set of elements that represent the contents of each tab
   public GameObject[] tabContents;

   #endregion

   public void performSwitch (int tabIndex) {
      if (tabContents == null || tabContents.Length == 0) {
         return;
      }

      hideTabs();
      showTab(tabIndex);
   }

   public void hideTabs () {
      foreach (GameObject tab in tabContents) {
         tab.SetActive(false);
      }
   }

   public void showTab (int index) {
      if (index < 0 || index >= tabContents.Length) {
         return;
      }

      GameObject tab = tabContents[index];

      if (tab == null) {
         return;
      }

      tab.SetActive(true);
   }

   #region Private Variables

   #endregion
}
