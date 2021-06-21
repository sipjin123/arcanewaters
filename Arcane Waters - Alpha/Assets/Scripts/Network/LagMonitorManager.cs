using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class LagMonitorManager : GenericGameManager
{
   #region Public Variables

   // The interval in seconds the clients log their ping
   public static float CLIENT_MONITOR_INTERVAL = 5;

   // The interval in seconds the server will calculate the ping and fps statistics
   public static float SERVER_MONITOR_INTERVAL = 30;

   // The time interval over which the server calculates the performance values
   public static float SERVER_PERFORMANCE_CALCULATION_INTERVAL = 1f;
   
   // The ping threshold in ms over which a warning must be logged on the server
   public static float PING_THRESHOLD = 500;

   // Self
   public static LagMonitorManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      _lastPingSentToServerTime = NetworkTime.time;
      InvokeRepeating(nameof(calculateAveragePing), CLIENT_MONITOR_INTERVAL, CLIENT_MONITOR_INTERVAL);
   }

   public void startLagMonitor () {
      InvokeRepeating(nameof(capturePerformance), SERVER_PERFORMANCE_CALCULATION_INTERVAL, SERVER_PERFORMANCE_CALCULATION_INTERVAL);
      InvokeRepeating(nameof(monitorServer), SERVER_MONITOR_INTERVAL, SERVER_MONITOR_INTERVAL);
   }

   public void Update () {
      if (NetworkServer.active) {
         // Add this frame and the delta time for fps calculation
         _fpsTotalTime += Time.unscaledDeltaTime;
         _frameCount++;
      }

      if (Util.isServerNonHost()) {
         return;
      }

      _pingSum += Util.getPing();
      _pingCount++;

      // Try to send the ping to the server as soon as possible when the time interval ends
      if (Global.player != null && NetworkTime.time - _lastPingSentToServerTime > CLIENT_MONITOR_INTERVAL) {
         Global.player.rpc.Cmd_ReceiveClientPing(_lastAveragePing);
         _lastPingSentToServerTime = NetworkTime.time;
      }
   }

   private void calculateAveragePing () {
      if (Util.isServerNonHost()) {
         return;
      }

      _lastAveragePing = (float) _pingSum / _pingCount;
      _pingSum = 0;
      _pingCount = 0;

      // When the ping becomes too high, log a warning on the client
      if (_lastAveragePing > PING_THRESHOLD) {
         D.warning("The server has become unresponsive!");
      }

      // Log the ping in auto test mode
      if (CommandCodes.get(CommandCodes.Type.AUTO_TEST)) {
         D.debug($"Ping: {(int)_lastAveragePing}");
      }
   }

   private void sendAveragePingToServer () {
      if (Util.isServerNonHost()) {
         return;
      }

      if (Global.player == null) {
         return;
      }

      Global.player.rpc.Cmd_ReceiveClientPing(_lastAveragePing);
   }

   [Server]
   private void capturePerformance () {
      // Calculate the fps
      int fps = Mathf.FloorToInt(((float) _frameCount) / _fpsTotalTime);
      _serverFps.Add(fps);
      _frameCount = 0;
      _fpsTotalTime = 0f;
   }

   [Server]
   public void receiveClientPing (int userId, float ping) {
      _averageClientPings[userId] = ping;
   }

   [Server]
   private void monitorServer () {
      if (!NetworkServer.active) {
         return;
      }

      if (ServerNetworkingManager.self.server == null) {
         return;
      }

      // Clear clients that are no longer connected
      List<int> usersToRemove = new List<int>();
      foreach (int userId in _averageClientPings.Keys) {
         if (!ServerNetworkingManager.self.server.connectedUserIds.Contains(userId)) {
            usersToRemove.Add(userId);
         }
      }

      foreach (int userId in usersToRemove) {
         _averageClientPings.Remove(userId);
      }

      getStatistics(_averageClientPings.Values, _averageClientPings.Count, out float pingMin, out float pingMax, out float pingMean, out float pingMedian);
      getStatistics(_serverFps, _serverFps.Count, out float fpsMin, out float fpsMax, out float fpsMean, out float fpsMedian);

      // Clear the values captured in this time interval
      _serverFps.Clear();

      if (CommandCodes.get(CommandCodes.Type.AUTO_TEST)) {
         // The seconds since the server started
         TimeSpan serverTime = TimeSpan.FromSeconds(NetworkTime.time);
         string serverTimeStr = string.Format("{0:D2}m:{1:D2}s", serverTime.Minutes, serverTime.Seconds);

         // Log the statistics
         D.debug($"Server statistics:\n" +
            $"The stress test started {serverTimeStr} ago.\n" +
            $"Ping ({_averageClientPings.Count} clients) - min: {(int) pingMin} max: {(int) pingMax} mean: {(int) pingMean} median: {(int) pingMedian}\n" +
            $"FPS (last {SERVER_MONITOR_INTERVAL} seconds) - min: {(int) fpsMin} max: {(int) fpsMax} mean: {(int) fpsMean} median: {(int) fpsMedian}");

         // Broadcast the time since the server started
         string message = "The stress test started " + serverTimeStr + " ago";
         ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.UtcNow, ChatInfo.Type.System);
         ServerNetworkingManager.self?.sendGlobalChatMessage(chatInfo);
      }

      // When the average ping goes over the threshold, log a warning
      if (pingMean > PING_THRESHOLD) {
         D.warning("The server has become unresponsive!");
      }
   }

   private void getStatistics (IEnumerable<float> data, int dataCount, out float min, out float max, out float mean, out float median) {
      // Default values
      min = 0;
      max = 0;
      mean = 0;
      median = 0;

      if (dataCount > 0) {
         min = data.Min();
         max = data.Max();
         mean = data.Average();

         // Calculate the median
         List<float> sortedData = data.OrderBy(n => n).ToList();
         median = sortedData[sortedData.Count / 2] + sortedData[(sortedData.Count - 1) / 2];
         median /= 2;
      }
   }

   #region Private Variables

   // The sum of pings
   private int _pingSum = 0;

   // The number of times the ping has been summed
   private int _pingCount = 0;

   // The time at which the last ping calculation was sent to the server
   private double _lastPingSentToServerTime = 0;

   // The last average ping calculated on this client
   private float _lastAveragePing = 0f;

   // The number of frames counted for the fps calculation
   private int _frameCount = 0;

   // The total time the frames have been counted for the fps calculation
   private float _fpsTotalTime = 0f;

   // The average pings for each connected user
   private Dictionary<int, float> _averageClientPings = new Dictionary<int, float>();

   // All the server fps calculated since the last time statistics were logged
   private List<float> _serverFps = new List<float>();

   #endregion
}

