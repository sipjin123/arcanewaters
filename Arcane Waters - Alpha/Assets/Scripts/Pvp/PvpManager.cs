using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI.Messaging;

public class PvpManager : MonoBehaviour {
   #region Public Variables

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
      VoyageManager.self.requestVoyageInstanceCreation(voyageId, areaKey, true);
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

      PvpGame newGame = new PvpGame();
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

   #region Private Variables

   // After how long the game creation will time out
   private const double INSTANCE_CREATION_TIMEOUT = 30.0;

   // A dictionary of all pvp games currently active on this server, indexed by instanceId
   Dictionary<int, PvpGame> _activeGames = new Dictionary<int, PvpGame>();

   // Set to true when we are in the process of creating a pvp game
   private bool _creatingPvpGame = false;

   #endregion
}
