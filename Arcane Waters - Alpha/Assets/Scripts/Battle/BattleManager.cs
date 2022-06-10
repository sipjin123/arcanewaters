using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;
using System.Linq;

[Serializable]
public class PrefabTypes
{
   // Determines the enemy type
   public Enemy.Type enemyType;

   // Holds the prefab of the type
   public Battler enemyPrefab;
}

public class BattleManager : MonoBehaviour {
   #region Public Variables

   // The amount of time we wait after a battle ends before moving players out of the battle view
   public static float END_BATTLE_DELAY = 5f;

   // How long we wait between consecutive battle ticks
   public static float TICK_INTERVAL = .5f;

   // The Prefab we use for creating new Battles
   public Battle battlePrefab;

   // The base battler behaviour that will be initialized depending on the type and data that the Scriptable Object includes
   public Battler baseBattlerPrefab;

   // The reference to the only battleboard 
   public BattleBoard battleBoard;

   // Self
   public static BattleManager self;

   // Holds the prefab reference for each monster type
   public List<PrefabTypes> prefabTypes;

   // The list of battleboards
   public List<BattleBoard> battleBoardList;

   // The action id that iterates per combat action
   public int actionIdIndex = 0;

   #endregion

   public void Awake () {
      self = this;
   }

   public void initializeBattleBoards () {
      // Store all of the Battle Boards that exist in the Scene
      foreach (BattleBoard board in battleBoardList) {
         _boards[board.xmlID] = board;
         BackgroundGameManager.self.serverSetSpritesToBattleBoard(board);
         board.gameObject.SetActive(false);
      }

      // Repeatedly call the tick() function for our Battle objects
      InvokeRepeating(nameof(tickBattles), 0f, TICK_INTERVAL);
   }

   public Battle createTeamBattle (Area area, Instance instance, Enemy enemy, BattlerInfo[] attackersData, PlayerBodyEntity playerBody, BattlerInfo[] defendersData, bool isShipBattle = false) {
      // We need to make a new one
      Battle battle = Instantiate(battlePrefab);

      if (instance.biome == Biome.Type.None) {
         D.debug("Invalid Area BIOME: Creating a team battle with biome type: NONE");
      }

      // Look up the Battle Board for this Area's tile type
      Biome.Type biomeType = instance.biome;
      battleBoard.biomeType = biomeType;
      BackgroundGameManager.self.serverSetSpritesToRandomBoard(battleBoard);

      battleBoard.gameObject.SetActive(true);

      // Set up our initial data and position
      battle.battleId = _id++;
      battle.area = area;
      battle.biomeType = biomeType;
      battle.battleBoard = battleBoard;
      battle.transform.SetParent(this.transform);
      battle.difficultyLevel = instance.difficulty;
      battle.enemyReference = enemy;

      int partyMemberCount = 1;
      playerBody.tryGetGroup(out VoyageGroupInfo voyageGroup);
      if (voyageGroup != null) {
         partyMemberCount = voyageGroup.members.Count;
      }
      battle.partyMemberCount = partyMemberCount;

      Util.setXY(battle.transform, battleBoard.transform.position);

      // Actually spawn the Battle as a Network object now
      NetworkServer.Spawn(battle.gameObject);

      // Keep track of the Battles we create
      _battles[battle.battleId] = battle;

      // Hashset for enemy types in the battle sequence
      HashSet<Enemy.Type> enemyTypes = new HashSet<Enemy.Type>();

      // Spawn an appropriate number of enemies based on the number of players in the instance
      if (defendersData != null && enemy != null) {
         foreach (BattlerInfo battlerInfo in defendersData) {
            if (battlerInfo.enemyType != Enemy.Type.PlayerBattler && battle.getTeam(Battle.TeamType.Defenders).Count < Battle.MAX_ENEMY_COUNT) {
               enemyTypes.Add(battlerInfo.enemyType);
               this.addEnemyToBattle(battle, battlerInfo.enemyReference == null ? enemy : battlerInfo.enemyReference, Battle.TeamType.Defenders, playerBody, battlerInfo.companionId, battlerInfo.battlerXp, instance.difficulty);
            }
         }
      }

      if (attackersData != null && enemy != null) {
         foreach (BattlerInfo battlerInfo in attackersData) {
            if (battlerInfo.enemyType != Enemy.Type.PlayerBattler) {
               enemyTypes.Add(battlerInfo.enemyType);
               this.addEnemyToBattle(battle, battlerInfo.enemyReference == null ? enemy : battlerInfo.enemyReference, Battle.TeamType.Attackers, playerBody, battlerInfo.companionId, battlerInfo.battlerXp, instance.difficulty);
            }
         }
      }

      battle.isShipBattle = isShipBattle;

      return battle;
   }

   public Battle getBattle (int battleId) {
      if (_battles.ContainsKey(battleId)) {
         return _battles[battleId];
      }

      return null;
   }

   public List<Battle> getAllBattles () {
      return _battles.Values.ToList();
   }

   public Battler getBattler (int userId) {
      if (_battlers.ContainsKey(userId)) {
         return _battlers[userId];
      }

      return null;
   }

   public bool isInBattle (int userId) {
      if (_activeBattles.ContainsKey(userId)) {
         return (_activeBattles[userId] != null);
      }

      return false;
   }

   public Battle getBattleForUser (int userId) {
      if (_activeBattles.ContainsKey(userId)) {
         return _activeBattles[userId];
      }

      return null;
   }

   public Battler getPlayerBattler () {
      if (Global.player == null) {
         return null;
      }

      return getBattler(Global.player.userId);
   }

   public void addPlayerToBattle (Battle battle, PlayerBodyEntity player, Battle.TeamType teamType) {
      // Maintain a Mapping of which players are in which Battles
      _activeBattles[player.userId] = battle;

      // The Player needs to return to this specific server if they reconnect
      ServerNetworkingManager.self.claimPlayer(player.userId);

      Battler battler = createBattlerForPlayer(battle, player, teamType);

      // Add the Battler to the Battle
      storeBattler(battler);
      if (teamType == Battle.TeamType.Attackers) {
         battle.attackers.Add(battler.userId);
         D.adminLog("Adding battler Player as Attacker" + " UID: " + battler.userId + " Type: " + battler.enemyType + " AtkrCount: " + battle.attackers.Count, D.ADMIN_LOG_TYPE.Combat);
      } else if (teamType == Battle.TeamType.Defenders) {
         battle.defenders.Add(battler.userId);
         D.adminLog("Adding battler Player as Defender" + " UID: " + battler.userId + " Type: " + battler.enemyType + " DefrCount: " + battle.defenders.Count, D.ADMIN_LOG_TYPE.Combat);
      }

      // Assign the Battle ID to the Sync Var
      player.battleId = battle.battleId;

      // Registers the action of combat entry to the achievement database for recording
      AchievementManager.registerUserAchievement(player, ActionType.EnterCombat);

      // Update the observers associated with the Battle and the associated players
      rebuildObservers(battler, battle);
   }

   public void addEnemyToBattle (Battle battle, Enemy enemy, Battle.TeamType teamType, PlayerBodyEntity aggressor, int companionId, int battlerXp, int difficultyLevel) {
      // Create a Battler for this Enemy
      Battler battler = createBattlerForEnemy(battle, enemy, teamType, companionId, battlerXp, difficultyLevel);
      self.storeBattler(battler);

      // Add the Battler to the Battle
      if (teamType == Battle.TeamType.Attackers) {
         battle.attackers.Add(battler.userId);
      } else if (teamType == Battle.TeamType.Defenders) {
         battle.defenders.Add(battler.userId);
      }

      if (battle.enemyReference == enemy) {
         // Assign the Battle ID to the Sync Var, causing movement to stop and facing direction to change
         enemy.assignBattleId(battle.battleId, aggressor);
      }

      // Assign the Net Entity
      battler.player = enemy;

      // Update the observers associated with the Battle and the associated players
      rebuildObservers(battler, battle);
   }

