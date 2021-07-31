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
using MapCustomization;
using System;
using MLAPI;
using System.Linq;
using MLAPI.Messaging;

public class MyNetworkManager : NetworkManager
{
   #region Public Variables

   // Server addresses
   public enum ServerType
   {
      None = 0, Localhost = 1, AmazonVPC = 2, AmazonSydney = 3, AmazonPerformance = 4
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

   // An event triggered when the client is about to start
   public event Action clientStarting;

   // An event triggered when the client is about to stop
   public event Action clientStopping;

   // An event triggered when the client successfully connected
   public event Action clientConnected;

   // An event triggered when the client has disconnected
   public event Action clientDisconnected;

   #endregion

   public override void Awake () {
      D.adminLog("MyNetworkManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      self = this;

   #if FORCE_AMAZON_SERVER
      Debug.Log("FORCE_AMAZON_SERVER is defined, updating the Network Manager server override.");
      this.serverOverride = ServerType.AmazonVPC;
   #endif
   #if FORCE_AMAZON_SERVER_PROD
      Debug.Log("FORCE_AMAZON_SERVER_PROD is defined, updating the Network Manager server override.");
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
         } else if (arg.Contains("ip=")) {
            string[] split = arg.Split('=');
            this.serverOverride = ServerType.None;
            this.networkAddress = split[1];
         }
      }

      if (Util.isForceServerLocalWithAutoDbconfig()) {
         this.serverOverride = ServerType.Localhost;
      }

      if (serverOverride != ServerType.None) {
         this.networkAddress = Global.getAddress(serverOverride);
      }

      base.Awake();
      D.adminLog("MyNetworkManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);

      D.debug("Command Line: " + System.Environment.CommandLine);
   }

   /*private void Start () {
      // Decrease the connection attempts, so that we don't have to wait forever to figure out if the server is down
      this.connectionConfig.MaxConnectionAttempt = 2;
   }*/

   #region Client Functions

   public override void OnStartClient () {
      D.debug($"Starting client: {this.networkAddress}:{getCurrentPort()} with Cloud Build version: {Util.getGameVersion()}");

      clientStarting?.Invoke();

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

      D.debug($"OnClientConnect triggered, Global.netId is {Global.netId}");

      // Check version
      CheckVersionMessage checkVersionMessage = new CheckVersionMessage(Global.netId, Global.clientGameVersion, Application.platform);
      NetworkClient.Send(checkVersionMessage);
   }

   public void continueConnectAfterClientVersionChecked () {
      // Now that we have a connection, we can send our login credentials
      ClientManager.sendAccountNameAndUserId();

      clientConnected?.Invoke();
   }

   public override void OnClientDisconnect (NetworkConnection conn) {
      clientDisconnected?.Invoke();

      D.debug("OnClientDisconnect was triggered.");
   }

   public override void OnStopClient () {
      clientStopping?.Invoke();

      if (NetworkClient.active) {
         D.debug($"OnStopClient is unregistering client handlers.");

         // It's recommended to unregister our existing handlers when the client stops
         MessageManager.unregisterClientHandlers();
      } else {
         D.debug($"OnStopClient was called when NetworkClient.active was false, so not calling unregisterClientHandlers.");
      }

      base.OnStopClient();
   }

   #endregion

   #region Server Functions

   public override void OnStartServer () {
      D.debug("Server started on port: " + MyNetworkManager.getCurrentPort() + " with Cloud Build version: " + Util.getGameVersion());

      // If this is the "Master" server, then start hosting the Server Network
      if (getCurrentPort() == Global.MASTER_SERVER_PORT) {
         ServerNetworkingManager.get().StartHost();
         processServerManagers();
      } else {
         processServerManagers();
         ServerNetworkingManager.get().StartClient();
      }

      // Make note that we started up a server
      wasServerStarted = true;
   }

