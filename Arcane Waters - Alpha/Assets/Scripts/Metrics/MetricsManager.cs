using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Diagnostics;

public class MetricsManager : GenericGameManager
{
   #region Public Variables

   // Contains the names of the available set of tracked metrics
   public class MetricNames
   {
      // The "Players Count" Metric
      public const string PLAYERS_COUNT = "PLAYERS_COUNT";
   }

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   public void Start () {
      startGatheringMetrics();
      StartCoroutine(CO_ClearAllPreviousGameMetrics());
   }

   private void startGatheringMetrics () {
      InvokeRepeating(nameof(gatherMetrics), 0, _updateFrequencySecs);
   }

   private void stopGatheringMetrics () {
      CancelInvoke(nameof(gatherMetrics));
   }

   private void gatherMetrics () {
      if (NetworkServer.active) {
         try {
            string machineID = string.Empty;
            machineID = Environment.MachineName;
            Process proc = Process.GetCurrentProcess();
            string procName = proc.ProcessName;
            string procID = proc.Id.ToString();

            int playersCount = InstanceManager.self.getPlayerCountAllInstances();

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               // Clear old metrics
               DB_Main.clearOldGameMetrics(_metricLifetimeSecs);

               // Upload Players Count metric
               DB_Main.setGameMetric(machineID, procID, procName, "PLAYERS_COUNT", playersCount.ToString(), Metric.MetricValueType.INT);
            });
         } catch (Exception ex) {
            D.warning("MetricsManager: Couldn't update metrics. | " + ex.Message);
         }
      }
   }

   private IEnumerator CO_ClearAllPreviousGameMetrics () {
      // Wait until our server is defined
      while (ServerNetworkingManager.self == null || ServerNetworkingManager.self.server == null) {
         yield return null;
      }

      // Wait until our server port is initialized
      while (ServerNetworkingManager.self.server.networkedPort.Value == 0) {
         yield return null;
      }

      // Only the master server performs this operation
      if (ServerNetworkingManager.self.server.isMasterServer()) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.clearOldGameMetrics(0);
         });
      }
   }

   private void OnDestroy () {
      stopGatheringMetrics();
   }

   #region Private Variables

   // Update frequency
   [SerializeField]
   private float _updateFrequencySecs = 10.0f;

   // Metrics Lifetime in Seconds
   [SerializeField]
   private int _metricLifetimeSecs = 300;

   #endregion
}
