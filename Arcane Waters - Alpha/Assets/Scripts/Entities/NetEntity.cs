using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;
using Smooth;

public class NetEntity : NetworkBehaviour
{
   #region Public Variables

   // The amount of time that must pass between movement changes
   public static float MOVE_CHANGE_INTERVAL = .05f;

   // The number of seconds after which an attacker stops being one
   public static float ATTACKER_STATUS_DURATION = 10f;

   // The account ID for this entity
   [SyncVar]
   public int accountId;

   // The user ID for this entity
   [SyncVar]
   public int userId;

   // The id of the Instance that this entity is in
   [SyncVar]
   public int instanceId;

   // The key of the area we're in
   [SyncVar]
   public string areaKey;

   // The Gender of this entity
   [SyncVar]
   public Gender.Type gender = Gender.Type.Male;

   // The types associated with our sprite layers
   [SyncVar]
   public BodyLayer.Type bodyType;
   [SyncVar]
   public EyesLayer.Type eyesType;
   [SyncVar]
   public HairLayer.Type hairType;

   // Our colors
   [SyncVar]
   public string eyesPalette1;
   [SyncVar]
   public string hairPalette1;
   [SyncVar]
   public string hairPalette2;

   // The Name of this entity
   [SyncVar]
   public string entityName;

   // Our current health
   [SyncVar]
   public int currentHealth = 1000;

   // Our max health
   [SyncVar]
   public int maxHealth = 1000;

   // The amount of XP we have, which we can use to show our level
   [SyncVar]
   public int XP;

   // Our desired angle of movement
   [SyncVar]
   public float desiredAngle;

   // Convenient Network Identity reference so we aren't repeatedly calling GetComponent
   [HideInInspector]
   public NetworkIdentity netIdent;

   // Our RPC manager for handling specific RPC messages
   [HideInInspector]
   public RPCManager rpc;

   // Our Crop Manager for handling crop-specific stuff
   [HideInInspector]
   public CropManager cropManager;

   // Our Admin Manager for admin related messages
   [HideInInspector]
   public AdminManager admin;

   // Our Ground Checker
   public GroundChecker groundChecker;

   // Our Water Checker
   public WaterChecker waterChecker;

   // The Text component that has our name
   public Text nameText;

   // The object we use for sorting our sprites
   public GameObject sortPoint;

   // The direction we're facing
   [SyncVar]
   public Direction facing = Direction.East;

   // Determines if this user is in single player mode
   [SyncVar]
   public bool isSinglePlayer;

   // Whether or not this Entity has sprites for diagonal directions
   public bool hasDiagonals;

   // The direction we're falling, if any
   public int fallDirection = 0;

   // The admin flag for this entity
   [SyncVar]
   public int adminFlag;

   // The flag that unlocks ship speed boost for this entity
   [SyncVar]
   public bool shipSpeedupFlag;

   // The guild this user is in
   [SyncVar]
   public int guildId;

   // The ID of the Battle this Enemy is currently in, if any
   [SyncVar]
   public int battleId;

   // The ID of the Voyage Group this user is currently in, if any
   [SyncVar]
   public int voyageGroupId = -1;

   // If this entity is jumping
   public bool isJumping = false;

   // The house layout for this user, if chosen
   public int customHouseBaseId;

   // The farm layout for this user, if chosen
   public int customFarmBaseId;

   // Gets set to true on the server when we're about to execute a warp
   public bool isAboutToWarpOnServer = false;

   // Determines if the player is animating an interact clip
   public bool interactingAnimation = false;

   // Determines if this unit is speeding
   public bool isSpeedingUp;

   // The speed multiplied when speed boosting
   public static float SPEEDUP_MULTIPLIER_SHIP = 1.5f;
   public static float MAX_SHIP_SPEED = 150;
   public static float SPEEDUP_MULTIPLIER_LAND = 1.5f;

   #endregion

   protected virtual void Awake () {
      // Look up components
      rpc = GetComponent<RPCManager>();
      cropManager = GetComponent<CropManager>();
      admin = GetComponent<AdminManager>();
      netIdent = GetComponent<NetworkIdentity>();
      _body = GetComponent<Rigidbody2D>();
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
      _smoothSync = GetComponent<SmoothSyncMirror>();
      _animators.AddRange(GetComponentsInChildren<Animator>());
      _renderers.AddRange(GetComponentsInChildren<SpriteRenderer>());

      if (this.gameObject.HasComponent<Animator>()) {
         _animators.Add(GetComponent<Animator>());
      }
      if (this.gameObject.HasComponent<SpriteRenderer>()) {
         _renderers.Add(GetComponent<SpriteRenderer>());
      }

      // Make the camera follow our player
      updatePlayerCamera();

      // Check command line
      _autoMove = CommandCodes.get(CommandCodes.Type.AUTO_MOVE);
   }