   private void processServerManagers () {
      // We have to register handlers to be able to send and receive messages
      MessageManager.registerServerHandlers();

      // Load the admin settings
      AdminGameSettingsManager.self.onServerStart();

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

      // Start the voyage maps and voyage groups management
      VoyageManager.self.startVoyageManagement();

      // Start the pvp games management
      PvpManager.self.startPvpManagement();

      // Fetch discovery data from DB
      DiscoveryManager.self.fetchDiscoveriesOnServer();

      // Download all palette configurations from DB
      PaletteSwapManager.self.fetchPaletteData();

      // Start the auction management
      AuctionManager.self.startAuctionManagement();

      // Start the mail management
      MailManager.self.startMailManagement();

      // Start the friend list management
      FriendListManager.self.startFriendListManagement();

      // Log the server start time
      ServerHistoryManager.self.onServerStart();

      // Load perk data from database
      PerkManager.self.loadPerkDataFromDatabase();

      // Start monitoring the client pings
      LagMonitorManager.self.startLagMonitor();

      // Start the open areas management
      RedirectionManager.self.startOpenAreasManagement();
   }

   private void initializeXmlData () {
      // Loads all SQL Data from server
      NPCManager.self.initializeQuestCache();
      ShipDataManager.self.initializeDataCache();
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
      TreasureDropsDataManager.self.initializeServerDataCache();
      NPCQuestManager.self.initializeServerDataCache();
      ItemDefinitionManager.self.loadFromDatabase();
      ProjectileStatManager.self.initializeDataCache();
   }

   public override void OnStopServer () {
      _players.Clear();
      DisconnectionManager.self.clearDisconnectedUsers();

      MessageManager.unregisterServerHandlers();

      ServerHistoryManager.self.logServerEvent(ServerHistoryInfo.EventType.ServerStop);

      // Stop any servers or clients on the Server Network
      ServerNetworkingManager.get().StopHost();
   }

   [ServerOnly]
   public override void OnServerAddPlayer (NetworkConnection conn) {
      int authenicatedAccountId = -1;
      int authenticatedUserId = -1;
      string steamUserId = "";

      // The time at which we started processing this new player connection
      float startTime = Time.realtimeSinceStartup;

      if (_players.TryGetValue(conn.connectionId, out ClientConnectionData data)) {
         authenicatedAccountId = data.accountId;
         authenticatedUserId = data.userId;
         steamUserId = data.steamId;
      }

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
         GuildInfo guildInfo = userObjects.guildInfo;
         GuildRankInfo guildRankInfo = userObjects.guildRankInfo;

         string previousAreaKey = userInfo.areaKey;

         // Get information about owned map
         string baseMapAreaKey = previousAreaKey;

         int mapOwnerId = CustomMapManager.isUserSpecificAreaKey(previousAreaKey) ? CustomMapManager.getUserId(previousAreaKey) : -1;
         UserInfo ownerInfo = mapOwnerId < 0 ? null : (mapOwnerId == userInfo.userId ? userInfo : DB_Main.getUserInfoById(mapOwnerId));

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Get the current voyage info the user is part of, if any
            int voyageId = VoyageGroupManager.self.tryGetGroupByUser(authenticatedUserId, out VoyageGroupInfo voyageGroupInfo) ? voyageGroupInfo.voyageId : -1;

            // If the user is not assigned to this server, we must ask the master server where to redirect him
            if (!ServerNetworkingManager.self.server.assignedUserIds.ContainsKey(authenticatedUserId)) {
               D.debug($"OnServerAddPlayer The user {userInfo.username} is not assigned to this server - Redirecting.");
               StartCoroutine(CO_RedirectUser(conn, userInfo.accountId, userInfo.userId, userInfo.username, voyageId, userObjects.isSinglePlayer, previousAreaKey, ""));
               return;
            }
            D.debug($"OnServerAddPlayer The user {userInfo.username} is assigned to this server.");

            // If this is a custom map, get the base key
            if (ownerInfo != null) {
               if (AreaManager.self.tryGetCustomMapManager(previousAreaKey, out CustomMapManager customMapManager)) {
                  baseMapAreaKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(ownerInfo));
               }
            }

