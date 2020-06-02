using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using MapCreationTool;
using MapCreationTool.Serialization;
using BackgroundTool;
using ServerCommunicationHandlerv2;
using MapCustomization;

public class MyNetworkManager : NetworkManager
{
   #region Public Variables

   // Server addresses
   public enum ServerType
   {
      None = 0, Localhost = 1, AmazonVPC = 2, AmazonSydney = 3, ProductionWindows = 4, ProductionLinux = 5
   }

   // The actual server we want to use
   public ServerType serverOverride;

   // The server Cloud Build wants to use (otherwise defaults to the above override)
   public static ServerType cloudBuildOverride = ServerType.None;

   // Whether we're running in Host mode
   public static bool isHost = false;

   // Gets set to true if a server was started up
   public static bool wasServerStarted = false;

   // A reference to the Message Manager
   public MessageManager messageManager;

   // Our Telepathy Transport component
   public TelepathyTransport telepathy;

   // Self
   public static MyNetworkManager self;

   #endregion

   public override void Awake () {
      self = this;

      #if FORCE_AMAZON_SERVER
      Debug.Log("FORCE_AMAZON_SERVER is defined, updating the Network Manager server override.");
      this.serverOverride = ServerType.AmazonVPC;
      #endif
      #if FORCE_SYDNEY
      this.serverOverride = ServerType.AmazonSydney;
      #endif
      #if FORCE_LOCALHOST
      this.serverOverride = ServerType.Localhost;
      #endif

      #if !FORCE_AMAZON_SERVER
      Debug.Log("FORCE_AMAZON_SERVER is not defined.");
      #endif

      foreach (string arg in System.Environment.GetCommandLineArgs()) {
         if (arg.Contains("port=")) {
            string[] split = arg.Split('=');
            telepathy.port = ushort.Parse(split[1]);
         } else if (arg.Contains("serverOverride=")) {
            string[] split = arg.Split('=');
            this.serverOverride = (ServerType) int.Parse(split[1]);
         }
      }

      if (Util.isForceServerLocalWithAutoDbconfig()) {
         this.serverOverride = ServerType.Localhost;
      }

      if (serverOverride != ServerType.None) {
         this.networkAddress = Global.getAddress(serverOverride);
      }

      base.Awake();
   }

   /*private void Start () {
      // Decrease the connection attempts, so that we don't have to wait forever to figure out if the server is down
      this.connectionConfig.MaxConnectionAttempt = 2;
   }*/

   #region Client Functions

   public override void OnStartClient () {
      D.debug("Starting client: " + this.networkAddress + " with Cloud Build version: " + Util.getGameVersion());

      // We have to register handlers to be able to send and receive messages
      MessageManager.registerClientHandlers();

      // Keep track of the values in our Title Screen text fields
      Global.lastUsedAccountName = TitleScreen.self.accountInputField.text;
      Global.lastUserAccountPassword = TitleScreen.self.passwordInputField.text;
      if (SteamManager.Initialized) {
         Steamworks.CSteamID steamId = Steamworks.SteamUser.GetSteamID();
         Global.lastSteamId = steamId.ToString();
      }

      // Get the client version number from the cloud build manifest
      Global.clientGameVersion = Util.getGameVersion();
   }

   public override void OnClientConnect (NetworkConnection conn) {
      // This code is called on the client after connecting to a server

      // Now that we have a connection, we can send our login credentials
      ClientManager.sendAccountNameAndUserId();
   }

   public override void OnClientDisconnect (NetworkConnection conn) {
      ClientScene.RemovePlayer();
   }

   #endregion

   #region Server Functions

   public override void OnStartServer () {
      D.debug("Server started on port: " + MyNetworkManager.getCurrentPort() + " with Cloud Build version: " + Util.getGameVersion());

      // Initializes the server
      ServerRoomManager.self.initializeServer();

      // We have to register handlers to be able to send and receive messages
      MessageManager.registerServerHandlers();

      // Look up and store all of the area keys and spawn positions from the database
      AreaManager.self.storeAreaInfo();
      SpawnManager.self.storeSpawnPositions();

      // Initialize all xml managers to fetch from database
      initializeXmlData();

      // Schedule the leader boards recalculation
      LeaderBoardsManager.self.scheduleLeaderBoardRecalculation();

      // Delete old records in the job_history table
      LeaderBoardsManager.self.pruneJobHistory();

      // Regularly updates the required game version
      GameVersionManager.self.scheduleMinimumGameVersionUpdate();

      // Create the initial map that everyone starts in
      MapManager.self.createLiveMap(Area.STARTING_TOWN);

      // Set up tutorial data
      NewTutorialManager.self.storeInfoFromDatabase();

      // Start the voyage maps and voyage groups management
      VoyageManager.self.startVoyageManagement();

      // Fetch discovery data from DB
      DiscoveryManager.self.fetchDiscoveriesOnServer();

      // Download all palette configurations from DB
      PaletteSwapManager.self.fetchPaletteData();

      // Make note that we started up a server
      wasServerStarted = true;
   }

