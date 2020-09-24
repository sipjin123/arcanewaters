using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Status : MonoBehaviour {
   #region Public Variables

   // The Type of Status effects
   public enum Type {  None = 0, Slow = 1, Freeze = 2, }

   // The Type of Status this is
   public Type statusType;

   // When this Status effect started
   public double startTime;

   // When this Status effect ends
   public double endTime;
      
   #endregion

   #region Private Variables
      
   #endregion
}
