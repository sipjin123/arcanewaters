using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BattleManager : MonoBehaviour {
   #region Public Variables

   // The amount of time we wait after a battle ends before moving players out of the battle view
   public static float END_BATTLE_DELAY = 3f;

   // How long we wait between consecutive battle ticks
   public static float TICK_INTERVAL = .5f;

   // The Prefab we use for creating new Battles
   public Battle battlePrefab;

   // The Prefab we use for creating new player Battlers
   public Battler battlerPrefab;

   // Self
   public static BattleManager self;

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
      Biome.Type biomeType = Area.getBiome(area.areaType);
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

      // Spawn an appropriate number of enemies based on the number of players in the instance
      for (int i = 0; i < getEnemyCount(enemy, playersInInstance); i++) {
         this.addEnemyToBattle(battle, enemy, Battle.TeamType.Defenders, playerBody);
      }

      return battle;
   }

   public Battle getBattle (int battleId) {
      return _battles[battleId];
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

   public BattleBoard getBattleBoardForBattler (Battler battler) {
      // Cycle over all of our Battle Boards
      foreach (BattleBoard battleBoard in _boards.Values) {
         // Check for one that contains the Battler position
         if (Vector2.Distance(battleBoard.transform.position, battler.transform.position) <= 5f) {
            return battleBoard;
         }
      }

      return null;
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

      // Initialize player battler abilities.
      battler.initAbilities();

      // Add the Battler to the Battle
      if (teamType == Battle.TeamType.Attackers) {
         battle.attackers.Add(battler.userId);
      } else if (teamType == Battle.TeamType.Defenders) {
         battle.defenders.Add(battler.userId);
      }

      // Assign the Battle ID to the Sync Var
      player.battleId = battle.battleId;

      // Update the observers associated with the Battle and the associated players
      rebuildObservers(battler, battle);
   }

   public void addEnemyToBattle (Battle battle, Enemy enemy, Battle.TeamType teamType, PlayerBodyEntity aggressor) {
      // Create a Battler for this Enemy
      MonsterBattler battler = createBattlerForEnemy(battle, enemy, teamType);

      // Initialize enemy abilities
      battler.initAbilities();
      BattleManager.self.storeBattler(battler);

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

      battle.onBattleEnded.Invoke();

      // Destroy the Battle from the Network
      NetworkServer.Destroy(battle.gameObject);
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
      Battler battler = Instantiate(battlerPrefab);

      // Set up our initial data and position
      battler.playerNetId = player.netId;
      battler.player = player;
      battler.battle = battle;
      battler.userId = player.userId;
      battler.battleId = battle.battleId;
      battler.teamType = teamType;
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

      // Set starting stats
      battler.health = battler.getStartingHealth();

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

      // Player battler abilities:
      battler.battlerBaseAbilities = AbilityInventory.self.equippedAbilitiesBPs;
      battler.initAbilities();

      return battler;
   }

   protected MonsterBattler createBattlerForEnemy (Battle battle, Enemy enemy, Battle.TeamType teamType) {
      // We need to make a new one
      MonsterBattler enemyPrefab = Resources.Load<MonsterBattler>("Battlers/" + enemy.enemyType + "_Battler");
      MonsterBattler battler = Instantiate(enemyPrefab);

      // Set up our initial data and position
      battler.playerNetId = enemy.netId;
      battler.player = enemy;
      battler.battle = battle;
      battler.battleId = battle.battleId;
      battler.transform.SetParent(battle.transform);
      battler.enemyType = enemy.enemyType;
      battler.teamType = teamType;
      battler.userId = _enemyId--;

      // Set starting stats
      battler.health = battler.getStartingHealth();

      // Figure out which Battle Spot we should be placed in
      battler.boardPosition = battle.getTeam(teamType).Count + 1;
      BattleSpot battleSpot = battle.battleBoard.getSpot(teamType, battler.boardPosition);
      battler.battleSpot = battleSpot;
      battler.transform.position = battleSpot.transform.position;

      battler.onBattlerSelect.AddListener(() => {
         BattleUIManager.self.triggerTargetUI(battler);
      });

      battler.onBattlerDeselect.AddListener(() => {
         BattleUIManager.self.hideTargetGameobjectUI();
      });

      // Actually spawn the Battler as a Network object now
      NetworkServer.Spawn(battler.gameObject);

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

   /// <summary>
   /// Executes an attack
   /// </summary>
   /// <param name="battle"> Battle reference </param>
   /// <param name="source"> Source battler that will execute the attack</param>
   /// <param name="targets"> Targets for the attack </param>
   /// <param name="abilityIndex"> Index to know which ability data to grab when executing the attack </param>
   public void executeAttack (Battle battle, Battler source, List<Battler> targets, int abilityInventoryIndex) {
      bool wasBlocked = false;
      bool wasCritical = false;
      bool isMultiTarget = targets.Count > 1;
      float timeToWait = battle.getTimeToWait(source, targets);

      // Get ability reference from the source battler, cause the source battler is the one executing the ability.
      AbilityData abilityData = source.getAbilities[abilityInventoryIndex];
      //Ability ability = AbilityManager.getAbility(abilityType);
      List<AttackAction> actions = new List<AttackAction>();

      // Apply the AP change
      int sourceApChange = abilityData.getApChange();
      source.addAP(sourceApChange);

      foreach (Battler target in targets) {
         // For now, players have a 50% chance of blocking monsters
         if (target.canBlock() && abilityData.getBlockStatus()) {
            wasBlocked = Random.Range(0f, 1f) > .50f;
         }

         // If the attack wasn't blocked, it can be a critical
         if (!wasBlocked) {
            wasCritical = Random.Range(0f, 1f) > .50f;
         }

         // Adjust the damage amount based on element, ability, and the target's armor
         Element element = abilityData.getElementType();
         float damage = source.getDamage(element) * abilityData.getModifier;
         damage *= (100f / (100f + target.getDefense(element)));
         float increaseAdditive = 0f;
         float decreaseMultiply = 1f;
         decreaseMultiply *= wasBlocked ? .50f : 1f;
         increaseAdditive += wasCritical ? .50f : 0f;

         // Adjust the damage based on the source and target stances
         increaseAdditive += (source.stance == Battler.Stance.Attack) ? .25f : 0f;
         decreaseMultiply *= (source.stance == Battler.Stance.Defense) ? .75f : 1f;
         if (!abilityData.isHeal()) {
            increaseAdditive += (target.stance == Battler.Stance.Attack) ? .25f : 0f;
            decreaseMultiply *= (target.stance == Battler.Stance.Defense) ? .75f : 1f;
         }

         // Decrease damage on protected targets
         if (target.isProtected(battle)) {
            decreaseMultiply *= .70f;
         }

         // Apply the adjustments to the damage
         damage *= (1f + increaseAdditive);
         damage *= decreaseMultiply;

         // Make note of the time that this battle action is going to be fully completed, considering animation times
         float timeAttackEnds = Util.netTime() + timeToWait + abilityData.getTotalAnimLength(source, target);
         float cooldownDuration = abilityData.getCooldown() * source.getCooldownModifier();
         source.cooldownEndTime = timeAttackEnds + cooldownDuration;

         // Apply the target's AP change
         int targetApChange = target.getApWhenDamagedBy(abilityData);
         target.addAP(targetApChange);

         // Create the Action object
         AttackAction action = new AttackAction(battle.battleId, AttackAction.ActionType.Melee, source.userId, target.userId,
             (int) damage, timeAttackEnds, abilityInventoryIndex, wasCritical, wasBlocked, cooldownDuration, sourceApChange, 
             targetApChange, abilityData.getItemID());
         actions.Add(action);

         // Make note how long the two Battler objects need in order to execute the attack/hit animations
         source.animatingUntil = timeAttackEnds;
         target.animatingUntil = timeAttackEnds;

         // Wait to apply the effects of the action here on the server until the appointed time
         StartCoroutine(applyActionAfterDelay(timeToWait, action, isMultiTarget));
      }

      // Send it to all clients
      List<string> stringList = new List<string>();
      foreach (AttackAction action in actions) {
         stringList.Add(action.serialize());
      }
      battle.Rpc_SendAttackAction(stringList.ToArray());
   }

   public void executeBuff (Battle battle, Battler source, List<Battler> targets, int abilityInventoryIndex) {
      bool isMultiTarget = targets.Count > 1;
      float timeToWait = battle.getTimeToWait(source, targets);
      List<BuffAction> actions = new List<BuffAction>();

      // Get the Ability object and apply the AP change
      // BuffAbility ability = (BuffAbility) AbilityManager.getAbility(abilityType);
      // AbilityData abilityReference = AbilityManager.getAbility(globalAbilityID);
      AbilityData abilityData = source.getAbilities[abilityInventoryIndex];
      int sourceApChange = abilityData.getApChange();
      source.addAP(sourceApChange);

      foreach (Battler target in targets) {
         // Make note of the time that this battle action is going to be fully completed, considering animation times
         float timeBuffStarts = Util.netTime() + timeToWait;
         float timeBuffEnds = timeBuffStarts + abilityData.getDuration();
         float timeActionEnds = timeBuffStarts + abilityData.getTotalAnimLength(source, target);
         float cooldownDuration = abilityData.getCooldown() * source.getCooldownModifier();
         source.cooldownEndTime = timeActionEnds + cooldownDuration;

         // Create the Action object
         BuffAction action = new BuffAction(battle.battleId, abilityInventoryIndex, source.userId, target.userId, timeBuffStarts,
             timeBuffEnds, cooldownDuration, timeActionEnds, sourceApChange, 0, abilityData.getItemID());
         actions.Add(action);

         // Make note how long the two Battler objects need in order to execute the attack/hit animations
         source.animatingUntil = timeActionEnds;
         target.animatingUntil = timeActionEnds;

         // Wait to apply the effects of the action here on the server until the appointed time
         StartCoroutine(applyActionAfterDelay(timeToWait, action, isMultiTarget));
      }

      // Send it to all clients
      List<string> stringList = new List<string>();
      foreach (BuffAction action in actions) {
         stringList.Add(action.serialize());
      }
      battle.Rpc_SendBuffAction(stringList.ToArray());
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

      // Apply the effects of whichever type of action it is
      if (actionToApply is StanceAction) {
         StanceAction action = (StanceAction) actionToApply;

         // Assign the new stance
         source.stance = action.newStance;

      } else if (actionToApply is AttackAction || actionToApply is BuffAction) {
         BattleAction action = (BattleAction) actionToApply;
         Battler target = battle.getBattler(action.targetId);

         // ZERONEV-COMMENT: It is supossed we are still grabbing the ability from the source battler to apply it.
         // So we will grab the source battler.
         AbilityData abilityData = source.getAbilities[action.abilityInventoryIndex];

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
            AbilityManager.self.execute(cancelAction);

         } else {
            if (action is AttackAction) {
               AttackAction attackAction = (AttackAction) action;

               // Apply damage
               target.health -= attackAction.damage;
               target.health = Util.clamp<int>(target.health, 0, target.getStartingHealth());

            } else if (action is BuffAction) {
               BuffAction buffAction = (BuffAction) action;

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
            Enemy.Type battlerType = battler.GetComponent<MonsterBattler>().enemyType;
            foreach (Battler participant in winningBattlers) {
               if (!participant.isMonster()) {
                  participant.player.rpc.spawnLandMonsterChest(battlerType, participant.player.instanceId, BodyManager.self.getBody(participant.player.userId).transform.position);
               }
            }
         }
      }

      // Pass along the request to the Battle Manager to handle shutting everything down
      this.endBattle(battle, teamThatWon);
   }

   #region Private Variables

   // Stores the Battle Board used for each Biome Type
   protected Dictionary<Biome.Type, BattleBoard> _boards = new Dictionary<Biome.Type, BattleBoard>();

   // Stores Battles by their Battle ID
   protected Dictionary<int, Battle> _battles = new Dictionary<int, Battle>();

   // Stores Battlers by their user ID
   protected Dictionary<int, Battler> _battlers = new Dictionary<int, Battler>();

   // Keeps track of which user IDs are associated with which Battles
   protected Dictionary<int, Battle> _activeBattles = new Dictionary<int, Battle>();

   // The unique ID we assign to new Battle objects that are created
   protected int _id = 1;

   // The unique ID we assign to enemy Battlers that are created
   protected int _enemyId = -1;

   #endregion
}
