using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;
using System.Linq;

public class ServerCommunicationSimulator : MonoBehaviour {
   #region Public Variables

   // Reference to the web request script
   public ServerWebRequests serverWebRequest;

   // Simulator flags
   public bool overrideDeviceName;
   public bool toggleGUI;

   #endregion

   #region Simulator Updates and UI

   private void OnGUI () {
      if (GUI.Button(new Rect(Screen.width - 100, 0, 100, 30), "OpenGUI")) {
         toggleGUI = !toggleGUI;
      }

      if (GUI.Button(new Rect(Screen.width - 400, 0, 200, 30), "Override Best Server: " + ServerNetwork.self.overrideBestServerConnection)) {
         ServerNetwork.self.overrideBestServerConnection = !ServerNetwork.self.overrideBestServerConnection;
      }
      if (ServerNetwork.self.overrideBestServerConnection) {
         string svName = "None";
         try {
            svName = ServerNetwork.self.servers.ToList()[ServerNetwork.self.overrideConnectServerIndex].port.ToString();
         } catch {

         }
         if (GUI.Button(new Rect(Screen.width - 400, 30, 200, 30), ServerNetwork.self.overrideConnectServerIndex + ": Set sv to 0: " + svName)) {
            ServerNetwork.self.overrideConnectServerIndex = 0;
         }
         if (GUI.Button(new Rect(Screen.width - 400, 60, 200, 30), ServerNetwork.self.overrideConnectServerIndex + ": Set sv to 1: " + svName)) {
            ServerNetwork.self.overrideConnectServerIndex = 1;
         }
      }

      GUI.Box(new Rect(Screen.width - 200, 30, 200, 30), "Port is: " + serverWebRequest.ourPort);
      GUI.Box(new Rect(Screen.width - 200, 60, 200, 30), "Name is: " + serverWebRequest.ourDeviceName);
      if (GUI.Button(new Rect(Screen.width - 250, 90, 250, 30), "Override Name: " + serverWebRequest.ourDeviceName + " : " + overrideDeviceName)) {
         overrideDeviceName = !overrideDeviceName;
      }
      if (GUI.Button(new Rect(Screen.width - 200, 120, 200, 30), "Send Glboal Msg")) {
         ServerNetwork.self.server.SendGlobalChat(_globalMessage, 0);
      }
      _globalMessage = GUI.TextField(new Rect(Screen.width - 400, 120, 200, 60), _globalMessage);

      if (!toggleGUI) {
         return;
      }

      GUILayout.BeginHorizontal();
      {
         _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(600), GUILayout.Height(400));
         GUILayout.BeginHorizontal();
         {
            foreach (ServerSqlData server in serverWebRequest.serverDataList) {
               GUILayout.BeginVertical();
               if (GUILayout.Button("Update Server: " + server.port)) {
                  serverWebRequest.updateSpecificServer(server, true);
               }

               GUILayout.Space(20);
               foreach (int ids in server.connectedUserIds) {
                  GUILayout.Box("User: " + ids);
               }
               GUILayout.Space(20);
               foreach (Voyage voyages in server.voyageList) {
                  GUILayout.Box("Voyages: " + voyages.voyageId);
               }
               GUILayout.EndVertical();
            }
         }
         GUILayout.EndHorizontal();
         GUILayout.EndScrollView();
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
         GUILayout.BeginVertical();
         {
            if (GUILayout.Button("Update local server")) {
               D.editorLog("Updating current server data", Color.green);
               serverWebRequest.updateSpecificServer(serverWebRequest.ourServerData, true);
            }
            if (GUILayout.Button("Create a voyage")) {
               createLocalVoyage();
            }
            if (GUILayout.Button("Create a user")) {
               createLocalPlayers();
            }
            if (GUILayout.Button("Create an area")) {
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
            if (GUILayout.Button("Manual Check Updates")) {
               serverWebRequest.checkServerUpdates();
            }
            if (GUILayout.Button("Get server Contents for all")) {
               serverWebRequest.getServerContent(serverWebRequest.serverDataList);
            }
            if (GUILayout.Button("Generate a new random server")) {
               createRandomServer();
            }
            if (GUILayout.Button("Update a Random Server")) {
               updateAnyRandomServer();
            }
            if (GUILayout.Button("Clear Server Data")) {
               serverWebRequest.serverDataList.Clear();
            }
         }
         GUILayout.EndVertical();

         GUILayout.BeginVertical();
         {
            GUILayout.Box("Pending Invites are: ");
            GUILayout.Space(5);
            foreach (VoyageInviteData pendingVoyage in serverWebRequest.pendingVoyageInvites) {
               GUILayout.Box("Confirm pending voyage : " + pendingVoyage.inviterId + " -> " + pendingVoyage.inviteeId + " {} " + pendingVoyage.voyageGroupId);
            }
         }
         GUILayout.EndVertical();
         GUILayout.BeginVertical();
         {
            GUILayout.Box("Pending Creations are: ");
            GUILayout.Space(5);
            foreach (PendingVoyageCreation pendingVoyage in serverWebRequest.pendingVoyageCreations) {
               if (GUILayout.Button("Confirm pending voyage : " + pendingVoyage.id + " - " + pendingVoyage.areaKey)) {
                  serverWebRequest.confirmVoyageCreation(pendingVoyage);
               }
            }
         }
         GUILayout.EndVertical();
      }
      GUILayout.EndHorizontal();
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

      foreach (Server server in ServerNetwork.self.servers) {
         bool containsUser = server.connectedUserIds.ToList().Contains(inviteeId);

         VoyageInviteData newVoyageInvite = new VoyageInviteData(server, inviterId, inviterName, inviteeId, voyageId, InviteStatus.Created, DateTime.UtcNow);
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.createVoyageInvite(newVoyageInvite);
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Server Created the invite", Color.green);
            });
         });
         break;
      }
   }

   private void updateAnyRandomServer () {
      List<ServerSqlData> newestList = new List<ServerSqlData>(serverWebRequest.serverDataList);
      ServerSqlData ourServerContent = newestList.Find(_ => _.deviceName == serverWebRequest.ourServerData.deviceName);
      newestList.Remove(ourServerContent);

      ServerSqlData randomServer = newestList[Random.Range(0, newestList.Count)];
      serverWebRequest.updateSpecificServer(randomServer, true);
   }

   private void createRandomServer () {
      D.editorLog("Creating a random server", Color.green);
      ServerSqlData sqlData = createRandomData();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setServerContent(sqlData);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Server was set: " + sqlData.deviceName, Color.blue);
         });
      });
   }

   private ServerSqlData createRandomData () {
      ServerSqlData newData = new ServerSqlData();

      newData.deviceName = "device_";
      for (int i = 0; i < 5; i++) {
         int randomIndex = Random.Range(1, _alphabets.Length);
         newData.deviceName += _alphabets[randomIndex].ToString();
      }
      newData.ip = "127.0.0.1";
      newData.port = 7777;
      newData.latestUpdate = DateTime.UtcNow;

      newData.voyageList = generateRandomVoyage();
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

      serverWebRequest.ourServerData.voyageList.Add(newVoyage);
      if (ServerNetwork.self != null) {
         ServerNetwork.self.server.voyages = serverWebRequest.ourServerData.voyageList;
      }
      serverWebRequest.updateSpecificServer(serverWebRequest.ourServerData, false, true);
   }

   private void createLocalPlayers () {
      int newUserId = Random.Range(1, 1000);
      serverWebRequest.ourServerData.connectedUserIds.Add(newUserId);
      if (ServerNetwork.self != null) {
         D.editorLog("Creater local user at: Local Server");
         serverWebRequest.ourServerData.connectedUserIds.Add(newUserId);
         ServerNetwork.self.server.connectedUserIds.Add(newUserId);
      }
      serverWebRequest.updateSpecificServer(serverWebRequest.ourServerData, false, true);
   }

   private void createOpenArea () {
      string newArea = "area_" + Random.Range(1, 1000);
      serverWebRequest.ourServerData.openAreas.Add(newArea);
      if (ServerNetwork.self != null) {
         ServerNetwork.self.server.openAreas = serverWebRequest.ourServerData.openAreas.ToArray();
      }
      serverWebRequest.updateSpecificServer(serverWebRequest.ourServerData, false, true);
   }

   public List<VoyageInviteData> generateVoyageInviteForServer (ServerSqlData serverSelected) {
      List<VoyageInviteData> newInviteList = new List<VoyageInviteData>();
      int inviteeId = serverSelected.connectedUserIds[0];
      string inviterName = "Player_";
      for (int i = 0; i < 5; i++) {
         inviterName += _alphabets[Random.Range(1, _alphabets.Length)];
      }
      int groupId = serverSelected.voyageList[0].voyageId;

      newInviteList.Add(new VoyageInviteData(ServerNetwork.self.server, 0, inviterName, inviteeId, groupId, InviteStatus.Created, DateTime.UtcNow));
      return newInviteList;
   }

   public List<Voyage> generateRandomVoyage () {
      List<Voyage> voyageList = new List<Voyage>();
      int randomCount = Random.Range(1, 5);

      for (int i = 0; i < randomCount; i++) {
         string voyageName = "voyage_" + Random.Range(1, 1000);
         int biomeCount = Enum.GetValues(typeof(Biome.Type)).Length;

         Voyage newVoyage = new Voyage {
            areaKey = voyageName,
            biome = (Biome.Type) Random.Range(1, biomeCount - 1),
            voyageId = Random.Range(1, 1000)
         };
         voyageList.Add(newVoyage);
      }

      return voyageList;
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

   #endregion
}