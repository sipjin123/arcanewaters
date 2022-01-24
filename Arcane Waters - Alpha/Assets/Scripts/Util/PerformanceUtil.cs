using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

public class PerformanceUtil : MonoBehaviour {
   #region Public Variables

   // Singleton instance
   public static PerformanceUtil self;

   [System.Serializable]
   public class PerformanceTestResult {
      // Set to true when the performance test is done, and the result is ready
      public bool isDone = false;

      // Values to measure cpu usage
      public float minCpu;
      public float maxCpu;
      public float averageCpu;

      // Values to measure ram usage
      public float minRam;
      public float maxRam;
      public float averageRam;

      // Values to measure fps
      public float minFps;
      public float maxFps;
      public float averageFps;

      public string getResultString () {
         return "CPU - Min: " + minCpu + "ms, Max: " + maxCpu + "ms, Avg: " + averageCpu + "ms. RAM - Min: " + minRam + ", Max: " + maxRam + ", Avg: " + averageRam + 
            ". FPS - Min: " + minFps + ", Max: " + maxFps + ", Avg: " + averageFps + ".";
      }
   }

   #endregion

   private void Awake () {
      self = this;
      _processorCount = SystemInfo.processorCount / 2;
      _lastCpuTime = new TimeSpan(0);
      _currentProcess = Process.GetCurrentProcess();

      D.debug("Starting up PerformanceUtil.");

      StartCoroutine(CO_Initialise());
   }

   private IEnumerator CO_Initialise () {
      // Wait for server to start up
      yield return new WaitForSeconds(60.0f);

      // Only measure performance on the server
      if (!NetworkServer.active) {
         D.debug("Disabling PerformanceUtil as this isn't a server.");
         this.enabled = false;
         yield break;
      }

      // Only measure performance using zabbix on the remote servers
      if (!Util.isBatchServer()) {
         yield break;
      } else {
         getZabbixPerformanceResult();
      }
   }

   private async void getZabbixPerformanceResult () {
      for (int i = 0; i < 60; i++) {
         D.debug("Starting to get zabbix performance result.");

         Stopwatch stopwatch = Stopwatch.StartNew();
         string processArguments = (Util.isProductionBuild()) ? $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 34410 -Mode 1 -DataTable 0 -Limit 1" : $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 34411 -Mode 1 -DataTable 0 -Limit 1";

         ProcessStartInfo startInfo = new ProcessStartInfo() {
            FileName = "powershell.exe",
            Arguments = processArguments,
            UseShellExecute = false,
            RedirectStandardOutput = true
         };

         Process test = Process.Start(startInfo);

         await Task.Run(() => {
            test.WaitForExit();
         });

         string result = test.StandardOutput.ReadToEnd();
         D.debug(result + ", Getting result from zabbix took: " + stopwatch.Elapsed.TotalSeconds + " seconds.");
      }
   }

   private void Update () {
      _currentProcess.Refresh();
      TimeSpan cpuTime = _currentProcess.TotalProcessorTime;
      TimeSpan newCpuTime = cpuTime - _lastCpuTime;

      _lastCpuTime = cpuTime;
      _cpuUsage = (float)newCpuTime.TotalMilliseconds;
   }

   public static IEnumerator CO_PerformanceTest (float duration, PerformanceTestResult result) {
      // Initial cpu values
      float minCpu = float.MaxValue;
      float maxCpu = 0.0f;
      float totalCpu = 0.0f;

      // Initial ram values
      float minRam = float.MaxValue;
      float maxRam = 0.0f;
      float totalRam = 0.0f;

      // Initial fps values
      float minFps = float.MaxValue;
      float maxFps = 0.0f;
      float totalFps = 0.0f;

      int frameCount = 0;
      float totalDeltaTime = 0.0f;
      float timer = 0.0f;

      // Begin the performance test
      while (timer <= duration) {

         // Get current usage values
         float cpuUsage = getCpuUsage();
         float ramUsage = getRamUsage();
         float fps = getFps(Time.deltaTime);

         // Update totals
         totalCpu += cpuUsage;
         totalRam += ramUsage;
         totalFps += fps;
         totalDeltaTime += Time.deltaTime;
         frameCount++;

         // Update minimum values, if applicable
         if (cpuUsage < minCpu) { minCpu = cpuUsage; }
         if (ramUsage < minRam) { minRam = ramUsage; }
         if (fps < minFps) { minFps = fps; }

         // Update maximum values, if applicable
         if (cpuUsage > maxCpu) { maxCpu = cpuUsage; }
         if (ramUsage > maxRam) { maxRam = ramUsage; }
         if (fps > maxFps) { maxFps = fps; }

         timer += Time.deltaTime;
         yield return null;
      }

      // Calculate averages
      float averageDeltaTime = totalDeltaTime / frameCount;
      float averageCpu = totalCpu * averageDeltaTime / timer;
      float averageRam = totalRam * averageDeltaTime / timer;
      float averageFps = totalFps * averageDeltaTime / timer;

      // Populate class with test results
      result.minCpu = minCpu;
      result.maxCpu = maxCpu;
      result.averageCpu = averageCpu;
      result.minRam = minRam;
      result.maxRam = maxRam;
      result.averageRam = averageRam;
      result.minFps = minFps;
      result.maxFps = maxFps;
      result.averageFps = averageFps;

      result.isDone = true;
   }

   public static float getCpuUsage () {
      return self._cpuUsage;
   }

   public static float getRamUsage () {
      return 0.0f;
   }

   public static float getFps (float deltaTime) {
      return (1.0f / deltaTime);
   }

   #region Private Variables

   // A reference to the process running this server/client
   private Process _currentProcess;

   // How many physical cores (processors) this machine has
   private int _processorCount;

   // The cpu usage, the last time we measured it
   private float _cpuUsage;

   // A reference to the timespan used to measure the cpu's time
   private TimeSpan _lastCpuTime;

   #endregion
}
