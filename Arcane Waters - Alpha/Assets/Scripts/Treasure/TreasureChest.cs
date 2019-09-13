﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureChest : NetworkBehaviour {
   #region Public Variables

   // The unique ID assigned to this chest
   [SyncVar]
   public int id;

   // The instance that this chest is in
   [SyncVar]
   public int instanceId;

   // The time at which this chest was created
   [SyncVar]
   public long creationTime;

   // Our sprite renderer
   public SpriteRenderer spriteRenderer;

   // The list of user IDs that have opened this chest
   public SyncListInt userIds = new SyncListInt();

   // The Open button
   public GameObject openButtonContainer;

   // The Shine effect
   public GameObject shineContainer;

   // Our box collider
   public BoxCollider2D boxCollider;

   // Our trigger collider for the Open button
   public CircleCollider2D triggerCollider;

   // Our Chest opening animation
   public SimpleAnimation chestOpeningAnimation;

   // Our Chest opening effect
   public SimpleAnimation chestBurstAnimation;

   // The Sprite we use after the chest has been opened
   public Sprite openedChestSprite;

   // The container for our animated arrow
   public GameObject arrowContainer;

   // Determines if this chest is a land or sea chest
   public bool isSeaChest;

   // The animator of the chest
   public Animator chestAnimator;

   // The type of enemy that dropped the loot
   [SyncVar]
   public int enemyType;

   #endregion

   public void Awake () {
      this.creationTime = System.DateTime.Now.ToBinary();

      // Start out with the open button hidden
      openButtonContainer.SetActive(false);

      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();

      // If we're running a server not in Host mode, we don't need the trigger collider
      if (!NetworkClient.active) {
         triggerCollider.enabled = false;
      }
   }

   public void Update () {
      // Activate certain things when the global player is nearby
      arrowContainer.SetActive(_isGlobalPlayerNearby && !hasBeenOpened());

      if (!isSeaChest) {
         // We only enable the box collider for clients in the relevant instance
         boxCollider.enabled = (Global.player != null && Global.player.instanceId == this.instanceId);
      } else {
         triggerCollider.enabled = !hasBeenOpened();
         chestAnimator.SetBool("open", hasBeenOpened());
      }

      // Figure out whether our outline should be showing
      handleSpriteOutline();

      // Allow pressing keyboard to open the chest
      if (InputManager.isActionKeyPressed() && !hasBeenOpened() && _isGlobalPlayerNearby) {
         sendOpenRequest();
      }

      // Always hide the Open button and shine effect for chests that have been opened
      if (hasBeenOpened()) {
         openButtonContainer.SetActive(false);
         shineContainer.SetActive(false);
         spriteRenderer.sprite = openedChestSprite;
      }
   }

   public void sendOpenRequest () {
      if (hasBeenOpened()) {
         return;
      }

      // The player has to be close enough
      if (!_isGlobalPlayerNearby) {
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      if (isSeaChest) {
         Global.player.rpc.Cmd_OpenSeaChest(this.id);
      } else {
         Global.player.rpc.Cmd_OpenChest(this.id);
      }
   }

   public void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
   }

   public Item getContents () {
      if (isSeaChest) {
         return getEnemyContents();
      }

      // Create a random item for now
      if (Random.Range(0f, 1f) > .5f) {
         Weapon weapon = new Weapon(0, (int) Weapon.Type.Sword_3, ColorType.DarkGreen, ColorType.DarkPurple, "");
         weapon.itemTypeId = (int) weapon.type;

         return weapon;
      } else {
         Armor armor = new Armor(0, (int) Armor.Type.Strapped, ColorType.DarkGray, ColorType.DarkBlue, "");
         armor.itemTypeId = (int) armor.type;

         return armor;
      }
   }

   public Item getEnemyContents () {
      // Gets loots for enemy type
      EnemyLootLibrary lootLibrary = RewardManager.self.enemyLootList.Find(_ => _.enemyType == (Enemy.Type)enemyType);
      List<LootInfo> processedLoots = lootLibrary.dropTypes.requestLootList();

      // Registers list of ingredient types for data fetching
      List<CraftingIngredients.Type> itemLoots = new List<CraftingIngredients.Type>();
      foreach(LootInfo info in processedLoots) {
         itemLoots.Add(info.lootType);
      }

      Item itemToCreate = new CraftingIngredients(0, processedLoots[0].lootType, ColorType.Black, ColorType.Black);
      return itemToCreate;
   }

   public bool hasBeenOpened () {
      if (Global.player == null) {
         return false;
      }

      return userIds.Contains(Global.player.userId);
   }

   public IEnumerator CO_CreatingFloatingIcon (Item item) {
      // Give some time for the chest to open
      yield return new WaitForSeconds(.18f);

      // Create a floating icon
      GameObject floatingIcon = Instantiate(TreasureManager.self.floatingIconPrefab, Vector3.zero, Quaternion.identity);
      floatingIcon.transform.SetParent(this.transform);
      floatingIcon.transform.localPosition = new Vector3(0f, .04f);
      Image image = floatingIcon.GetComponentInChildren<Image>();
      image.sprite = ImageManager.getSprite(item.getIconPath());

      // Create a new instance of the material we can use for recoloring
      Material sourceMaterial = MaterialManager.self.getGUIMaterial(item.getColorKey());
      if (sourceMaterial != null) {
         image.material = new Material(sourceMaterial);

         // Recolor
         floatingIcon.GetComponentInChildren<RecoloredSprite>().recolor(item.getColorKey(), item.color1, item.color2);
      }

      // Set the name text
      floatingIcon.GetComponentInChildren<FloatAndStop>().nameText.text = item.getName();
   }

   private void OnTriggerEnter2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      if (hasBeenOpened()) {
         return;
      }

      // If our player enters the trigger, we show the GUI
      if (entity != null && Global.player != null && entity.userId == Global.player.userId) {
         _isGlobalPlayerNearby = true;
      }
   }

   private void OnTriggerExit2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      // If our player exits the trigger, we hide the GUI
      if (entity != null && Global.player != null && entity.userId == Global.player.userId) {
         _isGlobalPlayerNearby = false;
      }
   }

   #region Private Variables

   // Gets set to true when the global player is nearby
   protected bool _isGlobalPlayerNearby;

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}
