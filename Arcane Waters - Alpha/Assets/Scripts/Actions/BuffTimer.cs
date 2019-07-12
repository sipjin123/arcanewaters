using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public struct BuffTimer {
   #region Public Variables

   // The ability that created this buff
   public Ability.Type buffType;

   // The time at which the buff starts
   public float buffStartTime;

   // The time at which the buff ends
   public float buffEndTime;

   #endregion

   #region Private Variables

   #endregion
}