   protected virtual void Start () {
      // Make the entity a child of the Area
      StartCoroutine(CO_SetAreaParent());

      if (this is PlayerBodyEntity || this is PlayerShipEntity) {
         // Create some name text that will follow us around
         SmoothFollow smoothFollow = Instantiate(PrefabsManager.self.nameTextPrefab);
         smoothFollow.followTarget = this.gameObject;
         _nameText = smoothFollow.GetComponentInChildren<Text>();
         _nameText.text = this.entityName;

         // We need the follow text to be lower for ships
         _nameText.GetComponent<RectTransform>().offsetMin = (this is PlayerShipEntity) ? new Vector2(0, 32) : new Vector2(0, 64);

         // Keep track in our Entity Manager
         EntityManager.self.storeEntity(this);
      }

      // Keep track of the Entity that we control
      if (isLocalPlayer) {
         Global.player = this;

         // Update our currently selected user ID, so it can be used for redirects
         Global.currentlySelectedUserId = this.userId;

         // Now that we have a player, we know that the redirection process is complete
         Global.isRedirecting = false;

         // The fast login is completed
         Global.isFastLogin = false;

         // Routinely compare our Time to the Server's time
         if (!isServer) {
            InvokeRepeating("requestServerTime", 0f, 1f);
         }

         // Fetch the perk points for this user
         Global.player.rpc.Cmd_FetchPerkPointsForUser();

         // Check if the character has completed the tutorial
         if (PlayerPrefs.GetInt(this.entityName, 0) == 0) {
            PanelManager.self.noticeScreen.show("Welcome to Arcane Waters!\nIf you ever need assistance, just press F12 to bring up the Help menu.\nSafe travels!");
            PlayerPrefs.SetInt(this.entityName, 1);
         }
      }

      // Routinely clean the attackers set
      InvokeRepeating("cleanAttackers", 0f, 1f);
   }

   protected virtual void Update () {
      // Clients in standalone don't receive the entity name until a little after instantiation
      if (_nameText != null) {
         _nameText.text = this.entityName;
      }

      if (!interactingAnimation && !Util.isBatch()) {
         bool moving = isMoving();
         bool battling = isInBattle();

         // Pass our angle and rigidbody velocity on to the Animator
         foreach (Animator animator in _animators) {
            animator.SetFloat("velocityX", _body.velocity.x);
            animator.SetFloat("velocityY", _body.velocity.y);
            animator.SetBool("isMoving", moving);
            animator.SetInteger("facing", (int) this.facing);
            animator.SetBool("inBattle", battling);

            if (this is BodyEntity) {
               animator.SetInteger("fallDirection", (int) this.fallDirection);
               animator.SetBool("isClimbing", _isClimbing);
               animator.SetFloat("climbingSpeedMultiplier", moving ? 1 : 0);
            }
         }

         if (moving != _movedLastFrame) {
            if (moving) {
               onStartMoving();
            } else {
               onEndMoving();
            }
         }

         if (this is BodyEntity && _previousBodySprite != getBodyRenderer().sprite) {
            _previousBodySprite = getBodyRenderer().sprite;
            _lastBodySpriteChangetime = Time.time;
         }

         _movedLastFrame = moving;
      }

      // Hide our name while we're dead
      if (isDead()) {
         Util.setAlpha(nameText, 0f);
      }

      // Check if we're showing a West sprite
      bool isFacingWest = this.facing == Direction.West || this.facing == Direction.NorthWest || this.facing == Direction.SouthWest;

      // Flip our sprite renderer if we're going west
      foreach (SpriteRenderer renderer in _renderers) {
         renderer.flipX = isFacingWest;
      }

      // If we changed areas, update our Camera
      if (this.areaKey != _previousAreaKey) {
         updatePlayerCamera();
      }

      // Handle the drawing or hiding of our outline
      handleSpriteOutline();

      // Keep track of our previous area type
      _previousAreaKey = this.areaKey;
   }

   protected virtual void FixedUpdate () {
      // We can only control movement for our own player, when chat isn't focused, we're not falling down or dead and the area has been loaded
      if (!isLocalPlayer || ChatPanel.self.inputField.isFocused || isFalling() || isDead() || PanelManager.self.hasPanelInStack() || !AreaManager.self.hasArea(areaKey)) {
         return;
      }

      // Only change our movement if enough time has passed
      if (this is SeaEntity && Time.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      // For players in land, we want to update every frame
      bool updateEveryFrame = !(this is SeaEntity);

      // Check if we need to use the alternate delayed movement mode
      if (this is SeaEntity && SeaManager.moveMode == SeaManager.MoveMode.Delay) {
         handleDelayMoveMode();
      } else if (this is SeaEntity && SeaManager.moveMode == SeaManager.MoveMode.Arrows) {
         handleArrowsMoveMode();
      } else {
         handleInstantMoveMode(updateEveryFrame);
      }
   }

   protected virtual void OnDestroy () {
      // Remove the entity from the manager
      if (this is PlayerBodyEntity || this is PlayerShipEntity) {
         EntityManager.self.removeEntity(userId);
      }

      Vector3 localPos = this.transform.localPosition;

      if (isLocalPlayer && !ClientManager.isApplicationQuitting && !TitleScreen.self.isActive()) {
         // Show the loading screen
         if (PanelManager.self.loadingScreen != null) {
            PanelManager.self.loadingScreen.show();
         }

         if (LocationBanner.self != null) {
            LocationBanner.self.hide();
         }
      }

      // Make sure the server saves our position and health when a player is disconnected (by any means other than a warp)
      if (MyNetworkManager.wasServerStarted && !isAboutToWarpOnServer && AreaManager.self.getArea(this.areaKey) != null) {
         Util.tryToRunInServerBackground(() => DB_Main.setNewLocalPosition(this.userId, localPos, this.facing, this.areaKey));
      }
   }

   public virtual void resetCombatInit () {
   }

   public virtual void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, Item hat,
      ShipInfo shipInfo, GuildInfo guildInfo) {
      this.entityName = userInfo.username;
      this.adminFlag = userInfo.adminFlag;
      this.guildId = userInfo.guildId;
      this.customFarmBaseId = userInfo.customFarmBaseId;
      this.customHouseBaseId = userInfo.customHouseBaseId;

      // Body
      this.gender = userInfo.gender;
      this.hairPalette1 = userInfo.hairPalette1;
      this.hairPalette2 = userInfo.hairPalette2;
      this.hairType = userInfo.hairType;
      this.eyesType = userInfo.eyesType;
      this.eyesPalette1 = userInfo.eyesPalette1;
      this.bodyType = userInfo.bodyType;
   }

