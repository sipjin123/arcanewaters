using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class PvpGame : MonoBehaviour {
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

   // How many players need to be on each team for the game to start
   public static int MIN_PLAYERS_PER_TEAM_TO_START = 3;

   // Transforms showing where the center of each lane is
   public Transform topLaneCenter, midLaneCenter, botLaneCenter;

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
         StartCoroutine(CO_AddPlayerToOngoingGame(player));
      }
   }

   private void addPlayerToPreGame (NetEntity player) {
      VoyageGroupManager.self.createGroup(player, voyageId, true);
      player.spawnInNewMap(voyageId, areaKey, Direction.South);
      D.debug("Adding player to pre-game. Added player: " + player.name);

      // If there are now enough players to start the game, start the game after a delay
      if (isGameReadyToBegin()) {
         StartCoroutine(CO_StartGame(delay: 10.0f));
      } else {
         int playersNeededToStart = (_numTeams * MIN_PLAYERS_PER_TEAM_TO_START) - getNumPlayers();
         sendGameMessage(player.nameText.text + " has joined the game. " + playersNeededToStart + " more players needed to begin!");
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
      player.pvpTeam = bestTeam;

      // If the team only has us, create a new voyage group with this player
      if (assignedTeam.Count == 1) {
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

   private IEnumerator CO_StartGame (float delay) {
      // For players who are currently in the game, create voyage groups for their teams, and add them to the groups
      _gameIsStarting = true;

      sendGameMessage("Game will begin in 10 seconds!");
      yield return new WaitForSeconds(delay);

      for (int i = 0; i < _usersInGame.Count; i++) {
         NetEntity player = EntityManager.self.getEntity(_usersInGame[i]);
         if (player.isDead()) {
            PlayerShipEntity playerShip = player.getPlayerShipEntity();
            if (playerShip) {
               playerShip.respawnPlayerInInstance();
            }
         }

         int teamIndex = i % _numTeams;
         int indexInTeam = i / _numTeams;
         PvpTeamType teamType = (PvpTeamType) (teamIndex + 1);
         PvpTeam team = _teams[teamType];
         team.Add(player.userId);
         player.currentHealth = player.maxHealth;
         player.pvpTeam = teamType;

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

      spawnStructures();
      _gameState = State.InGame;
      StartCoroutine(CO_SpawnWaves());
      sendGameMessage("Game has begun!");
   }

   private IEnumerator CO_SpawnWaves () {
      // Wait an initial delay at the start of the game
      yield return new WaitForSeconds(PvpManager.INITIAL_WAVE_DELAY);

      float spawnDelay = PvpManager.getShipyardSpawnDelay();

      // Begin the loop of spawning waves
      while (true) {
         // Spawn the first ship of the wave
         spawnShips();

         // Spawn the rest of the ships after a delay
         for (int i = 0; i < PvpManager.SHIPS_PER_WAVE - 1; i++) {
            yield return new WaitForSeconds(spawnDelay);
            spawnShips();
         }

         // Wait the interval between waves
         yield return new WaitForSeconds(PvpManager.WAVE_INTERVAL);
      }
   }

   private void spawnShips () {
      foreach (PvpShipyard shipyard in _shipyards) {
         if (shipyard && shipyard.gameObject && !shipyard.isDead())
         _ships.Add(shipyard.spawnShip());
      }
   }

   private void spawnStructures () {
      Instance instance = InstanceManager.self.getInstance(instanceId);
      Area area = AreaManager.self.getArea(instance.areaKey);

      PvpShipyard.SpawnLocation[] shipyardSpawnSides = { PvpShipyard.SpawnLocation.Top, PvpShipyard.SpawnLocation.Top, PvpShipyard.SpawnLocation.Right, PvpShipyard.SpawnLocation.Left, PvpShipyard.SpawnLocation.Bottom, PvpShipyard.SpawnLocation.Bottom };

      // We are spawning the structures into hard-coded positions for now, until we get them added to the map editor

      // Spawn in and setup the bases
      for (int i = 0; i < 2; i++) {
         PvpBase pvpBase = GameObject.Instantiate(PrefabsManager.self.pvpBasePrefab).GetComponent<PvpBase>();
         pvpBase.setAreaParent(area, true);
         pvpBase.transform.localPosition = PvpManager.self.baseSpawnPositions[i];
         pvpBase.areaKey = instance.areaKey;
         pvpBase.pvpTeam = (i == 0) ? PvpTeamType.A : PvpTeamType.B;
         pvpBase.onDeathAction += onStructureDestroyed;
         _bases.Add(pvpBase);

         InstanceManager.self.addSeaStructureToInstance(pvpBase, instance);
         NetworkServer.Spawn(pvpBase.gameObject);
      }

      // Spawn in and setup the towers
      for (int i = 0; i < 14; i++) {
         PvpTower tower = GameObject.Instantiate(PrefabsManager.self.pvpTowerPrefab).GetComponent<PvpTower>();
         tower.setAreaParent(area, true);
         tower.transform.localPosition = PvpManager.self.towerSpawnPositions[i];
         tower.areaKey = instance.areaKey;
         tower.pvpTeam = (i < 7) ? PvpTeamType.A : PvpTeamType.B;
         tower.onDeathAction += onStructureDestroyed;
         _towers.Add(tower);

         InstanceManager.self.addSeaStructureToInstance(tower, instance);
         NetworkServer.Spawn(tower.gameObject);
      }

      // Setup the shipyards
      for (int i = 0; i < 6; i++) {
         PvpShipyard shipyard = GameObject.Instantiate(PrefabsManager.self.pvpShipyardPrefab).GetComponent<PvpShipyard>();
         shipyard.setAreaParent(area, true);
         shipyard.transform.localPosition = PvpManager.self.shipyardSpawnPositions[i];
         shipyard.areaKey = instance.areaKey;
         shipyard.spawnLocation = shipyardSpawnSides[i];
         shipyard.pvpTeam = (i < 3) ? PvpTeamType.A : PvpTeamType.B;
         shipyard.laneType = (PvpLane) ((i % 3) + 1);
         shipyard.laneCenterTarget.localPosition = PvpManager.self.laneCenterPositions[(int) shipyard.laneType - 1] - shipyard.transform.localPosition;
         shipyard.onDeathAction += onStructureDestroyed;
         _shipyards.Add(shipyard);
      }

      // Setup the targetStructures for the shipyard, and then spawn them all in
      foreach (PvpShipyard shipyard in _shipyards) {
         shipyard.targetStructures = getTargetStructures(shipyard.pvpTeam, shipyard.laneType);
         InstanceManager.self.addSeaStructureToInstance(shipyard, instance);
         NetworkServer.Spawn(shipyard.gameObject);
      }

      setupStructureUnlocking();
   }

   private void setupStructureUnlocking () {
      // TODO: Set up a better way to do this

      // Team A, Top Lane
      getTower(PvpTowerType.TopA1).unlockAfterDeath = getTower(PvpTowerType.TopA2);
      getTower(PvpTowerType.TopA2).unlockAfterDeath = getShipyard(PvpTeamType.A, PvpLane.Top);
      getTower(PvpTowerType.TopA2).setIsInvulnerable(true);
      getShipyard(PvpTeamType.A, PvpLane.Top).unlockAfterDeath = getTower(PvpTowerType.BaseA);
      getShipyard(PvpTeamType.A, PvpLane.Top).setIsInvulnerable(true);

      // Team A, Mid Lane
      getTower(PvpTowerType.MidA1).unlockAfterDeath = getTower(PvpTowerType.MidA2);
      getTower(PvpTowerType.MidA2).unlockAfterDeath = getShipyard(PvpTeamType.A, PvpLane.Mid);
      getTower(PvpTowerType.MidA2).setIsInvulnerable(true);
      getShipyard(PvpTeamType.A, PvpLane.Mid).unlockAfterDeath = getTower(PvpTowerType.BaseA);
      getShipyard(PvpTeamType.A, PvpLane.Mid).setIsInvulnerable(true);

      // Team A, Bot Lane
      getTower(PvpTowerType.BotA1).unlockAfterDeath = getTower(PvpTowerType.BotA2);
      getTower(PvpTowerType.BotA2).unlockAfterDeath = getShipyard(PvpTeamType.A, PvpLane.Bot);
      getTower(PvpTowerType.BotA2).setIsInvulnerable(true);
      getShipyard(PvpTeamType.A, PvpLane.Bot).unlockAfterDeath = getTower(PvpTowerType.BaseA);
      getShipyard(PvpTeamType.A, PvpLane.Bot).setIsInvulnerable(true);

      // Team A, Base
      getTower(PvpTowerType.BaseA).unlockAfterDeath = getBase(PvpTeamType.A);
      getTower(PvpTowerType.BaseA).setIsInvulnerable(true);
      getBase(PvpTeamType.A).setIsInvulnerable(true);

      // Team B, Top Lane
      getTower(PvpTowerType.TopB1).unlockAfterDeath = getTower(PvpTowerType.TopB2);
      getTower(PvpTowerType.TopB2).unlockAfterDeath = getShipyard(PvpTeamType.B, PvpLane.Top);
      getTower(PvpTowerType.TopB2).setIsInvulnerable(true);
      getShipyard(PvpTeamType.B, PvpLane.Top).unlockAfterDeath = getTower(PvpTowerType.BaseB);
      getShipyard(PvpTeamType.B, PvpLane.Top).setIsInvulnerable(true);

      // Team B, Mid Lane
      getTower(PvpTowerType.MidB1).unlockAfterDeath = getTower(PvpTowerType.MidB2);
      getTower(PvpTowerType.MidB2).unlockAfterDeath = getShipyard(PvpTeamType.B, PvpLane.Mid);
      getTower(PvpTowerType.MidB2).setIsInvulnerable(true);
      getShipyard(PvpTeamType.B, PvpLane.Mid).unlockAfterDeath = getTower(PvpTowerType.BaseB);
      getShipyard(PvpTeamType.B, PvpLane.Mid).setIsInvulnerable(true);

      // Team B, Bot Lane
      getTower(PvpTowerType.BotB1).unlockAfterDeath = getTower(PvpTowerType.BotB2);
      getTower(PvpTowerType.BotB2).unlockAfterDeath = getShipyard(PvpTeamType.B, PvpLane.Bot);
      getTower(PvpTowerType.BotB2).setIsInvulnerable(true);
      getShipyard(PvpTeamType.B, PvpLane.Bot).unlockAfterDeath = getTower(PvpTowerType.BaseB);
      getShipyard(PvpTeamType.B, PvpLane.Bot).setIsInvulnerable(true);

      // Team B, Base
      getTower(PvpTowerType.BaseB).unlockAfterDeath = getBase(PvpTeamType.B);
      getTower(PvpTowerType.BaseB).setIsInvulnerable(true);
      getBase(PvpTeamType.B).setIsInvulnerable(true);
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

   private void sendGameMessage (string message, List<int> receivingPlayers = null) {
      // If players aren't specified, send the message to all players
      if (receivingPlayers == null) {
         receivingPlayers = _usersInGame;
      }

      sendGameMessageToPlayers(message, receivingPlayers);
   }

   private void sendGameMessageToPlayers (string message, List<int> receivingPlayers) {
      foreach (int userId in receivingPlayers) {
         NetEntity playerEntity = EntityManager.self.getEntity(userId);
         if (playerEntity && playerEntity.rpc) {
            playerEntity.rpc.Target_DisplayServerMessage(message);
         }
      }
   }

   public void forceStart () {
      if (!_gameIsStarting) {
         StartCoroutine(CO_StartGame(delay: 10.0f));
      }
   }

   private void onStructureDestroyed (SeaStructure structure) {
      if (structure is PvpBase) {
         PvpTeamType winningTeam = (structure.pvpTeam == PvpTeamType.A) ? PvpTeamType.B : PvpTeamType.A;
         onGameEnd(winningTeam);
      } else {
         sendGameMessage("Team " + structure.pvpTeam + "'s " + structure.GetType().ToString() + " has been destroyed.");
      }
   }

   private void onGameEnd (PvpTeamType winningTeam) {
      _gameState = State.PostGame;
      sendGameMessage("Team " + winningTeam.ToString() + " has won the game!");
      StopAllCoroutines();

      // Disable all sea structures
      foreach (PvpTower tower in _towers) {
         tower.gameObject.SetActive(false);
         NetworkServer.Destroy(tower.gameObject);
      }

      foreach (PvpShipyard shipyard in _shipyards) {
         shipyard.gameObject.SetActive(false);
         NetworkServer.Destroy(shipyard.gameObject);
      }

      foreach (PvpBase pvpBase in _bases) {
         pvpBase.gameObject.SetActive(false);
         NetworkServer.Destroy(pvpBase.gameObject);
      }

      foreach (BotShipEntity botShip in _ships) {
         if (botShip && botShip.gameObject) {
            NetworkServer.Destroy(botShip.gameObject);
         }
      }

      StartCoroutine(CO_PostGame());
   }

   private IEnumerator CO_PostGame () {
      yield return new WaitForSeconds(10.0f);
      sendGameMessage("This game will close in 20 seconds.");
      yield return new WaitForSeconds(20.0f);

      foreach (int userId in _usersInGame) {
         NetEntity player = EntityManager.self.getEntity(userId);

         if (player) {
            // If the player is in a voyage group, remove them from it
            if (player.tryGetGroup(out VoyageGroupInfo groupInfo)) {
               VoyageGroupManager.self.removeUserFromGroup(groupInfo, player.userId);
            }
         }

         Vector2 spawnPos = SpawnManager.self.getLocalPosition(Area.STARTING_TOWN, Spawn.STARTING_SPAWN);
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.setNewLocalPosition(userId, spawnPos, Direction.South, Area.STARTING_TOWN);
         });
      }
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
      int maxPlayers = _numTeams * Voyage.MAX_PLAYERS_PER_GROUP_PVP;
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
            if (playersOnTeam < leastPlayers && playersOnTeam < Voyage.MAX_PLAYERS_PER_GROUP_PVP) {
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

   private List<SeaStructure> getTargetStructures (PvpTeamType team, PvpLane lane) {
      if (team == PvpTeamType.A) {
         switch (lane) {
            case PvpLane.Top:
               return new List<SeaStructure>() { getTower(PvpTowerType.TopB1), getTower(PvpTowerType.TopB2), getShipyard(team, lane), getTower(PvpTowerType.BaseB) };
            case PvpLane.Mid:
               return new List<SeaStructure>() { getTower(PvpTowerType.MidB1), getTower(PvpTowerType.MidB2), getShipyard(team, lane), getTower(PvpTowerType.BaseB) };
            case PvpLane.Bot:
               return new List<SeaStructure>() { getTower(PvpTowerType.BotB1), getTower(PvpTowerType.BotB2), getShipyard(team, lane), getTower(PvpTowerType.BaseB) };
         }
      } else if (team == PvpTeamType.B) {
         switch (lane) {
            case PvpLane.Top:
               return new List<SeaStructure>() { getTower(PvpTowerType.TopA1), getTower(PvpTowerType.TopA2), getShipyard(team, lane), getTower(PvpTowerType.BaseA) };
            case PvpLane.Mid:
               return new List<SeaStructure>() { getTower(PvpTowerType.MidA1), getTower(PvpTowerType.MidA2), getShipyard(team, lane), getTower(PvpTowerType.BaseA) };
            case PvpLane.Bot:
               return new List<SeaStructure>() { getTower(PvpTowerType.BotA1), getTower(PvpTowerType.BotA2), getShipyard(team, lane), getTower(PvpTowerType.BaseA) };
         }
      }

      return null;
   }

   private PvpTower getTower (PvpTowerType type) {
      if (type == PvpTowerType.None) {
         return null;
      }
      
      return _towers[((int) type) - 1];
   }

   private PvpShipyard getShipyard (PvpTeamType teamType, PvpLane lane) {
      int laneNum = ((int) lane) - 1;
      int teamNum = ((int) teamType) - 1;
      int shipyardIndex = teamNum * 3 + laneNum;
      return _shipyards[shipyardIndex];
   }

   private PvpBase getBase (PvpTeamType teamType) {
      int baseIndex = ((int) teamType) - 1;
      return _bases[baseIndex];
   }

   public PvpTeamType getTeamForPlayer (int userId) {
      foreach (PvpTeamType teamType in _teams.Keys) {
         if (_teams.ContainsKey(teamType) && _teams[teamType].Contains(userId)) {
            return teamType;
         }
      }

      D.warning("PvpGame.getTeamForPlayer() couldn't find a team that the player was assigned to.");
      return PvpTeamType.None;
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

   // A list of references to all the shipyards in this game
   private List<PvpShipyard> _shipyards = new List<PvpShipyard>();

   // A list of references to all the towers in this game
   private List<PvpTower> _towers = new List<PvpTower>();

   // A list of references to all the bases in this game
   private List<PvpBase> _bases = new List<PvpBase>();

   // A list of all the bot ships in this game
   private List<BotShipEntity> _ships = new List<BotShipEntity>();

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

public enum PvpLane
{
   None = 0,
   Top = 1,
   Mid = 2,
   Bot = 3,
}

public enum PvpTowerType
{
   // Towers are named by their Lane + Team + Tier (Tier 1 tower is the first in the lane, closest to the enemy)
   None = 0,
   TopA1 = 1,
   TopA2 = 2,
   MidA1 = 3,
   MidA2 = 4,
   BotA1 = 5,
   BotA2 = 6,
   BaseA = 7,
   TopB1 = 8,
   TopB2 = 9,
   MidB1 = 10,
   MidB2 = 11,
   BotB1 = 12,
   BotB2 = 13,
   BaseB = 14,
}