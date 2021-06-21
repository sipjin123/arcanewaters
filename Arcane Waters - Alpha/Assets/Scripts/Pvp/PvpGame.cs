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

   // The pvp stats
   public PvpStatsData pvpStatData = new PvpStatsData();

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

      pvpStatData.playerStats = new List<PvpPlayerStat>();
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

         // Generate stat data for this player
         pvpStatData.playerStats.Add(new PvpPlayerStat(player.userId, (int) teamType));

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
      pvpStatData.isInitialized = true;

      detectStructures();
      setStructuresActivated(true);

      _gameState = State.InGame;
      StartCoroutine(CO_SpawnSeamonsters());
      StartCoroutine(CO_SpawnWaves());
   }

   private IEnumerator CO_SpawnSeamonsters () {
      Instance instance = InstanceManager.self.getInstance(instanceId);
      while (instance.pvpMonsterSpawners.Count < 1) {
         yield return 0;
      }

      foreach (PvpMonsterSpawner monsterSpawner in instance.pvpMonsterSpawners) {
         monsterSpawner.instanceId = instanceId;
         monsterSpawner.initializeSpawner();
      }
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

   private void detectStructures () {
      Instance instance = InstanceManager.self.getInstance(instanceId);

      // Count how many towers we have first, to assist in registering towers
      int numTowers = 0;
      foreach (SeaStructure structure in instance.seaStructures) {
         if (structure is PvpTower) {
            numTowers++;
         }
      }

      // We are currently assuming we'll have X towers per lane, plus one base tower each. So our total is 6 * X + 2.
      // Check if we have an appropriate number of towers
      if ((numTowers - 2) % 6 == 0) {
         _towersPerLane = (numTowers - 2) / 6;
         
         // Fill the lists with null for now, so we can access elements directly
         int structuresPerLane = 3 + _towersPerLane;
         for (int i = 0; i < structuresPerLane; i++) {
            _topStructuresA.Add(null);
            _midStructuresA.Add(null);
            _botStructuresA.Add(null);
            _topStructuresB.Add(null);
            _midStructuresB.Add(null);
            _botStructuresB.Add(null);
         }
      } else {
         D.error("This map doesn't have the correct number of towers. Aborting structure detection.");
         return;
      }

      // Register the sea structures in their appropriate places
      foreach (SeaStructure structure in instance.seaStructures) {
         if (structure is PvpTower) {
            registerTower((PvpTower) structure);
         } else if (structure is PvpShipyard) {
            registerShipyard((PvpShipyard) structure);
         } else if (structure is PvpBase) {
            registerBase((PvpBase) structure);
         }

         _allStructures.Add(structure);
      }

      // Setup the invlunerability and structure unlocking
      setupStructureUnlocking(_topStructuresA);
      setupStructureUnlocking(_midStructuresA);
      setupStructureUnlocking(_botStructuresA);
      setupStructureUnlocking(_topStructuresB);
      setupStructureUnlocking(_midStructuresB);
      setupStructureUnlocking(_botStructuresB);

      PvpWaypoint topWaypoint = instance.pvpWaypoints.Find((x) => x.lane == PvpLane.Top);
      PvpWaypoint midWaypoint = instance.pvpWaypoints.Find((x) => x.lane == PvpLane.Mid);
      PvpWaypoint botWaypoint = instance.pvpWaypoints.Find((x) => x.lane == PvpLane.Bot);

      if (!topWaypoint && !midWaypoint && !botWaypoint) {
         D.error("Couldn't find all Pvp waypoints of types: Top, Mid and Bot, check the map in the editor.");
         return;
      }

      // Setup the targetStructures for the shipyard, and then spawn them all in
      foreach (SeaStructure structure in instance.seaStructures) {
         if (structure is PvpShipyard) {
            PvpShipyard shipyard = (PvpShipyard) structure;
            PvpTeamType otherTeam = (shipyard.pvpTeam == PvpTeamType.A) ? PvpTeamType.B : PvpTeamType.A;
            shipyard.targetStructures = getStructures(otherTeam, shipyard.laneType);

            Vector3 waypointPosition;

            switch (shipyard.laneType) {
               case PvpLane.Top:
                  waypointPosition = topWaypoint.transform.localPosition;
                  break;
               case PvpLane.Mid:
                  waypointPosition = midWaypoint.transform.localPosition;
                  break;
               case PvpLane.Bot:
                  waypointPosition = botWaypoint.transform.localPosition;
                  break;
               default:
                  waypointPosition = Vector3.zero;
                  break;
            }
            shipyard.laneCenterTarget.localPosition = waypointPosition - shipyard.transform.localPosition;
         }
      }
   }

   private void setupStructureUnlocking (List<SeaStructure> laneStructures) {
      for (int i = 0; i < laneStructures.Count; i++) {
         SeaStructure structure = laneStructures[i];

         // All structures other than the first tower in each lane should be invulnerable
         if (i > 0) {
            structure.setIsInvulnerable(true);
         }

         // If there is a structure after this in the list, set it as the unlockondeath target
         if (i + 1 != laneStructures.Count) {
            SeaStructure nextStructure = laneStructures[i + 1];
            structure.unlockAfterDeath = nextStructure;
         }
      }
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

      destroyStructures(_topStructuresA);
      destroyStructures(_midStructuresA);
      destroyStructures(_botStructuresA);
      destroyStructures(_topStructuresB);
      destroyStructures(_midStructuresB);
      destroyStructures(_botStructuresB);

      foreach (BotShipEntity botShip in _ships) {
         if (botShip && botShip.gameObject) {
            NetworkServer.Destroy(botShip.gameObject);
         }
      }

      StartCoroutine(CO_PostGame());
   }

   private void destroyStructures (List<SeaStructure> structures) {
      foreach (SeaStructure structure in structures) {
         structure.gameObject.SetActive(false);
         NetworkServer.Destroy(structure.gameObject);
      }
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
               D.log("MT: Removed player from voyage group.");
            }

            player.spawnInBiomeHomeTown();
         } else {
            Vector2 spawnPos = SpawnManager.self.getLocalPosition(Area.STARTING_TOWN, Spawn.STARTING_SPAWN);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               DB_Main.setNewLocalPosition(userId, spawnPos, Direction.South, Area.STARTING_TOWN);
               D.log("BT: Set player's position in the DB.");
            });
         }
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

   public List<int> getAllUsersInGame () {
      return _usersInGame;
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

   private List<SeaStructure> getStructures (PvpTeamType team, PvpLane lane) {
      if (team == PvpTeamType.A) {
         if (lane == PvpLane.Top) {
            return _topStructuresA;
         } else if (lane == PvpLane.Mid) {
            return _midStructuresA;
         } else if (lane == PvpLane.Bot) {
            return _botStructuresA;
         }
      } else if (team == PvpTeamType.B) {
         if (lane == PvpLane.Top) {
            return _topStructuresB;
         } else if (lane == PvpLane.Mid) {
            return _midStructuresB;
         } else if (lane == PvpLane.Bot) {
            return _botStructuresB;
         }
      }
      return null;
   }

   private void registerTower (PvpTower tower) {
      tower.onDeathAction += onStructureDestroyed;
      int indexInList = 0;

      if (tower.laneType == PvpLane.Base) {
         indexInList = _towersPerLane + 1;

         // Base towers are registered in all lanes, for bot ship targeting and structure invulnerability purposes
         getStructures(tower.pvpTeam, PvpLane.Top)[indexInList] = tower;
         getStructures(tower.pvpTeam, PvpLane.Mid)[indexInList] = tower;
         getStructures(tower.pvpTeam, PvpLane.Bot)[indexInList] = tower;
      } else {
         indexInList = tower.indexInLane;
         getStructures(tower.pvpTeam, tower.laneType)[indexInList] = tower;
      }
   }
   
   private void registerShipyard (PvpShipyard shipyard) {
      shipyard.onDeathAction += onStructureDestroyed;
      int indexInList = _towersPerLane;
      getStructures(shipyard.pvpTeam, shipyard.laneType)[indexInList] = shipyard;
      _shipyards.Add(shipyard);
   }

   private void registerBase (PvpBase pvpBase) {
      pvpBase.onDeathAction += onStructureDestroyed;
      int indexInList = _towersPerLane + 2;

      // Bases are registered in all lanes, for bot ship targeting and structure invulnerability purposes
      getStructures(pvpBase.pvpTeam, PvpLane.Top)[indexInList] = pvpBase;
      getStructures(pvpBase.pvpTeam, PvpLane.Mid)[indexInList] = pvpBase;
      getStructures(pvpBase.pvpTeam, PvpLane.Bot)[indexInList] = pvpBase;
   }

   private void setStructuresActivated (bool value) {
      foreach (SeaStructure structure in _allStructures) {
         structure.setIsActivated(value);
         structure.Rpc_SetIsActivated(value);
      }
   }

   public static PvpShipyard.SpawnLocation getSpawnSideForShipyard (PvpTeamType teamType, PvpLane lane) {
      if (teamType == PvpTeamType.A) {
         switch (lane) {
            case PvpLane.Top:
               return PvpShipyard.SpawnLocation.Top;
            case PvpLane.Mid:
               return PvpShipyard.SpawnLocation.TopRight;
            case PvpLane.Bot:
               return PvpShipyard.SpawnLocation.Right;
         }
      } else if (teamType == PvpTeamType.B) {
         switch (lane) {
            case PvpLane.Top:
               return PvpShipyard.SpawnLocation.Left;
            case PvpLane.Mid:
               return PvpShipyard.SpawnLocation.BottomLeft;
            case PvpLane.Bot:
               return PvpShipyard.SpawnLocation.Bottom;
         }
      }

      return PvpShipyard.SpawnLocation.None;
   }

   #region Pvp stat functions

   public void addPlayerKillCount (int userId) {
      PvpPlayerStat playerStat = pvpStatData.playerStats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerKills++;
      D.adminLog("Added player kills for: " + userId + " Total of: " + playerStat.playerKills, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addShipKillCount (int userId) {
      PvpPlayerStat playerStat = pvpStatData.playerStats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerShipKills++;
      D.adminLog("Added ship kills for: " + userId + " Total of: " + playerStat.playerShipKills, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addMonsterKillCount (int userId) {
      PvpPlayerStat playerStat = pvpStatData.playerStats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerMonsterKills++;
      D.adminLog("Added monster kills for: " + userId + " Total of: " + playerStat.playerMonsterKills, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addBuildingDestroyedCount (int userId) {
      PvpPlayerStat playerStat = pvpStatData.playerStats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerStructuresDestroyed++;
      D.adminLog("Added structure kills for: " + userId + " Total of: " + playerStat.playerStructuresDestroyed, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addAssistCount (int userId) {
      PvpPlayerStat playerStat = pvpStatData.playerStats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerAssists++;
      D.adminLog("Added assist count for: " + userId + " Total of: " + playerStat.playerAssists, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addDeathCount (int userId) {
      PvpPlayerStat playerStat = pvpStatData.playerStats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerDeaths++;
      D.adminLog("Added death count for: " + userId + " Total of: " + playerStat.playerDeaths, D.ADMIN_LOG_TYPE.Pvp);
   }

   #endregion

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

   // A list of references to the structures in each lane, for each team
   private List<SeaStructure> _topStructuresA = new List<SeaStructure>();
   private List<SeaStructure> _midStructuresA = new List<SeaStructure>();
   private List<SeaStructure> _botStructuresA = new List<SeaStructure>();   
   private List<SeaStructure> _topStructuresB = new List<SeaStructure>();
   private List<SeaStructure> _midStructuresB = new List<SeaStructure>();
   private List<SeaStructure> _botStructuresB = new List<SeaStructure>();

   // A list of references to all the structures in the game
   private List<SeaStructure> _allStructures = new List<SeaStructure>();

   // A list of all the bot ships in this game
   private List<BotShipEntity> _ships = new List<BotShipEntity>();

   // How many towers are in this game
   private int _towersPerLane = 0;

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
   Base = 4,
}