using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Voyage
{
   #region Public Variables
   
   // The voyage difficulty
   public enum Difficulty { None = 0, Easy = 1, Medium = 2, Hard = 3 }

   // The voyage mode chosen by the player
   public enum Mode { Quickmatch = 1, Private = 2 }

   // The areaKey of the voyage map
   public string areaKey;

   // The voyage difficulty
   public Difficulty difficulty = Difficulty.None;

   // Gets set to true when the voyage is PvP - Otherwise, the voyage is PvE
   public bool isPvP = false;

   #endregion

   public Voyage () {

   }

   public Voyage(string areaKey, Difficulty difficulty, bool isPvP) {
      this.areaKey = areaKey;
      this.difficulty = difficulty;
      this.isPvP = isPvP;
   }

   public static int getMaxGroupSize (Difficulty voyageDifficulty) {
      switch (voyageDifficulty) {
         case Difficulty.Easy:
            return 2;
         case Difficulty.Medium:
            return 4;
         case Difficulty.Hard:
            return 6;
         default:
            return 6;
      }
   }

   #region Private Variables

   #endregion
}
