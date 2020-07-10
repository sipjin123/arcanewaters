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

   // The name of the user
   public Text userName;

   // The score of the user
   public Text score;

   // The user's guild icon
   public GuildIcon guildIcon;

   #endregion

   public void setRowForLeaderBoard (LeaderBoardInfo entry) {
      rankText.text = entry.userRank.ToString() + ".";
      userName.text = entry.userName;
      score.text = entry.score.ToString();

      guildIcon.initialize(entry.guildInfo);
   }

   #region Private Variables

   #endregion
}
