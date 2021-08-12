using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI.Messaging;
using MapCreationTool.Serialization;
using System;

[Serializable]
public class PvpAnnouncementClass {
   // The state of the pvp session
   public PvpGame.State pvpState;

   // The pvp id
   public int pvpId;

   // The instance id this pvp belongs to
   public int instanceId;

   // Last time the announcement has been triggered
   public DateTime lastAnnouncementTime;

   // The timestamp per player that receives the announcement
   public Dictionary<int, DateTime> playerTimeStamp = new Dictionary<int, DateTime>();
}

public class PvpManager : MonoBehaviour {
   #region Public Variables

   // List of pvp announcement data
   public List<PvpAnnouncementClass> pvpAnnouncementDataList = new List<PvpAnnouncementClass>();

   // If pvp maps are initialized
   public bool pvpMapsInitialized;

   #region Wave / spawning variables

   // How many ships will be spawned for each wave
   public static int SHIPS_PER_WAVE = 3;

   // The duration across which the ships are spawned
   public static float WAVE_SPAWNING_DURATION = 5.0f;

   // The interval inbetween waves
   public static float WAVE_INTERVAL = 35.0f;

   // The delay at the start of the game before waves are spawned
   public static float INITIAL_WAVE_DELAY = 20.0f;

   // The map we will use for pvp games
   public static string currentMap = "pvp_lanes_64";

   #endregion

   // A reference to the singleton instance of this class
   public static PvpManager self;

   // Interval between announcements in minutes
   public const float ANNOUNCEMENT_INTERVAL = 5f;

   // Interval between announcement checks in seconds
   public const float ANNOUNCEMENT_CHECK_INTERVAL = 1;

   #endregion

   private void Awake () {
      self = this;
   }

   private void checkPvpForAnnouncement () {
      if (!pvpMapsInitialized) {
         return;
      }

      foreach (PvpAnnouncementClass pvpAnnouncement in pvpAnnouncementDataList) {
         double announcementInterval = DateTime.UtcNow.Subtract(pvpAnnouncement.lastAnnouncementTime).TotalMinutes;

         // If the pvp game is not a pregame or the announcement interval is less than the expected, skip
         if (pvpAnnouncement.pvpState != PvpGame.State.PreGame) {
            continue;
         }

         // Reset the timer
         pvpAnnouncement.lastAnnouncementTime = DateTime.UtcNow;

         if (_activeGames.ContainsKey(pvpAnnouncement.instanceId)) {
            PvpGame activePvpGame = _activeGames[pvpAnnouncement.instanceId];
            pvpAnnouncement.pvpState = activePvpGame.getGameState();
            if (activePvpGame.getAllUsersInGame().Count > 0) {
               foreach (NetworkedServer currServer in ServerNetworkingManager.self.servers) {
                  foreach (KeyValuePair<int, AssignedUserInfo> userAssignedInfo in currServer.assignedUserIds) {
                     // Only send the message to the players that are outside the pvp game
                     if (activePvpGame.getAllUsersInGame().Contains(userAssignedInfo.Key)) {
                        // Remove user from recipient of pvp announcement if they are already in the pvp game
                        if (pvpAnnouncement.playerTimeStamp.ContainsKey(userAssignedInfo.Key)) {
                           pvpAnnouncement.playerTimeStamp.Remove(userAssignedInfo.Key);
                        }
                        continue;
                     }

                     string mapName = AreaManager.self.getMapInfo(activePvpGame.areaKey).displayName;
                     string message = "A battle is breaking out in " + mapName + "!  Click here to take part!";

                     // Check if user is already registered, if so then check if time stamp meets the required announcement interval
                     if (pvpAnnouncement.playerTimeStamp.ContainsKey(userAssignedInfo.Key)) {
                        double playerAnnouncementInterval = DateTime.UtcNow.Subtract(pvpAnnouncement.playerTimeStamp[userAssignedInfo.Key]).TotalMinutes;
                        if (playerAnnouncementInterval < ANNOUNCEMENT_INTERVAL) {
                           continue;
                        } else {
                           pvpAnnouncement.playerTimeStamp[userAssignedInfo.Key] = DateTime.UtcNow;

                           ChatInfo newChatInfo = new ChatInfo {
                              senderId = pvpAnnouncement.instanceId,
                              text = message,
                              sender = mapName,
                              recipient = userAssignedInfo.Key.ToString()
                           };
                           ServerNetworkingManager.self?.sendDirectChatMessage(newChatInfo);
                        }
                     } else {
                        // Register user and send message announcing the new pvp
                        pvpAnnouncement.playerTimeStamp.Add(userAssignedInfo.Key, DateTime.UtcNow);

                        ChatInfo newChatInfo = new ChatInfo {
                           senderId = pvpAnnouncement.instanceId,
                           text = message,
                           sender = mapName,
                           recipient = userAssignedInfo.Key.ToString()
                        };
                        ServerNetworkingManager.self?.sendDirectChatMessage(newChatInfo);
                     }
                  }
               }
            }
         }
      }
   }

