﻿using UnityEngine;
using System.Text;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class LeaderBoardsManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static LeaderBoardsManager self;

   // The leaderboard periods
   public enum Period { Day = 0, Week = 1, Month = 2 }

   #endregion

   public void scheduleLeaderBoardRecalculation () {
      StartCoroutine(CO_ScheduleLeaderBoardsRecalculation());
   }

   public void pruneJobHistory () {
      // Calculate the date before which the records must be deleted
      DateTime untilDate = DateTime.UtcNow - new TimeSpan(JOB_HISTORY_ENTRIES_LIFETIME, 0, 0, 0);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Delete the rows
         DB_Main.pruneJobHistory(untilDate);
      });
   }

   public TimeSpan getTimeLeftUntilRecalculation(Period period, DateTime lastCalculationDate) {
      // Calculate the time left
      TimeSpan timeLeft;
      switch (period) {
         case Period.Day:
            timeLeft = lastCalculationDate.AddHours(DAILY_RECALC_HOUR_INTERVAL).Subtract(DateTime.UtcNow);
            break;
         case Period.Week:
            timeLeft = lastCalculationDate.AddDays(WEEKLY_RECALC_DAY_INTERVAL).Subtract(DateTime.UtcNow);
            break;
         case Period.Month:
            timeLeft = lastCalculationDate.AddMonths(MONTHLY_RECALC_MONTH_INTERVAL).Subtract(DateTime.UtcNow);
            break;
         default:
            break;
      }
      return timeLeft;
   }

   private void Awake () {
      self = this;
   }

   private void tryRecalculateLeaderBoards () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         
         // Query the last calculation date of the boards
         DateTime lastDailyCalculationDate = DB_Main.getLeaderBoardEndDate(Period.Day);
         DateTime lastWeeklyCalculationDate = DB_Main.getLeaderBoardEndDate(Period.Week);
         DateTime lastMonthlyCalculationDate = DB_Main.getLeaderBoardEndDate(Period.Month);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // The recalculation end date is always a rounded up hour
            DateTime roundedUtcNow = DateTime.UtcNow.Date + new TimeSpan(DateTime.UtcNow.Hour, 0, 0);

            /* DAILY BOARDS */

            // Check if 24 hours have passed since the last calculation
            if (getTimeLeftUntilRecalculation(Period.Day, lastDailyCalculationDate).Ticks <= 0) {

               // Set a new 24h interval from the current date backwards
               DateTime endDate = roundedUtcNow;
               DateTime startDate = endDate.AddHours(-DAILY_RECALC_HOUR_INTERVAL);

               // Recalculate the leader boards
               calculateBoards(Period.Day, startDate, endDate);
            }

            /* WEEKLY BOARDS */

            // Check if one week has passed since the last calculation
            if (getTimeLeftUntilRecalculation(Period.Week, lastWeeklyCalculationDate).Ticks <= 0) {

               // Set a new interval of 7 days from the current date backwards
               DateTime endDate = roundedUtcNow;
               DateTime startDate = endDate.AddDays(-WEEKLY_RECALC_DAY_INTERVAL);

               // Recalculate the leader boards
               calculateBoards(Period.Week, startDate, endDate);
            }

            /* MONTHLY BOARDS */

            // Check if one month has passed since the last calculation
            if (getTimeLeftUntilRecalculation(Period.Month, lastMonthlyCalculationDate).Ticks <= 0) {

               // Set a new interval of 1 month from the current date backwards
               DateTime endDate = roundedUtcNow;
               DateTime startDate = endDate.AddMonths(-MONTHLY_RECALC_MONTH_INTERVAL);

               // Recalculate the leader boards
               calculateBoards(Period.Month, startDate, endDate);
            }
         });
      });
   }

   private void calculateBoards (Period period, DateTime startDate, DateTime endDate) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Delete the boards for this time period
         DB_Main.deleteLeaderBoards(period);

         // Calculate the boards with DB queries
         List<LeaderBoardInfo> farmingBoard = DB_Main.calculateLeaderBoard(Jobs.Type.Farmer, period, startDate, endDate);
         List<LeaderBoardInfo> sailingBoard = DB_Main.calculateLeaderBoard(Jobs.Type.Sailor, period, startDate, endDate);
         List<LeaderBoardInfo> exploringBoard = DB_Main.calculateLeaderBoard(Jobs.Type.Explorer, period, startDate, endDate);
         List<LeaderBoardInfo> tradingBoard = DB_Main.calculateLeaderBoard(Jobs.Type.Trader, period, startDate, endDate);
         List<LeaderBoardInfo> craftingBoard = DB_Main.calculateLeaderBoard(Jobs.Type.Crafter, period, startDate, endDate);
         List<LeaderBoardInfo> miningBoard = DB_Main.calculateLeaderBoard(Jobs.Type.Miner, period, startDate, endDate);
            
         // Insert the new records in the leader board table
         DB_Main.updateLeaderBoards(farmingBoard);
         DB_Main.updateLeaderBoards(sailingBoard);
         DB_Main.updateLeaderBoards(exploringBoard);
         DB_Main.updateLeaderBoards(tradingBoard);
         DB_Main.updateLeaderBoards(craftingBoard);
         DB_Main.updateLeaderBoards(miningBoard);

         // Update the leader board dates intervals
         DB_Main.updateLeaderBoardDates(period, startDate, endDate);
      });
   }

   private IEnumerator CO_ScheduleLeaderBoardsRecalculation () {
      // Wait until our server is defined
      while (ServerNetworkingManager.self.server == null) {
         yield return null;
      }

      // Wait until our server port is initialized
      while (ServerNetworkingManager.self.server.networkedPort.Value == 0) {
         yield return null;
      }

      // Get our server
      NetworkedServer server = ServerNetworkingManager.self.server;

      // Check that our server is the main server
      if (server.isMasterServer()) {
         InvokeRepeating(nameof(tryRecalculateLeaderBoards), 0f, (float) TimeSpan.FromHours(1).TotalSeconds);
      }
   }

   #region Private Variables

   // The number of days until the job history entries are deleted
   private static int JOB_HISTORY_ENTRIES_LIFETIME = 60;

   // The interval for the daily recalculation - in hours
   private static int DAILY_RECALC_HOUR_INTERVAL = 24;

   // The interval for the daily recalculation - in days
   private static int WEEKLY_RECALC_DAY_INTERVAL = 7;

   // The interval for the monthly recalculation - in months
   private static int MONTHLY_RECALC_MONTH_INTERVAL = 1;

   #endregion
}