   public bool isMale () {
      return gender == Gender.Type.Male;
   }

   public bool isDead () {
      return currentHealth <= 0;
   }

   public bool isAdmin () {
      return (adminFlag == (int) AdminManager.Type.Admin);
   }

   public bool isFalling () {
      return fallDirection != 0;
   }

   public bool isInBattle () {
      return battleId > 0;
   }

   public void requestAnimationPlay (Anim.Type animType) {
      if (interactingAnimation) {
         return;
      }

      interactingAnimation = true;
      foreach (Animator animator in _animators) {
         switch (animType) {
            case Anim.Type.Interact_East:
            case Anim.Type.Interact_North:
            case Anim.Type.Interact_South:
               animator.SetBool("interact", true);
               StartCoroutine(CO_DelayExitAnim(animType, 0.4f));
               break;
            case Anim.Type.NC_Jump_East:
            case Anim.Type.NC_Jump_North:
            case Anim.Type.NC_Jump_South:
               animator.SetBool("jump", true); 
               isJumping = true;
               StartCoroutine(CO_DelayExitAnim(animType, 0.4f));
               break;
         }
      }
   }

   IEnumerator CO_DelayExitAnim (Anim.Type animType, float delay) {
      yield return new WaitForSeconds(delay);
      foreach (Animator animator in _animators) {
         switch (animType) {
            case Anim.Type.Interact_East:
            case Anim.Type.Interact_North:
            case Anim.Type.Interact_South:
               animator.SetBool("interact", false);
               break;
            case Anim.Type.NC_Jump_East:
            case Anim.Type.NC_Jump_North:
            case Anim.Type.NC_Jump_South:
               isJumping = false;
               animator.SetBool("jump", false);
               break;
         }
      }
      interactingAnimation = false;
   }

   public virtual float getMoveSpeed () {
      // Figure out our base movement speed
      float baseSpeed = (this is SeaEntity) ? 70f : 135f;

      // Check if we need to apply a slow modifier
      float modifier = 1.0f;
      if (StatusManager.self.hasStatus(this.netId, Status.Type.Freeze)) {
         modifier = 0f;
      } else if (StatusManager.self.hasStatus(this.netId, Status.Type.Slow)) {
         modifier = .5f;
      } else if (_isClimbing) {
         if (Time.time - _lastBodySpriteChangetime <= .2f) {
            modifier = 0;
         } else {
            modifier = .5f;
         }
      }

      // Admin move speed overrides all modifiers
      // Debug speed boost for Admin users only
      int moveSpeedModifier = 1;
      if (Input.GetKey(KeyCode.LeftShift) && isAdmin() && this is PlayerShipEntity && shipSpeedupFlag) {
         moveSpeedModifier = 2;
      }

      float perkMultiplier = PerkManager.self.getPerkMultiplier(Perk.Category.WalkingSpeed);
      return (baseSpeed * modifier) * moveSpeedModifier * perkMultiplier;
   }

   public virtual void handleSpriteOutline () {
      if (_outline == null || Global.player == null) {
         return;
      }

      _outline.enabled = false;

      if (Global.player == this) {
         _outline.setNewColor(Color.white);
         _outline.enabled = (isMouseOver() || isAttackCursorOver()) && !isDead();
         _outline.setVisibility(_outline.enabled);
         return;
      }

      if (Global.player.instanceId != this.instanceId) {
         return;
      }

      if (isAttackCursorOver()) {
         // If the attack cursor is over us, draw a yellow outline
         _outline.setNewColor(Color.yellow);
         _outline.enabled = true;
         _outline.setVisibility(true);
      } else if (isEnemyOf(Global.player)) {
         // Draw a red outline around enemies of the Player
         _outline.setNewColor(Color.red);
         _outline.enabled = true;
         _outline.setVisibility(true);
      } else if (isAllyOf(Global.player)) {
         // Draw a green outline around allies of the Player
         _outline.setNewColor(Color.green);
         _outline.enabled = true;
         _outline.setVisibility(true);
      } else if (hasAttackers()) {
         // If we've been attacked by someone, we get an orange outline
         _outline.setNewColor(Util.getColor(255, 187, 51));
         _outline.enabled = true;
         _outline.setVisibility(true);
      } else {
         // Only show our outline when the mouse is over us
         Color color = this is Enemy ? Color.red : Color.white;
         _outline.setNewColor(color);
         _outline.enabled = isMouseOver() && !isDead();
         _outline.setVisibility(_outline.enabled);
      }
   }