   public void startPvpManagement () {
      // At server startup, for dev builds, make all the pvp arena maps accessible by creating one instance for each
      if (!Util.isCloudBuild()) {
         StartCoroutine(CO_CreateInitialPvpArenas());
      }

      // Regularly check that there are enough pvp games and create more if needed
      InvokeRepeating(nameof(createPvpGamesIfNeeded), 5f, 10f);
   }

   [Server]
   public void joinBestPvpGameOrCreateNew (NetEntity player) {
      Voyage bestGameInstance = getBestJoinableGameInstance();

      // If there are no active games, create a game
      if (bestGameInstance == null) {
         StartCoroutine(CO_CreateNewGameAndJoin(player));

      // If there are active games, join the one with the highest number of players, that isn't in session
      } else {
         ServerNetworkingManager.self.joinPvpGame(bestGameInstance.voyageId, player.userId, player.entityName, PvpTeamType.None);
      }
   }

   [Server]
   public void joinPvpGame (int voyageId, int userId, string userName, PvpTeamType team) {
      Voyage voyage;
      if (!VoyageManager.self.tryGetVoyage(voyageId, out voyage)) {
         ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. The game does not exist.");
         return;
      }

      PvpGame pvpGame = getGameWithInstance(voyage.instanceId);
      if (pvpGame == null) {
         ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. The game does not exist.");
         return;
      }

      addPlayerToGame(voyage.instanceId, userId, userName, team);
   }

   [Server]
   private IEnumerator CO_CreateInitialPvpArenas () {
      // Wait until our server is defined
      while (ServerNetworkingManager.self == null || ServerNetworkingManager.self.server == null) {
         yield return null;
      }

      // Wait until our server port is initialized
      while (ServerNetworkingManager.self.server.networkedPort.Value == 0) {
         yield return null;
      }

      // Only the master server launches the creation of pvp instances
      if (ServerNetworkingManager.self.server.isMasterServer()) {

         // Create a pvp instance for each available arena map
         List<string> pvpArenaMaps = VoyageManager.self.getPvpArenaAreaKeys();
         foreach (string areaKey in pvpArenaMaps) {
            Map areaData = AreaManager.self.getMapInfo(areaKey);
            VoyageManager.self.requestVoyageInstanceCreation(areaKey, true, difficulty: 2, biome: areaData == null ? Biome.Type.Forest : areaData.biome);
         }
      }

      pvpMapsInitialized = true;
      InvokeRepeating(nameof(checkPvpForAnnouncement), 1, ANNOUNCEMENT_CHECK_INTERVAL);
   }

