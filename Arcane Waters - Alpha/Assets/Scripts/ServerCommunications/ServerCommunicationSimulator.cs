using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;
using System.Linq;
using ServerCommunicationHandlerv2;

namespace ServerCommunicationHandlerv1 {
   public class ServerCommunicationSimulator : MonoBehaviour {
      #region Public Variables

      // Reference to the communication handler
      public ServerCommunicationHandlerv2.ServerCommunicationHandler communicationHandler;

      // Reference to the voyage handler
      public ServerCommunicationHandlerv2.SharedServerDataHandler voyageHandler;

      // Simulator flags
      public bool toggleGUI;

      // Enables the simulator GUI
      public bool enableGUI;

      // Determines if the best server selection should be overridden for simulation purposes
      public bool overrideBestServerConnection = false;

      // The server index to connect to for simulation purposes
      public int overrideConnectServerIndex = 0;

      #endregion

      #region Simulator Updates and UI

      private void OnGUI () {
         if (!Util.isServerBuild() || !enableGUI) {
            return;
         }

         if (GUI.Button(new Rect(Screen.width - 70, 0, 70, 30), "Server\nDebug")) {
            toggleGUI = !toggleGUI;
         }
         if (!toggleGUI) {
            return;
         }

         GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
         guiServerSetup();

         GUILayout.BeginHorizontal();
         {
            guiIndividualServerInfo();
         }
         GUILayout.EndHorizontal();

         GUILayout.BeginHorizontal();
         {
            guiButtonTests();
            forceClientServerConnection();
         }
         GUILayout.EndHorizontal();
      }

      private void forceClientServerConnection () {
         GUILayout.BeginVertical();
         GUILayout.Box("NOTE: This will force the clients\nto connect to a specific server");
         if (GUILayout.Button("Override Best Server: " + overrideBestServerConnection)) {
            overrideBestServerConnection = !overrideBestServerConnection;
         }
         string svName = "None";
         try {
            svName = ServerNetwork.self.servers.ToList()[overrideConnectServerIndex].port.ToString();
         } catch {
         }

         if (overrideBestServerConnection) {
            GUILayout.Box("The selected Server is: " + svName + "\nTotal Servers: " + ServerNetwork.self.servers.Count);
            GUILayout.Space(15);
            int index = 0;
            foreach (Server server in ServerNetwork.self.servers) {
               if (GUILayout.Button("Connect to this server:\n" + server.deviceName + " : " + server.port)) {
                  overrideConnectServerIndex = index;
               }
               index++;
            }
         }
         GUILayout.EndVertical();
      }

      private void guiServerSetup () {
         GUI.Box(new Rect(Screen.width - 200, 30, 200, 30), "Port is: " + communicationHandler.getPort());
         GUI.Box(new Rect(Screen.width - 200, 60, 200, 30), "Name is: " + communicationHandler.ourDeviceName);
         if (GUI.Button(new Rect(Screen.width - 400, 0, 200, 30), "Send Glboal Msg")) {
            ServerNetwork.self.server.SendGlobalChat(_globalMessage, 0);
         }
         _globalMessage = GUI.TextField(new Rect(Screen.width - 400, 30, 200, 60), _globalMessage);
      }

      private void guiIndividualServerInfo () {
         GUI.Box(new Rect(0, 0, serverLogWidth, serverLogHeight), "");
         _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(serverLogWidth), GUILayout.Height(serverLogHeight));
         GUILayout.BeginHorizontal();
         {
            foreach (ServerData server in communicationHandler.serverDataList) {
               GUILayout.BeginVertical();
               if (GUILayout.Button("Randomly Update Server:\n" + server.port + " : " + (server == communicationHandler.ourServerData ? "(LocalServer)" : "(RemoteServer)"))) {
               }

               GUILayout.Space(10);
               GUILayout.Box("======= Connected Users: " + server.connectedUserIds.Count);
               foreach (int ids in server.connectedUserIds) {
                  GUILayout.Box("Svr Data User: " + ids);
               }
               GUILayout.Space(10);

               Server equivalentServer = ServerNetwork.self.servers.ToList().Find(_ => _.port == server.port);
               try {
                  GUILayout.Box("======= Connected Users in ServerObj: " + equivalentServer.connectedUserIds.Count);
                  foreach (int ids in equivalentServer.connectedUserIds) {
                     GUILayout.Box("Svr User: " + ids);
                  }
               } catch {
                  GUILayout.Box("No equivalent Server for port: " + server.port);
               }
               GUILayout.Space(10);
               GUILayout.Box("======= Claimed Users: " + server.claimedUserIds.Count);
               foreach (int ids in server.claimedUserIds) {
                  GUILayout.Box("Claimed User: " + ids);
               }

               GUILayout.Space(10);
               GUILayout.Box("======= Pending Voyage Invites:" + server.pendingVoyageInvites.Count);
               foreach (VoyageInviteData voyages in server.pendingVoyageInvites) {
                  GUILayout.Box("Invite: " + voyages.inviterId + " -> " + voyages.inviteeId + " [" + voyages.voyageGroupId + "]");
               }
               GUILayout.Space(10);
               GUILayout.Box("======= Processed Voyage Invites:" + server.processedVoyageInvites.Count);
               foreach (VoyageInviteData voyages in server.processedVoyageInvites) {
                  GUILayout.Box("Invite: " + voyages.inviterId + " -> " + voyages.inviteeId + " [" + voyages.voyageGroupId + "]");
               }
               GUILayout.EndVertical();
            }
         }
         GUILayout.EndHorizontal();
         GUILayout.EndScrollView();
      }

