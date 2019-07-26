using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class NPC : MonoBehaviour {
   #region Public Variables

   // How close we have to be in order to talk to the NPC
   public static float TALK_DISTANCE = .65f;

   // The Types of different NPCs
   public enum Type { None = 0,
      Blackbeard = 1, Blacksmith = 2, Fatty = 3, Feather = 4, Fisherman = 5,
      Gardener = 6, Glasses = 7, Gramps = 8, Hammer = 9, Headband = 10,
      ItemShop = 11, Mapper = 12, Monocle = 13, Parrot = 14, Patch = 15,
      Pegleg = 16, Seagull = 17, Shipyard = 18, Shroom = 19, Skullhat = 20,
      Stripes = 21, Vest = 22, Dog = 23, Lizard = 24,
   }

   // Holds the scriptable object npc data
   public NPCData npcData;

   // Holds the current player answers depending on quest state
   public List<ClickableText.Type> currentAnswerDialogue = new List<ClickableText.Type>();

   // The Type of NPC this is
   public Type npcType;

   // The Faction of this NPC
   public Faction.Type faction = Faction.Type.Neutral;

   // The Specialty of this NPC
   public Specialty.Type specialty = Specialty.Type.Sailor;

   // The name of this NPC
   public string npcName = "NPC";

   // Whether we want to auto move around
   public bool autoMove = true;

   // The position we want to move to
   public Vector2 moveTarget;

   // The speed that we move at
   public float moveSpeed = 3f;

   // The direction we're facing
   public Direction facing = Direction.South;

   // Whether or not we have diagonal sprites
   public bool hasDiagonals = false;

   // The unique id assigned to this npc
   public int npcId;

   // The area that this NPC is in
   public Area.Type areaType;

   // The current trade gossip for this NPC, changes over time
   public string tradeGossip;

   // Our name text
   public Text nameText;

   // Stores the reply of the NPC
   public string npcReply;

   #endregion

   private void Awake () {
      // Figure out our area id
      Area area = GetComponentInParent<Area>();
      this.areaType = area.areaType;
   }

   void Start () {
      // Look up components
      _body = GetComponent<Rigidbody2D>();
      _startPosition = this.transform.position;
      _animators.AddRange(GetComponentsInChildren<Animator>());
      _renderers.AddRange(GetComponentsInChildren<SpriteRenderer>());
      _graphicRaycaster = GetComponentInChildren<GraphicRaycaster>();
      _outline = GetComponent<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
      _shopTrigger = GetComponent<ShopTrigger>();

      if (this.gameObject.HasComponent<Animator>()) {
         _animators.Add(GetComponent<Animator>());
      }
      if (this.gameObject.HasComponent<SpriteRenderer>()) {
         _renderers.Add(GetComponent<SpriteRenderer>());
      }

      // Figure out our Type from our sprite
      this.npcType = getTypeFromSprite();

      // Set our id, name, faction, and specialty
      this.npcId = getId();
      this.npcName = getNameForType();
      this.faction = getFaction(this.npcType);
      this.specialty = getSpecialty();

      // Keep track of the NPC in the Manager
      NPCManager.self.storeNPC(this);

      // Default
      this.moveTarget = _startPosition;
      Util.setAlpha(nameText, 0f);

      foreach (Animator animator in _animators) {
         animator.SetInteger("facing", (int) this.facing);
      }

      // Add some move targets
      _moveTargets.Add(_startPosition + new Vector2(.3f, 0f));
      _moveTargets.Add(_startPosition + new Vector2(-.3f, 0f));
      _moveTargets.Add(_startPosition + new Vector2(0f, .3f));
      _moveTargets.Add(_startPosition + new Vector2(0f, -.3f));

      // Continually pick new move targets
      InvokeRepeating("pickMoveTarget", 0f, 6f + Random.Range(-1f, 1f));

      // Update our various text responses
      InvokeRepeating("updateTradeGossip", 0f, 60 * 60);

      // Set the name
      if (nameText != null) {
         // nameText.text = "[" + npcType + "]";
         // setNameColor(nameText, npcType);
      }
   }

   private void Update () {
      // Disable our clickable canvas while a panel is showing
      if (_graphicRaycaster != null) {
         _graphicRaycaster.gameObject.SetActive(!PanelManager.self.hasPanelInStack());
      }

      // Only show our outline when the mouse is over us
      handleSpriteOutline();

      // Allow pressing keyboard to interact
      if (InputManager.isActionKeyPressed() && Global.player != null && isCloseToGlobalPlayer()) {
         clientClickedMe();
      }

      if (autoMove) {
         // Figure out the direction we want to face
         Vector2 direction = moveTarget - (Vector2) this.transform.position;

         // If this NPC is talking to this client's player, then face them
         if (isTalkingToGlobalPlayer()) {
            direction = Global.player.transform.position - this.transform.position;
         }

         // Calculate an angle for that direction
         float angle = Util.angle(direction);

         // Set our facing direction based on that angle
         this.facing = this.hasDiagonals ? Util.getFacingWithDiagonals(angle) : Util.getFacing(angle);

         // Pass our angle and velocity on to the Animator
         foreach (Animator animator in _animators) {
            animator.SetFloat("velocityX", _body.velocity.x);
            animator.SetFloat("velocityY", _body.velocity.y);
            animator.SetBool("isMoving", _body.velocity.magnitude > .01f);
            animator.SetInteger("facing", (int) this.facing);
         }
      }

      // Fade our floating name in and out (but this seems to be really performance intensive for some reason)
      /*if (nameText != null) {
         float currentAlpha = Mathf.Clamp(nameText.color.a, 0f, 1f);
         Util.setAlpha(nameText, _isNearPlayer ? currentAlpha + Time.deltaTime * 3f : currentAlpha - Time.deltaTime * 3f);
      }*/

      // Check if we're showing a West sprite
      bool isFacingWest = this.facing == Direction.West || this.facing == Direction.NorthWest || this.facing == Direction.SouthWest;

      // Flip our sprite renderer if we're going west
      foreach (SpriteRenderer renderer in _renderers) {
         renderer.flipX = isFacingWest;
      }
   }

   private void FixedUpdate () {
      // If we're talking to the player, don't move
      if (isTalkingToGlobalPlayer()) {
         return;
      }

      // If we're close to our move target, we don't need to do anything
      if (Vector2.Distance(this.transform.position, moveTarget) < .1f) {
         return;
      }

      // Figure out the direction of our movement
      Vector2 direction = moveTarget - (Vector2)this.transform.position;
      _body.AddForce(direction.normalized * moveSpeed);
   }

   public void checkQuest(DeliveryQuestPair deliveryQuestPair) {
      QuestState currentQuestState = deliveryQuestPair.questState;
      QuestDialogue currentDialogue = deliveryQuestPair.dialogueData.questDialogueList.Find(_ => _.questState == currentQuestState);

      // Sets npc response
      npcReply = currentDialogue.npcDialogue;
      PanelManager.self.get(Panel.Type.NPC_Panel).GetComponent<NPCPanel>().SetMessage(npcReply);
      currentAnswerDialogue.Clear();

      if(currentDialogue.checkCondition) {
         List<Item> itemList = InventoryCacheManager.self.itemList;
         DeliverQuest deliveryQuest = deliveryQuestPair.deliveryQuest;
         Item findingItemList = itemList.Find(_ => (CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) deliveryQuest.itemToDeliver.itemTypeId
         && _.category == Item.Category.CraftingIngredients);

         if (findingItemList != null) {
            if (findingItemList.count >= deliveryQuest.quantity) {
               // Sets the player to a positive response if Requirements are met
               currentAnswerDialogue.Add(currentDialogue.playerReply);
               Global.player.rpc.Cmd_GetClickableRows(this.npcId);
               return;
            }
         } else {
            // Sets the player to a negative response if Requirements are met
            currentAnswerDialogue.Add(currentDialogue.playerNegativeReply);
            Global.player.rpc.Cmd_GetClickableRows(this.npcId);
            return;
         }
      }

      // Sets the player to a positive response if Requirements are met
      currentAnswerDialogue.Add(currentDialogue.playerReply);
     
      // Send a request to the server to get the clickable text options
      Global.player.rpc.Cmd_GetClickableRows(this.npcId);
   }

   public void clientClickedMe () {
      if (Global.player == null || _clickableBox == null || Global.isInBattle()) {
         return;
      }

      // Only works when the player is close enough
      if (Vector2.Distance(this.transform.position, Global.player.transform.position) > TALK_DISTANCE) {
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      // If this is a Shop NPC, then show the appropriate panel
      if (_shopTrigger != null) {
         PanelManager.self.pushIfNotShowing(_shopTrigger.panelType);
      } else {
         // Send a request to the server to get the clickable text options
         Global.player.rpc.Cmd_GetNPCRelation(this.npcId, this.npcName);
      }
   }

   protected int getId () {
      // We can make a unique idea based on our area and NPC type
      int id = ((int) this.areaType * 100) + (int) this.npcType;

      return id;
   }

   public bool isCloseToGlobalPlayer () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) < TALK_DISTANCE);
   }

   public bool isTalkingToGlobalPlayer () {
      return (PanelManager.self.get(Panel.Type.NPC_Panel).isShowing() && isCloseToGlobalPlayer());
   }

   protected void pickMoveTarget () {
      if (autoMove) {
         this.moveTarget = _moveTargets.ChooseRandom();
      }
   }

   public static void setNameColor (Text nameText, Type npcType) {
      Color textColor = Color.white;
      Color outlineColor = Color.black;

      switch (getFaction(npcType)) {
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

   public static Faction.Type getFaction (Type npcType) {
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
      Area area = this.GetComponentInParent<Area>();
      List<Specialty.Type> specialties = getPossibleSpecialties(this.faction);
      int randomIndex = ((int) area.areaType * 50) + (int) npcType;
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

   protected void handleSpriteOutline () {
      if (_outline != null) {
         _outline.color = isEnemy() ? Color.red : Color.white;
         _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
      }
   }

   protected Type getTypeFromSprite () {
      string spriteName = GetComponent<SpriteRenderer>().sprite.name;

      // If we have a sprite swapper, we want to check that instead
      SpriteSwap swapper = GetComponent<SpriteSwap>();
      if (swapper != null) {
         spriteName = swapper.newTexture.name;
      }
      
      string[] split = spriteName.Split('_');
      spriteName = split[0];
      Type npcType = (Type) System.Enum.Parse(typeof(Type), spriteName, true);

      return npcType;
   }

   protected string getNameForType () {
      Area area = this.GetComponentInParent<Area>();

      return NameManager.self.getRandomName(getGender(this.npcType), area.areaType, this.npcType);
   }

   protected void updateTradeGossip () {
      // Get our current Biome
      Biome.Type currentBiome = Area.getBiome(this.areaType);

      // Set up a list that will contain possible offer
      List<CropOffer> possibleOffers = new List<CropOffer>();

      // Cycle over all of the offers
      foreach (CropOffer offer in ShopManager.self.getAllOffers()) {
         Biome.Type offerBiome = Area.getBiome(offer.areaType);

         // We only care about the merchant shops
         if (!offer.areaType.ToString().StartsWith("MerchantShop")) {
            continue;
         }

         // Skip offers in our current biome
         if (offerBiome == currentBiome) {
            continue;
         }

         // Skip offers that aren't rare enough
         if ((int) offer.rarity <= (int) Rarity.Type.Common) {
            continue;
         }

         // Add it to the list
         possibleOffers.Add(offer);
      }

      // Set our gossip
      tradeGossip = getTradeGossip(possibleOffers);
   }

   protected string getTradeGossip (List<CropOffer> offers) {
      if (offers.Count <= 0 || true) {
         return "I haven't heard anything recently.";
      }

      // Pick a random offer
      CropOffer offer = offers.ChooseRandom();

      Biome.Type biome = Area.getBiome(offer.areaType);
      string tradeGossip = string.Format("I heard that there's a merchant over in {0} that was {1} looking for {2}.",
         Biome.getName(biome), "really", IconUtil.getCrop(offer.cropType));

      return tradeGossip;
   }

   #region Private Variables

   // Our start position
   protected Vector2 _startPosition;

   // Our Rigid body
   protected Rigidbody2D _body;

   // The available positions to move to
   protected List<Vector2> _moveTargets = new List<Vector2>();

   // Our Animators
   protected List<Animator> _animators = new List<Animator>();

   // Our renderers
   protected List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

   // Gets set to true while the player is nearby
   protected bool _isNearPlayer = false;

   // The Raycaster for our clickable canvas
   protected GraphicRaycaster _graphicRaycaster;

   // Our outline
   protected SpriteOutline _outline;

   // Our clickable box
   protected ClickableBox _clickableBox;

   // Our Shop Trigger (if any)
   protected ShopTrigger _shopTrigger;

   #endregion
}