   public void endBattle (Battle battle, Battle.TeamType winningTeam) {
      BattleUIManager.self.isInitialEnemySelected = false;

      if (battle == null) {
         D.error("ERROR HERE! Battle should not be null before ending it!!!");
      }

      // Remove the Battle ID for any participants
      battle.resetAllBattleIDs(winningTeam);

      // Cycle over each Battler in the Battle
      foreach (Battler battler in battle.getParticipants()) {
         // Update our internal mapping of users to battles
         _activeBattles[battler.userId] = null;

         // Warp any losing players back to the starting town, or show the death animation for Enemies
         battler.handleEndOfBattle(winningTeam);

         // If this was a Player, we can release the server claim now, so they can relog into any server
         if (battler.userId > 0) {
            ServerNetworkingManager.self.releasePlayerClaim(battler.userId);
            battler.player.rpc.Target_ResetMoveDisable(battler.player.connectionToClient, battler.battleId.ToString());
         }

         // Destroy the Battler from the Network
         NetworkServer.Destroy(battler.gameObject);
      }
      D.adminLog("4. {" + battle.battleId + "} is now being destroyed", D.ADMIN_LOG_TYPE.CombatEnd);

      // Destroy the Battle from the Network
      NetworkServer.Destroy(battle.gameObject);
   }

   public void registerBattler (BattlerData battler) {
      if (_allBattlersData.Exists(_ => _.enemyType == battler.enemyType)) {
         return;
      }
      _allBattlersData.Add(battler);
   }

   public void storeBattle (Battle battle) {
      _battles[battle.battleId] = battle;
   }

   public void storeBattler (Battler battler) {
      _battlers[battler.userId] = battler;
   }

