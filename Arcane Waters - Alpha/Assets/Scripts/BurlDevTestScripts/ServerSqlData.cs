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

   // The list of active voyage instances on this server
   public List<Voyage> voyageList = new List<Voyage>();

   // Keeps track of the users connected to this server
   public List<int> connectedUserIds = new List<int>();

   // A listing of open area types on this server
   public List<string> openAreas = new List<string>();

   // The list of voyage invites
   public List<VoyageInviteData> voyageInvites = new List<VoyageInviteData>();

   public string getRawVoyage () {
      XmlSerializer ser = new XmlSerializer(voyageList.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, voyageList);
      }

      return sb.ToString();
   }

   public string getRawVoyageInvites () {
      XmlSerializer ser = new XmlSerializer(voyageInvites.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, voyageInvites);
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
      List<VoyageInviteData> voyageInvites = new List<VoyageInviteData>();
      if (DataUtil.getString(dataReader, "voyageInvites").Length > 1) {
         voyageInvites = Util.xmlLoad<List<VoyageInviteData>>(DataUtil.getString(dataReader, "voyageInvites"));
      } else {
         voyageInvites = new List<VoyageInviteData>();
      }

      this.latestUpdate = DateTime.Parse(DataUtil.getString(dataReader, "updateTime"));

      this.openAreas = new List<string>(openAreas);
      this.voyageList = voyages;
      this.connectedUserIds = connectedUsers;
      this.voyageInvites = voyageInvites;

      this.port = currentServerData.port;
      this.ip = currentServerData.ip;
      this.deviceName = currentServerData.deviceName;
   }

   #endif
}

[Serializable]
public class VoyageInviteData {
   // Name of the player inviting 
   public string inviterName;

   // The id of the player to receive the invite
   public int inviteeId;

   // The voyage group id of the invite
   public int voyageGroupId;

   // The status of the invite
   public InviteStatus inviteStatus;

   public VoyageInviteData () {

   }
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
   Pending = 0,
   Accepted = 1,
   Declinded = 2,
}