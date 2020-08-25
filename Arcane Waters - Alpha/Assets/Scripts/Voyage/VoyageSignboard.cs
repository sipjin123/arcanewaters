using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class VoyageSignboard : Signboard
{
   #region Public Variables

   #endregion

   protected override void onClick () {
      VoyageManager.self.showVoyagePanel(Global.player);
   }

   #region Private Variables

   #endregion
}
