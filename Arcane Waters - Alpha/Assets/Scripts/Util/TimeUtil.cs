using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

namespace Mirror {
   public static class TimeUtil {
      #region Public Variables
         
      #endregion

      public static float diff (double time1, double time2) {
         return (float) (time2 - time1);
      }

      public static float time(double time) {
         return (float) time;
      }

      #region Private Variables
         
      #endregion
   }
}