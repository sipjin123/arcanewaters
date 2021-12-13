using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class AdminPanelInstanceOverview : MonoBehaviour
{
   #region Public Variables

   // Instance area key text
   public Text areaKeyText = null;

   // Instance player count text
   public Text playerCountText = null;

   #endregion

   public void apply (InstanceOverview overview) {
      areaKeyText.text = overview.area;
      playerCountText.text = overview.count + " Players";
   }

   #region Private Variables

   #endregion
}
