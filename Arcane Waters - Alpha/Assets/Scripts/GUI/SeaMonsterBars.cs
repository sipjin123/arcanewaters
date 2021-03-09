using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterBars : ShipBars
{
   #region Public Variables

   #endregion

   protected override void Start () {
      base.Start();

      if (_entity == null) {
         return;
      }

      // Wait a few frames for the sea monster data to be initialized (also done at Start)
      Invoke(nameof(initializeHealthBar), 0.1f);
   }

   #region Private Variables

   #endregion
}
