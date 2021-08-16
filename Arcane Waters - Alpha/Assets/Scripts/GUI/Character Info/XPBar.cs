using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class XPBar : MonoBehaviour
{
   #region Public Variables

   // The text containing the level
   public Text levelText;

   // The experience progress bar
   public Image levelProgressBar;

   // The tooltip displaying the xp numbers
   public ToolTipComponent xpTooltip;

   #endregion

   public void setProgress (int xp) {
      int currentLevel = LevelUtil.levelForXp(xp);
      levelProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(xp) / (float) LevelUtil.xpForLevel(currentLevel + 1);
      levelText.text = "LVL " + currentLevel;
      xpTooltip.message = "EXP: " + LevelUtil.getProgressTowardsCurrentLevel(xp) + " / " + LevelUtil.xpForLevel(currentLevel + 1);
   }

   #region Private Variables

   #endregion
}