   private void initializeXmlData () {
      // Loads all SQL Data from server
      NPCManager.self.initializeQuestCache();
      ShipDataManager.self.initializeDataCache();
      // TutorialManager.self.initializeDataCache();
      ClassManager.self.initializeDataCache();
      FactionManager.self.initializeDataCache();
      SpecialtyManager.self.initializeDataCache();
      JobManager.self.initializeDataCache();
      CraftingManager.self.initializeDataCache();
      CropsDataManager.self.initializeDataCache();
      AchievementManager.self.initializeDataCache();
      SeaMonsterManager.self.initializeSeaMonsterCache();
      MonsterManager.self.initializeLandMonsterDataCache();
      ShipAbilityManager.self.initializDataCache();
      ShopXMLManager.self.initializDataCache();
      AbilityManager.self.initializeAbilities();
      BackgroundGameManager.self.initializeDataCache();
      EquipmentXMLManager.self.initializeDataCache();
      SoundEffectManager.self.initializeDataCache();
      XmlVersionManagerServer.self.initializeServerData();
   }

   public override void OnStopServer () {
      _players.Clear();
      DisconnectionManager.self.clearDisconnectedUsers();
   }

   [ServerOnly]
   public override void OnServerAddPlayer (NetworkConnection conn) {
      int authenicatedAccountId = _accountsForConnections[conn];
      int authenticatedUserId = _userIdForConnection[conn];

      if (authenicatedAccountId <= 0 || authenticatedUserId <= 0) {
         ServerMessageManager.sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
         return;
      }

      // Look up the info in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Grab all of the user data in a single query
         UserObjects userObjects = DB_Main.getUserObjects(authenticatedUserId);

         // Store references to the individual info objects for convenience
         UserInfo userInfo = userObjects.userInfo;
         ShipInfo shipInfo = userObjects.shipInfo;

         // Get the current voyage group the user is member of, if any
         VoyageGroupInfo voyageGroupInfo = DB_Main.getVoyageGroupForMember(authenticatedUserId);

         // Get the voyage id, if any
         int voyageId = voyageGroupInfo != null ? voyageGroupInfo.voyageId : -1;

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            string previousAreaKey = userInfo.areaKey;

            // Check if we need to redirect to a different server
            Server bestServer = ServerNetwork.self.findBestServerForConnectingPlayer(previousAreaKey, userInfo.username, userInfo.userId,
               conn.address, userObjects.isSinglePlayer, voyageId);

            if (bestServer != null && bestServer.port != ServerCommunicationHandler.self.ourPort) {
               // Send a Redirect message to the client
               RedirectMessage redirectMessage = new RedirectMessage(Global.netId, networkAddress, bestServer.port);
               conn.Send(redirectMessage);

               return;
            }

            // Check if the player disconnected a few seconds ago and its object is still in the server
            DisconnectionManager.self.reconnectDisconnectedUser(userInfo, shipInfo);

            // Manage the voyage groups on user connection
            VoyageManager.self.onUserConnectsToServer(userInfo.userId);

            // Get information about owned map
            string baseMapAreaKey = previousAreaKey;
            int mapOwnerId = -1;

            if (AreaManager.self.tryGetOwnedMapManager(previousAreaKey, out OwnedMapManager ownedMapManager)) {
               // Get base map area key for owned maps, for others keep it the same as area key
               baseMapAreaKey = ownedMapManager.getBaseMapAreaKey(previousAreaKey);

               if (OwnedMapManager.isUserSpecificAreaKey(previousAreaKey)) {
                  mapOwnerId = OwnedMapManager.getUserId(previousAreaKey);
               }
            }

            // Verify if the area is instantiated and get its position
            Vector2 mapPosition;
            if (AreaManager.self.hasArea(previousAreaKey)) {
               // If the area is already instantiated
               mapPosition = AreaManager.self.getArea(previousAreaKey).transform.position;
            } else if (MapManager.self.isAreaUnderCreation(previousAreaKey)) {
               // If the area is under creation
               mapPosition = MapManager.self.getAreaUnderCreationPosition(previousAreaKey);
            } else {
               // If we don't have the requested area, we need to create it now
               MapManager.self.createLiveMap(previousAreaKey, baseMapAreaKey);
               mapPosition = MapManager.self.getAreaUnderCreationPosition(previousAreaKey);
            }

            // Note where we're going to create the player
            Vector3 playerCreationPos = mapPosition + userInfo.localPos;

            // Create the Player object
            GameObject prefab = AreaManager.self.isSeaArea(previousAreaKey) == true ? PrefabsManager.self.playerShipPrefab : PrefabsManager.self.playerBodyPrefab;
            GameObject playerObject = Instantiate(prefab, playerCreationPos, Quaternion.identity);
            NetEntity player = playerObject.GetComponent<NetEntity>();
            player.isSinglePlayer = userObjects.isSinglePlayer;
            player.areaKey = previousAreaKey;
            player.userId = userInfo.userId;
            player.accountId = userInfo.accountId;
            player.facing = (Direction) userInfo.facingDirection;
            player.desiredAngle = DirectionUtil.getAngle(player.facing);
            player.XP = userInfo.XP;
            player.voyageGroupId = voyageGroupInfo != null ? voyageGroupInfo.groupId : -1;
            InstanceManager.self.addPlayerToInstance(player, previousAreaKey, voyageId);
            NetworkServer.AddPlayerForConnection(conn, player.gameObject);
            ServerCommunicationHandler.self.addPlayer(player.userId);

            player.setDataFromUserInfo(userInfo, userObjects.armor, userObjects.weapon, userObjects.helm, shipInfo);

            // Keep track
            _players[conn.connectionId] = player;

            // Update the observers associated with the instance and the associated players
            Instance instance = InstanceManager.self.getInstance(player.instanceId);
            InstanceManager.self.rebuildInstanceObservers(player, instance);

            // Tell the player information about the Area we're going to send them to
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               // Get map customizations if needed
               MapCustomizationData customizationData = new MapCustomizationData();
               if (mapOwnerId != -1) {
                  int baseMapId = DB_Main.getMapId(baseMapAreaKey);
                  customizationData = DB_Main.getMapCustomizationData(baseMapId, mapOwnerId) ?? customizationData;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  player.rpc.Target_ReceiveAreaInfo(player.connectionToClient, previousAreaKey, baseMapAreaKey, AreaManager.self.getAreaVersion(baseMapAreaKey), mapPosition, customizationData);
               });
            });

            // Server provides clients with info of the npc
            List<NPCData> referenceNPCData = NPCManager.self.getNPCDataInArea(previousAreaKey);

            // TODO: Check if this is still necessary
            // Sends npc data of the area to the client
            // player.rpc.Target_ReceiveNPCsForCurrentArea(player.connectionToClient, serializedNPCData(referenceNPCData));

            // Send any extra info as targeted RPCs
            player.cropManager.sendSiloInfo();

            // Gives the user admin features if it has an admin flag
            player.rpc.Target_GrantAdminAccess(player.connectionToClient, player.isAdmin());

            // Give the player local authority so that movement feels instantaneous
            player.netIdent.AssignClientAuthority(conn);
         });
      });
   }

   public string[] serializedNPCData (List<NPCData> referenceNPCData) {
      List<NPCData> newNPCDataList = new List<NPCData>();

      // Clears out quests and gifts provided to the Clients
      foreach (NPCData npcData in referenceNPCData) {
         // Creates a copy of the npc data with a cleared list of Quests and Gifts
         NPCData newNPCData = new NPCData(npcData.npcId, npcData.greetingTextStranger, npcData.greetingTextAcquaintance,
            npcData.greetingTextCasualFriend, npcData.greetingTextCloseFriend, npcData.greetingTextBestFriend, npcData.giftOfferNPCText,
            npcData.giftLikedText, npcData.giftNotLikedText, npcData.name, npcData.faction, npcData.specialty,
            npcData.hasTradeGossipDialogue, npcData.hasGoodbyeDialogue, npcData.lastUsedQuestId, new List<Quest>(), new List<NPCGiftData>(),
            npcData.iconPath, npcData.spritePath, npcData.isHireable, npcData.landMonsterId, npcData.achievementIdHiringRequirement);

         newNPCDataList.Add(newNPCData);
      }

      return Util.serialize(newNPCDataList);
   }

   public override void OnStartHost () {
      base.OnStartHost();

      isHost = true;
   }

   public override void OnStopHost () {
      base.OnStopHost();

      isHost = false;

      // Reset our Instance Manager
      InstanceManager.self.reset();
   }

   public override void OnServerDisconnect (NetworkConnection conn) {
      // Called on the server when a client disconnects

      // Clear out these references so we don't leak memory
      if (_accountsForConnections.ContainsKey(conn)) {
         _accountsForConnections.Remove(conn);
      }
      if (_userIdForConnection.ContainsKey(conn)) {
         _userIdForConnection.Remove(conn);
      }

      // Remove the player
      if (_players.ContainsKey(conn.connectionId)) {
         NetEntity player = _players[conn.connectionId];

         // Remove the player from our internal list
         _players.Remove(conn.connectionId);

         // Remove this player from the user list of the server
         ServerCommunicationHandler.self.removePlayer(player.userId);

         // Manage the voyage groups on user disconnection
         VoyageManager.self.onUserDisconnectsFromServer(player.userId);

         if (player is ShipEntity) {
            // If the player is a ship, keep it in the server for a few seconds
            DisconnectionManager.self.addToDisconnectedUsers(player);
         } else {
            destroyPlayer(player);
         }
      }
   }

   public void destroyPlayer (NetEntity player) {
      if (player == null) {
         return;
      }

      // Get the connection
      NetworkConnection conn = player.netIdent.connectionToClient;

      // Remove this player from the user list of the server
      ServerCommunicationHandler.self.removePlayer(player.userId);

      // Remove the player from the instance
      InstanceManager.self.removeEntityFromInstance(player);

      // Remove the player from our internal list
      _players.Remove(conn.connectionId);

      // Remove the player from the list of disconnected players
      DisconnectionManager.self.removeFromDisconnectedUsers(player);

      // Destroy the player object
      NetworkServer.DestroyPlayerForConnection(conn);
   }

   #endregion

   public static int getAccountId (NetworkConnection conn) {
      if (_accountsForConnections.ContainsKey(conn)) {
         return _accountsForConnections[conn];
      }

      return -1;
   }

   public static void noteAccountIdForConnection (int accountId, NetworkConnection conn) {
      // Keep track of the account ID that we're associating with this connection
      _accountsForConnections[conn] = accountId;
   }

   public static void noteUserIdForConnection (int selectedUserId, NetworkConnection conn) {
      // Keep track of the user ID that we're associating with this connection
      _userIdForConnection[conn] = selectedUserId;
   }

   public static int getCurrentPort () {
      if (self != null && self.telepathy != null) {
         return self.telepathy.port;
      }

      return 0;
   }

   public static Dictionary<int, NetEntity> getPlayers () {
      return _players;
   }

   public static T fetchEntityFromNetId<T> (uint netId) where T : NetEntity {
      if (!NetworkIdentity.spawned.ContainsKey(netId)) {
         return null;
      }

      NetworkIdentity identity = NetworkIdentity.spawned[netId];
      NetworkBehaviour[] behaviours = identity.NetworkBehaviours;
      T entity = null;

      if (identity != null && behaviours != null) {
         foreach (NetworkBehaviour behaviour in behaviours) {
            entity = behaviour as T;
            if (entity != null) {
               break;
            }
         }
      }

      return entity;
   }

   #region Private Variables

   // Keep track of players when they connect
   protected static Dictionary<int, NetEntity> _players = new Dictionary<int, NetEntity>();

   // Map connections to authenticated account IDs
   protected static Dictionary<NetworkConnection, int> _accountsForConnections = new Dictionary<NetworkConnection, int>();

   // Map connections to authenticated user IDs
   protected static Dictionary<NetworkConnection, int> _userIdForConnection = new Dictionary<NetworkConnection, int>();

   // An id we can use for testing
   protected static int _testId = 1;

   #endregion
}
