using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public abstract class EquippableItem : Item {
   #region Public Variables

   #endregion

   public override bool canBeStacked () {
      // Don't stack equippable items
      return false;
   }

   public abstract bool isEquipped ();

   #region Private Variables

   #endregion
}
