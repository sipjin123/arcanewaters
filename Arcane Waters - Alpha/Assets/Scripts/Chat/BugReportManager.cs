using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BugReportManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static BugReportManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   [ServerOnly]
   public void storeBugReportOnServer (NetEntity player, string subject, string message, int ping, int fps) {
      // Check when they last submitted a bug report
      if (_lastBugReportTime.ContainsKey(player.userId)) {
         float timeSinceLastReport = Time.time - _lastBugReportTime[player.userId];
         if (timeSinceLastReport < BUG_REPORT_INTERVAL) {
            D.warning("Ignoring bug report from player: " + player.userId + " who submitted one: " +
                timeSinceLastReport + " seconds ago.");
            return;
         }
      }

      // Confirm it's not too large, these values were already checked on the client
      if (subject.Length > MAX_SUBJECT_LENGTH || System.Text.Encoding.Unicode.GetByteCount(message) > MAX_BYTES) {
         D.warning("Bug report too large/long to store in the database! Subject: " + subject);
         return;
      }

      // Keep track of the time they last submitted a bug report
      _lastBugReportTime[player.userId] = Time.time;

      // Save the report in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.saveBugReport(player, subject, message, ping, fps);

         // Send a confirmation to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.BugReport, player);
         });
      });
   }

   public void sendBugReportToServer (string subjectString) {
      StopAllCoroutines();
      StartCoroutine(CO_CollectDataAndSendBugReport(subjectString));
   }

   private IEnumerator CO_CollectDataAndSendBugReport (string subjectString) {
      NetEntity player = Global.player;
      string bugReport = D.getLogString();
      int ping = PingPanel.getPing();

      if (player == null) {
         D.warning("Can't submit a bug report because we have no player object.");
         yield break;
      }

      // Require a decent subject
      if (subjectString.Length < MIN_SUBJECT_LENGTH) {
         ChatManager.self.addChat("Please include a descriptive subject for the bug report.", ChatInfo.Type.System);
         yield break;
      }

      // Make sure the subject isn't too long
      if (subjectString.Length > MAX_SUBJECT_LENGTH) {
         ChatManager.self.addChat("The subject of the bug report is too long.", ChatInfo.Type.System);
         yield break;
      }

      // Make sure the bug report file hasn't grown too large
      if (System.Text.Encoding.Unicode.GetByteCount(bugReport) > MAX_BYTES) {
         ChatManager.self.addChat("The bug report file is currently too large to submit.", ChatInfo.Type.System);
         yield break;
      }

      int userId = player.userId;

      // Make sure we're not spamming the server
      if (_lastBugReportTime.ContainsKey(userId)) {
         if (Time.time - _lastBugReportTime[userId] < BUG_REPORT_INTERVAL) {
            D.warning("Bug report already submitted.");
            yield break;
         }
      }

      // Calculate the frames per second over the given time interval
      float totalTime = 0;
      int frameCount = 0;
      while (totalTime < FPS_CALCULATION_INTERVAL) {
         totalTime += Time.unscaledDeltaTime;
         frameCount++;
         yield return null;
      }
      int fps = Mathf.FloorToInt(((float)frameCount) / totalTime);

      Global.player.rpc.Cmd_BugReport(subjectString, bugReport, ping, fps);
      _lastBugReportTime[Global.player.userId] = Time.time;
   }

   #region Private Variables

   // The amount of time to wait between consecutive bug reports
   protected static float BUG_REPORT_INTERVAL = 30f;

   // The minimum subject length for bugs
   protected static int MIN_SUBJECT_LENGTH = 3;

   // The max length of bug report subject lines
   protected static int MAX_SUBJECT_LENGTH = 256;

   // The max length of bug reports
   protected static int MAX_BUG_REPORT_LENGTH = 1600;

   // The number of bytes in a megabyte
   protected static int ONE_MEGABYTE_IN_BYTES = 1000000;

   // The maximum number of bytes we'll allow a submitted bug report to have
   protected static int MAX_BYTES = ONE_MEGABYTE_IN_BYTES * 5;

   // The time interval used to calculate the average fps
   protected static float FPS_CALCULATION_INTERVAL = 1f;

   // Stores the time at which a bug report was last submitted, indexed by user ID for the server's sake
   protected Dictionary<int, float> _lastBugReportTime = new Dictionary<int, float>();

   #endregion
}
