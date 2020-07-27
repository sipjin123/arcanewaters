using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class TabbedPanelController : MonoBehaviour {
   #region Public Variables

   // The different tabs we can select
   public List<PanelTabButton> tabButtons;
      
   #endregion

   public void initialize () {
      // Unselect all the tabs
      unselectAllTabs();

      // Select the first one
      tabButtons.First().setSelected();
   }

   public void unselectAllTabs () {
      foreach (PanelTabButton tab in tabButtons) {
         tab.setUnselected();
      }
   }

   #region Private Variables

   #endregion
}
