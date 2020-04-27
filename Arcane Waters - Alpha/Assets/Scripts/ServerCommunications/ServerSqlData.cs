using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using UnityEngine;

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

   // The last time the server sent a ping to the database
   public DateTime lastPingTime;

   // The list of active voyage instances on this server
   public List<Voyage> voyageList = new List<Voyage>();

   // Keeps track of the users connected to this server
   public List<int> connectedUserIds = new List<int>();

   // Keeps track of the users claimed by this server
   public List<int> claimedUserIds = new List<int>();

   // A listing of open area types on this server
   public List<string> openAreas = new List<string>();

   public static ServerSqlData copyData (ServerSqlData sourceData) {
      ServerSqlData newData = new ServerSqlData();
      newData.deviceName = sourceData.deviceName;
      newData.port = sourceData.port;
      newData.ip = sourceData.ip;

      newData.latestUpdate = sourceData.latestUpdate;
      newData.lastPingTime = sourceData.lastPingTime;

      newData.voyageList = sourceData.voyageList;
      newData.connectedUserIds = sourceData.connectedUserIds;
      newData.claimedUserIds = sourceData.claimedUserIds;
      newData.openAreas = sourceData.openAreas;

      return newData;
   }

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

   public static string getRawPendingVoyageCreation (List<PendingVoyageCreation> voyageCreationList) {
      XmlSerializer ser = new XmlSerializer(voyageCreationList.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, voyageCreationList);
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

   public string getClaimedUsers () {
      XmlSerializer ser = new XmlSerializer(claimedUserIds.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, claimedUserIds);
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
      List<int> claimedUserIds = Util.xmlLoad<List<int>>(DataUtil.getString(dataReader, "claimedUserIds"));
     
      this.latestUpdate = DateTime.Parse(DataUtil.getString(dataReader, "updateTime"));

      this.openAreas = new List<string>(openAreas);
      this.voyageList = voyages;
      this.connectedUserIds = connectedUsers;
      this.claimedUserIds = claimedUserIds;

      this.port = currentServerData.port;
      this.ip = currentServerData.ip;
      this.deviceName = currentServerData.deviceName;
   }

   #endif
}

[Serializable]
public class VoyageInviteData {
   // The voyage entry id in the database
   public int voyageXmlId;

   // The id of the player who sent the invite
   public int inviterId;

   // Name of the player inviting 
   public string inviterName;

   // The id of the player to receive the invite
   public int inviteeId;

   // The voyage group id of the invite
   public int voyageGroupId;

   // The status of the invite
   public InviteStatus inviteStatus;

   // The date this invite was created
   public DateTime creationTime;

   // The server info
   public string serverName;
   public int serverPort;
   public string serverIp;

   public VoyageInviteData (Server targetServer, int inviterId, string inviterName, int inviteeId, int voyageGroupId, InviteStatus inviteStatus, DateTime creationTime) {
      this.serverName = targetServer.deviceName;
      this.serverPort = targetServer.port;
      this.serverIp = targetServer.ipAddress;

      this.inviterId = inviterId;
      this.inviterName = inviterName;
      this.inviteeId = inviteeId;
      this.voyageGroupId = voyageGroupId;
      this.inviteStatus = inviteStatus;
      this.creationTime = creationTime;
   }
   
   #if IS_SERVER_BUILD

   public VoyageInviteData (MySql.Data.MySqlClient.MySqlDataReader dataReader) {
      this.voyageXmlId = DataUtil.getInt(dataReader, "id");
      this.inviterId = DataUtil.getInt(dataReader, "inviterId");
      this.inviterName = DataUtil.getString(dataReader, "inviterName");
      this.inviteeId = DataUtil.getInt(dataReader, "inviteeId");

      this.voyageGroupId = DataUtil.getInt(dataReader, "voyageGroupId");
      this.inviteStatus = (InviteStatus) DataUtil.getInt(dataReader, "inviteStatus");
      this.creationTime = DateTime.Parse(DataUtil.getString(dataReader, "creationTime"));

      this.serverName = DataUtil.getString(dataReader, "svrDeviceName");
      this.serverPort = DataUtil.getInt(dataReader, "svrPort");
      this.serverIp = DataUtil.getString(dataReader, "srvAddress");
   }

   #endif
}

[Serializable]
public class PendingVoyageCreation {
   // Determines if the creation is still pending
   public bool isPending = false;

   // The area key of the voyage
   public string areaKey;

   // The name of the server
   public string serverName = "";

   // The Ip of the server
   public string serverIp = "";

   // The port of the server
   public int serverPort;

   // The update time of the entry
   public DateTime updateTime;

   // The xml id of this entry
   public int id;
}

public enum InviteStatus {
   Created = 0,
   Pending = 1,
   Accepted = 2,
   Declined = 3,
}