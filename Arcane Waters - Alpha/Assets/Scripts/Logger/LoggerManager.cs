using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MiniJSON;

public class LoggerManager : MonoBehaviour {
   #region Public Variables
   public static LoggerManager self;
   public bool catchEditorLogs;
   // message patterns to include for regular logs LogType.Log
   public string[] includePatterns;
   // message patterns to exclude from logging
   public string[] excludePatterns;
   #endregion

   void Awake () {
      // WARNING: Never use D.warning or D.error! It will cause recursion!
      D.adminLog("LoggerManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      self = this;
      Init();
      D.adminLog("LoggerManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
      // AddMessage("message text test", "stack trace 2", LogType.Error);
   }
   
   void OnEnable()  {
      Application.logMessageReceived += AddMessage;
   }

   void OnDisable() {
      Application.logMessageReceived -= AddMessage;
   }

   // void Update () {
   //    // Test only
   //    AddMessage("message text test" + DateTime.Now.Millisecond, "stack trace", LogType.Error);
   // }
   
   void Init() {
      // Reading deploymentConfig data
      var deploymentConfigAsset = Resources.Load<TextAsset>("config");
      var deploymentConfig = Json.Deserialize(deploymentConfigAsset.text) as Dictionary<string,object>;
      deploymentId = "0";
      buildId = "0";
      
      if (deploymentConfig != null && deploymentConfig.ContainsKey("deploymentId")) {
         deploymentId = deploymentConfig["deploymentId"].ToString();
      }
      if (deploymentConfig != null && deploymentConfig.ContainsKey("buildId")) {
         buildId = deploymentConfig["buildId"].ToString();
      }

      // Init
      messagesDirPath = $"{Application.persistentDataPath}/{"messages"}";
      messagesToSend = "";
      messageIdFilePath = new Dictionary<string, string>();

      WWWForm form = new WWWForm();
      headers = form.headers;
      headers["Content-Type"] = "application/json";
      
      InitMessagesFolder();
      
      // Starting send messages in the thread
      UnityThreading.ActionThread sendDataThread = UnityThreadHelper.CreateThread(SendMessagesLoop);
   }
   
   void InitMessagesFolder () {
      if (Directory.Exists(messagesDirPath)) return;
      // Debug.Log ("Creating messages folder: " + messagesDirPath);
      Directory.CreateDirectory(messagesDirPath);
   }   

   
   #region Caching messages logic
   public void AddMessage(string logString, string stackTrace, LogType type) {
      // Skip default log messages if no include patters found
      if (type == LogType.Log && !IsPatternPresent(includePatterns, logString)) return;
      
      // Don't catch editor logs if not enabled
      if (Application.isEditor && !catchEditorLogs) return;
      
      // Skip all LoggerManager messages to avoid recursion
      if (stackTrace.Contains("LoggerManager.cs")) return;
      
      // Skip messages with exclude patterns
      if (IsPatternPresent(excludePatterns, logString)) return;

      try {
         var messageData = new Dictionary<string, string> {
            { "deploymentId", deploymentId }, { "buildId", buildId },
            { "message", logString },
            { "stackTrace", stackTrace },
            { "messageType", type.ToString() },
            { "systemInfo", SystemInfo.operatingSystem },
            { "host", Dns.GetHostName() },
            { "datetime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
            { "milliseconds", DateTime.UtcNow.Millisecond.ToString() },
            { "version", VERSION }
         };

         #if IS_SERVER_BUILD
         messageData.Add("appType" , "server");
         #else
         messageData.Add("appType", "client");
         #endif
         
         #if IS_MASTER_TOOL
         messageData.Add("appType" , "mastertool");
         #endif

         #if NUBIS
         messageData.Add("appType" , "nubis");
         #endif

         #if ARCANE_WATERS_WEBSITE
         messageData.Add("appType" , "website");
         #endif
         
         // Add to the cache
         CacheMessage(messageData);
      }
      catch (Exception ex)  {
         // WARNING: Never use D.warning or D.error! It will cause recursion!
         // D.debug(ex.Message);
         // D.debug(ex.StackTrace);
      }
   }

   void CacheMessage(IReadOnlyDictionary<string, string> messageData) {
      string messageDataString = Json.Serialize(messageData);
      // Debug.Log("StoreEventData: " + messageDataString);

      string messageDir = GetMessageDir(DateTime.Parse(messageData["datetime"]));
      if (!Directory.Exists(messageDir)) Directory.CreateDirectory(messageDir);

      string messageFileName = CalculateMD5Hash(messageDataString);
      if (File.Exists(messageFileName)) return;
      File.WriteAllText(messageDir + messageFileName, messageDataString);
   }
   #endregion


   #region Processing cached messages
   void SendMessagesLoop (UnityThreading.ActionThread thread) {
      while (!thread.ShouldStop) {
         SendMessages();
         Thread.Sleep(SEND_INTERVAL * 1000);
      }
   }

   void SendMessages() {
      try {
         ReadCachedMessages();
         if (messagesToSend != "" && messagesToSend != "{}") {
            var sendMessagesTask = UnityThreadHelper.UnityDispatcher.Dispatch(() => { SendMessagesAsync(); });
            sendMessagesTask.Wait();

            // Process response
            if (!string.IsNullOrEmpty(lastResponse)) {
               var responseDict = Json.Deserialize(lastResponse) as Dictionary<string, object>;

               // Checking each key and removing cached data
               foreach (var id in messageIdFilePath.Keys) {
                  if (responseDict.ContainsKey(id) && responseDict[id].ToString() != "0") {
                     string messageFileName = messageIdFilePath[id] + "/" + id;
                     try {
                        File.Delete(messageFileName);
                     } catch {
                        Debug.Log("Error deleting cached log file: " + messageFileName);
                     }
                  }
               }
            }
         }
      } catch (SystemException obj) {
         if (Util.isCloudBuild()) {
            Debug.Log("Some errors on sending messages: " + obj.Message);
         }
      }
   }

   void ReadCachedMessages() {
      Hashtable data = new Hashtable();
      messagesToSend = "";
      messageIdFilePath.Clear();

      // Days folders "messages/yyyy-MM-dd"
      string[] messagesDailyDirs = Directory.GetDirectories(messagesDirPath);
      for (int d=0; d<messagesDailyDirs.Length; d++) {

         var messagesDailyDir = messagesDailyDirs[d];
         DirectoryInfo messagesDailyDirInfo = new DirectoryInfo(messagesDailyDir);

         // Minutes folders "messages/yyyy-MM-dd/HH-mm"
         string[] messagesMinuteDirs = Directory.GetDirectories(messagesDailyDir);
         // Remove empty directory
         if (messagesMinuteDirs.Length == 0) {
            Directory.Delete(messagesDailyDir);
            continue;
         }
         
         // Cycle minutes folders
         for (int dm = 0; dm < messagesMinuteDirs.Length; dm++) {

            // Get messages
            string[] messagesFiles = Directory.GetFiles(messagesMinuteDirs[dm]);
            // Remove empty directory
            if (messagesFiles.Length == 0) {
               Directory.Delete(messagesMinuteDirs[dm]); 
               continue;
            }
            
            // Limit count to MESSAGES_LIMIT
            int messagesCount = messagesFiles.Length;
            if (messagesCount > MESSAGES_LIMIT - data.Keys.Count) {
               messagesCount = MESSAGES_LIMIT - data.Keys.Count;
            }
            
            // Add messages
            for (int f=0; f<messagesCount; f++) {
               FileInfo file = new FileInfo(messagesFiles[f]);
               string id = file.Name;
               data[id] = File.ReadAllText(messagesFiles[f]);
               messageIdFilePath.Add(id, messagesMinuteDirs[dm]);
            }

            // Limit to send MESSAGES_LIMIT statistic at once
            if (data.Keys.Count >= MESSAGES_LIMIT) {
               // Add check sums
               data["sum2"] = CalculateMD5Hash(string.Join("",messageIdFilePath.Keys));
               data["sum"] = CalculateMD5Hash(API_SALT + data["sum2"]);
               messagesToSend = Json.Serialize(data);
               return;
            }
         }
      }

      // Add sums if messages count less than MESSAGES_LIMIT
      if (data.Keys.Count > 0 ) {
         // Add check sums
         data["sum2"] = CalculateMD5Hash(string.Join("",messageIdFilePath.Keys));
         data["sum"] = CalculateMD5Hash(API_SALT + data["sum2"]);
         messagesToSend = Json.Serialize(data);
      }
   }
   #endregion

   
   #region Unity web request functions
   void SendMessagesAsync () {
      byte[] bytes = Encoding.UTF8.GetBytes(messagesToSend);
      WWW www = new WWW(API_MESSAGES_URL, bytes, headers);
      StartCoroutine(WaitForRequest(www));
   }

   IEnumerator WaitForRequest(WWW www) {
      yield return www;

      if (www.error == null) {
         // Debug.Log("WWW Ok!: " + www.text);
         lastResponse = www.text;
      }
      else {
         lastResponse = "";
         Debug.Log("Logger manager - send data api error: " + www.error);
      }
   }
   #endregion

   
   #region Utils functions
   bool IsPatternPresent(string[] patterns, string message) {
      foreach (string pattern in patterns) {
         Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
         MatchCollection matches = regex.Matches(message);
         if (matches.Count > 0) return true;
      }
      return false;
   }

   string GetMessageDir(DateTime datetime) {
      return messagesDirPath + "/" + 
             datetime.ToString("yyyy-MM-dd") + "/" +
             datetime.ToString("HH-mm") + "/"
         ;
   }   

   string CalculateMD5Hash (string input) {
      // step 1, calculate MD5 hash from input
      MD5 md5 = MD5.Create();
      byte[] inputBytes = Encoding.ASCII.GetBytes(input);
      byte[] hash = md5.ComputeHash(inputBytes);

      // step 2, convert byte array to hex string
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hash.Length; i++) {
         sb.Append(hash[i].ToString("X2"));
      }
      
      return sb.ToString();
   }
   #endregion

   
   #region Private Variables
   // Log manager parameters
   const string VERSION = "4";
   // const string SERVER_URL = "https://localhost:5001/";
   const string SERVER_URL = "https://tools.arcanewaters.com/";
   const string API_MESSAGES_URL = SERVER_URL + "api/logger/add";
   const string API_SALT = "as5HU5_YhkPaRWpr+dxmBMq#eTWAx98fu8XJF-b2Rg@AWtgF*8EqaEvLedMexk";
   const int SEND_INTERVAL = 20; // seconds 
   const int MESSAGES_LIMIT = 1000; // messages limit to send
   
   // Send data
   string messagesDirPath;
   string messagesToSend;
   Dictionary <string, string> messageIdFilePath;

   // Deployment config data
   string deploymentId, buildId;
   
   // Unity web request 
   Dictionary<string, string> headers;
   string lastResponse;
   #endregion
}
