using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

public class D : MonoBehaviour {
   #region Public Variables

   public enum ADMIN_LOG_TYPE { 
      None = 0,
      Combat = 1,
      Sea = 2,
      Mine = 3,
      Warp = 4,
      Ability = 5,
      Boss = 6,
      Equipment = 7,
      CustomMap = 8,
      LoggerManager = 9,
      Gamepad = 10,
      Initialization = 11,
      Crop = 12,
      Quest = 13,
      Server_AccountLogin = 14,
      Client_AccountLogin = 15,
      Treasure = 16,
      Refine = 17,
      Blueprints = 18,
      AnimationFreeze = 19,
      CombatEnd = 20,
      CancelAttack = 21,
      Pvp = 22,
      NetworkMessages = 23,
      InstanceProcess = 24,
      Tutorial = 25, 
      AbilityCast = 26,
      CombatStatus = 27,
      PvpAnnouncement = 28,
      SeaAbility = 29,
      Visit = 30,
      EnemyWaterSpawn = 31,
      PvpShop = 32,
      Redirecting = 33,
      Respawn = 34,
      POI_WARP = 35,
      Player_Menu = 36,
      Pvp_Instance = 37,
      OpenWorldPvp = 38,
      Simulation_Sea = 39,
      Warp_To_Town = 40,
      Battle_Selection = 41,
      Achievement = 42,
      DeathAnimDelay = 43,
      Performance = 44,
      Disconnection = 45,
      AreaClearing = 46,
      ShopContents = 47,
      EnemyLandSpawn = 48,
      CharacterSlots = 49,
      ShipPurchase = 50,
      NpcFriendship = 51,
      BattleMissingSelection = 52,
   }

   // Any log files older than this will be deleted
   public static int HOURS_TO_KEEP_LOG_FILES = 72;

   // Settings
   public static bool WRITE_TO_FILE = true;
   public static bool SHOW_LOGS_IN_CHAT = true;
   public static bool SHOW_COUNT = false;
   public static bool SHOW_CLASS = true;
   public static bool SHOW_FUNCTION = false;
   public static bool SHOW_TIME = true;
   public static bool LOG_TO_STRING = true;
   public static bool WRITE_DEBUG_TO_LOG_FILE = true;
   public static bool SHOW_DEBUG_IN_CHAT = false;

   // The maximum size of the server log chunks when sending it to clients
   public static int MAX_SERVER_LOG_CHUNK_SIZE = 512 * 1000;

   // The maximum number of server log chunks that will be sent to clients
   public static int MAX_SERVER_LOG_CHUNK_COUNT = 20;

   // Stores the contents of the last retrieved server log
   public static List<byte> serverLogBytes = new List<byte>();

   // Directory of the private logs
   public static string instanceProcessLogDirectory = "";
   public static string tutorialLogDirectory = "";
   public static string combatEndLogDirectory = "";
   public static string abilityCastLogDirectory = "";

   #endregion

