using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class AnimInfo {
   #region Public Variables

   // The Animation type
   public Anim.Type animType;

   // The minimum animation index
   public int minIndex;

   // The maximum animation index
   public int maxIndex;
      
   #endregion

   public AnimInfo () { }

   public AnimInfo (Anim.Type animType, int minIndex, int maxIndex) {
      this.animType = animType;
      this.minIndex = minIndex;
      this.maxIndex = maxIndex;
   }

   #region Private Variables
      
   #endregion
}
