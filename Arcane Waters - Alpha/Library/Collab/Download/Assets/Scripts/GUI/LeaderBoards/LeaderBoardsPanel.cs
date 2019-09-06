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

   // The faction icon
   public Image factionIcon;

   // The faction text
   public Text factionText;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;

      // Initialize the faction filter
      _factionList = new List<Faction.Type>();
      foreach (Faction.Type faction in System.Enum.GetValues(typeof(Faction.Type))) {
         _factionList.Add(faction);
      }
      _selectedFactionIndex = 0;
   }

   public override void Start () {
      base.Start();

      _initialized = true;
   }

   public void updatePanelWithLeaderBoardEntries(LeaderBoardsManager.Period period, Faction.Type boardFaction,
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

      // Temporarily disables the 'onValueChanged' event on the toggles
      ToggleEvent backupDayEvent = dayToggle.onValueChanged;
      ToggleEvent backupWeekEvent = weekToggle.onValueChanged;
      ToggleEvent backupMonthEvent = monthToggle.onValueChanged;
      dayToggle.onValueChanged = emptyToggleEvent;
      weekToggle.onValueChanged = emptyToggleEvent;
      monthToggle.onValueChanged = emptyToggleEvent;

      // Select the correct period toggle
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

      // Restore the onValueChanged events on the toggles
      dayToggle.onValueChanged = backupDayEvent;
      weekToggle.onValueChanged = backupWeekEvent;
      monthToggle.onValueChanged = backupMonthEvent;

      // Select the correct faction
      _selectedFactionIndex = _factionList.IndexOf(boardFaction);
      refreshFactionFilterDisplay();
   }

   public void onPeriodToggleChange (Toggle toggle) {
      // Avoid unecessary calls
      if (_initialized && isShowing() && toggle.isOn) {
         // Request the entries from the server
         Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(getSelectedPeriod(), getSelectedFaction());
      }
   }

   public void onFactionFilterLeftButtonPress () {
      _selectedFactionIndex--;
      if (_selectedFactionIndex < 0) {
         _selectedFactionIndex = _factionList.Count - 1;
      }

      // Update the displayed selected faction
      refreshFactionFilterDisplay();

      // Request the entries from the server
      Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(getSelectedPeriod(), getSelectedFaction());
   }

   public void onFactionFilterRightButtonPress () {
      _selectedFactionIndex++;
      if (_selectedFactionIndex >= _factionList.Count) {
         _selectedFactionIndex = 0;
      }

      // Update the displayed selected faction
      refreshFactionFilterDisplay();

      // Request the entries from the server
      Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(getSelectedPeriod(), getSelectedFaction());
   }

   private void refreshFactionFilterDisplay () {
      // Retrieve the correct faction icon
      factionIcon.sprite = Faction.getIcon(_factionList[_selectedFactionIndex]);

      // Set the faction name text
      if (_factionList[_selectedFactionIndex] == Faction.Type.None) {
         factionText.text = "All factions";
      } else {
         factionText.text = Faction.toString(_factionList[_selectedFactionIndex]);
      }
   }

   private LeaderBoardsManager.Period getSelectedPeriod () {
      // Determine which toggle is on
      if (dayToggle.isOn) {
         return LeaderBoardsManager.Period.Day;
      } else if (weekToggle.isOn) {
         return LeaderBoardsManager.Period.Week;
      } else {
         return LeaderBoardsManager.Period.Month;
      }
   }

   private Faction.Type getSelectedFaction () {
      return _factionList[_selectedFactionIndex];
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

   // Get set to true when the MonoBehaviour has started
   private bool _initialized;

   // The list of factions filters
   private List<Faction.Type> _factionList;

   // The index of the currently selected faction
   private int _selectedFactionIndex;

   #endregion
}
