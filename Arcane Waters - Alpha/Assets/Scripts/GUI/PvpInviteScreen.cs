using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpInviteScreen : GenericInviteScreen {
   #region Public Variables

   // Self
   public static PvpInviteScreen self;

   #endregion

   private void Awake () {
      self = this;
   }

   // TODO: Setup pvp invite logic here for team combat

   #region Private Variables

   #endregion
}
