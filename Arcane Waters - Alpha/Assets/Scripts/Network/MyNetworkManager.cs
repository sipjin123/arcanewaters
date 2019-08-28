using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MyNetworkManager : NetworkManager {
   #region Public Variables

   // Server addresses
   public enum ServerType { None = 0, Localhost = 1, AmazonVPC = 2, AmazonSydney = 3 }

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

   // The IP address we want to use for Photon (can be overridden on the command line)
   public string photonAddress = "127.0.0.1";

   // The port we want to use for Photon (can be overriden on the command line)
   public int photonPort = 4530;

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
         } else if (arg.Contains("photonAddress=")) {
            string[] split = arg.Split('=');
            this.photonAddress = split[1];
         } else if (arg.Contains("photonPort=")) {
            string[] split = arg.Split('=');
            this.photonPort = int.Parse(split[1]);
         } else if (arg.Contains("serverOverride=")) {
            string[] split = arg.Split('=');
            this.serverOverride = (ServerType) int.Parse(split[1]);
         }
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
      D.debug("Starting client: " + this.networkAddress);

      // We have to register handlers to be able to send and receive messages
      MessageManager.registerClientHandlers();

      // Keep track of the values in our Title Screen text fields
      Global.lastUsedAccountName = TitleScreen.self.accountInputField.text;
      Global.lastUserAccountPassword = TitleScreen.self.passwordInputField.text;
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
      // We have to register handlers to be able to send and receive messages
      MessageManager.registerServerHandlers();

      // Start up Photon so the servers can talk to each other
      connectToPhotonMaster();

      // Schedule the leader boards recalculation
      LeaderBoardsManager.self.scheduleLeaderBoardRecalculation();

      // Delete old records in the job_history table
      LeaderBoardsManager.self.pruneJobHistory();

      // Make note that we started up a server
      wasServerStarted = true;
   }

   public override void OnStopServer () {
      _players.Clear();
      PhotonNetwork.Disconnect();
   }

   [ServerOnly]
   public override void OnServerAddPlayer (NetworkConnection conn, AddPlayerMessage extraMessage) {
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
         Armor armor = userObjects.armor;
         ShipInfo shipInfo = userObjects.shipInfo;

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Area.Type previousAreaType = (Area.Type) userInfo.areaId;

            // Check if we need to redirect to a different server
            Server bestServer = ServerNetwork.self.findBestServerForConnectingPlayer(previousAreaType, userInfo.username, userInfo.userId, conn.address);
            if (bestServer != null && !bestServer.view.isMine) {
               // Send a Redirect message to the client
               RedirectMessage redirectMessage = new RedirectMessage(Global.netId, bestServer.ipAddress, bestServer.port);
               NetworkServer.SendToClient(conn.connectionId, redirectMessage);

               return;
            }

            // Create the Player object
            GameObject prefab = Area.isSea(previousAreaType) ? PrefabsManager.self.playerShipPrefab : PrefabsManager.self.playerBodyPrefab;
            GameObject playerObject = Instantiate(prefab, userInfo.localPos, Quaternion.identity);
            NetEntity player = playerObject.GetComponent<NetEntity>();
            player.areaType = previousAreaType;
            player.userId = userInfo.userId;
            player.accountId = userInfo.accountId;
            player.facing = (Direction) userInfo.facingDirection;
            player.desiredAngle = DirectionUtil.getAngle(player.facing);
            player.XP = userInfo.XP;
            InstanceManager.self.addPlayerToInstance(player, previousAreaType);
            NetworkServer.AddPlayerForConnection(conn, player.gameObject);

            if (player is BodyEntity) {
               BodyEntity body = (BodyEntity) player;
               body.setDataFromUserInfo(userInfo, userObjects.armor, userObjects.weapon);
            } else if (player is PlayerShipEntity) {
               PlayerShipEntity playerShip = (PlayerShipEntity) player;
               playerShip.setDataFromUserInfo(userInfo, armor, userObjects.weapon, shipInfo);
            }

            // Keep track
            _players[conn.connectionId] = player;

            // Update the observers associated with the instance and the associated players
            Instance instance = InstanceManager.self.getInstance(player.instanceId);
            InstanceManager.self.rebuildInstanceObservers(player, instance);

            // Send any extra info as targeted RPCs
            player.cropManager.sendSiloInfo();

            TutorialManager.self.sendTutorialInfo(player, false);
         });
      });
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

         // Remove the player from the instance
         InstanceManager.self.removeEntityFromInstance(player);

         // Remove the player from our internal list
         _players.Remove(conn.connectionId);

         NetworkServer.DestroyPlayerForConnection(conn);
      }
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

   public void connectToPhotonMaster () {
      // Start up Photon so the servers can talk to each other
      if (this.networkAddress == "127.0.0.1" || this.networkAddress == "localhost") {
         // During testing, we can use Photon Cloud to simplify things, and allow testing on Mac (where Photon Control can't run)
         PhotonNetwork.ConnectUsingSettings("v1");
         D.debug("Connecting to Photon cloud, because of network address: " + this.networkAddress);

      } else {
         // In actual deployments, we use self-hosted Photon for reliability and 0 latency between servers
         PhotonNetwork.ConnectToMaster(this.photonAddress, this.photonPort, "192d4dcc-4d63-458d-ab68-69838b16f638", "v1");
         D.debug("Connecting to Photon self-hosted at; " + this.photonAddress + ", port: " + this.photonPort);
      }
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
