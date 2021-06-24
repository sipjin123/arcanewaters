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
      ((PvpArenaPanel)PanelManager.self.get(Panel.Type.PvpArena)).togglePanel();
   }

   #region Private Variables

   #endregion
}
