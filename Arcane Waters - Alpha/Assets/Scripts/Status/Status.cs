﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Status {
   #region Public Variables

   // The Type of Status effects
   public enum Type { None = 0, Slowed = 1, Frozen = 2, Stunned = 3, Burning = 4, ArmorChangeDebuff = 5, WeaponChangeDebuff = 6, WeaponBreakDebuff = 7, ArmorBreakDebuff = 8 }

   // The Type of Status this is
   public Type statusType;

   // When this Status effect started
   public double startTime;

   // When this Status effect ends
   public double endTime;

   // How strong this status effect is
   public float strength;

   // If the status has been freshly created, this is set to false when the effect is refreshed
   public bool isNew = true;

   // A reference to the coroutine that will destroy this status effect
   public Coroutine removeStatusCoroutine;
      
   #endregion

   #region Private Variables
      
   #endregion
}
