using UnityEngine;
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

   // The object containing the special effects (This is disabled if the chest is using custom sprite, intended for secret chests)
   public GameObject effectsHolder;

   // Determines the type of chest if it is dropped by a land or sea monster or spawned at a treasure site
   public ChestSpawnType chestType;

   // The type of enemy that dropped the loot
   [SyncVar]
   public int enemyType;

   // Determines if this chest should be destroyed after interaction
   [SyncVar]
   public bool autoDestroy;

   // If this treasure is using a custom sprite
   [SyncVar]
   public bool useCustomSprite;

   // The custom sprite path
   [SyncVar]
   public string customSpritePath;

   // If this object is waiting for a server response (for animation sync purposes)
   public bool isWaitingForServerResponse;

   // The standard sprite count of a chest open animation to be based upon
   public const int DEFAULT_SPRITES_PER_SHEET = 24;

   #endregion

   private void Awake () {
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

      // Schedule the chest disappearance, except for Site chests
      if (chestType != ChestSpawnType.Site) {
         StartCoroutine(CO_DisableChestAfterLifetime());
      }
   }

   public void Start () {
      if (Util.isBatch()) {
         return;
      }
      
      Minimap.self.addTreasureChestIcon(this.gameObject);

      if (useCustomSprite) {
         Sprite[] customSprites = ImageManager.getSprites(customSpritePath);
         int lastIntex = customSprites.Length / 2;

         // Sets the sprite and sprite animations last frame
         spriteRenderer.sprite = customSprites[0];
         openedChestSprite = customSprites[lastIntex];
         chestOpeningAnimation.maxIndex = lastIntex;

         // Adjusts the speed of the animation depending on the number of sprites in the sheet
         float frameRateReduction = customSprites.Length / DEFAULT_SPRITES_PER_SHEET;
         frameRateReduction = Mathf.Clamp(frameRateReduction, .1f, 2f);
         chestOpeningAnimation.frameLengthOverride = (chestOpeningAnimation.frameLengthOverride / frameRateReduction);

         if (effectsHolder != null) {
            effectsHolder.SetActive(false);
         }
      }
   }

   public void Update () {
      // Activate certain things when the global player is nearby
      arrowContainer.SetActive(_isGlobalPlayerNearby && !hasBeenOpened());

      if (chestType == ChestSpawnType.Site) {
         // We only enable the box collider for clients in the relevant instance
         boxCollider.enabled = (Global.player != null && Global.player.instanceId == this.instanceId);
      } else if (chestType == ChestSpawnType.Sea) {
         boxCollider.enabled = false;
         triggerCollider.enabled = !hasBeenOpened();
      } else {
         boxCollider.enabled = !hasBeenOpened();
         triggerCollider.enabled = !hasBeenOpened();
      }

      // Figure out whether our outline should be showing
      handleSpriteOutline();

      // Always hide the Open button and shine effect for chests that have been opened
      if (hasBeenOpened()) {
         openButtonContainer.SetActive(false);
         shineContainer.SetActive(false);

         if (!Util.isBatch()) {
            if ((!isWaitingForServerResponse && chestOpeningAnimation.getIndex() == chestOpeningAnimation.minIndex) || chestOpeningAnimation.isPaused) {
               spriteRenderer.sprite = openedChestSprite;
            }
         }
      }
   }

   public void sendOpenRequest () {
      if (hasBeenOpened()) {
         return;
      }
      isWaitingForServerResponse = true;

      // The player has to be close enough
      if (!_isGlobalPlayerNearby) {
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      if (chestType == ChestSpawnType.Sea || chestType == ChestSpawnType.Land) {
         Global.player.rpc.Cmd_OpenLootBag(this.id);
      } else {
         Global.player.rpc.Cmd_OpenChest(this.id);
      }

      deleteTreasureChestIcon();
   }

   public void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      _outline.setVisibility(MouseManager.self.isHoveringOver(_clickableBox));
   }

   public Item getContents () {
      if (chestType == ChestSpawnType.Sea) {
         return getSeaMonsterLootContents();
      } else if (chestType == ChestSpawnType.Land) {
         return getLandMonsterLootContents();
      }

      // Create a random item for now
      if (Random.Range(0f, 1f) > .5f) {
         // TODO: This is a fixed loot, proposal: create a tool that will alter options of what loots to randomize
         int weaponID = EquipmentXMLManager.self.getWeaponData(3).weaponType;
         Weapon weapon = new Weapon(0, weaponID, ColorType.DarkGreen, ColorType.DarkPurple, "");

         return weapon;
      } else {
         Armor armor = new Armor(0, 1, ColorType.DarkGray, ColorType.DarkBlue, "");

         return armor;
      }
   }

   public Item getSeaMonsterLootContents () {
      // Gets loots for enemy type
      SeaMonsterLootLibrary lootLibrary = RewardManager.self.fetchSeaMonsterLootData((SeaMonsterEntity.Type) enemyType);
      List<LootInfo> processedLoots = lootLibrary.dropTypes.requestLootList();

      // Registers list of ingredient types for data fetching
      List<Item> itemLoots = new List<Item>();
      foreach(LootInfo info in processedLoots) {
         itemLoots.Add(info.lootType);
      }

      Item itemToCreate = new Item { category = itemLoots[0].category, itemTypeId = itemLoots[0].itemTypeId };
      return itemToCreate;
   }

   public Item getLandMonsterLootContents () {
      // Gets loot
      BattlerData battlerData = BattleManager.self.getAllBattlersData().Find(x => (int)x.enemyType == enemyType);
      List<LootInfo> processedLoot = battlerData.battlerLootData.requestLootList();

      // Registers list of ingredient types for data fetching
      List <Item> itemLoots = new List<Item>();
      foreach (LootInfo info in processedLoot) {
         itemLoots.Add(info.lootType);
      }

      Item itemToCreate = new Item { category = itemLoots[0].category, itemTypeId = itemLoots[0].itemTypeId, data = itemLoots[0].data };
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
      float animationDuration = chestOpeningAnimation.frameLengthOverride * chestOpeningAnimation.maxIndex;
      animationDuration = Mathf.Clamp(animationDuration, .1f, 2);
      yield return new WaitForSeconds(animationDuration + 0.2f);

      // Create a floating icon
      GameObject floatingIcon = Instantiate(TreasureManager.self.floatingIconPrefab, Vector3.zero, Quaternion.identity);
      floatingIcon.transform.SetParent(this.transform);
      floatingIcon.transform.localPosition = new Vector3(0f, .04f);
      Image image = floatingIcon.GetComponentInChildren<Image>();
      string itemName = item.getName();
      if (item.category != Item.Category.Blueprint) {
         if (item.category == Item.Category.Weapon) {
            WeaponStatData weaponStatData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            image.sprite = ImageManager.getSprite(weaponStatData.equipmentIconPath);
            itemName = weaponStatData.equipmentName;
         } else if (item.category == Item.Category.Armor) {
            ArmorStatData armorStatData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
            image.sprite = ImageManager.getSprite(armorStatData.equipmentIconPath);
            itemName = armorStatData.equipmentName;
         } else {
            image.sprite = ImageManager.getSprite(item.getIconPath());
         }
      } else {
         if (item.data.StartsWith(Blueprint.WEAPON_DATA_PREFIX)) {
            image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_WEAPON_ICON);
         } else if (item.data.StartsWith(Blueprint.ARMOR_DATA_PREFIX)) {
            image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_ARMOR_ICON);
         }
      }

      // Create a new instance of the material we can use for recoloring
      Material sourceMaterial = MaterialManager.self.getGUIMaterial(item.getColorKey());
      if (sourceMaterial != null) {
         image.material = new Material(sourceMaterial);

         // Recolor
         floatingIcon.GetComponentInChildren<RecoloredSprite>().recolor(item.getColorKey(), item.color1, item.color2);
      }

      // Set the name text
      floatingIcon.GetComponentInChildren<FloatAndStop>().nameText.text = itemName;

      isWaitingForServerResponse = false;

      // Show a confirmation in chat
      string msg = string.Format("You found one <color=red>{0}</color>!", itemName);
      ChatManager.self.addChat(msg, ChatInfo.Type.System);
   }

   private void OnTriggerStay2D (Collider2D other) {
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

   private void deleteTreasureChestIcon () {
      Minimap.self.deleteTreasureChestIcon(this.gameObject);
   }

   public void disableChest () {
      StartCoroutine(CO_DisableChestAfterDelay());
   }

   private IEnumerator CO_DisableChestAfterDelay() {
      yield return new WaitForSeconds(5);
      gameObject.SetActive(false);
      deleteTreasureChestIcon();
   }

   private IEnumerator CO_DisableChestAfterLifetime () {
      yield return new WaitForSeconds(30);
      gameObject.SetActive(false);
      deleteTreasureChestIcon();
   }

   #region Private Variables

   // Gets set to true when the global player is nearby
   protected bool _isGlobalPlayerNearby;

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}

public enum ChestSpawnType
{
   None = 0,
   Site = 1,
   Land = 2,
   Sea = 3
}