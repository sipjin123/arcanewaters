﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Diagnostics;

public class MetricsManager : GenericGameManager
{
   #region Public Variables

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   public void Start () {
      startGatheringMetrics();
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

            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               // Clear old metrics
               DB_Main.clearOldGameMetrics(_metricLifetimeSecs);

               // Upload Players Count metric
               int playersCount = InstanceManager.self.getPlayerCountAllInstances();
               DB_Main.setGameMetric(machineID, procID, procName, "PLAYERS_COUNT", playersCount.ToString(), Metric.MetricValueType.INT);
            });
         } catch (Exception ex) {
            D.warning("MetricsManager: Couldn't update metrics. | " + ex.Message);
         }
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
