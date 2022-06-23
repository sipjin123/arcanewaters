using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZabbixApi;
using ZabbixApi.Entities;
using Debug = UnityEngine.Debug;

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

      // StartCoroutine(CO_Initialise());

      if (Util.isBatch() && (Util.isProductionBuild() || Util.isDevelopmentBuild())) {
         setupZabbixWebRequest();
      }
   }

   private void setupZabbixWebRequest () {

      // Setup hist params
      _cpuHistParams.Add("output", "extend");
      _cpuHistParams.Add("sortfield", "clock");
      _cpuHistParams.Add("sortorder", "DESC");
      _cpuHistParams.Add("limit", "1");

      _ramHistParams.Add("output", "extend");
      _ramHistParams.Add("sortfield", "clock");
      _ramHistParams.Add("sortorder", "DESC");
      _ramHistParams.Add("limit", "1");

      // Fetch total ram once
      _totalRamHistParams.Add("output", "extend");
      _totalRamHistParams.Add("sortfield", "clock");
      _totalRamHistParams.Add("sortorder", "DESC");
      _totalRamHistParams.Add("limit", "1");

      if (Util.isDevelopmentBuild()) {
         _cpuHistParams.Add("itemids", "33748");
         _ramHistParams.Add("itemids", "33757");
         _totalRamHistParams.Add("itemids", "33756");
      } else {
         _cpuHistParams.Add("itemids", "33614");
         _ramHistParams.Add("itemids", "33623");
         _totalRamHistParams.Add("itemids", "33622");
      }

      string url = "";
      string user = "";
      string password = "";

      #if IS_SERVER_BUILD
      url = "https://zabbix.arcanewaters.com/api_jsonrpc.php";
      user = "integration";
      password = "xhsR8jmqhvt3QM7F4knZzVKJRugTVdpL";
      #endif
      
      _zabbixContext = new Context(url, user, password);

      updateZabbixTotalRam();
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
      if (self._zabbixContext != null) {
         string cpuString = self._zabbixContext.History.GetByType(History.HistoryType.FloatType, null, self._cpuHistParams).FirstOrDefault()?.value;
         if (float.TryParse(cpuString, out float cpuUsage)) {
            return cpuUsage;
         }
      }
      return 0.0f;
   }

   public static float getZabbixRamUsage () {
      // If total ram amount wasn't fetched properly, try again
      if (self._zabbixTotalRamBytes == 0) {
         updateZabbixTotalRam();

         // If fetching total ram still didn't work, return to avoid dividing by zero
         if (self._zabbixTotalRamBytes == 0) {
            return 0.0f;
         }
      }

      string memoryString = self._zabbixContext.History.GetByType(History.HistoryType.IntegerType, null, self._ramHistParams).FirstOrDefault()?.value;
      if (long.TryParse(memoryString, out long memoryUsage)) {
         return (float) (100 * memoryUsage / self._zabbixTotalRamBytes);
      }
      return 0.0f;
   }

   private static void updateZabbixTotalRam () {
      if (self._zabbixContext != null && self._zabbixContext.History != null) {
         string totalMemoryString = self._zabbixContext.History.GetByType(History.HistoryType.IntegerType, null, self._totalRamHistParams).FirstOrDefault()?.value;
         if (long.TryParse(totalMemoryString, out long totalRamBytes)) {
            self._zabbixTotalRamBytes = totalRamBytes;
         } else {
            D.error("PerformanceUtil couldn't get total ram amount from zabbix.");
         }
      }
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

   // The total ram, measured by zabbix
   private long _zabbixTotalRamBytes = 0;

   // Zabbix history fetching parameters, configured to fetch CPU data
   private Dictionary<string, object> _cpuHistParams = new Dictionary<string, object>();

   // Zabbix history fetching parameters, configured to fetch RAM data
   private Dictionary<string, object> _ramHistParams = new Dictionary<string, object>();
   
   // Zabbix history fetching parameters, configured to fetch total RAM data
   private Dictionary<string, object> _totalRamHistParams = new Dictionary<string, object>();

   // Context for fetching data from zabbix
   private Context _zabbixContext;

   #endregion
}
