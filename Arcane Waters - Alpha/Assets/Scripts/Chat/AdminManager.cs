﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MapCreationTool.Serialization;
using NubisDataHandling;

public class AdminManager : NetworkBehaviour
{
   #region Public Variables

   // The email address we use for test accounts
   public static string TEST_EMAIL_DOMAIN = "codecommode.com";

   public enum Type
   {
      AddGold = 0,
      AddShips = 1,
      CreateTestUsers = 2,
      DeleteTestUsers = 3,
      SetWeather = 4,
      SetWind = 5,
      CheckFps = 6,
      CreateTestInstances = 7,
      Shutdown = 8,
      SpawnShips = 9,
      ServerInfo = 10,
      PolynavTest = 11,
      CreateShopShips = 12,
      CreateShopItems = 13,
      PortGo = 14,
      ShipSpeedUp = 15,
      SiteGo = 16,
      PlayerGo = 17,
      BotWaypoint = 18,
      Warp = 19,
      Enemy = 20,
      Ability = 21,
      NPC = 22,
      GetItem = 23,
      GetAllItems = 24,
      InteractAnvil = 25,
      ScheduleServerRestart = 26,
      CancelServerRestart = 27,
      FreeWeapon = 28,
      GunAbilities = 29,
      GetAllHats = 30,
      GoToPlayer = 31,
      SummonPlayer = 32,
      TeachAbility = 33,
      SpawnEnemy = 34,
      MutePlayer = 35,
      ToggleInvisibility = 36,
      SpawnPirateShip = 37,
      SpawnPrivateerShip = 38,
      CreateVoyageMap = 39,
      ShipDamage = 40,
      ShipHealth = 41
   }

   #endregion

   void Start () {
      _player = GetComponent<NetEntity>();

      // If we're testing clients, have them warp around repeatedly
      if (Util.isAutoTesting()) {
         InvokeRepeating("warpRandomly", 10f, 10f);
      }

      // Request the list of blueprints from the server
      if (isLocalPlayer && _blueprintNames.Count == 0) {
         // TODO: Insert fetch blueprint data here
      }

      addCommands();
   }

   private void addCommands () {
      _commands[Type.AddGold] = "add_gold";
      _commands[Type.AddShips] = "add_ships";
      _commands[Type.CreateTestUsers] = "create_test_users";
      _commands[Type.DeleteTestUsers] = "delete_test_users";
      _commands[Type.SetWeather] = "set_weather";
      _commands[Type.SetWind] = "set_wind";
      _commands[Type.CheckFps] = "check_fps";
      _commands[Type.CreateTestInstances] = "create_test_instances";
      _commands[Type.Shutdown] = "shutdown";
      _commands[Type.SpawnShips] = "spawn_ships";
      _commands[Type.ServerInfo] = "server_info";
      _commands[Type.PolynavTest] = "polynav_test";
      _commands[Type.CreateShopShips] = "create_shop_ships";
      _commands[Type.CreateShopItems] = "create_shop_items";
      _commands[Type.PortGo] = "portgo";
      _commands[Type.ShipSpeedUp] = "ship_speedup";
      _commands[Type.SiteGo] = "sitego";
      _commands[Type.PlayerGo] = "pgo";
      _commands[Type.BotWaypoint] = "bot_waypoint";
      _commands[Type.Warp] = "warp";
      _commands[Type.Enemy] = "enemy";
      _commands[Type.Ability] = "all_abilities";
      _commands[Type.NPC] = "test_npc";
      _commands[Type.GetItem] = "get_item";
      _commands[Type.GetAllItems] = "get_all_items";
      _commands[Type.InteractAnvil] = "interact_anvil";
      _commands[Type.ScheduleServerRestart] = "restart";
      _commands[Type.CancelServerRestart] = "cancel";
      _commands[Type.FreeWeapon] = "free_weapon";
      _commands[Type.GunAbilities] = "gun_abilities";
      _commands[Type.GetAllHats] = "get_all_hats";
      _commands[Type.GoToPlayer] = "goto";
      _commands[Type.SummonPlayer] = "summon";
      _commands[Type.TeachAbility] = "learn";
      _commands[Type.SpawnEnemy] = "spawn";
      _commands[Type.MutePlayer] = "mute";
      _commands[Type.ToggleInvisibility] = "invisible";
      _commands[Type.SpawnPirateShip] = "spawn_pirate";
      _commands[Type.SpawnPrivateerShip] = "spawn_privateer";
      _commands[Type.CreateVoyageMap] = "create_voyage";
      _commands[Type.ShipDamage] = "ship_damage";
      _commands[Type.ShipHealth] = "ship_health"; 
   }

