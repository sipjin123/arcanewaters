using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

// Will load Battler Data and use that accordingly in all actions.
public class BattlerBehaviour : NetworkBehaviour, IAttackBehaviour {
   #region Public Variables

   // Will be used to fill all the information related to this battler, it is uninitialized. 
   public BattlerData battlerMainData;

   // Battler type (AI controlled or player controlled)
   public BattlerType battlerType;

   // Reference to the main SpriteRenderer, always set to the body sprite renderer.
   public SpriteRenderer mainSpriteRenderer;

   [Space(8)]

   // The amount of time a jump takes
   public static float JUMP_LENGTH = .2f;

   // The amount of time we pause after a jump forward or back
   public static float PAUSE_LENGTH = .1f;

   // The amount of time left after a melee attack makes contact
   public static float POST_CONTACT_LENGTH = .25f;

   // The amount of time it takes to animate a knockup effect
   public static float KNOCKUP_LENGTH = .45f;

   // The amount of time it takes to animate a shake effect
   public static float SHAKE_LENGTH = 1.3f;

   // The amount of time a projectile takes to reach its target
   public static float PROJECTILE_LENGTH = .35f;

   // The maxmimum AP a battler can have
   public static int MAX_AP = 20;

   // The types of stances a battler can be in
   public enum Stance { Balanced = 0, Attack = 1, Defense = 2 };

   // The userId associated with this Battler, if any
   [SyncVar]
   public int userId;

   // The battle ID that this Battler is in
   [SyncVar]
   public int battleId;

   // The type of Biome this battle is in
   [SyncVar]
   public Biome.Type biomeType;

   // The Team that this Battler is on
   [SyncVar]
   public Battle.TeamType teamType;

   // The Gender of this entity
   [SyncVar]
   public Gender.Type gender = Gender.Type.Male;

   // The amount of health we currently have
   [SyncVar]
   public int health;

   // The amount of health displayed by the client
   public int displayedHealth;

   // The amount of action points we currently have
   [SyncVar]
   public int AP;

   // The amount of AP displayed by the client
   public int displayedAP;

   // The amount of experience we have, used to determine our level
   [SyncVar]
   public int XP;

   // The board position we were placed at
   [SyncVar]
   public int boardPosition;

   // The time at which we can use the next ability
   [SyncVar]
   public float cooldownEndTime;

   // The time at which we can switch into another stance.
   [SyncVar]
   public float stanceCooldownEndTime;

   // Used for showing in the UI the time remaining for changing into another stance.
   public float stanceCurrentCooldown;

   // The current battle stance
   [SyncVar]
   public Stance stance = Stance.Balanced;

   // The time at which we last changed stances
   public float lastStanceChange = float.MinValue;

   // The time at which we last finished using an ability
   public float lastAbilityEndTime;

   // The time at which this Battler is no longer busy displaying attack/hit animations
   public float animatingUntil;

   // Determines the enemy type which is used to retrieve enemy data from XML
   public Enemy.Type enemyType;

   // Our associated player net ID
   [SyncVar]
   public uint playerNetId;

   // The Network Player associated with this Battler, if any
   public NetEntity player;

   // The Battle that this Battler is in
   public Battle battle;

   // The Battle Spot at which this Battler has been placed
   public BattleSpot battleSpot;

   // Gets set to true while we're jumping across the board
   public bool isJumping = false;

   // Our body layers
   [SyncVar]
   public BodyLayer.Type bodyType;
   [SyncVar]
   public EyesLayer.Type eyesType;
   [SyncVar]
   public HairLayer.Type hairType;

   // Our colors
   [SyncVar]
   public ColorType eyesColor1;
   [SyncVar]
   public ColorType hairColor1;
   [SyncVar]
   public ColorType hairColor2;

   // The time at which this Battler was last damaged
   [HideInInspector]
   public float lastDamagedTime = float.MinValue;

   // The buffs this Battler currently has
   public class SyncListBuffTimer : SyncList<BuffTimer> { }
   public SyncListBuffTimer buffs = new SyncListBuffTimer();

   // A Box we use for detecting clicks on this Battler
   public BoxCollider2D clickBox;

   // Our Item Managers
   public ArmorManager armorManager;
   public WeaponManager weaponManager;

   // Base select/deselect battlers events. Hidden from inspector to avoid untracked events.
   [HideInInspector] public UnityEvent onBattlerSelect = new UnityEvent();
   [HideInInspector] public UnityEvent onBattlerDeselect = new UnityEvent();
   [HideInInspector] public UnityEvent onBattlerAttackStart = new UnityEvent();
   [HideInInspector] public UnityEvent onBattlerAttackEnd = new UnityEvent();

   [HideInInspector] public BattlerDamagedEvent onBattlerDamaged = new BattlerDamagedEvent();

   #endregion

   private void Awake () {
      // Look up components
      _outline = GetComponent<SpriteOutline>();
      _renderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
      _clickableBox = GetComponentInChildren<ClickableBox>();

      // Keep track of all of our Simple Animation components
      _anims = new List<SimpleAnimation>(GetComponentsInChildren<SimpleAnimation>());

      mainInit();
   }

