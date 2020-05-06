using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System;

namespace ServerCommunicationHandlerv2 {
   public class ServerDataWriter : MonoBehaviour {
      #region Public Variables

      // Location where the text files are written
      public string path = "";

      // Self
      public static ServerDataWriter self;

      #endregion

      private void Awake () {
         self = this;
         path = Application.persistentDataPath + "/ServerData/";
      }

      public void clearExistingData (ServerData serverData) {
         // Updates the server data using the initialized local server which contains empty content
         updateServerData(serverData);
      }

      public void updateServerData (ServerData serverData) {
         string content = JsonUtility.ToJson(serverData);
         string fileName = serverData.port.ToString() + ".txt";

         // Make sure directory exists
         if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
         }

         // Write data to file
         if (!File.Exists(path + fileName)) {
            // Create file if non existent
            File.Create(path + fileName).Close();
            File.WriteAllText(path + fileName, content);
         } else {
            File.WriteAllText(path + fileName, content);
         }
      }

      public ServerData getLastServerUpdate (ServerData serverData) {
         // Fetches the last time the text file was modified
         DateTime lastModified = File.GetLastWriteTimeUtc(path + "/" + serverData.port + ".txt");
         serverData.latestUpdateTime = lastModified;

         return serverData;
      }

      public List<ServerData> getServerContent (List<ServerData> existingServerList) {
         List<ServerData> newServerContentList = new List<ServerData>();
         List<ServerData> copiedExistingServerList = copyServerContent(existingServerList);

         foreach (ServerData existingServerData in copiedExistingServerList) {
            StreamReader reader = new StreamReader(path + "/" + existingServerData.port + ".txt");
            string rawTextData = reader.ReadToEnd();
            reader.Close();

            ServerData newServerData = JsonUtility.FromJson<ServerData>(rawTextData);
            newServerData.latestUpdateTime = existingServerData.latestUpdateTime;

            newServerContentList.Add(newServerData);
         }

         return newServerContentList;
      }

      private List<ServerData> copyServerContent (List<ServerData> existingServerDataList) {
         List<ServerData> newDataList = new List<ServerData>();
         foreach (ServerData existingServerData in existingServerDataList) {
            ServerData newServerData = ServerData.copyData(existingServerData);
            newDataList.Add(newServerData);
         }

         return newDataList;
      }

      public List<ServerData> getServers () {
         List<ServerData> newDataList = new List<ServerData>();

         DirectoryInfo info = new DirectoryInfo(path);
         FileInfo[] fileInfo = info.GetFiles();
         foreach (FileInfo file in fileInfo) {
            string fileName = file.Name.Replace(".txt", "");
            DateTime lastModified = File.GetLastWriteTimeUtc(path + "/" + file.Name);

            ServerData newServerData = new ServerData();
            newServerData.port = int.Parse(fileName);
            newServerData.latestUpdateTime = lastModified;

            newDataList.Add(newServerData);
         }
         return newDataList;
      }

      #region Private Variables

      #endregion
   }
}