using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using static UnityEngine.UI.Toggle;
using System;

public class LeaderBoardsPanel : Panel
{
   #region Public Variables
   
   // Self
   public static LeaderBoardsPanel self;

   // The default period the panel displays
   public static LeaderBoardsManager.Period DEFAULT_PERIOD = LeaderBoardsManager.Period.Week;

   // The container for the rows for each leader board
   public GameObject farmingBoardRowsContainer;
   public GameObject sailingBoardRowsContainer;
   public GameObject exploringBoardRowsContainer;
   public GameObject tradingBoardRowsContainer;
   public GameObject craftingBoardRowsContainer;
   public GameObject miningBoardRowsContainer;

   // The time left until recalculation
   public Text timeLeftUntilRecalculationText;

   // The prefab we use for creating entry rows
   public LeaderBoardRow leaderBoardRowPrefab;

   // The period tabs
   public GameObject dayTab;
   public GameObject weekTab;
   public GameObject monthTab;

   // The period tab buttons
   public Button dayTabButton;
   public Button weekTabButton;
   public Button monthTabButton;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void updatePanelWithLeaderBoardEntries(LeaderBoardsManager.Period period,
      double secondsLeftUntilRecalculation, LeaderBoardInfo[] farmingEntries, LeaderBoardInfo[] sailingEntries,
      LeaderBoardInfo[] exploringEntries, LeaderBoardInfo[] tradingEntries, LeaderBoardInfo[] craftingEntries,
      LeaderBoardInfo[] miningEntries) {

      // Update the board entries
      updateBoardWithEntries(farmingBoardRowsContainer, farmingEntries);
      updateBoardWithEntries(sailingBoardRowsContainer, sailingEntries);
      updateBoardWithEntries(exploringBoardRowsContainer, exploringEntries);
      updateBoardWithEntries(tradingBoardRowsContainer, tradingEntries);
      updateBoardWithEntries(craftingBoardRowsContainer, craftingEntries);
      updateBoardWithEntries(miningBoardRowsContainer, miningEntries);

      // Update the time left until recalculation
      // Show the seconds if it happened in the last 60s, the
      // minutes in the last 60m, etc, up to the days.
      TimeSpan timeLeft = TimeSpan.FromSeconds(secondsLeftUntilRecalculation);
      if (timeLeft.TotalSeconds <= 60) {
         if (timeLeft.Seconds <= 1) {
            timeLeftUntilRecalculationText.text = timeLeft.Seconds.ToString() + " second left";
         } else {
            timeLeftUntilRecalculationText.text = timeLeft.Seconds.ToString() + " seconds left";
         }
      } else if (timeLeft.TotalMinutes <= 60) {
         timeLeftUntilRecalculationText.text = timeLeft.Minutes.ToString() + " min left";
      } else if (timeLeft.TotalHours <= 24) {
         if (timeLeft.Hours <= 1) {
            timeLeftUntilRecalculationText.text = timeLeft.Hours.ToString() + " hour left";
         } else {
            timeLeftUntilRecalculationText.text = timeLeft.Hours.ToString() + " hours left";
         }
      } else {
         if (timeLeft.Days <= 1) {
            timeLeftUntilRecalculationText.text = timeLeft.Days.ToString() + " day left";
         } else {
            timeLeftUntilRecalculationText.text = timeLeft.Days.ToString() + " days left";
         }
      }

      // Select the correct period tab
      switch (period) {
         case LeaderBoardsManager.Period.Day:
            dayTab.SetActive(true);
            weekTab.SetActive(false);
            monthTab.SetActive(false);
            dayTabButton.interactable = false;
            weekTabButton.interactable = true;
            monthTabButton.interactable = true;
            break;
         case LeaderBoardsManager.Period.Week:
            dayTab.SetActive(false);
            weekTab.SetActive(true);
            monthTab.SetActive(false);
            dayTabButton.interactable = true;
            weekTabButton.interactable = false;
            monthTabButton.interactable = true;
            break;
         case LeaderBoardsManager.Period.Month:
            dayTab.SetActive(false);
            weekTab.SetActive(false);
            monthTab.SetActive(true);
            dayTabButton.interactable = true;
            weekTabButton.interactable = true;
            monthTabButton.interactable = false;
            break;
         default:
            break;
      }
   }

   public void onDayPeriodTabButtonPress () {
      _selectedPeriod = LeaderBoardsManager.Period.Day;
      Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(_selectedPeriod);
   }

   public void onWeekPeriodTabButtonPress () {
      _selectedPeriod = LeaderBoardsManager.Period.Week;
      Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(_selectedPeriod);
   }

   public void onMonthPeriodTabButtonPress () {
      _selectedPeriod = LeaderBoardsManager.Period.Month;
      Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(_selectedPeriod);
   }

   private void updateBoardWithEntries (GameObject rowsContainer, LeaderBoardInfo[] entries) {
      // Clear out any old info
      rowsContainer.DestroyChildren();

      // Instantiate the rows
      for (int i = 0; i < entries.Length; i++) {
         LeaderBoardRow row = Instantiate(leaderBoardRowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForLeaderBoard(entries[i]);
      }
   }

   #region Private Variables

   // The currently selected period
   private LeaderBoardsManager.Period _selectedPeriod = DEFAULT_PERIOD;

   #endregion
}
