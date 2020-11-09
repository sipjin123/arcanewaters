using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using UnityEngine;

[Serializable]
public class ServerData {
   // The device name
   public string deviceName;

   // The port of the server
   public int port;

   // The ip of the server
   public string ip;

   // The latest update time of the server data
   public DateTime latestUpdateTime;

   // The list of active voyage instances on this server
   public List<Voyage> voyageList = new List<Voyage>();

   // Keeps track of the users connected to this server
   public List<int> connectedUserIds = new List<int>();

   // Keeps track of the users claimed by this server
   public List<int> claimedUserIds = new List<int>();

   // A listing of open area types on this server
   public List<string> openAreas = new List<string>();

   // A list containing the active voyage invites in the server (this is processed by the main server only, Server with Port 7777)
   public List<VoyageInviteData> pendingVoyageInvites = new List<VoyageInviteData>();

   // The list of invites processed by a server which will be read by the server containing the inviter to clear out the pendingInvite entry
   public List<VoyageInviteData> processedVoyageInvites = new List<VoyageInviteData>();

   public static ServerData copyData (ServerData sourceData) {
      ServerData newData = new ServerData();
      newData.deviceName = sourceData.deviceName;
      newData.port = sourceData.port;
      newData.ip = sourceData.ip;

      newData.latestUpdateTime = sourceData.latestUpdateTime;

      newData.voyageList = sourceData.voyageList;
      newData.pendingVoyageInvites = sourceData.pendingVoyageInvites;
      newData.processedVoyageInvites = sourceData.processedVoyageInvites;
      newData.connectedUserIds = sourceData.connectedUserIds;
      newData.claimedUserIds = sourceData.claimedUserIds;
      newData.openAreas = sourceData.openAreas;

      return newData;
   }

   public ServerData () {
   }
}

[Serializable]
public class VoyageInviteData {
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
   public string creationTime;

   // The server info
   public string serverName = "";
   public int serverPort = 0;
   public string serverIp = "";

   public VoyageInviteData (Server targetServer, int inviterId, string inviterName, int inviteeId, int voyageGroupId, InviteStatus inviteStatus, DateTime creationTime) {
      this.serverName = targetServer.deviceName;
      this.serverPort = targetServer.port;
      this.serverIp = targetServer.ipAddress;

      this.inviterId = inviterId;
      this.inviterName = inviterName;
      this.inviteeId = inviteeId;
      this.voyageGroupId = voyageGroupId;
      this.inviteStatus = inviteStatus;
      this.creationTime = creationTime.ToString();
   }
}

[Serializable]
public class PendingVoyageCreation {
   // Determines if the creation is still pending
   public bool isPending = false;

   // The area key of the voyage
   public string areaKey;

   // When false, the voyage instance is PvE
   public bool isPvP;

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

   // Biome of this voyage
   public int biome;
}

public enum InviteStatus {
   Created = 0,
   Pending = 1,
   Accepted = 2,
   Declined = 3,
}