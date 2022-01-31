﻿using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PvpScoreIndicator : MonoBehaviour
{
   #region Public Variables

   // The control showing the score for team A
   public TextMeshProUGUI scoreTeamA;

   // The control showing the score for team B
   public TextMeshProUGUI scoreTeamB;

   // Self
   public static PvpScoreIndicator self;

   #endregion

   public void Awake () {
      self = this;
   }

   private void Start () {
      // Immediately hide the indicator
      reset();
      toggle(show: false);
   }

   public void updateScore(PvpTeamType team, int newScore) {
      _score[team] = newScore;

      if (team == PvpTeamType.A) {
         scoreTeamA.text = newScore.ToString();
      } else if (team == PvpTeamType.B) {
         scoreTeamB.text = newScore.ToString();
      }
   }

   public int getScore(PvpTeamType team) {
      if (_score.TryGetValue(team, out int score)) {
         return score;
      }

      return 0;
   }

   public void toggle(bool show) {
      gameObject.SetActive(show);
   }

   public void reset () {
      scoreTeamA.text = "0";
      scoreTeamB.text = scoreTeamA.text;
   }

   #region Private Variables

   // Stores a copy of the score for each team
   private Dictionary<PvpTeamType, int> _score = new Dictionary<PvpTeamType, int>();

   #endregion
}