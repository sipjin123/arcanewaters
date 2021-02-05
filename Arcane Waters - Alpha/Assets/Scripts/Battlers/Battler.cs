using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Will load Battler Data and use that accordingly in all actions.
public class Battler : NetworkBehaviour, IAttackBehaviour
{
   #region Public Variables

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

   // The amount of time it takes to animate a knock back effect
   public static float KNOCKBACK_LENGTH = .45f;

   // The amount of time it takes to animate a knockup effect
   public static float KNOCKUP_LENGTH = .45f;

   // The amount of time it takes to animate a shake effect
   public static float SHAKE_LENGTH = .75f;

   // The amount of time a projectile takes to reach its target
   public static float PROJECTILE_LENGTH = .35f;

   // The maxmimum AP a battler can have
   public static int MAX_AP = 20;

   // The types of stances a battler can be in
   public enum Stance { Balanced = 0, Attack = 1, Defense = 2 };

   // The list of battler ability ID's
   public SyncList<int> basicAbilityIDList = new SyncList<int>();

   // The userId associated with this Battler, if any
   [SyncVar]
   public int userId;

   // The companion id if is a companion of the player
   [SyncVar]
   public int companionId = -1;

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
   public int health = 1;

   // The bonus attack stats provided by buffs
   [SyncVar]
   public int bonusFireAttack = 0,
      bonusWaterAttack = 0,
      bonusAirAttack = 0,
      bonusEarthAttack = 0,
      bonusPhysicalAttack = 0;

   // The bonus defense stats provided by buffs
   [SyncVar]
   public int bonusFireDefense = 0,
      bonusWaterDefense = 0,
      bonusAirDefense = 0,
      bonusEarthDefense = 0,
      bonusPhysicalDefense = 0;

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
   public double cooldownEndTime;

   // The time at which we can switch into another stance.
   [SyncVar]
   public double stanceCooldownEndTime;

   // Used for showing in the UI the time remaining for changing into another stance.
   public double stanceCurrentCooldown;

   // The current battle stance
   [SyncVar]
   public Stance stance = Stance.Balanced;

   // The time at which we last changed stances
   public double lastStanceChange = float.MinValue;

   // The time at which we last finished using an ability
   public double lastAbilityEndTime;

   // The time at which this Battler is no longer busy displaying attack/hit animations
   public double animatingUntil;

   // Determines the enemy type which is used to retrieve enemy data from XML
   [SyncVar]
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
   public string eyesPalettes;
   [SyncVar]
   public string hairPalettes;

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
   public HatManager hatManager;

   // Holds the reference to the battler bar
   public BattleBars selectedBattleBar, minionBattleBar, bossBattleBar;

   // Base select/deselect battlers events. Hidden from inspector to avoid untracked events.
   [HideInInspector] public UnityEvent onBattlerSelect = new UnityEvent();
   [HideInInspector] public UnityEvent onBattlerDeselect = new UnityEvent();
   [HideInInspector] public UnityEvent onBattlerAttackStart = new UnityEvent();
   [HideInInspector] public UnityEvent onBattlerAttackEnd = new UnityEvent();

   [HideInInspector] public BattlerDamagedEvent onBattlerDamaged = new BattlerDamagedEvent();

   // Determines the cast time for the aim animation
   public const float PRE_AIM_DELAY = .15f;

   // Determines the aiming duration
   public const float AIM_DURATION = 1;

   // Determines the delay before animation the Shoot clip
   public const float PRE_SHOOT_DELAY = .1f;

   // Determines the delay before ending Shoot Pose
   public const float POST_SHOOT_DELAY = .15f;

   // Determines the delay before animation the cast clip
   public const float PRE_CAST_DELAY = .08f;

   // Determines the delay before ending cast Pose
   public const float POST_CAST_DELAY = .13f;

   // Determines if the abilities have been initialized
   public bool battlerAbilitiesInitialized = false;

   // The location where the ui will snap to upon selection
   public Transform targetUISnapLocation;

   // Determines if this battler is a boss
   [SyncVar]
   public bool isBossType;

   // Caches the sizes of the monsters in pixel for offset purposes
   public const float LARGE_MONSTER_SIZE = 140;
   public const float LARGE_MONSTER_OFFSET = .25f;

   // The starting AP for all units
   public const int DEFAULT_AP = 5;

   // The current coroutine action
   public IEnumerator currentActionCoroutine = null;

   // If cancel state was received
   public bool receivedCancelState;

   // Reference to the shadow
   public Transform shadowTransform;

   // If action can be cancelled
   public bool canCancelAction;

   // If setup is completed
   public bool hasAssignedNetId = false;

   // Debug text mesh
   public GameObject debugLogCanvas;
   public Text debugTextLog;

   // The anim group
   public Anim.Group animGroup;

   #endregion

   public void stopActionCoroutine () {
      if (currentActionCoroutine != null) {
         StopCoroutine(currentActionCoroutine);
         receivedCancelState = true;
      }
   }

   public void registerNewActionCoroutine (IEnumerator newEnumerator, BattleActionType battleActionType) {
      currentActionCoroutine = newEnumerator;
      StartCoroutine(currentActionCoroutine);
   }

   private void Awake () {
      // Look up components
      _outline = GetComponent<SpriteOutline>();
      _renderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
      _clickableBox = GetComponentInChildren<ClickableBox>();

      // Keep track of all of our Simple Animation components
      _anims = new List<SimpleAnimation>(GetComponentsInChildren<SimpleAnimation>());

      AP = DEFAULT_AP;
   }

   private void Start () {
      // Look up our associated player object
      if (NetworkIdentity.spawned.ContainsKey(playerNetId)) {
         NetworkIdentity enemyIdent = NetworkIdentity.spawned[playerNetId];
         this.player = enemyIdent.GetComponent<NetEntity>();
      } else {
         StartCoroutine(CO_AssignPlayerNetId());
         return;
      }
      initializeBattler();
   }

