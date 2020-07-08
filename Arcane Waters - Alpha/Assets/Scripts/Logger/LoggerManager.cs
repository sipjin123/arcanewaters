using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Text;
using MiniJSON;

public class LoggerManager : MonoBehaviour {
   #region Public Variables
   public static LoggerManager self;
   #endregion

   void Awake () {
      self = this;
      Init();
      // AddMessage("message text", "stack trace", LogType.Error);
   }
   
   void OnEnable()  {
      Application.logMessageReceived += AddMessage;
   }

   void OnDisable() {
      Application.logMessageReceived -= AddMessage;
   }
   
   void Init() {
      messagesDirPath = $"{Application.persistentDataPath}/{"messages"}";
      currentSendDelay = 0;
      
      messagesAreSending = false;
      messagesToSend = "";
      messagesToSendDate = "";
      messagesIds = new List<string>();

      InitMessagesFolder();
      SendMessages();
   }

   void Update() {
      currentSendDelay += Time.deltaTime;
      if (currentSendDelay > SEND_INTERVAL) {
         currentSendDelay = 0;
         SendMessages();
      }
   }
   
   void InitMessagesFolder () {
      if (Directory.Exists(messagesDirPath)) return;
      Debug.Log ("Creating messages folder: " + messagesDirPath);
      Directory.CreateDirectory(messagesDirPath);
   }

   public void AddMessage(string logString, string stackTrace, LogType type) {
      if (type == LogType.Log) return; // skip default log messages
      // TODO - skip all LoggerManager messages to avoid recursion
      
      Dictionary <string, string> messageData = new Dictionary<string, string>();

      messageData["message"] = logString;
      messageData["stackTrace"] = stackTrace;
      messageData["messageType"] = type.ToString();

      messageData["messageHash"] = CalculateMD5Hash(logString);
      messageData["systemInfo"] = SystemInfo.operatingSystem;
      messageData["host"] = Dns.GetHostName();

      messageData["datetime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
      messageData["milliseconds"] = DateTime.UtcNow.Millisecond.ToString();
      messageData["version"] = VERSION;

      // add to event queue
      StoreMessage(messageData);
   }

   void StoreMessage(Dictionary <string, string> messageData) {
      string messageDataString = Json.Serialize(messageData);
      Debug.Log("StoreEventData: " + messageDataString);

      string messageDir = messagesDirPath + "/" + DateTime.Parse(messageData["datetime"]).ToString("yyyy-MM-dd") + "/";

      if (!Directory.Exists(messageDir)) Directory.CreateDirectory(messageDir);

      string messageFileName = CalculateMD5Hash(messageDataString);
      File.WriteAllText(messageDir + messageFileName, messageDataString);
   }

   void SendMessages () {
      if (messagesAreSending) return;

      Debug.Log("Time to send messages");

      try {
         messagesAreSending = true;
         GetMessagesData();
         if (messagesToSend != "" && messagesToSend != "{}")
            SendMessagesAsync();
         else
            messagesAreSending = false;
      }
      catch (SystemException obj) {
         Debug.Log("Some errors on sending messages: " + obj.Message);
         messagesAreSending = false;
      }
   }

   void GetMessagesData () {
      messagesToSend = "";
      messagesToSendDate = "";

      //  check all messages folders
      string[] messagesDirs = Directory.GetDirectories(messagesDirPath);
      for (int d=0; d<messagesDirs.Length; d++) {

         messagesIds.Clear();

         DirectoryInfo messagesDailyDir = new DirectoryInfo(messagesDirs[d]);
         messagesToSendDate = messagesDailyDir.Name;

         Hashtable data = new Hashtable();

         // get messages files lists, check and process
         string[] messagesFiles = Directory.GetFiles(messagesDirs[d]);
         for (int f=0; f<messagesFiles.Length; f++) {
            FileInfo file = new FileInfo(messagesFiles[f]);
            string id = file.Name;
            data[id] = File.ReadAllText(messagesFiles[f]);
            messagesIds.Add(id);
         }

         messagesToSend = Json.Serialize(data);
         if (messagesToSend != "" && messagesToSend != "{}") return; // limit to send one day statistic at once
         
         // TODO: remove empty directory
      }
   }

   void SendMessagesAsync () {
      // TODO: add hash with salt
      
      WWWForm form = new WWWForm();
      Dictionary<string,string> headers = form.headers;
      headers["Content-Type"] = "application/json";
      byte[] bytes = Encoding.UTF8.GetBytes(messagesToSend);
      
      WWW www = new WWW(API_MESSAGES_URL, bytes, headers);
      StartCoroutine(WaitForRequest(www));
   }

   IEnumerator WaitForRequest(WWW www) {
      yield return www;

      // check for errors
      if (www.error == null) {
         // Debug.Log("WWW Ok!: " + www.text);
         
         Dictionary<string, object> response = Json.Deserialize(www.text) as Dictionary<string, object>;
         for (int i=0; i<messagesIds.Count; i++) {
            if (response != null && response.ContainsKey(messagesIds[i]) && response[messagesIds[i]].ToString() != "0") {
               string messageFileName = messagesDirPath + "/" + messagesToSendDate + "/" + messagesIds[i];
               try {
                  File.Delete(messageFileName);
               }
               catch {
                  Debug.Log("Error deleting: " + messageFileName);
               }
            }
         }
      }
      else {
         Debug.Log("WWW Error: "+ www.error);
      }

      messagesAreSending = false;
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
   
   #region Private Variables
   const string VERSION = "1";
   // const string SERVER_URL = "http://127.0.0.1:3000/";
   const string SERVER_URL = "http://tools.arcanewaters.com/";
   const string API_MESSAGES_URL = SERVER_URL + "api/logger/add";
   const int SEND_INTERVAL = 20; // seconds

   string messagesDirPath;
   float currentSendDelay = 0;
   
   bool messagesAreSending;
   string messagesToSend;
   string messagesToSendDate;
   List <string> messagesIds;
   
   #endregion
}
