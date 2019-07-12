using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class LevelUtil : MonoBehaviour {
   #region Public Variables

   #endregion

   public static int xpForLevel (int level) {
      return 100 * (int) Mathf.Pow(level - 1, 2f);
   }

   public static int levelForXp (int xp) {
      for (int level = 2; level <= 100; level++) {
         if (xp < xpForLevel(level)) {
            return level - 1;
         }
      }

      // Max level
      return 100;
   }
   
   public static int getProgressTowardsCurrentLevel (int xp) {
      int currentLevel = levelForXp(xp);

      return (xp - xpForLevel(currentLevel));
   }

   public static bool gainedLevel (int oldXP, int newXP) {
      return levelForXp(oldXP) != levelForXp(newXP);
   }

   public static int levelsGained (int oldXP, int newXP) {
      int oldLevel = levelForXp(oldXP);
      int newLevel = levelForXp(newXP);

      return newLevel - oldLevel;
   }

   #region Private Variables

   #endregion
}