   void Update () {
      if (ChatPanel.self == null) {
         return;
      }

      // Any time the chat input is de-focused, we reset the chat history index
      if (!ChatPanel.self.inputField.isFocused) {
         _historyIndex = -1;
      }

      // Move player to position on certain key combination
      if (_player.isLocalPlayer && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetMouseButtonDown(0)) {
         Area area = _player.areaKey == null ? null : AreaManager.self.getArea(_player.areaKey);
         if (!_player.isInBattle() && area != null) {
            Vector2 wPos = CameraManager.defaultCamera.getCamera().ScreenToWorldPoint(Input.mousePosition);
            warpToPosition(area.transform.InverseTransformPoint(wPos));
         }
      }
   }

   public Dictionary<Type, string> getCommandList () {
      return _commands;
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

      if (!_player.isAdmin()) {
         string msg = string.Format("<color=red>{0}</color>!", "This account does not have Admin privileges.");
         ChatManager.self.addChat(msg, ChatInfo.Type.System);
         return;
      }

      if (_commands[Type.AddGold].Equals(adminCommand)) {
         // Add to a particular user's gold amount
         requestAddGold(parameters);
      } else if (_commands[Type.AddShips].Equals(adminCommand)) {
         Cmd_AddShips();
      } else if (_commands[Type.CheckFps].Equals(adminCommand)) {
         Cmd_CheckFPS();
      } else if (_commands[Type.Shutdown].Equals(adminCommand)) {
         Cmd_Shutdown();
      } else if (_commands[Type.Enemy].Equals(adminCommand)) {
         Cmd_SpawnEnemy();
      } else if (_commands[Type.SpawnEnemy].Equals(adminCommand)) {
         spawnCustomEnemy(parameters);
      } else if (_commands[Type.ServerInfo].Equals(adminCommand)) {
         Cmd_ServerInfo();
      } else if (_commands[Type.CreateShopShips].Equals(adminCommand)) {
         Cmd_CreateShopShips();
      } else if (_commands[Type.CreateShopItems].Equals(adminCommand)) {
         Cmd_CreateShopItems();
      } else if (_commands[Type.PlayerGo].Equals(adminCommand) || _commands[Type.GoToPlayer].Equals(adminCommand)) {
         requestPlayerGo(parameters);
      } else if (_commands[Type.SummonPlayer].Equals(adminCommand)) {
         requestSummonPlayer(parameters);
      } else if (_commands[Type.BotWaypoint].Equals(adminCommand)) {
         Cmd_BotWaypoint();
      } else if (_commands[Type.Warp].Equals(adminCommand)) {
         requestWarp(parameters);
      } else if (_commands[Type.Ability].Equals(adminCommand)) {
         requestAllAbilities(parameters);
      } else if (_commands[Type.GetItem].Equals(adminCommand)) {
         requestGetItem(parameters);
      } else if (_commands[Type.ShipSpeedUp].Equals(adminCommand)) {
         requestShipSpeedup();
      } else if (_commands[Type.GetAllItems].Equals(adminCommand)) {
         requestGetAllItems(parameters);
      } else if (_commands[Type.InteractAnvil].Equals(adminCommand)) {
         interactAnvil();
      } else if (_commands[Type.ScheduleServerRestart].Equals(adminCommand)) {
         requestScheduleServerRestart(parameters);
      } else if (_commands[Type.CancelServerRestart].Equals(adminCommand)) {
         Cmd_CancelServerRestart();
      } else if (_commands[Type.FreeWeapon].Equals(adminCommand)) {
         freeWeapon(parameters);
      } else if (_commands[Type.GetAllHats].Equals(adminCommand)) {
         requestGetAllHats(parameters);
      } else if (_commands[Type.TeachAbility].Equals(adminCommand)) {
         requestGiveAbility(parameters);
      } else if (_commands[Type.MutePlayer].Equals(adminCommand)) {
         requestMutePlayer(parameters);
      } else if (_commands[Type.ToggleInvisibility].Equals(adminCommand)) {
         requestInvisibility();
      } else if (_commands[Type.SpawnPirateShip].Equals(adminCommand)) {
         spawnPirateShip();
      } else if (_commands[Type.SpawnPrivateerShip].Equals(adminCommand)) {
         spawnPrivateerShip();
      } else if (_commands[Type.CreateVoyageMap].Equals(adminCommand)) {
         createVoyageInstance(parameters);
      } else if (_commands[Type.ShipDamage].Equals(adminCommand)) {
         Cmd_SetShipDamage(parameters);
      } else if (_commands[Type.ShipHealth].Equals(adminCommand)) {
         Cmd_SetShipHealth(parameters);
      }
   }

   [Command]
   protected void Cmd_SetShipDamage (string parameters) {
      if (GetComponent<PlayerShipEntity>() != null) {
         string[] list = parameters.Split(' ');
         if (list.Length > 0) {
            int shipdamage = int.Parse(list[0]);
            GetComponent<PlayerShipEntity>().damage = shipdamage;
         } 
      }
   }

