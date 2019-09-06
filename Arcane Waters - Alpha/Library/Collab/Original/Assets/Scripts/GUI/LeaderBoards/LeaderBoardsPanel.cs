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

   // An empty event to modify the UI toggle values without triggering an update of the boards
   public static Toggle.ToggleEvent emptyToggleEvent = new Toggle.ToggleEvent();

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

   // The period toggles
   public Toggle dayToggle;
   public Toggle weekToggle;
   public Toggle monthToggle;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void Start () {
      base.Start();

      _initialized = true;
   }

   public void updatePanelWithLeaderBoardEntries(LeaderBoardsManager.Period period, double secondsLeftUntilRecalculation,
      LeaderBoardInfo[] farmingEntries, LeaderBoardInfo[] sailingEntries, LeaderBoardInfo[] exploringEntries,
      LeaderBoardInfo[] tradingEntries, LeaderBoardInfo[] craftingEntries, LeaderBoardInfo[] miningEntries) {

      // Update the board entries
      updateBoardWithEntries(farmingBoardRowsContainer, farmingEntries);
      updateBoardWithEntries(sailingBoardRowsContainer, sailingEntries);
      updateBoardWithEntries(exploringBoardRowsContainer, exploringEntries);
      updateBoardWithEntries(tradingBoardRowsContainer, tradingEntries);
      updateBoardWithEntries(craftingBoardRowsContainer, craftingEntries);
      updateBoardWithEntries(miningBoardRowsContainer, miningEntries);

      // Updates the time left until recalculation
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

      // Temporarily disables the 'onValueChanged' event on the toggles
      ToggleEvent backupDayEvent = dayToggle.onValueChanged;
      ToggleEvent backupWeekEvent = weekToggle.onValueChanged;
      ToggleEvent backupMonthEvent = monthToggle.onValueChanged;
      dayToggle.onValueChanged = emptyToggleEvent;
      weekToggle.onValueChanged = emptyToggleEvent;
      monthToggle.onValueChanged = emptyToggleEvent;

      // Selects the correct period toggle
      switch (period) {
         case LeaderBoardsManager.Period.Day:
            dayToggle.isOn = true;
            break;
         case LeaderBoardsManager.Period.Week:
            weekToggle.isOn = true;
            break;
         case LeaderBoardsManager.Period.Month:
            monthToggle.isOn = true;
            break;
         default:
            break;
      }

      // Restores the onValueChanged events on the toggles
      dayToggle.onValueChanged = backupDayEvent;
      weekToggle.onValueChanged = backupWeekEvent;
      monthToggle.onValueChanged = backupMonthEvent;
   }

   public void onPeriodToggleChange (Toggle toggle) {
      // Avoid unecessary calls
      if (_initialized && isShowing() && toggle.isOn) {
         // Determines which toggle is on and request the entries from the server
         if (dayToggle.isOn) {
            Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(LeaderBoardsManager.Period.Day);
         } else if (weekToggle.isOn) {
            Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(LeaderBoardsManager.Period.Week);
         } else {
            Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(LeaderBoardsManager.Period.Month);
         }
      }
   }

   private void updateBoardWithEntries (GameObject rowsContainer, LeaderBoardInfo[] entries) {
      // Clear out any old info
      rowsContainer.DestroyChildren();

      // Instantiates the rows
      for (int i = 0; i < entries.Length; i++) {
         LeaderBoardRow row = Instantiate(leaderBoardRowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForLeaderBoard(entries[i]);
      }
   }

   #region Private Variables

   // Get set to true when the MonoBehaviour has started
   private bool _initialized;

   #endregion
}
