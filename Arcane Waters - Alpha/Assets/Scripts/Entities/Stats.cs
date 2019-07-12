using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Stats {
   #region Public Variables

   // The user ID the stats are for
   public int userId;

   // The various stats for this user
   public int strength;
   public int precision;
   public int vitality;
   public int intelligence;
   public int spirit;
   public int luck;

   #endregion

   public Stats () {

   }

   public Stats (int userId) {
      this.userId = userId;
   }

   #region Private Variables

   #endregion
}