   private void Start () {
      // Look up our associated player object
      NetworkIdentity enemyIdent = NetworkIdentity.spawned[playerNetId];
      this.player = enemyIdent.GetComponent<NetEntity>();

      MonsterManager.self.translateRawDataToBattlerData(enemyType, battlerMainData);

      // Set our sprite sheets according to our types
      if (battlerType == BattlerType.PlayerControlled) {
         updateSprites();
      } else {
         onBattlerSelect.AddListener(() => {
            BattleUIManager.self.triggerTargetUI(this);
         });

         onBattlerDeselect.AddListener(() => {
            BattleUIManager.self.hideTargetGameobjectUI();
         });
      }

      // Keep track of Battlers when they're created
      BattleManager.self.storeBattler(this);

      // Look up the Battle Board that contains this Battler
      BattleBoard battleBoard = BattleManager.self.getBattleBoard(this.biomeType);

      // The client needs to look up and assign the Battle Spot
      BattleSpot battleSpot = battleBoard.getSpot(teamType, this.boardPosition);

      this.battleSpot = battleSpot;

      // When our Battle is created, we need to switch to the Battle camera
      if (isLocalBattler()) {
         CameraManager.enableBattleDisplay();

         BattleUIManager.self.prepareBattleUI();
      }

      // Start off with the displayed values matching the sync vars
      this.displayedHealth = this.health;
      this.displayedAP = this.AP;

      // Keep track of the client's battler
      _isClientBattler = (Global.player != null && Global.player.userId == this.userId);

      // Stop and restart our animation at the correct speed
      Anim.Type animToPlay = isDead() ? Anim.Type.Death_East : Anim.Type.Battle_East;
      playAnim(animToPlay);

      StartCoroutine(CO_initializeClientBattler());

      if (battlerType == BattlerType.AIEnemyControlled) {
         setBattlerAbilities(_initializedBattlerData.getAbilities());

         // Extra cooldown time for AI controlled battlers, so they do not attack instantly
         this.cooldownEndTime = Util.netTime() + 5f;
      } else {
         setBattlerAbilities(AbilityInventory.self.playerAbilities);
      }

      // Flip sprites for the attackers
      checkIfSpritesShouldFlip();
   }

   private void Update () {
      // Handle the drawing or hiding of our outline
      handleSpriteOutline();
   }

   // Basic method that will handle the functionality for whenever we click on this battler
   public void selectThis () {
      onBattlerSelect.Invoke();
   }

   public void mainInit () {
      if (battlerMainData != null) {
         _initializedBattlerData = BattlerData.CreateInstance(battlerMainData);
      }
   }

   // Basic method that will handle the functionality for whenever we deselect this battler
   public void deselectThis () {
      onBattlerDeselect.Invoke();
   }

   private void updateSprites () {
      // Update the Body, Eyes, and Hair
      foreach (BodyLayer bodyLayer in GetComponentsInChildren<BodyLayer>()) {
         bodyLayer.setType(bodyType);

         // We only call recolor on the body because we want the material to be instanced like all the others
         bodyLayer.recolor(ColorType.None, ColorType.None);
      }
      foreach (EyesLayer eyesLayer in GetComponentsInChildren<EyesLayer>()) {
         eyesLayer.setType(eyesType);
         eyesLayer.recolor(eyesColor1, eyesColor1);
      }
      foreach (HairLayer hairLayer in GetComponentsInChildren<HairLayer>()) {
         hairLayer.setType(hairType);
         hairLayer.recolor(hairColor1, hairColor2);
      }

      // Update the Armor and Weapon
      if (armorManager.hasArmor()) {
         armorManager.updateSprites();
      }
      if (weaponManager.hasWeapon()) {
         weaponManager.updateSprites();
      }
   }

   private void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      Color color = battlerType.Equals(BattlerType.AIEnemyControlled) ? Color.red : Color.green;
      _outline.setNewColor(color);
      _outline.setVisibility(isMouseHovering() && !isDead());

