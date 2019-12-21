using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;

public class AdminManager : NetworkBehaviour {
   #region Public Variables

   // The types of administration privileges
   public enum Type { None = 0, Admin = 1 }

   // The email address we use for test accounts
   public static string TEST_EMAIL_DOMAIN = "codecommode.com";

   // The various command strings
   protected static string ADD_GOLD = "add_gold";
   protected static string ADD_SHIPS = "add_ships";
   protected static string CREATE_TEST_USERS = "create_test_users";
   protected static string DELETE_TEST_USERS = "delete_test_users";
   protected static string SET_WEATHER = "set_weather";
   protected static string SET_WIND = "set_wind";
   protected static string CHECK_FPS = "check_fps";
   protected static string CREATE_TEST_INSTANCES = "create_test_instances";
   protected static string SHUTDOWN = "shutdown";
   protected static string SPAWN_SHIPS = "spawn_ships";
   protected static string SERVER_INFO = "server_info";
   protected static string POLYNAV_TEST = "polynav_test";
   protected static string CREATE_SHOP_SHIPS = "create_shop_ships";
   protected static string CREATE_SHOP_ITEMS = "create_shop_items";
   protected static string PORT_GO = "portgo";
   protected static string SITE_GO = "sitego";
   protected static string PLAYER_GO = "pgo";
   protected static string BOT_WAYPOINT = "bot_waypoint";
   protected static string SKIP_TUTORIAL = "skip_tutorial";
   protected static string WARP = "warp";
   protected static string ENEMY = "enemy";
   protected static string ABILITY = "all_abilities";
   protected static string NPC = "test_npc";
   protected static string GET_ITEM = "get_item";
   protected static string GET_ALL_ITEMS = "get_all_items";

   #endregion

   void Start () {
      _player = GetComponent<NetEntity>();

      // If we're testing clients, have them warp around repeatedly
      if (Util.isAutoTesting()) {
         InvokeRepeating("warpRandomly", 10f, 10f);
      }
   }

   void Update () {
      if (ChatPanel.self == null) {
         return;
      }

      // Any time the chat input is de-focused, we reset the chat history index
      if (!ChatPanel.self.inputField.isFocused) {
         _historyIndex = -1;
      }
   }

   public void handleAdminCommandString (string inputString) {
      // Split things apart based on spaces
      string[] sections = inputString.Split(' ');

      // Make sure we have the right number of sections in the input string
      if (sections.Length < 1 || sections[0].Trim().Length < 1) {
         ChatManager.self.addChat("You must specify a remote command.", ChatInfo.Type.System);
         return;
      }

      // Parse out the command and any parameters
      string adminCommand = sections[0];
      string parameters = "";
      if (inputString.Length > adminCommand.Length + 1) {
         parameters = inputString.Remove(0, adminCommand.Length + 1); // +1 for the space
      }
      adminCommand = adminCommand.Trim();
      
      // Note the command we're about to request
      D.debug("Requesting admin command: " + adminCommand + " with parameters: " + parameters);
      adminCommand = adminCommand.ToLower();

      // Keep track of it in the history list
      _history.Add("/admin " + inputString);

      if (ADD_GOLD.Equals(adminCommand)) {
         // Add to a particular user's gold amount
         requestAddGold(parameters);
      } else if (ADD_SHIPS.Equals(adminCommand)) {
         Cmd_AddShips();
      } else if (CHECK_FPS.Equals(adminCommand)) {
         Cmd_CheckFPS();
      } else if (SHUTDOWN.Equals(adminCommand)) {
         Cmd_Shutdown();
      } else if (ENEMY.Equals(adminCommand)) {
         Cmd_SpawnEnemy();
      } else if (SERVER_INFO.Equals(adminCommand)) {
         Cmd_ServerInfo();
      } else if (CREATE_SHOP_SHIPS.Equals(adminCommand)) {
         Cmd_CreateShopShips();
      } else if (CREATE_SHOP_ITEMS.Equals(adminCommand)) {
         Cmd_CreateShopItems();
      } else if (PLAYER_GO.Equals(adminCommand)) {
         requestPlayerGo(parameters);
      } else if (BOT_WAYPOINT.Equals(adminCommand)) {
         Cmd_BotWaypoint();
      } else if (SKIP_TUTORIAL.StartsWith(adminCommand)) {
         requestSkipTutorial(parameters);
      } else if (WARP.Equals(adminCommand)) {
         requestWarp(parameters);
      } else if (ABILITY.Equals(adminCommand)) {
         requestAllAbilities(parameters);
      } else if (NPC.Equals(adminCommand)) {
         Cmd_NpcTest(parameters);
      } else if (GET_ITEM.Equals(adminCommand)) {
         requestGetItem(parameters);
      } else if (GET_ALL_ITEMS.Equals(adminCommand)) {
         requestGetAllItems(parameters);
      }
   }