   [Server]
   protected void createPvpGamesIfNeeded () {
      // Only the master server launches the creation of pvp instances
      NetworkedServer server = ServerNetworkingManager.self.server;
      if (server == null || !server.isMasterServer()) {
         return;
      }

      // Count the number of open pvp instances in all servers
      int openGameCount = 0;
      foreach (Voyage pvpInstance in VoyageManager.self.getAllPvpInstances()) {
         if (PvpGame.canGameBeJoined(pvpInstance)) {
            openGameCount++;
         }
      }

      // If there are missing games, create one
      if (openGameCount < OPEN_GAME_INSTANCES_COUNT) {
         List<string> pvpArenaAreaKeys = VoyageManager.self.getPvpArenaAreaKeys();

         if (pvpArenaAreaKeys.Count == 0) {
            D.error("Cannot create pvp arena instances! There are no pvp arena maps available!");
            return;
         }

         // Rotate through the pvp arena maps
         lastPvpArenaAreaIndex++;
         if (lastPvpArenaAreaIndex >= pvpArenaAreaKeys.Count) {
            lastPvpArenaAreaIndex = 0;
         }
         string areaKey = pvpArenaAreaKeys[lastPvpArenaAreaIndex];
         Map areaData = AreaManager.self.getMapInfo(areaKey);
         VoyageManager.self.requestVoyageInstanceCreation(areaKey, true, difficulty: 2, biome: areaData == null ? Biome.Type.Forest : areaData.biome);
      }
   }

   [Server]
   public void createNewGameForPvpInstance (Instance instance) {
      GameObject newGameObject = Instantiate(new GameObject("Pvp Game " + instance.id), transform);
      PvpGame newGame = newGameObject.AddComponent<PvpGame>();
      newGame.init(instance.voyageId, instance.id, instance.areaKey);
      _activeGames[instance.id] = newGame;

      pvpAnnouncementDataList.Add(new PvpAnnouncementClass {
         instanceId = instance.id,
         pvpState = PvpGame.State.PreGame,
         lastAnnouncementTime = DateTime.UtcNow,
         pvpId = instance.voyageId
      });
   }

   [Server]
   private IEnumerator CO_CreateNewGameAndJoin (NetEntity player) {
      // Create a random voyage instance biome
      VoyageManager.self.requestVoyageInstanceCreation(isPvP: true, difficulty: 2, biome: (Biome.Type) UnityEngine.Random.Range(1, 6));

      // The voyage instance creation always takes at least two frames
      yield return null;
      yield return null;

      // Wait until the voyage and game have been created
      Voyage voyage = null;
      double instanceCreationStartTime = NetworkTime.time;
      while (voyage == null) {
         // Check if the creation has timed out
         double elapsedCreationTime = NetworkTime.time - instanceCreationStartTime;
         if (elapsedCreationTime >= INSTANCE_CREATION_TIMEOUT) {
            if (player != null) {
               ServerMessageManager.sendError(ErrorMessage.Type.PvpJoinError, player, "Could not join the pvp game. The instance creation timed out.");
            }
            yield break;
         }

         voyage = getBestJoinableGameInstance();
         yield return null;
      }

      // Make the player join the game
      if (player != null) {
         ServerNetworkingManager.self.joinPvpGame(voyage.voyageId, player.userId, player.entityName, PvpTeamType.None);
      }
   }

   private Voyage getBestJoinableGameInstance () {
      // We will find the game with the highest number of players, that hasn't yet started
      Voyage bestGameInstance = null;
      int mostPlayers = 0;

      List<Voyage> voyages = VoyageManager.self.getAllPvpInstances();

      #if UNITY_EDITOR

      if (voyages == null) {
         return bestGameInstance;
      }

      // Try searching for the chosen map
      bestGameInstance = voyages.Find(_ => _.areaKey == currentMap || _.areaName == currentMap);
      
      if (bestGameInstance != null) {
         if (bestGameInstance.pvpGameState == PvpGame.State.InGame || bestGameInstance.pvpGameState == PvpGame.State.PreGame) {
            return bestGameInstance;
         }
      }

      #endif

      foreach (Voyage pvpInstance in voyages) {
         // Ignore games that are in post-game / invalid
         if (pvpInstance.pvpGameState == PvpGame.State.None || pvpInstance.pvpGameState == PvpGame.State.PostGame) {
            continue;
         }

         // If this game has a free spot, and has more players than the previous best, store it as the best
         int numPlayers = pvpInstance.playerCount;
         if (pvpInstance.pvpGameMaxPlayerCount - pvpInstance.playerCount > 0 && numPlayers >= mostPlayers) {
            bestGameInstance = pvpInstance;
            mostPlayers = numPlayers;
         }
      }

      return bestGameInstance;
   }

