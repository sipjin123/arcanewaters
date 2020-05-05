using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System;

namespace ServerCommunicationHandlerv2 {
   public class ServerDataWritter : MonoBehaviour {
      #region Public Variables

      // Location where the text files are written
      public string path = "";

      // Self
      public static ServerDataWritter self;

      #endregion

      private void Awake () {
         self = this;
         path = Application.persistentDataPath + "/ServerData/";
      }

      public void updateServerData (ServerSqlData serverData) {
         string content = serializedServerData(serverData);
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

      public ServerSqlData getServerPing (ServerSqlData serverData) {
         // Fetches the last time the text file was modified
         DateTime lastModified = File.GetLastWriteTimeUtc(path + "/" + serverData.port + ".txt");
         serverData.lastPingTime = lastModified;

         return serverData;
      }

      public List<ServerSqlData> getServerContent (List<ServerSqlData> existingServerList) {
         List<ServerSqlData> newServerContentList = new List<ServerSqlData>();
         List<ServerSqlData> copiedExistingServerList = copyServerSqlContent(existingServerList);

         foreach (ServerSqlData existingServerData in copiedExistingServerList) {
            StreamReader reader = new StreamReader(path + "/" + existingServerData.port + ".txt");
            string rawXmlData = reader.ReadToEnd();
            reader.Close();

            ServerSqlData newServerData = Util.xmlLoad<ServerSqlData>(rawXmlData);
            newServerData.lastPingTime = existingServerData.lastPingTime;
            newServerData.latestUpdate = existingServerData.latestUpdate;

            newServerContentList.Add(newServerData);
         }

         return newServerContentList;
      }

      private List<ServerSqlData> copyServerSqlContent (List<ServerSqlData> existingServerDataList) {
         List<ServerSqlData> newSqlDataList = new List<ServerSqlData>();
         foreach (ServerSqlData existingServerData in existingServerDataList) {
            ServerSqlData newServerData = ServerSqlData.copyData(existingServerData);
            newSqlDataList.Add(newServerData);
         }

         return newSqlDataList;
      }

      public List<ServerSqlData> getServersUpdateTime (ServerSqlData serverData) {
         List<ServerSqlData> newSqlDataList = new List<ServerSqlData>();

         DirectoryInfo info = new DirectoryInfo(path);
         FileInfo[] fileInfo = info.GetFiles();
         foreach (FileInfo file in fileInfo) {
            string fileName = file.Name.Replace(".txt", "");
            DateTime lastModified = File.GetLastWriteTimeUtc(path + "/" + file.Name);

            ServerSqlData newServerData = new ServerSqlData();
            newServerData.port = int.Parse(fileName);
            newServerData.deviceName = serverData.deviceName;
            newServerData.ip = serverData.ip;
            newServerData.latestUpdate = lastModified;

            newSqlDataList.Add(newServerData);
         }
         return newSqlDataList;
      }

      private string serializedServerData (ServerSqlData data) {
         XmlSerializer ser = new XmlSerializer(data.GetType());
         StringBuilder serverDataXml = new StringBuilder();
         using (var writer = XmlWriter.Create(serverDataXml)) {
            ser.Serialize(writer, data);
         }

         return serverDataXml.ToString();
      }

      #region Private Variables

      #endregion
   }
}