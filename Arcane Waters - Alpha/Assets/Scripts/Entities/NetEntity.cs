using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;
using TMPro;
using System.Linq;
using System;
using UnityEngine.InputSystem;

public class NetEntity : NetworkBehaviour
{
   #region Public Variables

   [Header("UserData")]

   // If this entity is participating in pvp
   [SyncVar]
   public bool enablePvp;

   // If this unit is in god mode
   [SyncVar]
   public bool isGodMode = false;

   // The account ID for this entity
   [SyncVar]
   public int accountId;

   // The steam ID for this entity
   [SyncVar]
   public string steamId = "";

   // The user ID for this entity
   [SyncVar]
   public int userId;

   // The id of the Instance that this entity is in
   [SyncVar]
   public int instanceId;

   // The key of the area we're in
   [SyncVar]
   public string areaKey;

   // Whether the admin user is invisible to other players and enemies
   [SyncVar]
   public bool isInvisible;

   // The Name of this entity
   [SyncVar]
   public string entityName;

   // Our current health
   [SyncVar]
   public int currentHealth = 1000;

   // Our max health
   [SyncVar(hook = "onMaxHealthChanged")]
   public int maxHealth = 1000;

   // The amount of XP we have, which we can use to show our level
   [SyncVar]
   public int XP;

   // Our desired angle of movement
   [SyncVar]
   public float desiredAngle;

   // Is this player stealth muted?
   [SyncVar]
   public bool isStealthMuted;

   // Determines the type of game mode this entity is in
   [SyncVar]
   public PvpGameMode openWorldGameMode = PvpGameMode.None;

   // The mute expiration date
   [SyncVar]
   public long muteExpirationDate;

   [Header("PlayerAppearance")]

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
   public string eyesPalettes;
   [SyncVar]
   public string hairPalettes;

   [Header("Components")]

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
   //public GroundChecker groundChecker;

   // Our Water Checker
   public WaterChecker waterChecker;

   // The Text component that has our name
   public TextMeshProUGUI nameText;

   // The Text component that has our name as outline
   public TextMeshProUGUI nameTextOutline;

   // The object we use for sorting our sprites
   public GameObject sortPoint;

   // Reference to the shadow
   public SpriteRenderer shadow;

   // Reference to the parent transform of status effect icons
   public Transform statusEffectContainer;

   // GameObject that holds the name of the entity
   public GameObject entityNameGO;

   [Header("Stats")]

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

   // The ID of the Battle this Enemy is currently in, if any
   [SyncVar]
   public int battleId;

   // The ID of the Voyage Group this user is currently in, if any
   [SyncVar]
   public int voyageGroupId = -1;

   // Gets set to true when the entity is invisible and untouchable
   [SyncVar]
   public bool isGhost = false;

   // Gets set to true when the player can be attacked by other players in PvP areas
   [SyncVar]
   public bool hasEnteredPvP = false;

   // Gets set to true when the player has unlocked the next biome, after the one of the current instance
   [SyncVar]
   public bool isNextBiomeUnlocked = false;

   // The house layout for this user, if chosen
   [SyncVar]
   public int customHouseBaseId;

   // The farm layout for this user, if chosen
   [SyncVar]
   public int customFarmBaseId;

   // If this unit is being controlled by another script
   public bool isUnderExternalControl;

   // Stores the last time this player talked to an NPC
   public float lastNPCTalkTime = -30.0f;

   // What pvp team this entity is affiliated with
   [SyncVar]
   public PvpTeamType pvpTeam = PvpTeamType.None;

   [SyncVar]
   public Faction.Type faction = Faction.Type.None;

   [Header("GuildInfo")]
   // The guild this user is in
   [SyncVar]
   public int guildId;

   // The current guild allies if any
   public SyncList<int> guildAllies = new SyncList<int>();

   // The guild name this user belongs to
   [SyncVar]
   public string guildName;

   // The guild permissions that this user holds
   [SyncVar]
   public int guildPermissions;

   // The guild rank priority that this user has
   [SyncVar]
   public int guildRankPriority;

   // The guild icon layers
   [SyncVar]
   public string guildIconBorder;
   [SyncVar]
   public string guildIconBackground;
   [SyncVar]
   public string guildIconSigil;

   // The guild icon colors
   [SyncVar]
   public string guildIconBackPalettes;
   [SyncVar]
   public string guildIconSigilPalettes;

   // The guild icon of the player
   public GuildIcon guildIcon;

   // The base id of the custom guild map for this user's guild
   [SyncVar]
   public int guildMapBaseId;

   // The base id of the custom guild house map for this user's guild
   [SyncVar]
   public int guildHouseBaseId;

   // The id of the inventory of the guild
   [SyncVar]
   public int guildInventoryId;

   // The demo status of account
   [SyncVar]
   public bool isDemoUser;

   [Header("Warping")]

   // Gets set to true when we're about to execute a warp on the server or client
   public bool isAboutToWarpOnServer = false;
   public bool isAboutToWarpOnClient = false;

   // Determines if the player is animating an interact clip
   public bool interactingAnimation = false;

   // Determines if this unit is speeding
   public bool isSpeedingUp;

   // Values that determines if the magnitude indicates moving
   public const float SPRINTING_MAGNITUDE = .2f;
   public const float SHIP_MOVING_MAGNITUDE = .005f;
   public const float NETWORK_SHIP_MOVING_MAGNITUDE = .001f;

   // The magnitude which determines that the ship has enough speed buildup
   public const float SHIP_SPEEDUP_MAGNITUDE = .01f;
   public const float SHIP_SLOWDOWN_MAGNITUDE = .006f;

   // The magnitude which determines that the ship has enough speed buildup through the network
   public const float NETWORK_PLAYER_SPEEDUP_MAGNITUDE = .02f;
   public const float NETWORK_SHIP_SPEEDUP_MAGNITUDE = .41f;

   // The amount of time that must pass between movement changes
   public static float MOVE_CHANGE_INTERVAL = .05f;

   // The number of seconds after which an attacker stops being one
   public static float ATTACKER_STATUS_DURATION = 30f;

   // The number of seconds one is 'in combat' for after taking damage
   public static float IN_COMBAT_STATUS_DURATION = 15.0f;

   // The speed multiplied when speed boosting
   public static float SPEEDUP_MULTIPLIER_SHIP = 1.5f;
   public static float MAX_SHIP_SPEED = 150;
   public static float SPEEDUP_MULTIPLIER_LAND = 1.5f;

   // The speed multiplier when in ghost mode
   public static float GHOST_SPEED_MULTIPLIER = 6f;

   // The duration of auto-move actions
   public static float AUTO_MOVE_ACTION_DURATION = 2f;

   // The interval in seconds between farming xp gain notifications
   public static float FARM_XP_NOTIFICATION_INTERVAL = 4f;

   // If this netentity is under control by a temporary controller, and had control passed from another temporary controller
   [SyncVar]
   public bool passedOnTemporaryControl = false;

   // Renderers added to this list will not be added to the _renderers list, and therefore not be modified by the NetEntity
   public List<SpriteRenderer> ignoredRenderers;

   // Should the renderers align with the direction faced by the current entity?
   public bool shouldAlignRenderersToFacingDirection = true;

   // If interact event was triggered
   public bool hasTriggeredInteractEvent;

   // If this entity is friendly to players
   [SyncVar]
   public bool isPlayerAlly;

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
      _clickableBoxCanvas = _clickableBox?.GetComponentInParent<Canvas>();
      _networkLerp = GetComponent<NetworkLerpRigidbody2D>();
      _animators.AddRange(GetComponentsInChildren<Animator>());
      _renderers.AddRange(GetComponentsInChildren<SpriteRenderer>(true));

      if (this.gameObject.HasComponent<Animator>()) {
         _animators.Add(GetComponent<Animator>());
      }

      foreach (Animator ignoredAnim in _ignoredAnimators) {
         _animators.Remove(ignoredAnim);
      }

      if (this.gameObject.HasComponent<SpriteRenderer>()) {
         _renderers.Add(GetComponent<SpriteRenderer>());
      }

      if (ignoredRenderers != null && ignoredRenderers.Count > 0) {
         foreach (SpriteRenderer renderer in ignoredRenderers) {
            _renderers.Remove(renderer);
         }
      }

      // Make the camera follow our player
      updatePlayerCamera();

      // Check command line
      _autoMove = Util.isAutoMove();

