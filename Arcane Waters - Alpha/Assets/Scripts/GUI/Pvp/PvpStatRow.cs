using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PvpStatRow : MonoBehaviour {
   #region Public Variables

   // The text fields representing each stat
   public TextMeshProUGUI kills, deaths, shipKills, monsterKills, assists, userName, buildingsDestroyed, silver;

   // A reference to the portrait used to display the character
   public CharacterPortrait portrait;

   // References to all images used to display the background images for the cells
   public List<Image> cellBackgroundImages;

   // Determines the team type
   public PvpTeamType pvpTeamType;

   #endregion

   public void setCellBackgroundSprites (Sprite newSprite) {
      foreach (Image cellImage in cellBackgroundImages) {
         cellImage.sprite = newSprite;
      }
   }

   #region Private Variables
      
   #endregion
}