   public void Awake () {
      D.adminLog("D.Awake...", D.ADMIN_LOG_TYPE.Initialization);

      // For now, we can't write to file in the web player
      if (Application.isMobilePlatform) {
         WRITE_TO_FILE = false;
      }

      //checkPrivateLogs();

      if (WRITE_TO_FILE) {
         // Create the "logs" directory and store the path to it
         #if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
         string logsDirectoryPath = Application.persistentDataPath + "/logs/";
         #else
         string logsDirectoryPath = Application.persistentDataPath + "\\logs\\";
         #endif
         Directory.CreateDirectory(logsDirectoryPath);

         // Clear out any old files from the directory
         deleteOldFiles(logsDirectoryPath);

         // Get the application name, so we can distinguish the Editor, Client.exe, Server.exe, Test.exe, etc.
         string appName = Util.getAppName();

         // Store the path to the log file
         _logFilePath = logsDirectoryPath + appName + " " + DateTime.Now.ToString(@"yyyy-M-d HH-mm-ss tt fffffff") + ".log";
         _serverLogFilePath = logsDirectoryPath + appName + " - SERVER LOG - " + DateTime.Now.ToString(@"yyyy-M-d HH-mm-ss tt fffffff") + ".log";

         // Log file name for AUTO_TEST
         if (Util.isAutoTest()) {
            _logFilePath = logsDirectoryPath + appName + " " + Util.getAutoTesterNumber().ToString() + ".log";
         }
         
         // Log file name for server
         #if IS_SERVER_BUILD
         foreach (string arg in System.Environment.GetCommandLineArgs()) {
            if (arg.Contains("port=")) {
               string[] split = arg.Split('=');
               string port = split[1];
               _logFilePath = logsDirectoryPath + appName + " " + port + " " + DateTime.Now.ToString(@"yyyy-M-d HH-mm-ss tt fffffff") + ".log";
            }
         }         
         #endif

         // UnityEngine.Debug.Log("Log file: " + _logFilePath);

         // Log the startup time
         debug(appName + " started.");

         Application.logMessageReceived += HandleUnityLog;

      }
      D.adminLog("D.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   private void checkPrivateLogs () {
      Global.privateLogTypesToShow.Add(ADMIN_LOG_TYPE.InstanceProcess);
      Global.privateLogTypesToShow.Add(ADMIN_LOG_TYPE.Tutorial);
      Global.privateLogTypesToShow.Add(ADMIN_LOG_TYPE.CombatEnd);
      Global.privateLogTypesToShow.Add(ADMIN_LOG_TYPE.AbilityCast);

      setupForLogType(ADMIN_LOG_TYPE.InstanceProcess);
      setupForLogType(ADMIN_LOG_TYPE.Tutorial);
      setupForLogType(ADMIN_LOG_TYPE.CombatEnd);
      setupForLogType(ADMIN_LOG_TYPE.AbilityCast);
   }

   private void setupForLogType (ADMIN_LOG_TYPE logType) {
      string fileNameAdditive = "_" +
         DateTime.UtcNow.Year + "-" +
         DateTime.UtcNow.Month + "-" +
         DateTime.UtcNow.Day + "_" +
         DateTime.UtcNow.Hour + "-" +
         DateTime.UtcNow.Minute + "-" +
         DateTime.UtcNow.Second + ".txt";

      string folderName = logType.ToString();
      string logsDirectoryPath = Application.persistentDataPath + "\\pvtLogs\\" + folderName + "\\";
      Directory.CreateDirectory(logsDirectoryPath);
      string finalName = logsDirectoryPath + logType.ToString() + fileNameAdditive;

      switch (logType) {
         case ADMIN_LOG_TYPE.InstanceProcess:
            instanceProcessLogDirectory = finalName;
            privateLogSetup(instanceProcessLogDirectory);
            break;
         case ADMIN_LOG_TYPE.Tutorial:
            tutorialLogDirectory = finalName;
            privateLogSetup(tutorialLogDirectory);
            break;
         case ADMIN_LOG_TYPE.CombatEnd:
            combatEndLogDirectory = finalName;
            privateLogSetup(combatEndLogDirectory);
            break;
         case ADMIN_LOG_TYPE.AbilityCast:
            abilityCastLogDirectory = finalName;
            privateLogSetup(abilityCastLogDirectory);
            break;
      }
   }

   private void privateLogSetup (string directory) {
      if (!File.Exists(directory)) {
         StreamWriter sr = File.CreateText(directory);
         sr.Close();
      }

      File.WriteAllText(directory, "[" + DateTime.UtcNow + "] Initialize");
   }

   void HandleUnityLog (string logString, string stackTrace, LogType type) {
      switch (type) {
         case LogType.Error:
         case LogType.Exception:
            error("Caught error: " + logString + "\n" + stackTrace);
            break;
      }
   }

   public static void openLogFile () {
      try {
         System.Diagnostics.Process.Start(_logFilePath);
      } catch (Exception e) {
         log($"Error opening log file. Message: {e.Message}");
      }
   }

   internal static void copyLogToClipboard () {
      GUIUtility.systemCopyBuffer = _logString;
   }

   public static void openServerLogFile () {
      if (Global.isLoggedInAsAdmin() && WRITE_TO_FILE) {
         if (!File.Exists(_serverLogFilePath)) {
            StreamWriter sr = File.CreateText(_serverLogFilePath);
            sr.Close();
         }

         File.WriteAllText(_serverLogFilePath, Encoding.ASCII.GetString(serverLogBytes.ToArray()));

         try {
            System.Diagnostics.Process.Start(_serverLogFilePath);
         } catch (Exception e) {
            log($"Error opening server log file. Message: {e.Message}");
         }
      }
   }

   internal static void copyServerLogToClipboard () {
      if (Global.isLoggedInAsAdmin()) {
         GUIUtility.systemCopyBuffer = Encoding.ASCII.GetString(serverLogBytes.ToArray());
      }
   }

   private static void log (string msg, string callingClass, string callingFunction, ChatInfo.Type type) {
      // We always show all messages in the internal Unity Debug output
      UnityEngine.Debug.Log(msg);

      // Check whether this specific message should write to file
      bool writeThisToFile = WRITE_TO_FILE;
      if (type == ChatInfo.Type.Debug && !WRITE_DEBUG_TO_LOG_FILE) {
         writeThisToFile = false;
      }

      // Get some strings for the line count, source class, and source function
      string countStr = SHOW_COUNT ? string.Format("{0,-3}", _lineCount) : "";
      string classStr = SHOW_CLASS ? callingClass : "";
      string functionStr = SHOW_FUNCTION ? callingFunction : "";

      // Put together the default prefix string
      string prefix = "[" + classStr + "." + functionStr + "] ";
      if (SHOW_CLASS && !SHOW_FUNCTION) {
         prefix = "[" + classStr + "] ";
      }

      // Prepend WARNING or ERROR for those log types
      prefix = (type == ChatInfo.Type.Warning) ? "[WARNING] " + prefix : prefix;
      prefix = (type == ChatInfo.Type.Error) ? "[ERROR] " + prefix : prefix;

      // WARNING: Never send other types to logger than D.warning or D.error! it will cause recursion!
      // Send messages to Logger
      if (type == ChatInfo.Type.Warning) {
         if (LoggerManager.self) LoggerManager.self.AddMessage(prefix + msg, "", LogType.Warning);
      }
      if (type == ChatInfo.Type.Error) {
         if (LoggerManager.self) LoggerManager.self.AddMessage(prefix + msg, "", LogType.Error);
      }

      // Maybe write it to a client log file
      try {
         if (writeThisToFile && !Util.isEmpty(_logFilePath)) {

            using (StreamWriter file = new StreamWriter(@"" + _logFilePath, true)) {
               string logTime = (SHOW_TIME) ? "[" + DateTime.Now + "] " : "";
               string lineToWrite = logTime + prefix + msg;

               file.WriteLine(Util.stripHTML(lineToWrite));
            }
         }
      } catch (Exception e) {
         UnityEngine.Debug.LogError($"Error writing to client log. Message: {e.Message}");
      }

      // Check whether this specific message should display in chat
      bool showThisInChat = SHOW_LOGS_IN_CHAT || type == ChatInfo.Type.Warning || type == ChatInfo.Type.Error;
      if (type == ChatInfo.Type.Debug && !SHOW_DEBUG_IN_CHAT) {
         showThisInChat = false;
      }

      // Take some precautions to avoid showing logs in chat for production builds
      if (!Application.isEditor) {
         showThisInChat = false;
      }

      // Maybe include the message in the Chat area
      if (showThisInChat && !ClientManager.isApplicationQuitting) {
         // Make sure this happens on the Unity thread and not the Background thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ChatManager chatManager = GameObject.FindObjectOfType<ChatManager>();
            if (chatManager != null) {
               ChatInfo chatInfo = new ChatInfo(0, countStr + msg, DateTime.Now, type, classStr);
               chatManager.addChatInfo(chatInfo);
            }
         });

         _lineCount++;
      }

