using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Battler : NetworkBehaviour {
   #region Public Variables

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

   // The current battle stance
   [SyncVar]
   public Stance stance = Stance.Balanced;

   // The time at which we last changed stances
   public float lastStanceChange = float.MinValue;

   // The time at which we last finished using an ability
   public float lastAbilityEndTime;

   // The time at which this Battler is no longer busy displaying attack/hit animations
   public float animatingUntil;

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

   #endregion

   public virtual void Awake () {
      // Look up components
      _outline = GetComponent<SpriteOutline>();
      _renderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
      _clickableBox = GetComponentInChildren<ClickableBox>();

      // Keep track of all of our Simple Animation components
      _anims = new List<SimpleAnimation>(GetComponentsInChildren<SimpleAnimation>());
   }

   public virtual void Start () {
      // Look up our associated player object
      NetworkIdentity enemyIdent = NetworkIdentity.spawned[playerNetId];
      this.player = enemyIdent.GetComponent<NetEntity>();

      // Set our sprite sheets according to our types
      if (!(this is MonsterBattler)) {
         updateSprites();
      }

      // Keep track of Battlers when they're created
      BattleManager.self.storeBattler(this);

      // Look up the Battle Board that contains this Battler
      BattleBoard battleBoard = BattleManager.self.getBattleBoardForBattler(this);

      // The client needs to look up and assign the Battle Spot
      BattleSpot battleSpot = battleBoard.getSpot(teamType, this.boardPosition);
      this.battleSpot = battleSpot;

      // When our Battle is created, we need to switch to the Battle camera
      if (isLocalBattler()) {
         CameraManager.enableBattleDisplay();
      }

      // Start off with the displayed values matching the sync vars
      this.displayedHealth = this.health;
      this.displayedAP = this.AP;

      // Keep track of the client's battler
      _isClientBattler = (Global.player != null && Global.player.userId == this.userId);

      // Stop and restart our animation at the correct speed
      Anim.Type animToPlay = isDead() ? Anim.Type.Death_East : Anim.Type.Battle_East;
      playAnim(animToPlay);
   }

   public virtual void Update () {
      // Flip sprites for the attackers
      checkIfSpritesShouldFlip();

      // Handle the drawing or hiding of our outline
      handleSpriteOutline();
   }

   public virtual void OnDestroy () {
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

   public void updateSprites () {
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

   public virtual void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      Color color = this is MonsterBattler ? Color.red : Color.green;
      _outline.setNewColor(color);
      _outline.setVisibility(isMouseHovering() && !isDead());

      // Any time out sprite changes, we need to regenerate our outline
      _outline.recreateOutlineIfVisible();
   }

   public bool isMouseHovering () {
      Vector3 mouseLocation = BattleCamera.self.getCamera().ScreenToWorldPoint(Input.mousePosition);

      // We don't care about the Z location for the purpose of Contains(), so make the click Z match the bounds Z
      mouseLocation.z = clickBox.bounds.center.z;

      return clickBox.bounds.Contains(mouseLocation);
   }

   public void checkIfSpritesShouldFlip () {
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

   public bool isDead () {
      return (health <= 0);
   }

   public bool isAttacker () {
      return teamType == Battle.TeamType.Attackers;
   }

   public bool isLocalBattler () {
      if (Global.player == null || Global.player.userId <= 0) {
         return false;
      }

      return (Global.player.userId == this.userId);
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

   public bool isProtected (Battle battle) {
      // Figure out the teammate spot that's in front of us
      int spotInFront = getSpotInFront();

      // If there isn't a spot in front of us, we're never protected
      if (spotInFront == 0) {
         return false;
      }

      // Otherwise, we're protected if there's a living Battler on our team in that spot
      foreach (Battler battler in getTeam()) {
         if (battler.boardPosition == spotInFront && !battler.isDead()) {
            return true;
         }
      }

      return false;
   }

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

   public void addAP (int amountToAdd) {
      this.AP += amountToAdd;
      this.AP = Util.clamp<int>(this.AP, 0, MAX_AP);
   }

   public void addBuff (BuffTimer buff) {
      // If we already had a buff of this type, then remove it
      removeBuffsOfType(buff.buffType);

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

   public bool hasBuffOfType (Ability.Type type) {
      foreach (BuffTimer buff in this.buffs) {
         if (buff.buffType == type) {
            return true;
         }
      }

      return false;
   }

   public void removeBuffsOfType (Ability.Type type) {
      List<BuffTimer> toRemove = new List<BuffTimer>();

      // Populate a separate list of the buffs that we're going to remove
      foreach (BuffTimer buff in this.buffs) {
         if (buff.buffType == type) {
            toRemove.Add(buff);
         }
      }

      // Now we iterate over our separate list to avoid concurrent modification exceptions
      foreach (BuffTimer buff in toRemove) {
         removeBuff(buff);
      }
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

   public virtual void playDeathSound () {
      // Nothing by default
   }

   public virtual int getApWhenDamagedBy (Ability ability) {
      // By default, characters gain a small amount of AP when taking damage
      return 3;
   }

   public virtual float getCooldownModifier () {
      // Some buffs affect our cooldown durations
      if (hasBuffOfType(Ability.Type.Haste)) {
         return .5f;
      }

      return 1f;
   }

   public virtual void handleEndOfBattle (Battle.TeamType winningTeam) {
      // Check if we lost the battle
      if (this.teamType != winningTeam) {
         // Players need to warp back to town
         if (this.userId > 0) {
            Spawn spawn = SpawnManager.self.getSpawn(Spawn.Type.ForestTownDock);

            // If they're still connected, we can warp them directly
            if (this.player != null && this.player.connectionToClient != null) {
               this.player.spawnInNewMap(spawn.AreaType, spawn, Direction.North);
            } else {
               // The user might be offline, in which case we need to modify their position in the DB
               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  DB_Main.setNewPosition(this.userId, spawn.transform.position, Direction.North, (int) spawn.AreaType);
               });
            }
         }
      }
   }

   public virtual float getPreContactLength () {
      // The amount of time our attack takes depends the type of Battler
      return .2f;
   }

   public virtual float getPreMagicLength (Ability ability) {
      // The amount of time before the ground effect appears depends on the type of Battler
      return .6f;
   }

   public virtual Vector2 getMagicGroundPosition () {
      Vector2 startPos = this.battleSpot.transform.position;
      return startPos + new Vector2(0f, -.15f);
   }

   public virtual Ability.Type getDefaultAttack () {
      return Ability.Type.Basic_Attack;
   }

   public virtual IEnumerator animateDeath () {
      // By default, we don't wait at all
      yield return new WaitForSeconds(0f);

      playAnim(Anim.Type.Death_East);
   }

   public virtual void playJumpSound () {
      SoundManager.playAttachedClip(SoundManager.Type.Character_Jump, this.transform);
   }

   public void playAnim (Anim.Type animationType) {
      // Make all of our Simple Animation components play the animation
      foreach (SimpleAnimation anim in _anims) {
         anim.playAnimation(animationType);
      }
   }

   public virtual Vector2 getMeleeStandPosition () {
      Vector2 startPos = battleSpot.transform.position;
      return startPos + (new Vector2(.35f, 0f) * (isAttacker() ? -1f : 1f));
   }

   public virtual Vector2 getRangedEndPosition () {
      Vector2 startPos = this.battleSpot.transform.position;
      return startPos + new Vector2(0f, .15f);
   }

   public virtual int getStartingHealth () {
      int level = LevelUtil.levelForXp(XP);

      // Calculate our health based on our base and gain per level
      float health = getBaseHealth() + (getHealthPerLevel() * level);

      return (int) health;
   }

   protected virtual float getBaseHealth () {
      // Default base health for all characters
      return 200f;
   }

   protected virtual float getHealthPerLevel () {
      // Default gain per level for all characters
      return 80f;
   }

   protected virtual float getBaseDefense (Ability.Element element) {
      // Default base defense for all characters
      return 20f;
   }

   protected virtual float getDefensePerLevel (Ability.Element element) {
      // Default gain for level for all characters
      return 4f;
   }

   public virtual float getDefense (Ability.Element element) {
      int level = LevelUtil.levelForXp(this.XP);

      // Calculate our defense based on our base and gain per level
      float defense = getBaseDefense(element) + (getDefensePerLevel(element) * level);

      // Add our armor's defense value, if we have any
      if (armorManager.hasArmor()) {
         defense += armorManager.getArmor().getDefense(element);
      }

      return defense;
   }

   public virtual float getDamage (Ability.Element element) {
      int level = LevelUtil.levelForXp(XP);

      // Calculate our offense based on our base and gain per level
      float damage = getBaseDamage(element) + (getDamagePerLevel(element) * level);

      // Add our weapon's damage value, if we have a weapon
      if (weaponManager.hasWeapon()) {
         damage += weaponManager.getWeapon().getDamage(element);
      }

      return damage;
   }

   protected virtual float getBaseDamage (Ability.Element element) {
      // Default base damage for all characters
      return 50f;
   }

   protected virtual float getDamagePerLevel (Ability.Element element) {
      // Default gain for level for all characters
      return 3f;
   }

   public virtual int getXPValue () {
      // Overridden by Monster classes
      return 0;
   }

   public virtual int getGoldValue () {
      // Overridden by Monster classes
      return 0;
   }

   public virtual BattlePlan getBattlePlan (Battle battle) {
      // By default, do nothing
      return new BattlePlan();
   }

   public List<Battler> getTeam () {
      Battle battle = BattleManager.self.getBattle(this.battleId);

      if (battle == null) {
         D.warning("Can't get team for null battle: " + this.battleId);
         return new List<Battler>();
      }

      if (teamType == Battle.TeamType.Attackers) {
         return battle.getAttackers();
      }

      if (teamType == Battle.TeamType.Defenders) {
         return battle.getDefenders();
      }

      D.warning("There is no team for battler: " + this);
      return new List<Battler>();
   }

   public void showDamageText (AttackAction action) {
      BattleSpot spot = this.battleSpot;
      Ability ability = AbilityManager.getAbility(action.abilityType);

      // Create the Text instance from the prefab
      GameObject damageTextObject = (GameObject) GameObject.Instantiate(PrefabsManager.self.damageTextPrefab);
      DamageText damageText = damageTextObject.GetComponent<DamageText>();

      // Place the damage numbers just above where the impact occurred for the given ability
      damageText.transform.position = (ability is ProjectileAbility) ?
          new Vector3(0f, .10f, -3f) + (Vector3) this.getRangedEndPosition() :
          new Vector3(transform.position.x, transform.position.y + .25f, -3f);
      damageText.setDamageAmount(action.damage, action.wasCritical, action.wasBlocked);
      damageText.transform.SetParent(EffectManager.self.transform, false);
      damageText.name = "DamageText";

      // The damage text should be on the same layer as the target's Battle Spot
      damageText.gameObject.layer = spot.gameObject.layer;

      // Color the text color and icon based on the damage type
      damageText.customizeForAction(action);

      // If the attack was blocked, show some cool text
      if (action.wasBlocked) {
         createBlockBattleText();
      }

      // If the attack was a critical, show some cool text
      if (action.wasCritical) {
         createCriticalBattleText();
      }

      // Make note of the time at which we were last damaged
      this.lastDamagedTime = Time.time;
   }

   public virtual void adjustCanvas (Canvas canvas) {
      // Let children classes override this if they want to customize the position
   }

   protected void createBlockBattleText () {
      GameObject battleTextInstance = Instantiate(PrefabsManager.self.battleTextPrefab);
      battleTextInstance.transform.SetParent(this.transform, false);
      battleTextInstance.GetComponentInChildren<BattleText>().customizeTextForBlock();
   }

   protected void createCriticalBattleText () {
      GameObject battleTextInstance = Instantiate(PrefabsManager.self.battleTextPrefab);
      battleTextInstance.transform.SetParent(this.transform, false);
      battleTextInstance.GetComponentInChildren<BattleText>().customizeTextForCritical();
   }

   public IEnumerator animateKnockback () {
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

   public IEnumerator animateKnockup () {
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

   public IEnumerator animateBlock (Battler attacker) {
      // Show the Block animation frame
      playAnim(Anim.Type.Block_East);
      EffectManager.playBlockEffect(attacker, this);
      yield return new WaitForSeconds(POST_CONTACT_LENGTH);
      playAnim(Anim.Type.Battle_East);
   }

   public IEnumerator animateHit (Battler attacker, AttackAction action) {
      // Display the Hit animation frame for a short period
      playAnim(Anim.Type.Hurt_East);
      yield return new WaitForSeconds(POST_CONTACT_LENGTH);
      playAnim(Anim.Type.Battle_East);
   }

   public IEnumerator animateShake () {
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

         yield return 0;
      }

      // Once we finish, make sure we're back at our starting position
      transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
   }

   protected IEnumerator removeBuffAfterDelay (float delay, Battler battler, BuffTimer buff) {
      yield return new WaitForSeconds(delay);

      battler.removeBuff(buff);
   }

   #region Private Variables

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

   #endregion
}
