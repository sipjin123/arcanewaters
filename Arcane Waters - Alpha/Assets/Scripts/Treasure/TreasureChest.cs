using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using DG.Tweening;
using System;

public class TreasureChest : NetworkBehaviour {
   #region Public Variables

   // The unique ID assigned to this chest
   [SyncVar]
   public int id;

   // The instance id
   [SyncVar]
   public int instanceId;

   // The time at which this chest was created
   [SyncVar]
   public long creationTime;

   // The spawn id of the chest based on the map data
   [SyncVar]
   public int chestSpawnId;

   // The chest content rarity
   [SyncVar]
   public Rarity.Type rarity;

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

   // Our trigger collider for the automatically opening treasure - used only for sea and land treasure bags
   public CircleCollider2D autoOpenCollider;

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

   // The loot group id reference
   public int lootGroupId;

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
      
      if (Global.player && allowedUserIds.Contains(Global.player.userId)) {
         Minimap.self.addTreasureChestIcon(this.gameObject);
      }

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
      } else {
         // Modify sprites based on rarity
         if (chestType == ChestSpawnType.Site) {
            Sprite[] customSprites = ImageManager.getSprites("Sprites/Treasure/Treasure" + rarity.ToString());
            spriteRenderer.sprite = customSprites[0];
            openedChestSprite = customSprites[customSprites.Length - 1];
         } else if (chestType == ChestSpawnType.Land) {
            spriteRenderer.sprite = ImageManager.getSprite("Sprites/Treasure/BagLandClosed" + rarity.ToString());
            openedChestSprite = ImageManager.getSprite("Sprites/Treasure/BagLandOpen" + rarity.ToString());
         } else if (chestType == ChestSpawnType.Sea) {
            spriteRenderer.sprite = ImageManager.getSprites("Sprites/Treasure/BagSeaClosed" + rarity.ToString())[0];
            openedChestSprite = ImageManager.getSprites("Sprites/Treasure/BagSeaOpen" + rarity.ToString())[0];
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

      // If the user is NOT part of the sync list of allowed user interaction, just disable this loot as the user will not be able to open it anyway
      if (Global.player && !allowedUserIds.Contains(Global.player.userId) && chestType != ChestSpawnType.Site) {
         gameObject.SetActive(false);
      }

      if (gameObject.activeInHierarchy) {
         StartCoroutine(CO_ReparentObject());
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

   private IEnumerator CO_ReparentObject () {
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return 0;
      }
      Area newArea = AreaManager.self.getArea(areaKey);
      bool worldPositionStays = newArea.cameraBounds.bounds.Contains((Vector2) transform.position);
      transform.SetParent(newArea.prefabParent, worldPositionStays);
   }

   public void sendOpenRequest () {
      if (hasBeenOpened()) {
         return;
      }

      // Check that the local player and chest are in the same instance
      if (Global.player == null || Global.player.instanceId != instanceId) {
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

      if ((chestType == ChestSpawnType.Sea && Global.player is PlayerShipEntity) || (chestType == ChestSpawnType.Land && Global.player is PlayerBodyEntity)) {
         Global.player.rpc.Cmd_OpenLootBag(this.id);
      } else {
         if (Global.player is PlayerBodyEntity) {
            // In league treasure sites, all enemies must be defeated before opening the map fragment chest
            Instance instance = Global.player.getInstance();
            if (GroupInstanceManager.isTreasureSiteArea(instance.areaKey) && instance.groupInstanceId > 0 && instance.aliveNPCEnemiesCount > 0) {
               FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asEnemiesAround();
               return;
            }

            Global.player.rpc.Cmd_OpenChest(this.id);
         } else {
            D.debug("Error here! Only players are allowed to open chests");
         }
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

   public Item getContents (int userId) {
      if (chestType == ChestSpawnType.Sea) {
         return getSeaMonsterLootContents(userId);
      } else if (chestType == ChestSpawnType.Land) {
         return getLandMonsterLootContents(userId);
      }

      Instance instance = InstanceManager.self.getInstance(instanceId);
      Biome.Type biome = instance.biome;
      bool getTreasureDropsByGroupId = (lootGroupId != TreasureDropsData.EMPTY_DROPS || lootGroupId > 0);
      List<TreasureDropsData> treasureDropsList = getTreasureDropsByGroupId
         ? TreasureDropsDataManager.self.getTreasureDropsById(lootGroupId, rarity) 
         : TreasureDropsDataManager.self.getTreasureDropsFromBiome(biome, rarity).ToList();
      string itemList = "Processing Chest Contents {" + lootGroupId + "} found a total of {" + treasureDropsList.Count + "} items";

      int randomChestItemCounter = 3;
      for (int i = 0; i < randomChestItemCounter; i ++) {
         // Try to fetch biome based drops if no treasure content is found
         if (treasureDropsList.Count < 1) {
            D.debug("Could not get contents from loot group: {" + lootGroupId + "}{" + biome + "}{" + rarity + "}");
            treasureDropsList = TreasureDropsDataManager.self.getTreasureDropsFromBiome(biome, rarity).ToList();
            itemList = "Processing Chest Contents from Biome {" + biome + "}:{" + rarity + "}:{" + lootGroupId + "} found a total of {" + treasureDropsList.Count + "} items";
         }
         D.adminLog(itemList, D.ADMIN_LOG_TYPE.Treasure);

         if (treasureDropsList.Count < 1) {
            // If no treasure content was fetched still, find the next viable content
            D.error("There are no treasure drops generated for user {" + userId + "} Biome:{" + biome + "} Checking alternative item: G:{" + lootGroupId + "} R:{" + rarity + "}");
            Item nextAvailableItem = getNextAvailableItemByRarity(rarity);
            if (nextAvailableItem != null && Item.isValidItem(nextAvailableItem)) {
               D.debug("Treasure Chest-A: Valid item selected! Id:{" + lootGroupId + "} Rarity:{" + rarity + "} Biome:{" + biome + "} " +
                  ":: Item:{" + nextAvailableItem.category + ":" + nextAvailableItem.itemTypeId + ":" + nextAvailableItem.data + "}");
               return nextAvailableItem;
            } else {
               D.error("Treasure Chest: There is no next available item by rarity! Returning Wood! Id:{" + lootGroupId + "} Rarity:{" + rarity + "} Biome:{" + biome + "}");
            }
         } else {
            // Process random entry between the fetched content
            TreasureDropsData randomEntry = treasureDropsList.ChooseRandom();
            if (randomEntry.item != null) {
               randomEntry.item.count = assignItemCount(randomEntry);
               if (Item.isValidItem(randomEntry.item)) {
                  D.debug("Treasure Chest-B: Valid item selected! Id:{" + lootGroupId + "} Rarity:{" + rarity + "} Biome:{" + biome + "} " +
                     ":: Item:{" + randomEntry.item.category + ":" + randomEntry.item.itemTypeId + ":" + randomEntry.item.data + "}");
                  return randomEntry.item;
               } else {
                  D.error("Treasure Chest: This is not a valid item {" + randomEntry.item.category + ":" + randomEntry.item.itemTypeId + "}{" + randomEntry.item.data + ":" + randomEntry.item.id + "}");
               }
            } else {
               D.error("Treasure Chest: Random Entry found is: NULL for biome {" + biome + "}");
            }
         }

         D.error("Treasure Chest: was generating incorrect item, retry {" + i + "/" + randomChestItemCounter + "} Id:{" + lootGroupId + "} Rarity:{" + rarity + "} Biome:{" + biome + "}");
      }
      return Item.defaultLootItem();
   }

   public Item getNextAvailableItemByRarity (Rarity.Type rarity) {
      if (rarity != Rarity.Type.Common && rarity != Rarity.Type.None) {
         // Iterate in descending order if rarity is greater than "common"
         for (int i = (int) rarity; i > 0; i--) {
            Item iteratedItem = getNextAvailableItem(i);
            if (iteratedItem != null) {
               return iteratedItem;
            }
         }
      } else {
         // Iterate in ascending order if rarity is common, will get the next rarity type item onwards
         int rarityMaxCount = Enum.GetValues(typeof(Rarity.Type)).Length;
         for (int i = 1; i < rarityMaxCount; i++) {
            Item iteratedItem = getNextAvailableItem(i);
            if (iteratedItem != null) {
               return iteratedItem;
            }
         }
      }

      return null;
   }

   public Powerup.Type getPowerUp () {
      SeaMonsterEntityData seaMonsterData = SeaMonsterManager.self.getMonster(enemyType);

      if (seaMonsterData == null) {
         D.debug("Warning! Sea Enemy Data {" + enemyType + "} does not exist!");
         return Powerup.Type.SpeedUp;
      }

      List<TreasureDropsData> treasureContent = TreasureDropsDataManager.self.getTreasureDropsById(seaMonsterData.lootGroupId, rarity).FindAll(_ => _.powerUp != Powerup.Type.None);
      if (treasureContent.Count > 0) {
         TreasureDropsData treasureDropsData = treasureContent.ChooseRandom();
         return treasureDropsData.powerUp;
      }

      D.debug("Warning! Sea Enemy Data {" + enemyType + "} does not have the right powerup for {" + seaMonsterData.lootGroupId + " : " + rarity + "}");
      return Powerup.Type.SpeedUp;
   }

   public Item getSeaMonsterLootContents (int userId) {
      SeaMonsterEntityData seaMonsterData = SeaMonsterManager.self.getMonster(enemyType);
      if (seaMonsterData == null) {
         D.debug("Warning! Sea Enemy Data {" + enemyType + "} does not exist!");
         return Item.defaultLootItem();
      }
      D.adminLog("Getting sea monster contents {" + seaMonsterData.lootGroupId + "}", D.ADMIN_LOG_TYPE.Treasure);
      return getGenericMonsterContent(seaMonsterData.lootGroupId, seaMonsterData.monsterName, false, userId);
   }

   public Item getLandMonsterLootContents (int userId) {
      Enemy.Type monsterType = (Enemy.Type) enemyType;
      BattlerData battlerData = MonsterManager.self.getBattlerData(monsterType);
      D.adminLog("Getting land monster contents {" + battlerData.lootGroupId + "}", D.ADMIN_LOG_TYPE.Treasure);
      return getGenericMonsterContent(battlerData.lootGroupId, monsterType.ToString(), true, userId);
   }

   private Item getGenericMonsterContent (int lootGroupId, string monsterName, bool isLandMonster, int userId) {
      this.lootGroupId = lootGroupId;

      List<TreasureDropsData> treasureDropsDataList = TreasureDropsDataManager.self.getTreasureDropsById(lootGroupId, rarity);
      if (treasureDropsDataList.Count < 1) {
         D.adminLog("Insufficient Data Here! Something went wrong with treasure drops (Blank List) when slaying {" + monsterName + "}, Loot ID: {" + lootGroupId + "} Rarity is {" + rarity + "} Finding alternative item", D.ADMIN_LOG_TYPE.Treasure);
      } else {
         // TODO: Remove this after polishing item drops
         foreach (TreasureDropsData newData in treasureDropsDataList) {
            if (newData.item != null) {
               newData.item.count = assignItemCount(newData);
               if (Item.isValidItem(newData.item)) {
                  D.adminLog("Treasure drops Content :: " +
                     "Category: {" + newData.item.category + "} " +
                     "TypeID: {" + newData.item.itemTypeId + "} " +
                     "Data: {" + newData.item.data + "}", D.ADMIN_LOG_TYPE.Treasure);
               }
            }
         }

         // If there is a valid item found in the loot group with the matching rarity, select random item of same rarity
         TreasureDropsData treasureData = treasureDropsDataList.ChooseRandom();
         if (treasureData != null && treasureData.item != null) {
            treasureData.item.count = assignItemCount(treasureData);
            if (Item.isValidItem(treasureData.item)) {
               return treasureData.item;
            }
         } else {
            D.debug("Error here! Something went wrong with treasure drops (NULL Data)," +
               " Loot ID: {" + lootGroupId + "} Rarity is {" + rarity + "} for monster {" + monsterName + "}");
         }
      }

      if (isLandMonster) {
         Item nextAvailableItem = getNextAvailableItemByRarity(rarity);
         if (nextAvailableItem != null) {
            return nextAvailableItem;
         }
         
         // If no valid items are stored in the loot group, return defaul item "Wood"
         return Item.defaultLootItem();
      } else {
         // If is a sea monster, blank items are allowed which will automatically randomize into a powerup loot 
         return new Item();
      }
   }

   private int assignItemCount (TreasureDropsData treasureData) {
      return UnityEngine.Random.Range(treasureData.dropMinCount > 0 ? treasureData.dropMinCount : 1, treasureData.dropMaxCount);
   }

   private Item getNextAvailableItem (int rarityIndex) {
      // Check all rarity types that are not None
      if (rarityIndex > 0) {
         // Iterate through the rarity items within the treasure drops data set, starting from common up to legendary
         List<TreasureDropsData> substituteDropsDataList = TreasureDropsDataManager.self.getTreasureDropsById(lootGroupId, (Rarity.Type) rarityIndex);

         // Check if the web tool content has the item that matches the rarity
         if (substituteDropsDataList.Count > 0) {
            // Choose a random item within the loot group data
            TreasureDropsData randomSubstituteData = substituteDropsDataList.ChooseRandom();

            // If the random item in this rarity set is valid, select this item
            if (randomSubstituteData.item != null) {
               randomSubstituteData.item.count = assignItemCount(randomSubstituteData);
               if (Item.isValidItem(randomSubstituteData.item)) {
                  D.debug("Has randomly selected next best item :: " +
                     "Rarity: " + (Rarity.Type) rarityIndex + " " +
                     "Category: " + randomSubstituteData.item.category + " " +
                     "Type: " + randomSubstituteData.item.itemTypeId);
                  return randomSubstituteData.item;
               } else {
                  D.debug("Could not process next available item! Invalid: " + randomSubstituteData.item.category + ":" + randomSubstituteData.item.itemTypeId);
               }
            }
         }
      }

      return null;
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
      animationDuration = Mathf.Clamp(animationDuration, .1f, 1);
      yield return new WaitForSeconds(animationDuration);

      // Create a floating icon
      GameObject floatingIcon = Instantiate(TreasureManager.self.floatingIconPrefab, Vector3.zero, Quaternion.identity);
      floatingIcon.transform.SetParent(this.transform);
      floatingIcon.transform.localPosition = new Vector3(0f, .04f);
      Image image = floatingIcon.GetComponentInChildren<Image>();
      string itemName = item.getName();
      if (item.category != Item.Category.Blueprint) {
         if (item.category == Item.Category.Weapon) {
            WeaponStatData weaponStatData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            if (weaponStatData != null) {
               image.sprite = ImageManager.getSprite(weaponStatData.equipmentIconPath);
               itemName = weaponStatData.equipmentName;
            }
         } else if (item.category == Item.Category.Armor) {
            ArmorStatData armorStatData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
            if (armorStatData != null) {
               image.sprite = ImageManager.getSprite(armorStatData.equipmentIconPath);
               itemName = armorStatData.equipmentName;
            }
         } else if (item.category == Item.Category.Hats) {
            HatStatData hatStatData = EquipmentXMLManager.self.getHatDataByItemType(item.itemTypeId);
            if (hatStatData != null) {
               image.sprite = ImageManager.getSprite(hatStatData.equipmentIconPath);
               itemName = hatStatData.equipmentName;
            }
         } else if (item.category == Item.Category.Ring) {
            RingStatData ringStatData = EquipmentXMLManager.self.getRingData(item.itemTypeId);
            if (ringStatData != null) {
               image.sprite = ImageManager.getSprite(ringStatData.equipmentIconPath);
               itemName = ringStatData.equipmentName;
            }
         } else if (item.category == Item.Category.Trinket) {
            TrinketStatData trinketStatData = EquipmentXMLManager.self.getTrinketData(item.itemTypeId);
            if (trinketStatData != null) {
               image.sprite = ImageManager.getSprite(trinketStatData.equipmentIconPath);
               itemName = trinketStatData.equipmentName;
            }
         } else if (item.category == Item.Category.Necklace) {
            NecklaceStatData necklaceStatData = EquipmentXMLManager.self.getNecklaceData(item.itemTypeId);
            if (necklaceStatData != null) {
               image.sprite = ImageManager.getSprite(necklaceStatData.equipmentIconPath);
               itemName = necklaceStatData.equipmentName;
            }
         } else if (item.category == Item.Category.Quest_Item) {
            QuestItem questItem = EquipmentXMLManager.self.getQuestItemById(item.itemTypeId);
            if (questItem != null) {
               image.sprite = ImageManager.getSprite(questItem.iconPath);
               itemName = questItem.itemName;
            }
         } else if (item.category == Item.Category.CraftingIngredients) {
            string iconPath = CraftingIngredients.getBorderlessIconPath((CraftingIngredients.Type) item.itemTypeId);
            image.sprite = ImageManager.getSprite(iconPath);
         } else {
            image.sprite = ImageManager.getSprite(item.getIconPath());
         }
      } else {
         CraftableItemRequirements craftableData = CraftingManager.self.getCraftableData(item.itemTypeId);
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.Loot_Blueprint);
         if (craftableData == null) {
            D.debug("Failed to load Crafting Data of: " + item.itemTypeId);
            yield return null;
         } else {
            if (item.data.StartsWith(Blueprint.WEAPON_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_WEAPON_ICON);
            } else if (item.data.StartsWith(Blueprint.ARMOR_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_ARMOR_ICON);
            } else if (item.data.StartsWith(Blueprint.HAT_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_HAT_ICON);
            } else if (item.data.StartsWith(Blueprint.INGREDIENT_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_INGREDIENT_ICON);
            } else if (item.data.StartsWith(Blueprint.RING_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_RING_ICON);
            } else if (item.data.StartsWith(Blueprint.NECKLACE_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_NECKLACE_ICON);
            } else if (item.data.StartsWith(Blueprint.TRINKET_DATA_PREFIX)) {
               image.sprite = ImageManager.getSprite(Blueprint.BLUEPRINT_TRINKET_ICON);
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
      FloatAndStop floatingComponent = floatingIcon.GetComponentInChildren<FloatAndStop>();
      if (floatingComponent.quantityText != null) {
         floatingComponent.nameText.text = itemName;
         if (item.count > 1) {
            floatingComponent.quantityText.text = item.count.ToString();
            floatingComponent.quantityText.gameObject.SetActive(true);
         }
      }

      isWaitingForServerResponse = false;

      // Show a confirmation in chat
      if (itemName.Length < 1) {
         Instance instance = InstanceManager.self.getInstance(instanceId);
         Biome.Type biome = instance == null ? Biome.Type.None : instance.biome;
         D.debug("TreasureChest:Invalid Item Name: The item found is: {" + item.category + "}:{" + item.itemTypeId + "}:{" + item.data + "}:{" + biome + "}");
      } else {
         string format = "You found <color=yellow>{0}</color> <color=red>{1}</color>";
         
         if (item.category == Item.Category.Blueprint) {
            format += " (Blueprint)";
         }

         string msg = string.Format(format, item.count, itemName);
         ChatManager.self.addChat(msg, ChatInfo.Type.System);
      }
   }

   public IEnumerator CO_CreatingFloatingMapFragmentIcon () {
      // Give some time for the chest to open
      float animationDuration = chestOpeningAnimation.frameLengthOverride * chestOpeningAnimation.maxIndex;
      animationDuration = Mathf.Clamp(animationDuration, .1f, 1);
      yield return new WaitForSeconds(animationDuration);

      // Create a floating icon
      GameObject floatingIcon = Instantiate(TreasureManager.self.floatingMapFragmentPrefab, Vector3.zero, Quaternion.identity);
      floatingIcon.transform.SetParent(this.transform);
      floatingIcon.transform.localPosition = new Vector3(0f, .04f);
      
      // Set the name text
      floatingIcon.GetComponentInChildren<FloatAndStop>().nameText.text = TreasureManager.MAP_FRAGMENT_NAME;

      isWaitingForServerResponse = false;

      // Show a confirmation in chat
      string msg = string.Format("You found a (<color=red>{0}</color>)", TreasureManager.MAP_FRAGMENT_NAME);
      ChatManager.self.addChat(msg, ChatInfo.Type.System);
   }

   public IEnumerator CO_CreatingFloatingPowerupIcon (Powerup.Type powerupType, Rarity.Type powerupRarity, PlayerShipEntity player) {
      // Give some time for the chest to open
      float animationDuration = chestOpeningAnimation.frameLengthOverride * chestOpeningAnimation.maxIndex;
      animationDuration = Mathf.Clamp(animationDuration, .1f, 1);
      yield return new WaitForSeconds(animationDuration);

      // Create the popup icon, make it scale up in size
      PowerupPopupIcon popupIcon = Instantiate(TreasureManager.self.powerupPopupIcon, Vector3.zero, Quaternion.identity).GetComponent<PowerupPopupIcon>();
      popupIcon.transform.SetParent(this.transform);
      popupIcon.transform.localPosition = new Vector3(0.0f, 0.04f);
      popupIcon.transform.SetParent(AreaManager.self.getArea(areaKey).transform);
      popupIcon.init(powerupType, powerupRarity);
      popupIcon.transform.localScale = Vector3.one * 0.25f;
      popupIcon.transform.DOScale(1.0f, 0.8f).SetEase(Ease.InElastic);
      yield return new WaitForSeconds(0.4f);

      // After a delay, have the popup icon move upwards
      // Play sfx
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.PICKUP_POWERUP, transform.position);

      popupIcon.transform.DOBlendableLocalMoveBy(Vector3.up * 0.3f, 0.4f).SetEase(Ease.OutSine);
      yield return new WaitForSeconds(1.4f);

      // After another delay, have the popup icon move towards the player
      popupIcon.gravitateToPlayer(player, 1.0f);

      isWaitingForServerResponse = false;

      // Show a confirmation in chat
      string powerupName = PowerupManager.self.getPowerupData(powerupType).powerupName;
      string msg = string.Format("You received powerup! <color=red>{0}</color>!", powerupName);
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

      // Auto-opening should be enabled only for sea/land treasure bags
      if (chestType == ChestSpawnType.Site) {
         return;
      }

      // If our player enters the treasure bag, automatically send request to open it
      if (entity != null && Global.player != null && entity.userId == Global.player.userId && !Global.player.isDead()) {
         // Ensure that correct player has entered correct trigger
         if (other.IsTouching(autoOpenCollider)) {
            sendOpenRequest();
         }
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