using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using System.IO;
using Mirror.Profiler;
using NubisDataHandling;
using MLAPI.Messaging;
using UnityEngine.InputSystem;
using System.Text;

public class AdminManager : NetworkBehaviour
{
   #region Public Variables

   // The email address we use for test accounts
   public static string TEST_EMAIL_DOMAIN = "codecommode.com";

   // The key for where we store the message of the day value in PlayerPrefs
   public static readonly string MOTD_KEY = "messageOfTheDay";

   #endregion

   void Start () {
      _player = GetComponent<NetEntity>();

      if (isLocalPlayer) {
         // If we're testing clients, have them warp around repeatedly
         if (Util.isAutoWarping()) {
            if (CommandCodes.get(CommandCodes.Type.AUTO_TEST)) {
               int testerNumber = Util.getCommandLineInt(CommandCodes.Type.AUTO_TEST + "");

               System.Random rand = new System.Random(testerNumber);

               int randTime = rand.Next(8, 13);

               InvokeRepeating(nameof(warpRandomly), randTime, randTime);
            }
         }

         StartCoroutine(CO_AddCommands());
      }
   }

   private IEnumerator CO_AddCommands () {
      // Wait until we receive data
      while (Util.isEmpty(_player.entityName)) {
         yield return null;
      }

      addCommands();
   }

