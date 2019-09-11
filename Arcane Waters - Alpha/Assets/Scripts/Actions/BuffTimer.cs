using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public struct BuffTimer {
   #region Public Variables

   // The ability that created this buff
   // ZERONEV-COMMENT: This will not work anymore cause each ability is instanced, we will not be able to know by this value alone.
   // So it was changed to the global ability index instead.
   public int buffAbilityGlobalID;
   //public Ability.Type buffType;

   // The time at which the buff starts
   public float buffStartTime;

   // The time at which the buff ends
   public float buffEndTime;

   #endregion

   #region Private Variables

   #endregion
}