   private void addPlayerToGame (int instanceId, int userId, string userName, PvpTeamType team) {      
      PvpGame game;
      if (!_activeGames.TryGetValue(instanceId, out game)) {
         D.error("Failed to add player: " + userName + " to game, game didn't exist in dictionary");
         ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. The game does not exist.");
         return;
      }

      game.addPlayerToGame(userId, userName, team);
   }

   public void onPlayerLoadedGameArea (int userId) {
      PvpGame game = getGameWithPlayer(userId);
      if (game) {
         game.onPlayerLoadedGameArea(userId);
      }
   }

   public PvpGame getGameWithPlayer (int playerUserId) {
      foreach (PvpGame activeGame in _activeGames.Values) {
         if (activeGame.containsUser(playerUserId)) {
            return activeGame;
         }
      }

      return null;
   }

   public PvpGame getGameWithPlayer (NetEntity player) {
      return getGameWithPlayer(player.userId);
   }

   public PvpGame getGameWithInstance (int instanceId) {
      if (_activeGames.ContainsKey(instanceId)) {
         return _activeGames[instanceId];
      }
      return null;
   }

   public void tryRemoveEmptyGame (int instanceId) {
      // If the game exists, remove it
      if (_activeGames.TryGetValue(instanceId, out PvpGame emptyGame)) {
         if (emptyGame && emptyGame.gameObject) {
            Destroy(emptyGame.gameObject);
         }
         _activeGames.Remove(instanceId);
      }
   }

   public static float getShipyardSpawnDelay () {
      if (SHIPS_PER_WAVE <= 1) {
         D.error("Ships per wave must be greater than one!");
         return 0.0f;
      }
      return (WAVE_SPAWNING_DURATION / (SHIPS_PER_WAVE - 1));
   }

   public void assignPvpTeam (NetEntity entity, int instanceId) {
      if (_activeGames.ContainsKey(instanceId)) {
         PvpTeamType playerTeam = _activeGames[instanceId].getTeamForUser(entity.userId);
         entity.pvpTeam = playerTeam;
      }
   }

   public static string getFlagPaletteForTeam (PvpTeamType teamType) {
      switch (teamType) {
         case PvpTeamType.A:
            return "flag_naturalists";
         case PvpTeamType.B:
            return "flag_privateers";
         default:
            return "flag_white";
      }
   }

   public static string getShipPaletteForTeam (PvpTeamType teamType) {
      switch (teamType) {
         case PvpTeamType.A:
            return "ship_flag_naturalists";
         case PvpTeamType.B:
            return "ship_flag_privateers";
         default:
            return "ship_flag_white";
      }
   }

   public static string getStructurePaletteForTeam (PvpTeamType teamType) {
      switch (teamType) {
         case PvpTeamType.A:
            return "structure_naturalists_outline, structure_naturalists_fill";
         case PvpTeamType.B:
            return "structure_privateers_outline, structure_privateers_fill";
         default:
            return "structure_white_outline, structure_white_fill";
      }
   }

   #region Private Variables

   // After how long the game creation will time out
   private const double INSTANCE_CREATION_TIMEOUT = 1.0;

   // The number of pvp games that must always be available
   private const int OPEN_GAME_INSTANCES_COUNT = 3;

   // A dictionary of all pvp games currently active on this server, indexed by instanceId
   Dictionary<int, PvpGame> _activeGames = new Dictionary<int, PvpGame>();

   // The index of the last area that was used to create a pvp arena
   private int lastPvpArenaAreaIndex = -1;

   #endregion
}
