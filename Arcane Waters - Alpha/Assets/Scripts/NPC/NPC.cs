using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using MapCreationTool.Serialization;
using System;
using Random = UnityEngine.Random;
using Pathfinding;
using UnityEngine.Events;

public class NPC : NetEntity, IMapEditorDataReceiver
{
   #region Public Variables

   // How close we have to be in order to talk to the NPC
   public static float TALK_DISTANCE = .65f;

   // How close we have to be in order to pet animal
   public static float ANIMAL_PET_DISTANCE = .5f;

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

   // The sprite path of the npc avatar
   [SyncVar]
   public string spritePath;

   // The sprite path of the npc icon
   [SyncVar]
   public string iconPath;

   [SyncVar]
   // The Type of NPC this is
   public Type npcType;

   // Shop id (if any)
   [SyncVar]
   public int shopId = 0;

   // The type of shop panel this npc will generate if this is a shop npc
   [SyncVar]
   public Panel.Type shopPanelType;

   [SyncVar]
   // The unique id assigned to this npc
   public int npcId;

   // Determines if this npc is spawned at debug mode
   public bool isDebug = false;

   [SyncVar]
   // Determines if this npc is a shop npc
   public bool isShopNpc;

   [SyncVar]
   // The Type of reaction after petting (if this NPC is animal)
   public AnimalPetting.ReactionType animalReactionType;

   // Determine if NPC being animal, is currently being pet by player
   public bool isInteractingAnimal = false;

   // Settings determining animal's reaction to petting
   public AnimalPettingConfig animalPettingConfig;

   // Determines if this npc is staying still or moving around
   [SyncVar]
   public bool isStationary;

   [SyncVar]
   // Check if NPC is currently able to move and if seeker should continue its work
   public bool canMove = true;

   // List of position that allows player to pet animal
   public List<GameObject> animalPettingPositions;

   // Event triggered after animal petting action
   public UnityEvent finishedPetting = new UnityEvent();

   // The game object that indicates that this npc has a quest for the player
   public GameObject questNotice;

   // The game object that indicates that this npc has a quest for the player but does not have enough requirements
   public GameObject insufficientQuestNotice;

   // A collider active on stationary npc preventing players to stand on top of them
   public Collider2D stationaryCollider;

   #endregion

   protected override void Awake () {
      base.Awake();
      // Look up components
      _graphicRaycaster = GetComponentInChildren<GraphicRaycaster>();

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

      // We can't proceed until our NPC Data has been received
      StartCoroutine(CO_InitializeAfterDataReady());
   }

   private IEnumerator CO_InitializeAfterDataReady () {
      while (npcId == 0 || NPCManager.self.getNPCData(npcId) == null) {
         yield return null;
      }

      NPCData npcData = NPCManager.self.getNPCData(npcId);

      finishInitialization(npcData);
   }

