using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

[Serializable]
public class ServerSqlData {
   // The device name
   public string deviceName;

   // The port of the server
   public int port;

   // The ip of the server
   public string ip;

   // The latest update time of the server data
   public DateTime latestUpdate;

   // The list of active voyage instances on this server
   public List<Voyage> voyageList;

   // Keeps track of the users connected to this server
   public List<int> connectedUserIds;

   // A listing of open area types on this server
   public string[] openAreas = new string[0];

   public string getRawVoyage () {
      XmlSerializer ser = new XmlSerializer(voyageList.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, voyageList);
      }

      return sb.ToString();
   }

   public string getOpenAreas () {
      XmlSerializer ser = new XmlSerializer(openAreas.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, openAreas);
      }

      return sb.ToString();
   }

   public string getConnectedUsers () {
      XmlSerializer ser = new XmlSerializer(connectedUserIds.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, connectedUserIds);
      }

      return sb.ToString();
   }

   public ServerSqlData () {
   }

   #if IS_SERVER_BUILD

   public ServerSqlData (MySql.Data.MySqlClient.MySqlDataReader dataReader) {
      this.port = DataUtil.getInt(dataReader, "svrPort");
      this.ip = DataUtil.getString(dataReader, "srvAddress");
      this.deviceName = DataUtil.getString(dataReader, "svrDeviceName");
      this.latestUpdate = DateTime.Parse(DataUtil.getString(dataReader, "updateTime"));
   }

   public void updateServerData (MySql.Data.MySqlClient.MySqlDataReader dataReader, ServerSqlData currentServerData) {
      string[] openAreas = Util.xmlLoad<string[]>(DataUtil.getString(dataReader, "openAreas"));
      List<Voyage> voyages = Util.xmlLoad<List<Voyage>>(DataUtil.getString(dataReader, "voyages"));
      List<int> connectedUsers = Util.xmlLoad<List<int>>(DataUtil.getString(dataReader, "connectedUserIds"));

      this.openAreas = openAreas;
      this.voyageList = voyages;
      this.connectedUserIds = connectedUsers;

      this.port = currentServerData.port;
      this.ip = currentServerData.ip;
      this.deviceName = currentServerData.deviceName;
      this.latestUpdate = currentServerData.latestUpdate;
   }

   #endif
}