   [Command]
   protected void Cmd_SetShipHealth (string parameters) {
      if (GetComponent<PlayerShipEntity>() != null) {
         string[] list = parameters.Split(' ');
         if (list.Length > 0) {
            int shipHealth = int.Parse(list[0]);
            GetComponent<PlayerShipEntity>().currentHealth = shipHealth;
         }
      }
   }

   private void requestInvisibility () {
      if (!_player.isAdmin()) {
         return;
      }

      _player.Cmd_ToggleAdminInvisibility(); 
   }

   private void requestMutePlayer (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');
      float seconds = 1;
      string username = "";

      try {
         username = list[0];
         seconds = float.Parse(list[1]);
      } catch (System.Exception e) {
         ChatManager.self.addChat("Invalid parameters. Correct use: /admin mute [player name] [seconds]", ChatInfo.Type.Error);
         return;
      }

      NetEntity entity = EntityManager.self.getEntityWithName(username);
      
      if (entity != null) {
         Global.player.rpc.Cmd_MutePlayer(entity.userId, seconds);
      }
   }

   private void interactAnvil () {
      NubisDataFetcher.self.fetchCraftableData(0, CraftingPanel.ROWS_PER_PAGE);
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
      StartCoroutine(ChatPanel.self.CO_MoveCaretToEnd());
   }