      if (shadow) {
         // Store the initial scale of the shadow
         _shadowInitialScale = shadow.transform.localScale;
      }
   }

   protected virtual void Start () {
      // Make the entity a child of the Area
      StartCoroutine(CO_SetAreaParent());

      if (isPlayerEntity()) {
         // Keep track in our Entity Manager
         EntityManager.self.storeEntity(this);

         if (isServer) {
            PerkManager.self.storePerkPointsForUser(userId);
         }
      }

      // Keep track of the Entity that we control
      if (isLocalPlayer) {
         Global.player = this;

         // Update our currently selected user ID, so it can be used for redirects
         Global.currentlySelectedUserId = this.userId;

         // Now that we have a player, we know that the redirection process is complete
         Global.isRedirecting = false;

         D.debug($"Our local NetEntity has been created, so setting Global.isRedirecting to false.");

         // The fast login is completed
         Global.isFastLogin = false;

         // Routinely compare our Time to the Server's time
         if (!isServer) {
            InvokeRepeating("requestServerTime", 0f, 1f);
         }

         // This will allow the local host in unity editor to simulate time for features such as crops
         if (Application.isEditor) {
            if (Global.player != null && isServer) {
               InvokeRepeating("requestServerTime", 0f, 1f);
            }
         }

         // Fetch the perk points for this user
         Global.player.rpc.Cmd_FetchPerkPointsForUser();

         // Download initial friends list
         FriendListManager.self.refreshFriendsData();

         if (isPlayerEntity()) {
            StartCoroutine(CO_LoadWeather());
         }

         // Start auto-move and/or auto-basic-actions
         if (_autoMove) {
            InvokeRepeating(nameof(autoMove), UnityEngine.Random.Range(0f, AUTO_MOVE_ACTION_DURATION), AUTO_MOVE_ACTION_DURATION);
         }
      }

      // Set invisible and disable colliders if the entity is in ghost mode
      if (this.isGhost) {
         enterGhostMode();
      }

      // Routinely clean the attackers set
      InvokeRepeating("cleanAttackers", 0f, 1f);

      // Registering the game user create event
      if (NetworkServer.active && (this is PlayerBodyEntity || this is PlayerShipEntity)) {
         Util.tryToRunInServerBackground(() => DB_Main.storeGameUserCreateEvent(this.userId, this.accountId, this.entityName, this.connectionToClient.address));
      }
   }

   public virtual PlayerBodyEntity getPlayerBodyEntity () {
      return null;
   }

   public virtual PlayerShipEntity getPlayerShipEntity () {
      return null;
   }

   public bool isPlayerEntity () {
      return getPlayerBodyEntity() || getPlayerShipEntity();
   }

   public override void OnStartClient () {
      base.OnStartClient();

      updateInvisibilityAlpha(isInvisible);


      if (isPlayerEntity()) {
         nameText.text = this.entityName;

         if (Global.player == getPlayerBodyEntity() && getPlayerBodyEntity() != null) {
            getPlayerBodyEntity().recolorNameText();
         }

         if (isLocalPlayer) {
            ClientManager.self.setDemoSuffixInVersionText(isDemoUser);
         }
      }
   }

   private IEnumerator CO_LoadWeather () {
      WeatherManager.self.setWeatherSimulation(WeatherEffectType.None);

      // Wait until our area finished loading
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return null;
      }

      // Process weather simulation for voyage area
      Area area = AreaManager.self.getArea(areaKey);
      WeatherManager.self.loadWeatherForArea(area);
   }

   protected virtual void Update () {
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

            PlayerBodyEntity bodyEntity = getPlayerBodyEntity();

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
      } else {
         // Show player names if in pvp
         if (pvpTeam != PvpTeamType.None && isPlayerShip()) {
            Util.setAlpha(nameText, 1.0f);
         }
      }

      // Check if we're showing a West sprite
      bool isFacingWest = this.facing == Direction.West || this.facing == Direction.NorthWest || this.facing == Direction.SouthWest;

      // Flip our sprite renderer if we're going west
      if (shouldAlignRenderersToFacingDirection) {
         foreach (SpriteRenderer renderer in _renderers) {
            renderer.flipX = isFacingWest;
         }
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
      // If a controller has taken over the control of entity, allow it to handle everything
      if (_temporaryControllers.Count > 0) {
         return;
      }

      // Disable movement control for our player under certain conditions
      if (!canReceiveInput()) {
         if (isLocalPlayer && !Util.isGeneralInputAllowed() && this is PlayerShipEntity) {
            PlayerShipEntity playerShip = (PlayerShipEntity) this;
            // Clears the server side movement input if general input was blocked but move direction still has value
            if (playerShip.getMovementInputDirection().magnitude > .1f) {
               // Do not clear movement based on magnitude if movement simulation is active
               if (!Util.isAutoMove()) {
                  playerShip.Cmd_ClearMovementInput();
               }
            }
         }

         // Do not block movement if movement simulation is active
         if (!Util.isAutoMove()) {
            return;
         }
      }

      // Only change our movement if enough time has passed
      if (this is SeaEntity && NetworkTime.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      // For players in land, we want to update every frame
      bool updateEveryFrame = !(this is SeaEntity);

      // Check if we need to use the alternate delayed movement mode
      if (this is SeaEntity && SeaManager.moveMode == SeaManager.MoveMode.Delay) {
         handleDelayMoveMode();
      } else if (this is SeaEntity && SeaManager.moveMode == SeaManager.MoveMode.ServerAuthoritative) {
         handleServerAuthoritativeMode();
      } else {
         handleInstantMoveMode(updateEveryFrame);
      }

      // In ghost mode, clamp the position to the area bounds
      clampToMapBoundsInGhost();
   }

   protected virtual void OnDestroy () {
      // If we are controlled by a temporary controller, allow it to finalize any data that's needed before we are destroyed
      while (_temporaryControllers.Count > 0) {
         _temporaryControllers[0].forceFastForward(this);
      }

      // Reset weather effects since player will be spawned somewhere else, weather effect manager will be set again after that
      if (isLocalPlayer && (this is PlayerBodyEntity || this is PlayerShipEntity)) {
         WeatherManager.self.setWeatherSimulation(WeatherEffectType.None);
         PowerupPanel.self.clearLandPowerups();
         PowerupPanel.self.clearSeaPowerups();
      }

      // Registering the game user destroy event
      if (NetworkServer.active && (this is PlayerBodyEntity || this is PlayerShipEntity)) {
         Util.tryToRunInServerBackground(() => DB_Main.storeGameUserDestroyEvent(this.userId, this.accountId, this.entityName, this.connectionToClient.address));
      }

      // Remove the entity from the manager
      if (this is PlayerBodyEntity || this is PlayerShipEntity) {
         EntityManager.self.removeEntity(this);
      }

      Vector3 localPos = this.transform.localPosition;

      if (Global.player == this && !ClientManager.isApplicationQuitting && !TitleScreen.self.isActive()) {
         // Show the loading screen
         if (PanelManager.self.loadingScreen != null) {
            PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.MapCreation);
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

   protected bool canReceiveInput () {
      return isLocalPlayer && Util.isGeneralInputAllowed() && !isFalling() && !isDead() && !isAboutToWarpOnClient;
   }

   public bool isMuted () {
      return DateTime.UtcNow.Ticks < this.muteExpirationDate;
   }

   [TargetRpc]
   public void Target_ReceiveOpenWorldStatus (PvpGameMode pvpMode, bool isOn, bool isInTown) {
      VoyageStatusPanel.self.refreshPvpStatDisplay();
      VoyageStatusPanel.self.setUserPvpMode(pvpMode);
      VoyageStatusPanel.self.togglePvpStatusInfo(isOn, isInTown);

      if (WorldMapManager.self.isWorldMapArea(areaKey)) {
         // Since the status of the pvp activity is not change able in open world, just display the icon
         VoyageStatusPanel.self.enablePvpStatDisplay(false);
         VoyageStatusPanel.self.togglePvpButtons(false);
      } else if (AreaManager.self.isTownArea(areaKey)) {
         // Allow the drop down of pvp status in town areas so it can be toggled on and off
         VoyageStatusPanel.self.enablePvpStatDisplay(true);
         VoyageStatusPanel.self.togglePvpButtons(true);
      }
   }

   [Command]
   public void Cmd_ToggleAdminInvisibility () {
      // Make sure only admin players can request invisibility
      if (!isAdmin()) {
         isInvisible = false;
         return;
      }

      isInvisible = !isInvisible;

      Rpc_OnInvisibilityUpdated(isInvisible);
   }

   [ClientRpc]
   public void Rpc_OnInvisibilityUpdated (bool isInvisible) {
      updateInvisibilityAlpha(isInvisible);
   }

   private void updateInvisibilityAlpha (bool isInvisible) {
      // Overwrite the visibility in ghost mode
      if (isGhost) {
         isInvisible = true;
      }

      if (Global.player == null || !Global.player.isAdmin()) {
         Util.setAlphaInShader(gameObject, isInvisible ? 0.0f : 1.0f);
         setCanvasVisibility(!isInvisible);
      } else {
         // Admins see other invisible admins and themselves as semi-transparent         
         Util.setAlphaInShader(gameObject, isInvisible ? 0.6f : 1.0f);
         setCanvasVisibility(true);
      }
   }

   public virtual void setCanvasVisibility (bool isVisible) {
      Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
      foreach (Canvas canvas in canvases) {
         canvas.enabled = isVisible;
      }
   }

   protected virtual void enterGhostMode () {
      updateInvisibilityAlpha(true);

      // Admins can click on other ghost admins
      if (Global.player == null || !Global.player.isAdmin()) {
         _clickableBox.gameObject.SetActive(false);
      } else {
         _clickableBox.gameObject.SetActive(true);
      }

      foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) {
         c.enabled = false;
      }
   }

   public void setAdminPrivileges (int newAdminFlag) {
      this.adminFlag = newAdminFlag;
   }

   public virtual void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, Item hat, Item ring, Item necklace, Item trinket,
      ShipInfo shipInfo, GuildInfo guildInfo, GuildRankInfo guildRankInfo) {
      this.entityName = userInfo.username;
      this.adminFlag = userInfo.adminFlag;
      this.customFarmBaseId = userInfo.customFarmBaseId;
      this.customHouseBaseId = userInfo.customHouseBaseId;
      this.guildId = userInfo.guildId;
      this.isDemoUser = userInfo.accountName.Contains("@demo");

      this.guildName = guildInfo.guildName;
      this.guildMapBaseId = guildInfo.guildMapBaseId;
      this.guildHouseBaseId = guildInfo.guildHouseBaseId;
      this.guildInventoryId = guildInfo.inventoryId;

      if (guildRankInfo != null) {
         this.guildPermissions = guildRankInfo.permissions;
      }

      // Body
      this.gender = userInfo.gender;
      this.hairPalettes = userInfo.hairPalettes;
      this.hairType = userInfo.hairType;
      this.eyesType = userInfo.eyesType;
      this.eyesPalettes = userInfo.eyesPalettes;
      this.bodyType = userInfo.bodyType;

      if (weapon.itemTypeId > 0) {
         WeaponStatData weaponStatData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);
         if (weaponStatData != null) {
            weapon.data = WeaponStatData.serializeWeaponStatData(weaponStatData);
         } else {
            D.debug("Weapon was null {" + weapon.itemTypeId + "}. User equipped weapon is not available");
         }
      }

      if (armor.itemTypeId > 0) {
         ArmorStatData armorStatData = EquipmentXMLManager.self.getArmorDataBySqlId(armor.itemTypeId);
         if (armorStatData != null) {
            armor.data = ArmorStatData.serializeArmorStatData(armorStatData);
         } else {
            D.debug("Armor was null {" + armor.itemTypeId + "}. User equipped armor is not available");
         }
      }

      if (hat.itemTypeId > 0) {
         HatStatData hatStatData = EquipmentXMLManager.self.getHatData(hat.itemTypeId);
         if (hatStatData != null) {
            hat.data = HatStatData.serializeHatStatData(hatStatData);
         } else {
            D.debug("Hat was null {" + hat.itemTypeId + "}. User equipped hat is not available");
         }
      }

      if (ring.itemTypeId > 0) {
         RingStatData ringStatData = EquipmentXMLManager.self.getRingData(ring.itemTypeId);
         if (ringStatData != null) {
            ring.data = RingStatData.serializeRingStatData(ringStatData);
         } else {
            D.debug("Ring was null {" + ring.itemTypeId + "}. User equipped ring is not available");
         }
      }
      if (necklace.itemTypeId > 0) {
         NecklaceStatData necklaceStatData = EquipmentXMLManager.self.getNecklaceData(necklace.itemTypeId);
         if (necklaceStatData != null) {
            necklace.data = NecklaceStatData.serializeNecklaceStatData(necklaceStatData);
         } else {
            D.debug("Necklace was null {" + necklace.itemTypeId + "}. User equipped necklace is not available");
         }
      }
      if (trinket.itemTypeId > 0) {
         TrinketStatData trinketStatData = EquipmentXMLManager.self.getTrinketData(trinket.itemTypeId);
         if (trinketStatData != null) {
            trinket.data = TrinketStatData.serializeTrinketStatData(trinketStatData);
         } else {
            D.debug("Trinket was null {" + trinket.itemTypeId + "}. User equipped trinket is not available");
         }
      }
   }

   public void updateGuildIconSprites () {
      // Assign sprites
      if (!string.IsNullOrEmpty(this.guildIconBackground)) {
         this.guildIcon.setBackground(this.guildIconBackground, this.guildIconBackPalettes);
      }
      if (!string.IsNullOrEmpty(this.guildIconBorder)) {
         this.guildIcon.setBorder(this.guildIconBorder);
      }
      if (!string.IsNullOrEmpty(this.guildIconSigil)) {
         this.guildIcon.setSigil(this.guildIconSigil, this.guildIconSigilPalettes);
      }
   }

   [ClientRpc]
   public void Rpc_UpdateGuildIconSprites (string background, string backgroundPalette, string border, string sigil, string sigilPalette) {
      // Assign sprites
      if (!string.IsNullOrEmpty(background)) {
         this.guildIcon.setBackground(background, backgroundPalette);
      } else {
         this.guildIcon.setBackground(null, null);
      }
      if (!string.IsNullOrEmpty(border)) {
         this.guildIcon.setBorder(border);
      } else {
         this.guildIcon.setBorder(null);
      }
      if (!string.IsNullOrEmpty(sigil)) {
         this.guildIcon.setSigil(sigil, sigilPalette);
      } else {
         this.guildIcon.setSigil(null, null);
      }
   }

   public void showGuildIcon () {
      if (this is PlayerShipEntity) {
         this.getPlayerShipEntity().guildIconGO.SetActive(true);
      }

      CanvasGroup guildIconCanvasGroup = guildIcon.canvasGroup;
      guildIconCanvasGroup.alpha = 1f;
      guildIconCanvasGroup.interactable = true;
      guildIconCanvasGroup.blocksRaycasts = true;
   }

   public void hideGuildIcon () {
      CanvasGroup guildIconCanvasGroup = guildIcon.canvasGroup;
      guildIconCanvasGroup.alpha = 0f;
      guildIconCanvasGroup.interactable = false;
      guildIconCanvasGroup.blocksRaycasts = false;

      if (this is PlayerShipEntity) {
         this.getPlayerShipEntity().guildIconGO.SetActive(false);
      }
   }

   public void showEntityName () {
      if (entityNameGO != null) {
         entityNameGO.SetActive(true);
      }
   }

   public void hideEntityName () {
      if (entityNameGO != null) {
         entityNameGO.SetActive(false);
      }
   }

   public bool isMale () {
      return gender == Gender.Type.Male;
   }

   public virtual bool isDead () {
      return currentHealth <= 0;
   }

   public bool isAdmin () {
      return (adminFlag == (int) PrivilegeType.Admin);
   }

   public bool isFalling () {
      return fallDirection != 0;
   }

   public bool isInBattle () {
      return battleId > 0;
   }

   [Server]
   public void moveToPosition (Vector3 position) {
      Target_SetPosition(position);
   }

   [TargetRpc]
   private void Target_SetPosition (Vector3 position) {
      transform.localPosition = position;
   }

   [Server]
   public void moveToWorldPosition (Vector3 position) {
      Target_SetWorldPosition(position);
   }

   [TargetRpc]
   private void Target_SetWorldPosition (Vector3 position) {
      transform.position = position;
   }

   public void requestAnimationPlay (Anim.Type animType, bool freezeAnim = false) {
      if (interactingAnimation) {
         return;
      }

      foreach (Animator animator in _animators) {
         switch (animType) {
            case Anim.Type.Interact_East:
            case Anim.Type.Interact_North:
            case Anim.Type.Interact_South:
            case Anim.Type.Fast_Interact_East:
            case Anim.Type.Fast_Interact_North:
            case Anim.Type.Fast_Interact_South:
               animator.SetBool("interact", true);
               break;
            case Anim.Type.Impact_Interact_East:
            case Anim.Type.Impact_Interact_North:
            case Anim.Type.Impact_Interact_South:
               animator.SetBool("interactImpact", true);
               break;
            case Anim.Type.Pet_East:
            case Anim.Type.Pet_North:
            case Anim.Type.Pet_South:
               animator.SetFloat("velocityX", 0);
               animator.SetFloat("velocityY", 0);
               animator.SetBool("isMoving", false);
               animator.SetBool("inBattle", false);
               animator.SetBool("petting", true);
               break;
            case Anim.Type.NC_Jump_East:
            case Anim.Type.NC_Jump_North:
            case Anim.Type.NC_Jump_South:
               animator.SetBool("jump", true);
               break;
         }
      }

      switch (animType) {
         case Anim.Type.Interact_East:
         case Anim.Type.Interact_North:
         case Anim.Type.Interact_South:
            hasTriggeredInteractEvent = false;
            StartCoroutine(CO_DelayExitAnim(animType, 0.4f));
            interactingAnimation = true;
            break;
         case Anim.Type.Fast_Interact_East:
         case Anim.Type.Fast_Interact_North:
         case Anim.Type.Fast_Interact_South:
            hasTriggeredInteractEvent = false;
            StartCoroutine(CO_DelayExitAnim(animType, 0.2f));
            interactingAnimation = true;
            break;
         case Anim.Type.Impact_Interact_East:
         case Anim.Type.Impact_Interact_North:
         case Anim.Type.Impact_Interact_South:
            hasTriggeredInteractEvent = false;
            StartCoroutine(CO_DelayExitAnim(animType, 0.4f));
            interactingAnimation = true;
            break;
         case Anim.Type.Pet_East:
         case Anim.Type.Pet_North:
         case Anim.Type.Pet_South:
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.CRITTER_PET, this.transform.position);
            StartCoroutine(CO_DelayExitAnim(animType, 1.4f));
            interactingAnimation = true;
            break;
         case Anim.Type.NC_Jump_East:
         case Anim.Type.NC_Jump_North:
         case Anim.Type.NC_Jump_South:

            if (!isBouncingOnWeb()) {
               // Play jumping sound effect
               SoundEffectManager.self.playFmodSfx(SoundEffectManager.JUMP, this.transform.position);
            }

            if (!freezeAnim) {
               StartCoroutine(CO_DelayExitAnim(animType, 0.5f));
            }
            break;
      }
   }

   IEnumerator CO_DelayExitAnim (Anim.Type animType, float delay) {
      yield return new WaitForSeconds(delay);
      foreach (Animator animator in _animators) {
         switch (animType) {
            case Anim.Type.Interact_East:
            case Anim.Type.Interact_North:
            case Anim.Type.Interact_South:
            case Anim.Type.Fast_Interact_East:
            case Anim.Type.Fast_Interact_North:
            case Anim.Type.Fast_Interact_South:
               animator.SetBool("interact", false);
               break;
            case Anim.Type.Impact_Interact_East:
            case Anim.Type.Impact_Interact_North:
            case Anim.Type.Impact_Interact_South:
               animator.SetBool("interactImpact", false);
               break;
            case Anim.Type.Pet_East:
            case Anim.Type.Pet_North:
            case Anim.Type.Pet_South:
               animator.SetBool("petting", false);
               break;
            case Anim.Type.NC_Jump_East:
            case Anim.Type.NC_Jump_North:
            case Anim.Type.NC_Jump_South:
               animator.SetBool("jump", false);
               break;
         }
      }

      switch (animType) {
         case Anim.Type.NC_Jump_East:
         case Anim.Type.NC_Jump_North:
         case Anim.Type.NC_Jump_South:
            shadow.transform.localScale = _shadowInitialScale;

            if (!isBouncingOnWeb() || (isBouncingOnWeb() && !_activeWeb.hasLinkedWeb)) {
               // Play jump landing sound effect
               SoundEffectManager.self.playJumpLandSfx(this.sortPoint.transform.position, this.areaKey);
            }

            break;
      }

      interactingAnimation = false;
   }

   [ClientRpc]
   public void Rpc_ShowDamage (Attack.Type attackType, Vector2 pos, int damage) {
      ShipDamageText attackText = PrefabsManager.self.getTextPrefab(attackType);
      if (attackText != null) {
         ShipDamageText damageText = Instantiate(attackText, pos, Quaternion.identity);
         damageText.setDamage(damage);
      }
   }

   protected virtual float getBaseMoveSpeed () {
      if (this is PlayerShipEntity) {
         return 70.0f;
      } else if (this is SeaEntity) {
         return 25.0f;
      } else {
         return 135.0f;
      }
   }

   public virtual float getMoveSpeed () {
      // Figure out our base movement speed
      float baseSpeed = getBaseMoveSpeed();

      // Check if we need to apply a slow modifier
      float modifier = 1.0f;

      // Apply exclusive conditions here for entities that cannot be slowed
      bool skipStatusModification = false;
      if (isSeaMonsterMinion()) {
         skipStatusModification = true;
      }

      if (!skipStatusModification) {
         if (StatusManager.self.hasStatus(this.netId, Status.Type.Stunned)) {
            modifier = 0f;
         } else if (StatusManager.self.hasStatus(this.netId, Status.Type.Slowed)) {
            modifier = Mathf.Clamp(1.0f - StatusManager.self.getStrongestStatus(this.netId, Status.Type.Slowed), 0.2f, 1.0f);
         } else if (_isClimbing) {
            if (Time.time - _lastBodySpriteChangetime <= .2f) {
               modifier = 0;
            } else {
               modifier = .5f;
            }
         }
      }

      // Admin move speed overrides all modifiers
      // Debug speed boost for Admin users only
      int moveSpeedModifier = 1;
      if (KeyUtils.GetKey(Key.LeftShift) && isAdmin() && this is PlayerShipEntity && shipSpeedupFlag) {
         moveSpeedModifier = 2;
      }

      // Speed boost calculation based on land powerup
      if (PowerupPanel.self.hasLandPowerup(LandPowerupType.SpeedBoost)) {
         moveSpeedModifier = (int) (moveSpeedModifier * 1.5f);
      }

      // Climb speed boost calculation based on land powerup
      if (_isClimbing) {
         if (PowerupPanel.self.hasLandPowerup(LandPowerupType.ClimbSpeedBoost)) {
            moveSpeedModifier = (int) (moveSpeedModifier * 2.5f);
         }
      }

      // Add a speed multiplier when in ghost mode 
      float ghostMultiplier = isGhost ? GHOST_SPEED_MULTIPLIER : 1;

      return (baseSpeed * modifier) * moveSpeedModifier * ghostMultiplier;
   }

   public virtual void handleSpriteOutline () {
      if (_outline == null || Global.player == null) {
         return;
      }

      if (Global.player == this) {
         _outline.setNewColor(Color.white);
         _outline.setVisibility((isMouseOver() || isAttackCursorOver()) && !isDead());
         return;
      }

      if (Global.player.instanceId != this.instanceId) {
         return;
      }

      if (isAttackCursorOver()) {
         // If the attack cursor is over us, draw a yellow outline
         _outline.setNewColor(Color.yellow);
         _outline.setVisibility(true);
      } else if (isEnemyOf(Global.player)) {
         // Draw a red outline around enemies of the Player
         _outline.setNewColor(Color.red);
         _outline.setVisibility(true);
      } else if (isAllyOf(Global.player)) {
         // Draw a green outline around allies of the Player
         _outline.setNewColor(Color.green);
         _outline.setVisibility(true);
      } else if (hasAttackers()) {
         // If we've been attacked by someone, we get an orange outline
         _outline.setNewColor(Util.getColor(255, 187, 51));
         _outline.setVisibility(true);
      } else {
         // Only show our outline when the mouse is over us
         Color color = this is Enemy ? Color.red : Color.white;
         _outline.setNewColor(color);
         _outline.setVisibility(isMouseOver() && !isDead());
      }
   }

   public virtual float getTurnDelay () {
      return .25f;
   }

   public virtual float getAngleDelay () {
      return .25f;
   }

   public virtual float getInputDelay () {
      return .25f;
   }

   public virtual float getFireCannonDelay () {
      return .25f;
   }

   public virtual double getAddForceDelay () {
      return .2;
   }

   public Rigidbody2D getRigidbody () {
      return _body;
   }

   public CircleCollider2D getMainCollider () {
      if (_mainCollider == null) {
         _mainCollider = GetComponent<CircleCollider2D>();
      }

      return _mainCollider;
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
      if (isServer) {
         return InstanceManager.self.getInstance(instanceId);
      } else {
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

   public void setupForWarpClient () {
      // Execute any changes, visual modifications for warping
      // Should only be called on client
      // Assumes this gameObject will be destroyed shortly during the warp

      // Indicate that we are about to warp
      isAboutToWarpOnClient = true;

      // Freeze rigidbody
      getRigidbody().constraints = RigidbodyConstraints2D.FreezeAll;

      // Disable all sprite animations in children
      foreach (SimpleAnimation anim in GetComponentsInChildren<SimpleAnimation>()) {
         anim.enabled = false;
      }

      // Disable all animators in children
      foreach (Animator anim in GetComponentsInChildren<Animator>()) {
         if (!_ignoredAnimators.Contains(anim)) {
            anim.enabled = false;
         }
      }
   }

   public void onWarpFailed () {
      isAboutToWarpOnClient = false;

      // Unfreeze the rigidbody
      getRigidbody().constraints = RigidbodyConstraints2D.FreezeRotation;

      // Enable all sprite animations in children
      foreach (SimpleAnimation anim in GetComponentsInChildren<SimpleAnimation>()) {
         anim.enabled = true;
      }

      // Enable all animators in children
      foreach (Animator anim in GetComponentsInChildren<Animator>()) {
         if (!_ignoredAnimators.Contains(anim)) {
            anim.enabled = true;
         }
      }
   }

   protected void requestServerTime () {
      Cmd_RequestServerDateTime();
   }

   protected void cleanAttackers () {
      List<uint> oldAttackers = new List<uint>();

      // Take note of all the attackers that must be removed
      foreach (KeyValuePair<uint, double> KV in _attackers) {
         NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(KV.Key);
         if (entity == null || entity.isDead() || NetworkTime.time - KV.Value > ATTACKER_STATUS_DURATION) {
            oldAttackers.Add(KV.Key);
         }
      }

      // Remove the old attackers
      foreach (uint attackerId in oldAttackers) {
         _attackers.Remove(attackerId);
      }
   }

   public virtual bool isMoving () {
      // If we're using a NetworkLerp component, check that to see if we're moving
      if (_networkLerp != null && !isLocalPlayer) {
         // Check if we've recently applied any sort of velocity
         return (NetworkTime.time - _networkLerp.lastVelocityTime) < .1f;
      }

      // Otherwise, just check the rigidbody velocity directly
      return getVelocity().magnitude > .01f;
   }

   public void setClimbing (bool isClimbing) {
      _isClimbing = isClimbing;
      shadow.enabled = !isClimbing;
   }

   public bool isClimbing () {
      return _isClimbing;
   }

   public bool isMouseOver () {
      return MouseManager.self.isHoveringOver(_clickableBox);
   }

   public bool isAttackCursorOver () {
      return AttackManager.self.isHoveringOver(this);
   }

   public bool wasAttackedBy (uint netId) {
      // Check if this net id participated in combat, and if the combat duration was within 10 seconds
      if (_totalAttackers.ContainsKey(netId)) {
         if (NetworkTime.time - _totalAttackers[netId].lastAttackTime < 10) {
            return true;
         }
      }
      return false;
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

   public bool isEnemyOf (NetEntity otherEntity, bool deadEntitiesAreFriendly = true) {
      if (otherEntity == null || (otherEntity.isDead() && deadEntitiesAreFriendly)) {
         return false;
      }

      if (otherEntity == this) {
         return false;
      }

      // This allows bot ships to be friendly to allies
      if (isBotShip() && isPlayerAlly && otherEntity is PlayerShipEntity) {
         return false;
      }

      // This allows player ships to be friendly to bot ships
      if (this is PlayerShipEntity && otherEntity.isPlayerAlly && otherEntity is BotShipEntity) {
         return false;
      }

      // Bot ships can fight if they do not have the same guild (pirates vs privateers)
      if (guildId > 0 && otherEntity.guildId > 0 && isBotShip() && otherEntity.isBotShip() && otherEntity.guildId != guildId) {
         return true;
      }

      if (this is SeaEntity && otherEntity is SeaEntity) {
         if (enablePvp && otherEntity.enablePvp) {
            if (openWorldGameMode == PvpGameMode.FreeForAll && otherEntity.openWorldGameMode == PvpGameMode.FreeForAll) {
               return true;
            } else if (openWorldGameMode == PvpGameMode.GuildWars && otherEntity.openWorldGameMode == PvpGameMode.GuildWars) {
               if (otherEntity.guildId != guildId) {
                  // Proceed to alliance check
                  if (otherEntity.guildAllies.Contains(guildId) || guildAllies.Contains(otherEntity.guildId) || otherEntity.voyageGroupId == voyageGroupId) {
                     // If guilds are allied to each other, they are not enemies
                     return false;
                  } else {
                     // If no alliance is formed, they are enemies
                     return true;
                  }
               } else {
                  // If guild id of this unit and its target is the same, they are allies
                  return false;
               }
            } else if (openWorldGameMode == PvpGameMode.GroupWars && otherEntity.openWorldGameMode == PvpGameMode.GroupWars) {
               return otherEntity.voyageGroupId != voyageGroupId;
            }
         }
      }

      // If both entities are on a pvp team, check if they're on our team
      if (pvpTeam != PvpTeamType.None && otherEntity.pvpTeam != PvpTeamType.None) {
         return (pvpTeam != otherEntity.pvpTeam);
      }

      // If this is a bot ship and the other entity isn't (or viceversa), we're enemies
      if (isBotShip() != otherEntity.isBotShip()) {
         return true;
      }

      // If this is a sea monster entity and the other entity isn't (or viceversa), we're enemies
      if (isSeaMonster() != otherEntity.isSeaMonster()) {
         return true;
      }

      if (VoyageGroupManager.isInGroup(this) && VoyageGroupManager.isInGroup(otherEntity)) {
         // In PvE voyage instances, players from other groups are not enemies
         Instance instance = getInstance();
         if (instance != null && !instance.isPvP) {
            return false;
         }

         if (this.voyageGroupId == otherEntity.voyageGroupId) {
            return false;
         } else {
            return true;
         }
      }

      if (hasBeenAttackedBy(otherEntity) || otherEntity.hasBeenAttackedBy(this)) {
         if (enablePvp != otherEntity.enablePvp || (!enablePvp && !otherEntity.enablePvp)) {
            return false;
         }
         return true;
      }

      return false;
   }

   public bool isAllyOf (NetEntity otherEntity) {
      if (otherEntity == null || otherEntity.isDead()) {
         return false;
      }

      // This allows bot ships to be friendly to allies
      if (isBotShip() && isPlayerAlly && otherEntity is PlayerShipEntity) {
         return true;
      }

      // This allows player ships to be friendly to bot ships
      if (this is PlayerShipEntity && otherEntity.isPlayerAlly && otherEntity is BotShipEntity) {
         return true;
      }

      // Bot ships can fight if they do not have the same guild (pirates vs privateers)
      if (guildId > 0 && otherEntity.guildId > 0 && isBotShip() && otherEntity.isBotShip() && otherEntity.guildId == guildId) {
         return true;
      }

      if (enablePvp && otherEntity.enablePvp) {
         if (openWorldGameMode == PvpGameMode.FreeForAll && otherEntity.openWorldGameMode == PvpGameMode.FreeForAll) {
            return false;
         } else if (openWorldGameMode == PvpGameMode.GuildWars && otherEntity.openWorldGameMode == PvpGameMode.GuildWars) {
            if (otherEntity.guildId != guildId) {
               // Proceed to alliance check
               if (otherEntity.guildAllies.Contains(guildId) && guildAllies.Contains(otherEntity.guildId) || otherEntity.voyageGroupId == voyageGroupId) {
                  // If guilds are allied to each other, they are not enemies
                  return true;
               } else {
                  // If no alliance is formed, they are enemies
                  return false;
               }
            } else {
               // If guild id of this unit and its target is the same, they are allies
               return true;
            }
         } else if (openWorldGameMode == PvpGameMode.GroupWars && otherEntity.openWorldGameMode == PvpGameMode.GroupWars) {
            return otherEntity.voyageGroupId == voyageGroupId;
         }
      }

      if (VoyageGroupManager.isInGroup(this) && VoyageGroupManager.isInGroup(otherEntity)) {
         if (this.voyageGroupId == otherEntity.voyageGroupId) {
            return true;
         } else {
            return false;
         }
      }

      if (pvpTeam != PvpTeamType.None && pvpTeam == otherEntity.pvpTeam) {
         return true;
      }

      return false;
   }

   public virtual bool isAdversaryInPveInstance (NetEntity otherEntity) {
      return false;
   }

   public virtual bool canBeAttackedByPlayers () {
      return true;
   }

   public Vector2 getVelocity () {
      return _body.velocity;
   }

   public virtual void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.transform, worldPositionStays);
   }

   protected virtual void handleInstantMoveMode (bool updatingEveryFrame) {
      // Skip if Input is disabled
      if (!InputManager.isInputEnabled() && !InputManager.self.IsMoveSimulated) {
         return;
      }

      // Calculate by how much to reduce the movement speed due to differing update steps
      float frameRateMultiplier = updatingEveryFrame ? 1 / Mathf.Ceil(MOVE_CHANGE_INTERVAL / Time.deltaTime) : 1f;

      // Get a list of the directions we're allowed to move (sometimes includes diagonal directions)
      List<Direction> availableDirections = DirectionUtil.getAvailableDirections(true, _isClimbing);

      // Check if we're pressing the keys for any of the directions, and if so, add an appropriate force
      foreach (Direction direction in availableDirections) {
         if (InputManager.isPressingDirection(direction)) {
            // Check if we need to update our facing direction SyncVar
            Direction newFacingDirection = DirectionUtil.getFacingDirection(hasDiagonals, direction);
            // Don't update the facing direction if we're performing an interact animation
            if (this.facing != newFacingDirection && !interactingAnimation) {
               this.facing = newFacingDirection;

               // Tell the server to pass it along to all clients
               Cmd_UpdateFacing(newFacingDirection);
            }

            // Figure out the force vector we should apply
            Vector2 forceToApply = DirectionUtil.getVectorForDirection(direction);
            float baseMoveSpeed = getMoveSpeed();
            float sprintingSpeedMultiplier = 1.0f;

            // Slow the player down if they're performing an interaction animation
            if (interactingAnimation) {
               sprintingSpeedMultiplier *= 0.25f;
            } else if (isSpeedingUp) {
               sprintingSpeedMultiplier = SPEEDUP_MULTIPLIER_LAND;
            }

            baseMoveSpeed *= sprintingSpeedMultiplier;
            _body.AddForce(forceToApply.normalized * baseMoveSpeed * frameRateMultiplier);

            // Make note of the time
            _lastMoveChangeTime = NetworkTime.time;

            break;
         }
      }
   }

   [ClientRpc]
   public void Rpc_ForceLookat (Direction direction) {
      this.facing = direction;

      foreach (Animator animator in _animators) {
         animator.SetInteger("facing", (int) this.facing);
      }
   }

   [TargetRpc]
   public void Target_WinBattle () {
      SoundEffectManager.self.playTriumphSfx();
   }

   protected virtual void handleDelayMoveMode () {
      // Check if enough time has passed for us to change our facing direction
      bool canChangeDirection = (NetworkTime.time - _lastFacingChangeTime > getTurnDelay());

      if (canChangeDirection) {
         if (InputManager.self.inputMaster.General.MoveLeft.IsPressed()) {
            Cmd_ModifyFacing(-1);
            _lastFacingChangeTime = NetworkTime.time;
         } else if (InputManager.self.inputMaster.General.MoveRight.IsPressed()) {
            Cmd_ModifyFacing(+1);
            _lastFacingChangeTime = NetworkTime.time;
         }
      }

      // Figure out the force vector we should apply
      if (InputManager.self.inputMaster.General.MoveUp.IsPressed()) {
         Vector2 forceToApply = DirectionUtil.getVectorForDirection(this.facing);
         _body.AddForce(forceToApply.normalized * getMoveSpeed());

         // Make note of the time
         _lastMoveChangeTime = NetworkTime.time;
      }
   }

   protected virtual void handleServerAuthoritativeMode () {
      // Handled by the PlayerShipEntity class
   }

   protected void clampToMapBoundsInGhost () {
      if (isGhost) {
         Area area = AreaManager.self.getArea(areaKey);
         if (area != null && !area.cameraBounds.bounds.Contains(_body.position)) {
            // Stop the movement in the direction that went outside the bounds
            if (_body.position.x < area.cameraBounds.bounds.min.x || _body.position.x > area.cameraBounds.bounds.max.x) {
               _body.velocity = new Vector2(0, _body.velocity.y);
            }
            if (_body.position.y < area.cameraBounds.bounds.min.y || _body.position.y > area.cameraBounds.bounds.max.y) {
               _body.velocity = new Vector2(_body.velocity.x, 0);
            }

            // Clamp the position inside the area
            _body.MovePosition(new Vector2(
               Mathf.Clamp(_body.position.x, area.cameraBounds.bounds.min.x, area.cameraBounds.bounds.max.x),
               Mathf.Clamp(_body.position.y, area.cameraBounds.bounds.min.y, area.cameraBounds.bounds.max.y)));
         }
      }
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

   [TargetRpc]
   public void Target_AutoAttack (float attackDelay) {
      ChatManager.self.addChat("This user will now auto attack: " + attackDelay, ChatInfo.Type.Debug);
      Global.autoAttack = true;
      Global.attackDelay = attackDelay;
   }

   [TargetRpc]
   public void Target_ForceJoin (bool autoAttack, float attackDelay) {
      ChatManager.self.addChat("This user will now force their group to join: " + autoAttack + " - " + attackDelay, ChatInfo.Type.Debug);
      Global.forceJoin = true;
      if (autoAttack) {
         Global.autoAttack = true;
         Global.attackDelay = attackDelay;
      }
   }

   [TargetRpc]
   public void Target_ReceiveNormalChat (string message, ChatInfo.Type type) {
      ChatManager.self.addChat(message, type);
   }

   [TargetRpc]
   protected void Target_ReceiveServerDateTime (NetworkConnection conn, float serverUnityTime, long serverDateTime) {
      TimeManager.self.setLastServerDateTime(serverDateTime);

      // Pass the server time and round trip time off to the Time Manager to keep track of
      TimeManager.self.setTimeOffset(serverUnityTime, (float) NetworkTime.rtt);
   }

   [TargetRpc]
   public void Target_ReceiveBattleExp (NetworkConnection connection, int xpGained) {
      StartCoroutine(CO_ProcessBattleExp(xpGained));
   }

   [Server]
   public void setMuteInfo (long expiresAt, bool isStealth) {
      this.muteExpirationDate = expiresAt;
      this.isStealthMuted = isStealth;
   }

   private IEnumerator CO_ProcessBattleExp (int xpGained) {
      yield return new WaitForSeconds(1.5f);
      GameObject xpCanvas = Instantiate(PrefabsManager.self.xpGainPrefab);
      Vector3 offset = new Vector3(.25f, .25f, 0);
      xpCanvas.transform.position = transform.position + offset;
      TextMeshProUGUI textObj = xpCanvas.GetComponentInChildren<TextMeshProUGUI>();
      textObj.text = "+" + xpGained + " XP";
      textObj.color = Color.magenta;
   }

   [TargetRpc]
   public void Target_GainedFarmXp (NetworkConnection conn, int xpGained, Jobs jobs) {
      CancelInvoke(nameof(displayFarmXpGain));

      _lastJobsXp = jobs;
      _accumulatedFarmingXp += xpGained;

      // If there has been no farming xp gain notification in the past seconds, immediately display the accumulated xp (in a few frames, in case there have been multiple simultaneous actions)
      if (NetworkTime.time - _lastFarmingXpNotificationTime > FARM_XP_NOTIFICATION_INTERVAL) {
         Invoke(nameof(displayFarmXpGain), 0.1f);
         return;
      }

      // If a few seconds pass without farming actions, display the accumulated xp gained
      Invoke(nameof(displayFarmXpGain), FARM_XP_NOTIFICATION_INTERVAL / 2);
   }

   protected void displayFarmXpGain () {
      onGainedJobXP(_accumulatedFarmingXp, _lastJobsXp, Jobs.Type.Farmer, -1, true);
      _accumulatedFarmingXp = 0;
      _lastFarmingXpNotificationTime = NetworkTime.time;
   }

   [TargetRpc]
   public void Target_GainedXP (NetworkConnection conn, int xpGained, Jobs jobs, Jobs.Type jobType, int cropNumber, bool showFloatingXp) {
      onGainedJobXP(xpGained, jobs, jobType, cropNumber, showFloatingXp);
   }

   public bool canGainXP (int xp) {
      if (!isDemoUser) {
         return true;
      }

      // Restrict user level to max 5
      return LevelUtil.levelForXp(this.XP) < 5;
   }

   protected void onGainedJobXP (int xpGained, Jobs jobs, Jobs.Type jobType, int cropNumber, bool showFloatingXp) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);

      // If it happened at a crop spot, show the XP gain there
      if (cropNumber > 0) {
         pos = CropSpotManager.self.getCropSpot(cropNumber, areaKey).transform.position + new Vector3(0f, .32f);
      }

      if (showFloatingXp) {
         // Show a message that they gained some XP
         GameObject xpCanvas = Instantiate(PrefabsManager.self.xpGainPrefab);
         xpCanvas.transform.position = pos;
         xpCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "+" + xpGained + " " + jobType + " XP";
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
      int newLevel = LevelUtil.levelForXp(newXP);

      // If they gained a level, show a special message
      if (levelsGained > 0) {
         // Show an effect
         rpc.Cmd_ShowLevelUpEffect(jobType);

         // Play a sound
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.TUTORIAL_STEP);

         // Show the level up in chat
         string levelsMsg = string.Format("You gained {0} {1} {2}! Current level: {3}", levelsGained, jobType, levelsGained > 1 ? "levels" : "level", newLevel);
         ChatManager.self.addChat(levelsMsg, ChatInfo.Type.System);
      }
   }

   [Server]
   public void onGainedXP (int oldXP, int newXP) {
      int levelsGained = LevelUtil.levelsGained(oldXP, newXP);

      if (levelsGained > 0) {
         AchievementManager.registerUserAchievement(this, ActionType.LevelUp, levelsGained);
      }
   }

   public virtual void showLevelUpEffect (Jobs.Type jobType) {
      return;
   }

   public void requestControl (TemporaryController controller, bool overrideMovement = false) {
      if (_temporaryControllers.Contains(controller)) {
         D.error("Requesting control of entity twice by the same controller.");
         return;
      }

      _temporaryControllers.Add(controller);

      if (hasAuthority) {
         Cmd_TemporaryControlRequested(controller.transform.localPosition.x, controller.transform.localPosition.y);
      }

      if (isServer && isClient) {
         rpc.Rpc_TemporaryControlRequested(controller.transform.localPosition);
      }

      if (_temporaryControllers.Count == 1) {
         controller.controlGranted(this, overrideMovement);
      }
   }

   public bool hasScheduledController (TemporaryController controller) {
      return _temporaryControllers.Contains(controller);
   }

   public void giveBackControl (TemporaryController controller) {
      bool willBeChanges = _temporaryControllers.Count > 0 && _temporaryControllers[0] == controller;

      _temporaryControllers.RemoveAll(c => c == controller);

      if (willBeChanges && _temporaryControllers.Count > 0) {
         _temporaryControllers[0].controlGranted(this);
      }
   }

   [Command]
   public void Cmd_TemporaryControlRequested (float controllerPosX, float controllerPosY) {
      Vector2 controllerLocalPosition = new Vector2(controllerPosX, controllerPosY);
      if (AreaManager.self.getArea(areaKey) != null) {
         TemporaryController con = AreaManager.self.getArea(areaKey).getTemporaryControllerAtPosition(controllerLocalPosition);
         if (con != null && !hasScheduledController(con)) {
            requestControl(con);
            rpc.Rpc_TemporaryControlRequested(controllerLocalPosition);
         }
      }
   }

   public void noteWebBounce (TemporaryController con) {
      if (con != null) {
         if (con is SpiderWeb) {
            SpiderWeb web = con as SpiderWeb;
            _activeWeb = web;
            _webBounceStartTime = (float) NetworkTime.time;

            // If we're continuing a bounce
            if (passedOnTemporaryControl) {
               _isDoingHalfBounce = true;
            } else {
               _isDoingHalfBounce = false;
            }

            PlayerBodyEntity body = getPlayerBodyEntity();
            if (body && isLocalPlayer) {
               body.Cmd_NoteJump();
            }
         }
      }
   }

   [TargetRpc]
   public void Target_ReceiveGlobalChat (int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted, bool isSenderAdmin, string extra) {
      // Convert Json string back into a GuildIconData object and add to chatInfo
      GuildIconData guildIconData = JsonUtility.FromJson<GuildIconData>(guildIconDataString);
      ChatInfo chatInfo = new ChatInfo(chatId, message, System.DateTime.FromBinary(timestamp), ChatInfo.Type.Global, senderName, "", senderUserId, guildIconData, guildName, isSenderMuted, isSenderAdmin, extra: extra);

      // Add it to the Chat Manager
      ChatManager.self.addChatInfo(chatInfo);
   }

   [ClientRpc]
   public void Rpc_ChatWasSent (int chatId, string message, long timestamp, ChatInfo.Type chatType, string guildIconDataString, string guildName, bool isSenderMuted, bool isSenderAdmin, string extra) {
      GuildIconData guildIconData = JsonUtility.FromJson<GuildIconData>(guildIconDataString);
      ChatInfo chatInfo = new ChatInfo(chatId, message, System.DateTime.FromBinary(timestamp), chatType, entityName, "", userId, guildIconData, guildName, isSenderMuted, isSenderAdmin, extra: extra);
      ChatManager.self.addChatInfo(chatInfo);
   }

   [TargetRpc]
   public void Target_ReceiveSpecialChat (NetworkConnection conn, int chatId, string message, string senderName, string receiverName, long timestamp, ChatInfo.Type chatType, GuildIconData guildIconData, string guildName, int senderId, bool isSenderMuted, string extra) {
      ChatInfo chatInfo = new ChatInfo(chatId, message, System.DateTime.FromBinary(timestamp), chatType, senderName, receiverName, senderId, guildIconData, guildName, isSenderMuted, extra: extra);

      // Add it to the Chat Manager
      ChatManager.self.addChatInfo(chatInfo);
   }

   [TargetRpc]
   public void Target_ReceivePvpChat (NetworkConnection conn, int instanceId, string message) {
      ChatInfo chatInfo = new ChatInfo {
         senderId = instanceId,
         messageType = ChatInfo.Type.PvpAnnouncement,
         text = message
      };

      // Add it to the Chat Manager
      ChatManager.self.addChatInfo(chatInfo);
   }

   [TargetRpc]
   public void Target_CensorGlobalMessagesFromUser (int userId) {
      ChatPanel.self.censorGlobalMessagesFromUser(userId);
   }

   [TargetRpc]
   public void Target_ReceiveGuildInvitationNotification (NetworkConnection conn, int guildId, string inviterName, int inviterUserId, string guildName) {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => GuildManager.self.acceptInviteOnClient(guildId, inviterName, inviterUserId, entityName, userId, guildName));

      // Show a confirmation panel with the user name
      string message = "The player " + inviterName + " has invited you to join the guild " + guildName + "!";
      PanelManager.self.confirmScreen.show(message);
   }

   [TargetRpc]
   public void Target_ReceiveGuildAcceptNotification (NetworkConnection conn, int guildId, string inviterName, int inviterUserId, string invitedUserName, int invitedUserId, string guildName, GuildInfo guildInfo) {
      D.debug("Player {" + invitedUserName + "} has accepted the guild invitation");
   }

   [TargetRpc]
   public void Target_ReceiveGroupInvitationNotification (NetworkConnection conn, int voyageGroupId, string inviterName) {
      VoyageGroupManager.self.receiveGroupInvitation(voyageGroupId, inviterName);
   }

   [TargetRpc]
   public void Target_GainedItem (NetworkConnection conn, string itemIconPath, string itemName, Jobs.Type jobType, float jobExp, int itemCount) {
      Vector3 nudge = new Vector3(0f, .32f);
      Vector3 pos = this.transform.position + nudge;

      // Move the already spawned messages a little up.
      FloatingCanvas[] spawnedCanvases = GameObject.FindObjectsOfType<FloatingCanvas>();
      if (spawnedCanvases != null && spawnedCanvases.Length > 0) {
         foreach (FloatingCanvas canvas in spawnedCanvases) {
            if (canvas.TryGetComponent<RectTransform>(out RectTransform rectTransform)) {
               Vector3 scaledRect = rectTransform.localScale * (rectTransform.rect.height + nudge.y);
               canvas.transform.position = canvas.transform.position + new Vector3(.0f, scaledRect.y, .0f);
            }
         }
      }

      // Show a message that they gained some XP along with the item they received
      GameObject gainItemCanvas = Instantiate(PrefabsManager.self.itemReceivedPrefab);
      gainItemCanvas.transform.position = pos;
      if (jobType != Jobs.Type.None) {
         gainItemCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "+ " + itemCount + " " + itemName + "\n" + "+ " + jobExp + " " + jobType + " XP";
      } else {
         gainItemCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "+ " + itemCount + " " + itemName;
      }
      gainItemCanvas.GetComponentInChildren<Image>().sprite = ImageManager.getSprite(itemIconPath);
   }

   [TargetRpc]
   public void Target_FloatingMessage (NetworkConnection conn, string message) {
      Vector3 pos = this.transform.position + new Vector3(0f, .32f);
      GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
      messageCanvas.transform.position = pos;
      messageCanvas.GetComponentInChildren<TextMeshProUGUI>().text = message;
   }

   [TargetRpc]
   public void Target_ReceiveUnreadMailNotification (NetworkConnection conn) {
      BottomBar.self.setUnreadMailNotificationStatus(true);
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.MAIL_NOTIFICATION);
   }

   [TargetRpc]
   public void Target_ReceiveFriendshipRequestNotification (NetworkConnection conn) {
      BottomBar.self.setFriendshipRequestNotificationStatus(true);
      ChatManager.self.addFriendRequestNotification();
   }

   [Command]
   public void Cmd_PlantCrop (Crop.Type cropType, int cropNumber, string areaKey) {
      // We have to holding the seed bag
      BodyEntity body = GetComponent<BodyEntity>();

      if (body == null || body.weaponManager.actionType != Weapon.ActionType.PlantCrop) {
         D.warning("Can't plant without seeds equipped!");
         return;
      }

      this.cropManager.plantCrop(cropType, cropNumber, areaKey, body.weaponManager.equippedWeaponId, true);
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
   public void Cmd_GoToGuildMap () {
      string areaTarget = CustomGuildMapManager.GROUP_AREA_KEY;

      // Don't allow users to go to the guild map if they are in combat.
      if (hasAttackers()) {
         if (isInCombat()) {
            int timeUntilCanLeave = (int) (IN_COMBAT_STATUS_DURATION - getTimeSinceAttacked());
            ServerMessageManager.sendError(ErrorMessage.Type.Misc, this, "Cannot move to guild map until out of combat for " + (int) IN_COMBAT_STATUS_DURATION + " seconds. \n(" + timeUntilCanLeave + " seconds left)");
            return;
         }
      }

      // End land combat is the user is in a battle
      if (battleId > 0) {
         Battle battle = BattleManager.self.getBattle(battleId);
         if (battle == null) {
            D.debug("Missing battle for user: {" + userId + "} using battle id: {" + battleId + "}, cant end battle properly");
            return;
         }

         Battler battler = BattleManager.self.getBattle(battleId).getBattler(userId);
         if (battler == null) {
            D.debug("Missing battler for user: {" + userId + "}, cant end battle properly");
            return;
         }

         battler.health = 0;
         battle.onBattleEnded.Invoke();
         return;
      }

      // If the user is currently in ghost mode, disable it
      if (isGhost && tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         VoyageGroupManager.self.removeUserFromGroup(voyageGroup, userId);
      }

      CustomMapManager mapManager;
      if (!AreaManager.self.tryGetCustomMapManager(areaTarget, out mapManager)) {
         D.error("Cmd_GoToGuildMap error: Couldn't get the custom map manager.");
         return;
      }

      CustomGuildMapManager guildMapManager = mapManager as CustomGuildMapManager;
      if (guildMapManager == null) {
         D.error("Cmd_GoToGuildMap error: Custom map manager was not a guild map manager.");
         return;
      }

      if (!string.IsNullOrEmpty(areaTarget)) {
         if (!mapManager.canUserWarpInto(this, areaTarget, out System.Action<NetEntity> denyWarpHandler)) {
            denyWarpHandler?.Invoke(this);
            return;
         }
      }

      if (guildMapBaseId == 0) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, this, "Guild leader must select a map layout.");
         return;
      }

      areaTarget = CustomGuildMapManager.getGuildSpecificAreaKey(guildId);

      spawnInNewMap(areaTarget);
   }

   [Command]
   public void Cmd_GoHome () {
      // Don't allow users to go home if they are in combat.
      if (hasAttackers()) {
         if (isInCombat()) {
            int timeUntilCanLeave = (int) (IN_COMBAT_STATUS_DURATION - getTimeSinceAttacked());
            ServerMessageManager.sendError(ErrorMessage.Type.Misc, this, "Cannot return to home location until out of combat for " + (int) IN_COMBAT_STATUS_DURATION + " seconds. \n(" + timeUntilCanLeave + " seconds left)");
            return;
         }
      }

      // End land combat is the user is in a battle
      if (battleId > 0) {
         Battle battle = BattleManager.self.getBattle(battleId);
         if (battle == null) {
            D.debug("Missing battle for user: {" + userId + "} using battle id: {" + battleId + "}, cant end battle properly");
            return;
         }

         Battler battler = BattleManager.self.getBattle(battleId).getBattler(userId);
         if (battler == null) {
            D.debug("Missing battler for user: {" + userId + "}, cant end battle properly");
            return;
         }

         battler.health = 0;
         battle.onBattleEnded.Invoke();
         return;
      }

      // If the user is currently in ghost mode, disable it
      if (isGhost && tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         VoyageGroupManager.self.removeUserFromGroup(voyageGroup, userId);
      }

      D.debug($"Returning player {entityName} to town: Go Home Command!");
      if (VoyageManager.isWorldMapArea(areaKey)) {
         spawnInBiomeHomeTown();
      } else {
         spawnInBiomeHomeTown(Biome.Type.Forest);
      }
   }

   [Server]
   public void spawnInBiomeHomeTown () {
      Instance instance = InstanceManager.self.getInstance(instanceId);
      spawnInBiomeHomeTown(instance == null ? Biome.Type.Forest : instance.biome);
   }

   [Server]
   public void spawnInBiomeHomeTown (Biome.Type biome) {
      // Go to background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         bool isBiomeUnlocked = false;

         if (Area.homeTownForBiome.TryGetValue(biome, out string biomeHomeTownAreaKey)) {
            isBiomeUnlocked = DB_Main.hasUserVisitedArea(userId, biomeHomeTownAreaKey);
         }

         // Back to unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // If the biome is unlocked, and we get an area key for the town, spawn them there
            if (Area.homeTownForBiome.TryGetValue(biome, out string townAreaKey) && isBiomeUnlocked) {
               if (Area.dockSpawnForBiome.TryGetValue(biome, out string dockSpawn)) {
                  spawnInNewMap(townAreaKey, dockSpawn, Direction.South);
               } else {
                  spawnInNewMap(townAreaKey);
               }
               // Otherwise, spawn them in the starting town.
            } else {
               spawnInNewMap(Area.STARTING_TOWN);
            }
         });
      });
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
      Direction adjustedNewFacingDirection = newFacingDirection;

      // Check if this is a custom map
      AreaManager.self.tryGetCustomMapManager(newArea, out CustomMapManager customMapManager);
      if (customMapManager != null) {
         D.adminLog("{" + entityName + ":" + userId + "} Spawning in new map {" + newArea + "}!", D.ADMIN_LOG_TYPE.Visit);

         // Check if user is not able to warp into owned area
         if (!customMapManager.canUserWarpInto(this, newArea, out System.Action<NetEntity> denyWarphandler)) {
            // Deny access to owned area
            denyWarphandler?.Invoke(this);
            D.adminLog("Warp has been denied here in warp!", D.ADMIN_LOG_TYPE.Visit);
            return;
         }

         // If this is a guild specific map
         if (customMapManager is CustomGuildMapManager) {
            // Make a guild-specific area key for this user, if it is not a guild-specific key already
            if (newArea == CustomGuildMapManager.GROUP_AREA_KEY) {
               newArea = CustomGuildMapManager.getGuildSpecificAreaKey(guildId);
            }

            // Get the base map key
            NetEntity guildMember = EntityManager.self.getEntity(userId);
            string baseMapKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(guildMember));
            targetLocalPos = (spawn == null) ? SpawnManager.self.getDefaultLocalPosition(baseMapKey) : SpawnManager.self.getLocalPosition(baseMapKey, spawn);

         } else if (customMapManager is CustomGuildHouseManager) {
            // Make a guild-specific area key for this user, if it is not a guild-specific key already
            if (newArea == CustomGuildHouseManager.GROUP_AREA_KEY) {
               newArea = CustomGuildHouseManager.getGuildSpecificAreaKey(guildId);
            }

            // Get the base map key
            NetEntity guildMember = EntityManager.self.getEntity(userId);
            string baseMapKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(guildMember));
            targetLocalPos = (spawn == null) ? SpawnManager.self.getDefaultLocalPosition(baseMapKey) : SpawnManager.self.getLocalPosition(baseMapKey, spawn);

         } else {
            // Make a user-specific area key for this user, if it is not a user-specific area key already
            if (!CustomMapManager.isUserSpecificAreaKey(newArea)) {
               D.adminLog("{" + entityName + ":" + userId + "} This is not a specific area key! {From: " + newArea + " moving to: " + customMapManager.getUserSpecificAreaKey(userId) + "}", D.ADMIN_LOG_TYPE.Visit);
               newArea = customMapManager.getUserSpecificAreaKey(userId);
            }

            // Get the owner of the target map
            int targetUserId = CustomMapManager.getUserId(newArea);
            D.adminLog("{" + entityName + ":" + userId + "} Spawning in new map with owner check! {" + newArea + ":" + targetUserId + "}", D.ADMIN_LOG_TYPE.Visit);

            NetEntity owner = EntityManager.self.getEntity(targetUserId);

            // Get the base map
            string baseMapKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(owner));

            D.adminLog("{" + entityName + ":" + userId + "} Spawning in new map finish! {" + newArea + ":" + baseMapKey + "} of user {" + owner == null ? "Null" : owner.userId + "}", D.ADMIN_LOG_TYPE.Visit);
            targetLocalPos = spawn == null
               ? SpawnManager.self.getDefaultLocalPosition(baseMapKey, true)
               : SpawnManager.self.getLocalPosition(baseMapKey, spawn, true);
         }

      } else {
         D.adminLog("{" + entityName + ":" + userId + "} Spawning in new map WITHOUT custom map Data! {" + newArea + "}", D.ADMIN_LOG_TYPE.Visit);
         targetLocalPos = spawn == null
            ? SpawnManager.self.getDefaultLocalPosition(newArea, true)
            : SpawnManager.self.getLocalPosition(newArea, spawn, true);

         if (spawn != null) {
            SpawnManager.SpawnData spawnData = SpawnManager.self.getMapSpawnData(newArea, spawn);
            if (spawnData != null) {
               adjustedNewFacingDirection = (Direction) spawnData.arriveFacing;
               D.adminLog("Override facing direction, Spawn is" + " " + newArea + " " + SpawnManager.self.getMapSpawnData(newArea, spawn) + " " + adjustedNewFacingDirection, D.ADMIN_LOG_TYPE.Warp);
            }
         }
      }

      // Override the local position if we are in an Open World Area
      if (WorldMapManager.self.isWorldMapArea(areaKey) && WorldMapManager.self.isWorldMapArea(newArea)) {
         targetLocalPos = transform.localPosition;
         float offset = 0.2f;

         if (newFacingDirection == Direction.North) {
            targetLocalPos = Util.mirrorY(targetLocalPos) + Vector2.up * offset;
         } else if (newFacingDirection == Direction.South) {
            targetLocalPos = Util.mirrorY(targetLocalPos) + Vector2.down * offset;
         } else if (newFacingDirection == Direction.East) {
            targetLocalPos = Util.mirrorX(targetLocalPos) + Vector2.right * offset;
         } else if (newFacingDirection == Direction.West) {
            targetLocalPos = Util.mirrorX(targetLocalPos) + Vector2.left * offset;
         }
      }

      spawnInNewMap(newArea, targetLocalPos, adjustedNewFacingDirection, -1, -1);
   }

   [Server]
   public void visitUserToPrivateLocation (int requesterUserId, int visitedUserId, UserLocationBundle targetLocation) {
      // If our player is in the same instance than the target, simply teleport
      if (ServerNetworkingManager.self.server.networkedPort.Value == targetLocation.serverPort && areaKey == targetLocation.areaKey && instanceId == targetLocation.instanceId) {
         D.adminLog("visitUserToLocation: Failed to warp to area {" + targetLocation.areaKey + "}, move instead", D.ADMIN_LOG_TYPE.Visit);
         moveToPosition(targetLocation.getLocalPosition());
         return;
      }

      if (CustomMapManager.isUserSpecificAreaKey(targetLocation.areaKey) && targetLocation.serverPort > 0) {
         // If instance id exists, select that server
         D.adminLog("visitUserToLocation: Visit Area: {" + targetLocation.areaKey + "} : {" + targetLocation.instanceId + "} : {" + targetLocation.serverPort + "}", D.ADMIN_LOG_TYPE.Visit);
         findBestServerAndWarp(targetLocation.areaKey, targetLocation.getLocalPosition(), -1, Direction.South, targetLocation.instanceId > 1 ? targetLocation.instanceId : -1, targetLocation.serverPort);
      } else {
         // Select next convenient server
         D.adminLog("visitUserToLocation: Failed to visit Area:{" + targetLocation.areaKey + "}, warp to town, InstanceId:{" + targetLocation.instanceId + "} Port:{" + targetLocation.serverPort + "}", D.ADMIN_LOG_TYPE.Visit);
         findBestServerAndWarp(targetLocation.areaKey, targetLocation.getLocalPosition(), -1, Direction.South, -1, -1);
      }
   }

   [Server]
   public void visitUserToLocation (int requesterUserId, UserLocationBundle targetLocation) {
      // If our player is in the same instance than the target, simply teleport
      if (ServerNetworkingManager.self.server.networkedPort.Value == targetLocation.serverPort && areaKey == targetLocation.areaKey && instanceId == targetLocation.instanceId) {
         D.adminLog("visitUserToLocation: Failed to warp to area {" + targetLocation.areaKey + "}, move instead", D.ADMIN_LOG_TYPE.Visit);
         moveToPosition(targetLocation.getLocalPosition());
         return;
      }

      // Had instance id check
      if (CustomMapManager.isUserSpecificAreaKey(targetLocation.areaKey) && targetLocation.serverPort > 0) {
         D.adminLog("visitUserToLocation: Visit Area: {" + targetLocation.areaKey + "} : {" + targetLocation.instanceId + "} : {" + targetLocation.serverPort + "}", D.ADMIN_LOG_TYPE.Visit);
         findBestServerAndWarp(targetLocation.areaKey, targetLocation.getLocalPosition(), -1, Direction.South, targetLocation.instanceId > 1 ? targetLocation.instanceId : -1, targetLocation.serverPort);
      } else {
         D.adminLog("visitUserToLocation: Failed to visit Area:{" + targetLocation.areaKey + "}, warp to town, InstanceId:{" + targetLocation.instanceId + "} Port:{" + targetLocation.serverPort + "}", D.ADMIN_LOG_TYPE.Visit);
         findBestServerAndWarp(targetLocation.areaKey, targetLocation.getLocalPosition(), -1, Direction.South, -1, -1);
      }
   }

   [Server]
   public void spawnInNewMap (string newArea, Vector2 newLocalPosition, Direction newFacingDirection, int instanceId, int serverPort) {
      // Only admins can warp to voyage areas without indicating the voyageId
      if (isAdmin() && (VoyageManager.isAnyLeagueArea(newArea) || VoyageManager.isTreasureSiteArea(newArea))) {
         VoyageManager.self.forceAdminWarpToVoyageAreas(this, newArea);
         return;
      }

      // Now that we know the target server, redirect them there
      findBestServerAndWarp(newArea, newLocalPosition, -1, newFacingDirection, instanceId, serverPort);
   }

   [Server]
   public void spawnInNewMap (int voyageId, string newArea, Direction newFacingDirection) {
      spawnInNewMap(voyageId, newArea, "", newFacingDirection);
   }

   [Server]
   public void spawnInNewMap (int voyageId, string newArea, string spawn, Direction newFacingDirection) {
      // Get the spawn position for the given spawn
      Vector2 spawnLocalPosition = spawn == ""
            ? SpawnManager.self.getDefaultLocalPosition(newArea, true)
            : SpawnManager.self.getLocalPosition(newArea, spawn, true);

      findBestServerAndWarp(newArea, spawnLocalPosition, voyageId, newFacingDirection, -1, -1);
   }

   [Server]
   public void findBestServerAndWarp (string newArea, Vector2 newLocalPosition, int voyageId, Direction newFacingDirection, int instanceId, int serverPort) {
      if (this.isAboutToWarpOnServer) {
         D.log($"The player {netId} is already being warped. Cannot warp again to {newArea}");
         return;
      }

      GameObject entityObject = this.gameObject;

      // Make a note that we're about to proceed with a warp
      this.isAboutToWarpOnServer = true;

      // Store the connection reference so that we don't lose it while on the background thread
      NetworkConnection connectionToClient = this.connectionToClient;

      if (isPlayerShip() && VoyageManager.isAnyLeagueArea(areaKey)) {
         if (VoyageManager.isAnyLeagueArea(newArea) || (isInGroup() && !isDead())) {
            // While the user is in a league or part of a group, the hp is persistent
            ((PlayerShipEntity) this).storeCurrentShipHealth();
         } else {
            // When leaving leagues or respawning, the hp is restored
            restoreMaxShipHealth();
         }
      }

      // If the player just entered an open world area, restore the health of the ships
      if (WorldMapManager.self.isWorldMapArea(newArea) && !WorldMapManager.self.isWorldMapArea(areaKey)) {
         restoreMaxShipHealth();
      }

      // Release any claim on the user
      ServerNetworkingManager.self.releasePlayerClaim(userId);

      // Update the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setNewLocalPosition(this.userId, newLocalPosition, newFacingDirection, newArea);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // If the current area is a treasure site, we must unregister this user from being inside
            if (VoyageManager.isTreasureSiteArea(areaKey)) {
               VoyageManager.self.unregisterUserFromTreasureSite(userId, instanceId);
            }

            // Remove the player from the current instance
            InstanceManager.self.removeEntityFromInstance(this);

            if (CustomMapManager.isUserSpecificAreaKey(areaKey) || CustomMapManager.isUserSpecificAreaKey(newArea)) {
               D.adminLog("D) Redirecting now! for Visit user {" + userId + "}{" + entityName + "}{" + instanceId + "}{" + serverPort + "}{" + newArea + ":" + areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
            }
            D.adminLog("NetEntity! Redirecting now! for user {" + userId + "}{" + entityName + "} Voyage ID: {" + voyageId + "}{" + instanceId + "}{" + serverPort + "}{" + newArea + ":" + areaKey + "}", D.ADMIN_LOG_TYPE.Redirecting);

            // Redirect the player to the best server
            MyNetworkManager.self.StartCoroutine(MyNetworkManager.self.CO_RedirectUser(this.connectionToClient, accountId, userId, entityName, voyageId, isSinglePlayer, newArea, areaKey, entityObject, instanceId, serverPort));
         });
      });
   }

   protected IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(this.areaKey) == null) {
         yield return 0;
      }

      Area area = AreaManager.self.getArea(this.areaKey);
      bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2) transform.position);
      setAreaParent(area, worldPositionStays);

      if (isLocalPlayer) {
         yield return null;

         // If the player is still outside of the area bounds, send him back home and show an error
         if (!area.cameraBounds.bounds.Contains((Vector2) transform.position)) {
            Global.player.Cmd_GoHome();
            D.error($"The player {entityName} position {transform.position} was outside of the area {area.areaKey} bounds, centered at {area.transform.position}");
            PanelManager.self.noticeScreen.show("Error when warping: the player was outside of the area bounds");
            yield break;
         }

         // Toggles the weather layer in the camera render if the player is inside buildings
         if (AreaManager.self.getArea(areaKey).isInterior) {
            WeatherManager.self.muteWeather();
         } else {
            WeatherManager.self.activateWeather();
         }

         // Wait until the loading screen is hidden
         while (PanelManager.self.loadingScreen.isShowing()) {
            yield return null;
         }

         // Show the Area name
         if (VoyageManager.isAnyLeagueArea(this.areaKey)) {
            LocationBanner.self.setText("League " + Voyage.getLeagueAreaName(getInstance().leagueIndex));
         } else {
            D.adminLog("User {" + entityName + " " + userId + "} Is Now in Area: {" + areaKey + "}!", D.ADMIN_LOG_TYPE.Visit);
            LocationBanner.self.setText(Area.getName(this.areaKey));
         }

         // Update the tutorial
         TutorialManager3.self.onUserSpawns(this.userId);

         // Trigger the tutorial
         if (VoyageManager.isLeagueArea(this.areaKey) || VoyageManager.isLeagueSeaBossArea(this.areaKey)) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.SpawnInLeagueNotLobby);
         } else if (VoyageManager.isAnyLeagueArea(this.areaKey)) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.SpawnInVoyage);
         }
         TutorialManager3.self.tryCompletingStepByLocation();

         // Update the instance status panel
         VoyageStatusPanel.self.onUserSpawn();

         // Setup the pvp structure status panel
         if (VoyageManager.isPvpArenaArea(area.areaKey)) {
            PvpStructureStatusPanel.self.onPlayerJoinedPvpGame();
            PvpStatPanel.self.onPlayerJoinedPvpGame();
         }

         // Signal the server
         rpc.Cmd_OnClientFinishedLoadingArea();
         float timeSinceRedirectMessage = (float) NetworkTime.time - ClientMessageManager.lastRedirectMessageTime;
         D.debug("[Timing] Client: from receiving redirection message to loading in area: " + areaKey + " took " + timeSinceRedirectMessage.ToString("F2") + "seconds.");

         // Check instance biome and play voyage sfx based on the biome
         while (InstanceManager.self.getInstance(instanceId) == null) {
            yield return 0;
         }

         Instance instanceReference = InstanceManager.self.getInstance(instanceId);

         SoundEffectManager.BgType bgParam = SoundEffectManager.self.getAreaBasedBgMusic(area.areaKey, instanceReference.biome);
         SoundEffectManager.AmbType ambienceParam = SoundEffectManager.self.getAreaBasedAmbience(area.areaKey, instanceReference.biome);
         SoundEffectManager.self.playBgMusic(bgParam, ambienceParam);
      }
   }

   #region NEWVISIT

   [Command]
   public void Cmd_VisitPrivateInstanceFarmById (int targetPlayerId, string areaKeyOverride) {
      visitPrivateInstanceFarmById(targetPlayerId, areaKeyOverride);
   }

   [Command]
   public void Cmd_VisitPrivateInstanceHouseById (int targetPlayerId, string areaKeyOverride) {
      visitPrivateInstanceHouseById(targetPlayerId, areaKeyOverride);
   }

   [Command]
   public void Cmd_PlayerVisitPrivateInstanceFarm (string targetPlayerName, string areaKeyOverride) {
      visitPrivateInstanceFarm(targetPlayerName, areaKeyOverride);
   }

   [Command]
   public void Cmd_PlayerVisitPrivateInstanceHouse (string targetPlayerName, string areaKeyOverride) {
      visitPrivateInstanceHouse(targetPlayerName, areaKeyOverride);
   }

   [Server]
   public void visitPrivateInstanceFarmById (int visitedUserId, string areaKeyOverride, string spawnTarget = "", Direction facing = Direction.South) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to retrieve the target info
         UserInfo visitedUserInfo = DB_Main.getUserInfoById(visitedUserId);
         string visitAreaKey = CustomFarmManager.GROUP_AREA_KEY + "_user" + visitedUserInfo.userId;

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            finalizeVisitParameters(visitedUserInfo, visitAreaKey, spawnTarget);
         });
      });
   }

   [Server]
   public void visitPrivateInstanceHouseById (int visitedUserId, string areaKeyOverride, string spawnTarget = "", Direction facing = Direction.South) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to retrieve the target info
         UserInfo visitedUserInfo = DB_Main.getUserInfoById(visitedUserId);
         string visitAreaKey = CustomHouseManager.GROUP_AREA_KEY + "_user" + visitedUserInfo.userId;

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            finalizeVisitParameters(visitedUserInfo, visitAreaKey, spawnTarget);
         });
      });
   }

   [Server]
   public void visitPrivateInstanceFarm (string visitedUserName, string areaKeyOverride, string spawnTarget = "", Direction facing = Direction.South) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to retrieve the target info
         UserInfo visitedUserInfo = DB_Main.getUserInfo(visitedUserName);
         string visitAreaKey = CustomFarmManager.GROUP_AREA_KEY + "_user" + visitedUserInfo.userId;

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            finalizeVisitParameters(visitedUserInfo, visitAreaKey, spawnTarget);
         });
      });
   }

   [Server]
   public void visitPrivateInstanceHouse (string visitedUserName, string areaKeyOverride, string spawnTarget = "", Direction facing = Direction.South) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to retrieve the target info
         UserInfo visitedUserInfo = DB_Main.getUserInfo(visitedUserName);
         string visitAreaKey = CustomHouseManager.GROUP_AREA_KEY + "_user" + visitedUserInfo.userId;

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            finalizeVisitParameters(visitedUserInfo, visitAreaKey, spawnTarget);
         });
      });
   }

   [Server]
   public void finalizeVisitParameters (UserInfo visitedUserInfo, string areaKey, string spawnTarget) {
      if (visitedUserInfo == null) {
         D.adminLog("Null Player: {" + visitedUserInfo.username + "}", D.ADMIN_LOG_TYPE.Visit);
         cancelUserVisit();
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, this, "The player " + visitedUserInfo.username + " doesn't exist!");
         return;
      }

      D.adminLog("Visitor: {" + this.userId + "} Will now searching for user to visit! " +
         "Visited: {" + visitedUserInfo.userId + ":" + visitedUserInfo.username + "} " +
         "From: {" + this.areaKey + "} To: {" + areaKey + "}", D.ADMIN_LOG_TYPE.Visit);

      // Make sure the visited user already selected their own farm and house
      if ((visitedUserInfo.customFarmBaseId < 1 && areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY))
         || (visitedUserInfo.customHouseBaseId < 1 && areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY))) {
         D.adminLog("Null Map Id: {" + visitedUserInfo.username + "} {" + areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
         cancelUserVisit();
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GeneralPopup, this, visitedUserInfo.username + " has not selected a house yet, so they can not receive visitors!");
         return;
      }

      string spawnTargetOverride = spawnTarget;
      D.debug("fetch now: " + visitedUserInfo.customHouseBaseId + " " + visitedUserInfo.customFarmBaseId + " target area: " + areaKey + " sourceArea: " + this.areaKey);
      List<MapCreationTool.Serialization.MapSpawn> spawnIds = new List<MapCreationTool.Serialization.MapSpawn>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // If target area is house, fetch all 
         if (areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY)) {
            spawnIds = DB_Main.getMapSpawnsById(visitedUserInfo.customHouseBaseId);
         } else if (areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY)) {
            spawnIds = DB_Main.getMapSpawnsById(visitedUserInfo.customFarmBaseId);
         } else {
            D.debug("Fetched No Spawn ids for area! " + areaKey);
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (spawnIds.Count < 1 || (!CustomMapManager.isPrivateCustomArea(areaKey))) {
               // Redirect to the master server to find the location of the target user
               ServerNetworkingManager.self.findUserPrivateAreaToVisit(this.userId, visitedUserInfo.userId, areaKey, spawnTargetOverride, facing);
            } else {
               if (this.areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY)) {
                  List<MapCreationTool.Serialization.MapSpawn> newList = spawnIds.FindAll(_ => _.name != "main");
                  if (newList.Count < 1) {
                     spawnTargetOverride = "main";
                  } else {
                     spawnTargetOverride = newList.ChooseRandom().name;
                  }
               } else if (this.areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY)) {
                  spawnTargetOverride = "main";
               } else {
                  spawnTargetOverride = "main";
               }

               if (spawnTargetOverride.Length < 1) {
                  spawnTargetOverride = spawnTarget;
               }

               // Redirect to the master server to find the location of the target user
               ServerNetworkingManager.self.findUserPrivateAreaToVisit(this.userId, visitedUserInfo.userId, areaKey, spawnTargetOverride, facing);
            }
         });
      });
   }

   #endregion

   [Server]
   public void denyUserVisit (int userId) {
      D.adminLog("Server User {" + userId + "} Visit Denied!", D.ADMIN_LOG_TYPE.Visit);
      Target_ReceiveNormalChat("You are not allowed to visit this user! ", ChatInfo.Type.System);
      cancelUserVisit();
   }

   [Server]
   public void privateInstanceDoestNotExist (int visitorUserId, string areaKey, Vector2 localPosition) {
      D.adminLog("Server User {" + visitorUserId + "} Visit Does not exist! Try to generate new one {" + areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
      privateInstanceDoesntExist(areaKey, localPosition);
   }

   [Server]
   public void cancelUserVisit () {
      D.adminLog("User {" + entityName + ":" + userId + "} Visit Cancelled as Server! Spawning in Starting Town", D.ADMIN_LOG_TYPE.Visit);
      spawnInNewMap(Area.STARTING_TOWN);
   }

   [Server]
   public void privateInstanceDoesntExist (string areaKey, Vector2 localPosition) {
      D.adminLog("User {" + entityName + ":" + userId + " : " + areaKey + "} Visit Cancelled as Server! Finding best server to warp into", D.ADMIN_LOG_TYPE.Visit);
      int visitedUserId = CustomMapManager.getUserId(areaKey);
      List<MapCreationTool.Serialization.MapSpawn> spawnIds = new List<MapCreationTool.Serialization.MapSpawn>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         if (CustomMapManager.isUserSpecificAreaKey(areaKey)) {
            UserInfo visitedUserInfo = DB_Main.getUserInfoById(visitedUserId);
            if (areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY)) {
               spawnIds = DB_Main.getMapSpawnsById(visitedUserInfo.customHouseBaseId);
            } else if (areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY)) {
               spawnIds = DB_Main.getMapSpawnsById(visitedUserInfo.customFarmBaseId);
            } else {
               D.debug("Fetched No Spawn ids for area! " + areaKey);
            }

            if (spawnIds.Find(_ => _.name == "main") != null) {
               MapCreationTool.Serialization.MapSpawn selectedSpawn = spawnIds.Find(_ => _.name == "main");
               localPosition = new Vector2(selectedSpawn.posX, selectedSpawn.posY);
            }
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            findBestServerAndWarp(areaKey, localPosition, -1, Direction.South, -1, -1);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveCancelUserVisit () {
      D.adminLog("User Visit Cancelled as Client! " + PanelManager.self.loadingScreen.isShowing(), D.ADMIN_LOG_TYPE.Visit);
      if (PanelManager.self.loadingScreen.isShowing()) {
         PanelManager.self.loadingScreen.hide();
      } else {
         D.adminLog("Warping back to town!", D.ADMIN_LOG_TYPE.Visit);
         Cmd_GoHome();
      }
   }

   [TargetRpc]
   public void Target_ReceiveContextMenuContent (int targetUserId, string targetName, int targetVoyageGroupId, int targetGuildId) {
      PanelManager.self.contextMenuPanel.processDefaultMenuForUser(null, targetUserId, targetName, targetGuildId, targetVoyageGroupId);
   }

   public Vector3 getProjectedPosition (float afterSeconds) {
      return _body.position + _body.velocity * afterSeconds;
   }

   public bool canPerformAction (GuildPermission action) {
      return ((this.guildPermissions & (int) action) != 0);
   }

   public bool canInviteGuild (NetEntity targetEntity) {
      return this.guildId > 0 && (targetEntity == null || (targetEntity != null && targetEntity.guildId == 0)) && canPerformAction(GuildPermission.Invite);
   }

   public bool canInvitePracticeDuel (NetEntity targetEntity) {
      return targetEntity != null && !targetEntity.isInBattle();
   }

   protected NetEntity getClickedBody () {
      // Don't allow to click on body if any panel is active
      if (Util.isAnyUiPanelActive()) {
         return null;
      }

      NetEntity entityHovered = null;
      foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
         if (entity.isMouseOver()) {
            entityHovered = entity;
            break;
         }
      }

      if (entityHovered != null) {
         return entityHovered;
      } else {
         return null;
      }
   }

   public bool isBouncingOnWeb () {
      if (!_activeWeb) {
         return false;
      }

      float timeSinceLastBounced = (float) (NetworkTime.time - _webBounceStartTime);
      return (timeSinceLastBounced < getWebBounceDuration());
   }

   protected float getWebBounceDuration () {
      return (_isDoingHalfBounce) ? _activeWeb.getBounceDuration() / 2.0f : _activeWeb.getBounceDuration();
   }

   public bool isInGroup () {
      return VoyageGroupManager.isInGroup(this);
   }

   [Server]
   public bool tryGetGroup (out VoyageGroupInfo groupInfo) {
      groupInfo = default;
      if (VoyageGroupManager.isInGroup(this) && VoyageGroupManager.self.tryGetGroupById(voyageGroupId, out VoyageGroupInfo g)) {
         groupInfo = g;
         return true;
      } else {
         return false;
      }
   }

   [Server]
   public bool tryGetVoyage (out Voyage voyage) {
      voyage = default;
      if (VoyageGroupManager.isInGroup(this) && VoyageManager.self.tryGetVoyageForGroup(this.voyageGroupId, out Voyage v)) {
         voyage = v;
         return true;
      } else {
         return false;
      }
   }

   protected bool tryToOpenChest () {
      // Max collisions to check
      const int MAX_COLLISION_COUNT = 32;

      Collider2D[] hits = Physics2D.OverlapPointAll(Util.getMousePos());
      int collisionCount = 0;

      foreach (Collider2D hit in hits) {
         if (collisionCount > MAX_COLLISION_COUNT) {
            break;
         }
         collisionCount++;

         Collider2D[] colliders = hit.GetComponents<Collider2D>();
         bool hitInBounds = false;
         foreach (Collider2D collider in colliders) {
            // If a collider contains the mouse
            float distToMouse = ((Vector2) (collider.transform.position - Util.getMousePos())).magnitude;
            if (distToMouse < collider.bounds.size.x) {
               hitInBounds = true;
            }
         }

         if (!hitInBounds) {
            continue;
         }

         // If we clicked on a chest, interact with it
         TreasureChest chest = hit.GetComponent<TreasureChest>();
         if (chest && !chest.hasBeenOpened() && chest.chestType != ChestSpawnType.Site && chest.autoOpenCollider == hit) {
            chest.sendOpenRequest();
            return true;
         }
      }

      return false;
   }

   public Canvas getClickableBoxCanvas () {
      return _clickableBoxCanvas;
   }

   public uint lastAttackerId () {
      return _lastAttackerNetId;
   }

   public double lastAttackedTime () {
      if (_lastAttackerNetId == 0) {
         return -IN_COMBAT_STATUS_DURATION;
      }

      if (_attackers.ContainsKey(_lastAttackerNetId)) {
         return _attackers[_lastAttackerNetId];
      }

      return -IN_COMBAT_STATUS_DURATION;
   }

   public bool isInCombat () {
      return (getTimeSinceAttacked() < IN_COMBAT_STATUS_DURATION);
   }

   public float getTimeSinceAttacked () {
      return (float) (NetworkTime.time - lastAttackedTime());
   }

   public SpriteOutline getOutline () {
      return _outline;
   }

   protected virtual void webBounceUpdate () { }

   protected virtual void onStartMoving () { }

   protected virtual void onEndMoving () { }

   public virtual bool isPlayerShip () { return false; }

   public virtual bool isBotShip () { return false; }

   public virtual bool isSeaStructure () { return false; }

   public virtual bool isPvpCaptureTargetHolder () { return false; }

   public virtual bool isPvpCaptureTarget () { return false; }

   public virtual bool isSeaMonster () { return false; }

   public virtual bool isSeaMonsterMinion () { return false; }

   public virtual bool isLandEnemy () { return false; }

   public virtual bool isPvpTower () { return false; }

   protected virtual void onMaxHealthChanged (int oldValue, int newValue) { }

   protected virtual void autoMove () { }

   [TargetRpc]
   public void Target_ReceiveSilverCurrency (NetworkConnection connection, int silverCount, SilverManager.SilverRewardReason rewardReason) {
      Transform bodyTx = Global.player.transform;

      if (Global.player.isPlayerShip()) {
         bodyTx = Global.player.getPlayerShipEntity().transform;
      }

      NetEntity playerBody = Global.player.getPlayerBodyEntity();

      if (playerBody != null) {
         bodyTx = playerBody.transform;
      }

      Vector3 pos = bodyTx.position;
      ReceiveSilverCurrencyImpl(silverCount, rewardReason, pos);
   }

   public void ReceiveSilverCurrencyImpl (int silverCount, SilverManager.SilverRewardReason rewardReason, Vector3 targetPos) {
      Vector3 pos = targetPos;

      if (silverCount > 0) {
         // Move the already spawned messages a little up
         FloatingCanvas[] spawnedCanvases = GameObject.FindObjectsOfType<FloatingCanvas>();
         if (spawnedCanvases != null && spawnedCanvases.Length > 0) {
            foreach (FloatingCanvas canvas in spawnedCanvases) {
               if (canvas.customTag == Global.player.userId.ToString()) {
                  float interDiffTransform = canvas.transform.position.y - pos.y;

                  // The item notification is targeted to the current player
                  if (canvas.TryGetComponent(out RectTransform rectTransform)) {
                     float nudge = rectTransform.rect.height - interDiffTransform;
                     if (nudge > 0) {
                        Vector3 scaledRect = rectTransform.localScale * (nudge);
                        canvas.transform.position = canvas.transform.position + new Vector3(.0f, scaledRect.y, .0f);
                     }
                  }
               }
            }
         }
      }

      // Show a message that they gained some XP along with the item they received
      GameObject gainItemCanvas = Instantiate(PrefabsManager.self.itemReceivedPrefab);
      gainItemCanvas.transform.position = pos;
      gainItemCanvas.GetComponentInChildren<TextMeshProUGUI>().text = silverCount.ToString();

      FloatingCanvas floatingCanvas = gainItemCanvas.GetComponentInChildren<FloatingCanvas>();
      floatingCanvas.customTag = Global.player.userId.ToString();
      floatingCanvas.lifetime *= 2;
      floatingCanvas.riseSpeed /= 3;

      if (silverCount < 0) {
         gainItemCanvas.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
      }

      if (silverCount > 0) {
         gainItemCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "+ " + silverCount;
      }

      // Adjust the displayed icon based on the reason for the award
      PvpStatPanel panel = (PvpStatPanel) PanelManager.self.get(Panel.Type.PvpScoreBoard);

      if (rewardReason == SilverManager.SilverRewardReason.Kill || rewardReason == SilverManager.SilverRewardReason.Death || rewardReason == SilverManager.SilverRewardReason.Heal || rewardReason == SilverManager.SilverRewardReason.None) {
         gainItemCanvas.GetComponentInChildren<Image>().sprite = panel.silverIcon;
      } else if (rewardReason == SilverManager.SilverRewardReason.Assist) {
         gainItemCanvas.GetComponentInChildren<Image>().sprite = panel.assistSilverIcon;
      }

      gainItemCanvas.GetComponentInChildren<Image>().SetNativeSize();

      // Play SFX
      if (silverCount > 0) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.GAIN_SILVER, targetPos);
      }

      // Update the Silver indicator
      PvpStatusPanel.self.addSilver(silverCount);
   }

   [ClientRpc]
   public void Rpc_BroadcastUpdatedCrop (CropInfo cropInfo, bool justGrew, bool isQuickGrow) {
      this.cropManager.receiveUpdatedCrop(cropInfo, justGrew, isQuickGrow);
   }

   [ClientRpc]
   public void Rpc_BroadcastHarvestedCrop (CropInfo cropInfo) {
      CropSpot cropSpot = CropSpotManager.self.getCropSpot(cropInfo.cropNumber, cropInfo.areaKey);
      Vector3 effectSpawnPos = cropSpot.cropPickupLocation;

      // Show some effects to notify client that the crop spot is now available again
      EffectManager.self.create(Effect.Type.Crop_Harvest, effectSpawnPos);
      EffectManager.self.create(Effect.Type.Crop_Dirt_Large, effectSpawnPos);

      // Then delete the crop
      if (cropSpot.crop != null) {
         Destroy(cropSpot.crop.gameObject);
      }

      // If the this is the player who harvested the crop, trigger the tutorial
      if (cropInfo.userId == Global.player.userId) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.HarvestCrop);
      }
   }

   [Command]
   public void Cmd_BroadcastCropProjectile (CropInfo cropInfo) {
      bool hasFarmingPermissions = (AreaManager.self.isFarmOfUser(cropInfo.areaKey, this.userId) || CustomGuildMapManager.canUserFarm(cropInfo.areaKey, this));
      if (hasFarmingPermissions) {
         Rpc_BroadcastCropProjectile(cropInfo, userId);
      } else {
         D.error("Client tried to allow harvesting of a crop that it doesn't have permission to farm.");
      }
   }

   [ClientRpc]
   public void Rpc_BroadcastCropProjectile (CropInfo cropInfo, int harvesterUserId) {
      CropSpot cropSpot = CropSpotManager.self.getCropSpot(cropInfo.cropNumber, cropInfo.areaKey);

      Crop harvestedCrop = cropSpot.crop;
      harvestedCrop.hideCrop();
      ExplosionManager.createFarmingParticle(Weapon.ActionType.HarvestCrop, cropSpot.transform.position, 1.5f, 4, false);

      CropProjectile cropProjectile = Instantiate(PrefabsManager.self.cropProjectilePrefab, AreaManager.self.getArea(areaKey).transform).GetComponent<CropProjectile>();
      cropProjectile.cropReference = harvestedCrop;
      cropProjectile.transform.position = cropSpot.transform.position;
      Vector2 dir = (cropSpot.transform.position - transform.position).normalized;
      cropProjectile.setSprite(harvestedCrop.cropType);
      cropProjectile.init(cropSpot.transform.position, dir, cropSpot);
      cropProjectile.harvesterUserId = harvesterUserId;
   }

   [Server]
   public void restoreMaxShipHealth () {
      Util.tryToRunInServerBackground(() => {
         ShipInfo shipInfo = DB_Main.getShipInfoForUser(userId);

         if (shipInfo != null) {
            DB_Main.restoreShipMaxHealth(shipInfo.shipId);
         }
      });
   }

   public virtual void toggleWarpInProgressEffect (bool show) {

   }

   // Web jump sound effect
   [Command]
   public void Cmd_PlayWebSound (Vector3 position) {
      Rpc_PlayWebSound(position);
   }

   [ClientRpc]
   public void Rpc_PlayWebSound (Vector3 position) {
      if (!isLocalPlayer) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.WEB_JUMP, position);
      }
   }

   #region Private Variables

   // Whether we should automatically move around
   protected bool _autoMove = false;

   // The key of the previous area that we were in
   protected string _previousAreaKey;

   // Our various component references
   protected Rigidbody2D _body;
   protected ClickableBox _clickableBox;
   protected Canvas _clickableBoxCanvas;
   protected SpriteOutline _outline;
   protected NetworkLerpRigidbody2D _networkLerp;

   [Header("PvtComponents")]

   [SerializeField]
   protected List<Animator> _ignoredAnimators = new List<Animator>();
   [SerializeField]
   protected List<Animator> _animators = new List<Animator>();
   protected List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

   // The time at which we last applied a change to our movement
   protected double _lastMoveChangeTime;

   // The time at which we last changed our facing direction
   protected double _lastFacingChangeTime;

   // The time at which we last changed our movement angle
   protected double _lastAngleChangeTime;

   // The time at which we last sent our input to the server
   protected double _lastInputChangeTime;

   // The time at which we last sent our aim angle to the server
   protected double _lastAimChangeTime;

   // Entities that have attacked us and the time when they attacked
   protected Dictionary<uint, double> _attackers = new Dictionary<uint, double>();

   // Entities that have attacked us and the time when they attacked (Does not Clear)
   protected Dictionary<uint, DamageRecord> _totalAttackers = new Dictionary<uint, DamageRecord>();

   // The netId of the last entity that attacked us
   protected uint _lastAttackerNetId = 0;

   // The initial scale of the entity's shadow
   protected Vector3 _shadowInitialScale;

   // Did the Entity move last frame?
   private bool _movedLastFrame;

   // The sprite used for the body in the previous frame
   private Sprite _previousBodySprite;

   // The time at which the body last changed sprites
   private float _lastBodySpriteChangetime;

   // Whether the player is climbing
   protected bool _isClimbing = false;

   // Keep a reference to the last instance accessed with getInstance
   private Instance _lastInstance = null;

   // Controllers, that are controlling the entity or are scheduled to
   private List<TemporaryController> _temporaryControllers = new List<TemporaryController>();

   // Main collider of the entity, used for collisions
   private CircleCollider2D _mainCollider;

   // The timestamp of our last web bounce start
   protected float _webBounceStartTime = 0.0f;

   // Whether the player is doing a half bounce on a web, or a full bounce
   protected bool _isDoingHalfBounce = false;

   // The web that we are currently bouncing on, if we have one
   protected SpiderWeb _activeWeb = null;

   // The last job xp update received from the server
   protected Jobs _lastJobsXp;

   // The farm xp gained in the last seconds, that has not been displayed yet
   protected int _accumulatedFarmingXp = 0;

   // The time at which the last farm xp notification has been shown
   protected double _lastFarmingXpNotificationTime = 0;

   #endregion
}