   public void finishInitialization (NPCData npcData) {
      // Set npc name
      _npcName = npcData.name;
      if (nameText != null) {
         nameText.text = _npcName;
      }

      // Set NPC texture
      SpriteSwap spriteSwap = GetComponent<SpriteSwap>();
      this.spritePath = npcData.spritePath;

      if (!Util.isBatch()) {
         try {
            spriteSwap.newTexture = ImageManager.getTexture(this.spritePath);
         } catch {
            spriteSwap.newTexture = NPCManager.self.defaultNpcBodySprite.texture;
         }

         if (spriteSwap.newTexture.name.Contains("empty")) {
            D.debug("Invalid NPC sprite, please complete details in NPC Editor" + " : " + npcData.npcId + " : " + npcData.name);
            spriteSwap.newTexture = NPCManager.self.defaultNpcBodySprite.texture;
         }

         List<ImageManager.ImageData> newSprites = ImageManager.getSpritesInDirectory(this.spritePath);
         if (newSprites.Count > 0) {
            GetComponent<SpriteRenderer>().sprite = newSprites[0].sprites[0];

            // If we have a sprite swapper, we want to check that instead
            Texture2D newTexture = newSprites[0].texture2D;
            if (newTexture) {
               spriteSwap.newTexture = newTexture;
            }
         }
      }

      // Continually pick new move targets
      if (isServer && !isStationary) {
         _seeker = GetComponent<Seeker>();
         if (_seeker == null) {
            D.debug("There has to be a Seeker Script attached to the NPC Prefab");
         }

         // Only use the graph in this area to calculate paths
         GridGraph graph = AreaManager.self.getArea(areaKey).getGraph();
         _seeker.graphMask = GraphMask.FromGraph(graph);

         _seeker.pathCallback = setPath_Asynchronous;

         _startPosition = transform.position;
      } else {
         _shopTrigger = gameObject.AddComponent<ShopTrigger>();
         _shopTrigger.panelType = shopPanelType;
      }

      shadow.transform.localScale = new Vector3(npcData.shadowScale, npcData.shadowScale, npcData.shadowScale);
      shadow.transform.localPosition = new Vector3(0, npcData.shadowOffsetY, 0);

      // Use shadow as our sort point
      sortPoint.transform.localPosition = shadow.transform.localPosition;

      // Add name to game object for editor preview
      NPCData fetchedData = NPCManager.self.getNPCData(npcId);
      gameObject.name = fetchedData == null ? gameObject.name : fetchedData.name;

      // Prevent players to stand on top of stationary npcs
      if (stationaryCollider != null) { 
         if (isStationary) {
            stationaryCollider.enabled = true;
            getRigidbody().constraints = RigidbodyConstraints2D.FreezeAll;
         } else {
            stationaryCollider.enabled = false;
         }
      }

      // Keep track of the NPC in the Manager
      NPCManager.self.storeNPC(this);
   }

   protected override void Update () {
      base.Update();

      // Disable our clickable canvas while a panel is showing
      if (_graphicRaycaster != null) {
         _graphicRaycaster.gameObject.SetActive(!(PanelManager.self.isAnyPanelShowing() || PanelManager.isLoading));
      }

      if (isUnderExternalControl) {
         return;
      }

      Vector2 direction;
      if (isServer && _currentPathIndex < _currentPath.Count) {
         direction = (Vector2) _currentPath[_currentPathIndex] - (Vector2) transform.position;
      } else {
         direction = Util.getDirectionFromFacing(facing);
      }
      // Figure out the direction we want to face

      // If this NPC is talking to this client's player, then face them
      if (isTalkingToGlobalPlayer()) {
         direction = Global.player.transform.position - transform.position;
      }

      if (!interactingAnimation) {
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

      // If we're talking to the player or movement is blocked, don't move
      if (isTalkingToGlobalPlayer() || !canMove) {
         return;
      }

      if (isUnderExternalControl) {
         return;
      }

      if (isServer && _seeker != null) {
         if (_currentPathIndex < _currentPath.Count) {
            // Move towards our current waypoint
            // Only change our movement if enough time has passed
            double moveTime = NetworkTime.time - _lastMoveChangeTime;
            if (moveTime >= MOVE_CHANGE_INTERVAL) {
               float moveSpeed = getMoveSpeed() * 0.5f;
               _body.AddForce(((Vector2) _currentPath[_currentPathIndex] - (Vector2) transform.position).normalized * moveSpeed);
               _lastMoveChangeTime = NetworkTime.time;
            }

            // Clears a node as the unit passes by
            float distanceToWaypoint = Vector2.Distance(_currentPath[_currentPathIndex], transform.position);
            if (distanceToWaypoint < .1f) {
               ++_currentPathIndex;
            }
         } else if (!isShopNpc && !isStationary && _seeker.IsDone() && _moving) {
            _moving = false;
            // Generate a new path
            Invoke("generateNewWaypoints", PAUSE_BETWEEN_PATHS);
         }
      }
   }

   public void clientClickedMe () {
      if (Global.player == null || _clickableBox == null || Global.isInBattle()) {
         return;
      }

      // If NPC is marked as "animal", player can interact with it
      if (isAnimal()) {
         if (!isInteractingAnimal) {
            GameObject closestSpot = null;
            foreach (GameObject spot in animalPettingPositions) {
               spot.SetActive(false);
            }

            Vector3 playerPos = Global.player.transform.position;

            // Choose spot to start petting
            switch (Global.player.facing) {
               case Direction.North:
                  closestSpot = animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Bottom"));
                  break;
               case Direction.East:
                  closestSpot = animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Left"));
                  break;
               case Direction.South:
                  float minDist = float.MaxValue;
                  foreach (GameObject spot in animalPettingPositions) {
                     if (Vector2.Distance(spot.transform.position, playerPos) < minDist) {
                        minDist = Vector2.Distance(spot.transform.position, playerPos);
                        closestSpot = spot;
                     }
                  }
                  closestSpot.SetActive(true);
                  FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asTooFar();
                  return;
               case Direction.West:
                  closestSpot = animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Right"));
                  break;
            }

            float distance = Vector2.Distance(closestSpot.transform.position, playerPos);

            // Play animation or show message that player is too far
            if (distance > ANIMAL_PET_DISTANCE) {
               closestSpot.SetActive(true);
               getAnimalPetting().StopAllCoroutines();
               getAnimalPetting().hideSpotsAfterTime(this);
               FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asTooFar();
            } else {
               if (Global.player.isMoving()) {
                  FloatingCanvas floatingCanvas = FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f));
                  if (floatingCanvas.text != null) {
                     floatingCanvas.text.text = "Stand still...";
                  }
                  return;
               }

               Vector2 distToMoveAnimal = playerPos - closestSpot.transform.position;
               startAnimalPetting(distToMoveAnimal, distance);
            }
         }
         return;
      }

