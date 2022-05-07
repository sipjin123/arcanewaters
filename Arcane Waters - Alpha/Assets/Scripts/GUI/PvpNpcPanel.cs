using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpNpcPanel : Panel {
   #region Public Variables

   public static PvpNpcPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void onClosePanelPressed () {
      // hide();
      PanelManager.self.unlinkPanel();
   }

   #region Private Variables

   #endregion
}
