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

   // Whether we should be updating our cpu and ram from zabbix
   public bool updateZabbixData = false;

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

      StartCoroutine(CO_Initialise());
   }

   private IEnumerator CO_Initialise () {
      // Wait for server to start up
      yield return new WaitForSeconds(60.0f);

      // Only measure performance on the server
      if (!NetworkServer.active) {
         this.enabled = false;
         yield break;
      }

      if (Util.isBatch() && (Util.isProductionBuild() || Util.isDevelopmentBuild())) {
         getTotalRamZabbix();
         updateCpuResultZabbix();
         updateRamResultZabbix();
      }
   }

   private async void updateCpuResultZabbix () {
      while (this.enabled) {
         if (updateZabbixData) {
            string cpuProcessArguments = (Util.isProductionBuild()) ? $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 33614 -Mode 1 -DataTable 0 -Limit 1" : $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 33748 -Mode 1 -DataTable 0 -Limit 1";
            string result = await getZabbixPerformanceParameter(cpuProcessArguments);
            float floatResult;
            if (float.TryParse(result, out floatResult)) {
               _zabbixCpuUsage = floatResult;
            }
         } else {
            await Task.Delay(1000);
         }
      }
   }

   private async void updateRamResultZabbix () {
      while (this.enabled) {
         if (updateZabbixData) {
            string ramProcessArguments = (Util.isProductionBuild()) ? $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 33623 -Mode 1 -DataTable 3 -Limit 1" 
               : $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 33757 -Mode 1 -DataTable 3 -Limit 1";
            string result = await getZabbixPerformanceParameter(ramProcessArguments);
            float floatResult;
            if (float.TryParse(result, out floatResult)) {
               _zabbixRamUsage = floatResult;
            }
         } else {
            await Task.Delay(1000);
         }
      }
   }

   private async void getTotalRamZabbix () {
      string totalRamProcessArguments = (Util.isProductionBuild()) ? $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 33622 -Mode 1 -DataTable 3 -Limit 1" : $"cd C:/integrations/zabbix; ./GetHistory.ps1 -ItemID 33756 -Mode 1 -DataTable 3 -Limit 1";
      string result = await getZabbixPerformanceParameter(totalRamProcessArguments);
      float floatResult;
      if (float.TryParse(result, out floatResult)) {
         _zabbixTotalRam = floatResult;
      }
   }

   private async Task<string> getZabbixPerformanceParameter (string processArguments) {
      ProcessStartInfo startInfo = new ProcessStartInfo() {
         FileName = "powershell.exe",
         Arguments = processArguments,
         UseShellExecute = false,
         RedirectStandardOutput = true,
         CreateNoWindow = true,
      };

      Process test = Process.Start(startInfo);

      await Task.Run(() => {
         test.WaitForExit();
      });

      return test.StandardOutput.ReadToEnd();
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

   public static float getZabbixCpuUsage () {
      return self._zabbixCpuUsage;
   }

   public static float getZabbixRamUsage () {
      // Avoid divide by zero
      if (self._zabbixTotalRam <= Mathf.Epsilon) {
         return 0.0f;
      }

      return (100.0f * (self._zabbixRamUsage / self._zabbixTotalRam));
   }

   public static void resetZabbixValues () {
      self._zabbixCpuUsage = 0.0f;
      self._zabbixRamUsage = 0.0f;
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

   // The cpu usage last measured by zabbix
   private float _zabbixCpuUsage = 0.0f;

   // The ram usage last measured by zabbix
   private float _zabbixRamUsage = 0.0f;

   // The total ram, measured by zabbix
   private float _zabbixTotalRam = 0.0f;

   #endregion
}
