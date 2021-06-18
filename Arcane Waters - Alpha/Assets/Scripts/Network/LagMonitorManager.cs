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

   // The interval in seconds the clients sent the average ping value to the server
   public static float CLIENT_MONITOR_INTERVAL = 60;

   // The interval in seconds the server will calculate the average ping of all clients
   public static float SERVER_MONITOR_INTERVAL = 60;

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
      InvokeRepeating(nameof(monitorClientPing), SERVER_MONITOR_INTERVAL, SERVER_MONITOR_INTERVAL);
   }

   public void Update () {
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
   public void receiveClientPing (int userId, float ping) {
      _averageClientPings[userId] = ping;
   }

   [Server]
   private void monitorClientPing () {
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

      // Default values
      float min = 0;
      float max = 0;
      float mean = 0;
      float median = 0;

      // Calculate ping statistics
      if (_averageClientPings.Count > 0) {
         min = _averageClientPings.Values.Min();
         max = _averageClientPings.Values.Max();
         mean = _averageClientPings.Values.Average();

         // Calculate the median ping
         List<float> sortedPings = _averageClientPings.Values.OrderBy(n => n).ToList();
         median = sortedPings[sortedPings.Count / 2] + sortedPings[(sortedPings.Count - 1) / 2];
         median /= 2;
      }

      // Log the statistics in auto test mode
      if (CommandCodes.get(CommandCodes.Type.AUTO_TEST)) {
         D.debug($"Ping statistics for {_averageClientPings.Count} connected clients: min: {(int) min} max: {(int) max} mean: {(int) mean} median: {(int) median}");
      }

      // When the average ping goes over the threshold, log a warning
      if (mean > PING_THRESHOLD) {
         D.warning("The server has become unresponsive!");
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

   // The average pings for each connected user
   private Dictionary<int, float> _averageClientPings = new Dictionary<int, float>();

   #endregion
}

