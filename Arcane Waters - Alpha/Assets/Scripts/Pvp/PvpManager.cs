using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI.Messaging;

public class PvpManager : MonoBehaviour {
   #region Public Variables

   #region Temp positions for spawning in game elements

   // The positions to spawn each shipyard, in the order: Top_A, Mid_A, Bot_A, Top_B, Mid_B, Bot_B
   public List<Vector3> shipyardSpawnPositions;

   // The positions of the centers of each lane, in the order: Top, Mid, Bot
   public List<Vector3> laneCenterPositions;

   // The positions to spawn each tower, in the order: Top_A1, Top_A2, Mid_A1, Mid_A2, Bot_A1, Bot_A2, Base_A, Top_B1, Top_B2, Mid_B1, Mid_B2, Bot_B1, Bot_B2, Base_B
   public List<Vector3> towerSpawnPositions;

   // The positions to spawn each base, in the order: Base_A, Base_B
   public List<Vector3> baseSpawnPositions;

   #endregion

   #region Wave / spawning variables

   // How many ships will be spawned for each wave
   public static int SHIPS_PER_WAVE = 3;

   // The duration across which the ships are spawned
   public static float WAVE_SPAWNING_DURATION = 5.0f;

   // The interval inbetween waves
   public static float WAVE_INTERVAL = 25.0f;

   // The delay at the start of the game before waves are spawned
   public static float INITIAL_WAVE_DELAY = 20.0f;

   // The map we will use for pvp games
   public static string currentMap = "pvp_lanes_64";

   #endregion

   // A reference to the singleton instance of this class
   public static PvpManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void startPvpManagement () { 
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
   protected void createPvpGamesIfNeeded () {
      // Only the master server launches the creation of pvp instances
      NetworkedServer server = ServerNetworkingManager.self.server;
      if (server == null || !server.isMasterServer()) {
         return;
      }

      // Count the number of pvp instances in pre-game state in all servers
      int preGameCount = 0;
      foreach (Voyage pvpInstance in VoyageManager.self.getAllPvpInstances()) {
         if (pvpInstance.pvpGameState == PvpGame.State.PreGame) {
            preGameCount++;
         }
      }

      // If there are missing games, create one
      if (preGameCount < PRE_GAME_INSTANCES_COUNT) {
         VoyageManager.self.requestVoyageInstanceCreation(isPvP: true, difficulty: 2, biome: Biome.Type.Forest);
      }
   }

   [Server]
   public void createNewGameForPvpInstance (Instance instance) {
      GameObject newGameObject = Instantiate(new GameObject("Pvp Game " + instance.id), transform);
      PvpGame newGame = newGameObject.AddComponent<PvpGame>();
      newGame.init(instance.voyageId, instance.id, instance.areaKey);
      _activeGames[instance.id] = newGame;
   }

   [Server]
   private IEnumerator CO_CreateNewGameAndJoin (NetEntity player) {
      VoyageManager.self.requestVoyageInstanceCreation(isPvP: true, difficulty: 2, biome: Biome.Type.Forest);

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
      
      foreach (Voyage pvpInstance in VoyageManager.self.getAllPvpInstances()) {
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
         PvpTeamType playerTeam = _activeGames[instanceId].getTeamForPlayer(entity.userId);
         entity.pvpTeam = playerTeam;
      }
   }

   public void onPvpEntityDestroyed (NetEntity entity, NetEntity lastAttackerEntity) {
      Instance instance = lastAttackerEntity.getInstance();
      if (instance.isPvP) {
         if (lastAttackerEntity == Global.player && lastAttackerEntity != null && lastAttackerEntity.isPlayerShip()) {
            if (entity.TryGetComponent(out PvpTower tower) || entity.isPlayerShip() || entity.isBotShip() || entity.isSeaMonster()) {

               // Increase the silver rank of the attacker
               SilverManager.SilverInfo lastAttackerSilverInfo = SilverManager.self.getSilverInfo(lastAttackerEntity.userId);

               SilverManager.self.setSilverRank(lastAttackerEntity.userId, lastAttackerSilverInfo.rank + 1);

               // Add silver to the attacker
               int silverGained = SilverManager.SILVER_KILL_REWARD * lastAttackerSilverInfo.rank;

               // Take into account the silver rank of the destroyed entity
               if (entity.isPlayerShip()) {
                  SilverManager.SilverInfo entitySilverInfo = SilverManager.self.getSilverInfo(entity.userId);
                  if (entitySilverInfo != null) {
                     silverGained += Mathf.Max(entitySilverInfo.rank - 1, 0) * SilverManager.SILVER_KILL_REWARD;
                  }
               }
               SilverManager.self.setSilverAmount(lastAttackerEntity.userId, lastAttackerSilverInfo.amount + silverGained);
            }

            SilverManager.SilverInfo silverInfo = SilverManager.self.getSilverInfo();
            Global.player.rpc.Target_GetSilverInfo(silverInfo.amount, silverInfo.rank);
         }
         if (entity == Global.player && entity != null && entity.isPlayerShip()) {
            // Reset the silver rank of the destroyed player
            SilverManager.self.setSilverRank(entity.userId, 1);
         }
      }
   }

   #region Private Variables

   // After how long the game creation will time out
   private const double INSTANCE_CREATION_TIMEOUT = 1.0;

   // The number of pvp games that must always be available in pre-game state
   private const int PRE_GAME_INSTANCES_COUNT = 10;

   // A dictionary of all pvp games currently active on this server, indexed by instanceId
   Dictionary<int, PvpGame> _activeGames = new Dictionary<int, PvpGame>();

   #endregion
}