   private void initializeBattler () {
      hasAssignedNetId = true;
      initializeBattlerData();

      // Set our sprite sheets according to our types
      if (battlerType == BattlerType.PlayerControlled && isLocalBattler()) {
         updateSprites();
      } else {
         // Only player battlers need sprite update
         if (battlerType == BattlerType.PlayerControlled && !isLocalBattler()) {
            updateSprites();
         }

         if (enemyType != Enemy.Type.PlayerBattler) {
            // Monster battlers are selectable
            onBattlerSelect.AddListener(() => {
               // Enable all offensive abilities
               BattleUIManager.self.updateAbilityStates(AbilityType.Standard);
            });
         }
      }

      // Keep track of Battlers when they're created
      BattleManager.self.storeBattler(this);

      // Look up the Battle Board that contains this Battler
      BattleBoard battleBoard = BattleManager.self.battleBoard;

      // The client needs to look up and assign the Battle Spot
      BattleSpot battleSpot = battleBoard.getSpot(teamType, this.boardPosition);
      this.battleSpot = battleSpot;

      // When our Battle is created, we need to switch to the Battle camera
      if (isLocalBattler()) {
         BattleUIManager.self.selectionId = 0;
         CameraManager.enableBattleDisplay();

         BattleUIManager.self.usernameText.text = Global.player.entityName;
         BattleUIManager.self.prepareBattleUI();
      } else {
         // This will allow the Ability UI to be triggered when an ally is selected (used for ally target abilities such as Heal and other Buffs)
         if (enemyType == Enemy.Type.PlayerBattler) {
            onBattlerSelect.AddListener(() => {
               Battler allyBattler = this;

               Vector3 pointOffset = new Vector3(allyBattler.clickBox.bounds.size.x / 4, allyBattler.clickBox.bounds.size.y * 1.75f);
               BattleUIManager.self.setRectToScreenPosition(BattleUIManager.self.mainPlayerRect, allyBattler.battleSpot.transform.position, pointOffset);
               BattleUIManager.self.setRectToScreenPosition(BattleUIManager.self.playerMainUIHolder.GetComponent<RectTransform>(), allyBattler.battleSpot.transform.position, pointOffset);

               BattleUIManager.self.playerMainUIHolder.gameObject.SetActive(true);
               BattleUIManager.self.playerBattleCG.Show();
               BattleUIManager.self.stanceChangeButton.gameObject.SetActive(false);
               BattleUIManager.self.usernameText.gameObject.SetActive(true);
               BattleUIManager.self.usernameText.text = BodyManager.self.getBody(allyBattler.userId).nameText.text;
               selectedBattleBar.gameObject.SetActive(false);

               // Enable all buff abilities
               BattleUIManager.self.updateAbilityStates(AbilityType.BuffDebuff);
            });

            onBattlerDeselect.AddListener(() => {
               BattleUIManager.self.playerBattleCG.Hide();
               selectedBattleBar.gameObject.SetActive(false);
               BattleUIManager.self.playerStanceFrame.SetActive(false);
               BattleUIManager.self.playerMainUIHolder.gameObject.SetActive(false);
            });
         } 
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

      // Flip sprites for the attackers
      checkIfSpritesShouldFlip();

      // This simulates the click on the enemy battler on client side
      if (!isLocalBattler() && BattleSelectionManager.self.selectedBattler == null && enemyType != Enemy.Type.PlayerBattler) {
         StartCoroutine(CO_SelectEnemyBattler());
      }

      if (Global.displayLandCombatStats) {
         // TODO: After observing multiplayer combat and confirmed that freezing on death anim is no longer occurring, remove this block
         if (player is PlayerBodyEntity) {
            debugLogCanvas.SetActive(true);
         }
      }
   }

   private IEnumerator CO_AssignPlayerNetId () {
      // Wait until the player is available in the spawned network identities
      while (NetworkIdentity.spawned.ContainsKey(playerNetId) == false) {
         yield return 0;
      }

      NetworkIdentity enemyIdent = NetworkIdentity.spawned[playerNetId];
      this.player = enemyIdent.GetComponent<NetEntity>();
      initializeBattler();
   }

   private IEnumerator CO_SelectEnemyBattler () {
      // Simulate battle selection upon entering combat
      yield return new WaitForSeconds(1);

      if (BattleSelectionManager.self.selectedBattler != this) {
         BattleSelectionManager.self.clickedArea(transform.position);
      } else {
         D.debug("Cannot select Battler since it is currently Selected");
      }
   }

   public void upateBattleSpots () {
      // Look up the Battle Board that contains this Battler
      BattleBoard battleBoard = BattleManager.self.battleBoard;

      // The client needs to look up and assign the Battle Spot
      BattleSpot battleSpot = battleBoard.getSpot(teamType, this.boardPosition);
      this.battleSpot = battleSpot;
   }

   public void snapToBattlePosition () {
      transform.position = battleSpot.transform.position;
   }

   private void Update () {
      if (!hasAssignedNetId) {
         return;
      }

      // This block is only enabled upon admin command and is double checked by the server if the user is an admin
      // TODO: After observing multiplayer combat and confirmed that freezing on death anim is no longer occurring, remove this block
      if (Global.displayLandCombatStats) {
         if (player is PlayerBodyEntity) {
            string newMessage = "IsDead" + " : " + isDead()
               + "\nPlayer: " + (player is PlayerBodyEntity) + " Local: " + player.isLocalPlayer
               + "\nCurrHealth: {" + health + "} : {" + displayedHealth + "}" 
               + "\nAnim: " + _anims[0].currentAnimation;
            if (player.isLocalPlayer) {
               debugTextLog.color = Color.red;
            } else {
               debugTextLog.color = Color.yellow;
            }
            debugTextLog.text = newMessage;
         }
      }

      // Handle the drawing or hiding of our outline
      if (!Util.isBatch()) {
         handleSpriteOutline();
      }
   }

   // Basic method that will handle the functionality for whenever we click on this battler
   public void selectThis () {
      onBattlerSelect.Invoke();
   }

   public void initializeBattlerData () {
      if (!_hasInitializedStats) {
         BattlerData battlerData = MonsterManager.self.getBattlerData(enemyType);

         if (battlerType == BattlerType.PlayerControlled) {
            battlerData = MonsterManager.self.getBattlerData(Enemy.Type.PlayerBattler);
         } else {
            // Sets the default monster if data is not yet created in xml editor
            if (battlerData == null) {
               battlerData = MonsterManager.self.getBattlerData(Enemy.Type.Lizard);
            }
         }

         if (battlerData != null) {
            _alteredBattlerData = BattlerData.CreateInstance(battlerData);
            setElementalWeakness();
         } else {
            D.debug("DATA IS NULL");
         }

         if (battlerType == BattlerType.AIEnemyControlled) {
            selectedBattleBar = minionBattleBar;

            // Change sprite fetched from battler data
            if (!Util.isBatch()) {
               Sprite fetchSprite = ImageManager.getSprite(battlerData.imagePath);
               if (fetchSprite != null) {
                  mainSpriteRenderer.sprite = fetchSprite;
               }

               // Offset sprite for large monsters
               if (fetchSprite.rect.height >= LARGE_MONSTER_SIZE) {
                  Vector3 localPos = mainSpriteRenderer.transform.localPosition;
                  mainSpriteRenderer.transform.localPosition = new Vector3(localPos.x, LARGE_MONSTER_OFFSET, localPos.z);
                  selectedBattleBar = bossBattleBar;

                  // Enlarge the click box of a boss type enemy
                  _clickableBox.GetComponent<BoxCollider2D>().size = new Vector2(1, 1);
               }

               shadowTransform.localScale = new Vector2(_alteredBattlerData.shadowScale, _alteredBattlerData.shadowScale);
               shadowTransform.localPosition = new Vector3(_alteredBattlerData.shadowOffset.x, _alteredBattlerData.shadowOffset.y, shadowTransform.localPosition.z);
            }
            selectedBattleBar.gameObject.SetActive(true);

            List<BasicAbilityData> basicAbilityDataList = new List<BasicAbilityData>();
            foreach (int id in _alteredBattlerData.battlerAbilities.attackAbilityDataList) {
               AttackAbilityData attackData = AbilityManager.getAttackAbility(id);
               basicAbilityDataList.Add(attackData);
            }
            foreach (int id in _alteredBattlerData.battlerAbilities.buffAbilityDataList) {
               BuffAbilityData buffData = AbilityManager.getBuffAbility(id);
               basicAbilityDataList.Add(buffData);
            }

            setBattlerAbilities(basicAbilityDataList, battlerType);

            // Extra cooldown time for AI controlled battlers, so they do not attack instantly
            this.cooldownEndTime = NetworkTime.time + 5f;
         } else if (battlerType == BattlerType.PlayerControlled && Global.player != null) {
            if (userId != Global.player.userId) {
               selectedBattleBar = minionBattleBar;
               selectedBattleBar.gameObject.SetActive(false);
            } else {
               BattleUIManager.self.abilitiesCG.Show();
               selectedBattleBar = minionBattleBar;
               selectedBattleBar.nameText.text = Global.player.nameText.text;
               BattleUIManager.self.playerBattleCG.Hide();
               selectedBattleBar.gameObject.SetActive(false);
            }
         }

         updateAnimGroup(battlerData.animGroup);

         // Enemy stat setup
         setupEnemyStats();

         _hasInitializedStats = true;
      }
   }

   private void syncAbilities () {
      if (battlerType == BattlerType.PlayerControlled) {
         // Create new ability record to assign to this entity
         _battlerBasicAbilities = new List<BasicAbilityData>();

         foreach (int basicID in basicAbilityIDList) {
            BasicAbilityData basicData = AbilityManager.getAbility(basicID, AbilityType.Undefined);
            if (basicData != null) {
               _battlerBasicAbilities.Add(basicData);
            }
         }
      }
   }

   #region Stat Related Functions

   private void setupEnemyStats () {
      if (battlerType == BattlerType.AIEnemyControlled) {
         int level = LevelUtil.levelForXp(XP);

         BattlerData battleData = getBattlerData();

         battleData.baseDamageMultiplierSet.physicalAttackMultiplier += (level * battleData.perLevelDamageMultiplierSet.physicalAttackMultiplierPerLevel);
         battleData.baseDamageMultiplierSet.fireAttackMultiplier += (level * battleData.perLevelDamageMultiplierSet.fireAttackMultiplierPerLevel);
         battleData.baseDamageMultiplierSet.waterAttackMultiplier += (level * battleData.perLevelDamageMultiplierSet.waterAttackMultiplierPerLevel);
         battleData.baseDamageMultiplierSet.airAttackMultiplier += (level * battleData.perLevelDamageMultiplierSet.airAttackMultiplierPerLevel);
         battleData.baseDamageMultiplierSet.earthAttackMultiplier += (level * battleData.perLevelDamageMultiplierSet.earthAttackMultiplierPerLevel);

         battleData.baseDefenseMultiplierSet.physicalDefenseMultiplier = Mathf.Abs(getBattlerData().baseDefenseMultiplierSet.physicalDefenseMultiplier) + (level * battleData.perLevelDefenseMultiplierSet.physicalDefenseMultiplierPerLevel);
         battleData.baseDefenseMultiplierSet.fireDefenseMultiplier = Mathf.Abs(getBattlerData().baseDefenseMultiplierSet.fireDefenseMultiplier) + (level * battleData.perLevelDefenseMultiplierSet.fireDefenseMultiplierPerLevel);
         battleData.baseDefenseMultiplierSet.waterDefenseMultiplier = Mathf.Abs(getBattlerData().baseDefenseMultiplierSet.waterDefenseMultiplier) + (level * battleData.perLevelDefenseMultiplierSet.waterDefenseMultiplierPerLevel);
         battleData.baseDefenseMultiplierSet.airDefenseMultiplier = Mathf.Abs(getBattlerData().baseDefenseMultiplierSet.airDefenseMultiplier) + (level * battleData.perLevelDefenseMultiplierSet.airDefenseMultiplierPerLevel);
         battleData.baseDefenseMultiplierSet.earthDefenseMultiplier = Mathf.Abs(getBattlerData().baseDefenseMultiplierSet.earthDefenseMultiplier) + (level * battleData.perLevelDefenseMultiplierSet.earthDefenseMultiplierPerLevel);
      }
   }

   private void addDefaultStats (UserDefaultStats stat) {
      _alteredBattlerData.baseHealth += (int) stat.bonusMaxHP;
      _alteredBattlerData.healthPerlevel += (int) stat.hpPerLevel;

      this.health += (int) stat.bonusMaxHP + ((int) stat.hpPerLevel * LevelUtil.levelForXp(XP));

      _alteredBattlerData.baseDamage += (int) stat.bonusATK;
      _alteredBattlerData.damagePerLevel += (int) stat.bonusATKPerLevel;

      _alteredBattlerData.baseDefense += (int) stat.bonusArmor;
      _alteredBattlerData.defensePerLevel += (int) stat.armorPerLevel;
   }

   private void addCombatStats (UserCombatStats stat) {
      int level = LevelUtil.levelForXp(player.XP);

      _alteredBattlerData.baseDamageMultiplierSet.airAttackMultiplier += stat.bonusDamageAir;
      _alteredBattlerData.baseDamageMultiplierSet.fireAttackMultiplier += stat.bonusDamageFire;
      _alteredBattlerData.baseDamageMultiplierSet.earthAttackMultiplier += stat.bonusDamageEarth;
      _alteredBattlerData.baseDamageMultiplierSet.waterAttackMultiplier += stat.bonusDamageWater;
      _alteredBattlerData.baseDamageMultiplierSet.physicalAttackMultiplier += stat.bonusDamagePhys;
      _alteredBattlerData.baseDamageMultiplierSet.allAttackMultiplier += stat.bonusDamageAll;

      _alteredBattlerData.baseDamageMultiplierSet.airAttackMultiplier += stat.bonusDamageAirPerLevel * level;
      _alteredBattlerData.baseDamageMultiplierSet.fireAttackMultiplier += stat.bonusDamageFirePerLevel * level;
      _alteredBattlerData.baseDamageMultiplierSet.earthAttackMultiplier += stat.bonusDamageEarthPerLevel * level;
      _alteredBattlerData.baseDamageMultiplierSet.waterAttackMultiplier += stat.bonusDamageWaterPerLevel * level;
      _alteredBattlerData.baseDamageMultiplierSet.physicalAttackMultiplier += stat.bonusDamagePhysicalPerLevel * level;
      _alteredBattlerData.baseDamageMultiplierSet.allAttackMultiplier += stat.bonusDamageAllPerLevel * level;

      _alteredBattlerData.baseDefenseMultiplierSet.airDefenseMultiplier += stat.bonusResistanceAir;
      _alteredBattlerData.baseDefenseMultiplierSet.fireDefenseMultiplier += stat.bonusResistanceFire;
      _alteredBattlerData.baseDefenseMultiplierSet.earthDefenseMultiplier += stat.bonusResistanceEarth;
      _alteredBattlerData.baseDefenseMultiplierSet.waterDefenseMultiplier += stat.bonusResistanceWater;
      _alteredBattlerData.baseDefenseMultiplierSet.physicalDefenseMultiplier += stat.bonusResistancePhys;
      _alteredBattlerData.baseDefenseMultiplierSet.allDefenseMultiplier += stat.bonusResistanceAll;

      _alteredBattlerData.baseDefenseMultiplierSet.airDefenseMultiplier += stat.bonusResistanceAirPerLevel * level;
      _alteredBattlerData.baseDefenseMultiplierSet.fireDefenseMultiplier += stat.bonusResistanceFirePerLevel * level;
      _alteredBattlerData.baseDefenseMultiplierSet.earthDefenseMultiplier += stat.bonusResistanceEarthPerLevel * level;
      _alteredBattlerData.baseDefenseMultiplierSet.waterDefenseMultiplier += stat.bonusResistanceWaterPerLevel * level;
      _alteredBattlerData.baseDefenseMultiplierSet.physicalDefenseMultiplier += stat.bonusResistancePhysPerLevel * level;
      _alteredBattlerData.baseDefenseMultiplierSet.allDefenseMultiplier += stat.bonusResistanceAllPerLevel * level;
   }

   #endregion

   // Basic method that will handle the functionality for whenever we deselect this battler
   public void deselectThis () {
      BattleSelectionManager.self.deselectTarget();
      onBattlerDeselect.Invoke();
   }

   private void updateSprites () {
      if (!Util.isBatch()) {
         // Update the Body, Eyes, and Hair
         foreach (BodyLayer bodyLayer in GetComponentsInChildren<BodyLayer>()) {
            bodyLayer.setType(bodyType);

            // We only call recolor on the body because we want the material to be instanced like all the others
            bodyLayer.recolor("");
         }
         foreach (EyesLayer eyesLayer in GetComponentsInChildren<EyesLayer>()) {
            eyesLayer.setType(eyesType);
            eyesLayer.recolor(eyesPalettes);
         }
         foreach (HairLayer hairLayer in GetComponentsInChildren<HairLayer>()) {
            hairLayer.setType(hairType);
            hairLayer.recolor(hairPalettes);
         }

         // Update the Armor, hat and Weapon
         if (armorManager.hasArmor()) {
            armorManager.updateSprites();
         }
         if (weaponManager.hasWeapon()) {
            weaponManager.updateSprites();
         }
         if (hatManager.hasHat()) {
            hatManager.updateSprites();
         }
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

      // Hide or show battler Name
      if (battlerType.Equals(BattlerType.PlayerControlled) && Global.player != null) {
         bool hoverPlayerNames = false;
         if (BattleSelectionManager.self.selectedBattler != null) {
            hoverPlayerNames = BattleSelectionManager.self.selectedBattler.enemyType != Enemy.Type.PlayerBattler;
         } else {
            hoverPlayerNames = true;
         }

         if (hoverPlayerNames) {
            BattleUIManager.self.usernameText.gameObject.SetActive(false);
            selectedBattleBar.gameObject.SetActive(isMouseHovering());
         } else {
            selectedBattleBar.gameObject.SetActive(false);
         }
      } else {
         if (selectedBattleBar != null) {
            selectedBattleBar.gameObject.SetActive(isMouseHovering());
         }
      }

      // Any time out sprite changes, we need to regenerate our outline
      _outline.recreateOutlineIfVisible();
   }

   public void setBattlerAbilities (List<BasicAbilityData> basicAbilityList, BattlerType battlerType) {
      if (basicAbilityList.Count == 0) {
         _battlerBasicAbilities.Add(AbilityManager.self.allAttackbilities[0]);
      }

      if (battlerAbilitiesInitialized) {
         return;
      }

      // Create initialized copies of the stances data.
      _balancedInitializedStance = BasicAbilityData.CreateInstance(AbilityInventory.self.balancedStance);
      _offenseInitializedStance = BasicAbilityData.CreateInstance(AbilityInventory.self.offenseStance);
      _defensiveInitializedStance = BasicAbilityData.CreateInstance(AbilityInventory.self.defenseStance);

      if (basicAbilityList != null) {
         foreach (BasicAbilityData abilityData in basicAbilityList) {
            _battlerBasicAbilities.Add(abilityData);

            // Sync ID's for battlers
            if (NetworkServer.active) {
               basicAbilityIDList.Add(abilityData.itemID);
            }
         }
         battlerAbilitiesInitialized = true;
      } else {
         D.debug("Issue here, There is no ability to set");
      }
   }

   private void checkIfSpritesShouldFlip () {
      // All of the Battlers on the right side of the board need to flip
      if (this.teamType == Battle.TeamType.Defenders) {
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
      double buffDuration = buff.buffEndTime - NetworkTime.time;

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

   public void inflictBossDamage (int damage) {
      StartCoroutine(CO_ProcessBossDamage(damage));
   }

   private IEnumerator CO_ProcessBossDamage (int damage) {
      yield return new WaitForSeconds(2);
      EffectManager.self.create(Effect.Type.Blunt_Physical, getCorePosition());
      playAnim(Anim.Type.Hurt_East);
      yield return new WaitForSeconds(.5f);
      playAnim(Anim.Type.Battle_East);
   }

   public void playDeathSound () {
      SoundEffect deathSoundEffect = SoundEffectManager.self.getSoundEffect(getBattlerData().deathSoundEffectId);

      if (deathSoundEffect == null) {
         Debug.LogWarning("Battler does not have a death sound effect");
         return;
      }

      SoundEffectManager.self.playSoundEffect(deathSoundEffect.id, transform);
   }

   public void handleEndOfBattle (Battle.TeamType winningTeam) {
      if (teamType != winningTeam) {
         if (isMonster()) {
            // Monster battler
            Enemy enemy = (Enemy) player;

            if (!enemy.isDefeated) {
               enemy.isDefeated = true;
            }
         } else {
            // The user might be offline, in which case we need to modify their position in the DB
            Vector2 pos = SpawnManager.self.getLocalPosition(Area.STARTING_TOWN, Spawn.STARTING_SPAWN);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               DB_Main.setNewLocalPosition(userId, pos, Direction.North, Area.STARTING_TOWN);
            });
         }
      } else {
         if (player is PlayerBodyEntity) {
            ((PlayerBodyEntity) player).Target_AllowPlayerCombat();
         }
      }
   }

   public void playJumpSound () {
      SoundEffect jumpSoundEffect = SoundEffectManager.self.getSoundEffect(getBattlerData().jumpSoundEffectId);

      if (jumpSoundEffect == null) {
         Debug.LogWarning("Battler does not have a jump sound effect");
         return;
      }

      SoundEffectManager.self.playSoundEffect(jumpSoundEffect.id, transform);
   }

   public void playAnim (Anim.Type animationType) {
      // Make all of our Simple Animation components play the animation
      foreach (SimpleAnimation anim in _anims) {
         if (anim.enabled) {
            anim.playAnimation(animationType);
         } 
      }
   }

   private void updateAnimGroup (Anim.Group animGroup) {
      this.animGroup = animGroup;
      foreach (SimpleAnimation anim in _anims) {
         anim.group = animGroup;
      }
   }

   public void pauseAnim (bool isPaused) {
      // Make all of our Simple Animation components play the animation
      foreach (SimpleAnimation anim in _anims) {
         if (anim.enabled) {
            anim.isPaused = isPaused;
         }
      }
   }

   public void modifyAnimSpeed (float speed) {
      // Make all of our Simple Animation components play the animation
      foreach (SimpleAnimation anim in _anims) {
         if (anim.enabled) {
            anim.modifyAnimSpeed(speed);
         }
      }
   }

   private void triggerAbilityCooldown (AbilityType abilityType, int abilityIndex, float cooldownDuration) {
      // Remove coroutine cache, this ability can not be stopped anymore
      currentActionCoroutine = null;

      // Implement ability button cooldowns
      if (enemyType == Enemy.Type.PlayerBattler && userId == Global.player.userId) {
         AttackPanel.self.clearCachedAbilityCast();
         BattleUIManager.self.initializeAbilityCooldown(abilityType, abilityIndex, cooldownDuration);
      }
   }

   public IEnumerator animateDeath () {
      playAnim(Anim.Type.Death_East);

      if (battlerType == BattlerType.AIEnemyControlled) {
         // Play our customized death sound
         playDeathSound();

         // Wait a little bit for it to finish
         yield return new WaitForSeconds(.25f);

         // Hide this unit when it dies
         if (_alteredBattlerData.disableOnDeath) {
            mainSpriteRenderer.enabled = false;
         }

         // Play a "Poof" effect on our head
         EffectManager.playPoofEffect(this);

         BattleSelectionManager.self.autoTargetNextOpponent();
      }
   }

   public IEnumerator buffDisplay (double timeToWait, BattleAction battleAction, bool isFirstAction) {
      // This feature handles all possible buffs such as Healing, Attack Buff etc etc
      Battle battle = BattleManager.self.getBattle(battleAction.battleId);
      Battler sourceBattler = battle.getBattler(battleAction.sourceId);

      BuffAbilityData abilityDataReference = (BuffAbilityData) AbilityManager.getAbility(battleAction.abilityGlobalID, AbilityType.BuffDebuff);
      BuffAbilityData globalAbilityData = BuffAbilityData.CreateInstance(abilityDataReference);

      // Highlight ability to be casted
      if (enemyType == Enemy.Type.PlayerBattler && userId == Global.player.userId) {
         BattleUIManager.self.initializeAbilityCooldown(AbilityType.BuffDebuff, battleAction.abilityInventoryIndex);
      }

      triggerAbilityCooldown(AbilityType.BuffDebuff, battleAction.abilityInventoryIndex, battleAction.cooldownDuration);

      switch (globalAbilityData.buffActionType) {
         case BuffActionType.Regeneration:
            // Cast version of the Buff Action
            BuffAction buffAction = (BuffAction) battleAction;
            Battler targetBattler = battle.getBattler(buffAction.targetId);

            // Don't start animating until both sprites are available
            yield return new WaitForSecondsDouble(timeToWait);

            // Make sure the battlers are still alive at this point
            if (sourceBattler.isDead()) {
               yield break;
            }

            // Start the buff animation that will eventually create the magic effect
            if (isFirstAction) {
               // Cast Animation
               sourceBattler.playAnim(Anim.Type.Toast);
            }

            // Play any sounds that go along with the ability casting
            abilityDataReference.playCastClipAtTarget(targetBattler.transform);

            // Play The effect of the buff
            Vector3 castPosition = targetBattler.getMagicGroundPosition();

            // TODO: Adjust casting offset based on cast position
            switch (abilityDataReference.abilityCastPosition) {
               case BasicAbilityData.AbilityCastPosition.AboveSelf:
                  castPosition = targetBattler.getMagicGroundPosition();
                  break;
               case BasicAbilityData.AbilityCastPosition.AboveTarget:
                  castPosition = targetBattler.getMagicGroundPosition();
                  break;
               case BasicAbilityData.AbilityCastPosition.Self:
                  castPosition = targetBattler.getMagicGroundPosition();
                  break;
               default:
                  D.debug("Unknown Cast Position: " + abilityDataReference.abilityCastPosition);
                  break;
            }
            EffectManager.playCastAbilityVFX(sourceBattler, buffAction, castPosition, BattleActionType.BuffDebuff);

            yield return new WaitForSeconds(sourceBattler.getPreContactLength());

            // Play the magic vfx such as (VFX for Heal, VFX for Attack Boost, etc etc)
            Vector2 effectPosition = targetBattler.mainSpriteRenderer.bounds.center;
            EffectManager.playCombatAbilityVFX(sourceBattler, targetBattler, buffAction, effectPosition, BattleActionType.BuffDebuff);

            // Play any sounds that go along with the ability taking effect
            abilityDataReference.playHitClipAtTarget(targetBattler.transform);

            // Shows how much health is being restored
            BattleUIManager.self.showHealText(buffAction, targetBattler);

            // Add the healing value
            targetBattler.displayedHealth += buffAction.buffValue;
            targetBattler.displayedHealth = Util.clamp<int>(targetBattler.displayedHealth, 0, targetBattler.getStartingHealth());

            yield return new WaitForSeconds(POST_CONTACT_LENGTH);

            if (isFirstAction) {
               // Switch back to our battle stance
               sourceBattler.playAnim(Anim.Type.Battle_East);

               // Add any AP we earned
               sourceBattler.addAP(buffAction.sourceApChange);
            }

            onBattlerAttackEnd.Invoke();
            break;
         case BuffActionType.BonusStat: {
               // Cast version of the Buff Action
               buffAction = (BuffAction) battleAction;
               targetBattler = battle.getBattler(buffAction.targetId);

               // Don't start animating until both sprites are available
               yield return new WaitForSecondsDouble(timeToWait);

               // Make sure the battlers are still alive at this point
               if (sourceBattler.isDead()) {
                  yield break;
               }

               // Start the buff animation that will eventually create the magic effect
               if (isFirstAction) {
                  // Cast Animation
                  sourceBattler.playAnim(Anim.Type.Toast);
               }

               // Play any sounds that go along with the ability being cast
               abilityDataReference.playCastClipAtTarget(targetBattler.transform);

               // Play The effect of the buff
               castPosition = targetBattler.getMagicGroundPosition();

               // TODO: Adjust casting offset based on cast position
               switch (abilityDataReference.abilityCastPosition) {
                  case BasicAbilityData.AbilityCastPosition.AboveSelf:
                     castPosition = targetBattler.getMagicGroundPosition();
                     break;
                  case BasicAbilityData.AbilityCastPosition.AboveTarget:
                     castPosition = targetBattler.getMagicGroundPosition();
                     break;
                  case BasicAbilityData.AbilityCastPosition.Self:
                     castPosition = targetBattler.getMagicGroundPosition();
                     break;
                  default:
                     D.debug("Unknown Cast Position: " + abilityDataReference.abilityCastPosition);
                     break;
               }
               EffectManager.playCastAbilityVFX(sourceBattler, buffAction, castPosition, BattleActionType.BuffDebuff);

               yield return new WaitForSeconds(sourceBattler.getPreContactLength());

               // Play any sounds that go along with the ability taking effect
               abilityDataReference.playHitClipAtTarget(targetBattler.transform);

               // Play the magic vfx such as (VFX for Heal, VFX for Attack Boost, etc etc)
               effectPosition = targetBattler.mainSpriteRenderer.bounds.center;
               EffectManager.playCombatAbilityVFX(sourceBattler, targetBattler, buffAction, effectPosition, BattleActionType.BuffDebuff);

               // Shows how much health is being restored
               BattleUIManager.self.showHealText(buffAction, targetBattler);

               // Add the stat value
               if (globalAbilityData.bonusStatType == BonusStatType.Attack) {
                  switch (globalAbilityData.elementType) {
                     case Element.Fire:
                        targetBattler.bonusFireAttack += buffAction.buffValue;
                        break;
                     case Element.Water:
                        targetBattler.bonusWaterAttack += buffAction.buffValue;
                        break;
                     case Element.Air:
                        targetBattler.bonusAirAttack += buffAction.buffValue;
                        break;
                     case Element.Earth:
                        targetBattler.bonusEarthAttack += buffAction.buffValue;
                        break;
                     case Element.Physical:
                        targetBattler.bonusPhysicalAttack += buffAction.buffValue;
                        break;
                  }
               }

               yield return new WaitForSeconds(POST_CONTACT_LENGTH);

               if (isFirstAction) {
                  // Switch back to our battle stance
                  sourceBattler.playAnim(Anim.Type.Battle_East);

                  // Add any AP we earned
                  sourceBattler.addAP(buffAction.sourceApChange);
               }

               onBattlerAttackEnd.Invoke();
            }
            break;
         default:
            D.warning("Ability doesn't know how to handle action: " + battleAction + ", ability: " + this);
            yield break;
      }
   }

   public IEnumerator attackDisplay (double timeToWait, BattleAction battleAction, bool isFirstAction) {
      // In here we will check all the information related to the ability we want to execute
      // If it is a melee attack, then normally we will get close to the target battler and execute an animation
      // If it is a ranged attack, then normally we will stay in our place, executing our cast particles (if any)

      // Then proceeding to execute the remaining for each path, it is a little extensive, but definitely a lot better than
      // creating a lot of different scripts

      Battle battle = BattleManager.self.getBattle(battleAction.battleId);
      Battler sourceBattler = battle.getBattler(battleAction.sourceId);
      modifyAnimSpeed(-1);

      // I believe we must grab the index from this battler, since this will be the one executing the attack
      AttackAbilityData attackerAbility = null;

      AttackAbilityData abilityDataReference = (AttackAbilityData) AbilityManager.getAbility(battleAction.abilityGlobalID, AbilityType.Standard);
      AttackAbilityData globalAbilityData = AttackAbilityData.CreateInstance(abilityDataReference);
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponManager.equipmentDataId);
      
      // Highlight ability to be casted
      if (enemyType == Enemy.Type.PlayerBattler && userId == Global.player.userId) {
         BattleUIManager.self.initializeAbilityCooldown(AbilityType.Standard, battleAction.abilityInventoryIndex);
      }

      // Init the position of the ability effect
      Vector2 effectPosition = new Vector2();

      // Cancel ability works differently, so before we try to get the ability from the local battler, 
      // we check if the ability we want to reference is a cancel ability

      if (!globalAbilityData.isCancel()) {
         attackerAbility = getAttackAbility(battleAction.abilityInventoryIndex);
      }

      switch (globalAbilityData.abilityActionType) {
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
            Battler targetBattler = battle.getBattler(action.targetId);
            Vector2 startPos = sourceBattler.battleSpot.transform.position;

            float jumpDuration = attackerAbility.getJumpDuration(sourceBattler, targetBattler);

            // Don't start animating until both sprites are available
            yield return new WaitForSecondsDouble(timeToWait);
            triggerAbilityCooldown(AbilityType.Standard, battleAction.abilityInventoryIndex, battleAction.cooldownDuration);

            // Make sure the source battler is still alive at this point
            if (sourceBattler.isDead()) {
               yield break;
            }

            if (isMovable()) {
               // Mark the source battler as jumping
               sourceBattler.isJumping = true;

               // Play an appropriate jump sound
               sourceBattler.playJumpSound();

               // Smoothly jump into position
               sourceBattler.playAnim(Anim.Type.Jump_East);
               Vector2 targetPosition = targetBattler.getMeleeStandPosition(sourceBattler.weaponManager.weaponType != 0);
               float startTime = Time.time;
               while (Time.time - startTime < jumpDuration) {
                  float timePassed = Time.time - startTime;
                  Vector2 newPos = Vector2.Lerp(startPos, targetPosition, (timePassed / jumpDuration));
                  sourceBattler.transform.position = new Vector3(newPos.x, newPos.y, sourceBattler.transform.position.z);
                  yield return 0;
               }

               // Make sure we're exactly in position now that the jump is over
               sourceBattler.transform.position = new Vector3(targetPosition.x, targetPosition.y, sourceBattler.transform.position.z);
            }

            // Pause for a moment after reaching our destination
            yield return new WaitForSeconds(PAUSE_LENGTH);

            // Plays the melee cast ability
            EffectManager.playCastAbilityVFX(sourceBattler, action, sourceBattler.transform.position, BattleActionType.Attack);
            if (sourceBattler.isUnarmed() && sourceBattler.enemyType == Enemy.Type.PlayerBattler) {
               sourceBattler.playAnim(Anim.Type.Punch);
            } else {
               if (enemyType == Enemy.Type.PlayerBattler) {
                  sourceBattler.playAnim(attackerAbility.getAnimation());
               } else {
                  sourceBattler.playAnim(Anim.Type.Ready_Attack);
               }
            }

            // Play any sounds that go along with the ability being cast
            attackerAbility.playCastClipAtTarget(targetBattler.transform);

            // Apply the damage at the correct time in the swing animation
            yield return new WaitForSeconds(sourceBattler.getPreContactLength());

            if (sourceBattler.isUnarmed() && sourceBattler.enemyType == Enemy.Type.PlayerBattler) {
               sourceBattler.playAnim(Anim.Type.Battle_East);
            } else {
               sourceBattler.playAnim(Anim.Type.Finish_Attack);
            }

            #region Display Block

            // Play the sound associated for hit
            attackerAbility.playHitClipAtTarget(targetBattler.transform);

            // Simulate the collision effect of the attack towards the target battler
            yield return StartCoroutine(CO_SimulateCollisionEffects(targetBattler, abilityDataReference, action));

            // If the action was blocked, animate that
            if (action.wasBlocked) {
               targetBattler.StartCoroutine(targetBattler.animateBlock(sourceBattler));
            } else {
               // Play an appropriate attack animation effect
               effectPosition = targetBattler.getMagicGroundPosition() + new Vector2(0f, .25f);
               EffectManager.playCombatAbilityVFX(sourceBattler, targetBattler, action, effectPosition, BattleActionType.Attack);

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

            if (isMovable()) {
               // Now jump back to where we started from
               sourceBattler.playAnim(Anim.Type.Jump_East);
               float startTime = Time.time;

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
            }

            // Switch back to our battle stance
            sourceBattler.playAnim(Anim.Type.Battle_East);
            sourceBattler.pauseAnim(false);

            // Mark the source sprite as no longer jumping
            sourceBattler.isJumping = false;

            // Add any AP we earned
            sourceBattler.displayedAP = Util.clamp<int>(sourceBattler.displayedAP + action.sourceApChange, 0, MAX_AP);
            targetBattler.displayedAP = Util.clamp<int>(targetBattler.displayedAP + action.targetApChange, 0, MAX_AP);

            // If the target died, animate that death now
            if (targetBattler.isDead()) {
               BattleSelectionManager.self.deselectTarget();
               targetBattler.StartCoroutine(targetBattler.animateDeath());
            }

            onBattlerAttackEnd.Invoke();

            break;
         case AbilityActionType.Ranged:
            // Cast version of the Attack Action
            action = (AttackAction) battleAction;
            targetBattler = battle.getBattler(action.targetId);

            // Don't start animating until both sprites are available
            yield return new WaitForSecondsDouble(timeToWait);
            triggerAbilityCooldown(AbilityType.Standard, battleAction.abilityInventoryIndex, battleAction.cooldownDuration);

            // The unused code is on the MagicAbility script
            // Make sure the battlers are still alive at this point
            if (sourceBattler.isDead()) {
               yield break;
            }

            float projectileSpawnOffsetX = sourceBattler.isAttacker() ? .38f : -.38f;
            float projectileSpawnOffsetY = .28f;
            Vector2 sourcePos = getMagicGroundPosition() + new Vector2(projectileSpawnOffsetX, projectileSpawnOffsetY);
            Vector2 targetPos = targetBattler.getMagicGroundPosition() + new Vector2(0, projectileSpawnOffsetY);

            // Determines the animation speed modification when playing shoot animation
            float shootAnimSpeed = 1.5f;

            // Start the attack animation that will eventually create the magic effect
            if (isFirstAction) {
               // Aim gun animation
               sourceBattler.playAnim(Anim.Type.Ready_Attack);

               yield return new WaitForSeconds(PRE_AIM_DELAY);

               // Play the sound associated for casting
               attackerAbility.playCastClipAtTarget(targetBattler.transform);

               yield return new WaitForSeconds(AIM_DURATION);
            }
            
            // Shoot the projectile after playing cast time
            string spriteProjectile = "";
            if (abilityDataReference.useCustomProjectileSprite) {
               spriteProjectile = attackerAbility.projectileSpritePath;
            } else {
               // Enemy battlers do not need to use this logic
               if (enemyType != Enemy.Type.PlayerBattler) {
                  spriteProjectile = attackerAbility.projectileSpritePath;
               } else {
                  if (weaponData == null) {
                     D.debug("Weapon data missing!: " + weaponManager.equipmentDataId + " : " + weaponManager.equippedWeaponId);
                  } else {
                     spriteProjectile = weaponData.projectileSprite;
                  }
               }
            }
            EffectManager.spawnProjectile(sourceBattler, action, sourcePos, targetPos, attackerAbility.projectileSpeed, spriteProjectile, attackerAbility.projectileScale, attackerAbility.FXTimePerFrame);

            EffectManager.show(Effect.Type.Cannon_Smoke, sourcePos);
            yield return new WaitForSeconds(PRE_SHOOT_DELAY);

            // Speed up animation then Animate Shoot clip for a Recoil Effect
            sourceBattler.modifyAnimSpeed(shootAnimSpeed);
            sourceBattler.pauseAnim(false);
            if (weaponData == null) {
               sourceBattler.playAnim(Anim.Type.Finish_Attack);
            } else {
               if (weaponData.weaponClass == Weapon.Class.Magic) {
                  sourceBattler.playAnim(Anim.Type.Throw_Projectile);
               } else {
                  sourceBattler.playAnim(Anim.Type.Finish_Attack);
               }
            }
            yield return new WaitForSeconds(POST_SHOOT_DELAY);

            // Return to battle stance
            sourceBattler.pauseAnim(false);
            sourceBattler.playAnim(Anim.Type.Battle_East);

            // Wait the appropriate amount of time before creating the magic effect
            float timeBeforeCollision = Vector2.Distance(sourcePos, targetPos) / attackerAbility.projectileSpeed;
            yield return new WaitForSeconds(timeBeforeCollision);
            sourceBattler.modifyAnimSpeed(-1);

            // Play the sound associated for hit
            attackerAbility.playHitClipAtTarget(targetBattler.transform);

            // Play the magic vfx such as (Flame effect on fire element attacks)
            effectPosition = targetBattler.mainSpriteRenderer.bounds.center;
            EffectManager.playCombatAbilityVFX(sourceBattler, targetBattler, action, effectPosition, BattleActionType.Attack);

            // Make the target sprite display its "Hit" animation
            targetBattler.StartCoroutine(targetBattler.animateHit(sourceBattler, action));

            // Simulate the collision effect of the attack towards the target battler
            yield return StartCoroutine(CO_SimulateCollisionEffects(targetBattler, abilityDataReference, action));
            
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
               BattleSelectionManager.self.deselectTarget();
               targetBattler.StartCoroutine(targetBattler.animateDeath());
            }

            onBattlerAttackEnd.Invoke();

            break;

         case AbilityActionType.CastToTarget:
            // Cast version of the Attack Action
            action = (AttackAction) battleAction;
            targetBattler = battle.getBattler(action.targetId);

            // Don't start animating until both sprites are available
            yield return new WaitForSecondsDouble(timeToWait);
            triggerAbilityCooldown(AbilityType.Standard, battleAction.abilityInventoryIndex, battleAction.cooldownDuration);

            // Make sure the battlers are still alive at this point
            if (sourceBattler.isDead()) {
               yield break;
            }

            float castAnimSpeed = 1.5f;
            projectileSpawnOffsetX = sourceBattler.isAttacker() ? .38f : -.38f;
            projectileSpawnOffsetY = .28f;
            sourcePos = getMagicGroundPosition() + new Vector2(projectileSpawnOffsetX, projectileSpawnOffsetY);
            targetPos = targetBattler.getMagicGroundPosition() + new Vector2(0, projectileSpawnOffsetY);

            // Start the attack animation that will eventually create the magic effect
            if (isFirstAction) {
               // Aim gun animation
               sourceBattler.playAnim(Anim.Type.Ready_Attack);

               yield return new WaitForSeconds(sourceBattler.getPreMagicLength());

               // Play any sounds that go along with the ability being cast
               attackerAbility.playCastClipAtTarget(targetBattler.transform);

               Vector3 castPosition = sourcePos;
               switch (abilityDataReference.abilityCastPosition) {
                  case BasicAbilityData.AbilityCastPosition.AboveSelf:
                     castPosition = sourceBattler.transform.position;
                     break;
                  case BasicAbilityData.AbilityCastPosition.AboveTarget:
                     castPosition = targetBattler.transform.position;
                     break;
                  case BasicAbilityData.AbilityCastPosition.Self:
                     castPosition = sourceBattler.transform.position;
                     break;
               }
               EffectManager.playCastAbilityVFX(sourceBattler, action, castPosition, BattleActionType.Attack);
            }
            yield return new WaitForSeconds(PRE_CAST_DELAY);

            // Shoot the projectile after playing cast time
            if (abilityDataReference.useCustomProjectileSprite) {
               spriteProjectile = attackerAbility.projectileSpritePath;
            } else {
               spriteProjectile = weaponData.projectileSprite;
            }
            EffectManager.spawnProjectile(sourceBattler, action, sourcePos, targetPos, attackerAbility.projectileSpeed, spriteProjectile, attackerAbility.projectileScale, attackerAbility.FXTimePerFrame);

            // Speed up animation then Animate Shoot clip for a Recoil Effect
            sourceBattler.modifyAnimSpeed(castAnimSpeed);
            sourceBattler.pauseAnim(false);
            if (weaponData.weaponClass == Weapon.Class.Magic) {
               sourceBattler.playAnim(Anim.Type.Throw_Projectile);
            } else {
               sourceBattler.playAnim(Anim.Type.Finish_Attack);
            }
            yield return new WaitForSeconds(POST_CAST_DELAY);

            // Return to battle stance
            sourceBattler.pauseAnim(false);
            sourceBattler.playAnim(Anim.Type.Battle_East);

            // Wait the appropriate amount of time before creating the magic effect
            timeBeforeCollision = Vector2.Distance(sourcePos, targetPos) / attackerAbility.projectileSpeed;
            float effectOffset = .1f;
            yield return new WaitForSeconds(timeBeforeCollision - effectOffset - POST_CAST_DELAY);
            sourceBattler.modifyAnimSpeed(-1);

            // Play the sound associated for hit
            attackerAbility.playHitClipAtTarget(targetBattler.transform);

            // Play the magic vfx such as (Flame effect on fire element attacks)
            effectPosition = targetBattler.mainSpriteRenderer.bounds.center;
            EffectManager.playCombatAbilityVFX(sourceBattler, targetBattler, action, effectPosition, BattleActionType.Attack);

            // Make the target sprite display its "Hit" animation
            targetBattler.StartCoroutine(targetBattler.animateHit(sourceBattler, action));

            // Simulate the collision effect of the attack towards the target battler
            yield return StartCoroutine(CO_SimulateCollisionEffects(targetBattler, abilityDataReference, action));

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
               BattleSelectionManager.self.deselectTarget();
               targetBattler.StartCoroutine(targetBattler.animateDeath());
            }

            onBattlerAttackEnd.Invoke();

            break;

         case AbilityActionType.Cancel:
            // Cancel requires time before activating.
            yield return new WaitForSecondsDouble(timeToWait);

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

      if (userId == Global.player.userId && enemyType == Enemy.Type.PlayerBattler) {
         setBattlerCanCastAbility(true);
         canCancelAction = true;
      }
   }

   #region Combat Effect Simulation

   private IEnumerator CO_SimulateCollisionEffects (Battler targetBattler, AttackAbilityData abilityDataReference, AttackAction action) {
      if (health > 0) {
         if (abilityDataReference.hasKnockup && targetBattler.isMovable()) {
            // If this magic ability has knockup, then start it now
            targetBattler.StartCoroutine(targetBattler.animateKnockup());
            yield return new WaitForSeconds(KNOCKUP_LENGTH);
         } else if (abilityDataReference.hasShake) {
            // If the ability magnitude will shake the screen to simulate impact
            Coroutine shakeCoroutine = targetBattler.StartCoroutine(targetBattler.animateShake());
            yield return new WaitForSeconds(SHAKE_LENGTH);
            targetBattler.StopCoroutine(shakeCoroutine);
            targetBattler.playAnim(Anim.Type.Battle_East);
         } else if (abilityDataReference.hasKnockBack) {
            // Move the sprite back and forward to simulate knockback
            targetBattler.playAnim(Anim.Type.Hurt_East);
            targetBattler.StartCoroutine(targetBattler.animateKnockback());
            yield return new WaitForSeconds(KNOCKBACK_LENGTH);
         }

         // Note that the contact is happening right now
         BattleUIManager.self.showDamageText(action, targetBattler);
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
         float yOffset = Mathf.Sin(radians) * .4f;

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

   private IEnumerator animateBlock (Battler attacker) {
      // Show the Block animation frame
      playAnim(Anim.Type.Block_East);
      EffectManager.playBlockEffect(attacker, this);
      yield return new WaitForSeconds(POST_CONTACT_LENGTH);
      playAnim(Anim.Type.Battle_East);
   }

   private IEnumerator animateHit (Battler attacker, AttackAction action) {
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

   private IEnumerator removeBuffAfterDelay (double delay, Battler battler, BuffTimer buff) {
      yield return new WaitForSecondsDouble(delay);

      battler.removeBuff(buff);
   }

   #endregion

   private IEnumerator CO_initializeClientBattler () {
      // We will wait until this battler battle is in the BattleManager battle list
      yield return new WaitUntil(() => BattleManager.self.getBattle(battleId) != null);

      if (battle == null) {
         if (battlerType == BattlerType.PlayerControlled) {
            battle = BattleManager.self.getBattle(battleId);
            transform.SetParent(battle.transform);
            transform.position = battleSpot.transform.position;
         }
      }

      yield return new WaitUntil(() => basicAbilityIDList.Count > 0);
      syncAbilities();
   }

   #endregion

   #region Getters

   #region Int / Float Functions

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
      return getBattlerData().apGainWhenDamaged;
   }

   public int getStartingHealth (Enemy.Type enemyType) {
      BattlerData battData = MonsterManager.self.getBattlerData(enemyType);
      int level = LevelUtil.levelForXp(XP);

      // Calculate our health based on our base and gain per level
      int health = (int) battData.baseHealth + ((int) battData.healthPerlevel * LevelUtil.levelForXp(XP));

      return (int) health;
   }

   public int getStartingHealth () {
      BattlerData battData = MonsterManager.self.getBattlerData(enemyType);
      int level = LevelUtil.levelForXp(XP);

      // Calculate our health based on our base and gain per level
      float health = (int) battData.baseHealth + ((int) battData.healthPerlevel * level);

      return (int) health;
   }

   public int getXPValue () {
      return getBattlerData().baseXPReward;
   }

   public int getGoldValue () {
      return getBattlerData().baseGoldReward;
   }

   public float getActionTimerPercent () {
      // Figure out how full our timer bar should be
      float fillPercent = 1f;
      float cooldownLength = (float) (this.cooldownEndTime - this.lastAbilityEndTime);

      if (cooldownLength > 0f) {
         fillPercent = (float) (NetworkTime.time - this.lastAbilityEndTime) / cooldownLength;
      }

      return Util.clamp<float>(fillPercent, 0f, 1f);
   }

   public float getStanceCooldown (Stance stance) {
      switch (stance) {
         case Stance.Balanced:
            return _balancedInitializedStance.abilityCooldown;
         case Stance.Attack:
            return _offenseInitializedStance.abilityCooldown;
         case Stance.Defense:
            return _defensiveInitializedStance.abilityCooldown;
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
      return 0.35f;
   }

   public float getPreMagicLength () {
      // The amount of time before the ground effect appears depends on the type of Battler
      return .6f;
   }

   public float getProjectileSpeed () {
      // The speed of the projectile
      return .2f;
   }

   public float getDefense (Element element) {
      int level = LevelUtil.levelForXp(XP);

      // Get the battler data
      BattlerData battlerData = getBattlerData();

      // Calculate our defense based on our base and gain per level
      float defense = battlerData.baseDefense + (battlerData.defensePerLevel * level);

      // Add our armor's defense value, if we have any
      if (armorManager.hasArmor()) {
         defense += armorManager.getArmor().getDefense(element);
      }

      if (hatManager.hasHat()) {
         defense += hatManager.getHat().getDefense(element);
      }

      float elementalMultiplier = 1;
      switch (element) {
         case Element.Physical:
            defense += bonusPhysicalDefense;
            elementalMultiplier = battlerData.baseDefenseMultiplierSet.physicalDefenseMultiplier + (battlerData.perLevelDefenseMultiplierSet.physicalDefenseMultiplierPerLevel * level);
            break;
         case Element.Fire:
            defense += bonusFireDefense;
            elementalMultiplier = battlerData.baseDefenseMultiplierSet.fireDefenseMultiplier + (battlerData.perLevelDefenseMultiplierSet.fireDefenseMultiplierPerLevel * level);
            break;
         case Element.Earth:
            defense += bonusEarthDefense;
            elementalMultiplier = battlerData.baseDefenseMultiplierSet.earthDefenseMultiplier + (battlerData.perLevelDefenseMultiplierSet.earthDefenseMultiplierPerLevel * level);
            break;
         case Element.Air:
            defense += bonusAirDefense;
            elementalMultiplier = battlerData.baseDefenseMultiplierSet.airDefenseMultiplier + (battlerData.perLevelDefenseMultiplierSet.airDefenseMultiplierPerLevel * level);
            break;
         case Element.Water:
            defense += bonusWaterDefense;
            elementalMultiplier = battlerData.baseDefenseMultiplierSet.waterDefenseMultiplier + (battlerData.perLevelDefenseMultiplierSet.waterDefenseMultiplierPerLevel * level);
            break;
      }
      defense *= Mathf.Abs(elementalMultiplier);

      // TODO: Temporary Disable until formula is finalized
      // We will add as an additional the "All" multiplier with the base defense
      // defense += getBattlerData().baseDefense + (getBattlerData().defensePerLevel * level);

      return defense;
   }

   public float getDamage (Element element) {
      int level = LevelUtil.levelForXp(XP);

      // Get the battler data
      BattlerData battlerData = getBattlerData();

      // Calculate our offense based on our base and gain per level
      float damage = battlerData.baseDamage + (battlerData.damagePerLevel * level);

      // Add our weapon's damage value, if we have a weapon
      if (weaponManager.hasWeapon()) {
         damage += weaponManager.getWeapon().getDamage(element);
      }

      float multiplier = 1;
      switch (element) {
         case Element.Physical:
            damage += bonusPhysicalAttack;
            multiplier = battlerData.baseDamageMultiplierSet.physicalAttackMultiplier + (battlerData.perLevelDamageMultiplierSet.physicalAttackMultiplierPerLevel * level);
            break;
         case Element.Fire:
            damage += bonusFireAttack;
            multiplier = battlerData.baseDamageMultiplierSet.fireAttackMultiplier + (battlerData.perLevelDamageMultiplierSet.fireAttackMultiplierPerLevel * level);
            break;
         case Element.Earth:
            damage += bonusEarthAttack;
            multiplier = battlerData.baseDamageMultiplierSet.earthAttackMultiplier + (battlerData.perLevelDamageMultiplierSet.earthAttackMultiplierPerLevel * level);
            break;
         case Element.Air:
            damage += bonusAirAttack;
            multiplier = battlerData.baseDamageMultiplierSet.airAttackMultiplier + (battlerData.perLevelDamageMultiplierSet.airAttackMultiplierPerLevel * level);
            break;
         case Element.Water:
            damage += bonusWaterAttack;
            multiplier = battlerData.baseDamageMultiplierSet.waterAttackMultiplier + (battlerData.perLevelDamageMultiplierSet.waterAttackMultiplierPerLevel * level);
            break;
      }
      damage *= multiplier;

      // TODO: Temporary Disable until formula is finalized
      // We will add as an additional the "All" multiplier with the base defense.
      // damage += (getBattlerData().baseDamage * getBattlerData().allAttackMultiplier);

      return damage;
   }

   public static float getElementalMultiplier (Element outgoingElement, Element resistingElement) {
      float neutralDamage = 1;
      float weakDamage = 0;
      float strongDamage = 1.5f;

      switch (outgoingElement) {
         case Element.Air: {
               if (resistingElement == Element.Water) {
                  // Amplifies damage if opposite is weak to it
                  return strongDamage;
               } else if (resistingElement == Element.Earth && resistingElement == Element.Air) {
                  // Negates elements that are similar or opposite
                  return weakDamage;
               } else {
                  // Neutral Damage
                  return neutralDamage;
               }
            }
         case Element.Water: {
               if (resistingElement == Element.Fire) {
                  // Amplifies damage if opposite is weak to it
                  return strongDamage;
               } else if (resistingElement == Element.Air && resistingElement == Element.Water) {
                  // Negates elements that are similar or opposite
                  return weakDamage;
               } else {
                  // Neutral Damage
                  return neutralDamage;
               }
            }
         case Element.Fire: {
               if (resistingElement == Element.Earth) {
                  // Amplifies damage if opposite is weak to it
                  return strongDamage;
               } else if (resistingElement == Element.Water && resistingElement == Element.Fire) {
                  // Negates elements that are similar or opposite
                  return weakDamage;
               } else {
                  // Neutral Damage
                  return neutralDamage;
               }
            }
         case Element.Earth: {
               if (resistingElement == Element.Air) {
                  // Amplifies damage if opposite is weak to it
                  return strongDamage;
               } else if (resistingElement == Element.Fire && resistingElement == Element.Earth) {
                  // Negates elements that are similar or opposite
                  return weakDamage;
               } else {
                  // Neutral Damage
                  return neutralDamage;
               }
            }
      }
      return neutralDamage;
   }

   #endregion 

   #region Bool functions

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
      foreach (Battler battler in getTeam()) {
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

   public bool isMovable () {
      if (isBossType) {
         return false;
      }

      return true;
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
      return (health <= 0 || displayedHealth <= 0);
   }

   public bool isAttacker () {
      return teamType == Battle.TeamType.Attackers;
   }

   private void setElementalWeakness () {
      HashSet<Element> elementalWeakness = new HashSet<Element>();
      HashSet<Element> elementalResistance = new HashSet<Element>();

      BattlerData battleData = getBattlerData();

      if (battleData.baseDefenseMultiplierSet.fireDefenseMultiplier < 0) {
         elementalWeakness.Add(Element.Fire);
      } else {
         elementalResistance.Add(Element.Fire);
      }

      if (battleData.baseDefenseMultiplierSet.waterDefenseMultiplier < 0) {
         elementalWeakness.Add(Element.Water);
      } else {
         elementalResistance.Add(Element.Water);
      }

      if (battleData.baseDefenseMultiplierSet.airDefenseMultiplier < 0) {
         elementalWeakness.Add(Element.Air);
      } else {
         elementalResistance.Add(Element.Air);
      }

      if (battleData.baseDefenseMultiplierSet.earthDefenseMultiplier < 0) {
         elementalWeakness.Add(Element.Earth);
      } else {
         elementalResistance.Add(Element.Earth);
      }

      battleData.elementalWeakness = elementalWeakness.ToArray();
      battleData.elementalResistance = elementalResistance.ToArray();
   }

   public bool isWeakAgainst (Element outgoingElement) {
      // Determines if the battler is weak against the element
      return getBattlerData().elementalWeakness.Contains(outgoingElement);
   }

   public bool isUnarmed () {
      return weaponManager.weaponType == 0;
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

   #region Getters

   public Vector2 getMagicGroundPosition () {
      return new Vector2(transform.position.x, transform.position.y - (mainSpriteRenderer.bounds.extents.y / 2));
   }

   public Vector2 getCorePosition () {
      float spawnOffsetX = this.isAttacker() ? .14f : -.14f;
      float spawnOffsetY = .28f;
      Vector2 sourcePos = getMagicGroundPosition() + new Vector2(spawnOffsetX, spawnOffsetY);
      return sourcePos;
   }

   public Vector2 getMeleeStandPosition (bool hasWeapon = true) {
      Vector2 startPos = battleSpot.transform.position;
      return startPos + new Vector2(mainSpriteRenderer.bounds.extents.x, 0) * (isAttacker() ? (hasWeapon ? 1f : .7f) : (hasWeapon ? -1f : -.7f));
   }

   public Vector2 getRangedEndPosition () {
      Vector2 startPos = battleSpot.transform.position;
      float xValue = mainSpriteRenderer.bounds.extents.x / 2;
      return startPos + new Vector2(xValue, .15f);
   }

   // Gets the battler initialized data (health, ap, etc)
   public BattlerData getBattlerData () { return _alteredBattlerData; }

   // Used for AI controlled battlers
   public BattlePlan getBattlePlan (Battle battle) {
      if (battlerType == BattlerType.AIEnemyControlled) {
         Battler target = getRandomTargetFor(getBasicAttack(), battle);
         List<Battler> allies = getBattlerAllies(battle);

         // Set up a list of targets
         List<Battler> targets = new List<Battler>();
         if (target != null) {
            targets.Add(target);
         }

         // By default, AI battlers will use the Monster attack ability
         BattlePlan newBattlePlan = new BattlePlan(getBasicAttack(), targets);
         newBattlePlan.targetAllies = allies;
         return newBattlePlan;
      } else {
         Debug.LogError("Error in battle logic, a non AI controlled battler cannot have a Battle Plan");
         return null;
      }
   }
   protected List<Battler> getBattlerAllies (Battle battle) {
      List<Battler> allies = new List<Battler>();

      // Cycle over all of the participants in the battle
      foreach (Battler targetBattler in battle.getParticipants()) {
         // Check if the battler is on the same team and not dead
         if (targetBattler.teamType == this.teamType && !targetBattler.isDead()) {
            allies.Add(targetBattler);
         }
      }
      return allies;
   }

   protected Battler getRandomTargetFor (AttackAbilityData abilityData, Battle battle) {
      List<Battler> options = new List<Battler>();

      // Cycle over all of the participants in the battle
      foreach (Battler targetBattler in battle.getParticipants()) {

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

   // Initialized stances
   public BasicAbilityData getBalancedStance () { return _balancedInitializedStance; }
   public BasicAbilityData getOffenseStance () { return _offenseInitializedStance; }
   public BasicAbilityData getDefensiveStance () { return _defensiveInitializedStance; }

   // Ability Getters
   public List<BasicAbilityData> getBasicAbilities () { return _battlerBasicAbilities; }

   public List<AttackAbilityData> getAttackAbilities () {
      List<AttackAbilityData> attackAbilities = new List<AttackAbilityData>();
      foreach (BasicAbilityData basicData in _battlerBasicAbilities) {
         if (basicData.abilityType == AbilityType.Standard) {
            attackAbilities.Add(AbilityManager.getAttackAbility(basicData.itemID));
         }
      }
      return attackAbilities;
   }

   public List<BuffAbilityData> getBuffAbilities () {
      List<BuffAbilityData> buffAbilities = new List<BuffAbilityData>();
      foreach (BasicAbilityData basicData in _battlerBasicAbilities) {
         if (basicData.abilityType == AbilityType.BuffDebuff) {
            buffAbilities.Add(AbilityManager.getBuffAbility(basicData.itemID));
         }
      }
      return buffAbilities;
   }

   public AttackAbilityData getAttackAbility (int indexID) {
      return getAttackAbilities()[indexID];
   }

   public BuffAbilityData getBuffAbilitiy (int indexID) {
      return getBuffAbilities()[indexID];
   }

   public AttackAbilityData getBasicAttack () {
      // Safe check
      if (getAttackAbilities().Count <= 0) {
         if (AbilityManager.self.allAttackbilities.Count > 0) {
            D.error("This battler do not have any abilities, setting default ability as: " + AbilityManager.self.allAttackbilities[0].itemName);
            return AbilityManager.self.allAttackbilities[0];
         } else {
            D.error("Something went wrong with ability manager!");
         }
      }

      return getAttackAbility(0);
   }

   #endregion

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

   public bool canCastAbility () {
      return _canCastAbility;
   }

   public void setBattlerCanCastAbility (bool canCast) {
      _canCastAbility = canCast;
   }

   #region Private Variables

   // If the user can cast an ability
   [SerializeField]
   private bool _canCastAbility = true;

   // Attack abilities that will be used in combat
   [SerializeField] private List<BasicAbilityData> _battlerBasicAbilities = new List<BasicAbilityData>();

   // Battler data reference that will be initialized (ready to be used, use getBattlerData() )
   [SerializeField] private BattlerData _alteredBattlerData;

   // Our Animators
   protected List<SimpleAnimation> _anims;

   // Our renderers
   protected List<SpriteRenderer> _renderers;

   // Our Sprite Outline
   protected SpriteOutline _outline;

   // Our clickable box
   protected ClickableBox _clickableBox;

   // Initialized stats
   protected bool _hasInitializedStats;

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