   private void addCommands () {
      ChatManager cm = ChatManager.self;

      if (!isLocalPlayer || !_player.isAdmin()) {
         return;
      }

      ChatManager.self.removeAdminCommands();

      cm.addCommand(new CommandData("set_admin", "Sets admin privileges for the user", requestSetAdminPrivileges, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "username", "adminFlag" }));
      cm.addCommand(new CommandData("add_gold", "Gives an amount of gold to the user", requestAddGold, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "username", "goldAmount" }));
      cm.addCommand(new CommandData("add_ships", "Gives all ships to the user", requestAddShips, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("check_fps", "Checks the user's FPS (Not implemented yet)", requestCheckFPS, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("shutdown", "Shuts down the server (Not implemented yet)", requestShutdown, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("enemy", "Spawns a lizard enemy", requestSpawnEnemy, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("spawn", "Spawns a custom enemy type", spawnCustomEnemy, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "enemyType" }));
      cm.addCommand(new CommandData("server_info", "Displays the current server info", requestServerInfo, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("create_shop_ships", "Creates shop ships (Not implemented yet)", requestCreateShopShips, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("create_shop_items", "Creates shop items (Not implemented yet)", requestCreateShopItems, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("pgo", "Warps you to a user", requestPlayerGo, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "username" }));
      cm.addCommand(new CommandData("goto", "Warps you to a user", requestPlayerGo, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "username" }));
      cm.addCommand(new CommandData("summon", "Summons a user to you", requestSummonPlayer, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "username" }));
      cm.addCommand(new CommandData("bot_waypoint", "Sets a bot waypoint (Not implemented yet)", requestBotWaypoint, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("all_abilities", "Gives you access to all abilities", requestAllAbilities, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("get_item", "Gives you an amount of a specified item", requestGetItem, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "category", "itemName", "quantity" }));
      cm.addCommand(new CommandData("ship_speedup", "Speeds up your ship", requestShipSpeedup, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("get_all_items", "Gives you all items in the game", requestGetAllItems, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("interact_anvil", "Allows you to use the anvil", interactAnvil, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("restart", "Schedules a server restart. (Build version 0 is latest)", requestScheduleServerRestart, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "delayInMinutes", "buildVersion" }));
      cm.addCommand(new CommandData("cancel", "Cancels a scheduled server restart", requestCancelServerRestart, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("free_weapon", "Gives you a weapon with a specified palette", freeWeapon, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "weaponName", "paletteName" }));
      cm.addCommand(new CommandData("get_all_hats", "Gives you all hats in the game", requestGetAllHats, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("learn", "Teaches your player a new ability", requestGiveAbility, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "username", "abilityName" }));
      cm.addCommand(new CommandData("mute", "Mutes a player for [duration] minutes", requestMutePlayer, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "playerName", "duration", "reason" }));
      cm.addCommand(new CommandData("stealth_mute", "Mutes a player for [duration] minutes without notifying", requestStealthMutePlayer, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "playerName", "duration", "reason" }));
      cm.addCommand(new CommandData("invisible", "Turns your player invisible", requestInvisibility, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("create_voyage", "Creates a new voyage map", createVoyageInstance, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "isPvp", "difficulty", "biome", "areaKey" }));
      cm.addCommand(new CommandData("ship_damage", "Sets your ship's damage", requestSetShipDamage, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "damage" }));
      cm.addCommand(new CommandData("ship_health", "Sets your ship's health", requestSetShipHealth, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "health" }));
      cm.addCommand(new CommandData("spawn_sea_enemy", "Spawns a sea enemy at your mouse position", requestSpawnSeaEnemy, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "enemyId" }));
      cm.addCommand(new CommandData("warp_anywhere", "Allows user warp anywhere without getting returned to town", requestWarpAnywhere, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("kick", "Disconnects a player from the game", kickPlayer, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "playerName", "reason" }));
      cm.addCommand(new CommandData("ban", "Ban a player from the game for [duration] minutes", banPlayerTemporary, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "playerName", "duration", "reason" }));
      cm.addCommand(new CommandData("ban_permanently", "Bans a player from the game indefinitely", banPlayerIndefinite, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "playerName", "reason" }));
      cm.addCommand(new CommandData("god", "Gives the player's ship very high health and damage", requestGod, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("ore_voyage", "Enables the players and ores within the area of the player to have valid voyage id", requestOre, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "voyage" }));
      cm.addCommand(new CommandData("/motd", "Displays the message of the day", requestGetMotd));
      cm.addCommand(new CommandData("set_motd", "Sets the message of the day", requestSetMotd, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "message" }));
      cm.addCommand(new CommandData("lose", "Kills player in a land battle", requestLose, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("win", "Kills all of the enemies in a land battle", requestWin, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("difficulty", "Enables the players to alter difficulty of current instance", requestDifficulty, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "difficultyLevel" }));
      cm.addCommand(new CommandData("throw_errors", "Throws various errors and warnings on the server to test the logger tool", requestThrowTestErrors, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "message" }));
      cm.addCommand(new CommandData("toggle_nubis", "Enable or disable the usage of the Nubis server for this client", toggleNubis, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "isEnabled" }));
      cm.addCommand(new CommandData("add_perk_points", "Gives the player perk points", requestPerkPoints, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "numPoints" }));
      cm.addCommand(new CommandData("add_powerup", "Gives you a powerup of specified type and rarity", requestPowerup, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "powerupType", "powerupRarity" }));
      cm.addCommand(new CommandData("clear_powerups", "Clears all of your current powerups", requestClearPowerups, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("unlock_world_map", "Unlocks all the biomes and town warps in the world map", unlockWorldMap, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("pvp_join", "Warps the player to the current pvp game", joinPvp, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("pvp_force_start", "Forces the pvp game the player is in to start, regardless of how many players are in it", forceStartPvp, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("pvp_set_map", "Changes which map pvp games will be created in", setPvpMap, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("warp", "Warps you to an area", requestWarp, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "areaName" }, parameterAutocompletes: AreaManager.self.getAllAreaNames()));
      cm.addCommand(new CommandData("show_admin_panel", "Show the Admin Panel", showAdminPanel, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("reset_shop", "Refreshes all the shops", resetShops, requiredPrefix: CommandType.Admin));

      // Used for combat simulation
      cm.addCommand(new CommandData("auto_attack", "During land combat, attacks automatically", autoAttack, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "attackDelay" }));
      cm.addCommand(new CommandData("force_join", "During land combat, forces group memebers to join automatically", forceJoin, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "autoAttack", "attackDelay" }));
      cm.addCommand(new CommandData("battle_simulate", "Simulate the combat", battleSimulate, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "rounds", "enemy" }));
      cm.addCommand(new CommandData("freeze_ships", "Freezes all bot ships", freezeShips, requiredPrefix: CommandType.Admin));

      // Log Commands for investigation
      cm.addCommand(new CommandData("xml", "Logs the xml content of the specific manager", requestXmlLogs, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "xmlType" }));
      cm.addCommand(new CommandData("screen_log", "Allows screen to log files using D.debug()", requestScreenLogs, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("log", "Enables isolated debug loggers", requestLogs, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "logType", "isTrue" }));
      cm.addCommand(new CommandData("network_profile", "Saves last 60 seconds of network profiling data", networkProfile, requiredPrefix: CommandType.Admin));
      cm.addCommand(new CommandData("temp_password", "Temporary access any account using temporary password", overridePassword, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "accountName", "tempPassword" }));
      cm.addCommand(new CommandData("db_test", "Runs a given number of queries per seconds and returns execution time statistics", requestDBTest, requiredPrefix: CommandType.Admin, parameterNames: new List<string>() { "queriesPerSecond" }));

      /*    NOT IMPLEMENTED
      _commands[Type.CreateTestUsers] = "create_test_users";
      _commands[Type.DeleteTestUsers] = "delete_test_users";
      _commands[Type.SetWeather] = "set_weather";
      _commands[Type.SetWind] = "set_wind";
      _commands[Type.CreateTestInstances] = "create_test_instances";
      _commands[Type.SpawnShips] = "spawn_ships";
      _commands[Type.PolynavTest] = "polynav_test";
      _commands[Type.PortGo] = "portgo";
      _commands[Type.SiteGo] = "sitego";
      _commands[Type.NPC] = "test_npc";
      _commands[Type.GunAbilities] = "gun_abilities";

      */

      ChatManager.self.addAdminCommand(this);

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
      if (_player.isLocalPlayer && _player.isAdmin() && (KeyUtils.GetKey(Key.LeftCtrl) || KeyUtils.GetKey(Key.RightCtrl)) && KeyUtils.GetButtonDown(MouseButton.Left)) {
         Area area = _player.areaKey == null ? null : AreaManager.self.getArea(_player.areaKey);
         if (!_player.isInBattle() && area != null) {
            Vector2 wPos = CameraManager.defaultCamera.getCamera().ScreenToWorldPoint(MouseUtils.mousePosition);
            warpToPosition(area.transform.InverseTransformPoint(wPos));
         }
      }
   }

   public bool checkAdminCommand (string prefix, string parameter) {
      // Note the command we're about to request
      D.debug("Requesting admin command: " + prefix + " with parameters: " + parameter);

      _history.Add(prefix + parameter);

      if (!_player.isAdmin()) {
         string msg = string.Format("<color=red>{0}</color>!", "This account does not have Admin privileges.");
         ChatManager.self.addChat(msg, ChatInfo.Type.System);
         return false;
      }

      return true;
   }

   private void setPvpMap (string mapName) {
      Cmd_SetPvpMap(mapName);
   }

   [Command]
   private void Cmd_SetPvpMap (string mapName) {
      PvpManager.currentMap = mapName;
   }

   private void forceStartPvp () {
      Cmd_ForceStartPvp();
   }

   [Command]
   private void Cmd_ForceStartPvp () {
      PvpGame game = PvpManager.self.getGameWithPlayer(_player);
      if (game != null) {
         game.forceStart();
      }
   }

   private void joinPvp () {
      Cmd_JoinPvp();
   }

   [Command]
   private void Cmd_JoinPvp () {
      PvpManager.self.joinBestPvpGameOrCreateNew(_player);
   }

   private void requestPerkPoints (string parameters) {
      Cmd_AddPerkPoints(parameters);
   }

   [Command]
   private void Cmd_AddPerkPoints (string parameters) {
      string[] inputs = parameters.Split(' ');
      if (inputs.Length == 0) {
         return;
      }

      int points = int.Parse(inputs[0]);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.addPerkPointsForUser(_player.userId, new List<Perk> { new Perk(0, points) });

         List<Perk> userPerks = DB_Main.getPerkPointsForUser(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_SetPerkPoints(connectionToClient, userPerks.ToArray());
         });
      });
   }

   private void requestClearPowerups () {
      PowerupPanel.self.clearPowerups();
      Cmd_ClearPowerups();
   }

   [Command]
   private void Cmd_ClearPowerups () {
      PowerupManager.self.clearPowerupsForUser(_player.userId);
   }

   private void requestPowerup (string parameters) {
      Cmd_AddPowerup(parameters);
   }

   [Command]
   private void Cmd_AddPowerup (string parameters) {
      string[] inputs = parameters.Split(' ');

      if (inputs.Length > 1) {
         Powerup.Type type = (Powerup.Type) int.Parse(inputs[0]);
         Rarity.Type rarity = (Rarity.Type) int.Parse(inputs[1]);

         if (type == Powerup.Type.None || rarity == Rarity.Type.None) {
            return;
         }

         Powerup newPowerup = new Powerup(type, rarity);
         PowerupManager.self.addPowerupServer(_player.userId, newPowerup);
         _player.rpc.Target_AddPowerup(connectionToClient, newPowerup);
      }
   }

   private void requestLose (string parameter) {
      Cmd_Lose(parameter);
   }

   [Command]
   protected void Cmd_Lose (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }
      Battle battle = BattleManager.self.getBattleForUser(_player.userId);
      if (battle == null) {
         return;
      }

      List<Battler> participants = battle.getParticipants();

      foreach (Battler battler in participants) {
         if ((battler != null) && battler.userId == _player.userId) {
            battler.health = 0;
         }
      }
   }

   private void requestWin (string parameter) {
      Cmd_Win(parameter);
   }

   [Command]
   protected void Cmd_Win (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      // Land battles
      Battle battle = BattleManager.self.getBattleForUser(_player.userId);
      if (battle != null) {

         List<Battler> participants = battle.getParticipants();

         foreach (Battler battler in participants) {
            if ((battler != null) && battler.isMonster()) {
               battler.health = 0;
            }
         }

      }

      // Sea battles
      if (_player.isPlayerShip()) {
         Instance playerInstance = InstanceManager.self.getInstance(_player.instanceId);
         int enemiesCounter = 0;
         int killedEnemiesCounter = 0;
         foreach (NetworkBehaviour entity in playerInstance.entities.ToArray()) {
            SeaEntity targetEntity = entity.transform.GetComponent<SeaEntity>();

            if (targetEntity == null) {
               targetEntity = entity.transform.GetComponentInParent<SeaEntity>();
            }

            // Ensure that the target entity is valid and
            // Ensure the target entity belongs to the same instance of the player and
            // Ensure the target entity is not the player
            if (targetEntity == null || targetEntity.instanceId != _player.instanceId || targetEntity.netId == _player.netId) {
               continue;
            }

            // Prevent players from being damaged by other players if they have not entered PvP yet
            if (_player != null && _player.isPlayerShip() && !targetEntity.canBeAttackedByPlayers()) {
               continue;
            }

            // Do not eliminate other members in the voyage
            if ((_player.voyageGroupId > 0 || targetEntity.voyageGroupId > 0) && (_player.voyageGroupId == targetEntity.voyageGroupId)) {
               continue;
            }

            // Skip dead entities
            if (targetEntity.isDead()) {
               continue;
            }

            if (!_player.isEnemyOf(targetEntity)) {
               continue;
            }

            // Make sure that this is called on the server
            if (NetworkServer.active) {

               int finalDamage = targetEntity.applyDamage(int.MaxValue, _player.netId);

               // Apply the status effect
               StatusManager.self.create(Status.Type.Slowed, 1.0f, 3f, targetEntity.netId);

               if (targetEntity.isDead()) {
                  killedEnemiesCounter++;
               }

            }

            enemiesCounter++;
         }

         string message = $"No sea enemy detected!";
         if (enemiesCounter > 0) {
            message = $"{killedEnemiesCounter} out of {enemiesCounter} sea enemies eliminated!";
         }
         _player.rpc.Target_ReceiveNoticeFromServer(_player.connectionToClient, message);
      }

   }

   private void kickPlayer (string parameters) {
      Cmd_KickPlayer(parameters);
   }

   private void requestGetMotd () {
      Cmd_GetMotd();
   }

   [Command]
   private void Cmd_GetMotd () {
      Target_GetMotd(connectionToClient, PlayerPrefs.GetString(MOTD_KEY, ""));
   }

   [TargetRpc]
   private void Target_GetMotd (NetworkConnection connection, string message) {
      ChatPanel.self.addChatInfo(new ChatInfo(0, message, System.DateTime.Now, ChatInfo.Type.System));
   }

   private void requestSetMotd (string parameters) {
      Cmd_SetMotd(parameters);
   }

   [Command]
   private void Cmd_SetMotd (string parameters) {
      PlayerPrefs.SetString(MOTD_KEY, parameters);
   }

   private void requestLogs (string parameters) {
      Cmd_SetServerLog(parameters);
   }

   [Command]
   protected void Cmd_SetServerLog (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');

      if (list.Length > 1) {
         string logType = "";
         string isEnabledString = "false";
         try {
            logType = list[0].ToLower();
            isEnabledString = list[1].ToLower();
         } catch {
            _player.Target_FloatingMessage(_player.connectionToClient, "Invalid Parameters" + " : " + parameters);
         }
         bool isEnabled = isEnabledString == "false" ? false : true;

         // Handle server logs
         processLogAuthorization(logType, isEnabled, parameters);
         D.debug("Server will Log Display: {" + logType + "} as {" + isEnabledString + "}");

         // Handle client logs
         Target_ReceiveLogAuthorization(_player.connectionToClient, logType, isEnabled, parameters);
      } else {
         _player.Target_FloatingMessage(_player.connectionToClient, "Invalid Parameters" + " : " + parameters);
      }
   }

   private void networkProfile (string parameters) {
      Cmd_NetworkProfile(parameters);
   }

   private void overridePassword (string parameters) {
      Cmd_OverridePassword(parameters);
   }

   [Command]
   protected void Cmd_OverridePassword (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');

      if (list.Length >= 1) {
         string accountName = list[0];
         string tempPassword = "test";
         if (list[1].Length > 1) {
            tempPassword = list[1];
         }

         // Add account override to the master server
         NetworkedServer masterServer = ServerNetworkingManager.self.getServer(Global.MASTER_SERVER_PORT);
         if (!masterServer.accountOverrides.ContainsKey(accountName)) {
            masterServer.accountOverrides.Add(accountName, tempPassword);
         }

         // Send message notification to server and client admin
         string message = "New password was set for account {" + accountName + "} " + tempPassword;
         D.debug(message);
         _player.rpc.Target_ReceiveNoticeFromServer(_player.connectionToClient, message);
      } else {
         D.debug("Invalid admin commant, needs additional parameters");
      }
   }

   [Command]
   protected void Cmd_NetworkProfile (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string dumpsDirPath = Application.persistentDataPath + "/networkprofiler/";
      if (!Directory.Exists(dumpsDirPath)) {
         Directory.CreateDirectory(dumpsDirPath);
      }
      string dumpFile = dumpsDirPath + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss") + "_dump.netdata";

      AdminManagerSingleton.self.networkProfiler.IsRecording = false;
      AdminManagerSingleton.self.networkProfiler.Save(dumpFile);
      AdminManagerSingleton.self.networkProfiler.IsRecording = true;
   }

   private void requestDifficulty (string parameters) {
      Cmd_SetDifficulty(parameters);
   }

   [Command]
   protected void Cmd_SetDifficulty (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');

      if (list.Length > 0) {
         int difficultyDevel = 1;
         try {
            difficultyDevel = int.Parse(list[0]);
         } catch {
            _player.Target_FloatingMessage(_player.connectionToClient, "Invalid Voyage ID" + " : " + parameters);
         }

         _player.voyageGroupId = difficultyDevel;
         Instance playersInstance = InstanceManager.self.getInstance(_player.instanceId);
         if (playersInstance) {
            playersInstance.difficulty = difficultyDevel;
            string message = "Modified Difficulty level of instance:{" + playersInstance.id + "} Difficulty to:{" + difficultyDevel + "}";
            D.debug(message);
            _player.rpc.Target_ReceiveNoticeFromServer(_player.connectionToClient, message);
         }
      }
   }

   private void requestThrowTestErrors (string parameters) {
      Cmd_ThrowTestErrors(parameters);
   }

   [Command]
   protected void Cmd_ThrowTestErrors (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      D.error("[TEST-LOG] [D.error] " + parameters);
      D.warning("[TEST-LOG] [D.warning] " + parameters);
      D.log("[TEST-LOG] [D.log] " + parameters);
      Debug.LogError("[TEST-LOG] [Debug.LogError] " + parameters);
      Debug.LogWarning("[TEST-LOG] [Debug.LogWarning] " + parameters);
      Debug.Log("[TEST-LOG] [Debug.Log] " + parameters);
   }

   private void toggleNubis (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      if (int.TryParse(parameters, out int isEnabledInt)) {
         if (isEnabledInt > 0) {
            Global.isUsingNubis = true;
            ChatManager.self.addChat("Nubis has been enabled", ChatInfo.Type.System);
         } else {
            Global.isUsingNubis = false;
            ChatManager.self.addChat("Nubis has been disabled", ChatInfo.Type.System);
         }
      } else {
         ChatManager.self.addChat("Could not parse the isEnabled parameter (only 0 or 1 values allowed): " + parameters, ChatInfo.Type.Error);
      }
   }

   private void requestDBTest (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      if (int.TryParse(parameters, out int queriesPerSecond)) {
         Cmd_RunDBQueriesTest(queriesPerSecond);
      } else {
         ChatManager.self.addChat("Could not parse the queries per second parameter: " + parameters, ChatInfo.Type.Error);
      }
   }

   [Command]
   public void Cmd_RunDBQueriesTest (int queriesPerSecond) {
      if (!_player.isAdmin()) {
         return;
      }

      List<int> userIdList = new List<int>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         userIdList = DB_Main.getAllUserIds();
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (userIdList.Count <= 0) {
               userIdList.Add(_player.userId);
            }

            StartCoroutine(CO_RunDBQueriesTest(userIdList, queriesPerSecond));
         });
      });
   }

   private IEnumerator CO_RunDBQueriesTest (List<int> userIdList, int queriesPerSecond) {
      float testDuration = 1f;
      float testDurationSafetyLimit = 20f;

      int currentQueryCount = 0;
      int targetQueryCount = Mathf.CeilToInt(testDuration * queriesPerSecond);

      int userId = _player.userId;

      List<float> queryDurationList = new List<float>();

      DateTime startTime = DateTime.UtcNow;

      while (currentQueryCount < targetQueryCount) {
         int targetThisFrame = Mathf.FloorToInt((float) (DateTime.UtcNow - startTime).TotalSeconds * queriesPerSecond);
         targetThisFrame = Mathf.Clamp(targetThisFrame, 0, targetQueryCount);

         // Run all the db queries needed to catch up on the target count this frame
         for (int i = 0; i < targetThisFrame - currentQueryCount; i++) {
            runSingleTestDBQuery(userIdList.ChooseRandom(), queryDurationList, startTime, testDurationSafetyLimit);
         }

         currentQueryCount = targetThisFrame;
         yield return null;
      }

      // Wait until all the queries have been executed
      while (queryDurationList.Count < targetQueryCount && (DateTime.UtcNow - startTime).TotalSeconds < testDurationSafetyLimit) {
         yield return null;
      }

      float timeSinceStart = (float)(DateTime.UtcNow - startTime).TotalSeconds;

      if (timeSinceStart > testDurationSafetyLimit) {
         _player.Target_ReceiveNormalChat($"The test was stopped earlier because it lasted more than {testDurationSafetyLimit}s", ChatInfo.Type.System);

         // Wait a few more seconds so that the background threads can finish and log their results
         yield return new WaitForSeconds(2f);
      }
      
      // Calculate the statistics on the query durations
      float averageDuration = 0;
      float maxDuration = 0;
      if (queryDurationList.Count > 0) {
         averageDuration = queryDurationList.Average();
         maxDuration = queryDurationList.Max();
      }

      string message = $"{queryDurationList.Count} getUserObjects queries ran over {timeSinceStart.ToString("F3")}s - avg: {averageDuration}ms max: {maxDuration}ms";

      _player.Target_ReceiveNormalChat(message, ChatInfo.Type.System);
   }

   private void runSingleTestDBQuery (int userId, List<float> queryDurationList, DateTime startTime, float durationSafetyLimit) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Don't execute the query if too much time has passed since the beginning of the test
         if ((DateTime.UtcNow - startTime).TotalSeconds > durationSafetyLimit) {
            return;
         }

         System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
         stopWatch.Start();
         UserObjects userObjects = DB_Main.getUserObjects(userId);
         stopWatch.Stop();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            queryDurationList.Add(stopWatch.ElapsedMilliseconds);
         });
      });
   }

   private void battleSimulate (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      // Stops the existing coroutines
      StopAllCoroutines();

      // Forces party members to auto attack and join the combat
      forceJoin("1 .25");

      string[] list = parameters.Split(' ');
      if (list.Length > 0) {
         int battleCount = 1;
         try {
            // Simulate the combat x number of times
            battleCount = int.Parse(list[0]);
            try {
               int enemyType = int.Parse(list[1]);
               StartCoroutine(CO_battleSimulate(battleCount, enemyType));
            } catch {
               StartCoroutine(CO_battleSimulate(battleCount));
            }
         } catch {
            // If parameter is invalid, simulate combat 10 times
            StartCoroutine(CO_battleSimulate(10));
         }
      } else {
         // If parameter is null, simulate combat 10 times
         StartCoroutine(CO_battleSimulate(10));
      }
   }

   private IEnumerator CO_battleSimulate (int battleCounter, int enemyId = (int) Enemy.Type.Pirate_Shooter) {
      // Spawns an enemy infront of the player that will trigger combat automatically
      string enemyName = ((Enemy.Type) enemyId).ToString();
      spawnCustomEnemy(enemyName);
      
      // Wait for player to enter combat before proceeding
      if (!Global.player.isInBattle()) {
         while (!Global.player.isInBattle()) {
            yield return 0;
         }
      }

      // Wait for combat to end
      while (Global.player.isInBattle()) {
         yield return 0;
      }

      yield return new WaitForSeconds(1);

      // Deduct battle counter, if counter is not 0 then keep repeating battle simulation
      battleCounter -= 1;
      if (battleCounter > 0) {
         StartCoroutine(CO_battleSimulate(battleCounter));
      } else {
         D.debug("Combat Simulation Ended");
      }
   }

   private void autoAttack (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');
      if (list.Length > 0) {
         float attackDelay = 0.01f;
         try {
            attackDelay = float.Parse(list[0]);
         } catch {
            D.debug("AttackDelay is invalid: " + list[0]);
         }

         float clampedAttackDelay = Mathf.Clamp(attackDelay, .1f, attackDelay);
         Cmd_AutoAttack(clampedAttackDelay);
      } else {
         Cmd_AutoAttack(.5f);
      }
   }

   private void freezeShips () {
      Cmd_FreezeShips();
   }

   [Command]
   protected void Cmd_FreezeShips () {
      if (!_player.isAdmin()) {
         return;
      }
      Global.freezeShips = !Global.freezeShips;

      _player.Target_FloatingMessage(_player.connectionToClient, Global.freezeShips ? "All ships will now be frozen in place" : "All bot ships will now be unfrozen");
   }


   [Command]
   protected void Cmd_AutoAttack (float attackDelay) {
      if (!_player.isAdmin()) {
         return;
      }

      _player.Target_AutoAttack(attackDelay);
   }

   private void forceJoin (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');
      if (list.Length > 1) {
         int autoAttack = 0;
         try {
            autoAttack = int.Parse(list[0]);
         } catch {
            D.debug("AutoAttack is invalid: " + list[0]);
         }

         float attackDelay = 0f;
         try {
            attackDelay = float.Parse(list[1]);
         } catch {
            D.debug("AttackDelay is invalid: " + list[1]);
         }

         float clampedAttackDelay = Mathf.Clamp(attackDelay, .1f, attackDelay);
         Cmd_ForceJoin(autoAttack == 1 ? true : false, clampedAttackDelay);
      } else {
         Cmd_ForceJoin(false, 1);
      }
   }

   [Command]
   protected void Cmd_ForceJoin (bool autoAttack, float attackDelay) {
      if (!_player.isAdmin()) {
         return;
      }

      _player.Target_ForceJoin(autoAttack, attackDelay);
      if (autoAttack) {
         // Get the group the player belongs to
         List<int> groupMembers = _player.tryGetGroup(out VoyageGroupInfo voyageGroup) ? voyageGroup.members : new List<int>();

         foreach (int memberId in groupMembers) {
            NetEntity entity = EntityManager.self.getEntity(memberId);
            if (entity != null) {
               if (entity.userId != _player.userId && _player.instanceId == entity.instanceId) {
                  entity.Target_ForceJoin(autoAttack, attackDelay);
               }
            }
         }
      }
   }

   private void unlockWorldMap () {
      if (!_player.isAdmin()) {
         return;
      }

      Cmd_UnlockWorldMap();
   }

   [Command]
   protected void Cmd_UnlockWorldMap () {
      if (!_player.isAdmin()) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<Biome.Type> unlockedBiomeList = DB_Main.getUnlockedBiomes(_player.userId);

         foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
            if (!unlockedBiomeList.Contains(biome)) {
               DB_Main.addUnlockedBiome(_player.userId, biome);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.Target_ReceiveNormalChat("The world map has been unlocked", ChatInfo.Type.System);
         });
      });
   }

   [Command]
   protected void Cmd_KickPlayer (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');

      if (list.Length < 2) {
         _player.Target_ReceiveNormalChat($"Could not parse the parameters for the command kick: {parameters}", ChatInfo.Type.System);
         return;
      }

      string playerName = list[0];
      string reason = parameters.Replace(list[0] + " ", "");

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         var (accId, usrId, usrName) = DB_Main.getUserDataTuple(playerName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (accId <= 0) {
               _player.Target_ReceiveNormalChat($"The player {playerName} doesn't exist!", ChatInfo.Type.System);
               return;
            }

            PenaltyInfo penaltyInfo = new PenaltyInfo(-1, -1, "", PenaltyType.Kick, reason, 0, false);
            ServerNetworkingManager.self.setPenaltyInPlayerEntityByUser(usrId, penaltyInfo);
         });
      });
   }

   private void requestOre (string parameters) {
      Cmd_RequestOreVoyage(parameters);
   }

   [Command]
   private void Cmd_RequestOreVoyage (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

      string[] list = parameters.Split(' ');

      if (list.Length > 0) {
         int voyageId = -1;
         try {
            voyageId = int.Parse(list[0]);
         } catch {
            _player.Target_FloatingMessage(_player.connectionToClient, "Invalid Voyage ID" + " : " + parameters);
         }

         _player.voyageGroupId = voyageId;
         Instance playersInstance = InstanceManager.self.getInstance(_player.instanceId);
         if (playersInstance) {
            D.debug("Total Ores in instance is" + " : " + playersInstance.getOreEntities().Count);
            foreach (NetworkBehaviour temp in playersInstance.getOreEntities()) {
               ((OreNode) temp).voyageId = voyageId;
            }
         }
      }
   }

   private void requestGod () {
      // Don't allow the player to use /admin god when they're dead
      PlayerShipEntity playerShip = _player.getPlayerShipEntity();
      if (playerShip) {
         if (playerShip.isDead()) {
            return;
         }
      }

      Cmd_SetShipDamage("100000");
      Cmd_SetShipHealth("100000");
      requestGodWeapon();
      requestGodArmor();
   }

   private void requestGodWeapon () {
      string godWeaponName = "colts totally not overpowered test weapon";
      Cmd_CreateAndEquipItem(Item.Category.Weapon, godWeaponName, 1);
   }

   private void requestGodArmor () {
      string godArmorName = "ward";
      Cmd_CreateAndEquipItem(Item.Category.Armor, godArmorName, 1);
   }

   private void banPlayerIndefinite (string parameters) {
      Cmd_RequestPermanentPlayerBan(parameters);
   }

   private void banPlayerTemporary (string parameters) {
      Cmd_RequestTemporaryPlayerBan(parameters);
   }

   [Command]
   private void Cmd_RequestPermanentPlayerBan (string parameters) {
      if (!_player.isAdmin()) {
         D.warning("Requested command by non-admin");
         return;
      }

      string[] list = parameters.Split(' ');

      if (list.Length > 0) {
         string playerName = list[0];
         string reason = parameters.Replace(list[0] + " ", "");

         PenaltyInfo penaltyInfo = new PenaltyInfo(_player.accountId, _player.userId, _player.entityName, PenaltyType.Ban, reason, 0, true);
         penaltyInfo.penaltyEnd = DateTime.MinValue.ToBinary();
         banPlayer(playerName, penaltyInfo);
      }
   }

   [ServerOnly]
   protected void mutePlayer (string playerName, PenaltyInfo muteInfo) {
      if (muteInfo.penaltyType == PenaltyType.None) {
         D.error("Invalid PenaltyType: None");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         var (accId, usrId, usrName) = DB_Main.getUserDataTuple(playerName);

         if (accId > 0) {
            if (accId == _player.accountId) {
               _player.Target_ReceiveNormalChat("You can't mute yourself!", ChatInfo.Type.System);
               return;
            }

            muteInfo.targetAccId = accId;
            muteInfo.targetUsrId = usrId;
            muteInfo.targetUsrName = usrName;

            PenaltyStatus status = DB_Main.penalizeAccount(muteInfo);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // If the player is successfully muted
               if (status == PenaltyStatus.None) {
                  _player.Target_ReceiveNormalChat($"Account for player {usrName} has been muted until {Util.getTimeInEST(DateTime.FromBinary(muteInfo.penaltyEnd))} EST", ChatInfo.Type.System);
                  ServerNetworkingManager.self.setPenaltyInPlayerEntityByUser(muteInfo.targetUsrId, muteInfo);
               } else if (status == PenaltyStatus.AlreadyMuted) {
                  _player.Target_ReceiveNormalChat($"Account for player {usrName} is already muted", ChatInfo.Type.System);
               } else {
                  _player.Target_ReceiveNormalChat($"An error ocurred while trying to mute the account for player {playerName}", ChatInfo.Type.System);
               }
            });
         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.Target_ReceiveNormalChat($"No account was found for player {playerName}.", ChatInfo.Type.System);
            });
         }
      });
   }

   [ServerOnly]
   protected void banPlayer (string playerName, PenaltyInfo penaltyInfo) {
      if (penaltyInfo.penaltyType == PenaltyType.None) {
         D.error("Invalid PenaltyType: None");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         (int accId, int usrId, string usrName) = DB_Main.getUserDataTuple(playerName);

         if (accId > 0) {
            if (accId == _player.accountId) {
               _player.Target_ReceiveNormalChat("You can't ban yourself!", ChatInfo.Type.System);
               return;
            }

            penaltyInfo.targetAccId = accId;
            penaltyInfo.targetUsrId = usrId;
            penaltyInfo.targetUsrName = usrName;

            PenaltyStatus status = DB_Main.penalizeAccount(penaltyInfo);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // If the player is successfully banned from the game
               if (status == PenaltyStatus.None) {
                  if (penaltyInfo.isTemporary()) {
                     _player.Target_ReceiveNormalChat($"Account for player {usrName} has been temporarily banned until {Util.getTimeInEST(DateTime.FromBinary(penaltyInfo.penaltyEnd))} EST", ChatInfo.Type.System);
                  } else {
                     _player.Target_ReceiveNormalChat($"Account for player {usrName} has been indefinitely banned", ChatInfo.Type.System);
                  }

                  ServerNetworkingManager.self.setPenaltyInPlayerEntityByUser(penaltyInfo.targetUsrId, penaltyInfo);
                  // If not...
               } else if (status == PenaltyStatus.AlreadyBanned) {
                  _player.Target_ReceiveNormalChat($"Account for player {usrName} is already banned", ChatInfo.Type.System);
               } else {
                  _player.Target_ReceiveNormalChat($"An error ocurred while trying to ban the account for player {playerName}", ChatInfo.Type.System);
               }
            });
         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.Target_ReceiveNormalChat($"No account was found for player {playerName}.", ChatInfo.Type.System);
            });
         }
      });
   }

   [Server]
   public void setPenaltyInPlayerEntity (NetEntity player, PenaltyInfo penaltyInfo) {
      switch (penaltyInfo.penaltyType) {
         case PenaltyType.Mute:
            player.muteExpirationDate = DateTime.FromBinary(penaltyInfo.penaltyEnd);
            player.isStealthMuted = false;
            player.Target_ReceiveNormalChat($"You have been muted until {Util.getTimeInEST(DateTime.FromBinary(penaltyInfo.penaltyEnd))} EST", ChatInfo.Type.System);
            break;
         case PenaltyType.StealthMute:
            player.muteExpirationDate = DateTime.FromBinary(penaltyInfo.penaltyEnd);
            player.isStealthMuted = true;
            break;
         case PenaltyType.Ban:
            player.connectionToClient.Send(new ErrorMessage(Global.netId, ErrorMessage.Type.Banned, ServerMessageManager.getPenaltyMessage(penaltyInfo)));
            break;
         case PenaltyType.Kick:
            player.connectionToClient.Send(new ErrorMessage(Global.netId, ErrorMessage.Type.Kicked, ServerMessageManager.getPenaltyMessage(penaltyInfo)));
            break;
         default:
            break;
      }
   }

   [Server]
   public void liftPenaltyInPlayerEntity (NetEntity player, PenaltyType penaltyType) {
      if (penaltyType == PenaltyType.Mute || penaltyType == PenaltyType.StealthMute) {
         player.muteExpirationDate = DateTime.MinValue;
         player.isStealthMuted = false;
      }
   }

   [Command]
   private void Cmd_RequestPlayerStealthMute (string parameters) {
      if (!_player.isAdmin()) {
         D.warning("Requested command by non-admin");
         return;
      }

      string[] list = parameters.Split(' ');

      // Make sure we have at least 2 params: name and duration. Reason is optional.
      if (list.Length >= 2) {
         string playerName = list[0];

         if (!int.TryParse(list[1], out int minutes)) {
            D.log($"Invalid paramters for /admin mute command: cannot parse {list[1]} to int.");
         }

         string reason = parameters.Replace($"{list[0]} {list[1]} ", "");

         PenaltyInfo penaltyInfo = new PenaltyInfo(_player.accountId, _player.userId, _player.entityName, PenaltyType.StealthMute, reason, minutes, false);
         mutePlayer(playerName, penaltyInfo);
      }
   }

   [Command]
   private void Cmd_RequestPlayerMute (string parameters) {
      if (!_player.isAdmin()) {
         D.warning("Requested command by non-admin");
         return;
      }

      string[] list = parameters.Split(' ');

      // Make sure we have at least 2 params: name and duration. Reason is optional.
      if (list.Length >= 2) {
         string playerName = list[0];

         if (!int.TryParse(list[1], out int minutes)) {
            D.log($"Invalid paramters for /admin mute command: cannot parse {list[1]} to int.");
         }

         string reason = parameters.Replace($"{list[0]} {list[1]} ", "");

         PenaltyInfo penaltyInfo = new PenaltyInfo(_player.accountId, _player.userId, _player.entityName, PenaltyType.Mute, reason, minutes, false);
         mutePlayer(playerName, penaltyInfo);
      }
   }

   [Command]
   private void Cmd_RequestTemporaryPlayerBan (string parameters) {
      if (!_player.isAdmin()) {
         D.warning("Requested command by non-admin");
         return;
      }

      string[] list = parameters.Split(' ');

      // Make sure we have at least 2 parameters: name and duration. Reason is optional.
      if (list.Length >= 2) {
         string playerName = list[0];

         if (!int.TryParse(list[1], out int minutes)) {
            D.log($"Invalid parameters for /admin ban command: cannot parse {list[1]} to int.");
            return;
         }

         string reason = parameters.Replace($"{list[0]} {list[1]} ", "");

         PenaltyInfo penaltyInfo = new PenaltyInfo(_player.accountId, _player.userId, _player.entityName, PenaltyType.Ban, reason, minutes, false);
         banPlayer(playerName, penaltyInfo);
      }
   }

   [Command]
   protected void Cmd_SpawnSeaEnemy (string parameters, Vector3 spawnPosition) {
      string[] list = parameters.Split(' ');
      if (list.Length > 0) {
         int enemyId = int.Parse(list[0]);

         SeaMonsterEntityData spawnEnemyData = null;

         foreach (SeaMonsterEntityData data in SeaMonsterManager.self.seaMonsterDataList) {
            if (data.xmlId == enemyId) {
               spawnEnemyData = data;
               break;
            }
         }

         if (spawnEnemyData == null) {
            ChatManager.self.addChat("Couldn't find sea enemy with id: " + enemyId, ChatInfo.Type.System);
         } else {
            InstanceManager.self.getInstance(_player.instanceId).spawnSeaEnemy(spawnEnemyData, spawnPosition);
         }
      }
   }

   [Command]
   protected void Cmd_SetShipDamage (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

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
      if (!_player.isAdmin()) {
         return;
      }

      if (GetComponent<PlayerShipEntity>() != null) {
         string[] list = parameters.Split(' ');
         if (list.Length > 0) {
            int shipHealth = int.Parse(list[0]);
            GetComponent<PlayerShipEntity>().currentHealth = shipHealth;
         }
      }
   }

   [Command]
   protected void Cmd_RequestWarpAnywhere () {
      if (!_player.isAdmin()) {
         return;
      }

      // If the player is admin, allow them to warp anywhere without being forced back to town
      EntityManager.self.addBypassForUser(_player.userId);
   }

   [TargetRpc]
   public void Target_ReceiveLogAuthorization (NetworkConnection connection, string logType, bool isEnabled, string parameters) {
      processLogAuthorization(logType, isEnabled, parameters);
      D.log("This user has " + (isEnabled ? "Enabled" : "Disabled") + " {" + logType + "} logs!");
   }

   private void processLogAuthorization (string logType, bool isEnabled, string parameters) {
      D.ADMIN_LOG_TYPE newLogType = D.ADMIN_LOG_TYPE.None;
      switch (logType) {
         case "cancelatk":
            newLogType = D.ADMIN_LOG_TYPE.CancelAttack;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "gamepad":
            newLogType = D.ADMIN_LOG_TYPE.Gamepad;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "combat":
            newLogType = D.ADMIN_LOG_TYPE.Combat;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "crop":
            newLogType = D.ADMIN_LOG_TYPE.Crop;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "sea":
            newLogType = D.ADMIN_LOG_TYPE.Sea;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "warp":
            newLogType = D.ADMIN_LOG_TYPE.Warp;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "network":
            newLogType = D.ADMIN_LOG_TYPE.NetworkMessages;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "boss":
            newLogType = D.ADMIN_LOG_TYPE.Boss;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "mine":
            newLogType = D.ADMIN_LOG_TYPE.Mine;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "ability":
            newLogType = D.ADMIN_LOG_TYPE.Ability;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "equipment":
            newLogType = D.ADMIN_LOG_TYPE.Equipment;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "announcement":
            newLogType = D.ADMIN_LOG_TYPE.PvpAnnouncement;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "quest":
            newLogType = D.ADMIN_LOG_TYPE.Quest;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "treasure":
            newLogType = D.ADMIN_LOG_TYPE.Treasure;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "refine":
            newLogType = D.ADMIN_LOG_TYPE.Refine;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         case "bp":
            newLogType = D.ADMIN_LOG_TYPE.Blueprints;
            Global.updateAdminLog(newLogType, isEnabled);
            break;
         default:
            if (Util.isServerBuild()) {
               _player.Target_FloatingMessage(_player.connectionToClient, "Invalid Parameters" + " : " + parameters);
            }
            break;
      }
      D.adminLog("Test Log", newLogType);
   }

   [Command]
   protected void Cmd_RequestScreenLog () {
      if (!_player.isAdmin()) {
         return;
      }

      // If the player is admin, send them an rpc allowing them to activate the GUI of the screen logger
      Target_ReceiveScreenLog(_player.connectionToClient);
   }

   [TargetRpc]
   public void Target_ReceiveScreenLog (NetworkConnection connection) {
      ScreenLogger.self.adminActivateLogger();
   }

   private void requestSpawnSeaEnemy (string parameters) {
      Cmd_SpawnSeaEnemy(parameters, Util.getMousePos());
   }

   private void requestInvisibility () {
      if (!_player.isAdmin()) {
         return;
      }

      _player.Cmd_ToggleAdminInvisibility();
   }

   private void requestAddShips () {
      Cmd_AddShips();
   }

   private void requestCheckFPS () {
      Cmd_CheckFPS();
   }

   private void requestShutdown () {
      Cmd_Shutdown();
   }

   private void requestSpawnEnemy () {
      Cmd_SpawnEnemy();
   }

   private void requestServerInfo () {
      Cmd_ServerInfo();
   }

   private void requestCreateShopShips () {
      Cmd_CreateShopShips();
   }

   private void requestCreateShopItems () {
      Cmd_CreateShopItems();
   }

   private void requestBotWaypoint () {
      Cmd_BotWaypoint();
   }

   private void requestCancelServerRestart () {
      Cmd_CancelServerRestart();
   }

   private void requestSetShipDamage (string parameter) {
      Cmd_SetShipDamage(parameter);
   }

   private void requestSetShipHealth (string parameter) {
      Cmd_SetShipHealth(parameter);
   }

   private void requestXmlLogs (string parameter) {
      string[] list = parameter.Split(' ');
      if (list.Length > 0) {
         string xmlType = list[0];

         switch (xmlType) {
            case "weapon":
               foreach (WeaponStatData weapon in EquipmentXMLManager.self.weaponStatList) {
                  D.debug("->Weapon:: Name{" + weapon.equipmentName + "} ID{" + weapon.sqlId + "}");
               }
               break;
            case "armor":
               foreach (ArmorStatData armor in EquipmentXMLManager.self.armorStatList) {
                  D.debug("->Armor:: Name{" + armor.equipmentName + "} ID{" + armor.sqlId + "}");
               }
               break;
         }
      }
   }

   private void requestScreenLogs () {
      Cmd_RequestScreenLog();
   }

   private void requestWarpAnywhere () {
      Cmd_RequestWarpAnywhere();
   }

   private void requestMutePlayer (string parameters) {
      Cmd_RequestPlayerMute(parameters);
   }

   private void requestStealthMutePlayer (string parameters) {
      Cmd_RequestPlayerStealthMute(parameters);
   }

   private static void interactAnvil () {
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
      StartCoroutine(ChatPanel.self.CO_MoveCaretToEnd(ChatPanel.self.inputField));
   }

   public void tryAutoCompleteForGetItemCommand (string inputString) {
      if (!_player.isAdmin()) {
         return;
      }

      if (!inputString.StartsWith("/admin get_item ") && !inputString.StartsWith("/a get_item ")) {
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
               dictionary = AdminManagerSingleton.self.weaponNames;
               break;
            case "a":
               dictionary = AdminManagerSingleton.self.armorNames;
               break;
            case "h":
               dictionary = AdminManagerSingleton.self.hatNames;
               break;
            case "c":
               dictionary = AdminManagerSingleton.self.craftingIngredientNames;
               break;
            case "u":
               dictionary = AdminManagerSingleton.self.usableNames;
               break;
            case "b":
               dictionary = AdminManagerSingleton.self.blueprintNames;
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

   protected void requestSetAdminPrivileges (string parameters) {
      string[] list = parameters.Split(' ');
      string username = "";
      int adminFlag = 0;
      try {
         username = list[0];
         adminFlag = Convert.ToInt32(list[1]);
      } catch (System.Exception e) {
         D.warning("Unable to set admin privileges for the player " + username);
         return;
      }

      // Send the request to the server
      Cmd_SetAdminPrivileges(username, adminFlag);
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

   protected static void requestAllAbilities () {
      List<BasicAbilityData> allAbilities = AbilityManager.self.allGameAbilities;
      Global.player.rpc.Cmd_UpdateAbilities(AbilitySQLData.TranslateBasicAbility(allAbilities).ToArray());
   }

   private void requestSummonPlayer (string parameters) {
      if (!_player.isAdmin()) {
         return;
      }

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
         case "h":
            category = Item.Category.Hats;
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
            if (AdminManagerSingleton.self.weaponNames.ContainsKey(itemName)) {
               itemTypeId = AdminManagerSingleton.self.weaponNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the weapon " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.Armor:
            if (AdminManagerSingleton.self.armorNames.ContainsKey(itemName)) {
               itemTypeId = AdminManagerSingleton.self.armorNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the armor " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.Hats:
            if (AdminManagerSingleton.self.hatNames.ContainsKey(itemName)) {
               itemTypeId = AdminManagerSingleton.self.hatNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the hat " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.Usable:
            if (AdminManagerSingleton.self.usableNames.ContainsKey(itemName)) {
               itemTypeId = AdminManagerSingleton.self.usableNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the usable item " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.CraftingIngredients:
            if (AdminManagerSingleton.self.craftingIngredientNames.ContainsKey(itemName)) {
               itemTypeId = AdminManagerSingleton.self.craftingIngredientNames[itemName];
            } else {
               ChatManager.self.addChat("Could not find the crafting ingredient " + itemName, ChatInfo.Type.Error);
               return;
            }
            break;
         case Item.Category.Blueprint:
            if (AdminManagerSingleton.self.blueprintNames.ContainsKey(itemName)) {
               itemTypeId = AdminManagerSingleton.self.blueprintNames[itemName];
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
            D.editorLog("Added new abilities", Color.green);
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
   protected void Cmd_SetAdminPrivileges (string username, int adminFlag) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      // Update the player's admin privileges in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int userId = DB_Main.getUserId(username);

         if (userId > 0) {
            DB_Main.setAdmin(userId, adminFlag);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (NetEntity entity in MyNetworkManager.getPlayers()) {
                  if (entity.userId == userId) {
                     entity.setAdminPrivileges(adminFlag);
                  }
               }
               // Send confirmation back to the player who issued the command
               string message = "";
               if (adminFlag == 1) {
                  message = string.Format("Admin privileges have been granted to {0}.", username);
               } else {
                  message = string.Format("Admin privileges have been removed from {0}.", username);
               }
               ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.General, userId, message);
               ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.General, _player.userId, message);
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
      int difficulty = 0;
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
            try {
               difficulty = int.Parse(list[1]);
            } catch {
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
   private void Cmd_CreateVoyageInstance (bool isPvP, int difficulty, Biome.Type biome, string areaKey) {
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

      VoyageManager.self.requestVoyageInstanceCreation(areaKey, isPvP, false, 0, -1, biome, difficulty);
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
      if (!_player.isAdmin()) {
         return;
      }
      
      ChatManager.self.addChat($"Server Shutdown Command Sent!]", ChatInfo.Type.Local);
      
      // To the database thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateServerShutdown(true);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            string message = $"Server will shutdown now!";
            ChatInfo.Type chatType = ChatInfo.Type.Global;
            ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.UtcNow, chatType);
            ServerNetworkingManager.self?.sendGlobalChatMessage(chatInfo);

            RestartManager.self.onServerShutdown();
         });
      });

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

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
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
      });
   }

   [Command]
   private void Cmd_SummonPlayer (string targetPlayerName) {
      if (!_player.isAdmin()) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to retrieve the target info
         UserInfo targetUserInfo = DB_Main.getUserInfo(targetPlayerName);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (targetUserInfo == null) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The player " + targetPlayerName + " doesn't exists!");
               return;
            }

            if (!ServerNetworkingManager.self.isUserOnline(targetUserInfo.userId)) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The player " + targetPlayerName + " is offline!");
               return;
            }

            if (targetUserInfo.userId == _player.userId) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You cannot warp to yourself!");
               return;
            }

            UserLocationBundle location = new UserLocationBundle();
            location.userId = _player.userId;
            location.serverPort = ServerNetworkingManager.self.server.networkedPort.Value;
            location.areaKey = _player.areaKey;
            location.instanceId = _player.instanceId;
            location.localPositionX = _player.transform.localPosition.x;
            location.localPositionY = _player.transform.localPosition.y;
            location.voyageGroupId = _player.voyageGroupId;

            // Find the target user in the server network and try to warp him here
            ServerNetworkingManager.self.summonUser(targetUserInfo.userId, location);
         });
      });
   }

   [Command]
   protected void Cmd_PlayerGo (string targetPlayerName) {
      if (!_player.isAdmin()) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to retrieve the target info
         UserInfo targetUserInfo = DB_Main.getUserInfo(targetPlayerName);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (targetUserInfo == null) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The player " + targetPlayerName + " doesn't exists!");
               return;
            }

            if (!ServerNetworkingManager.self.isUserOnline(targetUserInfo.userId)) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The player " + targetPlayerName + " is offline!");
               return;
            }

            if (targetUserInfo.userId == _player.userId) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You cannot warp to yourself!");
               return;
            }

            // Redirect to the master server to find the location of the target user
            ServerNetworkingManager.self.findUserLocationForAdminGoTo(_player.userId, targetUserInfo.userId);
         });
      });
   }

   [Server]
   public void forceWarpToLocation (int requesterUserId, UserLocationBundle targetLocation) {
      // If our player is in the same instance than the target, simply teleport
      if (ServerNetworkingManager.self.server.networkedPort.Value == targetLocation.serverPort && _player.areaKey == targetLocation.areaKey && _player.instanceId == targetLocation.instanceId) {
         _player.moveToPosition(targetLocation.getLocalPosition());
         return;
      }

      // Handle warping to an instance that is specific to a group
      if (VoyageManager.isAnyLeagueArea(targetLocation.areaKey) || VoyageManager.isTreasureSiteArea(targetLocation.areaKey) || VoyageManager.isPvpArenaArea(targetLocation.areaKey)) {
         if (!VoyageManager.self.tryGetVoyageForGroup(targetLocation.voyageGroupId, out Voyage targetVoyage)) {
            ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.General, requesterUserId, "Error when warping user: could not find the target voyage.");
            return;
         }

         if (_player.isAdmin()) {
            // If our user is an admin, we can warp anywhere
            VoyageGroupManager.self.forceAdminJoinVoyage(_player, targetVoyage.voyageId);

            // For treasure sites, the admin must be registered in the treasure site object that warps to the instance
            if (VoyageManager.isTreasureSiteArea(targetLocation.areaKey)) {
               ServerNetworkingManager.self.registerUserInTreasureSite(_player.userId, targetVoyage.voyageId, targetLocation.instanceId);
            }
         } else { 
            // Non admins can only warp or be warped to instances that they have access to, through their current group
            if (!_player.tryGetGroup(out VoyageGroupInfo ourGroup) || ourGroup.voyageId != targetVoyage.voyageId) {
               ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.General, requesterUserId, "Error when warping user: the user does not have access to the target instance.");
               return;
            }
         }

         // Warp to the specific instance
         _player.findBestServerAndWarp(targetLocation.areaKey, targetLocation.getLocalPosition(), targetVoyage.voyageId, Direction.South);
         return;
      }

      _player.spawnInNewMap(targetLocation.areaKey, targetLocation.getLocalPosition(), Direction.South);
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

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (scheduled) {
               string timeUnit = (delay == 1) ? "minute" : "minutes";
               string message = $"Server will reboot in {delay} {timeUnit}!";
               ChatInfo.Type chatType = ChatInfo.Type.Global;
               ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.UtcNow, chatType);
               ServerNetworkingManager.self?.sendGlobalChatMessage(chatInfo);

               RestartManager.self.onScheduledServerRestart(timePoint);
            }
         });
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

      RestartManager.self.onCanceledServerRestart();
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
         string[] testAreaKeys = { "bot_test" };
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

      if (Util.isAutoWarping()) {
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

   private void spawnCustomEnemy (string parameters) {
      parameters = parameters.Trim(' ').Replace(" ", "_");
      if (Enum.TryParse(parameters, true, out Enemy.Type enemyType)) {
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
      if (checkAdminCommand("/admin warp", "")) {
         requestWarp("");
      }
   }

   public void warpToPosition (Vector3 localPosition) {
      // Set position locally only if the player isn't a ShipEntity. Ships' positions are controlled by the server.
      if (_player.getPlayerBodyEntity() != null) {
         _player.transform.localPosition = localPosition;

         // Show a smoke effect in the new position
         EffectManager.show(Effect.Type.Cannon_Smoke, _player.transform.position);
      }

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
         _player.rpc.Rpc_PlayWarpEffect(_player.transform.position);
      }
   }

   [Command]
   protected void Cmd_CreateAndEquipItem (Item.Category category, string itemName, int count) {
      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      int itemTypeId = -1;

      // Get the item type ID
      switch (category) {
         case Item.Category.Armor:
            itemTypeId = AdminManagerSingleton.self.armorNames[itemName];
            break;
         case Item.Category.Weapon:
            itemTypeId = AdminManagerSingleton.self.weaponNames[itemName];
            break;
         case Item.Category.Hats:
            itemTypeId = AdminManagerSingleton.self.hatNames[itemName];
            break;
      }

      // Go to the background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         int itemId = DB_Main.getItemID(_player.userId, (int) category, itemTypeId);

         // Check if the user has the item
         if (!DB_Main.hasItem(_player.userId, itemId, (int) category)) {
            // If not, create the item object
            Item item = new Item(-1, category, itemTypeId, count, "", "", Item.MAX_DURABILITY);

            // Write the item in the DB
            DB_Main.createItemOrUpdateItemCount(_player.userId, item);
            itemId = DB_Main.getItemID(_player.userId, (int) category, itemTypeId);
         }

         // Back to the main thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            switch (category) {
               case Item.Category.Armor:
                  _player.rpc.requestSetArmorId(itemId);
                  break;
               case Item.Category.Weapon:
                  _player.rpc.requestSetWeaponId(itemId);
                  break;
               case Item.Category.Hats:
                  _player.rpc.requestSetHatId(itemId);
                  break;
            }
         });
      });
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
         Item item = new Item(-1, category, itemTypeId, count, "", "", Item.MAX_DURABILITY);

         // Write the item in the DB
         Item databaseItem = DB_Main.createItemOrUpdateItemCount(_player.userId, item);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send confirmation back to the player who issued the command
            int createdItemCount = databaseItem.count < count ? databaseItem.count : count;
            string message = string.Format("Added {0} {1} to the inventory.", createdItemCount, EquipmentXMLManager.self.getItemName(databaseItem));
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
         Item item = new Item(-1, category, itemTypeId, count, paletteNames, "", Item.MAX_DURABILITY);

         // Write the item in the DB
         Item databaseItem = DB_Main.createItemOrUpdateItemCount(_player.userId, item);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send confirmation back to the player who issued the command
            int createdItemCount = databaseItem.count < count ? databaseItem.count : count;
            string message = string.Format("Added {0} {1} to the inventory.", createdItemCount, EquipmentXMLManager.self.getItemName(databaseItem));
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
         // TODO: Uncomment this only after usable items are supported
         /*
         // Create all the usable
         foreach (UsableItem.Type usableType in Enum.GetValues(typeof(UsableItem.Type))) {
            if (usableType != UsableItem.Type.None) {
               if (createItemIfNotExistOrReplenishStack(Item.Category.Usable, (int) usableType, count)) {
                  usableCount++;
               }
            }
         }*/

         // Create all the blueprints
         foreach (CraftableItemRequirements craftData in CraftingManager.self.craftingDataList) {
            Item newItem = new Item {
               category = Item.Category.Blueprint,
               itemTypeId = craftData.resultItem.itemTypeId,
               count = 1
            };
            switch (craftData.resultItem.category) {
               case Item.Category.Weapon:
                  newItem.data = Blueprint.WEAPON_DATA_PREFIX;
                  DB_Main.createItemOrUpdateItemCount(_player.userId, newItem);
                  break;
               case Item.Category.Armor:
                  newItem.data = Blueprint.ARMOR_DATA_PREFIX;
                  DB_Main.createItemOrUpdateItemCount(_player.userId, newItem);
                  break;
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
         foreach (HatStatData hatData in EquipmentXMLManager.self.hatStatList) {
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
      bool shouldCreateItem = false;

      // Retrieve the item from the user inventory, if it exists
      Item databaseItem = DB_Main.getFirstItem(_player.userId, category, itemTypeId);

      if (databaseItem != null) {
         Item castedItem = databaseItem.getCastItem();
         if (castedItem != null) {
            // If the item can be stacked and there are less items than what is requested, replenish the stack
            if (castedItem.canBeStacked() && castedItem.count < count && castedItem.category == Item.Category.CraftingIngredients) {
               DB_Main.updateItemQuantity(_player.userId, castedItem.id, count);
               wasItemCreated = true;
            }
         } else {
            shouldCreateItem = true;
         }
      } else {
         shouldCreateItem = true;
      }

      if (shouldCreateItem) {
         // If the item does not exist, create a new one
         Item baseItem = new Item(-1, category, itemTypeId, count, "", "", Item.MAX_DURABILITY).getCastItem();
         DB_Main.createItemOrUpdateItemCount(_player.userId, baseItem);
         wasItemCreated = true;
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

   [Command]
   public void Cmd_GetServerLogString () {
      // Transform into a byte array to avoid the 'buffer is too small' error
      byte[] data = Encoding.ASCII.GetBytes(D.getLogString());

      Target_ReceiveServerLogString(connectionToClient, data);
   }

   [TargetRpc]
   public void Target_ReceiveServerLogString (NetworkConnection connection, byte[] serverLogData) {
      D.serverLogString = Encoding.ASCII.GetString(serverLogData);
   }

   protected void resetShops () {
      if (!_player.isAdmin()) {
         return;
      }

      Cmd_ResetShops();
   }

   [Command]
   public void Cmd_ResetShops () {
      D.debug("Shop items have now been regenerated!");
      ShopManager.self.randomlyGenerateShips();
      ShopManager.self.generateItemsFromXML();
   }

   protected void showAdminPanel () {
      if (!_player.isAdmin()) {
         return;
      }

      AdminPanel.self.show();
   }

   #region Private Variables

   // A history of commands we've requested
   protected List<string> _history = new List<string>();

   // The index of our chat history we're currently using in the input field, if any
   protected int _historyIndex = -1;

   // Our associated Player object
   protected NetEntity _player;

   // The last chat input that went through the auto complete process
   private string _lastAutoCompletedInput = "";

   #endregion
}