            // If the area is invalid, warp the player to the starting town as a fallback machamism
            if (!AreaManager.self.doesAreaExists(baseMapAreaKey)) {
               D.debug($"OnServerAddPlayer The user '{userInfo.username}' claims to be in the '{baseMapAreaKey}' area, but this area is not valid - Redirecting.");
               
               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  DB_Main.setNewLocalPosition(userInfo.userId, Vector2.zero, Direction.South, Area.STARTING_TOWN);

                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     StartCoroutine(CO_RedirectUser(conn, userInfo.accountId, userInfo.userId, userInfo.username, voyageId, userObjects.isSinglePlayer, Area.STARTING_TOWN, ""));
                  });
               });

               return;
            }

            // Check if the player disconnected a few seconds ago and its object is still in the server
            ClientConnectionData existingConnection = DisconnectionManager.self.getConnectionDataForUser(authenticatedUserId);
            if (existingConnection != null && existingConnection.isReconnecting) {
               D.debug($"There was an existing connection for user id {authenticatedUserId}, so we're going to remove that user ID from the Disconnection Manager.");

               userInfo.localPos = existingConnection.netEntity.transform.localPosition;
               userInfo.facingDirection = (int) existingConnection.netEntity.facing;
               shipInfo.health = existingConnection.netEntity.currentHealth;

               DisconnectionManager.self.removeFromDisconnectedUsers(authenticatedUserId);
            }

            // Notify the master server that a user connected to this server
            ServerNetworkingManager.self.onUserConnectsToServer(userInfo.userId);

            // Create the Player object
            GameObject prefab = AreaManager.self.isSeaArea(previousAreaKey) == true ? PrefabsManager.self.playerShipPrefab : PrefabsManager.self.playerBodyPrefab;
            GameObject playerObject = Instantiate(prefab);
            NetEntity player = playerObject.GetComponent<NetEntity>();

            // User the syncvars to save guild data
            player.guildId = userInfo.guildId;
            if (guildRankInfo != null) {
               player.guildPermissions = guildRankInfo.permissions;
            }

            // Use the syncvars to populate the icon fields
            player.guildIconBackground = guildInfo.iconBackground;
            player.guildIconBackPalettes = guildInfo.iconBackPalettes;
            player.guildIconBorder = guildInfo.iconBorder;
            player.guildIconSigil = guildInfo.iconSigil;
            player.guildIconSigilPalettes = guildInfo.iconSigilPalettes;

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
               MapManager.self.createLiveMap(previousAreaKey, baseMapAreaKey, InstanceManager.getBiomeForInstance(previousAreaKey, voyageId));
               mapPosition = MapManager.self.getAreaUnderCreationPosition(previousAreaKey);
            }

            // Note where we're going to create the player
            Vector3 playerCreationPos = mapPosition + userInfo.localPos;

            // Set information for the player
            player.transform.position = playerCreationPos;
            player.isSinglePlayer = userObjects.isSinglePlayer;
            player.areaKey = previousAreaKey;
            player.userId = userInfo.userId;
            player.accountId = userInfo.accountId;
            player.facing = (Direction) userInfo.facingDirection;
            player.desiredAngle = DirectionUtil.getAngle(player.facing);
            player.XP = userInfo.XP;
            player.adminFlag = userInfo.adminFlag;
            player.voyageGroupId = voyageGroupInfo != null ? voyageGroupInfo.groupId : -1;
            player.isGhost = voyageGroupInfo != null ? voyageGroupInfo.isGhost : false;
            InstanceManager.self.addPlayerToInstance(player, previousAreaKey, voyageId);

            NetworkServer.AddPlayerForConnection(conn, player.gameObject);

            player.setDataFromUserInfo(userInfo, userObjects.armor, userObjects.weapon, userObjects.hat, shipInfo, guildInfo, guildRankInfo);
            player.steamId = steamUserId;

            if (existingConnection != null && existingConnection.isReconnecting) {
               finishPlayerDisconnection(existingConnection);
            }

            _players[conn.connectionId].netEntity = player;

            // Update the observers associated with the instance and the associated players
            Instance instance = InstanceManager.self.getInstance(player.instanceId);
            InstanceManager.self.rebuildInstanceObservers(player, instance);

            // If player was in battle, reconnect player to existing battle
            if (BattleManager.self.getActiveBattlersData().ContainsKey(player.userId)) {
               Battle activeBattle = BattleManager.self.getActiveBattlersData()[player.userId];
               if (activeBattle != null) {
                  Battler activeBattlerObj = activeBattle.getBattler(player.userId);

                  if (!activeBattle.isOver()) {
                     // Reassign the updated info
                     activeBattlerObj.playerNetId = player.netId;
                     activeBattlerObj.player = player;

                     // Assign the Battle ID to the Sync Var
                     player.battleId = activeBattle.battleId;

                     // Update the observers associated with the Battle and the associated players
                     BattleManager.self.rebuildObservers(activeBattlerObj, activeBattle);

                     // Send player the data of the background and their abilities
                     player.rpc.Target_ReceiveBackgroundInfo(player.connectionToClient, activeBattle.battleBoard.xmlID);
                     player.rpc.processPlayerAbilities((PlayerBodyEntity) player, new List<PlayerBodyEntity> { (PlayerBodyEntity) player });
                  }
               }
            }

            // Tell the player information about the Area we're going to send them to
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               // Get map customizations if needed
               MapCustomizationData customizationData = new MapCustomizationData();
               if (mapOwnerId != -1) {
                  int baseMapId = DB_Main.getMapId(baseMapAreaKey);
                  customizationData = DB_Main.exec((cmd) => DB_Main.getMapCustomizationData(cmd, baseMapId, mapOwnerId) ?? customizationData);
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  // Send information about the upcoming map to the user
                  Map map = AreaManager.self.getMapInfo(baseMapAreaKey);
                  if (map != null) {
                     player.rpc.Target_ReceiveMapInfo(map);
                  }
                  player.rpc.Target_ReceiveAreaInfo(player.connectionToClient, previousAreaKey, baseMapAreaKey, AreaManager.self.getAreaVersion(baseMapAreaKey), mapPosition, customizationData, instance.biome);
               });
            });

            // Get player's mute info if exists, and send it to the client
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               PenaltyInfo muteInfo = DB_Main.getPenaltyInfoForAccount(player.accountId, PenaltyType.Mute);
               if (muteInfo != null) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     player.muteExpirationDate = DateTime.FromBinary(muteInfo.penaltyEnd);
                     player.isStealthMuted = muteInfo.penaltyType == PenaltyType.StealthMute;
                  });
               }
            });

            // Send any extra info as targeted RPCs
            player.cropManager.sendSiloInfo();
            player.rpc.sendItemShortcutList();
            player.rpc.checkForUnreadMails();
            player.rpc.checkForPendingFriendshipRequests();
            player.rpc.sendVoyageGroupMembersInfo();
            player.rpc.setNextBiomeUnlocked();
            player.rpc.setAdminBattleParameters();

            // In sea voyages, if the player is spawning in a different position than the default spawn, we conclude he is returning from a treasure site and has already entered PvP
            if (instance.isVoyage && AreaManager.self.isSeaArea(player.areaKey) &&
               Vector3.Distance(userInfo.localPos, SpawnManager.self.getDefaultLocalPosition(player.areaKey)) > 2f) {
               player.hasEnteredPvP = true;
            }

            // Gives the user admin features if it has an admin flag
            player.rpc.Target_GrantAdminAccess(player.connectionToClient, player.isAdmin());

            // Note how long it took to completely process the user login for this account
            LagMonitorManager.self.noteAccountLoginDuration(userInfo.accountName, (Time.realtimeSinceStartup - startTime));
         });
      });
   }

   public ClientConnectionData getConnectedClientDataForAccount (int accountId) {
      return _players.Values.FirstOrDefault(x => x.isAuthenticated() && x.accountId == accountId);
   }

   public ClientConnectionData getConnectedClientDataForUser (int userId) {
      return _players.Values.FirstOrDefault(x => x.isAuthenticated() && x.userId == userId);
   }

   public void finishPlayerDisconnection (ClientConnectionData data) {
      if (data == null || data.netEntity == null) {
         D.warning("Entity has already been destroyed");
         return;
      }

      // Remove the player from the instance
      InstanceManager.self.removeEntityFromInstance(data.netEntity);

      NetworkServer.Destroy(data.netEntity.gameObject);

      removeClientConnectionData(data);
   }

   public void destroyPlayerObjectForAccountId (int accountId) {
      NetEntity entity = getConnectedClientDataForAccount(accountId).netEntity;

      if (entity != null) {
         NetworkServer.Destroy(entity.gameObject);
      }
   }

   public string[] serializedNPCData (List<NPCData> referenceNPCData) {
      List<NPCData> newNPCDataList = new List<NPCData>();

      // Clears out quests and gifts provided to the Clients
      foreach (NPCData npcData in referenceNPCData) {
         // Creates a copy of the npc data with a cleared list of Quests and Gifts
         NPCData newNPCData = new NPCData(npcData.npcId, npcData.greetingTextStranger, npcData.greetingTextAcquaintance,
            npcData.greetingTextCasualFriend, npcData.greetingTextCloseFriend, npcData.greetingTextBestFriend, npcData.giftOfferNPCText,
            npcData.giftLikedText, npcData.giftNotLikedText, npcData.name,
            npcData.interactable, npcData.hasTradeGossipDialogue, npcData.hasGoodbyeDialogue, npcData.lastUsedQuestId, npcData.questId, new List<NPCGiftData>(),
            npcData.iconPath, npcData.spritePath, npcData.isHireable, npcData.landMonsterId, npcData.achievementIdHiringRequirement, npcData.isActive, npcData.shadowOffsetY, npcData.shadowScale, npcData.isStationary);

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

   public override void OnServerConnect (NetworkConnection conn) {
      base.OnServerConnect(conn);

      ClientConnectionData connectionData = new ClientConnectionData();
      connectionData.connection = conn;
      connectionData.connectionId = conn.connectionId;
      connectionData.address = conn.address;

      _players.Add(conn.connectionId, connectionData);
   }

   public override void OnServerDisconnect (NetworkConnection conn) {
      // Called on the server when a client disconnects
      disconnectClient(conn);
   }

   public void disconnectClient (int accountId) {
      NetworkConnection connection = getConnectedClientDataForAccount(accountId).connection;
      disconnectClient(connection);
   }

   public void disconnectClient (NetworkConnection conn, bool forceFinishDisconnection = false) {
      if (conn == null) {
         return;
      }

      if (!_players.ContainsKey(conn.connectionId)) {
         NetworkServer.DestroyPlayerForConnection(conn);
         conn.Disconnect();
         return;
      }

      ClientConnectionData data = _players[conn.connectionId];
      NetEntity player = data.netEntity;

      // Notify the master server that a user disconnected from this server
      int userId = player != null ? player.userId : data.userId;
      if (userId > 0) {
         ServerNetworkingManager.self.onUserDisconnectsFromServer(userId);
      }

      if (player != null) {
         D.debug($"In disconnectClient(), we found an existing NetEntity object for {player.entityName}, so we only call finishPlayerDisconnect() instead of removeClientConnectionData()");

         if (player.getPlayerShipEntity() != null && !forceFinishDisconnection) {
            // If the player is a ship, keep it in the server for a few seconds
            DisconnectionManager.self.addToDisconnectedUsers(data);

            // Remove the account from the connected accounts in the server network so that the user can reconnect anywhere
            if (data.isAuthenticated()) {
               ServerNetworkingManager.self.server.synchronizeConnectedAccount(data.accountId);
            }
         } else {
            finishPlayerDisconnection(data);
         }
      } else {
         // The connection must be removed, so that it doesn't block further login attempts
         removeClientConnectionData(data);
      }
   }

   public IEnumerator CO_RedirectUser (NetworkConnection conn, int accountId, int userId, string userName, int voyageId, bool isSinglePlayer, string destinationAreaKey, string currentAreaKey) {
      // Ask the master server where to redirect this user
      RpcResponse<int> bestServerPort = ServerNetworkingManager.self.redirectUserToBestServer(userId, userName, voyageId, isSinglePlayer, destinationAreaKey, conn.address, currentAreaKey);
      while (!bestServerPort.IsDone) {
         yield return null;
      }

      RedirectMessage redirectMessage = new RedirectMessage(Global.netId, networkAddress, bestServerPort.Value);

      // If the destination server is this same server, we don't disconnect the client
      if (bestServerPort.Value == getCurrentPort()) {
         conn.Send(redirectMessage);
         yield break;
      }

      ServerNetworkingManager.self.server.connectedAccountIds.Remove(accountId);

      // Wait for the update of connected accounts to be serialized and sent to other servers
      yield return null;
      yield return null;

      // Send the Redirect message to the client
      conn.Send(redirectMessage);

      D.debug($"OnServerAddPlayer is sending redirect to {bestServerPort.Value} for {userName}");

      // Disconnect the player from this server
      disconnectClient(conn, true);
   }

   public IEnumerator CO_RedirectUser (NetworkConnection conn, int accountId, int userId, string userName, int voyageId, bool isSinglePlayer, string destinationAreaKey, string currentAreaKey, GameObject entityToDestroy) {
      yield return CO_RedirectUser(conn, accountId, userId, userName, voyageId, isSinglePlayer, destinationAreaKey, currentAreaKey);

      // Destroy the old Player object
      NetworkServer.DestroyPlayerForConnection(conn);
      if (entityToDestroy != null) {
         NetworkServer.Destroy(entityToDestroy);
      }
   }

   #endregion

   public static int getAccountId (NetworkConnection conn) {
      if (conn == null || !_players.TryGetValue(conn.connectionId, out ClientConnectionData connData)) {
         return -1;
      } else {
         return connData.accountId;
      }
   }

   public static bool isAccountAlreadyOnline (int accountId, NetworkConnection conn) {
      ClientConnectionData existingConnection = self.getConnectedClientDataForAccount(accountId);

      // Check if the account is already registered online with a different connection - in this server
      if (existingConnection != null && existingConnection.connectionId != conn.connectionId) {
         if (!DisconnectionManager.self.isUserPendingDisconnection(existingConnection.userId)) {
            D.debug($"Account {accountId} is already online with another connection on this server.");
            return true;
         }
      }

      // Check if the account is registered online in another server
      if (ServerNetworkingManager.self.isAccountOnlineInAnotherServer(accountId)) {
         D.debug($"Account {accountId} is already online in another server.");
         return true;
      }

      return false;
   }

   public static void noteAccountIdForConnection (int accountId, NetworkConnection conn) {
      // Keep track of the account ID that we're associating with this connection
      _players[conn.connectionId].accountId = accountId;

      // Add the account to the connected accounts in the server network data
      ServerNetworkingManager.self.server.synchronizeConnectedAccount(accountId);
   }

   public static void noteUserIdForConnection (int selectedUserId, string steamUserId, NetworkConnection conn) {
      if (conn == null) {
         return;
      }

      // Make sure an entity gameobject for this user doesn't already exist (e.g. due to a player reconnecting before the timeout)
      ClientConnectionData existingConnection = self.getConnectedClientDataForUser(selectedUserId);

      if (existingConnection != null && DisconnectionManager.self.isUserPendingDisconnection(selectedUserId)) {
         existingConnection.isReconnecting = true;
      }

      // Keep track of the user ID that we're associating with this connection
      ClientConnectionData connData = _players[conn.connectionId];
      connData.userId = selectedUserId;
      connData.steamId = steamUserId;
   }

   public static int getCurrentPort () {
      if (self != null && self.telepathy != null) {
         return self.telepathy.port;
      }

      return 0;
   }

   public static HashSet<NetEntity> getPlayers () {
      HashSet<NetEntity> list = new HashSet<NetEntity>();

      foreach (ClientConnectionData connData in _players.Values) {
         if (connData.netEntity != null) {
            list.Add(connData.netEntity);
         }
      }

      return list;
   }

   public static List<ClientConnectionData> getClientConnectionData () {
      return _players.Values.ToList();
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

   private void removeClientConnectionData (ClientConnectionData data) {
      _players.Remove(data.connectionId);

      // Immediately remove the connection from the server network, to avoid an 'account already online' error when warping between servers
      if (data.isAuthenticated()) {
         ServerNetworkingManager.self.server.synchronizeConnectedAccount(data.accountId);
      } else {
         D.debug($"The ClientConnectionData is not authenticated, so not removing account {data.accountId} from this server's connectedAccountIds.");
      }
   }

   #region Private Variables

   // Keep track of players when they connect
   protected static Dictionary<int, ClientConnectionData> _players = new Dictionary<int, ClientConnectionData>();

   // An id we can use for testing
   protected static int _testId = 1;

   #endregion
}