   protected void tickBattles () {
      // Call tick() on all of our battles
      foreach (Battle battle in _battles.Values) {
         // Don't tick unless the Battle is still in progress
         if (!battle.isOver()) {
            Battle.TickResult tickResult = battle.tick();

            // If a battle just ended, notify all the clients involved
            if (tickResult == Battle.TickResult.BattleOver) {
               StartCoroutine(endBattleAfterDelay(battle, END_BATTLE_DELAY));
            }
            if (tickResult == Battle.TickResult.Missing) {
               D.debug("Battle manager has detected a missing battle data: " + battle.battleId);
               StartCoroutine(endBattleAfterDelay(battle, END_BATTLE_DELAY));
   public void initializeClientBattler (int battleId, int userId, uint playerNetId, uint battlerNetId) {
      StartCoroutine(CO_ClientInitializeBattler(battleId, userId, playerNetId, battlerNetId));
   }

   private IEnumerator CO_ClientInitializeBattler (int battleId, int userId, uint playerNetId, uint battlerNetId) {
      yield return new WaitForSeconds(.5f);
      uint netIdRef = 0;
      if (Global.player != null && Global.player.userId == userId) {
         netIdRef = Global.player.netId;
         double startTime = NetworkTime.time;
         while (getBattle(battleId) == null) {
            if ((NetworkTime.time - startTime) > 30) {
               D.debug("Time has expired! " + (NetworkTime.time - startTime).ToString("f1"));
               yield return null;
            }
            yield return 0;
         }

         // Make sure that the battler reference exists
         while (!NetworkIdentity.spawned.ContainsKey(battlerNetId)) {
            yield return 0;
         }
         NetworkIdentity newBattlerConnection = NetworkIdentity.spawned[battlerNetId];
         Battler battlerRef = newBattlerConnection.GetComponent<Battler>();
         if (battlerRef != null) {
            // Initialize the battler for reconnection
            if (battlerRef.userId > 0 && battlerRef.userId == userId) {
               battlerRef.player = Global.player;
               battlerRef.initializeBattler();
            } else {
               D.debug("Battler: {" + battlerRef + "} NetID:{" + playerNetId + "} UserId:{" + userId + "} BattlerNetId:{" + battlerNetId + "}");
            }
         }
      }
   }

   protected Battler createBattlerForPlayer (Battle battle, PlayerBodyEntity player, Battle.TeamType teamType) {
      if (battle == null) {
         D.error("Battle is missing!");
      }

      // We need to make a new one
      Battler battler = Instantiate(baseBattlerPrefab);

      // Note that this battler is in a pvp or not
      battler.isPvp = battle.isPvp;
      battler = assignBattlerData(battle, battler, player, teamType);
      battler.health = battler.getStartingHealth(Enemy.Type.PlayerBattler, true);
      battler.enemyType = Enemy.Type.PlayerBattler;
      battler.playerNetId = player.netId;

      // Figure out which Battle Spot we should be placed in
      battler.boardPosition = battle.getTeam(teamType).Count + 1;
      BattleSpot battleSpot = battle.battleBoard.getSpot(teamType, battler.boardPosition);
      battler.battleSpot = battleSpot;
      battler.transform.position = battleSpot.transform.position;
      assignBattlerSyncData(battler, player, true);

      // Actually spawn the Battler as a Network object now
      NetworkServer.Spawn(battler.gameObject);

      return battler;
   }

   private Battler assignBattlerData (Battle battle, Battler battler, PlayerBodyEntity player, Battle.TeamType teamType) {
      battler.battlerType = BattlerType.PlayerControlled;

      // Set up our initial data and position
      battler.player = player;
      battler.battle = battle;
      battler.userId = player.userId;

      // This function will handle the computed stats of the player depending on their Specialty/Faction/Job/Class
      battler.initializeBattlerData();

      // Zeronev: this will not sync the battle ID to all created battlers, a CMD is needed.
      battler.battleId = battle.battleId;
      battler.teamType = teamType;
      battler.biomeType = battle.biomeType;
      battler.transform.SetParent(battle.transform);

      // Copy the layer data
      battler.gender = player.gender;
      battler.XP = player.XP;
      battler.bodyType = player.bodyType;
      battler.eyesType = player.eyesType;
      battler.hairType = player.hairType;
      battler.hairPalettes = player.hairPalettes;
      battler.eyesPalettes = player.eyesPalettes;

      return battler;
   }

   private void assignBattlerSyncData (Battler battler, PlayerBodyEntity player, bool ifFirstEquip) {
      if (!ifFirstEquip) {
         if (battler.armorManager.armorType != player.armorManager.armorType) {
            // TODO: Setup a way to determine the duration of debuffs, maybe a debuff web tool?
            // Add armor debuff to the player, lasts 15 seconds
            if (!battler.debuffList.ContainsKey(Status.Type.ArmorChangeDebuff)) {
               battler.debuffList.Add(Status.Type.ArmorChangeDebuff, new StatusData {
                  abilityIdReference = -1,
                  statusDuration = 15
               });
            }
         }

         if (battler.weaponManager.weaponType != player.weaponManager.weaponType) {
            // TODO: Setup a way to determine the duration of debuffs, maybe a debuff web tool?
            // Add damage debuff to the player, lasts 15 seconds
            if (!battler.debuffList.ContainsKey(Status.Type.WeaponChangeDebuff)) {
               battler.debuffList.Add(Status.Type.WeaponChangeDebuff, new StatusData {
                  abilityIdReference = -1,
                  statusDuration = 15
               });
            }
         }
      } else {
         if (player.weaponManager.weaponDurability < 1) {
            if (!battler.debuffList.ContainsKey(Status.Type.WeaponBreakDebuff)) {
               battler.debuffList.Add(Status.Type.WeaponBreakDebuff, new StatusData {
                  abilityIdReference = -1,
                  statusDuration = -1
               });
            }
         }

         if (player.armorManager.armorDurability < 1) {
            if (!battler.debuffList.ContainsKey(Status.Type.ArmorChangeDebuff)) {
               battler.debuffList.Add(Status.Type.ArmorBreakDebuff, new StatusData {
                  abilityIdReference = -1,
                  statusDuration = -1
               });
            }
         }
      }

      // Copy the Armor Info
      if (player.armorManager.armorType < 1) {
         battler.armorManager.updateArmorSyncVars(0, 0, "", 0);
         battler.armorManager.armorType = 0;
         battler.armorManager.palettes = "";
      } else {
         battler.armorManager.updateArmorSyncVars(player.armorManager.equipmentDataId, player.armorManager.equippedArmorId, player.armorManager.palettes, player.armorManager.armorDurability);
         battler.armorManager.armorType = player.armorManager.armorType;
         battler.armorManager.palettes = player.armorManager.palettes;
         D.adminLog("Equipping battler armor: {" + battler.armorManager.armorType + "}", D.ADMIN_LOG_TYPE.Equipment);
      }

      // Copy the Weapon Info
      if (player.weaponManager.weaponType < 1) {
         battler.weaponManager.updateWeaponSyncVars(0, 0, "", 0, 0);
         battler.weaponManager.weaponType = 0;
         battler.weaponManager.palettes = "";
      } else {
         battler.weaponManager.updateWeaponSyncVars(player.weaponManager.equipmentDataId, player.weaponManager.equippedWeaponId, player.weaponManager.palettes, player.weaponManager.weaponDurability, player.weaponManager.count);
         battler.weaponManager.weaponType = player.weaponManager.weaponType;
         battler.weaponManager.palettes = player.weaponManager.palettes;
         D.adminLog("Equipping battler weapon: {" + battler.weaponManager.weaponType + "}", D.ADMIN_LOG_TYPE.Equipment);
      }

      // Copy the Hat Info
      if (player.hatsManager.hatType < 1) {
         battler.hatManager.updateHatSyncVars(0, 0, "");
         battler.hatManager.hatType = 0;
         battler.hatManager.palettes = "";
      } else {
         battler.hatManager.updateHatSyncVars(player.hatsManager.equipmentDataId, player.hatsManager.equippedHatId, "");
         battler.hatManager.hatType = player.hatsManager.hatType;
         battler.hatManager.palettes = player.hatsManager.palettes;
      }
   }

   private Battler createBattlerForEnemy (Battle battle, Enemy enemy, Battle.TeamType teamType, int companionId, int battlerXp, int difficultyLevel) {
      Enemy.Type overrideType = enemy.enemyType;
      Battler enemyPrefab = prefabTypes.Find(_ => _.enemyType == Enemy.Type.Lizard).enemyPrefab;

      BattlerData data = getAllBattlersData().Find(x => x.enemyType == overrideType);
      Battler battler = Instantiate(enemyPrefab);
      battler.biomeType = battle.biomeType;
      battler.enemyType = overrideType;

      // Note that this battler is in a pvp or not
      battler.isPvp = battle.isPvp;
      battler.useSpecialAttack = true;
      battler.isBossType = data.isBossType;
      battler.name = data.enemyName;
      battler.companionId = companionId;
      battler.XP = battlerXp;
      battler.difficultyLevel = difficultyLevel;
      battler.playerNetId = enemy.netId;

      // Set starting stats
      battler.battle = battle;
      battler.health = battler.getStartingHealth(overrideType, true);
      battler.displayedHealth = battler.getStartingHealth(overrideType);

      // Set up our initial data and position
      battler.player = enemy;
      battler.battle = battle;
      battler.battleId = battle.battleId;
      battler.transform.SetParent(battle.transform);
      battler.teamType = teamType;
      battler.userId = _enemyId--;

      // Figure out which Battle Spot we should be placed in
      battler.boardPosition = battle.getTeam(teamType).Count + 1;
      BattleSpot battleSpot = battle.battleBoard.getSpot(teamType, battler.boardPosition);
      if (battleSpot == null) {
         D.debug("Error here! Failed to fetch battle spot: " + " TeamCount: " + battle.getTeam(teamType).Count);
      } else {
         battler.battleSpot = battleSpot;
         battler.transform.position = battleSpot.transform.position;
      }

      // Actually spawn the Battler as a Network object now
      NetworkServer.Spawn(battler.gameObject);
      battler.transform.SetParent(battle.transform, false);
      battler.updateBattleSpots();

      return battler;
   }

   public void rebuildObservers (Battler newBattler, Battle battle) {
      // If this entity is a Bot and not a Player, then all it needs to do is make itself visible to clients in the Battle
      if (!(newBattler.player is PlayerBodyEntity)) {
         newBattler.netIdentity.RebuildObservers(false);
      } else {
         // Everything in the Battle needs to update its observer list to include the new Battler
         foreach (Battler battler in battle.getParticipants()) {
            battler.netIdentity.RebuildObservers(false);
         }

         // We also need the Battle object to be viewable by the new Battler
         battle.netIdentity.RebuildObservers(false);
      }
   }

   protected int getEnemyCount (Enemy enemy, int playersInInstance) {
      float randomRoll = Random.Range(0f, 1f);
      int enemyCount = playersInInstance;

      if (playersInInstance == 1) {
         return 1;
      }

      // If it's a boss, we always have just 1
      if (enemy.isBossType) {
         return 1;
      }

      // Slight chance of less enemies than players
      if (randomRoll < .25f) {
         enemyCount = playersInInstance - 1;
      }

      // Slight chance of more enemies than players
      if (randomRoll > .75f) {
         enemyCount = playersInInstance + 1;
      }

      // Clamp to reasonable values
      enemyCount = Mathf.Clamp(enemyCount, 1, 6);

      return enemyCount;
   }

   #region Attack Execution

   public void cancelBattleAction (Battle battle, Battler source, List<Battler> targets, int abilityInventoryIndex, AbilityType abilityType) {
      BasicAbilityData abilityData = new BasicAbilityData();
      List<BattleAction> actions = new List<BattleAction>();

      // String action list that will be sent to clients
      List<string> stringList = new List<string>();

      if (source.getAttackAbilities().Count > 0) {
         if (abilityType == AbilityType.Standard) {
            abilityData = source.getAttackAbilities()[abilityInventoryIndex];
         } else if (abilityType == AbilityType.BuffDebuff) {
            abilityData = source.getBuffAbilities()[abilityInventoryIndex];
         }
      } else {
         D.editorLog("Enemy: " + source.battlerType + " has no proper ability assigned", Color.red);
         abilityData = AbilityManager.self.getPunchAbility();
      }

      if (abilityType == AbilityType.Standard) {
         CancelAction cancelAction = new CancelAction(battle.battleId, source.userId, 0, 0, 0);
         cancelAction.abilityGlobalID = abilityData.itemID;
         actions.Add(cancelAction);

         if (cancelAction != null) {
            cancelAction.battleActionType = BattleActionType.Cancel;

            foreach (CancelAction action in actions) {
               stringList.Add(action.serialize());
            }

            // Force the cooldown to reach current time so a new ability can be casted
            source.cooldownEndTime = NetworkTime.time - .1f;

            // Send it to clients
            battle.Rpc_SendCombatAction(stringList.ToArray(), BattleActionType.Attack, true);
         }
      }
   }

   public void executeBattleAction (Battle battle, Battler source, List<Battler> targets, int abilityInventoryIndex, AbilityType abilityType) {
      // This function processes on the server side only
      BattleActionType actionType = BattleActionType.UNDEFINED;

      // Get ability reference from the source battler, cause the source battler is the one executing the ability
      BasicAbilityData abilityData = new BasicAbilityData();

      if (source.getAttackAbilities().Count > 0 && abilityType == AbilityType.Standard && abilityInventoryIndex >= 0) {
         abilityData = source.getAttackAbilities()[abilityInventoryIndex];
      } else if (source.getBuffAbilities().Count > 0 && abilityType == AbilityType.BuffDebuff && abilityInventoryIndex >= 0) {
         abilityData = source.getBuffAbilities()[abilityInventoryIndex];
      } else {
         if (source.enemyType == Enemy.Type.PlayerBattler) {
            D.debug("Ability is set to punch! " + source.getAttackAbilities().Count + " : " + source.getBasicAbilities().Count + " : " + abilityType + " : " + abilityInventoryIndex);
         }
         abilityData = AbilityManager.self.getPunchAbility();
      }

      bool wasBlocked = false;
      bool wasCritical = false;
      bool isMultiTarget = targets.Count > 1;
      double timeToWait = battle.getTimeToWait(source, targets);
      double actionEndTime = 0;

      List<BattleAction> actions = new List<BattleAction>();

      // String action list that will be sent to clients
      List<string> stringList = new List<string>();

      if (abilityData == null) {
         D.error("The Ability Data is NULL!! : " + source.enemyType);
         return;
      }
      if (abilityData.abilityType == AbilityType.Standard) {
         AttackAbilityData attackAbilityData = abilityData as AttackAbilityData;

         // Apply the AP change
         int sourceApChange = abilityData.apChange;
         source.addAP(sourceApChange);

         float critChance = 0.25f;
         if (source.userId > 0) {
            critChance = getStanceCritChance(source.stance);
         }

         foreach (Battler target in targets) {

            float blockChance = 0.25f;
            if (target.canBlock()) {
               blockChance = getStanceBlockChance(target.stance);
               blockChance += PerkManager.self.getPerkMultiplierAdditive(Perk.Category.BlockChance);
            }

            // For now, players have a 25% chance of blocking monsters
            if (target.canBlock() && attackAbilityData.canBeBlocked) {
               float blockRandomizer = Random.Range(0.0f, 1.0f);
               wasBlocked = blockRandomizer < blockChance;
            }

            // If the attack wasn't blocked, 25% chance to be a critical attack
            if (!wasBlocked) {
               wasCritical = Random.Range(0.0f, 1.0f) < critChance;
            }

            // Adjust the damage amount based on element, ability, and the target's armor
            Element element = abilityData.elementType;

            float sourceDamageElement = source.getDamage(element);
            float damage = sourceDamageElement + attackAbilityData.baseDamage * attackAbilityData.getModifier;

            // Add powerup damage
            if (source.userId > 0) {
               int allDamageBoost = 0;
               if (LandPowerupManager.self.hasPowerup(source.userId, LandPowerupType.DamageBoost)) {
                  allDamageBoost = (int) (damage * LandPowerupManager.self.getPowerupValue(source.userId, LandPowerupType.DamageBoost) / 100);
               }
               
               PlayerBodyEntity playerBody = source.player.GetComponent<PlayerBodyEntity>();
               if (playerBody != null) {
                  WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(playerBody.weaponManager.equipmentDataId);
                  if (weaponData != null) {
                     int weaponBoostDamage = 0;
                     switch (weaponData.weaponClass) {
                        case Weapon.Class.Melee:
                           if (LandPowerupManager.self.hasPowerup(source.userId, LandPowerupType.MeleeDamageBoost)) {
                              weaponBoostDamage = (int) (damage * LandPowerupManager.self.getPowerupValue(source.userId, LandPowerupType.MeleeDamageBoost) / 100);
                           }
                           break;
                        case Weapon.Class.Ranged:
                           if (LandPowerupManager.self.hasPowerup(source.userId, LandPowerupType.RangeDamageBoost)) {
                              weaponBoostDamage = (int) (damage * LandPowerupManager.self.getPowerupValue(source.userId, LandPowerupType.RangeDamageBoost) / 100);
                           }
                           break;
                     }
                     damage += weaponBoostDamage;
                  }
                  
                  // Add damage for trinket based buffs
                  TrinketStatData trinketData = EquipmentXMLManager.self.getTrinketData(playerBody.gearManager.equippedTrinkedXmlId);
                  if (trinketData != null) {
                     if (trinketData.gearBuffType == EquipmentStatData.GearBuffType.PlayerDamageBoost) {
                        if (trinketData.isBuffPercentage) {
                           damage += (int) (damage * (trinketData.itemBuffValue * 0.01));
                        } else {
                           damage += trinketData.itemBuffValue;
                        }
                     }
                  }
               }

               // Stacks the weapon class damage boost as part of the overall damage boost
               damage += allDamageBoost;
            }

            // If this is a boss monster, add damage (based from admin game settings) depending on number of team members
            if (source.isBossType) {
               float damagePercentageValueRaw = damage * (AdminGameSettingsManager.self.settings.bossDamagePerMember / 100);
               float teamDamageValue = damagePercentageValueRaw * battle.partyMemberCount;
               damage += (int) teamDamageValue;
            }

            if (source.enemyType != Enemy.Type.PlayerBattler && source.battlerType == BattlerType.AIEnemyControlled) {
               // Setup damage multiplier based on difficulty, additional damage {Easy: Dmg+10% / Medium: Dmg+20% / Hard: Dmg+30%} 
               float addedDamageForDifficulty = (damage * (battle.difficultyLevel * .1f));
               damage += addedDamageForDifficulty;
            }

            float targetDefenseElement = target.getDefense(element);

            // Add powerup defense
            if (target.userId > 0) {
               if (LandPowerupManager.self.hasPowerup(source.userId, LandPowerupType.DefenseBoost)) {
                  int boostedDefense = (int) (targetDefenseElement * LandPowerupManager.self.getPowerupValue(source.userId, LandPowerupType.DefenseBoost) / 100);
                  targetDefenseElement += boostedDefense;
               }
            }

            // Determines if the unit is resistant or weak against an element
            float resistantModifier = 0;
            if (target.isWeakAgainst(element)) {
               resistantModifier = ((100f + targetDefenseElement) / 100);
            } else {
               resistantModifier = (100f / (100f + targetDefenseElement));
            }
            damage *= resistantModifier;

            // Determines the damage effectiveness
            DamageMagnitude damageEffectMagnitude = DamageMagnitude.Default;
            if (resistantModifier < .9f) {
               damageEffectMagnitude = DamageMagnitude.Resistant;
            } else if (resistantModifier > 1.1f) {
               damageEffectMagnitude = DamageMagnitude.Weakness;
            }

            float increaseAdditive = 0f;
            float decreaseMultiply = 1f;
            decreaseMultiply *= wasBlocked ? .50f : 1f;
            increaseAdditive += wasCritical ? .50f : 0f;

            // Adjust the damage based on the source and target stances
            increaseAdditive += (source.stance == Battler.Stance.Attack) ? .25f : 0f;
            decreaseMultiply *= (source.stance == Battler.Stance.Defense) ? .75f : 1f;
            if (!attackAbilityData.isHeal()) {
               increaseAdditive += (target.stance == Battler.Stance.Attack) ? .25f : 0f;
               decreaseMultiply *= (target.stance == Battler.Stance.Defense) ? .75f : 1f;
            }

            // Adjust the damage based on perks
            if (source.enemyType == Enemy.Type.PlayerBattler) {
               if (attackAbilityData.abilityActionType == AbilityActionType.Melee) {
                  increaseAdditive += PerkManager.self.getPerkMultiplierAdditive(source.userId, Perk.Category.MeleeDamage);
               } else if (attackAbilityData.abilityActionType == AbilityActionType.Ranged) {
                  increaseAdditive += PerkManager.self.getPerkMultiplierAdditive(source.userId, Perk.Category.RangedDamage);
               }
            }

            // Decrease damage on protected targets
            if (target.isProtected(battle)) {
               decreaseMultiply *= .70f;
            }

            // Apply the adjustments to the damage
            damage *= (1f + increaseAdditive);
            damage *= decreaseMultiply;

            // Make note of the time that this battle action is going to be fully completed, considering animation times
            double timeAttackEnds = NetworkTime.time + timeToWait + attackAbilityData.getTotalAnimLength(source, target);
            float cooldownDuration = abilityData.abilityCooldown * source.getCooldownModifier() * AdminGameSettingsManager.self.settings.battleAttackCooldown;
            source.cooldownEndTime = timeAttackEnds + cooldownDuration;

            // Apply the target's AP change
            int targetApChange = target.getApWhenDamaged();
            target.addAP(targetApChange);

            AttackAction.ActionType currentActionType = AttackAction.ActionType.Melee;
            if (attackAbilityData.abilityActionType == AbilityActionType.Ranged) {
               currentActionType = AttackAction.ActionType.Range;
            }

            // Note if the target recently changed their armor
            if (target.debuffList.ContainsKey(Status.Type.ArmorChangeDebuff)) {
               D.adminLog("Target {" + target.userId + "}  Defense Deducted debuff is {"
                  + Status.Type.ArmorChangeDebuff + " : "
                  + target.debuffList[Status.Type.ArmorChangeDebuff] + " secs} "
                  + "Damage Received is set from {" + damage.ToString("f1") + "} to {" + (damage + (damage * .2f)).ToString("f1") + "}", D.ADMIN_LOG_TYPE.Equipment);

               // TODO: Setup a cleaner way of doing this
               // Add 50% damage receive if target just recently changed armor
               damage += damage * .5f;
            }

            // Note if the source recently changed their weapon
            if (source.debuffList.ContainsKey(Status.Type.WeaponChangeDebuff)) {
               D.adminLog("Source {" + source.userId + "} Damage Deducted debuff is {"
                  + Status.Type.WeaponChangeDebuff + " : "
                  + source.debuffList[Status.Type.WeaponChangeDebuff] + " secs} "
                  + "Damage Inflicted is set from {" + damage.ToString("f1") + "} to {" + (damage - (damage * .2f)).ToString("f1") + "}", D.ADMIN_LOG_TYPE.Equipment);

               // TODO: Setup a cleaner way of doing this
               // Deduct 50% damage output if source just recently changed weapon
               damage -= damage * .5f;
            }

            // Create the Action object
            AttackAction action = new AttackAction(battle.battleId, currentActionType, source.userId, target.userId,
                (int) damage, timeAttackEnds, abilityInventoryIndex, wasCritical, wasBlocked, cooldownDuration, sourceApChange,
                targetApChange, abilityData.itemID, damageEffectMagnitude);
            action.actionId = actionIdIndex;
            action.targetStartingHealth = target.health;
            actions.Add(action);

            actionIdIndex++;

            // Make note how long the two Battler objects need in order to execute the attack/hit animations
            source.animatingUntil = timeAttackEnds;
            target.animatingUntil = timeAttackEnds;

            // TODO: Remove after fixing bug wherein Golem boss action is stuck for a long time
            if (source.isBossType) {
               D.adminLog("Boss Monster is attacking: "
                  + " TimeToWait: " + (timeToWait)
                  + " Cooldown: " + cooldownDuration
                  + " AtkEnds: " + timeAttackEnds.ToString("f2")
                  + " CurrTime: " + NetworkTime.time.ToString("f2")
                  + " Target: " + target.userId + " - " + target.enemyType
                  + " TotalTargets: " + targets.Count, D.ADMIN_LOG_TYPE.Boss);
            }
            actionEndTime = action.actionEndTime;

            // Wait to apply the effects of the action here on the server until the appointed time
            StartCoroutine(applyActionAfterDelay(timeToWait, action, isMultiTarget));
         }

         // Compile all attack actions in a string list to be sent across the rpc
         foreach (AttackAction attackAction in actions) {
            stringList.Add(attackAction.serialize());
         }

         switch (attackAbilityData.abilityActionType) {
            case AbilityActionType.Melee:
               actionType = BattleActionType.Attack;
               break;
            case AbilityActionType.Ranged:
               actionType = BattleActionType.Attack;
               break;
            case AbilityActionType.CastToTarget:
               actionType = BattleActionType.Attack;
               break;
            case AbilityActionType.Cancel:
               actionType = BattleActionType.Cancel;
               break;
         }

      } else if (abilityData.abilityType == AbilityType.BuffDebuff) {
         actionType = BattleActionType.BuffDebuff;
         BuffAbilityData buffAbility = abilityData as BuffAbilityData;

         // Apply the AP change
         int sourceApChange = abilityData.apChange;
         source.addAP(sourceApChange);

         foreach (Battler target in targets) {
            // Make note of the time that this battle action is going to be fully completed, considering animation times
            double timeBuffEnds = NetworkTime.time + timeToWait + buffAbility.getTotalAnimLength(source, target);
            float cooldownDuration = abilityData.abilityCooldown * source.getCooldownModifier() * AdminGameSettingsManager.self.settings.battleAttackCooldown;
            source.cooldownEndTime = timeBuffEnds + cooldownDuration;

            // Apply the target's AP change
            int targetApChange = target.getApWhenDamaged();
            target.addAP(targetApChange);

            // Create the Action object
            BuffAction action = new BuffAction(battle.battleId, 0, source.userId, target.userId, timeBuffEnds, timeBuffEnds + 10, cooldownDuration, timeBuffEnds, sourceApChange, targetApChange, abilityData.itemID, buffAbility.value, buffAbility.elementType, buffAbility.bonusStatType, buffAbility.buffActionType);
            action.targetStartingHealth = target.health;
            actions.Add(action);

            // Make note how long the two Battler objects need in order to execute the cast effect animations
            source.animatingUntil = timeBuffEnds;
            target.animatingUntil = timeBuffEnds;
            actionEndTime = action.actionEndTime;

            // Wait to apply the effects of the action here on the server until the appointed time
            StartCoroutine(applyActionAfterDelay(timeToWait, action, isMultiTarget));

            foreach (BuffAction buffAction in actions) {
               stringList.Add(action.serialize());
            }
         }

         AchievementManager.registerUserAchievement(source.player, ActionType.UseStatsBuff);
      } else {
         D.debug("Battle action was not prepared correctly");
      }

      QueuedRpcAction queuedRpc = new QueuedRpcAction {
         actionSerialized = stringList.ToArray(),
         battleActionType = actionType,
         isCancelAction = false,
         actionEndTime = actionEndTime,
      };

      battle.queuedRpcActionList.Add(queuedRpc);

      // Send it to clients
      battle.Rpc_SendCombatAction(stringList.ToArray(), actionType, false);
   }

   // Stance action does not requires another target or an ability inventory index, thus, being removed from executeBattleAction
   public void executeStanceChangeAction (Battle battle, Battler source, Battler.Stance newStance) {
      source.stance = newStance;
      double timeActionEnds = NetworkTime.time + source.getStanceCooldown(newStance);
      StanceAction stanceAction = new StanceAction(battle.battleId, source.userId, timeActionEnds, newStance);

      string serializedValue = stanceAction.serialize();
      string[] values = new[] { serializedValue };

      battle.Rpc_SendCombatAction(values, BattleActionType.Stance, false);
   }

   #endregion

   protected int getGoldForDefeated (List<Battler> defeatedList) {
      int totalGold = 0;

      // Get the value from each individual battler
      foreach (Battler battler in defeatedList) {
         totalGold += battler.getGoldValue();
      }

      return totalGold;
   }

   protected int getXPForDefeated (List<Battler> defeatedList) {
      int total = 0;

      // Get the value from each individual battler
      foreach (Battler battler in defeatedList) {
         total += battler.getXPValue();
      }

      return total;
   }

   protected IEnumerator applyActionAfterDelay (double timeToWait, BattleAction actionToApply, bool hasMultipleTargets) {
      // Check if the battle has ended
      Battle battle = getBattle(actionToApply.battleId);
      if (battle == null) {
         D.debug("Missing battle! {" + actionToApply.battleId + "}");
         yield break;
      }

      // Get the Battler object
      Battler source = battle.getBattler(actionToApply.sourceId);
      if (source == null) {
         D.debug("Missing source battler! {" + actionToApply.sourceId + "}");
         yield break;
      }

      Battler target = battle.getBattler(actionToApply.targetId);
      if (target == null) {
         D.debug("Missing target battler! {" + actionToApply.sourceId + "}");
         yield break;
      }

      // This function processes on the server side only, process waiting time of action
      double startTimer = NetworkTime.time;
      double endTime = startTimer + timeToWait;
      while (NetworkTime.time < endTime) {
         if (source.isDead() || target.isDead() || source.isDisabledByStatus()) {
            // Don't create Cancel Actions for multi-target abilities
            if (hasMultipleTargets && !source.isDisabledByStatus()) {
               yield break;
            }

            if (source.isDisabledByStatus()) {
               D.adminLog("Cancel action because source is disabled by status!", D.ADMIN_LOG_TYPE.CombatStatus);
            }

            processCancelAction(battle, source, target, actionToApply, endTime - NetworkTime.time);
            yield break;
         }
         yield return 0;
      }

      // Check if battle has ended after wait timer
      if (getBattle(actionToApply.battleId) == null) {
         yield break;
      }

      if (actionToApply is AttackAction) {
         AttackAction attackAction = (AttackAction) actionToApply;

         // Registers the usage of the Offensive Skill for achievement recording
         if (source.player.userId != 0) {
            AchievementManager.registerUserAchievement(source.player, ActionType.OffensiveSkillUse);
         }

         // Applies damage delay for abilities with extra animation durations such as casting and aiming
         float attackApplyDelay = 0;
         AttackAbilityData abilityDataReference = (AttackAbilityData) AbilityManager.getAbility(actionToApply.abilityGlobalID, AbilityType.Standard);
         if (abilityDataReference.abilityActionType == AbilityActionType.Ranged || abilityDataReference.abilityActionType == AbilityActionType.CastToTarget) {
            attackApplyDelay += abilityDataReference.getAimDuration();
         }

         yield return new WaitForSeconds(attackApplyDelay);

         // Apply damage
         target.health -= attackAction.damage;
         target.health = Util.clamp<int>(target.health, 0, target.getStartingHealth());
         source.canExecuteAction = true;

         // Apply attack status here
         if (abilityDataReference.statusType != Status.Type.None) {
            bool canBeDisabled = true;
            if (target.isBossType) {
               switch (abilityDataReference.statusType) {
                  case Status.Type.Slowed:
                  case Status.Type.Stunned:
                     canBeDisabled = false;
                     break;
               }
            }

            if (canBeDisabled) {
               float randomizedChance = Random.Range(1, 100);
               if (randomizedChance < abilityDataReference.statusChance) {
                  StartCoroutine(CO_UpdateStatusAfterCollision(target, abilityDataReference.statusType, abilityDataReference.statusDuration, actionToApply.actionEndTime, abilityDataReference.itemID, source.userId));
               }
            }
         }

         // Setup server to declare a battler is dead when the network time reaches the time action ends
         if (target.health <= 0) {
            StartCoroutine(CO_KillBattlerAtEndTime(attackAction));
            StartCoroutine(CO_SendWinNoticeAfterEnd(attackAction));
         }
      } else if (actionToApply is BuffAction) {
         BuffAction buffAction = (BuffAction) actionToApply;

         // Registers the usage of the Buff Skill for achievement recording
         AchievementManager.registerUserAchievement(source.player, ActionType.BuffSkillUse);

         if (source.enemyType == Enemy.Type.PlayerBattler) {
            float buffValue = buffAction.buffValue;
            buffValue *= 1.0f + PerkManager.self.getPerkMultiplierAdditive(buffAction.sourceId, Perk.Category.Healing);
         }

         // Apply damage
         if (buffAction.buffActionType == BuffActionType.Regeneration) {
            target.health += buffAction.buffValue;
            target.health = Util.clamp<int>(target.health, 0, target.getStartingHealth());
         }

         // Apply the Buff
         target.addBuff(buffAction.getBuffTimer());
      }
   }

   private void processCancelAction (Battle battle, Battler source, Battler target, BattleAction actionToApply, double remainingTime) {
      float animLength = .6f;
      if (actionToApply is AttackAction) {
         // ZERONEV-COMMENT: It is supossed we are still grabbing the ability from the source battler to apply it
         // So we will grab the source battler
         AttackAbilityData abilityData = source.getAttackAbilities()[actionToApply.abilityInventoryIndex];

         // Remove the action cooldown and animation duration from the source's timestamps
         animLength = abilityData.getTotalAnimLength(source, target);
      }

      float timeToSubtract = actionToApply.cooldownDuration + animLength;

      // Update the battler's action timestamps here on the server
      target.animatingUntil -= animLength;
      source.animatingUntil -= animLength;
      source.cooldownEndTime -= timeToSubtract;

      // Create a Cancel Action to send to the clients
      if (source.canCancelAction) {
         if (source.userId > 0) {
            D.adminLog("Cancelling attack! " + (remainingTime > 0 ? "Remaining time was {" + remainingTime.ToString("f1") + "}" : "") + " for user: {" + source.userId + "}", D.ADMIN_LOG_TYPE.CancelAttack);
         }
         CancelAction cancelAction = new CancelAction(actionToApply.battleId, actionToApply.sourceId, actionToApply.targetId, NetworkTime.time, timeToSubtract);
         AbilityManager.self.execute(new[] { cancelAction });
         battle.Rpc_ReceiveCancelAction(actionToApply.battleId, actionToApply.sourceId, actionToApply.targetId, NetworkTime.time, timeToSubtract);
      } else {
         D.log("Cannot cancel action");
      }
   }

   protected IEnumerator CO_UpdateStatusAfterCollision (Battler battlerReference, Status.Type statusType, float statusDuration, double endTime, int abilityId, int casterId) {
      // Wait for client side attack to finish before applying the status effect
      while (NetworkTime.time < endTime) {
         yield return 0;
      }

      battlerReference.applyStatusEffect(statusType, statusDuration, abilityId, casterId);
   }

   protected IEnumerator CO_SendWinNoticeAfterEnd (AttackAction attackAction) {
      // Wait for action end time
      while (NetworkTime.time < attackAction.actionEndTime) {
         yield return 0;
      }

      Battle battle = getBattle(attackAction.battleId);
      if (battle == null) {
         yield return null;
      }
      Battler target = battle.getBattler(attackAction.targetId);
      if (target == null) {
         yield return null;
      }
      Battler source = battle.getBattler(attackAction.sourceId);
      if (source == null) {
         yield return null;
      }

      bool battleEnds = true;
      if (source.enemyType == Enemy.Type.PlayerBattler && source.isAttacker()) {
         foreach (Battler currBattler in battle.getDefenders()) {
            if (currBattler.health > 0) {
               battleEnds = false;
               break;
            }
         }
      }
      if (battleEnds && battle.isTeamDead(Battle.TeamType.Defenders)) {
         foreach (Battler currBattler in battle.getAttackers()) {
            if (currBattler.player is PlayerBodyEntity) {
               currBattler.player.Target_WinBattle();
            }
         }
      }
   }

   protected IEnumerator CO_KillBattlerAtEndTime (AttackAction attackAction) {
      // Wait for action end time
      double startTime = NetworkTime.time;
      while (NetworkTime.time < attackAction.actionEndTime - .1f) {
         yield return 0;
      }

      Battle battle = getBattle(attackAction.battleId);
      if (battle == null) {
         yield return null;
      }
      Battler target = battle.getBattler(attackAction.targetId);
      if (target == null) {
         yield return null;
      }

      // Declare target as dead using the syncvar which will be read by the clients
      if (target.enemyType != Enemy.Type.PlayerBattler) {
         D.adminLog("Setting the target as dead! Waiting time was {" + (NetworkTime.time - startTime) + "} TargetTime was: {" + attackAction.actionEndTime + "}", D.ADMIN_LOG_TYPE.DeathAnimDelay);
      }
      target.isAlreadyDead = true;
   }

   protected IEnumerator endBattleAfterDelay (Battle battle, float delay) {
      D.adminLog("1. {" + battle.battleId + "} End battle after delay", D.ADMIN_LOG_TYPE.CombatEnd);
      Battle.TeamType teamThatWon = battle.getTeamThatWon();

      // Wait a bit for the death animations to finish
      yield return new WaitForSeconds(delay);

      List<Battler> defeatedBattlers = (battle.teamThatWon == Battle.TeamType.Attackers) ? battle.getDefenders() : battle.getAttackers();
      List<Battler> winningBattlers = (battle.teamThatWon == Battle.TeamType.Attackers) ? battle.getAttackers() : battle.getDefenders();

      // Calculate how much gold to give the winners
      int goldWon = getGoldForDefeated(defeatedBattlers);

      // Calculate how much base XP to give the winners
      int baseXpWon = getXPForDefeated(defeatedBattlers);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Update the gold and XP in the database for the winners
         foreach (Battler participant in battle.getParticipants()) {
            if (participant.teamType == battle.teamThatWon) {
               int xpWon = baseXpWon;

               // If user has exp boost, add experience here
               if (LandPowerupManager.self.hasPowerup(participant.userId, LandPowerupType.ExperienceBoost)) {
                  float experienceValue = .2f; // Add 20% exp for now
                  xpWon = (int) (xpWon + (xpWon * experienceValue));
               }

               if (participant.enemyType == Enemy.Type.PlayerBattler && participant.player != null) {
                  if (participant.player.canGainXP(xpWon)) {
                     DB_Main.addGoldAndXP(participant.userId, goldWon, xpWon);
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        participant.player.Target_ReceiveBattleExp(participant.player.connectionToClient, xpWon);
                     });
                  } else {
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        if (participant.player != null) {
                           ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, participant.player, "No XP gained - max demo level reached.");
                        }
                     });
                  }
               }
            }
         }
      });

      // Update the XP amount on the PlayerController objects for the connected players
      foreach (Battler battler in winningBattlers) {
         if (!battler.isMonster()) {
            if (battler.player is PlayerBodyEntity) {
               PlayerBodyEntity body = (PlayerBodyEntity) battler.player;

               int xpWon = baseXpWon;
               // If user has exp boost, add experience here
               if (LandPowerupManager.self.hasPowerup(body.userId, LandPowerupType.ExperienceBoost)) {
                  float experienceValue = .2f; // Add 20% exp for now
                  xpWon = (int) (xpWon + (xpWon * experienceValue));
               }

               if (body.canGainXP(xpWon)) {
                  body.onGainedXP(body.XP, body.XP + xpWon);

                  body.XP += xpWon;
               }

               foreach (Battler companionBattlers in winningBattlers) {
                  if (companionBattlers.companionId > 0) {
                     // Reward each companion with battle exp
                     body.rpc.updateCompanionExp(companionBattlers.companionId, xpWon);
                  }
               }
            }
         } else {
            // Set the enemy entity voyage group id to -1 so they can engage in battle again
            battler.player.voyageGroupId = -1;
         }
      }

      if (teamThatWon == Battle.TeamType.Attackers && defeatedBattlers[0].getBattlerData().enemyType != Enemy.Type.PlayerBattler) {
         // Only Spawn one lootbag per combat win
         int battlerEnemyID = (int) defeatedBattlers[0].getBattlerData().enemyType;
         if (defeatedBattlers[0].player is Enemy) {
            // Filter spawn nodes
            List<Transform> filteredNodeTarget = ((Enemy) defeatedBattlers[0].player).lootSpawnPositions.ToList();
            foreach (Battler winningBattler in winningBattlers) {
               Transform spawnNode = filteredNodeTarget.Find(_ => (Vector2) _.position == (Vector2) winningBattler.transform.position);
               if (spawnNode != null) {
                  filteredNodeTarget.Remove(spawnNode);
               }
            }

            // Spawn Chest
            Enemy enemy = (Enemy) defeatedBattlers[0].player;
            List<Transform> spawnNodeTarget = enemy.lootSpawnPositions.ToList();
            Vector3 targetSpawnPos = filteredNodeTarget.Count > 0 ? filteredNodeTarget.ChooseRandom().position : spawnNodeTarget.ChooseRandom().position;

            // Offset spawn position of the loot spawn for boss monsters, due to their huge corpse sprite
            if (enemy.isBossType) {
               targetSpawnPos += new Vector3(0, .95f, 0);
            }

            StartCoroutine(CO_SpawnChest(winningBattlers[0].player, targetSpawnPos, winningBattlers[0].player.instanceId, battlerEnemyID));
         }
      }

      string playersEndedBattle = "Players: {";
      foreach (Battler winner in winningBattlers) {
         if (winner.enemyType == Enemy.Type.PlayerBattler && !battle.isPvp) {
            winner.player.rpc.endBattle(battle.battleId.ToString());
            playersEndedBattle += ": " + winner.player.userId;

            bool damageWeapon = false;
            bool damageArmor = false;

            // Randomize chance to deduct the weapon durability the player is currently wearing
            int itemDamageChance = Random.Range(0, Item.PERCENT_CHANCE);
            if (itemDamageChance < Item.WEAPON_DURABILITY_DEDUCTION) {
               damageWeapon = true;
            }

            // Randomize chance to deduct the armor durability the player is currently wearing
            itemDamageChance = Random.Range(0, Item.PERCENT_CHANCE);
            if (itemDamageChance < Item.ARMOR_DURABILITY_DEDUCTION) {
               damageArmor = true;
            }
            winner.player.rpc.modifyItemDurability(winner.player, damageWeapon == false ? -1 : winner.weaponManager.equippedWeaponId, Item.ITEM_DURABILITY_DEDUCTION,
               damageArmor == false ? -1 : winner.armorManager.equippedArmorId, Item.ITEM_DURABILITY_DEDUCTION);
         } else if (winner.enemyType == Enemy.Type.PlayerBattler && battle.isPvp) {
            winner.player.rpc.Target_DisplayServerMessage("You have defeated " + defeatedBattlers[0].player.entityName + " in a duel!");
         }
      }

      // Process monster type reward
      foreach (Battler battler in defeatedBattlers) {
         if (battler.isMonster()) {
            int battlerEnemyID = (int) battler.getBattlerData().enemyType;

            foreach (Battler participant in winningBattlers) {
               if (!participant.isMonster()) {
                  // Registers the kill count of the combat
                  AchievementManager.registerUserAchievement(participant.player, ActionType.KillLandMonster, defeatedBattlers.Count);

                  // Registers the gold earned for achievement recording
                  AchievementManager.registerUserAchievement(participant.player, ActionType.EarnGold, goldWon);
               }
            }
         } else {
            if (battler.player is PlayerBodyEntity) {
               if (!battle.isPvp) {
                  // Registers the death of the player in combat
                  AchievementManager.registerUserAchievement(battler.player, ActionType.CombatDie);

                  // TODO: Implement logic here that either warps the player to a different place or give invulnerability time so they can move away from previous enemy, otherwise its an unlimited battle until the player wins
                  if (battler.player != null) {
                     if (battler.player.isInGroup() && VoyageManager.isTreasureSiteArea(battler.player.areaKey)) {
                        // When dying in a voyage treasure site, respawn at its entrance
                        Vector2 spawnLocalPosition = SpawnManager.self.getDefaultLocalPosition(battler.player.areaKey);
                        battler.player.transform.localPosition = spawnLocalPosition;
                        LandPowerupManager.self.clearPowerupsForUser(battler.player.userId);
                        battler.player.rpc.Target_UpdateLandPowerups(battler.player.connectionToClient, LandPowerupManager.self.getPowerupsForUser(battler.player.userId));
                        battler.player.rpc.Target_RespawnAtTreasureSiteEntrance(battler.player.connectionToClient, spawnLocalPosition);
                     } else {
                        LandPowerupManager.self.clearPowerupsForUser(battler.player.userId);
                        battler.player.rpc.Target_UpdateLandPowerups(battler.player.connectionToClient, LandPowerupManager.self.getPowerupsForUser(battler.player.userId));
                        battler.player.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.North);
                     }
                  }
               } else {
                  battler.player.rpc.Target_DisplayServerMessage("You have been defeated by " + winningBattlers[0].player.entityName + " in a duel!");
               }
            }
         }
      }
      D.adminLog("2. {" + battle.battleId + "} End battle processing done, PlayerCount: {" + winningBattlers.Count + "} " + playersEndedBattle + "}", D.ADMIN_LOG_TYPE.CombatEnd);

      // Pass along the request to the Battle Manager to handle shutting everything down
      this.endBattle(battle, teamThatWon);
   }

   private IEnumerator CO_SpawnChest (NetEntity winningBattler, Vector3 targetSpawnPos, int battlerInstanceId, int enemyId) {
      yield return new WaitForSeconds(.1f);

      // This prevents the chest from bounding over obstructed areas
      int collisionCount = 40;
      Collider2D[] hits = new Collider2D[collisionCount];
      float radiusSize = 0.005f;
      Physics2D.OverlapCircleNonAlloc(targetSpawnPos, radiusSize, hits);
      int index = 0;

      // Check if initial spawning collides with any map objects
      if (hits.Length > 0) {
         foreach (Collider2D hit in hits) {
            if (hit != null) {
               index++;
            }
         }
      }

      Vector3 newSpawnPosition = targetSpawnPos;
      if (index > 0) {
         int tryCount = 100;
         float internalRadiusSize = 0.005f;
         float externalRadiusSize = 0.4f;
         for (int i = 0; i < tryCount; i++) {
            int internalCollisionCount = 10;
            Collider2D[] internalHits = new Collider2D[internalCollisionCount];
            Physics2D.OverlapCircleNonAlloc(newSpawnPosition, internalRadiusSize, internalHits);

            int objectHits = 0;
            foreach (Collider2D hit in internalHits) {
               if (hit != null && hit.gameObject != null) {
                  // Ignore camera bounds, check for object collisions
                  if (hit.GetComponent<PolygonCollider2D>() == null) {
                     objectHits++;
                  }
               }
            }

            // If collided with anything, find random position that has no collider
            if (internalHits.Length > 0 && objectHits > 0) {
               Vector3 offset = Random.insideUnitCircle * externalRadiusSize;
               newSpawnPosition = targetSpawnPos + offset;
            } else {
               break;
            }
         }
      }

      winningBattler.rpc.spawnBattlerMonsterChest(battlerInstanceId, newSpawnPosition, enemyId);
   }

   public void onPlayerEquipItem (PlayerBodyEntity playerBody) {
      Battler battler = getBattler(playerBody.userId);
      if (battler != null && playerBody != null) {
         D.adminLog("Player {" + battler.userId + "} has equipped item", D.ADMIN_LOG_TYPE.Equipment);
         assignBattlerSyncData(battler, playerBody, false);
         playerBody.rpc.processPlayerAbilities(playerBody, new List<PlayerBodyEntity>() { playerBody });
      } else {
         D.debug("Error! Failed to process player ability! Missing Battler/PlayerBody");
      }
   }

   public List<BattlerData> getAllBattlersData () { return _allBattlersData; }

   public Dictionary<int, Battle> getActiveBattlersData () { return _activeBattles; }

   private float getStanceCritChance (Battler.Stance stance) {
      switch (stance) {
         case Battler.Stance.Attack:
            return 0.4f;
         case Battler.Stance.Balanced:
            return 0.25f;
         case Battler.Stance.Defense:
            return 0.1f;
      }

      return 0.25f;
   }

   private float getStanceBlockChance (Battler.Stance stance) {
      switch (stance) {
         case Battler.Stance.Defense:
            return 0.4f;
         case Battler.Stance.Balanced:
            return 0.25f;
         case Battler.Stance.Attack:
            return 0.1f;
      }

      return 0.25f;
   }

   public void onBattlerDeath (Battler battler) {
      if (!NetworkServer.active) {
         return;
      }

      if (battler.battlerType == BattlerType.AIEnemyControlled) {
         // Silver management
         int silverReward = battler.isBossType ? SilverManager.SILVER_PLAYER_LAND_BATTLE_BOSS_KILL_REWARD : SilverManager.SILVER_PLAYER_LAND_BATTLE_KILL_REWARD;

         // Get the battlers in the opposite team
         Battle.TeamType otherTeam = (battler.teamType == Battle.TeamType.Attackers) ? Battle.TeamType.Defenders : Battle.TeamType.Attackers;
         List<Battler> otherBattlers = battler.battle.getTeam(otherTeam);

         // Compute position
         Vector3 silverBurstEffectOffsetWhenFacingEast = new Vector3(-0.194f, -0.129f, 0);
         Vector3 silverBurstEffectOffsetWhenFacingWest = silverBurstEffectOffsetWhenFacingEast;
         silverBurstEffectOffsetWhenFacingWest.Scale(new Vector3(-1.0f, 1.0f, 1.0f));
         Vector3 spotPosition = battler.battleSpot.transform.position;
         Vector3 shiftedSpotPosition = new Vector3(spotPosition.x, spotPosition.y, 0);
         shiftedSpotPosition += ((battler.teamType == Battle.TeamType.Defenders) ? silverBurstEffectOffsetWhenFacingWest : silverBurstEffectOffsetWhenFacingEast);
         StartCoroutine(CO_ProcessSilverAfterBattlerDeath(shiftedSpotPosition, otherBattlers, battler, silverReward, 1.5f));

      } else if (battler.battlerType == BattlerType.PlayerControlled && GameStatsManager.self.isUserRegistered(battler.userId)) {
         battler.player.rpc.assignVoyageRatingPoints(VoyageRatingManager.computeVoyageRatingPointsReward(VoyageRatingManager.RewardReason.DeathOnLand));
         D.debug($"Battler '{battler.userId}' died in battle.");
         int penalty = SilverManager.computeSilverPenalty(battler.player);
         D.debug($"Battler '{battler.userId}' received a penalty of {penalty} silver.");
         GameStatsManager.self.addSilverAmount(battler.userId, -penalty);
         battler.player.Target_ReceiveSilverCurrency(battler.player.connectionToClient, -penalty, SilverManager.SilverRewardReason.Death);
      }
   }

   private IEnumerator CO_ProcessSilverAfterBattlerDeath (Vector3 position, List<Battler> opposingBattlers, Battler deadBattler, int silverReward, float delay) {
      yield return new WaitForSeconds(delay);
      bool shouldRewardSilver = opposingBattlers.Any(_ => GameStatsManager.self.isUserRegistered(_.userId));

      if (shouldRewardSilver) {
         deadBattler.Rpc_ShowSilverBurstEffect();
         deadBattler.Rpc_ReceiveSilverCurrencyWithEffect(silverReward, SilverManager.SilverRewardReason.Kill, position);

         foreach (Battler opposingBattler in opposingBattlers) {
            GameStatsManager.self.addSilverAmount(opposingBattler.player.userId, silverReward);
         }
      }
   }

   #region Private Variables

   // Stores the Battle Board used for each Biome Type
   protected Dictionary<int, BattleBoard> _boards = new Dictionary<int, BattleBoard>();

   // Stores Battles by their Battle ID
   protected Dictionary<int, Battle> _battles = new Dictionary<int, Battle>();

   // Stored references of the battlers that are currently in a battle
   private Dictionary<int, Battler> _battlers = new Dictionary<int, Battler>();

   // Keeps track of which user IDs are associated with which Battles
   protected Dictionary<int, Battle> _activeBattles = new Dictionary<int, Battle>();

   // The unique ID we assign to new Battle objects that are created
   protected int _id = 1;

   // The unique ID we assign to enemy Battlers that are created
   protected int _enemyId = -1;

   // All battlers in the game
   [SerializeField] private List<BattlerData> _allBattlersData = new List<BattlerData>();

   #endregion
}
