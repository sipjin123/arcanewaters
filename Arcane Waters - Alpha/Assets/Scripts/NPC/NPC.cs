using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using MapCreationTool.Serialization;
using System;
using Random = UnityEngine.Random;
using AStar;

public class NPC : NetEntity, IMapEditorDataReceiver
{
   #region Public Variables

   // How close we have to be in order to talk to the NPC
   public static float TALK_DISTANCE = .65f;

   // The Types of different NPCs
   public enum Type
   {
      None = 0,
      Blackbeard = 1, Blacksmith = 2, Fatty = 3, Feather = 4, Fisherman = 5,
      Gardener = 6, Glasses = 7, Gramps = 8, Hammer = 9, Headband = 10,
      ItemShop = 11, Mapper = 12, Monocle = 13, Parrot = 14, Patch = 15,
      Pegleg = 16, Seagull = 17, Shipyard = 18, Shroom = 19, Skullhat = 20,
      Stripes = 21, Vest = 22, Dog = 23, Lizard = 24,

      ExplorerGuy = 25, DesertGuy = 26, Topknot = 27, Snowgirl = 28, SkullHat = 29,
      ShamanGirl = 30, Pyromancer = 31, Mushroomdruid = 32, Lizard_Shaman = 33, Lizard_Armored = 34, Golem = 35,
   }

   [SyncVar]
   // The Type of NPC this is
   public Type npcType;

   // The speed that we move at
   public float moveSpeed = 3f;

   [SyncVar]
   // The unique id assigned to this npc
   public int npcId;

   // The current trade gossip for this NPC, changes over time
   public string tradeGossip;

   // Determines if this npc is spawned at debug mode
   public bool isDebug = false;

   // Determines if this npc is a shop npc
   public bool isShopNpc;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      _startPosition = transform.position;
      _graphicRaycaster = GetComponentInChildren<GraphicRaycaster>();
      _shopTrigger = GetComponent<ShopTrigger>();

      if (this.gameObject.HasComponent<Animator>()) {
         _animators.Add(GetComponent<Animator>());
      }
      if (this.gameObject.HasComponent<SpriteRenderer>()) {
         _renderers.Add(GetComponent<SpriteRenderer>());
      }

