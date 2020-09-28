using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class OverworldScreen : Panel {
   #region Public Variables

   // Self
   public static OverworldScreen self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   #region Private Variables

   #endregion
}