   public void tryAutoCompleteForGetItemCommand (string inputString) {
      if (!_player.isAdmin()) {
         return;
      }

      if (!inputString.StartsWith("/admin get_item ")) {
         return;
      }

      // Only auto complete if the user added exactly one character to the previous input
      if (inputString.Length - _lastAutoCompletedInput.Length != 1 ||
         !inputString.StartsWith(_lastAutoCompletedInput)) {

         // Prevent trying to auto complete this input again
         _lastAutoCompletedInput = inputString;
         return;
      }

      // Prevent trying to auto complete this input again
      _lastAutoCompletedInput = inputString;

      // Split the command
      string[] sections = inputString.Split(' ');

      // Check if the user is writing the item name
      if (sections.Length >= 4 && sections[3].Length > 0) {
         string categoryStr = sections[2];

         // Since there can be spaces in the item names, join the whole end of the input command to form it
         string inputItemName = sections[3];
         for (int i = 4; i < sections.Length; i++) {
            inputItemName = inputItemName + " " + sections[i];
         }

         // Get the correct item name dictionary
         Dictionary<string, int> dictionary;
         switch (categoryStr.Substring(0, 1).ToLower()) {
            case "w":
               dictionary = _weaponNames;
               break;
            case "a":
               dictionary = _armorNames;
               break;
            case "c":
               dictionary = _craftingIngredientNames;
               break;
            case "u":
               dictionary = _usableNames;
               break;
            case "b":
               dictionary = _blueprintNames;
               break;
            default:
               return;
         }

         // Find the first item name that starts with what the user wrote
         string foundItemName = null;
         foreach (string itemName in dictionary.Keys) {
            // Verify that this item name is longer than what the user wrote
            if (itemName.Length > inputItemName.Length) {

               // Cut the item name at the same length than what the user wrote
               string partialItemName = itemName.Substring(0, inputItemName.Length);

               // Compare case insensitive
               if (partialItemName.Equals(inputItemName, StringComparison.CurrentCultureIgnoreCase)) {
                  foundItemName = itemName;
                  break;
               }
            }
         }

         if (foundItemName != null) {
            // Remove the part of the item name that is already written
            string autoComplete = foundItemName.Substring(inputItemName.Length);

            // Add the auto complete to the input field text
            ChatPanel.self.inputField.SetTextWithoutNotify(ChatPanel.self.inputField.text + autoComplete);

            // Select the auto complete part
            ChatPanel.self.inputField.selectionAnchorPosition = ChatPanel.self.inputField.text.Length;
            ChatPanel.self.inputField.selectionFocusPosition = ChatPanel.self.inputField.text.Length - autoComplete.Length;
         }
      }
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

   private void requestGiveAbility (string parameters) {
      string[] list = parameters.Split(' ');
      string ability = "";
      string username = "";

      try {
         username = list[0];
         ability = parameters.Replace(username, "").Trim(' ');
      } catch (System.Exception e) {
         ChatManager.self.addChat($"Unable to give ability {ability} to player {username}. Correct use: /admin learn [username] [ability]. Exception: {e.Message}", ChatInfo.Type.Error);
         return;
      }

      Cmd_AddAbility(username, ability);
   }

   private void Cmd_AddAbility (string username, string ability) {
      if (!_player.isAdmin()) {
         ChatManager.self.addChat("AddAbility command requested from non-admin player.", ChatInfo.Type.Error);
         return;
      }

      BasicAbilityData abilityData = AbilityManager.self.allGameAbilities.FirstOrDefault(x => x.itemName.ToUpper() == ability.ToUpper());

      if (abilityData == null) {
         return;
      }

      BodyEntity targetBody = BodyManager.self.getBodyWithName(username);

      if (targetBody != null) {
         targetBody.rpc.giveAbilityToPlayer(targetBody.userId, new int[] { abilityData.itemID });
      }
   }

   protected void requestAllAbilities (string parameters) {
      List<BasicAbilityData> allAbilities = AbilityManager.self.allGameAbilities;
      Global.player.rpc.Cmd_UpdateAbilities(AbilitySQLData.TranslateBasicAbility(allAbilities).ToArray());
   }

   private void requestSummonPlayer (string parameters) {
      string[] list = parameters.Split(' ');
      string targetPlayerName = "";

      try {
         targetPlayerName = list[0];
      } catch (System.Exception e) {
         D.warning("Unable to parse from: " + parameters + ", exception: " + e);
         ChatManager.self.addChat("Not a valid player name", ChatInfo.Type.Error);
         return;
      }

      Cmd_SummonPlayer(targetPlayerName);
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
      // Send the request to the server
      Cmd_Warp(parameters);
   }

   protected void requestShipSpeedup () {
      // Unlocks ship speed boost x2
      _player.shipSpeedupFlag = true;
   }

   protected void requestGetItem (string parameters) {
      string[] sections = parameters.Split(' ');
      string categoryStr = "";
      string itemName = "";
      int count = 1;

      // Parse the parameters
      try {
         categoryStr = sections[0];

         // Determine how many sections form the item name (there can be spaces in the name)
         int lastNameSectionIndex = 1;

         // If the last section is an integer, it is the count
         if (sections.Length > 2 && int.TryParse(sections[sections.Length - 1], out count)) {
            lastNameSectionIndex = sections.Length - 2;
         } else {
            lastNameSectionIndex = sections.Length - 1;
            count = 1;
         }

         // Join the name sections in a single string
         itemName = sections[1];
         for (int i = 2; i <= lastNameSectionIndex; i++) {
            itemName = itemName + " " + sections[i];
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
            ChatManager.self.addChat("Not a valid item category", ChatInfo.Type.Error);
            D.error("Could not parse the category " + categoryStr);
            return;
      }

      // Search for the item name in lower case
      itemName = itemName.ToLower();

      // Get the item type
      int itemTypeId;
      switch (category) {
         case Item.Category.Weapon:
            if (_weaponNames.ContainsKey(itemName)) {
               itemTypeId = _weaponNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the weapon " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.Armor:
            if (_armorNames.ContainsKey(itemName)) {
               itemTypeId = _armorNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the armor " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.Usable:
            if (_usableNames.ContainsKey(itemName)) {
               itemTypeId = _usableNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the usable item " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.CraftingIngredients:
            if (_craftingIngredientNames.ContainsKey(itemName)) {
               itemTypeId = _craftingIngredientNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the crafting ingredient " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.Blueprint:
            if (_blueprintNames.ContainsKey(itemName)) {
               itemTypeId = _blueprintNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the blueprint " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         default:
            D.error("The category " + category.ToString() + " is not managed.");
            return;
      }

      // Send the request to the server
      Cmd_CreateItem(category, itemTypeId, count);
   }

   private void requestGetAllHats (string parameters) {
      Cmd_RequestAllHats();
   }

   [Command]
   private void Cmd_RequestAllHats () {
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin player.");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int hatCount = 0;

         // Create all the hats
         foreach (HatStatData hatData in EquipmentXMLManager.self.hatStatList) {
            if (hatData.hatType != 0) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Hats, hatData.hatType, 1)) {
                  hatCount++;
               }
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.log("You received " + hatCount + " hats.");
         });
      });
   }

   protected void requestGetAllItems (string parameters) {
      string[] list = parameters.Split(' ');
      int count = 100;

      // Parse the optional item count (for stacks)
      if (list.Length > 0 && !int.TryParse(list[0], out count)) {
         count = 100;
      }

      // Send the request to the server
      Cmd_CreateAllItems(count);
   }

   protected void freeWeapon (string parameters) {
      string[] sections = parameters.Split(' ');
      string itemName = "";
      const int count = 1;

      if (sections.Length == 0) {
         ChatManager.self.addChat("Please specify parameters - weapon_name, palette_names", ChatInfo.Type.Log);
         return;
      }
      itemName = sections[0];

      // Set the category
      Item.Category category = Item.Category.Weapon;

      // Search for the item name in lower case
      itemName = itemName.ToLower();

      // Get the item type
      int itemTypeId = -1;

      foreach (WeaponStatData weaponStat in EquipmentXMLManager.self.weaponStatList) {
         if (weaponStat.equipmentName.ToLower() == itemName) {
            itemTypeId = weaponStat.sqlId;
         }
      }

      List<string> paletteNames = new List<string>();
      for (int i = 1; i < sections.Length; i++) {
         paletteNames.Add(sections[i]);
      }

      if (itemTypeId == -1) {
         ChatManager.self.addChat("Could not find the weapon " + itemName, ChatInfo.Type.Error);
         return;
      }

      // Send the request to the server
      Cmd_CreateItemWithPalettes(category, itemTypeId, count, Item.parseItmPalette(paletteNames.ToArray()));
   }

   [Command]
   protected void Cmd_AddGunAbilities () {
      List<AbilitySQLData> newAbilityList = getAbilityDataSQL(new List<int> { 27, 78, 79, 80, 81 });
      string abilityIdStr = "";

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (AbilitySQLData ability in newAbilityList) {
            DB_Main.updateAbilitiesData(Global.player.userId, ability);
            abilityIdStr += ability.abilityID + " ";
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Added new abilities: " + _commands[Type.Ability], Color.green);
         });
      });
   }

   private List<AbilitySQLData> getAbilityDataSQL (List<int> abilityIds) {
      List<AbilitySQLData> newAbility = new List<AbilitySQLData>();
      foreach (int abilityId in abilityIds) {

         AttackAbilityData abilityData = AbilityManager.self.allAttackbilities.Find(_ => _.itemID == abilityId);
         newAbility.Add(new AbilitySQLData {
            abilityID = abilityData.itemID,
            abilityLevel = 1,
            abilityType = AbilityType.Standard,
            description = abilityData.itemDescription,
            name = abilityData.itemName,
            equipSlotIndex = -1
         });
      }
      return newAbility;
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
               AchievementManager.registerUserAchievement(_player, ActionType.EarnGold, gold);
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

   protected void requestScheduleServerRestart (string parameters) {
      string[] list = parameters.Split(' ');
      int buildVersion = 0; // 0 means 'latest'
      int delayMinutes = 5; // Default delay 5 minutes

      if (list.Length > 0) {
         bool delayMinutesIsValid = int.TryParse(list[0], out int parsedDelayMinutes);
         if (delayMinutesIsValid) {
            delayMinutes = parsedDelayMinutes;
         }
      }

      if (delayMinutes <= 0) {
         D.warning($"The requested delay is not valid.");
         ChatManager.self.addChat("Invalid Delay", ChatInfo.Type.Error);
         return;
      }

      if (list.Length > 1) {
         bool buildVersionIsValid = int.TryParse(list[1], out int parsedBuildVersion);
         if (buildVersionIsValid) {
            buildVersion = parsedBuildVersion;
         }
      }

      if (buildVersion < 0) {
         D.warning($"The requested build number is not valid.");
         ChatManager.self.addChat("Invalid Build Number", ChatInfo.Type.Error);
         return;
      }

      string buildVersionPrompt = buildVersion == 0 ? "latest" : buildVersion.ToString();
      string delayMinutesPrompt = delayMinutes.ToString();
      ChatManager.self.addChat($"Server Restart Command Sent! [delay = {delayMinutesPrompt} minutes, version = {buildVersionPrompt}]", ChatInfo.Type.Local);

      // Send the request to the server
      Cmd_ScheduleServerRestart(delayMinutes, buildVersion);
   }

   private void createVoyageInstance (string parameters) {
      string[] list = parameters.Split(' ');

      // Default parameter values
      Voyage.Difficulty difficulty = Voyage.Difficulty.None;
      Biome.Type biome = Biome.Type.None;
      bool isPvP = UnityEngine.Random.value > 0.5f;
      string areaKey = "";

      // Parse the parameters
      if (list.Length > 0 && !(list.Length == 1 && string.IsNullOrEmpty(list[0]))) {
         try {
            isPvP = int.Parse(list[0]) > 0;
         } catch {
            ChatManager.self.addChat("Invalid PvP parameter for command create_voyage: " + list[0], ChatInfo.Type.Error);
            return;
         }
            
         if (list.Length > 1) {
            if (!(Enum.TryParse(list[1], out difficulty) && Enum.IsDefined(typeof(Voyage.Difficulty), difficulty))) {
               ChatManager.self.addChat("Invalid difficulty parameter for command create_voyage: " + list[1], ChatInfo.Type.Error);
               return;
            }

            if (list.Length > 2) {
               if (!(Enum.TryParse(list[2], out biome) && Enum.IsDefined(typeof(Biome.Type), biome))) {
                  ChatManager.self.addChat("Invalid biome parameter for command create_voyage: " + list[2], ChatInfo.Type.Error);
                  return;
               }

               if (list.Length > 3) {
                  areaKey = "";
                  for (int i = 3; i < list.Length; i++) {
                     areaKey += list[i] + ' ';
                  }

                  // Deletes the last space
                  areaKey = areaKey.Substring(0, areaKey.Length - 1);
               }
            }
         }
      }

      Cmd_CreateVoyageInstance(isPvP, difficulty, biome, areaKey);
   }

   [Command]
   private void Cmd_CreateVoyageInstance (bool isPvP, Voyage.Difficulty difficulty, Biome.Type biome, string areaKey) {
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin");
         return;
      }

      if (!string.IsNullOrEmpty(areaKey)) {
         // Get the list of voyage sea maps
         List<string> voyageSeaMaps = VoyageManager.self.getVoyageAreaKeys();

         // Get the valid area key closest to the given key
         areaKey = getClosestAreaKey(voyageSeaMaps, areaKey);
      }

      VoyageManager.self.requestVoyageInstanceCreation(areaKey, isPvP, biome, difficulty);
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
         if (shipType == Ship.Type.None) {
            continue;
         }
         Rarity.Type rarity = Rarity.Type.Uncommon;
         
         // Set up the Ship Info
         ShipInfo ship = Ship.generateNewShip(shipType, rarity);
         ship.userId = _player.userId;

         // Create the ship in the database
         DB_Main.createShipFromShipyard(_player.userId, ship);
      }
   }

   [Command]
   private void Cmd_SummonPlayer (string targetPlayerName) {
      if (!_player.isAdmin()) {
         return;
      }

      NetEntity targetEntity = EntityManager.self.getEntityWithName(targetPlayerName);

      if (targetEntity == null) {
         return;
      }

      if (_player.areaKey != targetEntity.areaKey || _player.instanceId != targetEntity.instanceId) {
         targetEntity.spawnInNewMap(_player.areaKey, _player.transform.localPosition, Direction.South);
      } else {
         targetEntity.moveToPosition(_player.transform.position);
      }
   }

   [Command]
   protected void Cmd_PlayerGo (string targetPlayerName) {
      if (!_player.isAdmin()) {
         return;
      }

      NetEntity targetEntity = EntityManager.self.getEntityWithName(targetPlayerName);

      if (targetEntity == null) {
         return;
      }

      if (_player.areaKey != targetEntity.areaKey || _player.instanceId != targetEntity.instanceId) {
         _player.spawnInNewMap(targetEntity.areaKey, targetEntity.transform.localPosition, Direction.South);
      } else {
         _player.moveToPosition(targetEntity.transform.position);
      }      
   }

   [Command]
   protected void Cmd_BotWaypoint () {

   }

   [Command]
   protected void Cmd_ScheduleServerRestart (int delay, int build) {
      if (!_player.isAdmin()) {
         return;
      }

      DateTime timePoint = DateTime.UtcNow + TimeSpan.FromMinutes(delay);
      long ticks = timePoint.Ticks;

      // To the database thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         bool scheduled = DB_Main.updateDeploySchedule(ticks, build);
         if (scheduled) {
            string timeUnit = (delay == 1) ? "minute" : "minutes";
            string message = $"Server will reboot in {delay} {timeUnit}!";
            ChatInfo.Type chatType = ChatInfo.Type.Global;
            ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.UtcNow, chatType);
            ServerNetworkingManager.self?.sendGlobalChatMessage(chatInfo);
         }
      });
   }

   [Command]
   protected void Cmd_CancelServerRestart () {
      if (!_player.isAdmin()) {
         return;
      }

      // request to schedule a restart. - use ButlerClient
      //ButlerClient.CancelServerRestart((success)=>{
      //});

      DB_Main.cancelDeploySchedule();

   }

   [Command]
   protected void Cmd_Warp (string partialAreaKey) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      // Get all the area keys
      List<string> areaKeys = AreaManager.self.getAreaKeys();

      string closestAreaKey;

      // For owned maps, the base map key
      string baseMapAreaKey = null;

      // If no partialAreaKey passed as parameter, then choosing random area to warp
      if (string.IsNullOrEmpty(partialAreaKey)) {
         // string[] testAreaKeys = { "Pineward_Shipyard", "Far Sands", "Snow Weapon Shop 1", "Andriusti", "Starting Sea Map", "Starting Treasure Site", "Andrius Ledge" };
         string[] testAreaKeys = { "Snow Town Lite", "Far Sands", "Starting Treasure Site" };
         closestAreaKey = testAreaKeys[UnityEngine.Random.Range(0, testAreaKeys.Count())];
      } else {
         // Try to find a custom map of this key
         if (AreaManager.self.tryGetCustomMapManager(partialAreaKey, out CustomMapManager customMapManager)) {
            // Check if user is allowed to access this map
            if (!customMapManager.canUserWarpInto(_player, partialAreaKey, out Action<NetEntity> denyHandler)) {
               denyHandler?.Invoke(_player);
               return;
            }

            int userId = CustomMapManager.isUserSpecificAreaKey(partialAreaKey) ? CustomMapManager.getUserId(partialAreaKey) : _player.userId;
            NetEntity entity = EntityManager.self.getEntity(userId);

            // Owner of the target map has to be in the server
            // TODO: remove this constraint
            if (entity == null) {
               D.log("Owner of the map is not currently in the server.");
               return;
            }

            baseMapAreaKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(entity));
            closestAreaKey = partialAreaKey;
         } else {
            // Get the area key closest to the given partial key
            closestAreaKey = getClosestAreaKey(areaKeys, partialAreaKey);
         }
      }

      // Get the default spawn for the destination area
      // TODO: better checking for if destination exists
      Vector2 spawnLocalPos = SpawnManager.self.getDefaultLocalPosition(baseMapAreaKey ?? closestAreaKey);

      if (spawnLocalPos == Vector2.zero) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "Could not determine the warp destination. Area Name: " + closestAreaKey);
         return;
      }

      if (Util.isAutoTesting()) {
         D.debug("Player {" + _player.userId + "} is warping to: {" + closestAreaKey + "}");
      }

      _player.spawnInNewMap(closestAreaKey);
   }

   private string getClosestAreaKey (List<string> areaKeys, string partialAreaKey) {
      // Try to select area keys whose beginning match exactly with the user input
      List<string> exactMatchKeys = areaKeys.Where(s => s.StartsWith(partialAreaKey, StringComparison.CurrentCultureIgnoreCase)).ToList();
      if (exactMatchKeys.Count > 0) {
         // If there are matchs, use that sub-list instead
         areaKeys = exactMatchKeys;
      }

      // Get the area key closest to the given partial key
      return areaKeys.OrderBy(s => Util.compare(s, partialAreaKey)).First();
   }

   protected void spawnPirateShip () {
      BotShipGenerator.spawnPirate(_player);
   }

   protected void spawnPrivateerShip () {
      BotShipGenerator.spawnPrivateer(_player);
   }

   private void spawnCustomEnemy (string parameters) {
      parameters = parameters.Trim(' ').Replace(" ", "_");
      if (Enum.TryParse(parameters, out Enemy.Type enemyType)) {
         Cmd_SpawnCustomEnemy(enemyType);
      }
   }

   [Command]
   private void Cmd_SpawnCustomEnemy (Enemy.Type enemyType) {
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin");
         return;
      }

      EnemyManager.self.spawnEnemyAtLocation(enemyType, _player.getInstance(), _player.transform.localPosition);
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
      NetworkServer.Spawn(enemy.gameObject);
   }

   protected void warpRandomly () {
      handleAdminCommandString("warp");
   }

   public void warpToPosition (Vector3 localPosition) {
      // Set position locally
      _player.transform.localPosition = localPosition;

      // Set it in the server
      Cmd_WarpToPosition(localPosition);
   }

   [Command]
   public void Cmd_WarpToPosition (Vector3 localPosition) {
      // localPosition is the local position inside the map the player is currently in

      // Check that requester is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      // Check that player is currently in an area and it exists
      Area area = _player.areaKey == null ? null : AreaManager.self.getArea(_player.areaKey);
      if (area != null) {
         _player.transform.localPosition = localPosition;
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
         Item item = new Item(-1, category, itemTypeId, count, "", "");

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
   protected void Cmd_CreateItemWithPalettes (Item.Category category, int itemTypeId, int count, string paletteNames) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      // Go to the background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Create the item object
         Item item = new Item(-1, category, itemTypeId, count, paletteNames, "");

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
      int hatCount = 0;
      int usableCount = 0;
      int ingredientsCount = 0;
      int blueprintCount = 0;
      int itemDefinitionCount = 0;

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

         // Get all the crafting data
         foreach (CraftableItemRequirements itemRequirements in CraftingManager.self.getAllCraftableData()) {

            // Get the result item
            Item resultItem = itemRequirements.resultItem;

            // Determine if the result item is a weapon or an armor
            if (resultItem.category == Item.Category.Weapon) {
               // Create the item
               if (createItemIfNotExistOrReplenishStack(Item.Category.Blueprint, resultItem.itemTypeId, count)) {
                  blueprintCount++;
               }
            } else if (resultItem.category == Item.Category.Armor) {
               // Create the item
               if (createItemIfNotExistOrReplenishStack(Item.Category.Blueprint, resultItem.itemTypeId, count)) {
                  blueprintCount++;
               }
            }
         }

         // Create all item definitions
         foreach (ItemDefinition itemDefinition in ItemDefinitionManager.self.getDefinitions()) {
            ItemInstance itemInstance = new ItemInstance(itemDefinition.id, _player.userId, count);
            if (createItemIfNotExistOrReplenishStack(itemInstance)) {
               itemDefinitionCount++;
            }
         }

         // Create all the armors
         foreach (ArmorStatData armorData in EquipmentXMLManager.self.armorStatList) {
            if (armorData.armorType != 0) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Armor, armorData.armorType, 1)) {
                  armorCount++;
               }
            }
         }

         // Create all the weapons
         foreach (WeaponStatData weaponData in EquipmentXMLManager.self.weaponStatList) {
            if (weaponData.weaponType != 0) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Weapon, weaponData.weaponType, 1)) {
                  weaponsCount++;
               }
            }
         }

         // Create all the hats
         foreach (HatStatData hatData in EquipmentXMLManager.self.hatStatData) {
            if (hatData.hatType != 0) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Hats, hatData.hatType, 1)) {
                  hatCount++;
               }
            }
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send confirmation back to the player who issued the command
            string message = string.Format("Added {0} weapons, {1} armors, {2} usable items, {3} ingredients, {4} hats, {5} blueprints and {6} item definitions to the inventory.",
               weaponsCount, armorCount, usableCount, ingredientsCount, hatCount, blueprintCount, itemDefinitionCount);
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
         Item baseItem = new Item(-1, category, itemTypeId, count, "", "").getCastItem();
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

   [Server]
   private bool createItemIfNotExistOrReplenishStack (ItemInstance itemInstance) {
      bool wasItemCreated = false;

      // Retrieve the item from the user inventory, if it exists
      ItemInstance existingItem = DB_Main.exec((cmd) => DB_Main.getItemInstance(cmd, itemInstance.ownerUserId, itemInstance.itemDefinitionId));

      if (existingItem == null) {
         // If the item does not exist, create a new one
         DB_Main.exec((cmd) => DB_Main.createOrAppendItemInstance(cmd, itemInstance));
         wasItemCreated = true;
      } else {
         // If there are less items than what is requested, replenish the stack
         if (existingItem.count < itemInstance.count) {
            DB_Main.exec((cmd) => DB_Main.increaseItemInstanceCount(cmd, existingItem.id, itemInstance.count - existingItem.count));
            wasItemCreated = true;
         }
      }

      return wasItemCreated;
   }

   public void buildItemNamesDictionary () {
      // Clear all the dictionaries
      _weaponNames.Clear();
      _armorNames.Clear();
      _usableNames.Clear();
      _craftingIngredientNames.Clear();

      // Set all the weapon names
      foreach (WeaponStatData weaponData in EquipmentXMLManager.self.weaponStatList) {
         addToItemNameDictionary(_weaponNames, Item.Category.Weapon, weaponData.sqlId);
      }

      // Set all the armor names
      foreach (ArmorStatData armorData in EquipmentXMLManager.self.armorStatList) {
         addToItemNameDictionary(_armorNames, Item.Category.Armor, armorData.sqlId);
      }

      // Set all the usable items names
      foreach (UsableItem.Type usableType in Enum.GetValues(typeof(UsableItem.Type))) {
         addToItemNameDictionary(_usableNames, Item.Category.Usable, (int) usableType);
      }

      // Set all the crafting ingredients names
      foreach (CraftingIngredients.Type craftingIngredientsType in Enum.GetValues(typeof(CraftingIngredients.Type))) {
         addToItemNameDictionary(_craftingIngredientNames, Item.Category.CraftingIngredients, (int) craftingIngredientsType);
      }
   }

   private void addToItemNameDictionary (Dictionary<string, int> dictionary, Item.Category category, int itemTypeId) {
      // Create a base item
      Item baseItem = new Item(-1, category, itemTypeId, 1, "", "");
      baseItem = baseItem.getCastItem();

      // Get the item name in lower case
      string itemName = baseItem.getName().ToLower();

      // Add the new entry in the dictionary
      if (!"undefined".Equals(itemName) && !"usable item".Equals(itemName) && !"undefined design".Equals(itemName) && !itemName.ToLower().Contains("none") && itemName != "") {
         if (!dictionary.ContainsKey(itemName)) {
            dictionary.Add(itemName, itemTypeId);
         } else {
            D.warning(string.Format("The {0} item name ({1}) is duplicated.", category.ToString(), itemName));
         }
      }
   }

   #region Private Variables

   // A list of the available admin commands
   protected Dictionary<Type, string> _commands = new Dictionary<Type, string>();

   // A history of commands we've requested
   protected List<string> _history = new List<string>();

   // The index of our chat history we're currently using in the input field, if any
   protected int _historyIndex = -1;

   // Our associated Player object
   protected NetEntity _player;

   // The dictionary of item ids accessible by in-game item name
   protected Dictionary<string, int> _weaponNames = new Dictionary<string, int>();
   protected Dictionary<string, int> _armorNames = new Dictionary<string, int>();
   protected Dictionary<string, int> _usableNames = new Dictionary<string, int>();
   protected Dictionary<string, int> _craftingIngredientNames = new Dictionary<string, int>();

   // The dictionary of blueprint names
   protected static Dictionary<string, int> _blueprintNames = new Dictionary<string, int>();

   // The last chat input that went through the auto complete process
   private string _lastAutoCompletedInput = "";

   #endregion
}