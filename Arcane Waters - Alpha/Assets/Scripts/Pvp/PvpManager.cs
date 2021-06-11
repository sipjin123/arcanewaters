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

   #endregion

   // A reference to the singleton instance of this class
   public static PvpManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   [Server]
   public void tryJoinPvpGame (NetEntity player) {
      PvpGame bestGame = getBestJoinableGame();

      // If there are no active games, create a game
      if (bestGame == null) {
         createNewGame(onCreationComplete: (instanceId) => {
            // Add the player to the game
            addPlayerToGame(instanceId, player);
         });

      // If there are active games, join the one with the highest number of players, that isn't in session
      } else {
         addPlayerToGame(bestGame.instanceId, player);
      }
   }

   [Server]
   public void createNewGame (string areaKey = "pvp_lanes", System.Action<int> onCreationComplete = null) {
      if (_creatingPvpGame) {
         D.warning("Can't create a new pvp game, one is already being created");
         return;
      }

      StartCoroutine(CO_CreateNewGame(areaKey, onCreationComplete));
   }

   private IEnumerator CO_CreateNewGame (string areaKey, System.Action<int> onCreationComplete) {
      _creatingPvpGame = true;
      
      Voyage newGameVoyage;

      // Get a new voyage id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageId();
      while (!response.IsDone) {
         yield return null;
      }
      int voyageId = response.Value;

      // Create an instance with that voyage id
      VoyageManager.self.requestVoyageInstanceCreation(voyageId, areaKey, true, difficulty: 2, biome: Biome.Type.Forest);
      double instanceCreationStartTime = NetworkTime.time;

      // Wait for the instance to be created
      while (!VoyageManager.self.tryGetVoyage(voyageId, out newGameVoyage)) {
         // Check if the creation has timed out
         double elapsedCreationTime = NetworkTime.time - instanceCreationStartTime;
         if (elapsedCreationTime >= INSTANCE_CREATION_TIMEOUT) {
            D.error("Voyage instance creation timed out for pvp game");
            _creatingPvpGame = false;
            yield break;
         }
         
         yield return null;
      }

      GameObject newGameObject = Instantiate(new GameObject("Pvp Game " + newGameVoyage.instanceId), transform);
      PvpGame newGame = newGameObject.AddComponent<PvpGame>();
      newGame.init(voyageId, newGameVoyage.instanceId, areaKey);
      _activeGames[newGameVoyage.instanceId] = newGame;

      _creatingPvpGame = false;

      onCreationComplete.Invoke(newGameVoyage.instanceId);
   }

   private PvpGame getBestJoinableGame () {
      // We will find the game with the highest number of players, that hasn't yet started
      PvpGame bestGame = null;
      int mostPlayers = 0;
      
      foreach (PvpGame game in _activeGames.Values) {
         // Ignore games that are in post-game / invalid
         PvpGame.State gameState = game.getGameState();
         if (gameState == PvpGame.State.None || gameState == PvpGame.State.PostGame) {
            continue;
         }

         // If this game has a free spot, and has more players than the previous best, store it as the best
         int numPlayers = game.getNumPlayers();
         if (game.getAvailableSlots() > 0 && numPlayers > mostPlayers) {
            bestGame = game;
            mostPlayers = numPlayers;
         }
      }

      return bestGame;
   }

   private void addPlayerToGame (int instanceId, NetEntity player) {      
      PvpGame game;
      if (!_activeGames.TryGetValue(instanceId, out game)) {
         D.error("Failed to add player: " + player.name + " to game, game didn't exist in dictionary");
         return;
      }

      game.addPlayerToGame(player);
   }

   public PvpGame getGameWithPlayer (NetEntity player) {
      foreach (PvpGame activeGame in _activeGames.Values) {
         if (activeGame.containsUser(player.userId)) {
            return activeGame;
         }
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

   #region Private Variables

   // After how long the game creation will time out
   private const double INSTANCE_CREATION_TIMEOUT = 30.0;

   // A dictionary of all pvp games currently active on this server, indexed by instanceId
   Dictionary<int, PvpGame> _activeGames = new Dictionary<int, PvpGame>();

   // Set to true when we are in the process of creating a pvp game
   private bool _creatingPvpGame = false;

   #endregion
}