      private void guiButtonTests () {
         GUILayout.BeginVertical();
         {
            if (GUILayout.Button("Create a test voyage")) {
               createLocalVoyage();
            }
            if (GUILayout.Button("Create a test user")) {
               createLocalPlayers();
            }
            if (GUILayout.Button("Create a test area")) {
               createOpenArea();
            }
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("InviterId: ");
            _inviterId = GUILayout.TextField(_inviterId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("InviteeId: ");
            _inviteeId = GUILayout.TextField(_inviteeId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Voyage: ");
            _voyageGroupId = GUILayout.TextField(_voyageGroupId);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create an invite")) {
               simulateVoyageInvite();
            }
         }
         GUILayout.EndVertical();

         GUILayout.BeginVertical();
         {
            if (GUILayout.Button("Clear Server Data")) {
               communicationHandler.serverDataList.Clear();
            }
         }
         GUILayout.EndVertical();
      }

      #endregion

      #region Simulator Functions

      private void simulateVoyageInvite () {
         int inviterId = int.Parse(_inviterId);
         int inviteeId = int.Parse(_inviteeId);
         int voyageId = int.Parse(_voyageGroupId);
         string inviterName = "UnNamed";
         try {
            inviterName = BodyManager.self.getBody(inviterId).entityName;
         } catch {
         }

         Server serverContaininguser = ServerNetwork.self.getServerContainingUser(inviteeId);
         if (serverContaininguser == null) {
            serverContaininguser = ServerNetwork.self.server;
         }
         SharedServerDataHandler.self.createInvite(serverContaininguser, voyageId, inviterId, inviterName, inviteeId);
      }

      private ServerData createRandomData () {
         ServerData newData = new ServerData();

         newData.deviceName = "device_";
         for (int i = 0; i < 5; i++) {
            int randomIndex = Random.Range(1, _alphabets.Length);
            newData.deviceName += _alphabets[randomIndex].ToString();
         }
         newData.ip = "127.0.0.1";
         newData.port = 7777;
         newData.latestUpdateTime = DateTime.UtcNow;

         newData.voyageList = new List<Voyage>();
         newData.openAreas = generateRandomArea();
         newData.connectedUserIds = generateRandomUserIds();

         return newData;
      }

      private void createLocalVoyage () {
         string voyageName = "voyage_" + Random.Range(1, 1000);
         int biomeCount = Enum.GetValues(typeof(Biome.Type)).Length;

         Voyage newVoyage = new Voyage {
            areaKey = voyageName,
            biome = (Biome.Type) Random.Range(1, biomeCount - 1),
            voyageId = Random.Range(1, 1000)
         };

         communicationHandler.ourServerData.voyageList.Add(newVoyage);
         if (ServerNetwork.self != null) {
            ServerNetwork.self.server.voyages = communicationHandler.ourServerData.voyageList;
         }
      }

      private void createLocalPlayers () {
         int newUserId = Random.Range(1, 1000);
         if (ServerNetwork.self != null) {
            communicationHandler.addPlayer(newUserId);
         }
      }

      private void createOpenArea () {
         string newArea = "area_" + Random.Range(1, 1000);
         communicationHandler.ourServerData.openAreas.Add(newArea);
         if (ServerNetwork.self != null) {
            ServerNetwork.self.server.openAreas = communicationHandler.ourServerData.openAreas.ToArray();
         }
      }

      public List<string> generateRandomArea () {
         int randomCount = Random.Range(1, 5);
         List<string> areaList = new List<string>();

         for (int i = 0; i < randomCount; i++) {
            areaList.Add("area_" + Random.Range(1, 1000));
         }

         return areaList;
      }

      public List<int> generateRandomUserIds () {
         int randomCount = Random.Range(2, 5);
         List<int> userIdList = new List<int>();

         for (int i = 0; i < randomCount; i++) {
            userIdList.Add(Random.Range(1, 1000));
         }

         return userIdList;
      }

      #endregion

      #region Private Variables

      // Randomized character source
      private string _alphabets = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

      // GUI scroll position
      private Vector2 _scrollPosition;

      // The message to be sent to all players for simulation purposes
      private string _globalMessage;

      // Invitation parameters for simulation
      private string _inviterId;
      private string _inviteeId;
      private string _voyageGroupId;

      // Log Sizes
      private float serverLogWidth = 600;
      private float serverLogHeight = 400;

      #endregion
   }
}