   public virtual float getTurnDelay () {
      return .25f;
   }

   public virtual float getAngleDelay () {
      return .10f;
   }

   public Rigidbody2D getRigidbody () {
      return _body;
   }

   public List<SpriteRenderer> getRenderers () {
      return _renderers;
   }

   public SpriteRenderer getBodyRenderer () {
      foreach (SpriteRenderer renderer in _renderers) {
         if (renderer.name == "Body") {
            return renderer;
         }
      }

      return null;
   }

   public virtual Armor getArmorCharacteristics () {
      return new Armor(0, 0);
   }

   public virtual Weapon getWeaponCharacteristics () {
      return new Weapon(0, 0);
   }

   public virtual Hat getHatCharacteristics () {
      return new Hat(0, 0);
   }

   public Instance getInstance () {
      // Check if the last requested instance is the one where we are located
      if (_lastInstance != null && _lastInstance.id == instanceId) {
         return _lastInstance;
      } else {
         // Look for the instance in the InstanceManager childs
         foreach (Instance instance in InstanceManager.self.GetComponentsInChildren<Instance>()) {
            if (instance.id == instanceId) {
               // Store the instance for subsequent calls
               _lastInstance = instance;
               return instance;
            }
         }
      }

      return null;
   }

   public void updatePlayerCamera () {
      // Only do this for our own player, and never 
      if (!this.isLocalPlayer) {
         return;
      }

      Area area = AreaManager.self.getArea(this.areaKey);
      if (area != null) {
         CinemachineVirtualCamera vcam = area.vcam;
         Util.activateVirtualCamera(vcam);
         vcam.Follow = this.transform;
      }
   }

   protected void requestServerTime () {
      Cmd_RequestServerDateTime();
   }

   protected void cleanAttackers () {
      List<uint> oldAttackers = new List<uint>();

      // Take note of all the attackers that must be removed
      foreach (KeyValuePair<uint, float> KV in _attackers) {
         NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(KV.Key);
         if (entity == null || entity.isDead() || TimeManager.self.getSyncedTime() - KV.Value > ATTACKER_STATUS_DURATION) {
            oldAttackers.Add(KV.Key);
         }
      }

      // Remove the old attackers
      foreach (uint attackerId in oldAttackers) {
         _attackers.Remove(attackerId);
      }
   }

   public bool isMoving () {
      // The velocity is handled differently for locally controlled and remotely controlled entities
      return getVelocity().magnitude > .01f;
   }

   public void setClimbing (bool isClimbing) {
      _isClimbing = isClimbing;
      getBodyRenderer().material.SetFloat("_Cutoff", _isClimbing ? HIDDEN_SHADOW_ALPHACUTOFF : VISIBLE_SHADOW_ALPHACUTOFF);
   }

   public bool isMouseOver () {
      return MouseManager.self.isHoveringOver(_clickableBox);
   }

   public bool isAttackCursorOver () {
      return AttackManager.self.isHoveringOver(this);
   }

   public bool hasAttackers () {
      return _attackers.Count > 0;
   }

   public virtual bool hasAnyCombat () {
      return hasAttackers();
   }

   public bool hasBeenAttackedBy (NetEntity otherEntity) {
      if (otherEntity == null) {
         return false;
      }

      return hasAttackers() && _attackers.ContainsKey(otherEntity.netId);
   }

   public bool isEnemyOf (NetEntity otherEntity) {
      if (otherEntity == null || otherEntity.isDead()) {
         return false;
      }

      if (otherEntity == this) {
         return false;
      }

      if (VoyageManager.isInVoyage(this) && VoyageManager.isInVoyage(otherEntity)) {
         if (this.voyageGroupId == otherEntity.voyageGroupId) {
            return false;
         } else {
            return true;
         }
      }

      if (hasBeenAttackedBy(otherEntity) || otherEntity.hasBeenAttackedBy(this)) {
         return true;
      }

      return false;
   }

   public bool isAllyOf (NetEntity otherEntity) {
      if (otherEntity == null || otherEntity.isDead()) {
         return false;
      }

      if (VoyageManager.isInVoyage(this) && VoyageManager.isInVoyage(otherEntity)) {
         if (this.voyageGroupId == otherEntity.voyageGroupId) {
            return true;
         } else {
            return false;
         }
      }

      return false;
   }

   public Vector2 getVelocity () {
      // The velocity is handled differently for locally controlled and remotely controlled entities
      if (_body.velocity.magnitude != 0f) {
         return _body.velocity;
      } else {
         return _smoothSync.latestReceivedVelocity;
      }
   }

