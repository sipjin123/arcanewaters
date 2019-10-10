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

   // The account ID for this entity
   [SyncVar]
   public int accountId;

   // The user ID for this entity
   [SyncVar]
   public int userId;

   // The id of the Instance that this entity is in
   [SyncVar]
   public int instanceId;

   // The Area type that we're in
   [SyncVar]
   public Area.Type areaType;

   // The Gender of this entity
   [SyncVar]
   public Gender.Type gender = Gender.Type.Male;

   // The Name of this entity
   [SyncVar]
   public string entityName;

   // The nation of this Entity
   [SyncVar]
   public Nation.Type nationType;

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

   // Whether or not this Entity has sprites for diagonal directions
   public bool hasDiagonals;

   // The direction we're falling, if any
   public int fallDirection = 0;

   // The admin flag for this entity, only valid on the server
   public int adminFlag;

   // The Class that this player has chosen
   public Class.Type classType;

   // The Specialty that this player has chosen
   public Specialty.Type specialty;

   // The Faction that this player has chosen
   public Faction.Type faction;

   // The guild this user is in
   [SyncVar]
   public int guildId;

   // The ID of the Battle this Enemy is currently in, if any
   [SyncVar]
   public int battleId;

   // Gets set to true on the server when we're about to execute a warp
   public bool isAboutToWarpOnServer = false;

   // Determines if the player is animating an interact clip
   public bool interactingAnimation = false;

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
      if (this is PlayerBodyEntity || this is PlayerShipEntity) {
         // Create some name text that will follow us around
         SmoothFollow smoothFollow = Instantiate(PrefabsManager.self.nameTextPrefab);
         smoothFollow.followTarget = this.gameObject;
         _nameText = smoothFollow.GetComponentInChildren<Text>();
         _nameText.text = this.entityName;

         // We need the follow text to be lower for ships
         _nameText.GetComponent<RectTransform>().offsetMin = (this is PlayerShipEntity) ? new Vector2(0, 32) : new Vector2(0, 64);
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

         // Set the music according to our Area
         SoundManager.setBackgroundMusic(this.areaType);

         // Show the Area name
         LocationBanner.self.setText(Area.getName(this.areaType));

         // Have Gramps start telling us what to do
         GrampsManager.startTalkingToPlayerAfterDelay();

         // Routinely compare our Time to the Server's time
         if (!isServer) {
            InvokeRepeating("requestServerTime", 0f, 1f);
         }
      }
   }

   protected virtual void Update () {
      // Clients in standalone don't receive the entity name until a little after instantiation
      if (_nameText != null) {
         _nameText.text = this.entityName;
      }

      if (!interactingAnimation) {
         // Pass our angle and rigidbody velocity on to the Animator
         foreach (Animator animator in _animators) {
            animator.SetFloat("velocityX", _body.velocity.x);
            animator.SetFloat("velocityY", _body.velocity.y);
            animator.SetBool("isMoving", isMoving());
            animator.SetInteger("facing", (int) this.facing);
            animator.SetBool("inBattle", isInBattle());

            if (this is BodyEntity) {
               animator.SetInteger("fallDirection", (int) this.fallDirection);
            }
         }
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
      if (this.areaType != _previousAreaType) {
         updatePlayerCamera();
      }

      // Handle the drawing or hiding of our outline
      handleSpriteOutline();

      // Keep track of our previous area type
      _previousAreaType = this.areaType;
   }

   protected virtual void FixedUpdate () {
      // We can only control movement for our own player, when chat isn't focused, and we're not falling down or dead
      if (!isLocalPlayer || ChatPanel.self.inputField.isFocused || isFalling() || isDead() || PanelManager.self.hasPanelInStack()) {
         return;
      }

      // Only change our movement if enough time has passed
      if (Time.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      // Check if we need to use the alternate delayed movement mode
      if (this is SeaEntity && SeaManager.moveMode == SeaManager.MoveMode.Delay) {
         handleDelayMoveMode();
      } else if (this is SeaEntity && SeaManager.moveMode == SeaManager.MoveMode.Arrows) {
         handleArrowsMoveMode();
      } else {
         handleInstantMoveMode();
      }
   }

   protected virtual void OnDestroy () {
      Vector3 pos = this.transform.position;

      if (isLocalPlayer && !TitleScreen.self.isShowing() && Application.platform != RuntimePlatform.OSXPlayer) {
         CircleFader.self.doCircleFade();
      }

      // Make sure the server saves our position and health when a player is disconnected (by any means other than a warp)
      if (MyNetworkManager.wasServerStarted && !isAboutToWarpOnServer) {
         Util.tryToRunInServerBackground(() => DB_Main.setNewPosition(this.userId, pos, this.facing, (int) this.areaType));
      }
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
            case Anim.Type.Mining:
               animator.SetBool("mining", true);
               break;
         }
      }
      StartCoroutine(CO_DelayExitAnim(animType));
   }

   IEnumerator CO_DelayExitAnim(Anim.Type animType) {
      yield return new WaitForSeconds(.2f);
      foreach (Animator animator in _animators) {
         switch (animType) {
            case Anim.Type.Mining:
               animator.SetBool("mining", false);
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
      if (StatusManager.self.hasStatus(this.userId, Status.Type.Freeze)) {
         modifier = 0f;
      } else if (StatusManager.self.hasStatus(this.userId, Status.Type.Slow)) {
         modifier = .5f;
      }

      return baseSpeed * modifier;
   }

   public virtual void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Draw a red outline around enemies of the Player
      if (isEnemyOf(Global.player)) {
         _outline.setNewColor(Color.red);
         _outline.setVisibility(true);
      } else if (hasAttackers()) {
         // If we've been attacked by someone, we get an orange outline
         _outline.setNewColor(Util.getColor(255, 187, 51));
         _outline.setVisibility(true);
      } else {
         // Only show our outline when the mouse is over us
         Color color = this is Enemy ? Color.red : Color.white; ;
         _outline.setNewColor(color);
         _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox) && !isDead());
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

   protected void updatePlayerCamera () {
      // Only do this for our own player, and never 
      if (!this.isLocalPlayer) {
         return;
      }

      Area area = AreaManager.self.getArea(this.areaType);
      CinemachineVirtualCamera vcam = area.vcam;
      Util.activateVirtualCamera(vcam);
      vcam.Follow = this.transform;
   }

   protected void requestServerTime () {
      Cmd_RequestServerDateTime();
   }

   public bool isMoving () {
      // The velocity is handled differently for locally controlled and remotely controlled entities
      return getVelocity().magnitude > .01f;
   }

   public bool hasAttackers () {
      return _attackers.Count > 0;
   }

   public bool hasBeenAttackedBy (NetEntity otherEntity) {
      if (otherEntity == null) {
         return false;
      }

      return _attackers.Contains(otherEntity);
   }

   public bool isEnemyOf (NetEntity otherEntity) {
      if (otherEntity == null || otherEntity.isDead()) {
         return false;
      }

      if (otherEntity == this) {
         return false;
      }

      if (hasBeenAttackedBy(otherEntity) || otherEntity.hasBeenAttackedBy(this)) {
         return true;
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

   protected void handleInstantMoveMode () {
      // Get a list of the directions we're allowed to move (sometimes includes diagonal directions)
      List<Direction> availableDirections = DirectionUtil.getAvailableDirections(true);

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
            _body.AddForce(forceToApply.normalized * getMoveSpeed());

            // Make note of the time
            _lastMoveChangeTime = Time.time;

            break;
         }
      }
   }

   [Command]
   public void Cmd_ForceFaceDirection(Direction direction) {
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
   public void Rpc_WarpToArea (Area.Type areaType, Vector3 newPosition) {
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
   public void Target_GainedXP (NetworkConnection conn, int xpGained, Jobs jobs, Jobs.Type jobType, int cropNumber) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);

      // If it happened at a crop spot, show the XP gain there
      if (cropNumber > 0) {
         pos = CropSpotManager.self.getCropSpot(cropNumber).transform.position + new Vector3(0f, .32f);
      }

      // Show a message that they gained some XP
      GameObject xpCanvas = Instantiate(PrefabsManager.self.xpGainPrefab);
      xpCanvas.transform.position = pos;
      xpCanvas.GetComponentInChildren<Text>().text = "+" + xpGained + " " + jobType + " XP";

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

   [Command]
   public void Cmd_PlantCrop (Crop.Type cropType, int cropNumber) {
      // We have to holding the seed bag
      BodyEntity body = GetComponent<BodyEntity>();

      if (body == null || body.weaponManager.weaponType != Weapon.Type.Seeds) {
         D.warning("Can't plant without seeds equipped!");
         return;
      }

      this.cropManager.plantCrop(cropType, cropNumber);
   }

   [Command]
   public void Cmd_WaterCrop (int cropNumber) {
      // We have to holding the watering pot
      BodyEntity body = GetComponent<BodyEntity>();

      if (body == null || body.weaponManager.weaponType != Weapon.Type.WateringPot) {
         D.warning("Can't water without a watering pot!");
         return;
      }

      this.cropManager.waterCrop(cropNumber);
   }

   [Command]
   public void Cmd_HarvestCrop (int cropNumber) {
      // We have to holding the pitchfork
      BodyEntity body = GetComponent<BodyEntity>();

      if (body == null || body.weaponManager.weaponType != Weapon.Type.Pitchfork) {
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
   public void Cmd_CompletedTutorialStep (Step step) {
      // We handle the logic in a non-Cmd function, so that the code can be called from other places
      completedTutorialStep(step);
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
   public void Cmd_SpawnIntoGeneratedMap (MapSummary mapSummary) {
      // Look up the Spawn object and the server that is associated with this map data
      Area area = AreaManager.self.getArea(mapSummary.areaType);
      Spawn spawn = area.GetComponentInChildren<Spawn>();
      Server server = ServerNetwork.self.getServer(mapSummary.serverAddress, mapSummary.serverPort);

      // Proceed to spawn onto the requested server
      spawnOnSpecificServer(server, mapSummary.areaType, spawn, Direction.North);
   }

   [Command]
   public void Cmd_SpawnInNewMap (Area.Type newArea, Spawn.Type spawnType, Direction newFacingDirection) {
      Spawn spawn = SpawnManager.self.getSpawn(spawnType);

      spawnInNewMap(newArea, spawn, newFacingDirection);
   }

   [Server]
   public void spawnInNewMap (Area.Type newArea, Spawn spawn, Direction newFacingDirection) {
      // Check which server we're likely to redirect to
      Server bestServer = ServerNetwork.self.findBestServerForConnectingPlayer(newArea, this.entityName, this.userId, this.connectionToClient.address);

      // Now that we know the target server, redirect them there
      spawnOnSpecificServer(bestServer, newArea, spawn, newFacingDirection);
   }

   [Server]
   public void spawnOnSpecificServer (Server newServer, Area.Type newArea, Spawn spawn, Direction newFacingDirection) {
      int connectionId = this.connectionToClient.connectionId;
      Vector2 newPosition = spawn.getSpawnPosition();

      // Make a note that we're about to proceed with a warp
      this.isAboutToWarpOnServer = true;

      // Store the connection reference so that we don't lose it while on the background thread
      NetworkConnection connectionToClient = this.connectionToClient;

      // Update the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setNewPosition(this.userId, newPosition, newFacingDirection, (int) newArea);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Remove the player from the current instance
            InstanceManager.self.removeEntityFromInstance(this);

            // Destroy the old Player object
            NetworkServer.DestroyPlayerForConnection(connectionToClient);
            NetworkServer.Destroy(this.gameObject);

            // Send a Redirect message to the client
            RedirectMessage redirectMessage = new RedirectMessage(this.netId, newServer.ipAddress, newServer.port);
            NetworkServer.SendToClient(connectionId, redirectMessage);
         });
      });
   }

   [Server]
   public void completedTutorialStep (Step step) {
      // Make sure we don't process the same tutorial step multiple times
      if (_processedTutorialSteps.ContainsKey(step)) {
         return;
      }
      _processedTutorialSteps[step] = true;

      // Database thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Step completedStep = DB_Main.completeTutorialStep(this.userId, step);

         // If they completed certain steps, they get items
         if (completedStep == Step.FindSeedBag) {
            DB_Main.insertNewWeapon(this.userId, Weapon.Type.Seeds, ColorType.White, ColorType.White);
         } else if (completedStep == Step.GetWateringCan) {
            DB_Main.insertNewWeapon(this.userId, Weapon.Type.WateringPot, ColorType.White, ColorType.White);
         } else if (completedStep == Step.GetPitchfork) {
            DB_Main.insertNewWeapon(this.userId, Weapon.Type.Pitchfork, ColorType.White, ColorType.White);
         } else if (completedStep == Step.HarvestCrops) {
            ShipInfo shipInfo = DB_Main.createStartingShip(userId);
            DB_Main.setCurrentShip(userId, shipInfo.shipId);
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the info to the player
            if (completedStep > 0) {
               TutorialManager.self.sendTutorialInfo(this, true);
            }
         });
      });
   }

   #region Private Variables

   // Whether we should automatically move around
   protected bool _autoMove = false;

   // The previous type of area that we were in
   protected Area.Type _previousAreaType;

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

   // Entities that have attacked us
   protected HashSet<NetEntity> _attackers = new HashSet<NetEntity>();

   // Used by the server to keep track of which tutorial steps have already been processed
   protected Dictionary<Step, bool> _processedTutorialSteps = new Dictionary<Step, bool>();

   #endregion
}
