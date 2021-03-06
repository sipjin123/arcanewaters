using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

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

   // The spawn id of the chest based on the map data
   [SyncVar]
   public int chestSpawnId;

   // Our sprite renderer
   public SpriteRenderer spriteRenderer;

   // The list of user IDs that have opened this chest
   public SyncList<int> userIds = new SyncList<int>();

   // This is a list of user ids that are allowed to interact with this loot bag, this is so other users cannot loot a loot bag that was not spawned by the enemy they defeated
   public SyncList<int> allowedUserIds = new SyncList<int>();

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

   // If this gameobject is expired and should no longer render
   [SyncVar]
   public bool isExpired;

   // The custom sprite path
   [SyncVar]
   public string customSpritePath;

   // If this object is waiting for a server response (for animation sync purposes)
   public bool isWaitingForServerResponse;

   // The standard sprite count of a chest open animation to be based upon
   public const int DEFAULT_SPRITES_PER_SHEET = 24;

   // The player pref key for saving a treasure state
   public const string PREF_CHEST_STATE = "TREASURE_CHEST_STATE_";

   // If is interacted locally
   public bool isLocallyInteracted;

   // The areakey
   [SyncVar]
   public string areaKey;

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

      // Check if this chest is marked as interacted
      if (Global.userObjects != null && chestType == ChestSpawnType.Site) {
         int userId = Global.userObjects.userInfo.userId;
         if (PlayerPrefs.GetInt(PREF_CHEST_STATE + "_" + userId + "_" + areaKey + "_" + chestSpawnId, 0) == 1 && userId > 0) {
            isLocallyInteracted = true;
         } 
      } 

      // Disables opened loot bags
      if (isExpired || (hasBeenOpened() && (chestType == ChestSpawnType.Land || chestType == ChestSpawnType.Sea))) {
         gameObject.SetActive(false);
      }
   }

   public void Update () {
      // Activate certain things when the global player is nearby
      arrowContainer.SetActive(_isGlobalPlayerNearby && !hasBeenOpened());

      if (chestType == ChestSpawnType.Site) {
         // We only enable the box collider for clients in the relevant instance
         boxCollider.enabled = (Global.player != null && Global.player.instanceId == this.instanceId);
      } else if (chestType == ChestSpawnType.Sea || chestType == ChestSpawnType.Land) {
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
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asTooFar();
         return;
      }

      // If the user is NOT part of the sync list of allowed user interaction, do not proceed and send out a warning
      if (!allowedUserIds.Contains(Global.player.userId) && chestType != ChestSpawnType.Site && chestType != ChestSpawnType.Sea) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asInvalidLoot();
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

      Instance instance = InstanceManager.self.getInstance(instanceId);
      string areaKey = instance.areaKey;
      Biome.Type biome = instance.biome;
      List<TreasureDropsData> treasureDropsList = TreasureDropsDataManager.self.getTreasureDropsFromBiome(biome).OrderBy(_=>_.spawnChance).ToList();

      foreach (TreasureDropsData treasureDropsEntry in treasureDropsList) {
         float randomizer = Random.Range(0, 100);
         if (randomizer < treasureDropsEntry.spawnChance) {
            return treasureDropsEntry.item;
         }
      }

      return new Item {
         category = Item.Category.CraftingIngredients,
         count = 1,
         itemTypeId = (int) CraftingIngredients.Type.Wood,
      };
   }

   public Item getSeaMonsterLootContents () {
      SeaMonsterEntity.Type monsterType = (SeaMonsterEntity.Type) enemyType;
      SeaMonsterEntityData battlerData = SeaMonsterManager.self.getMonster(monsterType);

      List<TreasureDropsData> treasureDropsDataList = TreasureDropsDataManager.self.getTreasureDropsById(battlerData.lootGroupId);
      return processItemChance(treasureDropsDataList);
   }

   public Item getLandMonsterLootContents () {
      Enemy.Type monsterType = (Enemy.Type) enemyType;
      BattlerData battlerData = MonsterManager.self.getBattlerData(monsterType);

      List<TreasureDropsData> treasureDropsDataList = TreasureDropsDataManager.self.getTreasureDropsById(battlerData.lootGroupId);
      return processItemChance(treasureDropsDataList);
   }

   private Item processItemChance (List<TreasureDropsData> treasureDropsDataList) {
      if (treasureDropsDataList.Count > 0) {
         foreach (TreasureDropsData treasureDropData in treasureDropsDataList.OrderBy(_ => _.spawnChance)) {
            float randomizer = Random.Range(0, 100);
            if (randomizer < treasureDropData.spawnChance) {
               return treasureDropData.item;
            }
         }
      }
      return treasureDropsDataList[0].item;
   }

   public bool hasBeenOpened () {
      if (isLocallyInteracted) {
         return true;
      }

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
            ArmorStatData armorStatData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
            image.sprite = ImageManager.getSprite(armorStatData.equipmentIconPath);
            itemName = armorStatData.equipmentName;
         } else if (item.category == Item.Category.CraftingIngredients) {
            string iconPath = CraftingIngredients.getBorderlessIconPath((CraftingIngredients.Type) item.itemTypeId);
            image.sprite = ImageManager.getSprite(iconPath);
         } else {
            image.sprite = ImageManager.getSprite(item.getIconPath());
         }
      } else {
         CraftableItemRequirements craftableData = CraftingManager.self.getCraftableData(item.itemTypeId);
         if (craftableData == null) {
            D.debug("Failed to load Crafting Data of: " + item.itemTypeId);
            yield return null;
         } else {
            if (item.data.StartsWith(Blueprint.WEAPON_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_WEAPON_ICON);
            } else if (item.data.StartsWith(Blueprint.ARMOR_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_ARMOR_ICON);
            }
            item.itemTypeId = craftableData.resultItem.itemTypeId;
            itemName = EquipmentXMLManager.self.getItemName(item);
         }
      }

      // Create a new instance of the material we can use for recoloring
      Material sourceMaterial = MaterialManager.self.getGUIMaterial();
      if (sourceMaterial != null) {
         image.material = new Material(sourceMaterial);

         // Recolor
         floatingIcon.GetComponentInChildren<RecoloredSprite>().recolor(item.paletteNames);
      }

      // Set the name text
      floatingIcon.GetComponentInChildren<FloatAndStop>().nameText.text = itemName;

      isWaitingForServerResponse = false;

      // Show a confirmation in chat
      if (itemName.Length < 1) {
         D.debug("Invalid Item Name: The item found is: " + item.category + " : " + item.itemTypeId + " : " + item.data);
      } else {
         string msg = string.Format("You found one <color=red>{0}</color>!", itemName);
         ChatManager.self.addChat(msg, ChatInfo.Type.System);
      }
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
      // Delete chest icon, only if not in batchmode
      if (!Util.isBatch()) {
         Minimap.self.deleteTreasureChestIcon(this.gameObject);
      }
   }

   public void disableChest () {
      StartCoroutine(CO_DisableChestAfterDelay());
   }

   private IEnumerator CO_DisableChestAfterDelay() {
      yield return new WaitForSeconds(5);
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