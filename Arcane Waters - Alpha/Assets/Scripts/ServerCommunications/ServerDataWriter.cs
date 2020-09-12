using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System;
using SimpleJSON;

namespace ServerCommunicationHandlerv2 {
   public class ServerDataWriter : MonoBehaviour {
      #region Public Variables

      // Location where the text files are written
      public string path = "";

      // Location where the pending invites are registered by the main server
      public string invitePath = "";

      // The file name containing the voyage invites
      public const string voyageInviteText = "VoyageInvites.txt";

      // Self
      public static ServerDataWriter self;

      // The list of invites that the remote servers cannot process and will be passed on the main server
      public List<VoyageInviteData> pendingNetworkVoyageInvites;

      #endregion

      private void Awake () {
         self = this;
         path = Application.persistentDataPath + "/ServerData/";
         invitePath = Application.persistentDataPath + "/VoyageInvites/";
      }

      public void initializeMainServerInviteFile () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Makes sure the invite file is existing
            Directory.CreateDirectory(invitePath);
            File.Create(invitePath + voyageInviteText).Close();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Frequently check if other servers are requesting the main server to process the invites
               InvokeRepeating("processRemoteServerInvites", 1, 2);
            });
         });
      }

      public void processRemoteServerInvites () {
         List<VoyageInviteData> remoteVoyageInvites = new List<VoyageInviteData>();

         // Extract all voyage invites from remote servers
         foreach (ServerData remoteServerData in ServerCommunicationHandler.self.serverDataList) {
            foreach (VoyageInviteData remoteServerInvite in remoteServerData.pendingVoyageInvites) {
               remoteVoyageInvites.Add(remoteServerInvite);
            }
         }

         // Create new data to serialize
         VoyageInviteCompiler newInvitesCompiler = new VoyageInviteCompiler();
         newInvitesCompiler.serverVoyageInviteData = remoteVoyageInvites;
         string[] content = Util.serialize(remoteVoyageInvites);

         // Write to file to be read by other servers
         try {
            File.WriteAllText(invitePath + voyageInviteText, content.Length > 0 ? content[0] : "");
         } catch {
            D.debug("Cancelled File Write to avoid violation");
         }
      }

      public List<VoyageInviteData> fetchVoyageInvitesFromText () {
         // Read file from text file
         StreamReader reader = new StreamReader(invitePath + voyageInviteText);
         string rawTextData = reader.ReadToEnd();
         reader.Close();

         // Return empty list if the text file is empty
         if (rawTextData.Length < 1) {
            return new List<VoyageInviteData>();
         }

         // Unserialize text content
         string[] voyageString = new string[1] { rawTextData };
         List<VoyageInviteData> voyageSerializer = Util.unserialize<VoyageInviteData>(voyageString);

         // Fetch only the invites that is relevant to our port
         List<VoyageInviteData> newInvites = voyageSerializer.FindAll(_ => _.serverPort == ServerCommunicationHandler.self.getPort());
         return newInvites;
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

public class VoyageInviteCompiler {
   // The list of voyage invites to be serialized in json format
   public List<VoyageInviteData> serverVoyageInviteData;
}