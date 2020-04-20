using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

public class D : MonoBehaviour {
   #region Public Variables

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

   #endregion

   public void Awake () {
      // For now, we can't write to file in the web player
      if (Application.isMobilePlatform) {
         WRITE_TO_FILE = false;
      }

      if (WRITE_TO_FILE) {
         // Create the "logs" directory and store the path to it
         string logsDirectoryPath = Application.persistentDataPath + "\\logs\\";
         Directory.CreateDirectory(logsDirectoryPath);

         // Clear out any old files from the directory
         deleteOldFiles(logsDirectoryPath);

         // Get the application name, so we can distinguish the Editor, Client.exe, Server.exe, Test.exe, etc.
         string appName = Util.getAppName();

         // Store the path to the log file
         _logFilePath = logsDirectoryPath + appName + " " + DateTime.Now.ToString(@"yyyy-M-d HH-mm-ss tt fffffff") + ".log";

         // UnityEngine.Debug.Log("Log file: " + _logFilePath);

         // Log the startup time
         debug(appName + " started.");
      }
   }

   void OnEnable () {
      Application.logMessageReceived += HandleUnityLog;
   }

   void OnDisable () {
      Application.logMessageReceived -= HandleUnityLog;
   }

   void HandleUnityLog (string logString, string stackTrace, LogType type) {
      switch (type) {
         case LogType.Error:
         case LogType.Exception:
            D.warning("Caught error: " + logString + "\n" + stackTrace);
            break;
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

      // Maybe write it to a client log file
      if (writeThisToFile) {
         using (StreamWriter file = new StreamWriter(@"" + _logFilePath, true)) {
            string logTime = (SHOW_TIME) ? "[" + DateTime.Now + "] " : "";
            string lineToWrite = logTime + prefix + msg;

            file.WriteLine(Util.stripHTML(lineToWrite));
         }
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

   public static void editorLog (string text, Color color = new Color()) {
      if (Util.isBatch()) {
         return;
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

   #region Private Variables

   // Stores the caller class and line number, so we know if something has been previously logged
   protected static Dictionary<string, bool> _logSources = new Dictionary<string, bool>();

   // Keep track of how many lines of output we've written (helpful for the chat display)
   protected static int _lineCount = 1;

   // Stores the name of the log file we created at initialization time
   protected static string _logFilePath = "";

   // Stores the contents of the current log in a string so that Web Players can submit bug reports
   protected static string _logString = "";

   #endregion
}
