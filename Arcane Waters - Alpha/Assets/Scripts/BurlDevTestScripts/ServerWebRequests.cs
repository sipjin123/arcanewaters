using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using System;

public class ServerWebRequests : MonoBehaviour
{
   #region Public Variables

   // The server directory
   public static string WEB_DIRECTORY = "http://localhost/";
   public static string LAST_UPDATED_GET = "server_getUpdateTime.php";
   
   // The current port
   public int port;

   // The list of server data fetched from the database
   public List<ServerSqlData> serverDataList = new List<ServerSqlData>();

   // Our current server data
   public ServerSqlData ourServerData = new ServerSqlData();

   #endregion

   private void Update () {
      if (Input.GetKeyDown(KeyCode.Alpha1)) {
         StartCoroutine(CO_FetchServersUpdateTime());
      }
   }

   private IEnumerator CO_FetchServersUpdateTime () {
      string myDeviceName = SystemInfo.deviceName;
      D.editorLog("Server is preparing: " + myDeviceName, Color.green);
      yield return new WaitForSeconds(.5f);

      UnityWebRequest www = UnityWebRequest.Get(WEB_DIRECTORY + LAST_UPDATED_GET);
      yield return www.SendWebRequest();
      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         string rawData = www.downloadHandler.text;
         rawData = rawData.Replace("\n", "");

         // Split each entry data
         string splitter = "[next]";
         string[] xmlGroup = rawData.Split(new string[] { splitter }, StringSplitOptions.None);
         foreach (string subgroup in xmlGroup) {
            string subSplitter = "[space]";
            string[] newGroup = subgroup.Split(new string[] { subSplitter }, StringSplitOptions.None);
            if (newGroup.Length == 4) {
               string port = newGroup[0];
               string ip = newGroup[1];
               string deviceName = newGroup[2];
               string updateTime = newGroup[3];

               ServerSqlData newSqlData = new ServerSqlData {
                  deviceName = deviceName,
                  ip = ip,
                  port = int.Parse(port),
                  latestUpdate = DateTime.Parse(updateTime)
               };

               if (deviceName == myDeviceName) {
                  D.editorLog("Here is the current data of OUR server: " + port + " - " + ip + " - " + deviceName + " - " + updateTime, Color.blue);
                  ourServerData = newSqlData;
               } else {
                  D.editorLog("This server is not our, adding to list: " + port + " - " + ip + " - " + deviceName + " - " + updateTime, Color.blue);
                  string currentKey = newSqlData.port + "_" + newSqlData.deviceName;

                  ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port);
                  if (existingSqlData != null) {
                     D.editorLog("This server was cached already: " + newSqlData.port + "_" + newSqlData.deviceName +" === "+ newSqlData.latestUpdate, Color.cyan);
                     if (existingSqlData.latestUpdate < newSqlData.latestUpdate) {
                        D.editorLog("The server: " + newSqlData.deviceName + " has updated its data from: " + existingSqlData.latestUpdate + " to " + newSqlData.latestUpdate, Color.yellow);
                     } else {
                        D.editorLog("The server: " + newSqlData.deviceName + " has the same data: " + existingSqlData.latestUpdate, Color.yellow);
                     }
                  } else {
                     D.editorLog("This server is New, Cachign now: " + newSqlData.port + "_" + newSqlData.deviceName + " === " + DateTime.Parse(PlayerPrefs.GetString(currentKey, DateTime.UtcNow.ToString())), Color.cyan);
                     serverDataList.Add(newSqlData);
                  }
               }
            }
         }
      }
   }

   private IEnumerator CO_FetchServersContent (ServerSqlData serverData) {
      yield return new WaitForSeconds(.5f);
      // TODO: Fetch server content here
   }

   #region Private Variables

   #endregion
}

[Serializable]
public class ServerSqlData {
   // The port of the server
   public int port;

   // The ip of the server
   public string ip;

   // The device name
   public string deviceName;

   // The latest update time of the server data
   public DateTime latestUpdate;

   // The list of active voyage instances on this server
   public List<Voyage> voyageList;

   // Keeps track of the users connected to this server
   public HashSet<int> connectedUserIds;

   // A listing of open area types on this server
   public string[] openAreas = new string[0];
}