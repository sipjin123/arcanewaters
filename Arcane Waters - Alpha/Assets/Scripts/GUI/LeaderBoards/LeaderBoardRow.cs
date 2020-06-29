using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class LeaderBoardRow : MonoBehaviour
{
   #region Public Variables

   // The rank of the user
   public Text rankText;

   // The faction icon
   public Image factionIcon;

   // The name of the user
   public Text userName;

   // The score of the user
   public Text score;

   #endregion

   public void setRowForLeaderBoard (LeaderBoardInfo entry) {
      rankText.text = entry.userRank.ToString() + ".";
      factionIcon.sprite = null;
      userName.text = entry.userName;
      score.text = entry.score.ToString();
   }

   #region Private Variables

   #endregion
}