   public void cycleCommandHistory (int amount) {
      // Get the messages that were typed by the user, in reverse order
      List<string> history = new List<string>(_history);
      history.Reverse();

      // Add the specified amount to the current index
      _historyIndex += amount;

      // Clamp to the min/max bounds
      _historyIndex = Mathf.Clamp(_historyIndex, -1, history.Count - 1);

      // If the index is -1, we just clear out the input
      if (_historyIndex == -1) {
         ChatPanel.self.inputField.text = "";
      } else {
         ChatPanel.self.inputField.text = history[_historyIndex];
      }

      // Move the input caret to the end
      StartCoroutine(ChatPanel.self.moveCaretToEnd());
   }

   protected void requestAddGold (string parameters) {
      string[] list = parameters.Split(' ');
      int gold = 0;
      string username = "";

      try {
         username = list[0];
         gold = System.Convert.ToInt32(list[1]);
      } catch (System.Exception e) {
         D.warning("Unable to parse gold int from: " + parameters + ", exception: " + e);
         return;
      }

      // Send the request to the server
      Cmd_AddGold(gold, username);
   }

   protected void requestSkipTutorial (string parameters) {
      int stepNumber = 0;

      if (!Util.isEmpty(parameters)) {
         string[] list = parameters.Split(' ');

         try {
            stepNumber = System.Convert.ToInt32(list[0]);
         } catch (System.Exception e) {
            D.warning("Unable to parse step number int from: " + parameters + ", exception: " + e);
            return;
         }
      }

      // Send the request to the server
      Cmd_SkipTutorial(stepNumber);
   }

   protected void requestAllAbilities (string parameters) {
      List<BasicAbilityData> allAbilities = AbilityManager.self.allGameAbilities;
      Global.player.rpc.Cmd_UpdateAbilities(AbilitySQLData.TranslateBasicAbility(allAbilities).ToArray());
   }

   protected void requestPlayerGo (string parameters) {
      string[] list = parameters.Split(' ');
      string targetPlayerName = "";

      try {
         targetPlayerName = list[0];
      } catch (System.Exception e) {
         D.warning("Unable to parse from: " + parameters + ", exception: " + e);
         ChatManager.self.addChat("Not a valid player name", ChatInfo.Type.Error);
         return;
      }

      // Send the request to the server
      Cmd_PlayerGo(targetPlayerName);
   }

   protected void requestWarp (string parameters) {
      string[] list = parameters.Split(' ');
      string areaKey = list[0];
      string spawnKey = list[1];

      // Send the request to the server
      Cmd_Warp(areaKey, spawnKey);
   }