   public virtual void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.transform, worldPositionStays);
   }

   protected void handleInstantMoveMode (bool updatingEveryFrame) {
      // Calculate by how much to reduce the movement speed due to differing update steps
      float frameRateMultiplier = updatingEveryFrame ? 1 / Mathf.Ceil(MOVE_CHANGE_INTERVAL / Time.deltaTime) : 1f;

      // Get a list of the directions we're allowed to move (sometimes includes diagonal directions)
      List<Direction> availableDirections = DirectionUtil.getAvailableDirections(true, _isClimbing);

      // Check if we're pressing the keys for any of the directions, and if so, add an appropriate force
      foreach (Direction direction in availableDirections) {
         if (DirectionUtil.isPressingDirection(direction)) {
            // Check if we need to update our facing direction SyncVar
            Direction newFacingDirection = DirectionUtil.getFacingDirection(hasDiagonals, direction);
            if (this.facing != newFacingDirection) {
               this.facing = newFacingDirection;

               // Tell the server to pass it along to all clients
               Cmd_UpdateFacing(newFacingDirection);
            }

            // Figure out the force vector we should apply
            Vector2 forceToApply = DirectionUtil.getVectorForDirection(direction);
            float baseMoveSpeed = getMoveSpeed();
            if (isSpeedingUp) {
               baseMoveSpeed *= SPEEDUP_MULTIPLIER_LAND;
            }

            _body.AddForce(forceToApply.normalized * baseMoveSpeed * frameRateMultiplier);

            // Make note of the time
            _lastMoveChangeTime = Time.time;

            break;
         }
      }
   }

   [Command]
   public void Cmd_ForceFaceDirection (Direction direction) {
      Rpc_ForceLookat(direction);
   }

   [ClientRpc]
   public void Rpc_ForceLookat (Direction direction) {
      this.facing = direction;

      foreach (Animator animator in _animators) {
         animator.SetInteger("facing", (int) this.facing);
      }
   }

   protected void handleDelayMoveMode () {
      // Check if enough time has passed for us to change our facing direction
      bool canChangeDirection = (Time.time - _lastFacingChangeTime > getTurnDelay());

      if (canChangeDirection) {
         if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
            Cmd_ModifyFacing(-1);
            _lastFacingChangeTime = Time.time;
         } else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
            Cmd_ModifyFacing(+1);
            _lastFacingChangeTime = Time.time;
         }
      }

      // Figure out the force vector we should apply
      if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
         Vector2 forceToApply = DirectionUtil.getVectorForDirection(this.facing);
         _body.AddForce(forceToApply.normalized * getMoveSpeed());

         // Make note of the time
         _lastMoveChangeTime = Time.time;
      }
   }

   protected virtual void handleArrowsMoveMode () {
      // Handled by the PlayerShipEntity class
   }

   protected virtual void updateMassAndDrag (bool increasedMass) {
      if (increasedMass) {
         _body.mass = 40f;
         _body.drag = 1.5f;
         _body.angularDrag = 0f;
      } else {
         _body.mass = 1f;
         _body.drag = 50f;
         _body.angularDrag = 0.05f;
      }
   }

   [ClientRpc]
   public void Rpc_WarpToArea (string areaKey, Vector3 newPosition) {
      this.transform.position = newPosition;
   }

   [TargetRpc]
   public void Target_ReceiveSiloInfo (NetworkConnection connection, SiloInfo[] siloInfo) {
      _siloInfo = new List<SiloInfo>(siloInfo);

      // Update our GUI display
      CargoBoxManager.self.updateCargoBoxes(_siloInfo);
   }

   [TargetRpc]
   public void Target_ReceiveTutorialInfo (NetworkConnection connection, TutorialInfo[] infoArray, bool justCompletedStep) {
      // Pass along the info to the Tutorial Manager
      TutorialManager.self.receivedTutorialInfo(new List<TutorialInfo>(infoArray), justCompletedStep);
   }

   [TargetRpc]
   protected void Target_ReceiveServerDateTime (NetworkConnection conn, float serverUnityTime, long serverDateTime) {
      TimeManager.self.setLastServerDateTime(serverDateTime);

      // Pass the server time and round trip time off to the Time Manager to keep track of
      TimeManager.self.setTimeOffset(serverUnityTime, (float) NetworkTime.rtt);
   }

   [TargetRpc]
   public void Target_GainedXP (NetworkConnection conn, int xpGained, Jobs jobs, Jobs.Type jobType, int cropNumber, bool showFloatingXp) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);

      // If it happened at a crop spot, show the XP gain there
      if (cropNumber > 0) {
         pos = CropSpotManager.self.getCropSpot(cropNumber, areaKey).transform.position + new Vector3(0f, .32f);
      }

      if (showFloatingXp) {
         // Show a message that they gained some XP
         GameObject xpCanvas = Instantiate(PrefabsManager.self.xpGainPrefab);
         xpCanvas.transform.position = pos;
         xpCanvas.GetComponentInChildren<Text>().text = "+" + xpGained + " " + jobType + " XP";
      }

      // Show some types of gain in chat
      if (jobType == Jobs.Type.Trader || jobType == Jobs.Type.Miner || jobType == Jobs.Type.Crafter) {
         string message = string.Format("You gained {0} {1} XP!", xpGained, jobType);
         ChatManager.self.addChat(message, ChatInfo.Type.System);
      }

      // Figure out what the old and new XP is for this job type
      int newXP = jobs.getXP(jobType);
      int oldXP = newXP - xpGained;
      int levelsGained = LevelUtil.levelsGained(oldXP, newXP);

      // If they gained a level, show a special message
      if (levelsGained > 0) {
         GameObject levelUpCanvas = Instantiate(PrefabsManager.self.levelGainPrefab);
         levelUpCanvas.transform.position = this.transform.position;
         string message = string.Format("+{0} {1} level{2}!", levelsGained, jobType, levelsGained > 1 ? "s" : "");
         levelUpCanvas.GetComponentInChildren<Text>().text = message;

         // Show an effect
         GameObject sortPoint = GetComponent<ZSnap>().sortPoint;
         GameObject sparkles = EffectManager.show(Effect.Type.Item_Discovery_Particles, sortPoint.transform.position + new Vector3(0f, .24f));
         sparkles.transform.SetParent(this.transform, true);
         Destroy(sparkles, 5f);

         // Play a sound
         SoundManager.create3dSound("tutorial_step", Global.player.transform.position);

         // Registers the achievement of leveling up for recording
         AchievementManager.registerUserAchievement(userId, ActionType.LevelUp);

         // Show the level up in chat
         string levelsMsg = string.Format("You gained {0} {1} {2}!", levelsGained, jobType, levelsGained > 1 ? "levels" : "level");
         ChatManager.self.addChat(levelsMsg, ChatInfo.Type.System);
      }
   }

   [TargetRpc]
   public void Target_ReceiveGlobalChat (int chatId, string message, long timestamp, string senderName, int senderUserId) {
      ChatInfo chatInfo = new ChatInfo(chatId, message, System.DateTime.FromBinary(timestamp), ChatInfo.Type.Global, senderName, senderUserId);

      // Add it to the Chat Manager
      ChatManager.self.addChatInfo(chatInfo);
   }

   [ClientRpc]
   public void Rpc_ChatWasSent (int chatId, string message, long timestamp, ChatInfo.Type chatType) {
      ChatInfo chatInfo = new ChatInfo(chatId, message, System.DateTime.FromBinary(timestamp), chatType, entityName, userId);

      // Add it to the Chat Manager
      ChatManager.self.addChatInfo(chatInfo);
   }

   [TargetRpc]
   public void Target_ReceiveSpecialChat (NetworkConnection conn, int chatId, string message, string senderName, long timestamp, ChatInfo.Type chatType) {
      ChatInfo chatInfo = new ChatInfo(chatId, message, System.DateTime.FromBinary(timestamp), chatType, senderName, userId);

      // Add it to the Chat Manager
      ChatManager.self.addChatInfo(chatInfo);
   }

   [TargetRpc]
   public void Target_ReceiveVoyageGroupInvitation (NetworkConnection conn, int voyageGroupId, string inviterName) {
      VoyageManager.self.receiveVoyageInvitation(voyageGroupId, inviterName);
   }

   [TargetRpc]
   public void Target_GainedItem (NetworkConnection conn, string itemIconPath, string itemName, Jobs.Type jobType, float jobExp, int itemCount) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);

      // Show a message that they gained some XP along with the item they received
      GameObject gainItemCanvas = Instantiate(PrefabsManager.self.itemReceivedPrefab);
      gainItemCanvas.transform.position = pos;
      if (jobType != Jobs.Type.None) {
         gainItemCanvas.GetComponentInChildren<Text>().text = "+ " + itemCount + " " + itemName + "\n" + "+ " + jobExp + " " + jobType + " XP";
      } else {
         gainItemCanvas.GetComponentInChildren<Text>().text = "+ " + itemCount + " " + itemName;
      }
      gainItemCanvas.GetComponentInChildren<Image>().sprite = ImageManager.getSprite(itemIconPath);
   }

   [TargetRpc]
   public void Target_FloatingMessage (NetworkConnection conn, string message) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);
      GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
      messageCanvas.transform.position = pos;
      messageCanvas.GetComponentInChildren<Text>().text = message;
   }

   [Command]
   public void Cmd_PlantCrop (Crop.Type cropType, int cropNumber, string areaKey) {
      // We have to holding the seed bag
      BodyEntity body = GetComponent<BodyEntity>();

      if (body == null || body.weaponManager.actionType != Weapon.ActionType.PlantCrop) {
         D.warning("Can't plant without seeds equipped!");
         return;
      }
      
      this.cropManager.plantCrop(cropType, cropNumber, areaKey);
   }

   [Command]
   public void Cmd_WaterCrop (int cropNumber) {
      // We have to holding the watering pot
      BodyEntity body = GetComponent<BodyEntity>();

      if (body == null || body.weaponManager.actionType != Weapon.ActionType.WaterCrop) {
         D.warning("Can't water without a watering pot!");
         return;
      }

      this.cropManager.waterCrop(cropNumber);
   }

   [Command]
   public void Cmd_HarvestCrop (int cropNumber) {
      // We have to holding the pitchfork
      BodyEntity body = GetComponent<BodyEntity>();

      if (body == null || body.weaponManager.actionType != Weapon.ActionType.HarvestCrop) {
         D.warning("Can't harvest without a pitchfork!");
         return;
      }

      this.cropManager.harvestCrop(cropNumber);
   }

   [Command]
   protected void Cmd_RequestServerDateTime () {
      // Send the client our current server DateTime
      if (isServer) {
         Target_ReceiveServerDateTime(this.connectionToClient, Time.time, System.DateTime.UtcNow.ToBinary());
      }
   }

   [Command]
   public void Cmd_CompletedTutorialStep (int stepIndex) {
      // We handle the logic in a non-Cmd function, so that the code can be called from other places
      completedTutorialStep(stepIndex);
   }

   [Command]
   public void Cmd_UpdateFacing (Direction newFacing) {
      this.facing = newFacing;
   }

   [Command]
   public void Cmd_ModifyFacing (int modifier) {
      // Check which way we're currently facing
      int currentFacing = (int) this.facing;

      // Apply the specified modifier
      currentFacing += modifier;

      // Make sure we stay inside the allowed directions
      if (currentFacing > 8) {
         currentFacing -= 8;
      } else if (currentFacing < 1) {
         currentFacing += 8;
      }

      this.facing = (Direction) currentFacing;
   }

   [Command]
   public void Cmd_ChangeMass (bool increasedMass) {
      updateMassAndDrag(increasedMass);

      // Update the desired angle Sync Var based on our current facing direction
      if (this is PlayerShipEntity) {
         PlayerShipEntity ship = (PlayerShipEntity) this;
         ship.desiredAngle = DirectionUtil.getAngle(this.facing);
      }
   }

   [Command]
   public void Cmd_GoHome () {
      // Don't allow users to go home if they are in PVP.
      if (hasAttackers()) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, this, "Cannot return to home location while in combat.");
         return;
      }

      spawnInNewMap(Area.STARTING_TOWN);
   }

   [Command]
   public void Cmd_SpawnInNewMap (string areaKey) {
      spawnInNewMap(areaKey);
   }

   [Command]
   public void Cmd_SpawnInNewMapSpawn (string newArea, string spawn, Direction newFacingDirection) {
      spawnInNewMap(newArea, spawn, newFacingDirection);
   }

   [Server]
   public void spawnInNewMap (string areaKey) {
      spawnInNewMap(areaKey, null, Direction.South);
   }

   [Server]
   public void spawnInNewMap (string newArea, string spawn, Direction newFacingDirection) {
      // Local position in the target map that we should be arriving in
      Vector2 targetLocalPos = Vector2.zero;

      // Check if this is a custom map
      if (AreaManager.self.tryGetCustomMapManager(newArea, out CustomMapManager customMapManager)) {
         // Check if user is not able to warp into owned area
         if (!customMapManager.canUserWarpInto(this, newArea, out System.Action<NetEntity> denyWarphandler)) {
            // Deny access to owned area
            denyWarphandler?.Invoke(this);
            return;
         }

         // Make a user-specific area key for this user, if it is not a user-specific key already
         if (!CustomMapManager.isUserSpecificAreaKey(newArea)) {
            newArea = customMapManager.getUserSpecificAreaKey(userId);
         }

         // Get the owner of the target map
         int targetUserId = CustomMapManager.getUserId(newArea);
         NetEntity owner = EntityManager.self.getEntity(targetUserId);

         // Get the base map
         string baseMapKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(owner));

         targetLocalPos = spawn == null
            ? SpawnManager.self.getDefaultLocalPosition(baseMapKey)
            : SpawnManager.self.getLocalPosition(baseMapKey, spawn);
      } else {
         targetLocalPos = spawn == null
            ? SpawnManager.self.getDefaultLocalPosition(newArea)
            : SpawnManager.self.getLocalPosition(newArea, spawn);
      }

      spawnInNewMap(newArea, targetLocalPos, newFacingDirection);
   }

   [Server]
   public void spawnInNewMap (string newArea, Vector2 newLocalPosition, Direction newFacingDirection) {
      // Check which server we're likely to redirect to
      Server bestServer = ServerNetwork.self.findBestServerForConnectingPlayer(newArea, this.entityName, this.userId, this.connectionToClient.address, isSinglePlayer, -1);

      // Now that we know the target server, redirect them there
      spawnOnSpecificServer(bestServer, newArea, newLocalPosition, newFacingDirection);

      SecretsManager.self.checkIfUserIsInSecret(this.userId);
   }

   [Server]
   public void spawnInNewMap (int voyageId, string newArea, Direction newFacingDirection) {
      // Find the server hosting the voyage
      Server voyageServer = ServerNetwork.self.getServerHostingVoyage(voyageId);

      // Get the default spawn of the area
      Vector2 spawnLocalPosition = SpawnManager.self.getDefaultLocalPosition(newArea);

      // Now that we know the target server, redirect them there
      spawnOnSpecificServer(voyageServer, newArea, spawnLocalPosition, newFacingDirection);
   }

   [Server]
   public void spawnOnSpecificServer (Server newServer, string newArea, Vector2 newLocalPosition, Direction newFacingDirection) {
      GameObject entityObject = this.gameObject;

      // Make a note that we're about to proceed with a warp
      this.isAboutToWarpOnServer = true;

      // Store the connection reference so that we don't lose it while on the background thread
      NetworkConnection connectionToClient = this.connectionToClient;

      // Update the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setNewLocalPosition(this.userId, newLocalPosition, newFacingDirection, newArea);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Remove the player from the current instance
            InstanceManager.self.removeEntityFromInstance(this);

            // Send a Redirect message to the client
            RedirectMessage redirectMessage = new RedirectMessage(this.netId, MyNetworkManager.self.networkAddress, newServer.port);
            this.connectionToClient.Send(redirectMessage);

            // Destroy the old Player object
            NetworkServer.DestroyPlayerForConnection(connectionToClient);
            NetworkServer.Destroy(entityObject);
         });
      });
   }

   [Server]
   public void completedTutorialStep (int stepIndex) {
      // Make sure we don't process the same tutorial step multiple times
      if (_processedTutorialSteps.ContainsKey(stepIndex)) {
         return;
      }
      _processedTutorialSteps[stepIndex] = true;

      // Database thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         TutorialData completedStep = DB_Main.completeTutorialStep(this.userId, stepIndex);

         // If they completed certain steps, they get items
         if (completedStep.requirementType == RequirementType.Item) {
            Item item = JsonUtility.FromJson<Item>(completedStep.rawDataJson);

            if (item.category == Item.Category.Weapon) {
               DB_Main.insertNewWeapon(this.userId, item.itemTypeId, PaletteDef.WHITE_WEAPON_COMPLETED_TUTORIAL_STEP, PaletteDef.WHITE_WEAPON_COMPLETED_TUTORIAL_STEP);
            }
         }
         if (completedStep.actionType == ActionType.HarvestCrop) {
            ShipInfo shipInfo = DB_Main.createStartingShip(userId);
            DB_Main.setCurrentShip(userId, shipInfo.shipId);
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the info to the player
            if (completedStep.stepOrder > 0) {
               TutorialManager.self.sendTutorialInfo(this, true);
            }
         });
      });
   }

   protected IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(this.areaKey) == null) {
         yield return 0;
      }

      Area area = AreaManager.self.getArea(this.areaKey);
      bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2)transform.position);
      setAreaParent(area, worldPositionStays);

      if (isLocalPlayer) {
         // Wait until the loading screen is hidden
         while (PanelManager.self.loadingScreen.isShowing()) {
            yield return null;
         }

         // Set the music according to our Area
         SoundManager.setBackgroundMusic(this.areaKey);

         // Show the Area name
         LocationBanner.self.setText(Area.getName(this.areaKey));

         // Now that the area is fully loaded client-side, verify if the voyage (if any) exists
         if (VoyageManager.isInVoyage(this)) {
            rpc.Cmd_VerifyVoyageConsistencyAtSpawn();
         }
      }

      // Wait until the position is stabilized by SmoothSync, then disable distance threshold to save a distance calculation
      yield return new WaitForSeconds(0.5f);
      if (_smoothSync != null) {
         _smoothSync.snapPositionThreshold = 0;
      }
   }

   protected virtual void onStartMoving () { }
   protected virtual void onEndMoving () { }

   public virtual bool isBotShip () { return false; }

   #region Private Variables

   // Whether we should automatically move around
   protected bool _autoMove = false;

   // The key of the previous area that we were in
   protected string _previousAreaKey;

   // Info on the crops in our silo
   protected List<SiloInfo> _siloInfo = new List<SiloInfo>();

   // Our various component references
   protected Rigidbody2D _body;
   protected ClickableBox _clickableBox;
   protected SpriteOutline _outline;
   protected SmoothSyncMirror _smoothSync;
   protected List<Animator> _animators = new List<Animator>();
   protected List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

   // The time at which we last applied a change to our movement
   protected float _lastMoveChangeTime;

   // The time at which we last changed our facing direction
   protected float _lastFacingChangeTime;

   // The time at which we last changed our movement angle
   protected float _lastAngleChangeTime;

   // The nameText that follows us around
   protected Text _nameText;

   // Entities that have attacked us and the time when they attacked
   protected Dictionary<uint, float> _attackers = new Dictionary<uint, float>();

   // Used by the server to keep track of which tutorial steps have already been processed
   protected Dictionary<int, bool> _processedTutorialSteps = new Dictionary<int, bool>();

   // Did the Entity move last frame?
   private bool _movedLastFrame;

   // The sprite used for the body in the previous frame
   private Sprite _previousBodySprite;

   // The time at which the body last changed sprites
   private float _lastBodySpriteChangetime;

   // Wether the player is climbing
   private bool _isClimbing = false;

   // Keep a reference to the last instance accessed with getInstance
   private Instance _lastInstance = null;

   // The alpha cutoff value when we want to show the shadow
   private const float VISIBLE_SHADOW_ALPHACUTOFF = 0;

   // The alpha cutoff value when we want to hide the shadow
   private const float HIDDEN_SHADOW_ALPHACUTOFF = 0.5f;

   #endregion
}
