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
   public ServerCommunicationHandler serverWebRequest;

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
         guiInviteInfo();
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
      GUI.Box(new Rect(Screen.width - 200, 30, 200, 30), "Port is: " + serverWebRequest.ourPort);
      GUI.Box(new Rect(Screen.width - 200, 60, 200, 30), "Name is: " + serverWebRequest.ourDeviceName);
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
         foreach (ServerSqlData server in serverWebRequest.serverDataList) {
            GUILayout.BeginVertical();
            if (GUILayout.Button("Randomly Update Server:\n" + server.port +" : "+ (server == serverWebRequest.ourServerData ? "(LocalServer)" : "(RemoteServer)"))) {
               serverWebRequest.updateServerDatabase(server, true);
            }

            GUILayout.Space(10);
            GUILayout.Box("Connected Users: " + server.connectedUserIds.Count);
            foreach (int ids in server.connectedUserIds) {
               GUILayout.Box("User: " + ids);
            }
            GUILayout.Space(10);
            GUILayout.Box("Existing Voyages:" + server.voyageList.Count);
            foreach (Voyage voyages in server.voyageList) {
               GUILayout.Box("Voyages: " + voyages.voyageId);
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
         if (GUILayout.Button("Generate a \nnew random server")) {
            createRandomServer();
         }
         if (GUILayout.Button("Update and \nRandomize any Server")) {
            updateAnyRandomServer();
         }
         if (GUILayout.Button("Clear Server Data")) {
            serverWebRequest.serverDataList.Clear();
         }
      }
      GUILayout.EndVertical();
   }

   private void guiInviteInfo () {
      GUILayout.BeginVertical();
      {
         GUILayout.Box("Pending Invites are: ");
         GUILayout.Space(5);
         int index = 0;
         foreach (VoyageInviteData pendingVoyage in serverWebRequest.pendingVoyageInvites) {
            GUILayout.Box("Invite: " + index + ": Inviter:{" + pendingVoyage.inviterId + "} Invitee:{" + pendingVoyage.inviteeId + "}\nStatus:{" + pendingVoyage.inviteStatus + "} GrpID:{" + pendingVoyage.voyageGroupId + "}");
            index++;
         }
      }
      GUILayout.EndVertical();
      GUILayout.BeginVertical();
      {
         GUILayout.Box("Pending Creations are: " + serverWebRequest.pendingVoyageCreations.Count);
         GUILayout.Space(5);
         foreach (PendingVoyageCreation pendingVoyage in serverWebRequest.pendingVoyageCreations) {
            if (GUILayout.Button("Clear pending voyage\nCreation: ID:{" + pendingVoyage.id + "} - Area:{" + pendingVoyage.areaKey + "}")) {
               //serverWebRequest.clearVoyageCreation(pendingVoyage);
            }
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

      foreach (Server server in ServerNetwork.self.servers) {
         bool containsUser = server.connectedUserIds.ToList().Contains(inviteeId);

         VoyageInviteData newVoyageInvite = new VoyageInviteData(server, inviterId, inviterName, inviteeId, voyageId, InviteStatus.Created, DateTime.UtcNow);
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.createVoyageInvite(newVoyageInvite);
         });
         break;
      }
   }

   private void updateAnyRandomServer () {
      List<ServerSqlData> newestList = new List<ServerSqlData>(serverWebRequest.serverDataList);
      ServerSqlData ourServerContent = newestList.Find(_ => _.deviceName == serverWebRequest.ourServerData.deviceName);
      newestList.Remove(ourServerContent);

      ServerSqlData randomServer = newestList[Random.Range(0, newestList.Count)];
      serverWebRequest.updateServerDatabase(randomServer, true);
   }

   private void createRandomServer () {
      ServerSqlData sqlData = createRandomData();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setServerContent(sqlData);
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
      serverWebRequest.updateServerDatabase(serverWebRequest.ourServerData, true);
   }

   private void createLocalPlayers () {
      int newUserId = Random.Range(1, 1000);
      if (ServerNetwork.self != null) {
         serverWebRequest.addPlayer(newUserId);
      }
      serverWebRequest.updateServerDatabase(serverWebRequest.ourServerData, true);
   }

   private void createOpenArea () {
      string newArea = "area_" + Random.Range(1, 1000);
      serverWebRequest.ourServerData.openAreas.Add(newArea);
      if (ServerNetwork.self != null) {
         ServerNetwork.self.server.openAreas = serverWebRequest.ourServerData.openAreas.ToArray();
      }
      serverWebRequest.updateServerDatabase(serverWebRequest.ourServerData, true);
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

   // Log Sizes
   private float serverLogWidth = 600;
   private float serverLogHeight = 400;

   #endregion
}