      if (type == ChatInfo.Type.Debug) {
         if (ScreenLogger.self != null && ScreenLogger.isEnabled) {
            ScreenLogger.self.displayLogMsg(msg);
         }
      }

      // Keep track of the log as a string, for submitting bug reports
      if (LOG_TO_STRING) {
         string logTime = (SHOW_TIME) ? "[" + DateTime.Now + "] " : "";
         string lineToWrite = logTime + prefix + msg;
         _logString += Util.stripHTML(lineToWrite) + "\n";
      }
   }

   public static void log (string msg) {
      log(msg, ChatInfo.Type.Log);
   }

   public static void adminLog (string text, ADMIN_LOG_TYPE logType) {
      if (Global.privateLogTypesToShow.Contains(logType)) {
         string logTime = (SHOW_TIME) ? "[" + DateTime.Now + "] " : "";
         string lineToWrite = "\n" + logTime + text;

         switch (logType) {
            case ADMIN_LOG_TYPE.InstanceProcess:
               File.AppendAllText(instanceProcessLogDirectory, lineToWrite);
               break;
            case ADMIN_LOG_TYPE.AbilityCast:
               File.AppendAllText(abilityCastLogDirectory, lineToWrite);
               break;
            case ADMIN_LOG_TYPE.Tutorial:
               File.AppendAllText(tutorialLogDirectory, lineToWrite);
               break;
            case ADMIN_LOG_TYPE.CombatEnd:
               File.AppendAllText(combatEndLogDirectory, lineToWrite);
               break;
         }
         return;
      }

      if (!Global.logTypesToShow.Contains(logType)) {
         return;
      }

      string additiveLog = "";
      if (logType == ADMIN_LOG_TYPE.AnimationFreeze) {
         try {
            additiveLog = "[" + Mirror.NetworkTime.time.ToString("f2") + "]";
         } catch { 
         
         }
      }

      log("[" + logType.ToString().ToUpper() + "]" + additiveLog + text, ChatInfo.Type.Debug);
   }

   public static void editorLog (string text, Color color = new Color()) {
      if (UnityThreadHelper.IsMainThread) {
         editorLogProcess(text, color);
      } else {
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            editorLogProcess(text, color);
         });
      }
   }

   private static void editorLogProcess (string text, Color color) {
      try {
         if (Util.isBatch()) {
            return;
         }
      } catch (Exception e) {
         UnityEngine.Debug.LogWarning("Batch test failed with exception: " + e.ToString());
      }

      #if UNITY_EDITOR
      UnityEngine.Debug.Log(string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte) (color.r * 255f), (byte) (color.g * 255f), (byte) (color.b * 255f), text));
      #endif
   }

   // Log something if it hasn't been previously logged
   public static void logOnce (string msg) {
      // Get call stack
      StackTrace stackTrace = new StackTrace(true);

      // Get calling method name
      string callingFunction = stackTrace.GetFrame(1).GetMethod().Name;
      string callingClass = stackTrace.GetFrame(1).GetMethod().DeclaringType.Name;
      int callLineNum = stackTrace.GetFrame(1).GetFileLineNumber();

      // See if we already logged this
      if (_logSources.ContainsKey(callingClass + callingFunction + callLineNum)) {
         return;
      } else {
         _logSources.Add(callingClass + callingFunction + callLineNum, true);
      }

      // Do the actual logging
      log(msg, callingClass, callingFunction, ChatInfo.Type.Log);
   }

   public static void debug (string msg) {
      log(msg, ChatInfo.Type.Debug);
   }

   public static void warning (string msg) {
      log(msg, ChatInfo.Type.Warning);
   }

   public static void error (string msg) {
      log(msg, ChatInfo.Type.Error);
   }

   private static void log (string msg, ChatInfo.Type messageType) {
      // Get call stack
      StackTrace stackTrace = new StackTrace();

      // Get calling method name
      string callingFunction = stackTrace.GetFrame(2).GetMethod().Name;
      string callingClass = stackTrace.GetFrame(2).GetMethod().DeclaringType.Name;

      // Do the actual logging
      log(msg, callingClass, callingFunction, messageType);
   }

   public static string getLogString () {
      return _logString;
   }

   protected static void deleteOldFiles (string directoryPath) {
      string[] files = Directory.GetFiles(directoryPath);

      foreach (string file in files) {
         FileInfo fi = new FileInfo(file);
         if (fi.CreationTime < DateTime.Now.AddHours(-1 * HOURS_TO_KEEP_LOG_FILES)) {
            fi.Delete();
         }
      }
   }

   public static byte[] getLogForSendingToClient () {
      // Transform into a byte array to avoid the 'buffer is too small' error
      byte[] data = Encoding.ASCII.GetBytes(getLogString());

      // Remove the beginning of the log if it is too large
      int maxServerLogSize = D.MAX_SERVER_LOG_CHUNK_SIZE * D.MAX_SERVER_LOG_CHUNK_COUNT;
      if (data.Length > maxServerLogSize) {
         data = data.RangeSubset(data.Length - maxServerLogSize, maxServerLogSize);
      }

      return data;
   }

   #region Private Variables

   // Stores the caller class and line number, so we know if something has been previously logged
   protected static Dictionary<string, bool> _logSources = new Dictionary<string, bool>();

   // Keep track of how many lines of output we've written (helpful for the chat display)
   protected static int _lineCount = 1;

   // Stores the name of the log file we created at initialization time
   protected static string _logFilePath = "";

   // Stores the contents of the current log in a string so that Web Players can submit bug reports
   protected static string _logString = "";

   // Stores the name of the server log file on clients
   protected static string _serverLogFilePath = "";

   #endregion
}