      if (nameText != null) {
         gameObject.name = "NPC_" + nameText.text;
      }
   }

   protected override void Start () {
      base.Start();

      // Initially hides name ui for npc name
      Util.setAlpha(nameText, 0f);

      foreach (Animator animator in _animators) {
         animator.SetInteger("facing", (int) facing);
      }

      // Add some move targets
      _moveTargets.Add(_startPosition + new Vector2(.3f, 0f));
      _moveTargets.Add(_startPosition + new Vector2(-.3f, 0f));
      _moveTargets.Add(_startPosition + new Vector2(0f, .3f));
      _moveTargets.Add(_startPosition + new Vector2(0f, -.3f));

      // Continually pick new move targets
      if (isServer) {
         _gridReference = AreaManager.self.getArea(areaKey).pathfindingGrid;
         if (_gridReference == null) {
            D.error("There has to be an AStarGrid Script attached to the MapTemplate Prefab");
         }

         _pathfindingReference = GetComponent<Pathfinding>();
         if (_pathfindingReference == null) {
            D.error("There has to be a Pathfinding Script attached to the NPC Prefab");
         }
         _pathfindingReference.gridReference = _gridReference;

         if (!isShopNpc) {
            StartCoroutine(generateNewWaypoints(6f + Random.Range(-1f, 1f)));
         } else {
            facing = Direction.South;
         }
      } else {
         setupClientSideSprites();
      }

      // Get faction from type
      _faction = getFactionFromType(npcType);

      // Update our various text responses
      InvokeRepeating("updateTradeGossip", 0f, 60 * 60);

      // Keep track of the NPC in the Manager
      NPCManager.self.storeNPC(this);

      // Set the name
      if (nameText != null) {
         // nameText.text = "[" + npcType + "]";
         // setNameColor(nameText, npcType);
      }

      if (AreaManager.self.getArea(areaKey) != null) {
         transform.SetParent(AreaManager.self.getArea(areaKey).npcParent);
      }
   }

   private void setupClientSideSprites () {
      string spriteAddress = "Assets/Sprites/NPCs/Bodies/" + this.npcType + "/";
      List<ImageManager.ImageData> newSprites = ImageManager.getSpritesInDirectory(spriteAddress);
      if (newSprites.Count > 0) {
         GetComponent<SpriteRenderer>().sprite = newSprites[0].sprites[0];

         // If we have a sprite swapper, we want to check that instead
         Texture2D newTexture = newSprites[0].texture2D;
         if (newTexture) {
            SpriteSwap swapper = GetComponent<SpriteSwap>();
            swapper.newTexture = newTexture;
         }
      }
   }

   public void initData () {
      NPCData npcData = NPCManager.self.getNPCData(npcId);

      // Sprite path insert here
      if (npcData != null) {
         // Set npc name and specialty
         _npcName = npcData.name;
         _specialty = npcData.specialty;
         if (nameText != null) {
            nameText.text = _npcName;
         }
         try {
            GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(npcData.spritePath);
         } catch {
            D.log("Cant get Sprite for NPC: " + this.npcId);
            GetComponent<SpriteSwap>().newTexture = NPCManager.self.defaultNpcBodySprite.texture;
         }
      } else {
         D.log("Cant get Data for NPC: " + npcId);
      }

      if (GetComponent<SpriteSwap>().newTexture.name.Contains("empty")) {
         D.log("Invalid NPC Path, please complete details in NPC Editor");
         GetComponent<SpriteSwap>().newTexture = NPCManager.self.defaultNpcBodySprite.texture;
      }
   }

   protected override void Update () {
      base.Update();

      // Disable our clickable canvas while a panel is showing
      if (_graphicRaycaster != null) {
         _graphicRaycaster.gameObject.SetActive(!PanelManager.self.hasPanelInStack());
      }

      // Only show our outline when the mouse is over us
      handleSpriteOutline();

      if (isServer) {
         Vector2 direction;
         if (_currentPath.Count > 0) {
            direction = (Vector2) _currentPath[0].vPosition - (Vector2) transform.position;
         } else {
            direction = Util.getDirectionFromFacing(facing);
         }
         // Figure out the direction we want to face

         // If this NPC is talking to this client's player, then face them
         if (isTalkingToGlobalPlayer()) {
            direction = Global.player.transform.position - transform.position;
         }

         // Calculate an angle for that direction
         float angle = Util.angle(direction);

         // Set our facing direction based on that angle
         facing = hasDiagonals ? Util.getFacingWithDiagonals(angle) : Util.getFacing(angle);

         // Pass our angle and velocity on to the Animator
         foreach (Animator animator in _animators) {
            animator.SetFloat("velocityX", _body.velocity.x);
            animator.SetFloat("velocityY", _body.velocity.y);
            animator.SetBool("isMoving", _body.velocity.magnitude > .01f);
            animator.SetInteger("facing", (int) facing);
         }
      }

      // Check if we're showing a West sprite
      bool isFacingWest = facing == Direction.West || facing == Direction.NorthWest || facing == Direction.SouthWest;

      // Flip our sprite renderer if we're going west
      foreach (SpriteRenderer renderer in _renderers) {
         renderer.flipX = isFacingWest;
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // If we're talking to the player, don't move
      if (isTalkingToGlobalPlayer()) {
         return;
      }

      if (_currentPath.Count > 0) {
         // Move towards our current waypoint
         Vector2 waypointDirection = _currentPath[0].vPosition - transform.position;
         _body.AddForce(waypointDirection.normalized * moveSpeed);
         _lastMoveChangeTime = Time.time;

         // Clears a node as the unit passes by
         float distanceToWaypoint = Vector2.Distance(_currentPath[0].vPosition, transform.position);
         if (distanceToWaypoint < .1f) {
            _currentPath.RemoveAt(0);
         }
      }
   }

   public void clientClickedMe () {
      if (Global.player == null || _clickableBox == null || Global.isInBattle()) {
         return;
      }

      // Only works when the player is close enough
      if (Vector2.Distance(transform.position, Global.player.transform.position) > TALK_DISTANCE) {
         Instantiate(PrefabsManager.self.tooFarPrefab, transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      if (_shopTrigger != null) {
         // If this is a Shop NPC, then show the appropriate panel
         switch (_shopTrigger.panelType) {
            case Panel.Type.Adventure:
               AdventureShopScreen adventurePanel = (AdventureShopScreen) PanelManager.self.get(_shopTrigger.panelType);
               adventurePanel.shopName = _shopName;
               break;
            case Panel.Type.Shipyard:
               ShipyardScreen shipyardPanel = (ShipyardScreen) PanelManager.self.get(_shopTrigger.panelType);
               shipyardPanel.shopName = _shopName;
               break;
            case Panel.Type.Merchant:
               MerchantScreen merchantPanel = (MerchantScreen) PanelManager.self.get(_shopTrigger.panelType);
               merchantPanel.shopName = _shopName;
               break;
         } 
         PanelManager.self.pushIfNotShowing(_shopTrigger.panelType);
      } else {
         // Send a request to the server to get the npc panel info
         Global.player.rpc.Cmd_RequestNPCQuestSelectionListFromServer(npcId);
      }
   }

   public bool isCloseToGlobalPlayer () {
      if (Global.player == null) {
         return false;
      }

      return Vector2.Distance(Global.player.transform.position, transform.position) < TALK_DISTANCE;
   }

   public bool isTalkingToGlobalPlayer () {
      return PanelManager.self.get(Panel.Type.NPC_Panel).isShowing() && isCloseToGlobalPlayer();
   }

   [Server]
   protected IEnumerator generateNewWaypoints (float moveAgainDelay) {
      // Initial delay
      float waitTime = 1.0f;
      while (true) {
         yield return new WaitForSeconds(waitTime);

         if (_currentPath.Count > 0) {
            // There's still points left of the old path, wait 1 second and check again if there's no more waypoints
            waitTime = 1.0f;
            continue;
         }

         _currentPath = _pathfindingReference.findPathNowInit(transform.position, _moveTargets.ChooseRandom());
         if (_currentPath == null || _currentPath.Count <= 0) {
            // Invalid Path, attempt again after 1 second
            waitTime = 1.0f;
            continue;
         }

         // We have a new path, set to normal delay
         waitTime = moveAgainDelay;
      }
   }

   public static void setNameColor (Text nameText, Type npcType) {
      Color textColor = Color.white;
      Color outlineColor = Color.black;

      switch (getFactionFromType(npcType)) {
         case Faction.Type.Pirates:
            textColor = Color.black;
            outlineColor = Color.white;
            break;
         case Faction.Type.Privateers:
            textColor = Color.cyan;
            break;
         case Faction.Type.Merchants:
            textColor = Color.yellow;
            break;
      }

      nameText.color = textColor;
      nameText.GetComponent<Outline>().effectColor = outlineColor;
      nameText.GetComponent<Shadow>().effectColor = outlineColor;
   }

   public Faction.Type getFaction () {
      // Try to get the faction from the data file
      Faction.Type faction = NPCManager.self.getFaction(npcId);

      // If the faction is not defined in the file, use the default one
      if (faction == Faction.Type.None) {
         faction = _faction;
      }
      return faction;
   }

   public static Faction.Type getFactionFromType (Type npcType) {
      switch (npcType) {
         case Type.Blackbeard:
         case Type.Headband:
         case Type.Patch:
            return Faction.Type.Pirates;
         case Type.Stripes:
         case Type.Skullhat:
         case Type.Fatty:
            return Faction.Type.Pillagers;
         case Type.Blacksmith:
         case Type.ItemShop:
         case Type.Hammer:
         case Type.Shipyard:
            return Faction.Type.Builders;
         case Type.Feather:
         case Type.Pegleg:
            return Faction.Type.Privateers;
         case Type.Fisherman:
         case Type.Gardener:
         case Type.Shroom:
            return Faction.Type.Naturalists;
         case Type.Glasses:
         case Type.Monocle:
         case Type.Vest:
            return Faction.Type.Merchants;
         case Type.Mapper:
            return Faction.Type.Cartographers;
         default:
            return Faction.Type.Neutral;
      }
   }

   public Specialty.Type getSpecialty () {
      // Try to get the specialty from the data file
      Specialty.Type specialty = NPCManager.self.getSpecialty(npcId);

      // If the specialty is not defined in the file, use the default one
      if (specialty == Specialty.Type.None) {
         specialty = _specialty;
      }

      return specialty;
   }

   protected Specialty.Type getRandomSpecialty () {
      Area area = GetComponentInParent<Area>();
      List<Specialty.Type> specialties = getPossibleSpecialties(getFaction());
      int randomIndex = (Area.getAreaId(areaKey) * 50) + (int) npcType;
      randomIndex %= specialties.Count;
      return specialties[randomIndex];
   }

   public static List<Specialty.Type> getPossibleSpecialties (Faction.Type factionType) {
      switch (factionType) {
         case Faction.Type.Builders:
            return new List<Specialty.Type>() { Specialty.Type.Crafter, Specialty.Type.Adventurer, Specialty.Type.Merchant };
         case Faction.Type.Cartographers:
            return new List<Specialty.Type>() { Specialty.Type.Adventurer, Specialty.Type.Sailor, Specialty.Type.Treasure };
         case Faction.Type.Merchants:
            return new List<Specialty.Type>() { Specialty.Type.Merchant, Specialty.Type.Sailor, Specialty.Type.Adventurer };
         case Faction.Type.Naturalists:
            return new List<Specialty.Type>() { Specialty.Type.Adventurer, Specialty.Type.Farmer };
         case Faction.Type.Pillagers:
         case Faction.Type.Pirates:
         case Faction.Type.Privateers:
            return new List<Specialty.Type>() { Specialty.Type.Brawler, Specialty.Type.Cannoneer, Specialty.Type.Fencer, Specialty.Type.Sharpshooter };
      }

      return new List<Specialty.Type>() { Specialty.Type.Adventurer, Specialty.Type.Crafter, Specialty.Type.Farmer, Specialty.Type.Merchant, Specialty.Type.Sailor };
   }

   public static Gender.Type getGender (Type npcType) {
      switch (npcType) {
         case Type.Gardener:
         case Type.ItemShop:
         case Type.Mapper:
         case Type.Skullhat:
            return Gender.Type.Female;
      }

      return Gender.Type.Male;
   }

   public bool isEnemy () {
      if (npcType == Type.Lizard) {
         return true;
      }

      return false;
   }

   public Type getTypeFromSprite () {
      string spriteName = GetComponent<SpriteRenderer>().sprite.name;

      // If we have a sprite swapper, we want to check that instead
      SpriteSwap swapper = GetComponent<SpriteSwap>();
      if (swapper != null) {
         spriteName = swapper.newTexture.name;
      }

      string[] split = spriteName.Split('_');
      spriteName = split[0];
      try {
         return npcType = (Type) Enum.Parse(typeof(Type), spriteName, true);
      } catch {
         Debug.LogWarning("Invalid Enum Type: " + spriteName);
         return Type.Blackbeard;
      }
   }

   public string getName () {
      // Retrieve the name from the data file
      string name = NPCManager.self.getName(npcId);

      // If the name is not defined in the file, use the default one
      if (name == null || string.Equals("", name)) {
         name = _npcName;
      }

      return name;
   }

   protected string getRandomName () {
      Area area = GetComponentInParent<Area>();
      return NameManager.self.getRandomName(getGender(npcType), area.areaKey, npcType);
   }

   protected void updateTradeGossip () {
      // TODO System is going to be removed soon
      tradeGossip = "I haven't heard anything recently.";
   }

   protected string getTradeGossip (List<CropOffer> offers) {
      if (offers.Count <= 0 || true) {
         return "I haven't heard anything recently.";
      }

      // Pick a random offer
      CropOffer offer = offers.ChooseRandom();

      Area area = GetComponentInParent<Area>();
      if (!area) {
         D.error("Couldn't get trade gossip - area not found");
         return "I haven't heard anything recently.";
      }
      Biome.Type biome = area.biome;
      string tradeGossip = string.Format("I heard that there's a merchant over in {0} that was {1} looking for {2}.",
         Biome.getName(biome), "really", IconUtil.getCrop(offer.cropType));

      return tradeGossip;
   }

   public void receiveData (DataField[] dataFields) {
      string shopName = "";
      string panelName = "None";
      int id = 0;
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.NPC_DATA_KEY) == 0) {
            // Get ID from npc data field
            // Field arrives in format <npc id>: <npc name>
            id = int.Parse(field.v.Split(':')[0]);
            isDebug = true;
            npcId = id;

            NPCData npcData = NPCManager.self.getNPCData(npcId);
            if (npcData != null) {
               string spritePath = npcData.spritePath;
               if (spritePath != "") {
                  GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(spritePath);
               }
            }
            Area area = GetComponentInParent<Area>();
            areaKey = area.areaKey;

            // Figure out our Type from our sprite
            npcType = getTypeFromSprite();

            NPCManager.self.storeNPC(this);
         } else if (field.k.CompareTo(DataField.NPC_SHOP_NAME_KEY) == 0) {
            // Get Shop Name (if any)
            shopName = field.v.Split(':')[0];
         } else if (field.k.CompareTo(DataField.NPC_PANEL_TYPE_KEY) == 0) {
            // Get Shop Panel Type (if any)
            panelName = field.v.Split(':')[0];
         }
      }

      if (panelName != "None") {
         D.editorLog("This npc is a shop npc: " + id + " - " + shopName, Color.cyan);
         _shopTrigger = gameObject.AddComponent<ShopTrigger>();
         _shopName = shopName;
         _shopTrigger.panelType = (Panel.Type) Enum.Parse(typeof(Panel.Type), panelName);
         isShopNpc = true;
      }
   }

   public static int fetchDataFieldID (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.NPC_DATA_KEY) == 0) {
            // Get ID from npc data field
            if (field.tryGetIntValue(out int npcId)) {
               return npcId;
            }
         }
      }
      return 0;
   }

   #region Private Variables

   // Our start position
   protected Vector2 _startPosition;

   // The available positions to move to
   protected List<Vector2> _moveTargets = new List<Vector2>();

   // Gets set to true while the player is nearby
   protected bool _isNearPlayer = false;

   // The Raycaster for our clickable canvas
   protected GraphicRaycaster _graphicRaycaster;

   // Our Shop Trigger (if any)
   protected ShopTrigger _shopTrigger;

   // The default faction, when not defined in the data file
   protected Faction.Type _faction = Faction.Type.Neutral;

   // The default specialty, when not defined in the data file
   protected Specialty.Type _specialty = Specialty.Type.Sailor;

   // The default name, when not defined in the data file
   protected string _npcName = "NPC";

   // Shop Name (if any)
   protected string _shopName = ShopManager.DEFAULT_SHOP_NAME;

   // The AStarGrid Reference fetched from the Main Scene
   protected AStarGrid _gridReference;

   // The Pathfinding Reference fetched from the Main Scene
   protected Pathfinding _pathfindingReference;

   // The current waypoint path
   protected List<ANode> _currentPath;

   #endregion
}