      // Any time out sprite changes, we need to regenerate our outline
      _outline.recreateOutlineIfVisible();
   }

   public void setBattlerAbilities (List<BasicAbilityData> values) {
      // Create initialized copies of the stances data.
      _balancedInitializedStance = BasicAbilityData.CreateInstance(AbilityInventory.self.balancedStance);
      _offenseInitializedStance = BasicAbilityData.CreateInstance(AbilityInventory.self.offenseStance);
      _defensiveInitializedStance = BasicAbilityData.CreateInstance(AbilityInventory.self.defenseStance);

      foreach (BasicAbilityData item in values) {
         switch (item.getAbilityType()) {
            case AbilityType.Standard:
               AttackAbilityData atkAbilityData = AttackAbilityData.CreateInstance(item as AttackAbilityData);
               _battlerAttackAbilities.Add(atkAbilityData);
               break;
            case AbilityType.BuffDebuff:
               BuffAbilityData buffAbilityData = BuffAbilityData.CreateInstance(item as BuffAbilityData);
               _battlerBuffAbilities.Add(buffAbilityData);
               break;
         }
      }
   }

   private void checkIfSpritesShouldFlip () {
      // All of the Battlers on the right side of the board need to flip
      if (this.teamType == Battle.TeamType.Attackers) {
         foreach (SpriteRenderer renderer in _renderers) {
            renderer.flipX = true;
         }
      } else {
         foreach (SpriteRenderer renderer in _renderers) {
            renderer.flipX = false;
         }
      }
   }

   public void addAP (int amountToAdd) {
      this.AP += amountToAdd;
      this.AP = Util.clamp<int>(this.AP, 0, MAX_AP);
   }

   public void addBuff (BuffTimer buff) {
      // If we already had a buff of this type, then remove it
      removeBuffsOfType(buff.buffAbilityGlobalID);

      // Add the new buff
      this.buffs.Add(buff);

      // Calculate the duration of the buff
      float buffDuration = buff.buffEndTime - Util.netTime();

      // Start up a coroutine to remove this buff at the appropriate time
      BattleManager.self.StartCoroutine(removeBuffAfterDelay(buffDuration, this, buff));
   }

   public void removeBuff (BuffTimer buff) {
      // Make sure we haven't died and been removed, to avoid Sync List error
      if (this == null) {
         return;
      }

      // Remove the buff
      if (this.buffs.Contains(buff)) {
         this.buffs.Remove(buff);
      }
   }

   public void removeBuffsOfType (int globalAbilityID) {
      List<BuffTimer> toRemove = new List<BuffTimer>();

      // Populate a separate list of the buffs that we're going to remove
      foreach (BuffTimer buff in this.buffs) {
         if (buff.buffAbilityGlobalID.Equals(globalAbilityID)) {
            toRemove.Add(buff);
         }
      }

      // Now we iterate over our separate list to avoid concurrent modification exceptions
      foreach (BuffTimer buff in toRemove) {
         removeBuff(buff);
      }
   }

   #region Battle Callers

   public void playDeathSound () {
      if (getBattlerData().getDeathSound() == null) {
         Debug.LogWarning("Battler does not have a death sound");
         return;
      }

      SoundManager.playClipOneShotAtPoint(getBattlerData().getDeathSound(), transform.position);
   }

   public void handleEndOfBattle (Battle.TeamType winningTeam) {
      if (teamType != winningTeam) {

         // Monster battler
         if (isMonster()) {
            Enemy enemy = (Enemy) player;

            if (!enemy.isDefeated) {
               enemy.isDefeated = true;
            }

            // Player battler
         } else {
            Spawn spawn = SpawnManager.self.getSpawn(Spawn.Type.ForestTownDock);

            // If they're still connected, we can warp them directly
            if (player != null && player.connectionToClient != null) {
               player.spawnInNewMap(spawn.AreaType, spawn, Direction.North);
            } else {
               // The user might be offline, in which case we need to modify their position in the DB
               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  DB_Main.setNewPosition(userId, spawn.transform.position, Direction.North, (int) spawn.AreaType);
               });
            }
         }
      }
   }

   public void playJumpSound () {
      if (getBattlerData().getAttackJumpSound() == null) {
         Debug.LogWarning("Battler does not have a jump sound");
         return;
      }

      SoundManager.playClipOneShotAtPoint(getBattlerData().getAttackJumpSound(), transform.position);
   }

   public void playAnim (Anim.Type animationType) {
      // Make all of our Simple Animation components play the animation
      foreach (SimpleAnimation anim in _anims) {
         anim.playAnimation(animationType);
      }
   }

   public IEnumerator animateDeath () {
      playAnim(Anim.Type.Death_East);

      if (battlerType == BattlerType.AIEnemyControlled) {
         // Play our customized death sound
         playDeathSound();

         // Wait a little bit for it to finish
         yield return new WaitForSeconds(.25f);

         // Play a "Poof" effect on our head
         EffectManager.playPoofEffect(this);
      }
   }

   public IEnumerator attackDisplay (float timeToWait, BattleAction battleAction, bool isFirstAction) {
      // In here we will check all the information related to the ability we want to execute
      // If it is a melee attack, then normally we will get close to the target battler and execute an animation
      // If it is a ranged attack, then normally we will stay in our place, executing our cast particles (if any)

      // Then proceeding to execute the remaining for each path, it is a little extensive, but definitely a lot better than
      // creating a lot of different scripts
      
      Battle battle = BattleManager.self.getBattle(battleAction.battleId);
      BattlerBehaviour sourceBattler = battle.getBattler(battleAction.sourceId);

      // I believe we must grab the index from this battler, since this will be the one executing the attack
      AttackAbilityData attackerAbility = null;

      AttackAbilityData abilityDataReference = (AttackAbilityData) AbilityManager.getAbility(battleAction.abilityGlobalID);
      AttackAbilityData globalAbilityData = AttackAbilityData.CreateInstance(abilityDataReference);

      Vector2 effectPosition = new Vector2();

      // Cancel ability works differently, so before we try to get the ability from the local battler, 
      // we check if the ability we want to reference is a cancel ability

      if (!globalAbilityData.isCancel()) {
         attackerAbility = _battlerAttackAbilities[battleAction.abilityInventoryIndex];
      }

      switch (globalAbilityData.getAbilityActionType()) {
         case AbilityActionType.Melee:

            onBattlerAttackStart.Invoke();

            // Default all abilities to display as attacks
            if (!(battleAction is AttackAction)) {
               D.warning("Ability doesn't know how to handle action: " + battleAction + ", ability: " + this);
               yield break;
            }

            // Cast version of the Attack Action
            AttackAction action = (AttackAction) battleAction;

            // Look up our needed references
            BattlerBehaviour targetBattler = battle.getBattler(action.targetId);
            Vector2 startPos = sourceBattler.battleSpot.transform.position;

            float jumpDuration = attackerAbility.getJumpDuration(sourceBattler, targetBattler);

            // Don't start animating until both sprites are available
            yield return new WaitForSeconds(timeToWait);

            // Make sure the source battler is still alive at this point
            if (sourceBattler.isDead()) {
               yield break;
            }

            // Mark the source battler as jumping
            sourceBattler.isJumping = true;

            // Play an appropriate jump sound
            sourceBattler.playJumpSound();

            // Smoothly jump into position
            sourceBattler.playAnim(Anim.Type.Jump_East);
            Vector2 targetPosition = targetBattler.getMeleeStandPosition();
            float startTime = Time.time;
            while (Time.time - startTime < jumpDuration) {
               float timePassed = Time.time - startTime;
               Vector2 newPos = Vector2.Lerp(startPos, targetPosition, (timePassed / jumpDuration));
               sourceBattler.transform.position = new Vector3(newPos.x, newPos.y, sourceBattler.transform.position.z);
               yield return 0;
            }

            // Make sure we're exactly in position now that the jump is over
            sourceBattler.transform.position = new Vector3(targetPosition.x, targetPosition.y, sourceBattler.transform.position.z);

            // Pause for a moment after reaching our destination
            yield return new WaitForSeconds(PAUSE_LENGTH);

            sourceBattler.playAnim(attackerAbility.getAnimation());

            // Play any sounds that go along with the ability being cast

            // We want to play the cast clip at our battler position
            attackerAbility.playCastClipAtTarget(transform.position);

            // Apply the damage at the correct time in the swing animation
            yield return new WaitForSeconds(sourceBattler.getPreContactLength());

            #region Display Block

            // Note that the contact is happening right now
            BattleUIManager.self.showDamageText(action, targetBattler);

            // Play an impact sound appropriate for the ability
            attackerAbility.playHitClipAtTarget(targetBattler.transform.position);

            // Move the sprite back and forward to simulate knockback
            targetBattler.StartCoroutine(targetBattler.animateKnockback());

            // If the action was blocked, animate that
            if (action.wasBlocked) {
               targetBattler.StartCoroutine(targetBattler.animateBlock(sourceBattler));

            } else {
               // Play an appropriate attack animation effect
               effectPosition = targetBattler.getMagicGroundPosition() + new Vector2(0f, .25f);
               EffectManager.playCombatAbilityVFX(sourceBattler, targetBattler, action, effectPosition);

               // Make the target sprite display its "Hit" animation
               targetBattler.StartCoroutine(targetBattler.animateHit(sourceBattler, action));
            }

            // If either sprite is owned by the client, play a camera shake
            if (Util.isPlayer(sourceBattler.userId) || Util.isPlayer(targetBattler.userId)) {
               BattleCamera.self.shakeCamera(.25f);
            }

            targetBattler.displayedHealth -= action.damage;
            targetBattler.displayedHealth = Util.clamp<int>(targetBattler.displayedHealth, 0, targetBattler.getStartingHealth());

            #endregion

            yield return new WaitForSeconds(POST_CONTACT_LENGTH);

            // Now jump back to where we started from
            sourceBattler.playAnim(Anim.Type.Jump_East);
            startTime = Time.time;
            while (Time.time - startTime < jumpDuration) {
               float timePassed = Time.time - startTime;
               Vector2 newPos = Vector2.Lerp(targetBattler.getMeleeStandPosition(), startPos, (timePassed / jumpDuration));
               sourceBattler.transform.position = new Vector3(newPos.x, newPos.y, sourceBattler.transform.position.z);
               yield return 0;
            }

            // Make sure we're exactly in position now that the jump is over
            sourceBattler.transform.position = new Vector3(startPos.x, startPos.y, sourceBattler.transform.position.z);

            // Wait for a moment after we reach our jump destination
            yield return new WaitForSeconds(PAUSE_LENGTH);

            // Switch back to our battle stance
            sourceBattler.playAnim(Anim.Type.Battle_East);

            // Mark the source sprite as no longer jumping
            sourceBattler.isJumping = false;

            // Add any AP we earned
            sourceBattler.displayedAP = Util.clamp<int>(sourceBattler.displayedAP + action.sourceApChange, 0, MAX_AP);
            targetBattler.displayedAP = Util.clamp<int>(targetBattler.displayedAP + action.targetApChange, 0, MAX_AP);

            // If the target died, animate that death now
            if (targetBattler.displayedHealth <= 0) {
               targetBattler.StartCoroutine(targetBattler.animateDeath());
            }

            onBattlerAttackEnd.Invoke();

            break;
         case AbilityActionType.Ranged:

            // Cast version of the Attack Action
            action = (AttackAction) battleAction;
            targetBattler = battle.getBattler(action.targetId);

            // Don't start animating until both sprites are available
            yield return new WaitForSeconds(timeToWait);

            // The unused code is on the MagicAbility script
            // Make sure the battlers are still alive at this point
            if (sourceBattler.isDead() || targetBattler.isDead()) {
               yield break;
            }

            // Start the attack animation that will eventually create the magic effect
            if (isFirstAction) {
               sourceBattler.playAnim(attackerAbility.getAnimation());

               SoundManager.playClipOneShotAtPoint(abilityDataReference.getCastAudioClip(), sourceBattler.transform.position);
               Vector2 castEffectPosition = new Vector2(transform.position.x, transform.position.y - (mainSpriteRenderer.bounds.extents.y / 2));
               EffectManager.playCastAbilityVFX(sourceBattler, action, getMagicGroundPosition());
            }

            // Wait the appropriate amount of time before creating the magic effect
            yield return new WaitForSeconds(sourceBattler.getPreMagicLength());

            // Play the sound associated with the magic efect
            SoundManager.playClipOneShotAtPoint(abilityDataReference.getHitAudioClip(), targetBattler.transform.position);

            effectPosition = targetBattler.mainSpriteRenderer.bounds.center;
            EffectManager.playCombatAbilityVFX(sourceBattler, targetBattler, action, effectPosition);

            // Make the target sprite display its "Hit" animation
            targetBattler.StartCoroutine(targetBattler.animateHit(sourceBattler, action));

            // If this magic ability has knockup, then start it now
            if (abilityDataReference.hasKnockup()) {
               targetBattler.StartCoroutine(targetBattler.animateKnockup());
               yield return new WaitForSeconds(KNOCKUP_LENGTH);
            } else if (abilityDataReference.hasShake()) {
               targetBattler.StartCoroutine(targetBattler.animateShake());
               yield return new WaitForSeconds(SHAKE_LENGTH);
            }

            BattleUIManager.self.showDamageText(action, targetBattler);
            // Wait until the animation gets to the point that it deals damage
            yield return new WaitForSeconds(abilityDataReference.getPreDamageLength);

            targetBattler.displayedHealth -= action.damage;
            targetBattler.displayedHealth = Util.clamp<int>(targetBattler.displayedHealth, 0, targetBattler.getStartingHealth());

            // Now wait the specified amount of time before switching back to our battle stance
            yield return new WaitForSeconds(POST_CONTACT_LENGTH);

            if (isFirstAction) {
               // Switch back to our battle stance
               sourceBattler.playAnim(Anim.Type.Battle_East);

               // Add any AP we earned
               sourceBattler.addAP(action.sourceApChange);
            }

            // Add any AP the target earned
            targetBattler.addAP(action.targetApChange);

            // If the target died, animate that death now
            if (targetBattler.isDead()) {
               targetBattler.StartCoroutine(targetBattler.animateDeath());
            }

            onBattlerAttackEnd.Invoke();
            break;

         case AbilityActionType.Cancel:
            // Cancel requires time before activating.
            yield return new WaitForSeconds(timeToWait);

            // Cast version of the Attack Action
            CancelAction cancelAction = (CancelAction) battleAction;

            // Look up our needed references
            battle = BattleManager.self.getBattle(cancelAction.battleId);

            // If the battle has ended, no problem
            if (battle == null) {
               yield break;
            }

            // Display the cancel icon over the source's head
            GameObject cancelPrefab = PrefabsManager.self.cancelIconPrefab;
            GameObject cancelInstance = Instantiate(cancelPrefab);
            cancelInstance.transform.SetParent(sourceBattler.transform, false);
            cancelInstance.transform.position = new Vector3(
                sourceBattler.transform.position.x,
                sourceBattler.transform.position.y + .55f,
                -5f
            );

            break;
         default:
            D.warning("Ability doesn't know how to handle action: " + battleAction + ", ability: " + this);
            yield break;
      }
   }

   private IEnumerator animateKnockback () {
      float startTime = Time.time;
      float halfDuration = POST_CONTACT_LENGTH / 2f;
      Vector3 startPos = battleSpot.transform.position;

      // Animate the knockback during the post-contact duration
      while (Time.time - startTime < POST_CONTACT_LENGTH) {
         float timePassed = Time.time - startTime;
         bool overHalfwayDone = timePassed > halfDuration;

         // Check if we're over halfway done animating the knockback
         if (overHalfwayDone) {
            timePassed -= halfDuration;
         }

         // Calculate a positional offset based on whether we're moving back or forward
         float offset = overHalfwayDone ? Mathf.Lerp(.20f, 0f, (timePassed / halfDuration)) :
                                          Mathf.Lerp(0f, .20f, (timePassed / halfDuration));
         offset *= isAttacker() ? 1f : -1f;

         // Add a positional offset to our starting position
         transform.position = new Vector3(
             startPos.x + offset,
             startPos.y,
             transform.position.z
         );

         yield return 0;
      }

      // Once we finish, make sure we're back at our starting position
      transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
   }

   private IEnumerator animateKnockup () {
      float startTime = Time.time;
      Vector2 startPos = this.battleSpot.transform.position;

      // Animate the knockup 
      while (Time.time - startTime < KNOCKUP_LENGTH) {
         float timePassed = Time.time - startTime;

         // We want the offset to be 0 at the beginning and end, and 1 at the middle
         float degrees = (timePassed / KNOCKUP_LENGTH) * 180;
         float radians = degrees * Mathf.Deg2Rad;
         float yOffset = Mathf.Sin(radians) * .8f;

         // Add a positional offset to our starting position
         transform.position = new Vector3(
             startPos.x,
             startPos.y + yOffset,
             transform.position.z
         );

         yield return 0;
      }

      // Once we finish, make sure we're back at our starting position
      transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
   }

   private IEnumerator animateBlock (BattlerBehaviour attacker) {
      // Show the Block animation frame
      playAnim(Anim.Type.Block_East);
      EffectManager.playBlockEffect(attacker, this);
      yield return new WaitForSeconds(POST_CONTACT_LENGTH);
      playAnim(Anim.Type.Battle_East);
   }

   private IEnumerator animateHit (BattlerBehaviour attacker, AttackAction action) {
      // Display the Hit animation frame for a short period
      playAnim(Anim.Type.Hurt_East);
      yield return new WaitForSeconds(POST_CONTACT_LENGTH);
      playAnim(Anim.Type.Battle_East);
   }

   private IEnumerator animateShake () {
      float startTime = Time.time;
      Vector2 startPos = this.battleSpot.transform.position;

      // If the client's player is the target of this shake, then also shake the camera
      if (Util.isPlayer(this.userId)) {
         BattleCamera.self.shakeCamera(SHAKE_LENGTH);
      }

      // Animate the shake 
      while (Time.time - startTime < SHAKE_LENGTH) {
         float timePassed = Time.time - startTime;

         // We want the offset to be 0 at the beginning and end, and 1 at the middle
         float degrees = (timePassed / SHAKE_LENGTH) * 1800;
         float radians = degrees * Mathf.Deg2Rad;
         float xOffset = Mathf.Sin(radians) * .03f;

         // Add a positional offset to our starting position
         transform.position = new Vector3(
             startPos.x + xOffset,
             startPos.y,
             transform.position.z
         );

         // Alternate between the damage and the battle animation
         if (transform.position.x > startPos.x) {
            playAnim(Anim.Type.Hurt_East);
         } else {
            playAnim(Anim.Type.Hurt_East);
         }

         yield return null;
      }

      // Once we finish, make sure we're back at our starting position
      transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
   }

   private IEnumerator removeBuffAfterDelay (float delay, BattlerBehaviour battler, BuffTimer buff) {
      yield return new WaitForSeconds(delay);

      battler.removeBuff(buff);
   }

   private IEnumerator CO_initializeClientBattler () {
      // We will wait until this battler battle is in the BattleManager battle list
      yield return new WaitUntil(() => BattleManager.self.getBattle(battleId) != null);

      if (battle == null) {
         if (battlerType == BattlerType.PlayerControlled) {
            battle = BattleManager.self.getBattle(battleId);
            transform.SetParent(battle.transform, false);
         }
      }
   }

   #endregion

   #region Getters

   public int getSpotInFront () {
      switch (this.boardPosition) {
         case 4:
         case 5:
         case 6:
         case 10:
         case 11:
         case 12:
            return this.boardPosition - 3;
         default:
            return 0;
      }
   }

   public int getApWhenDamaged () {
      // By default, characters gain a small amount of AP when taking damage
      return getBattlerData().getApWhenDamaged();
   }

   public int getStartingHealth () {
      int level = LevelUtil.levelForXp(XP);

      // Calculate our health based on our base and gain per level
      float health = getBattlerData().getBaseHealth() + (getBattlerData().getHealthPerLevel() * level);

      return (int) health;
   }

   public int getXPValue () {
      return getBattlerData().getBaseXPReward();
   }

   public int getGoldValue () {
      return getBattlerData().getBaseGoldReward();
   }

   public float getActionTimerPercent () {
      // Figure out how full our timer bar should be
      float fillPercent = 1f;
      float cooldownLength = (this.cooldownEndTime - this.lastAbilityEndTime);

      if (cooldownLength > 0f) {
         fillPercent = (Util.netTime() - this.lastAbilityEndTime) / cooldownLength;
      }

      return Util.clamp<float>(fillPercent, 0f, 1f);
   }

   public float getStanceCooldown (Stance stance) {
      switch (stance) {
         case Stance.Balanced:
            return _balancedInitializedStance.getCooldown();
         case Stance.Attack:
            return _offenseInitializedStance.getCooldown();
         case Stance.Defense:
            return _defensiveInitializedStance.getCooldown();
      }

      return 0;
   }

   // TODO ZERONEV: When buffs are implemented, implement this
   public float getCooldownModifier () {
      // Some buffs affect our cooldown durations

      return 1f;
   }
   
   public float getPreContactLength () {
      // The amount of time our attack takes depends the type of Battler
      return 0.3f;
   }
   
   public float getPreMagicLength () {
      // The amount of time before the ground effect appears depends on the type of Battler
      return .6f;
   }

   public float getDefense (Element element) {
      int level = LevelUtil.levelForXp(getBattlerData().getCurrentXP());

      // Calculate our defense based on our base and gain per level
      float defense = getBattlerData().getBaseDefense() + (getBattlerData().getDefensePerLevel() * level);

      // Add our armor's defense value, if we have any
      if (armorManager.hasArmor()) {
         defense += armorManager.getArmor().getDefense(element);
      }

      switch (element) {
         case Element.Physical:
            defense *= getBattlerData().getPhysicalDefMultiplier();
            break;
         case Element.Fire:
            defense *= getBattlerData().getFireDefMultiplier();
            break;
         case Element.Earth:
            defense *= getBattlerData().getEarthDefMultiplier();
            break;
         case Element.Air:
            defense *= getBattlerData().getAirDefMultiplier();
            break;
         case Element.Water:
            defense *= getBattlerData().getWaterDefMultiplier();
            break;
      }

      // We will add as an additional the "All" multiplier with the base defense
      defense += getBattlerData().getBaseDefense() + (getBattlerData().getDefensePerLevel() * level);

      return defense;
   }

   public float getDamage (Element element) {
      int level = LevelUtil.levelForXp(XP);

      // Calculate our offense based on our base and gain per level
      float damage = getBattlerData().getBaseDamage() + (getBattlerData().getDamagePerLevel() * level);

      // Add our weapon's damage value, if we have a weapon
      if (weaponManager.hasWeapon()) {
         damage += weaponManager.getWeapon().getDamage(element);
      }

      switch (element) {
         case Element.Physical:
            damage *= getBattlerData().getPhysicalAtkMultiplier();
            break;
         case Element.Fire:
            damage *= getBattlerData().getFireAtkMultiplier();
            break;
         case Element.Earth:
            damage *= getBattlerData().getEarthAtkMultiplier();
            break;
         case Element.Air:
            damage *= getBattlerData().getAirAtkMultiplier();
            break;
         case Element.Water:
            damage *= getBattlerData().getWaterAtkMultiplier();
            break;
      }

      // We will add as an additional the "All" multiplier with the base defense.
      damage += (getBattlerData().getBaseDamage() * getBattlerData().getAllAtkMultiplier());

      return damage;
   }

   public bool hasBuffOfType (int globalAbilityID) {
      foreach (BuffTimer buff in this.buffs) {
         if (buff.buffAbilityGlobalID == globalAbilityID) {
            return true;
         }
      }

      return false;
   }

   public bool isProtected (Battle battle) {
      // Figure out the teammate spot that's in front of us
      int spotInFront = getSpotInFront();

      // If there isn't a spot in front of us, we're never protected
      if (spotInFront == 0) {
         return false;
      }

      // Otherwise, we're protected if there's a living Battler on our team in that spot
      foreach (BattlerBehaviour battler in getTeam()) {
         if (battler.boardPosition == spotInFront && !battler.isDead()) {
            return true;
         }
      }

      return false;
   }

   public bool isMonster () {
      // Monsters have negative user IDs
      return (userId < 0);
   }

   public bool canBlock () {
      // Monsters don't get to block, that would be annoying
      if (isMonster()) {
         return false;
      }

      // Otherwise, can only block if we're holding a weapon
      return (weaponManager.hasWeapon());
   }

   public bool isDead () {
      return (health <= 0);
   }

   public bool isAttacker () {
      return teamType == Battle.TeamType.Attackers;
   }
   
   public Vector2 getMagicGroundPosition () {
      return new Vector2(transform.position.x, transform.position.y - (mainSpriteRenderer.bounds.extents.y / 2));
   }
   
   public Vector2 getMeleeStandPosition () {
      Vector2 startPos = battleSpot.transform.position;
      return startPos + new Vector2(mainSpriteRenderer.bounds.extents.x, 0) * (isAttacker() ? -1f : 1f);
   }
   
   public Vector2 getRangedEndPosition () {
      Vector2 startPos = battleSpot.transform.position;
      return startPos + new Vector2(0f, mainSpriteRenderer.bounds.extents.x * 2) * (isAttacker() ? -1f : 1f);
   }

   // Gets the battler initialized data (health, ap, etc)
   public BattlerData getBattlerData () { return _initializedBattlerData; }

   // Used for AI controlled battlers
   public BattlePlan getBattlePlan (Battle battle) {
      if (battlerType == BattlerType.AIEnemyControlled) {
         BattlerBehaviour target = getRandomTargetFor(getBasicAttack(), battle);

         // Set up a list of targets
         List<BattlerBehaviour> targets = new List<BattlerBehaviour>();
         if (target != null) {
            targets.Add(target);
         }

         // By default, AI battlers will use the Monster attack ability
         return new BattlePlan(getBasicAttack(), targets);
      } else {
         Debug.LogError("Error in battle logic, a non AI controlled battler cannot have a Battle Plan");
         return null;
      }
   }

   protected BattlerBehaviour getRandomTargetFor (AttackAbilityData abilityData, Battle battle) {
      List<BattlerBehaviour> options = new List<BattlerBehaviour>();

      // Cycle over all of the participants in the battle
      foreach (BattlerBehaviour targetBattler in battle.getParticipants()) {

         // Check if the battler is on the other team and not dead
         if (targetBattler.teamType != this.teamType && !targetBattler.isDead()) {

            // If it's a Melee Ability, make sure the target isn't protected
            if (abilityData.isMelee() && targetBattler.isProtected(battle)) {
               continue;
            }

            options.Add(targetBattler);
         }
      }

      // If we have at least one option, choose a random target
      if (options.Count > 0) {
         return options[Random.Range(0, options.Count)];
      }

      // There weren't any available targets still alive
      return null;
   }

   public void unPauseAnims () {
      for (int i = 0; i < _anims.Count; i++) {
         _anims[i].isPaused = false;
      }
   }

   public List<BattlerBehaviour> getTeam () {
      Battle battle = BattleManager.self.getBattle(this.battleId);

      if (battle == null) {
         D.warning("Can't get team for null battle: " + this.battleId);
         return new List<BattlerBehaviour>();
      }

      if (teamType == Battle.TeamType.Attackers) {
         return battle.getAttackers();
      }

      if (teamType == Battle.TeamType.Defenders) {
         return battle.getDefenders();
      }

      D.warning("There is no team for battler: " + this);
      return new List<BattlerBehaviour>();
   }

   // Initialized stances
   public BasicAbilityData getBalancedStance () { return _balancedInitializedStance; }
   public BasicAbilityData getOffenseStance () { return _offenseInitializedStance; }
   public BasicAbilityData getDefensiveStance () { return _defensiveInitializedStance; }

   public List<AttackAbilityData> getAttackAbilities () { return _battlerAttackAbilities; }

   public AttackAbilityData getBasicAttack () {
      // Safe check
      if (_battlerAttackAbilities.Count <= 0) {
         Debug.LogError("This battler do not have any abilities");
         return null;
      }

      return _battlerAttackAbilities[0];
   }

   private bool isMouseHovering () {
      Vector3 mouseLocation = BattleCamera.self.getCamera().ScreenToWorldPoint(Input.mousePosition);

      // We don't care about the Z location for the purpose of Contains(), so make the click Z match the bounds Z
      mouseLocation.z = clickBox.bounds.center.z;

      return clickBox.bounds.Contains(mouseLocation);
   }

   private bool isLocalBattler () {
      if (Global.player == null || Global.player.userId <= 0) {
         return false;
      }

      return (Global.player.userId == this.userId);
   }

   #endregion

   private void OnDestroy () {
      // Don't do anything if we're closing the application
      if (ClientManager.isApplicationQuitting) {
         return;
      }

      // Check if our Battler was just deleted
      if (_isClientBattler) {
         // If we're showing a Battle, exit out
         if (CameraManager.isShowingBattle()) {
            CameraManager.disableBattleDisplay();
         }
      }
   }

   #region Private Variables

   // Attack abilities that will be used in combat
   private List<AttackAbilityData> _battlerAttackAbilities = new List<AttackAbilityData>();

   // Buff abilities that will be used when buffing/debuffing a target in combat.
   private List<BuffAbilityData> _battlerBuffAbilities = new List<BuffAbilityData>();

   // Battler data reference that will be initialized (ready to be used, use getBattlerData() )
   private BattlerData _initializedBattlerData;

   // Our Animators
   protected List<SimpleAnimation> _anims;

   // Our renderers
   protected List<SpriteRenderer> _renderers;

   // Our Sprite Outline
   protected SpriteOutline _outline;

   // Our clickable box
   protected ClickableBox _clickableBox;

   // Gets set to true if this is the Battler that the client owns
   protected bool _isClientBattler = false;

   // The initialized stances that the battlers will have, used for reading correctly the cooldowns
   private BasicAbilityData _balancedInitializedStance;
   private BasicAbilityData _offenseInitializedStance;
   private BasicAbilityData _defensiveInitializedStance;

   #endregion
}

public enum BattlerType
{
   UNDEFINED = 0,
   AIEnemyControlled = 1,
   PlayerControlled = 2
}
