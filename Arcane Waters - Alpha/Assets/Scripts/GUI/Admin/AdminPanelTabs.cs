using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using UnityEngine.Events;

public class AdminPanelTabs : MonoBehaviour
{
   #region Public Variables

   // The set of GameObjects that represent inactive tabs
   public GameObject[] tabsUnder;

   // The set of GameObjects that represent active tabs
   public GameObject[] tabsAbove;

   // Reference to the event triggered when a Tab is pressed
   public OnTabPressedEvent onTabPressed = new OnTabPressedEvent();

   // Event that is triggered when a tab is pressed
   public class OnTabPressedEvent : UnityEvent<int>
   {

   }

   #endregion

   public GameObject getTabAbove (GameObject tabUnder) {
      if (tabUnder == null || tabsAbove == null || tabsAbove.Length == 0) {
         return null;
      }

      int index = tabsUnder.ToList().IndexOf(tabUnder);
      return getTabAbove(index);
   }

   public GameObject getTabUnder (GameObject tabAbove) {
      if (tabAbove == null || tabsUnder == null || tabsUnder.Length == 0) {
         return null;
      }

      int index = tabsAbove.ToList().IndexOf(tabAbove);
      return getTabUnder(index);
   }

   public GameObject getTabAbove (int tabIndex) {
      if (tabIndex < 0 || tabIndex >= tabsAbove.Length) {
         return null;
      }
      
      return tabsAbove[tabIndex];
   }

   public GameObject getTabUnder (int tabIndex) {
      if (tabIndex < 0 || tabIndex >= tabsUnder.Length) {
         return null;
      }
      
      return tabsUnder[tabIndex];
   }

   public void toggleTab(int tabIndex, bool activated) {
      GameObject tabUnder = getTabUnder(tabIndex);

      if (tabUnder != null) {
         tabUnder.SetActive(!activated);
      }

      GameObject tabAbove = getTabAbove(tabIndex);

      if (tabAbove != null) {
         tabAbove.SetActive(activated);
      }
   }

   public void toggleTabs(bool activated) {
      for (int i = 0; i < tabsUnder.Length; i++) {
         toggleTab(i, activated);
      }
   }

   public void performTabPressed(int tabIndex) {
      if (onTabPressed != null) {
         onTabPressed.Invoke(tabIndex);
      }

      toggleTabs(false);
      toggleTab(tabIndex,true);
   }

   #region Private Variables

   #endregion
}