      // Only works when the player is close enough
      if (Vector2.Distance(transform.position, Global.player.transform.position) > TALK_DISTANCE) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asTooFar();
         return;
      }

      // Only works if npc is interactable
      if (!NPCManager.self.getNPCData(npcId).interactable) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asNoResponse();
         return;
      }

      if (_shopTrigger != null && _shopTrigger.panelType != Panel.Type.None) {
         // If this is a Shop NPC, then show the appropriate panel
         switch (_shopTrigger.panelType) {
            case Panel.Type.Adventure:
               AdventureShopScreen adventurePanel = (AdventureShopScreen) PanelManager.self.get(_shopTrigger.panelType);
               adventurePanel.shopId = shopId;
               adventurePanel.headIconSprite = getHeadIconSprite();
               adventurePanel.refreshPanel();
               break;
            case Panel.Type.Shipyard:
               ShipyardScreen shipyardPanel = (ShipyardScreen) PanelManager.self.get(_shopTrigger.panelType);
               shipyardPanel.shopId = shopId;
               shipyardPanel.headIconSprite = getHeadIconSprite();
               shipyardPanel.refreshPanel();
               break;
            case Panel.Type.Merchant:
               MerchantScreen merchantPanel = (MerchantScreen) PanelManager.self.get(_shopTrigger.panelType);
               merchantPanel.shopId = shopId;
               merchantPanel.headIconSprite = getHeadIconSprite();
               merchantPanel.refreshPanel();
               break;
            case Panel.Type.Store:
               BottomBar.self.toggleStorePanel();
               break;
         }
      } else {
         // Make sure the panel is showing
         NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);
         if (!panel.isShowing()) {
            NPCData npcData = NPCManager.self.getNPCData(npcId);
            panel.setNPC(npcId, npcData.name, -1);
         }

         // Send a request to the server to get the npc panel info
         Global.player.rpc.Cmd_RequestNPCQuestSelectionListFromServer(npcId);
      }

      // Set a load icon above the NPC while the panel data is requested from the server
      PanelManager.isLoading = true;
      FloatingLoadIcon floatingLoadIcon = FloatingLoadIcon.instantiateAt(transform.position + new Vector3(0f, .28f));
   }

   public bool isCloseToGlobalPlayer () {
      if (Global.player == null) {
         return false;
      }

      return Vector2.Distance(Global.player.transform.position, transform.position) < TALK_DISTANCE;
   }

   public bool isTalkingToGlobalPlayer () {
      return (PanelManager.self.get(Panel.Type.NPC_Panel).isShowing() || PanelManager.isLoading) && isCloseToGlobalPlayer();
   }

   [Server]
   protected void generateNewWaypoints () {
      findAndSetPath_Asynchronous(_startPosition + Random.insideUnitCircle * MAX_MOVE_DISTANCE);
   }

   [Server]
   private void findAndSetPath_Asynchronous (Vector3 targetPosition) {
      if (!_seeker.IsDone()) {
         _seeker.CancelCurrentPathRequest();
      }
      _seeker.StartPath(transform.position, targetPosition);
   }

   [Server]
   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 0;
      _moving = true;
      _seeker.CancelCurrentPathRequest(true);
   }

   public static void setNameColor (Text nameText, Type npcType) {
      Color textColor = Color.white;
      Color outlineColor = Color.black;

      nameText.color = textColor;
      nameText.GetComponent<Outline>().effectColor = outlineColor;
      nameText.GetComponent<Shadow>().effectColor = outlineColor;
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

   public Type getTypeFromSprite (string spriteName) {
      // If we have a sprite swapper, we want to check that instead
      SpriteSwap swapper = GetComponent<SpriteSwap>();
      if (swapper != null) {
         if (swapper.newTexture == null) {
            D.debug("Swappers Texture Can NOT be null!!");
         } else {
            spriteName = swapper.newTexture.name;
         }
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

   public Sprite getHeadIconSprite () {
      Sprite spriteRet = ImageManager.getSprite(iconPath);
      if (spriteRet == null || spriteRet == ImageManager.self.blankTexture) {
         spriteRet = NPCManager.self.defaultNpcFaceSprite;
      }

      return spriteRet;
   }

   public void receiveData (DataField[] dataFields) {
      // This map data setup is processed on the server side only
      int shopId = 0;
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
                  this.spritePath = spritePath;
                  animalReactionType = calculateAnimalReactionType(this.spritePath);
               }
               iconPath = npcData.iconPath;

               NPCManager.self.storeNPC(this);
            }
            Area area = GetComponentInParent<Area>();
            areaKey = area.areaKey;
         } else if (field.k.CompareTo(DataField.NPC_SHOP_NAME_KEY) == 0) {
            // Get Shop id (if any)
            try {
               shopId = int.Parse(field.v.Split(':')[0]);
            } catch {
               shopId = 0;
            }
         } else if (field.k.CompareTo(DataField.NPC_PANEL_TYPE_KEY) == 0) {
            // Get Shop Panel Type (if any)
            panelName = field.v.Split(':')[0];
         } else if (field.k.CompareTo(DataField.NPC_DIRECTION_KEY) == 0) {
            try {
               facing = (Direction) Enum.Parse(typeof(Direction), field.v.Split(':')[0]);
            } catch {
               facing = Direction.South;
            }
         } else if (field.k.CompareTo(DataField.NPC_STATIONARY_KEY) == 0) {
            string isStationaryData = field.v.Split(':')[0];
            isStationary = isStationaryData.ToLower() == "true" ? true : false;
         }
      }

      if (panelName != "None") {
         _shopTrigger = gameObject.AddComponent<ShopTrigger>();
         this.shopId = shopId;
         _shopTrigger.panelType = (Panel.Type) Enum.Parse(typeof(Panel.Type), panelName);
         isShopNpc = true;
         shopPanelType = _shopTrigger.panelType;
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

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.npcParent, worldPositionStays);
   }

   private void startAnimalPetting (Vector2 distToMoveAnimal, float distanceToAnimal) {
      if (Global.player != null) {
         float currentDistance = Vector2.Distance((Vector2) transform.position, Global.player.transform.position);
         bool isWithinStationaryBounds = false;
         float distance = .2f;
         float nearestDistance = 10;
         Direction overrideDirection = Direction.North;
         if (isStationary) {
            // Check nearest pet node to snap on to for stationary pets
            if (Vector2.Distance(transform.position, Global.player.transform.position) < distance) {
               float bottomDist = Vector2.Distance(animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Bottom")).transform.position, Global.player.transform.position);
               float leftDist = Vector2.Distance(animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Left")).transform.position, Global.player.transform.position);
               float rightDist = Vector2.Distance(animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Right")).transform.position, Global.player.transform.position);
               if (bottomDist < nearestDistance) {
                  nearestDistance = bottomDist;
                  isWithinStationaryBounds = true;
                  overrideDirection = Direction.North;
               }
               if (leftDist < nearestDistance) {
                  nearestDistance = leftDist;
                  isWithinStationaryBounds = true;
                  overrideDirection = Direction.East;
               }
               if (rightDist < nearestDistance) {
                  nearestDistance = rightDist;
                  isWithinStationaryBounds = true;
                  overrideDirection = Direction.West;
               }
            }
         }

         if (!isStationary || isWithinStationaryBounds) {
            // Set correct NPC state
            isInteractingAnimal = true;
            interactingAnimation = true;
            canMove = false;

            Vector2 animalEndPos = new Vector2(transform.position.x, transform.position.y) + distToMoveAnimal;
            float maxTime = Mathf.Lerp(0.0f, 0.75f, distanceToAnimal / ANIMAL_PET_DISTANCE);
            Global.player.rpc.Cmd_StartPettingAnimal(this.netIdentity.netId, isStationary ? (int) overrideDirection : (int) Global.player.facing);

            // Take control over player to ensure that character stays in place
            gameObject.AddComponent<AnimalPettingPuppetController>().startControlOverPlayer(Global.player, isStationary);
         }
      }
   }

   public void triggerPetAnimation (uint playerEntityId, Vector2 animalEndPos, float maxTime) {
      if (Vector2.Distance(animalEndPos, transform.position) > NpcControlOverride.CLIENT_PET_DISTANCE && !isStationary) {
         // Wait for destination to sync before playing animation
         CO_WaitToReachDestination(maxTime + 0.05f, playerEntityId, animalEndPos);
      } else {
         // Start player's petting animation
         StartCoroutine(CO_ContinueAnimalPettingWithCorrectPos(maxTime + 0.05f, playerEntityId));
      }
   }

   public void triggerPetControl (Vector2 animalEndPos, float maxTime) {
      isInteractingAnimal = true;
      interactingAnimation = true;
      canMove = false;

      // Play animal animation - moving to correct position
      AnimalPuppet puppet = GetComponent<AnimalPuppet>();
      if (puppet == null) {
         puppet = gameObject.AddComponent<AnimalPuppet>();
      }

      puppet.setData(animalEndPos, maxTime + 0.05f);
      puppet.controlGranted(this);
   }

   private IEnumerator CO_WaitToReachDestination (float timeToWait, uint playerEntityId, Vector2 animalEndPos) {
      while (Vector2.Distance(animalEndPos, transform.position) > NpcControlOverride.CLIENT_PET_DISTANCE) {
         yield return null;
      }

      StartCoroutine(CO_ContinueAnimalPettingWithCorrectPos(timeToWait, playerEntityId));
   }

   private IEnumerator CO_ContinueAnimalPettingWithCorrectPos (float timeToWait, uint playerEntityId) {
      // Wait until animal has moved to correct spot
      yield return new WaitForSeconds(timeToWait);

      // Spot animal animators when player is playing petting animation
      foreach (Animator animator in _animators) {
         animator.enabled = false;
      }

      // TemporaryController is no longer needed - destroy it
      AnimalPuppet puppet = GetComponent<AnimalPuppet>();
      Destroy(puppet);

      // Play player animation of petting animal
      NetEntity player = MyNetworkManager.fetchEntityFromNetId<NetEntity>(playerEntityId);
      if (player) {
         player.requestAnimationPlay(Anim.Type.Pet_East, false);
      }

      // Play animal's reaction
      getAnimalPetting().playAnimalAnimation(this, animalReactionType);
   }

   private AnimalPetting getAnimalPetting () {
      if (_animalPetting == null) {
         _animalPetting = GetComponent<AnimalPetting>();
         if (_animalPetting == null) {
            _animalPetting = this.gameObject.AddComponent<AnimalPetting>();
         }
      }
      return _animalPetting;
   }

   private AnimalPetting.ReactionType calculateAnimalReactionType (string path) {
      string[] splits = path.Split('/');
      if (splits.Length > 0) {
         path = splits[splits.Length - 1];
         foreach (string name in animalPettingConfig.heartReaction) {
            if (path == name) {
               return AnimalPetting.ReactionType.Hearts;
            }
         }

         foreach (string name in animalPettingConfig.angryReaction) {
            if (path == name) {
               return AnimalPetting.ReactionType.Angry;
            }
         }

         foreach (string name in animalPettingConfig.confusedReaction) {
            if (path == name) {
               return AnimalPetting.ReactionType.Confused;
            }
         }
      }

      return AnimalPetting.ReactionType.None;
   }

   public void finishAnimalPetting () {
      if (isInteractingAnimal) {
         foreach (Animator animator in _animators) {
            animator.enabled = true;
         }
      }
   }

   [ClientRpc]
   public void Rpc_ContinuePetMoveControl (Vector2 animalEndPos, float maxTime) {
      triggerPetControl(animalEndPos, maxTime);
   }

   [ClientRpc]
   public void Rpc_ContinuePettingAnimal (uint playerEntityId, Vector2 animalEndPos, float maxTime) {
      triggerPetAnimation(playerEntityId, animalEndPos, maxTime);
   }

   public void finishAnimalReaction () {
      finishedPetting.Invoke();
      if (isInteractingAnimal) {
         isInteractingAnimal = false;
         canMove = true;
      }
      interactingAnimation = false;

      AnimalPettingPuppetController animalPettingController = gameObject.GetComponent<AnimalPettingPuppetController>();
      if (animalPettingController != null) {
         animalPettingController.stopAnimalPetting();
         Destroy(animalPettingController);
      }

      // Make sure that server can update this pet again
      isUnderExternalControl = false;
   }

   public bool isAnimal () {
      return animalReactionType != AnimalPetting.ReactionType.None;
   }

   #region Private Variables

   // How long, in seconds, the NPC should pause between finding new paths to walk
   protected float PAUSE_BETWEEN_PATHS = 6.0f;

   // How far the NPC will be able to move from it's starting position
   protected float MAX_MOVE_DISTANCE = 0.3f;

   // Our start position
   protected Vector2 _startPosition;

   // Gets set to true while the player is nearby
   protected bool _isNearPlayer = false;

   // The Raycaster for our clickable canvas
   protected GraphicRaycaster _graphicRaycaster;

   // Our Shop Trigger (if any)
   protected ShopTrigger _shopTrigger;

   // The default name, when not defined in the data file
   protected string _npcName = "NPC";

   // Shop Name (if any)
   protected string _shopName = ShopManager.DEFAULT_SHOP_NAME;

   // The Seeker that handles Pathfinding
   protected Seeker _seeker;

   // The current Path
   protected List<Vector3> _currentPath = new List<Vector3>();

   // The current Point Index of the Path
   private int _currentPathIndex;

   // Are we currently moving this NPC along a Path?
   private bool _moving = true;

   // Script used for handling petting sequence of an animal
   private AnimalPetting _animalPetting;

   #endregion
}
