using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;

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

   // Self
   public static BattleManager self;

   // Holds the prefab reference for each monster type
   public List<PrefabTypes> prefabTypes;

   #endregion

   public void Awake () {
      self = this;
   }

   public void Start () {
      // Store all of the Battle Boards that exist in the Scene
      foreach (BattleBoard board in FindObjectsOfType<BattleBoard>()) {
         _boards[board.biomeType] = board;

         // TEMP -- for now, we only have one board for all biome types
         foreach (Biome.Type biomeType in System.Enum.GetValues(typeof(Biome.Type))) {
            _boards[biomeType] = board;
         }
      }

      // Repeatedly call the tick() function for our Battle objects
      InvokeRepeating("tickBattles", 0f, TICK_INTERVAL);
   }
   
   public Battle createBattle (Area area, Instance instance, Enemy enemy, PlayerBodyEntity playerBody) {
      // We need to make a new one
      Battle battle = Instantiate(battlePrefab);

      // Look up the Battle Board for this Area's tile type
      Biome.Type biomeType = Area.getBiome(area.areaKey);
      BattleBoard battleBoard = _boards[biomeType];

      // Set up our initial data and position
      battle.battleId = _id++;
      battle.area = area;
      battle.biomeType = biomeType;
      battle.battleBoard = battleBoard;
      battle.transform.SetParent(this.transform);
      Util.setXY(battle.transform, battleBoard.transform.position);

      // Actually spawn the Battle as a Network object now
      NetworkServer.Spawn(battle.gameObject);

      // Keep track of the Battles we create
      _battles[battle.battleId] = battle;

      // Check how many players are in the Instance
      int playersInInstance = instance.getPlayerCount();

      // Hashset for enemy types in the battle sequence
      HashSet<Enemy.Type> enemyTypes = new HashSet<Enemy.Type>();

      // Spawn an appropriate number of enemies based on the number of players in the instance
      for (int i = 0; i < getEnemyCount(enemy, playersInInstance); i++) {
         enemyTypes.Add(enemy.enemyType);
         this.addEnemyToBattle(battle, enemy, Battle.TeamType.Defenders, playerBody);
      }

      // Provides the clients with the list of monsters present in the battle sequence
      playerBody.rpc.Cmd_ProcessMonsterData(new List<Enemy.Type>(enemyTypes).ToArray());

      return battle;
   }

   public Battle getBattle (int battleId) {
      if (_battles.ContainsKey(battleId)) {
         return _battles[battleId];
      }

      return null;
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

   public BattleBoard getBattleBoard (Biome.Type biomeType) {
      return _boards[biomeType];
   }
   
   public void addPlayerToBattle (Battle battle, PlayerBodyEntity player, Battle.TeamType teamType) {
      // Maintain a Mapping of which players are in which Battles
      _activeBattles[player.userId] = battle;

      // The Player needs to return to this specific server if they reconnect
      ServerNetwork.self.claimPlayer(player.userId);

      // Create a Battler for this Player
      Battler battler = createBattlerForPlayer(battle, player, teamType);
      BattleManager.self.storeBattler(battler);

      // Add the Battler to the Battle
      if (teamType == Battle.TeamType.Attackers) {
         battle.attackers.Add(battler.userId);
      } else if (teamType == Battle.TeamType.Defenders) {
         battle.defenders.Add(battler.userId);
      }

      // Assign the Battle ID to the Sync Var
      player.battleId = battle.battleId;

      // Registers the action of combat entry to the achievement database for recording
      AchievementManager.registerUserAchievement(player.userId, ActionType.EnterCombat);

      // Update the observers associated with the Battle and the associated players
      rebuildObservers(battler, battle);
   }
   
   public void addEnemyToBattle (Battle battle, Enemy enemy, Battle.TeamType teamType, PlayerBodyEntity aggressor) {
      // Create a Battler for this Enemy
      Battler battler = createBattlerForEnemy(battle, enemy, teamType);
      self.storeBattler(battler);

      // Add the Battler to the Battle
      if (teamType == Battle.TeamType.Attackers) {
         battle.attackers.Add(battler.userId);
      } else if (teamType == Battle.TeamType.Defenders) {
         battle.defenders.Add(battler.userId);
      }

      // Assign the Battle ID to the Sync Var, causing movement to stop and facing direction to change
      enemy.assignBattleId(battle.battleId, aggressor);

      // Assign the Net Entity
      battler.player = enemy;

      // Update the observers associated with the Battle and the associated players
      rebuildObservers(battler, battle);
   }

   public void endBattle (Battle battle, Battle.TeamType winningTeam) {
      // Remove the Battle ID for any participants
      battle.resetAllBattleIDs();

      // Cycle over each Battler in the Battle
      foreach (Battler battler in battle.getParticipants()) {
         // Update our internal mapping of users to battles
         _activeBattles[battler.userId] = null;

         // If this was a Player, we can release the server claim now, so they can relog into any server
         if (battler.userId > 0) {
            ServerNetwork.self.releaseClaim(battler.userId);
         }

         // Warp any losing players back to the starting town, or show the death animation for Enemies
         battler.handleEndOfBattle(winningTeam);

         // Destroy the Battler from the Network
         NetworkServer.Destroy(battler.gameObject);
      }

      // Destroy the Battle from the Network
      NetworkServer.Destroy(battle.gameObject);
   }

   public void registerBattler (BattlerData battler) {
      if(_allBattlersData.Exists(_=>_.enemyType == battler.enemyType)) {
         Debug.LogWarning("Duplicated Type: " + battler.enemyType);
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
         }
      }
   }

   protected Battler createBattlerForPlayer (Battle battle, PlayerBodyEntity player, Battle.TeamType teamType) {
      // We need to make a new one
      Battler battler = Instantiate(baseBattlerPrefab);
      battler.battlerType = BattlerType.PlayerControlled;

      // Set up our initial data and position
      battler.playerNetId = player.netId;
      battler.player = player;
      battler.battle = battle;
      battler.userId = player.userId;

      // This function will handle the computed stats of the player depending on their Specialty/Faction/Job/Class
      battler.initialize();
      battler.health = battler.getStartingHealth(Enemy.Type.PlayerBattler);

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
      battler.hairColor1 = player.hairColor1;
      battler.hairColor2 = player.hairColor2;
      battler.eyesColor1 = player.eyesColor1;

      // Figure out which Battle Spot we should be placed in
      battler.boardPosition = battle.getTeam(teamType).Count + 1;
      BattleSpot battleSpot = battle.battleBoard.getSpot(teamType, battler.boardPosition);
      battler.battleSpot = battleSpot;
      battler.transform.position = battleSpot.transform.position;

      // Actually spawn the Battler as a Network object now
      NetworkServer.Spawn(battler.gameObject);

      // Copy the Armor Info
      battler.armorManager.updateArmorSyncVars(player.armorManager.getArmor());
      battler.armorManager.armorType = player.armorManager.armorType;
      battler.armorManager.color1 = player.armorManager.color1;
      battler.armorManager.color2 = player.armorManager.color2;

      // Copy the Weapon Info
      battler.weaponManager.updateWeaponSyncVars(player.weaponManager.getWeapon());
      battler.weaponManager.weaponType = player.weaponManager.weaponType;
      battler.weaponManager.color1 = player.weaponManager.color1;
      battler.weaponManager.color2 = player.weaponManager.color2;

      return battler;
   }

   private Battler createBattlerForEnemy (Battle battle, Enemy enemy, Battle.TeamType teamType) {
      Enemy.Type overrideType = Enemy.Type.Lizard;
      Battler enemyPrefab = prefabTypes.Find(_ => _.enemyType == Enemy.Type.Lizard).enemyPrefab;

      _spawnCounter++;
      // For testing Purposes, adds a chance to spawn a Golem Monster
      if (_spawnCounter == 2) {
         Debug.Log("Spawning a Debug Monster, Delete after feature completion!");
         overrideType = Enemy.Type.Golem;
      }
      BattlerData data = getAllBattlersData().Find(x => x.enemyType == overrideType);
      Battler battler = Instantiate(enemyPrefab);
      battler.enemyType = overrideType;
      battler.name = data.enemyName;

      // Set starting stats
      battler.health = battler.getStartingHealth(overrideType);

      // Set up our initial data and position
      battler.playerNetId = enemy.netId;
      battler.player = enemy;
      battler.battle = battle;
      battler.battleId = battle.battleId;
      battler.transform.SetParent(battle.transform);
      battler.teamType = teamType;
      battler.userId = _enemyId--;

      // Figure out which Battle Spot we should be placed in
      battler.boardPosition = battle.getTeam(teamType).Count + 1;
      BattleSpot battleSpot = battle.battleBoard.getSpot(teamType, battler.boardPosition);
      battler.battleSpot = battleSpot;
      battler.transform.position = battleSpot.transform.position;

      // Actually spawn the Battler as a Network object now
      NetworkServer.Spawn(battler.gameObject);
      battler.transform.SetParent(battle.transform, false);

      return battler;
   }

   protected void rebuildObservers (Battler newBattler, Battle battle) {
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
      if (enemy.isBoss()) {
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

   public void executeBattleAction (Battle battle, Battler source, List<Battler> targets, int abilityInventoryIndex, AbilityType abilityType) {
      // Get ability reference from the source battler, cause the source battler is the one executing the ability
      BasicAbilityData abilityData = new BasicAbilityData();

      if (abilityType == AbilityType.Standard) {
         abilityData = source.getAttackAbilities()[abilityInventoryIndex];
      } else if (abilityType == AbilityType.BuffDebuff) {
         abilityData = source.getBuffAbilities()[abilityInventoryIndex];
      }

      BattleActionType actionType = BattleActionType.UNDEFINED;

      bool wasBlocked = false;
      bool wasCritical = false;
      bool isMultiTarget = targets.Count > 1;
      float timeToWait = battle.getTimeToWait(source, targets);

      List<BattleAction> actions = new List<BattleAction>();

      // String action list that will be sent to clients
      List<string> stringList = new List<string>();

      if (abilityData == null) {
         D.error("The Ability Data is NULL!! : "+ source.enemyType);
         return;
      }
      if (abilityData.abilityType == AbilityType.Standard) {
         AttackAbilityData attackAbilityData = abilityData as AttackAbilityData;

         // Apply the AP change
         int sourceApChange = abilityData.apChange;
         source.addAP(sourceApChange);

         foreach (Battler target in targets) {
            // For now, players have a 50% chance of blocking monsters
            if (target.canBlock() && attackAbilityData.canBeBlocked) {
               wasBlocked = Random.Range(0f, 1f) > .50f;
            }

            // If the attack wasn't blocked, it can be a critical
            if (!wasBlocked) {
               wasCritical = Random.Range(0f, 1f) > .50f;
            }

            // TODO: Temporary remove critical and block possibilities
            wasCritical = false;
            wasBlocked = false;

            // Adjust the damage amount based on element, ability, and the target's armor
            Element element = abilityData.elementType;

            float sourceDamageElement = source.getDamage(element);
            float damage = sourceDamageElement * attackAbilityData.getModifier;

            float targetDefenseElement = target.getDefense(element);

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

            // Decrease damage on protected targets
            if (target.isProtected(battle)) {
               decreaseMultiply *= .70f;
            }

            // TODO: Temporary Remove additive for simplified computation
            increaseAdditive = 0;
            decreaseMultiply = 1;

            // Apply the adjustments to the damage
            damage *= (1f + increaseAdditive);
            damage *= decreaseMultiply;
            
            // Make note of the time that this battle action is going to be fully completed, considering animation times
            float timeAttackEnds = Util.netTime() + timeToWait + attackAbilityData.getTotalAnimLength(source, target);

            float cooldownDuration = abilityData.abilityCooldown * source.getCooldownModifier();
            source.cooldownEndTime = timeAttackEnds + cooldownDuration;

            // Apply the target's AP change
            int targetApChange = target.getApWhenDamaged();
            target.addAP(targetApChange);

            AttackAction.ActionType currentActionType = AttackAction.ActionType.Melee;
            if (attackAbilityData.abilityActionType == AbilityActionType.Ranged) {
               currentActionType = AttackAction.ActionType.Range;
            }

            // Create the Action object
            AttackAction action = new AttackAction(battle.battleId, currentActionType, source.userId, target.userId,
                (int) damage, timeAttackEnds, abilityInventoryIndex, wasCritical, wasBlocked, cooldownDuration, sourceApChange,
                targetApChange, abilityData.itemID, damageEffectMagnitude);
            actions.Add(action);

            // Make note how long the two Battler objects need in order to execute the attack/hit animations
            source.animatingUntil = timeAttackEnds;
            target.animatingUntil = timeAttackEnds;

            // Wait to apply the effects of the action here on the server until the appointed time
            StartCoroutine(applyActionAfterDelay(timeToWait, action, isMultiTarget));

            foreach (AttackAction attackAction in actions) {
               stringList.Add(action.serialize());
            }
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
            float timeBuffEnds = Util.netTime() + timeToWait + buffAbility.getTotalAnimLength(source, target);
            float cooldownDuration = abilityData.abilityCooldown * source.getCooldownModifier();
            source.cooldownEndTime = timeBuffEnds + cooldownDuration;

            // Apply the target's AP change
            int targetApChange = target.getApWhenDamaged();
            target.addAP(targetApChange);

            // Create the Action object
            BuffAction action = new BuffAction(battle.battleId, 0, source.userId, target.userId, timeBuffEnds, timeBuffEnds + 10, cooldownDuration, timeBuffEnds, sourceApChange, targetApChange, abilityData.itemID, buffAbility.value, buffAbility.elementType, buffAbility.bonusStatType);
            
            actions.Add(action);

            // Make note how long the two Battler objects need in order to execute the cast effect animations
            source.animatingUntil = timeBuffEnds;
            target.animatingUntil = timeBuffEnds;

            // Wait to apply the effects of the action here on the server until the appointed time
            StartCoroutine(applyActionAfterDelay(timeToWait, action, isMultiTarget));

            foreach (BuffAction buffAction in actions) {
               stringList.Add(action.serialize());
            }
         }
      } else {
         D.log("Battle action was not prepared correctly");
      }

      // Send it to clients
      battle.Rpc_SendCombatAction(stringList.ToArray(), actionType);
   }

   // Stance action does not requires another target or an ability inventory index, thus, being removed from executeBattleAction
   public void executeStanceChangeAction (Battle battle, Battler source, Battler.Stance newStance) {
      float timeActionEnds = Util.netTime() + source.getStanceCooldown(newStance);
      StanceAction stanceAction = new StanceAction(battle.battleId, source.userId, timeActionEnds, newStance);

      string serializedValue = stanceAction.serialize();
      string[] values = new[] { serializedValue };

      battle.Rpc_SendCombatAction(values, BattleActionType.Stance);
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

   protected IEnumerator applyActionAfterDelay (float timeToWait, BattleAction actionToApply, bool hasMultipleTargets) {
      yield return new WaitForSeconds(timeToWait);

      // Check if the battle has ended
      Battle battle = getBattle(actionToApply.battleId);
      if (battle == null) {
         yield break;
      }

      // Get the Battler object
      Battler source = battle.getBattler(actionToApply.sourceId);

      if (actionToApply is AttackAction || actionToApply is BuffAction) {
         BattleAction action = (BattleAction) actionToApply;
         Battler target = battle.getBattler(action.targetId);

         // ZERONEV-COMMENT: It is supossed we are still grabbing the ability from the source battler to apply it
         // So we will grab the source battler
         AttackAbilityData abilityData = source.getAttackAbilities()[action.abilityInventoryIndex];

         // If the source or target is already dead, then send a Cancel Action
         if (source.isDead() || target.isDead()) {
            // Don't create Cancel Actions for multi-target abilities
            if (hasMultipleTargets) {
               yield break;
            }

            // Remove the action cooldown and animation duration from the source's timestamps
            float animLength = abilityData.getTotalAnimLength(source, target);
            float timeToSubtract = action.cooldownDuration + animLength;

            // Update the battler's action timestamps here on the server
            target.animatingUntil -= animLength;
            source.animatingUntil -= animLength;
            source.cooldownEndTime -= timeToSubtract;

            // Create a Cancel Action to send to the clients
            CancelAction cancelAction = new CancelAction(action.battleId, action.sourceId, action.targetId, Util.netTime(), timeToSubtract);
            AbilityManager.self.execute(new[] { cancelAction });

         } else {
            if (action is AttackAction) {
               AttackAction attackAction = (AttackAction) action;

               // Registers the usage of the Offensive Skill for achievement recording
               AchievementManager.registerUserAchievement(source.player.userId, ActionType.OffensiveSkillUse);

               // Applies damage delay for abilities with extra animation durations such as casting and aiming
               float delayMagnitude = .5f;
               float attackApplyDelay = 0;
               AttackAbilityData abilityDataReference = (AttackAbilityData) AbilityManager.getAbility(action.abilityGlobalID, AbilityType.Standard);
               if (abilityDataReference.abilityActionType == AbilityActionType.Ranged || abilityDataReference.abilityActionType == AbilityActionType.CastToTarget) {
                  attackApplyDelay += abilityDataReference.getTotalAnimLength(source, target) * delayMagnitude;
               }

               yield return new WaitForSeconds(attackApplyDelay);

               // Apply damage
               target.health -= attackAction.damage;
               target.health = Util.clamp<int>(target.health, 0, target.getStartingHealth());

            } else if (action is BuffAction) {
               BuffAction buffAction = (BuffAction) action;

               // Registers the usage of the Buff Skill for achievement recording
               AchievementManager.registerUserAchievement(source.player.userId, ActionType.BuffSkillUse);

               // Apply the Buff
               target.addBuff(buffAction.getBuffTimer());
            }
         }
      }
   }

   protected IEnumerator endBattleAfterDelay (Battle battle, float delay) {
      Battle.TeamType teamThatWon = battle.getTeamThatWon();
      List<Battler> defeatedBattlers = (battle.teamThatWon == Battle.TeamType.Attackers) ? battle.getDefenders() : battle.getAttackers();
      List<Battler> winningBattlers = (battle.teamThatWon == Battle.TeamType.Attackers) ? battle.getAttackers() : battle.getDefenders();

      // Wait a bit for the death animations to finish
      yield return new WaitForSeconds(delay);

      // Calculate how much gold to give the winners
      int goldWon = getGoldForDefeated(defeatedBattlers);

      // Calculate how much XP to give the winners
      int xpWon = getXPForDefeated(defeatedBattlers);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Update the gold and XP in the database for the winners
         foreach (Battler participant in battle.getParticipants()) {
            if (participant.teamType == battle.teamThatWon) {
               DB_Main.addGoldAndXP(participant.userId, goldWon, xpWon);
            }
         }
      });

      // Update the XP amount on the PlayerController objects for the connected players
      foreach (Battler battler in winningBattlers) {
         if (!battler.isMonster()) {
            if (battler.player is PlayerBodyEntity) {
               PlayerBodyEntity body = (PlayerBodyEntity) battler.player;
               body.XP += xpWon;
            }
         }
      }

      // Process monster type reward
      foreach (Battler battler in defeatedBattlers) {
         if (battler.isMonster()) {
            int battlerEnemyID = (int) battler.getBattlerData().enemyType;
            foreach (Battler participant in winningBattlers) {
               if (!participant.isMonster()) {
                  Vector3 chestPos = BodyManager.self.getBody(participant.player.userId).transform.position;

                  // Registers the kill count of the combat
                  AchievementManager.registerUserAchievement(participant.player.userId, ActionType.KillLandMonster, defeatedBattlers.Count);

                  // Registers the gold earned for achievement recording
                  AchievementManager.registerUserAchievement(participant.player.userId, ActionType.EarnGold, goldWon);

                  participant.player.rpc.spawnBattlerMonsterChest(participant.player.instanceId, chestPos, battlerEnemyID);
               }
            }
         } else {
            if (battler.player is PlayerBodyEntity) {
               // Registers the death of the player in combat
               AchievementManager.registerUserAchievement(battler.player.userId, ActionType.CombatDie);
            }
         }
      }

      // Pass along the request to the Battle Manager to handle shutting everything down
      this.endBattle(battle, teamThatWon);
   }

   public List<BattlerData> getAllBattlersData () { return _allBattlersData; }

   #region Private Variables

   // Stores the Battle Board used for each Biome Type
   protected Dictionary<Biome.Type, BattleBoard> _boards = new Dictionary<Biome.Type, BattleBoard>();

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

   // A Test variable only for spawning variety of monsters
   protected int _spawnCounter = 0;

   // All battlers in the game
#pragma warning disable
   [SerializeField] private List<BattlerData> _allBattlersData;
#pragma warning restore

   #endregion
}