   protected void requestGetItem (string parameters) {
      string[] list = parameters.Split(' ');
      string categoryStr = "";
      string itemTypeStr = "";
      int count = 1;

      // Parse the parameters
      try {
         categoryStr = list[0];
         itemTypeStr = list[1];
         if (list.Length > 2) {
            count = int.Parse(list[2]);
         }
      } catch (System.Exception e) {
         D.warning("Unable to parse from: " + parameters + ", exception: " + e);
         return;
      }

      // Get the category
      Item.Category category;
      switch (categoryStr.Substring(0, 1).ToLower()) {
         case "w":
            category = Item.Category.Weapon;
            break;
         case "a":
            category = Item.Category.Armor;
            break;
         case "c":
            category = Item.Category.CraftingIngredients;
            break;
         case "u":
            category = Item.Category.Usable;
            break;
         case "b":
            category = Item.Category.Blueprint;
            break;
         default:
            D.error("Could not parse the item category " + categoryStr);
            return;
      }

      // Get the item type
      int itemTypeId;
      switch (category) {
         case Item.Category.Weapon:
            try {
               itemTypeId = (int) Enum.Parse(typeof(Weapon.Type), itemTypeStr);
            } catch (Exception e) {
               D.error("Could not parse the weapon type " + itemTypeStr + ", exception: " + e);
               return;
            }
            break;
         case Item.Category.Armor:
            try {
               itemTypeId = (int) Enum.Parse(typeof(Armor.Type), itemTypeStr);
            } catch (Exception e) {
               D.error("Could not parse the armor type " + itemTypeStr + ", exception: " + e);
               return;
            }
            break;
         case Item.Category.Usable:
            try {
               itemTypeId = (int) Enum.Parse(typeof(UsableItem.Type), itemTypeStr);
            } catch (Exception e) {
               D.error("Could not parse the usable item type " + itemTypeStr + ", exception: " + e);
               return;
            }
            break;
         case Item.Category.CraftingIngredients:
            try {
               itemTypeId = (int) Enum.Parse(typeof(CraftingIngredients.Type), itemTypeStr);
            } catch (Exception e) {
               D.error("Could not parse the crafting ingredient type " + itemTypeStr + ", exception: " + e);
               return;
            }
            break;
         case Item.Category.Blueprint:
            string prefix = "";

            try {
               // Try to parse a weapon with the given type
               itemTypeId = (int) Enum.Parse(typeof(Weapon.Type), itemTypeStr);
               prefix = Blueprint.WEAPON_PREFIX;
            } catch (Exception e1) {
               try {
                  // Try to parse an armor with the given type
                  itemTypeId = (int) Enum.Parse(typeof(Armor.Type), itemTypeStr);
                  prefix = Blueprint.ARMOR_PREFIX;
               } catch (Exception e2) {
                  D.error("Could not parse the blueprint type " + itemTypeStr + "\n" + e1.ToString() + "\n" + e2.ToString());
                  return;
               }
            }

            // Convert the type id to a string
            string itemTypeIdStr = itemTypeId.ToString();

            // Add the prefix
            itemTypeIdStr = prefix + itemTypeIdStr;

            // Convert back to an integer
            itemTypeId = int.Parse(itemTypeIdStr);
            break;
         default:
            D.error("Could not parse the item type " + itemTypeStr + " of category " + category.ToString());
            return;
      }

      // Send the request to the server
      Cmd_CreateItem(category, itemTypeId, count);
   }

   protected void requestGetAllItems (string parameters) {
      string[] list = parameters.Split(' ');
      int count = 100;

      // Parse the optional item count (for stacks)
      try {
         if (list.Length > 0) {
            count = int.Parse(list[0]);
         }
      } catch (System.Exception e) {
      }

      // Send the request to the server
      Cmd_CreateAllItems(count);
   }

   [Command]
   protected void Cmd_AddGold (int gold, string username) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      // Update the player's gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int userId = DB_Main.getUserId(username);

