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

   public void addPlayerToGame (int userId, string userName, PvpTeamType teamType) {
      // Check that the game is in a valid state to add the player
      if (_gameState == PvpGame.State.None || _gameState == PvpGame.State.PostGame) {
         D.error("Failed to add player: " + userName + " to game, game was in an invalid state");
         ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. The game is in an invalid state.");
         return;
      }

      // Check that there is still space in the game
      if (getAvailableSlots() <= 0) {
         D.warning("Failed to add player: " + userName + " to the game, it was full");
         ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. The game is full!");
         return;
      }

      // If no team is given, select the best
      if (teamType == PvpTeamType.None) {
         teamType = getBestTeamWithSpace();

         if (teamType == PvpTeamType.None) {
            D.error("Couldn't find a team for player: " + userName);
            ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. No teams were found.");
            return;
         }
      }

      // Check that the chosen team can be joined
      PvpTeam assignedTeam;
      if (_teams.TryGetValue(teamType, out assignedTeam)) {
         foreach (PvpTeam team in _teams.Values) {
            if (team.Count < assignedTeam.Count) {
               D.error("The chosen team has more players than the others and cannot be joined by player: " + userName);
               ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. Please pick a team with less players!");
               return;
            }
         }
      } else {
         ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, $"Could not join the PvP Game. The team {teamType} does not exist.");
         return;
      }

      // If the player is in a voyage group, remove them from it
      if (VoyageGroupManager.self.tryGetGroupByUser(userId, out VoyageGroupInfo groupInfo)) {
         VoyageGroupManager.self.removeUserFromGroup(groupInfo, userId);
      }

      _usersInGame.Add(userId);
      assignedTeam.Add(userId);
      PowerupManager.self.clearPowerupsForUser(userId);

      if (_gameState == State.PreGame) {
         GameStatsManager.self.registerUser(userId, userName, assignedTeam.teamType);
         addPlayerToPreGame(userId, userName);
      } else if (_gameState == State.InGame) {
         GameStatsManager.self.registerUser(userId, userName, assignedTeam.teamType);
         StartCoroutine(CO_AddPlayerToOngoingGame(userId, userName, assignedTeam));
      }
   }

   private void addPlayerToPreGame (int userId, string userName) {
      VoyageGroupManager.self.createGroup(userId, voyageId, true);
      ServerNetworkingManager.self.warpUser(userId, voyageId, areaKey, Direction.South);
      D.debug("Adding player to pre-game. Added player: " + userName);

      // If there are now enough players to start the game, start the game after a delay
      if (isGameReadyToBegin()) {
         StartCoroutine(CO_StartGame());
      } else {
         int playersNeededToStart = (_numTeams * MIN_PLAYERS_PER_TEAM_TO_START) - getNumPlayers();
         if (_gameIsStarting) {
            sendGameMessage(userName + " has joined the game.");
         } else {
            // Don't send this message to the player joining the game, as it will display before they warp
            List<int> playersToReceiveMessage = new List<int>(_usersInGame);
            playersToReceiveMessage.Remove(userId);
            sendGameMessage(userName + " has joined the game. Waiting for " + playersNeededToStart + " more players to begin!", playersToReceiveMessage);
         }
      }
   }

   private IEnumerator CO_AddPlayerToOngoingGame (int userId, string userName, PvpTeam assignedTeam) {
      // If the team only has us, create a new voyage group with this player
      if (assignedTeam.Count == 1) {
         VoyageGroupManager.self.createGroup(userId, voyageId, true);

         // It takes at least one frame to synchronize the groups in the server network
         yield return null;

         // Wait for the group to be created
         VoyageGroupInfo newGroup;
         while (!VoyageGroupManager.self.tryGetGroupByUser(userId, out newGroup)) {
            yield return null;
         }

         // Store the team's voyage group id
         _teamVoyageGroupIds[assignedTeam.teamType] = newGroup.groupId;
         D.debug("Adding player to game in-progress. Added player: " + userName + " to team: " + assignedTeam.teamType.ToString() + ", and created group");

         // If the team isn't empty, add the player to the existing voyage group
      } else {
         if (VoyageGroupManager.self.tryGetGroupById(_teamVoyageGroupIds[assignedTeam.teamType], out VoyageGroupInfo voyageGroup)) {
            VoyageGroupManager.self.addUserToGroup(voyageGroup, userId, userName);
            D.debug("Adding player to game in-progress. Added player: " + userName + " to team: " + assignedTeam.teamType.ToString());
         } else {
            D.error("Couldn't find the voyage group for team: " + assignedTeam.teamType.ToString());
            yield break;
         }
      }
      string teamSpawn = getSpawnForTeam(assignedTeam.teamType);
      ServerNetworkingManager.self.warpUser(userId, voyageId, areaKey, Direction.South, teamSpawn);
   }

   private IEnumerator CO_StartGame () {
      // For players who are currently in the game, create voyage groups for their teams, and add them to the groups
      _gameIsStarting = true;

      sendGameMessage("The game will begin in " + GAME_START_DELAY + " seconds!");
      yield return new WaitForSeconds(GAME_START_DELAY - 3.0f);

      sendGameMessage("The game will begin in 3 seconds!");
      yield return new WaitForSeconds(1.0f);
      sendGameMessage("The game will begin in 2 seconds!");
      yield return new WaitForSeconds(1.0f);
      sendGameMessage("The game will begin in 1 seconds!");
      yield return new WaitForSeconds(1.0f);
      sendGameMessage("The game has begun!");

      detectStructures();
      setStructuresActivated(true);

      for (int i = 0; i < _usersInGame.Count; i++) {
         NetEntity player = EntityManager.self.getEntity(_usersInGame[i]);

         // If the player object doesn't exist yet, add them to a list, to setup later
         if (!player) {
            _latePlayers.Add(_usersInGame[i]);
            continue;
         }

         GameStatsManager.self.unregisterUser(player.userId);

         PlayerShipEntity playerShip = player.getPlayerShipEntity();

         if (playerShip) {
            if (player.isDead()) {
               playerShip.respawnPlayerInInstance();
            }

            playerShip.cancelCannonBarrage();
         }

         PvpTeamType teamType = getTeamForUser(_usersInGame[i]);
         PvpTeam team = _teams[teamType];
         player.currentHealth = player.maxHealth;
         player.pvpTeam = teamType;

         // Generate stat data for this player
         GameStatsManager.self.registerUser(player.userId);

         // Remove the user from its current group
         if (player.tryGetGroup(out VoyageGroupInfo currentGroup)) {
            VoyageGroupManager.self.removeUserFromGroup(currentGroup, player.userId);
         }

         // If a voyage group doesn't exist for the team, create it with this player
         if (!_teamVoyageGroupIds.ContainsKey(team.teamType)) {
            VoyageGroupManager.self.createGroup(player.userId, voyageId, true);
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
               VoyageGroupManager.self.addUserToGroup(newGroup, player.userId, player.entityName);
               D.debug("Adding player: " + player.name + " to team: " + team.teamType.ToString());
            }
         }

         // Move the player to their spawn position, with a small offset so players aren't stacked on eachother.
         Vector2 spawnPosition = getSpawnPositionForTeam(team.teamType);
         Util.setLocalXY(player.transform, spawnPosition);
      }

      _gameState = State.InGame;
      StartCoroutine(CO_SpawnSeamonsters());
      StartCoroutine(CO_SpawnWaves());
      StartCoroutine(CO_SetupLatePlayers());
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

   private IEnumerator CO_SetupLatePlayers () {
      while (_latePlayers.Count > 0) {
         yield return new WaitForSeconds(5.0f);

         for (int i = _latePlayers.Count - 1; i >= 0; i--) {
            int userId = _latePlayers[i];
            NetEntity player = EntityManager.self.getEntity(userId);
            if (player) {
               PvpTeamType bestTeam = getTeamForUser(userId);
               string userName = player.nameText.text;

               // Check if any team was found
               if (bestTeam == PvpTeamType.None) {
                  D.error("Couldn't find a team for player: " + userName);
                  ServerNetworkingManager.self.displayNoticeScreenWithError(userId, ErrorMessage.Type.PvpJoinError, "Could not join the PvP Game. No teams were found.");
                  player.spawnInBiomeHomeTown();
                  yield break;
               }

               PvpTeam assignedTeam = _teams[bestTeam];
               assignedTeam.Add(userId);

               // Generate stat data for this player
               GameStatsManager.self.registerUser(player.userId);

               // Remove the user from its current group
               if (player.tryGetGroup(out VoyageGroupInfo currentGroup)) {
                  VoyageGroupManager.self.removeUserFromGroup(currentGroup, player.userId);
               }

               // If the team only has us, create a new voyage group with this player
               if (assignedTeam.Count == 1) {
                  VoyageGroupManager.self.createGroup(userId, voyageId, true);

                  // It takes at least one frame to synchronize the groups in the server network
                  yield return null;

                  // Wait for the group to be created
                  VoyageGroupInfo newGroup;
                  while (!VoyageGroupManager.self.tryGetGroupByUser(userId, out newGroup)) {
                     yield return null;
                  }

                  // Store the team's voyage group id
                  _teamVoyageGroupIds[bestTeam] = newGroup.groupId;
                  D.debug("Adding player to game in-progress. Added player: " + userName + " to team: " + bestTeam.ToString() + ", and created group");

                  // If the team isn't empty, add the player to the existing voyage group
               } else {
                  if (VoyageGroupManager.self.tryGetGroupById(_teamVoyageGroupIds[bestTeam], out VoyageGroupInfo voyageGroup)) {
                     VoyageGroupManager.self.addUserToGroup(voyageGroup, userId, userName);
                     D.debug("Adding player to game in-progress. Added player: " + userName + " to team: " + bestTeam.ToString());
                  } else {
                     D.error("Couldn't find the voyage group for team: " + bestTeam.ToString());
                     yield break;
                  }
               }
               Vector2 spawnPosition = getSpawnPositionForTeam(bestTeam);
               Util.setLocalXY(player.transform, spawnPosition);

               _latePlayers.Remove(player.userId);
            }
         }
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

      // Store the positions of the bases
      _basePositions.Add(Vector3.zero);
      _basePositions.Add(_topStructuresA.Find((x) => x is PvpBase).transform.localPosition);
      _basePositions.Add(_topStructuresB.Find((x) => x is PvpBase).transform.localPosition);
   }

   private void setupStructureUnlocking (List<SeaStructure> laneStructures) {
      for (int i = 0; i < laneStructures.Count; i++) {
         SeaStructure structure = laneStructures[i];

         // All structures other than the first tower in each lane should be invulnerable
         if (i == 0) {
            structure.setIsInvulnerable(false);
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

      GameStatsManager.self.unregisterUser(player.userId);
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
            playerEntity.rpc.Target_ReceiveBroadcastPvpAnnouncement(message);
         }
      }
   }

   public void forceStart () {
      if (!_gameIsStarting) {
         StartCoroutine(CO_StartGame());
      }
   }

   private void onStructureDestroyed (SeaStructure structure) {
      if (structure is PvpBase) {
         PvpTeamType winningTeam = (structure.pvpTeam == PvpTeamType.A) ? PvpTeamType.B : PvpTeamType.A;
         onGameEnd(winningTeam);
      } else {
         sendGameMessage(getTeamName(structure.pvpTeam) + " Team's " + structure.GetType().ToString() + " has been destroyed.");
      }
   }

   private void onGameEnd (PvpTeamType winningTeam) {
      _gameState = State.PostGame;
      sendGameMessage(getTeamName(winningTeam) + " Team has won the game!");
      StopAllCoroutines();

      setStructuresActivated(false);

      foreach (BotShipEntity botShip in _ships) {
         if (botShip && botShip.gameObject) {
            NetworkServer.Destroy(botShip.gameObject);
         }
      }

      StartCoroutine(CO_PostGame());
   }

   public void onPlayerLoadedGameArea (int userId) {
      if (_gameState == State.PreGame) {
         sendGameMessageToPlayers("Waiting for " + " more players to begin.", new List<int>() { userId });
      }
   }

   private IEnumerator CO_PostGame () {
      yield return new WaitForSeconds(10.0f);
      sendGameMessage("This game will close in 20 seconds.");
      yield return new WaitForSeconds(20.0f);

      // Delete the team groups
      foreach (int voyageGroupId in _teamVoyageGroupIds.Values) {
         if (VoyageGroupManager.self.tryGetGroupById(voyageGroupId, out VoyageGroupInfo voyageGroupInfo)) {
            VoyageGroupManager.self.deleteGroup(voyageGroupInfo);
         }
      }

      foreach (int userId in _usersInGame) {
         NetEntity player = EntityManager.self.getEntity(userId);

         if (player) {
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
      return getMaxPlayerCount() - getNumPlayers();
   }

   public int getMaxPlayerCount () {
      return _numTeams * Voyage.MAX_PLAYERS_PER_GROUP_PVP;
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
      Vector3 spawnPosition;

      // If base positions have been setup, return the position of the team's base
      if ((int)teamType < _basePositions.Count) {
         spawnPosition = _basePositions[(int) teamType];
      
      // Otherwise, use spawns from map editor
      } else {
         int teamIndex = (int) (teamType) - 1;
         SpawnManager.MapSpawnData mapSpawnData = SpawnManager.self.getAllMapSpawnData(areaKey);

         spawnPosition = mapSpawnData.spawns.Values.ElementAt(teamIndex).localPosition;
      }

      // Choose an area around the spawn position
      Vector2 spawnPositionOffset = (teamType == PvpTeamType.A) ? Vector2.one : -Vector2.one;
      spawnPositionOffset *= 0.5f;
      int numTeamSpawns = (teamType == PvpTeamType.A) ? _teamASpawns++ : _teamBSpawns++;
      numTeamSpawns = numTeamSpawns % 6;
      float rotationAngle = numTeamSpawns * 60.0f;
      spawnPositionOffset = spawnPositionOffset.Rotate(rotationAngle);
      spawnPosition += (Vector3)spawnPositionOffset;

      return spawnPosition;
   }

   public PvpTeamType getTeamForUser (int userId) {
      foreach (PvpTeam team in _teams.Values) {
         if (team.Contains(userId)) {
            return team.teamType;
         }
      }

      if (_gameState == State.InGame) {
         D.warning("PvpGame.getTeamForPlayer() couldn't find a team that the player was assigned to.");
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

   public int getPlayerCountInTeam (PvpTeamType teamType) {
      if (_teams.TryGetValue(teamType, out PvpTeam team)) {
         return team.Count;
      } else {
         return 0;
      }
   }

   public static string getGameStateLabel (State state) {
      switch (state) {
         case State.PreGame:
            return "Pre-Game";
         case State.InGame:
            return "In Game";
         case State.PostGame:
            return "Post Game";
         default:
            return "";
      }
   }

   #region Pvp stat functions

   public static Color getColorForTeam (PvpTeamType team) {
      switch (team) {
         case PvpTeamType.None:
            return Color.white;
         case PvpTeamType.A:
            return Color.blue;
         case PvpTeamType.B:
            return Color.red;
         case PvpTeamType.C:
            return Color.green;
         case PvpTeamType.D:
            return Color.yellow;
         default:
            return Color.white;
      }
   }

   public static string getTeamName (PvpTeamType teamType) {
      switch (teamType) {
         case PvpTeamType.A:
            return "Green";
         case PvpTeamType.B:
            return "Red";
         default:
            return "None";
      }
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

   // A list of player user ids who couldn't be setup at game start
   private List<int> _latePlayers = new List<int>();

   // The positions of each pvp base, indexed by team
   private List<Vector3> _basePositions = new List<Vector3>();

   // How many times a player has spawned on each team
   private int _teamASpawns = 0, _teamBSpawns = 0;

   // How long the game will take to start, after enough players are present
   private const float GAME_START_DELAY = 10.0f;

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