using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class PvpGame {
   #region Public Variables

   // A PvpTeam is a list of users in the team, by userId
   public class PvpTeam : List<int> {
      // What type of pvp team this is
      public PvpTeamType teamType = PvpTeamType.None;
   };

   // A state describing what stage of the game we are at
   public enum State { None = 0, PreGame = 1, InGame = 2, PostGame = 3 }

   // The id of the voyage that this pvp game is in
   public int voyageId;

   // The id of the instance that this pvp game is in
   public int instanceId;

   // The key for the area this pvp game is in
   public string areaKey;

   // The max number of players allowed on each team
   public static int MAX_PLAYERS_PER_TEAM = 6;

   // How many players need to be on each team for the game to start
   public static int MIN_PLAYERS_PER_TEAM_TO_START = 2;

   #endregion

   public void init (int voyageId, int instanceId, string areaKey) {
      this.voyageId = voyageId;
      this.instanceId = instanceId;
      this.areaKey = areaKey;
      _gameState = State.PreGame;

      // Set the number of teams based on the map spawns
      SpawnManager.MapSpawnData mapSpawnData = SpawnManager.self.getAllMapSpawnData(areaKey);
      if (mapSpawnData == null) {
         D.error("Couldn't find spawn data for area: " + areaKey);
         return;
      } else {
         _numTeams = mapSpawnData.spawns.Count;
      }

      for (int i = 0; i < _numTeams; i++) {
         PvpTeamType teamType = (PvpTeamType) (i + 1);
         _teams[teamType] = new PvpTeam();
         _teams[teamType].teamType = teamType;
      }
   }

   public void addPlayerToGame (NetEntity player) {
      // Check that the game is in a valid state to add the player
      if (_gameState == PvpGame.State.None || _gameState == PvpGame.State.PostGame) {
         D.error("Failed to add player: " + player.name + " to game, game was in an invalid state");
         return;
      }

      // Check that there is still space in the game
      if (getAvailableSlots() <= 0) {
         D.warning("Failed to add player: " + player.name + " to the game, it was full");
         return;
      }

      // If the player is in a voyage group, remove them from it
      if (player.tryGetGroup(out VoyageGroupInfo groupInfo)) {
         VoyageGroupManager.self.removeUserFromGroup(groupInfo, player.userId);
      }

      _usersInGame.Add(player.userId);

      if (_gameState == State.PreGame) {
         addPlayerToPreGame(player);
      } else if (_gameState == State.InGame) {
         PvpManager.self.StartCoroutine(CO_AddPlayerToOngoingGame(player));
      }
   }

   private void addPlayerToPreGame (NetEntity player) {
      VoyageGroupManager.self.createGroup(player, voyageId, true);
      player.spawnInNewMap(voyageId, areaKey, Direction.South);
      D.debug("Adding player to pre-game. Added player: " + player.name);

      // If there are now enough players to start the game, start the game after a delay
      if (isGameReadyToBegin()) {
         PvpManager.self.StartCoroutine(CO_StartGame(delay: 10.0f));
      }
   }

   private IEnumerator CO_AddPlayerToOngoingGame (NetEntity player) {
      PvpTeamType bestTeam = getBestTeamWithSpace();

      // Check if any team was found
      if (bestTeam == PvpTeamType.None) {
         D.error("Couldn't find a team for player: " + player.name);
         yield break;
      }

      PvpTeam assignedTeam = _teams[bestTeam];
      assignedTeam.Add(player.userId);

      // If the team is empty, create a new voyage group with this player
      if (assignedTeam.Count == 0) {
         VoyageGroupManager.self.createGroup(player, voyageId, true);
         VoyageGroupInfo newGroup;

         // Wait for group to be created
         while (!player.tryGetGroup(out newGroup)) {
            yield return null;
         }

         // Store the team's voyage group id
         _teamVoyageGroupIds[bestTeam] = newGroup.groupId;
         D.debug("Adding player to game in-progress. Added player: " + player.name + " to team: " + bestTeam.ToString() + ", and created group");

      // If the team isn't empty, add the player to the existing voyage group
      } else {
         if (VoyageGroupManager.self.tryGetGroupById(_teamVoyageGroupIds[bestTeam], out VoyageGroupInfo voyageGroup)) {
            VoyageGroupManager.self.addUserToGroup(voyageGroup, player);
            D.debug("Adding player to game in-progress. Added player: " + player.name + " to team: " + bestTeam.ToString());
         } else {
            D.error("Couldn't find the voyage group for team: " + bestTeam.ToString());
            yield break;
         }
      }
      string teamSpawn = getSpawnForTeam(bestTeam);
      player.spawnInNewMap(voyageId, areaKey, teamSpawn, Direction.South);
   }

   public IEnumerator CO_StartGame (float delay) {
      // For players who are currently in the game, create voyage groups for their teams, and add them to the groups
      _gameIsStarting = true;

      D.debug("Starting PVP game in " + delay + "seconds");
      yield return new WaitForSeconds(delay);
      D.debug("Starting PVP game now!");

      for (int i = 0; i < _usersInGame.Count; i++) {
         NetEntity player = EntityManager.self.getEntity(_usersInGame[i]);
         int teamIndex = i % _numTeams;
         int indexInTeam = i / _numTeams;
         PvpTeamType teamType = (PvpTeamType) (teamIndex + 1);
         PvpTeam team = _teams[teamType];
         team.Add(player.userId);
         player.currentHealth = player.maxHealth;

         // If a voyage group doesn't exist for the team, create it with this player
         if (!_teamVoyageGroupIds.ContainsKey(team.teamType)) {
            VoyageGroupManager.self.createGroup(player, voyageId, true);
            VoyageGroupInfo newGroup;

            // Wait for group to be created
            while (!player.tryGetGroup(out newGroup)) {
               yield return null;
            }
            _teamVoyageGroupIds[team.teamType] = newGroup.groupId;
            D.debug("Player: " + player.name + " created group for team: " + team.teamType.ToString());

            // If a voyage group does exist for the team, add the player to it
         } else {
            if (VoyageGroupManager.self.tryGetGroupById(_teamVoyageGroupIds[team.teamType], out VoyageGroupInfo newGroup)) {
               VoyageGroupManager.self.addUserToGroup(newGroup, player);
               D.debug("Adding player: " + player.name + " to team: " + team.teamType.ToString());
            }
         }

         // Move the player to their spawn position, with a small offset so players aren't stacked on eachother.
         Vector2 spawnPosition = getSpawnPositionForTeam(team.teamType);
         spawnPosition += (Vector2.one * 0.5f).Rotate(45.0f * indexInTeam);
         Util.setLocalXY(player.transform, spawnPosition);
      }

      _gameState = State.InGame;
   }

   public void removePlayerFromGame (NetEntity player) {
      // Remove the player from the _teams dictionary
      PvpTeamType userTeam = getTeamForUser(player.userId);
      if (userTeam != PvpTeamType.None) {
         _teams[userTeam].Remove(player.userId);
      }

      VoyageGroupInfo voyageGroup;
      if (player.tryGetGroup(out voyageGroup)) {
         // If the player was the only person in their team, remove their voyage group id from _teamVoyageGroupIds   
         if (voyageGroup.members.Count == 1) {
            _teamVoyageGroupIds.Remove(userTeam);
         }

         // Remove the player from their voyage group
         VoyageGroupManager.self.removeUserFromGroup(voyageGroup, player.userId);
      }

      // Remove the player from _usersInGame
      _usersInGame.Remove(player.userId);
   }

   public Vector3 getSpawnPositionForUser (PlayerShipEntity playerShip) {
      if (_gameState == State.InGame) {
         return getSpawnPositionForTeam(getTeamForUser(playerShip.userId));
      } else {
         return getSpawnPositionForTeam(PvpTeamType.A);
      }
   }

   public State getGameState () {
      return _gameState;
   }

   public int getAvailableSlots () {
      int maxPlayers = _numTeams * MAX_PLAYERS_PER_TEAM;
      return maxPlayers - getNumPlayers();
   }

   public int getNumPlayers () {
      return _usersInGame.Count;
   }

   public bool isGameReadyToBegin () {
      // Only check games in the pre-game state
      if (_gameState != State.PreGame || _gameIsStarting) {
         return false;
      }

      // Check if there are enough players to form teams of minimum size
      int playersNeeded = _numTeams * MIN_PLAYERS_PER_TEAM_TO_START;

      return (getNumPlayers() >= playersNeeded);
   }

   private string getSpawnForTeam (PvpTeamType teamType) {
      int teamIndex = (int)(teamType) - 1;
      SpawnManager.MapSpawnData mapSpawnData = SpawnManager.self.getAllMapSpawnData(areaKey);

      return mapSpawnData.spawns.Values.ElementAt(teamIndex).name;
   }

   private Vector3 getSpawnPositionForTeam (PvpTeamType teamType) {
      int teamIndex = (int) (teamType) - 1;
      SpawnManager.MapSpawnData mapSpawnData = SpawnManager.self.getAllMapSpawnData(areaKey);

      return mapSpawnData.spawns.Values.ElementAt(teamIndex).localPosition;
   }

   private PvpTeamType getTeamForUser (int userId) {
      foreach (PvpTeam team in _teams.Values) {
         if (team.Contains(userId)) {
            return team.teamType;
         }
      }

      return PvpTeamType.None;
   }

   private PvpTeamType getBestTeamWithSpace () {
      PvpTeamType bestTeam = PvpTeamType.None;
      int leastPlayers = 1000;

      // Find the team with the least number of players, that still has space
      for (int i = 0; i < _numTeams; i++) {
         PvpTeamType teamType = (PvpTeamType) (i + 1);
         if (_teams.ContainsKey(teamType)) {
            int playersOnTeam = _teams[teamType].Count;
            if (playersOnTeam < leastPlayers && playersOnTeam < MAX_PLAYERS_PER_TEAM) {
               bestTeam = teamType;
               leastPlayers = playersOnTeam;
            }
         }
      }

      return bestTeam;
   }

   public bool containsUser (int userId) {
      return _usersInGame.Contains(userId);
   }

   #region Private Variables

   // A dictionary of all the teams in this pvp game
   private Dictionary<PvpTeamType, PvpTeam> _teams = new Dictionary<PvpTeamType, PvpTeam>();

   // A dictionary of the voyage group ids for each team
   private Dictionary<PvpTeamType, int> _teamVoyageGroupIds = new Dictionary<PvpTeamType, int>();

   // A list of all the users in this pvp game, by userId
   private List<int> _usersInGame = new List<int>();

   // How many teams are in this Pvp game
   private int _numTeams;

   // What state the game is currently in
   private State _gameState;

   // Set to true when the game is starting
   private bool _gameIsStarting = false;

   #endregion
}

// An enum to represent pvp teams 1-4
public enum PvpTeamType
{
   None = 0,
   A = 1,
   B = 2,
   C = 3,
   D = 4,
}