         if (userId > 0) {
            DB_Main.addGold(userId, gold);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Send confirmation back to the player who issued the command
               string message = string.Format("Added {0} gold to {1}'s inventory.", gold, username);

               // Registers the gold gains to achievement data for recording
               AchievementManager.registerUserAchievement(_player.userId, ActionType.EarnGold, gold);
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.AddGold, _player, message);
            });

         } else {
            // Send the failure message back to the client
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               string message = string.Format("Could not find user {0}.", username);
               ServerMessageManager.sendError(ErrorMessage.Type.UsernameNotFound, _player, message);
            });
         }
      });
   }

   [Command]
   protected void Cmd_CreateTestUsers (int count) {

   }

   [Command]
   protected void Cmd_DeleteTestUsers () {

   }

   [Command]
   protected void Cmd_SetWind (float x, float y) {

   }

   [Command]
   protected void Cmd_CheckFPS () {

   }

   [Command]
   protected void Cmd_Shutdown () {

   }

   [Command]
   protected void Cmd_CreateTestInstances (int count) {

   }

   [Command]
   protected void Cmd_SpawnShips (int count, Ship.Type shipType) {

   }

   [Command]
   protected void Cmd_ServerInfo () {

   }

   [Command]
   protected void Cmd_PolyNavTest (int count) {

   }

   [Command]
   protected void Cmd_CreateShopShips () {

   }

   [Command]
   protected void Cmd_CreateShopItems () {

   }

   [Command]
   protected void Cmd_AddShips () {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      foreach (Ship.Type shipType in System.Enum.GetValues(typeof(Ship.Type))) {
         Rarity.Type rarity = Rarity.Type.Uncommon;
         int speed = (int) (Ship.getBaseSpeed(shipType) * Rarity.getIncreasingModifier(rarity));
         speed = Mathf.Clamp(speed, 70, 130);
         int sailors = (int) (Ship.getBaseSailors(shipType) * Rarity.getDecreasingModifier(rarity));
         int suppliesRoom = (int) (Ship.getBaseSuppliesRoom(shipType) * Rarity.getIncreasingModifier(rarity));
         int cargoRoom = (int) (Ship.getBaseCargoRoom(shipType) * Rarity.getIncreasingModifier(rarity));
         int damage = (int) (Ship.getBaseDamage(shipType) * Rarity.getIncreasingModifier(rarity));
         int health = (int) (Ship.getBaseHealth(shipType) * Rarity.getIncreasingModifier(rarity));
         int price = (int) (Ship.getBasePrice(shipType) * Rarity.getIncreasingModifier(rarity));
         int attackRange = (int) (Ship.getBaseAttackRange(shipType) * Rarity.getIncreasingModifier(rarity));

         // Let's use nice numbers
         sailors = Util.roundToPrettyNumber(sailors);
         suppliesRoom = Util.roundToPrettyNumber(suppliesRoom);
         cargoRoom = Util.roundToPrettyNumber(cargoRoom);
         damage = Util.roundToPrettyNumber(damage);
         health = Util.roundToPrettyNumber(health);
         price = Util.roundToPrettyNumber(price);
         attackRange = Util.roundToPrettyNumber(attackRange);

         // Set up the Ship Info
         ShipInfo ship = new ShipInfo(0, _player.userId, shipType, Ship.SkinType.None, Ship.MastType.Caravel_1, Ship.SailType.Caravel_1, shipType + "",
            ColorType.None, ColorType.None, ColorType.None, ColorType.None, suppliesRoom, suppliesRoom, cargoRoom, health, health, damage, attackRange, speed, sailors, rarity);

         // Create the ship in the database
         DB_Main.createShipFromShipyard(_player.userId, ship);
      }
   }

   [Command]
   protected void Cmd_PlayerGo (string targetPlayerName) {

   }

   [Command]
   protected void Cmd_BotWaypoint () {

   }

   [Command]
   protected void Cmd_Warp (string areaKey, string spawnKey) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      if ("".Equals(spawnKey)) {
         return;
      }

      Spawn spawn = SpawnManager.self.getSpawn(areaKey, spawnKey);

      _player.spawnInNewMap(spawn.AreaKey, spawn, Direction.South);
   }

   [Command]
   protected void Cmd_SkipTutorial (int stepNumber) {
      // Stop it if it's already running
      StopCoroutine("CO_SkipTutorial");

      // Start skipping steps
      StartCoroutine(CO_SkipTutorial(stepNumber));
   }

   [Command]
   public void Cmd_SpawnEnemy () {
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin.");
         return;
      }

      // Create an Enemy in this instance
      Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
      enemy.enemyType = Enemy.Type.Lizard;

      // Add it to the Instance
      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      InstanceManager.self.addEnemyToInstance(enemy, instance);
      enemy.transform.position = _player.transform.position;
      enemy.desiredPosition = enemy.transform.position;
      NetworkServer.Spawn(enemy.gameObject);
   }

   protected IEnumerator CO_SkipTutorial (int stepNumber) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         yield break;
      }

      // Make sure they aren't already done with the tutorial
      int maxStep = System.Enum.GetValues(typeof(Step)).Cast<int>().Max();
      int highestCompletedStep = TutorialManager.getHighestCompletedStep(_player.userId);

      if (highestCompletedStep >= maxStep) {
         D.warning("This player has already completed the tutorial!");
         yield break;
      }

      foreach (Step step in System.Enum.GetValues(typeof(Step))) {
         if (step == Step.None) {
            continue;
         }

         if (stepNumber == 0 || (int) step == stepNumber) {
            _player.completedTutorialStep(step);
            yield return new WaitForSeconds(.5f);
         }
      }
   }

   protected void warpRandomly () {
      List<SpawnID> allSpawnKeys = SpawnManager.get().getAllSpawnKeys();
      SpawnID spawnKey = allSpawnKeys[UnityEngine.Random.Range(0, allSpawnKeys.Count)];

      handleAdminCommandString("warp " + spawnKey.areaKey + " " + spawnKey.spawnKey);
   }

   [Command]
   public void Cmd_NpcTest(string messg)
   {
      spawnNPCs();
      string[] serialize = MyNetworkManager.self.serializedNPCData(NPCManager.self.npcDataList);
      Rpc_NpcTestReceive(serialize);
   }

   [ClientRpc]
   public void Rpc_NpcTestReceive(string[] npcDataSerialized)
   {
      NPCData[] npcDataList = Util.unserialize<NPCData>(npcDataSerialized).ToArray();
      NPCManager.self.initializeNPCClientData(npcDataList);
      spawnNPCs();
   }

   private void spawnNPCs () {
      Vector3 playerPos = _player.transform.position;
      int indexCounter = 0;
      float offsetMagnitude = .3f;
      int gridCount = 5;
      float xOffset = -(2 * offsetMagnitude);
      float yOffset = 0;

      foreach (NPCData npcData in NPCManager.self.npcDataList) {
         if (NPCManager.self.getNPC(npcData.npcId) == null) {
            Sprite npcSprite = null;
            
            if (npcData.spritePath != null && npcData.spritePath != "") {
               npcSprite = ImageManager.getSprite(npcData.spritePath);
               if (npcSprite == null || npcSprite.name.Contains("empty_layer")) {
                  D.log("Invalid NPC Path, please complete details in NPC Editor");
                  npcSprite = ImageManager.getSprite(ImageManager.DEFAULT_NPC_PATH);
               }
            } else {
               D.log("Invalid NPC Sprite Path, please complete details in NPC Editor");
               npcSprite = ImageManager.getSprite(ImageManager.DEFAULT_NPC_PATH);
            }

            // Object Setup
            GameObject npcPref = Instantiate(PrefabsManager.self.npcPrefab.gameObject, AreaManager.self.getArea(_player.areaKey).transform);
            npcPref.transform.position = new Vector3(playerPos.x + xOffset, playerPos.y + yOffset, playerPos.z);

            // Data Setup
            NPC npc = npcPref.GetComponent<NPC>();
            npc.isDebug = true;
            npc.npcId = npcData.npcId;
            npc.GetComponent<SpriteSwap>().newTexture = npcSprite.texture;
            npc.gameObject.SetActive(true);
            npc.initData();

            // Spawn Grid Setup
            if (indexCounter % gridCount != 0) {
               xOffset += offsetMagnitude;
            } else {
               yOffset += offsetMagnitude;
            }
            indexCounter++;
         }
      }
   }

   [Command]
   protected void Cmd_CreateItem (Item.Category category, int itemTypeId, int count) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      // Go to the background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Create the item object
         Item item = new Item(-1, category, itemTypeId, count, Util.randomEnum<ColorType>(), Util.randomEnum<ColorType>(), "");

         // Write the item in the DB
         Item databaseItem = DB_Main.createItemOrUpdateItemCount(_player.userId, item);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send confirmation back to the player who issued the command
            int createdItemCount = databaseItem.count < count ? databaseItem.count : count;
            string message = string.Format("Added {0} {1} to the inventory.", createdItemCount, databaseItem.getName());
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ItemsAddedToInventory, _player, message);
         });
      });
   }

   [Command]
   protected void Cmd_CreateAllItems (int count) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      int weaponsCount = 0;
      int armorCount = 0;
      int usableCount = 0;
      int ingredientsCount = 0;
      int blueprintCount = 0;

      // Go to the background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Create all the usable
         foreach (UsableItem.Type usableType in Enum.GetValues(typeof(UsableItem.Type))) {
            if (usableType != UsableItem.Type.None) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Usable, (int) usableType, count)) {
                  usableCount++;
               }
            }
         }

         // Create all the crafting ingredients
         foreach (CraftingIngredients.Type craftingIngredientsType in Enum.GetValues(typeof(CraftingIngredients.Type))) {
            if (craftingIngredientsType != CraftingIngredients.Type.None) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.CraftingIngredients, (int) craftingIngredientsType, count)) {
                  ingredientsCount++;
               }
            }
         }

         // Create all the weapon blueprints
         foreach (Weapon.Type weaponType in Enum.GetValues(typeof(Weapon.Type))) {
            if (weaponType != Weapon.Type.None) {
               // Get the item type id
               int itemTypeId = (int) weaponType;

               // Convert the type id into a string and add the weapon prefix
               string itemTypeIdStr = Blueprint.WEAPON_PREFIX + itemTypeId.ToString();

               // Reconvert the type id into an integer
               itemTypeId = int.Parse(itemTypeIdStr);

               // Create the item
               if (createItemIfNotExistOrReplenishStack(Item.Category.Blueprint, (int) itemTypeId, count)) {
                  blueprintCount++;
               }
            }
         }

         // Create all the armor blueprints
         foreach (Armor.Type armorType in Enum.GetValues(typeof(Armor.Type))) {
            if (armorType != Armor.Type.None) {
               // Get the item type id
               int itemTypeId = (int) armorType;

               // Convert the type id into a string and add the weapon prefix
               string itemTypeIdStr = Blueprint.ARMOR_PREFIX + itemTypeId.ToString();

               // Reconvert the type id into an integer
               itemTypeId = int.Parse(itemTypeIdStr);

               // Create the item
               if (createItemIfNotExistOrReplenishStack(Item.Category.Blueprint, (int) itemTypeId, count)) {
                  blueprintCount++;
               }
            }
         }

         // Create all the armors
         foreach (Armor.Type armorType in Enum.GetValues(typeof(Armor.Type))) {
            if (armorType != Armor.Type.None) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Armor, (int) armorType, 1)) {
                  armorCount++;
               }
            }
         }

         // Create all the weapons
         foreach (Weapon.Type weaponType in Enum.GetValues(typeof(Weapon.Type))) {
            if (weaponType != Weapon.Type.None) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Weapon, (int) weaponType, 1)) {
                  weaponsCount++;
               }
            }
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send confirmation back to the player who issued the command
            string message = string.Format("Added {0} weapons, {1} armors, {2} usable items, {3} ingredients and {4} blueprints to the inventory.",
               weaponsCount, armorCount, usableCount, ingredientsCount, blueprintCount);
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ItemsAddedToInventory, _player, message);
         });
      });
   }

   [Server]
   private bool createItemIfNotExistOrReplenishStack (Item.Category category, int itemTypeId, int count) {
      bool wasItemCreated = false;

      // Retrieve the item from the user inventory, if it exists
      Item existingItem = DB_Main.getFirstItem(_player.userId, category, itemTypeId);

      if (existingItem == null) {
         // If the item does not exist, create a new one
         Item baseItem = new Item(-1, category, itemTypeId, count, Util.randomEnum<ColorType>(), Util.randomEnum<ColorType>(), "").getCastItem();
         DB_Main.createItemOrUpdateItemCount(_player.userId, baseItem);
         wasItemCreated = true;
      } else {
         // If the item can be stacked and there are less items than what is requested, replenish the stack
         if (existingItem.canBeStacked() && existingItem.count < count) {
            DB_Main.updateItemQuantity(_player.userId, existingItem.id, count);
            wasItemCreated = true;
         }
      }

      return wasItemCreated;
   }

   #region Private Variables

   // A history of commands we've requested
   protected List<string> _history = new List<string>();

   // The index of our chat history we're currently using in the input field, if any
   protected int _historyIndex = -1;

   // Our associated Player object
   protected NetEntity _player;

   #endregion
}