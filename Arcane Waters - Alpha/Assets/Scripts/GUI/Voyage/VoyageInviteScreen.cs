using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;

public class VoyageInviteScreen : GenericInviteScreen
{
   #region Public Variables

   #endregion

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public void Update () {
      // Hide the panel during battle
      if (Global.player == null || Global.player.isInBattle()) {
         hide();
      } else {
         show();
      }
   }

   #region Private Variables

   #endregion
}