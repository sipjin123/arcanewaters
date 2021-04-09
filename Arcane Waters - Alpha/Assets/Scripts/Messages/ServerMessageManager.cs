using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using SteamLoginSystem;
using System;
using System.Linq;

public class ServerMessageManager : MonoBehaviour
{
   #region Public Variables

   #endregion

   [ServerOnly]
   public static void On_LogInUserMessage (NetworkConnection conn, LogInUserMessage logInUserMessage) {
      int selectedUserId = logInUserMessage.selectedUserId;

      // Determine the minimum client version for the client's platform
      int minClientGameVersion;
      if (logInUserMessage.clientPlatform == RuntimePlatform.OSXPlayer) {
         minClientGameVersion = GameVersionManager.self.minClientGameVersionMac;
      } else if (logInUserMessage.clientPlatform == RuntimePlatform.LinuxPlayer) {
         minClientGameVersion = GameVersionManager.self.minClientGameVersionLinux;
      } else {
         minClientGameVersion = GameVersionManager.self.minClientGameVersionWin;
      }

      // Only check the client version at login, not when warping between areas or servers
      if (logInUserMessage.isFirstLogin) {
         // Make sure they have the required game version
         if (logInUserMessage.clientGameVersion < minClientGameVersion) {
            string msg = string.Format("Refusing login for {0}, client version {1}, the current version in the cloud is {2}", logInUserMessage.accountName, logInUserMessage.clientGameVersion, minClientGameVersion);
            D.debug(msg);
            sendError(ErrorMessage.Type.ClientOutdated, conn.connectionId);
            return;
         }
      }

      // Grab the user info from the database for the relevant account ID
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<UserInfo> users = new List<UserInfo>();
         List<Armor> armorList = new List<Armor>();
         List<Weapon> weaponList = new List<Weapon>();
         List<Hat> hatList = new List<Hat>();

         int accountId = 0;
         bool isUnauthenticatedSteamUser = logInUserMessage.accountName == "" && logInUserMessage.accountPassword == "";
         bool hasFailedToCreateAccount = false;
         NetworkedServer masterServer = ServerNetworkingManager.self.getServer(Global.MASTER_SERVER_PORT);

         if (logInUserMessage.isSteamLogin) {
            // If the app id is the playtest id then alter the user name
            if (logInUserMessage.steamAppId == SteamLoginManagerServer.GAMEPLAYTEST_APPID && !logInUserMessage.accountName.Contains("@playtest")) {
               logInUserMessage.accountName = logInUserMessage.accountName + "@playtest";
            }
            if (logInUserMessage.steamAppId == SteamLoginManagerServer.GAME_APPID && !logInUserMessage.accountName.Contains("@steam")) {
               logInUserMessage.accountName = logInUserMessage.accountName + "@steam";
            }
            D.adminLog("Account Log: Is Steam Login as account:{" + logInUserMessage.accountName + "}", D.ADMIN_LOG_TYPE.Server_AccountLogin);

            // Get steam account id without requiring password since it is already authenticated by the server
            if (!isUnauthenticatedSteamUser) {
               // Steam user has been verified at this point, continue login using credentials
               accountId = DB_Main.getSteamAccountId(logInUserMessage.accountName);
               D.adminLog("Account Log: Fetched steam account id for STEAM {" + logInUserMessage.accountName + "}" + " : {" + accountId + "}", D.ADMIN_LOG_TYPE.Server_AccountLogin);
            }
         } else {
            D.adminLog("Account Log: Is Standalone Login", D.ADMIN_LOG_TYPE.Server_AccountLogin);

            // If this is not a steam login, users attempting to login should not have steam into their account name
            if (logInUserMessage.accountName.ToLower().Contains("@steam") && !masterServer.accountOverrides.ContainsKey(logInUserMessage.accountName)) {
               D.debug("A non steam user is trying to access a steam account!" + " : " + logInUserMessage.accountName);
               sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
               return;
            }

            // Look up the account ID corresponding to the provided account name and password
            string salt = Util.createSalt("arcane");
            string hashedPassword = Util.hashPassword(salt, logInUserMessage.accountPassword);

            if (masterServer.accountOverrides.ContainsKey(logInUserMessage.accountName)) {
               if (masterServer.accountOverrides[logInUserMessage.accountName] == logInUserMessage.accountPassword) {
                  D.debug("This account password is temporarily overridden: " + logInUserMessage.accountName);

                  // Login account, bypassing password
                  accountId = DB_Main.getOverriddenAccountId(logInUserMessage.accountName);
                  D.debug("Account id fetched for overridden account" + " : " + accountId);
               } else {
                  D.debug("Incorrect account override password");
               }
            } else {
               // Manual login system using input user name and password
               accountId = DB_Main.getAccountId(logInUserMessage.accountName, hashedPassword);
               D.adminLog("Account Log: Fetched steam account id for STANDALONE {" + logInUserMessage.accountName + "}" + " : {" + accountId + "}", D.ADMIN_LOG_TYPE.Server_AccountLogin);
            }
         }

         // Prevent banned accounts from signing in
         if (accountId > 0) {
            PenaltyInfo banInfo = DB_Main.getPenaltyInfoForAccount(accountId, PenaltyType.Ban);
            // If the ban isn't expired
            if (banInfo != null && !banInfo.hasPenaltyExpired()) {
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.debug("This is a banned user! {" + accountId + "}");
                  sendError(ErrorMessage.Type.Banned, conn.connectionId, getBannedMessage(banInfo));
               });
               return;
            }
         }

         // Get the user objects for the selected user ID
         UserObjects userObjects = null;
         string selectedUsrName = "";

         if (selectedUserId > 0) {
            userObjects = DB_Main.getUserObjects(selectedUserId);
            selectedUsrName = userObjects.userInfo.username;
         }

         if (accountId > 0) {
            users = DB_Main.getUsersForAccount(accountId, selectedUserId);
            armorList = DB_Main.getArmorForAccount(accountId, selectedUserId);
            weaponList = DB_Main.getWeaponsForAccount(accountId, selectedUserId);
            hatList = DB_Main.getHatsForAccount(accountId, selectedUserId);
            DB_Main.updateAccountMode(accountId, logInUserMessage.isSinglePlayer);
         } else {
            // Create an account for this new steam user after it is authorized
            if (logInUserMessage.isSteamLogin && !isUnauthenticatedSteamUser) {
               D.adminLog("Attempting to create a new steam user for: {" + logInUserMessage.accountName + "}", D.ADMIN_LOG_TYPE.Server_AccountLogin);

               if (logInUserMessage.accountName.Length > SteamLoginManager.MIN_STEAM_ID_LENGTH) {
                  accountId = DB_Main.createAccount(logInUserMessage.accountName, logInUserMessage.accountPassword, logInUserMessage.accountName.Replace("@", "") + "@codecommode.com", 0);
                  D.adminLog("Account Log: Creating an account for STEAM {" + logInUserMessage.accountName + "}" + " : {" + accountId + "}", D.ADMIN_LOG_TYPE.Server_AccountLogin);
               } else {
                  D.debug("Failed to process account creation! User does not own this app" + " : " + logInUserMessage.accountName + " : " + logInUserMessage.steamUserId);
                  sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
               }

               if (accountId != 0) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     D.debug("Successfully created account for Steam User: {" + logInUserMessage.accountName + "}");
                     On_LogInUserMessage(conn, logInUserMessage);
                  });
                  return;
               } else {
                  hasFailedToCreateAccount = true;
                  D.debug("Failed to create account for Steam User: {" + logInUserMessage.accountName + "}");
               }
            } else {
               D.debug("Account Log: This is Neither a Steam account or is already an Authenticated user {" + logInUserMessage.accountName + "}" + " : {" + accountId + "}");
            }
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (accountId > 0) {
               // Stop the login process if the account is already logged in
               if (MyNetworkManager.isAccountAlreadyOnline(accountId, conn)) {
                  sendError(ErrorMessage.Type.AlreadyOnline, conn.connectionId);
                  return;
               }

               MyNetworkManager.noteAccountIdForConnection(accountId, conn);
            }

            if (isUnauthenticatedSteamUser && !hasFailedToCreateAccount) {
               // Steam user has not been authorized yet, start auth process
               processSteamUserAuth(conn, logInUserMessage);
               return;
            }

            // Cancel process if steam user creation fails
            if (hasFailedToCreateAccount) {
               D.debug("Failed to create an account for user: {" + logInUserMessage.accountName + "}" + " : {" + logInUserMessage.accountPassword + "}");
               sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
               return;
            }

            if (masterServer.accountOverrides.ContainsKey(logInUserMessage.accountName)) {
               D.debug("Attempting to login overridden user {" + logInUserMessage.accountName + "} : {" + accountId + "} : {" + logInUserMessage.selectedUserId + "}");
            } 

            // If there was a valid account ID and a specified user ID, tell the client we authenticated them
            if (accountId > 0 && logInUserMessage.selectedUserId > 0 && users.Count == 1) {
               // Keep track of the user ID that's been authenticated for this connection
               MyNetworkManager.noteUserIdForConnection(logInUserMessage.selectedUserId, logInUserMessage.steamUserId, conn);

               // Storing login info
               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  if (conn != null && logInUserMessage.isFirstLogin) {
                     DB_Main.storeLoginInfo(logInUserMessage.selectedUserId, accountId, selectedUsrName, conn.address, logInUserMessage.machineIdentifier ?? "", logInUserMessage.deploymentId);
                  }
               });

               string loginMessage = (logInUserMessage.isFirstLogin) ? PlayerPrefs.GetString(AdminManager.MOTD_KEY, "") : "";
               D.adminLog("Account Log: Login Authentication Complete! {" + logInUserMessage.accountName + "}" + " : {" + accountId + "}", D.ADMIN_LOG_TYPE.Server_AccountLogin);

               // Now tell the client to move forward with the login process
               LogInCompleteMessage msg = new LogInCompleteMessage(Global.netId, (Direction) users[0].facingDirection,
                  userObjects.accountEmail, userObjects.accountCreationTime, loginMessage);
               conn.Send(msg);

            } else if (accountId > 0 && logInUserMessage.selectedUserId == 0) {
               // We have to deal with these separately because of a bug in Unity
               string[] armorPalettes = new string[armorList.Count];

               // Must be casted to items because data transfer using inherited variables loses its data
               List<Item> weaponItemList = EquipmentXMLManager.self.translateWeaponItemsToItems(weaponList);
               List<Item> armorItemList = EquipmentXMLManager.self.translateArmorItemsToItems(armorList);
               List<Item> hatItemList = EquipmentXMLManager.self.translateHatItemsToItems(hatList);

               // Set the armor palettes of the character
               int paletteIndex = 0;
               foreach (Item armorItem in armorItemList) {
                  armorPalettes[paletteIndex] = EquipmentXMLManager.self.getArmorDataBySqlId(armorItem.itemTypeId).palettes;
                  paletteIndex++;
               }

               // Get the info of the starter armors for character creation
               List<int> startingEquipmentIds = new List<int>();
               List<int> startingSpriteIds = new List<int>();

               int currentArmorId = 1;
               for (int i = 1; i < 4; i++) {
                  ArmorStatData startArmorData = EquipmentXMLManager.self.getArmorDataBySqlId(currentArmorId);
                  if (startArmorData != null) {
                     // Only output visually unique armors
                     startArmorData = EquipmentXMLManager.self.getArmorDataBySqlId(currentArmorId);
                     if (startArmorData != null) {
                        startingEquipmentIds.Add(startArmorData.sqlId);
                        startingSpriteIds.Add(startArmorData.armorType);
                     } else {
                        D.debug("Failed to fetch the armor content of: " + currentArmorId);
                     }
                     currentArmorId++;
                  } else {
                     D.debug("Cannot process starting armor equipment: ArmorType:" + currentArmorId);
                  }
               }
               D.adminLog("Account Log: Login Complete with No Characters! {" + logInUserMessage.accountName + "}" + " : {" + accountId + "}", D.ADMIN_LOG_TYPE.Server_AccountLogin);

               // If there was an account ID but not user ID, send the info on all of their characters for display on the Character screen
               CharacterListMessage msg = new CharacterListMessage(Global.netId, users.ToArray(), armorItemList.ToArray(), weaponItemList.ToArray(), hatItemList.ToArray(), armorPalettes, startingEquipmentIds.ToArray(), startingSpriteIds.ToArray());
               conn.Send(msg);
            } else {
               D.debug("Error! Failed to process login for user");
               sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
            }
         });
      });
   }

   public static string getBannedMessage (PenaltyInfo penaltyInfo) {
      if (penaltyInfo.hasPenaltyExpired()) {
         return "";
      }

      string message;

      if (penaltyInfo.isTemporary()) {
         message = $"Your account has been suspended until {Util.getTimeInEST(penaltyInfo.penaltyEnd)} EST";
      } else {
         message = $"Your account has been suspended indefinitely";
      }

      message += $"\n\nReason: {penaltyInfo.penaltyReason}";

      return message;
   }

   [ServerOnly]
   private static void processSteamUserAuth (NetworkConnection conn, LogInUserMessage loginUserMsg) {
      AuthenticateTicketEvent newTicketEvent = new AuthenticateTicketEvent();
      newTicketEvent.AddListener(_ => {
         // Fetch steam user id from the event response
         string steamUserId = _.response.newParams.ownersteamid;

         if (_.response.newParams.ownersteamid.Length < SteamLoginManager.MIN_STEAM_ID_LENGTH) {
            D.debug("Error! Will fail to process steam app ownership, failed to fetch Steam ID: {" + _.response.newParams.ownersteamid + "}");
            sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
            return;
         }

         // Proceed to next process
         processSteamAppOwnership(conn, loginUserMsg, steamUserId);

         // Clear event listeners
         SteamLoginManagerServer.self.disposeAuthenticationEvent(newTicketEvent);
      });

      // Send ticket to be processed and fetch steam user data
      SteamLoginManagerServer.self.authenticateTicket(loginUserMsg.steamAuthTicket, loginUserMsg.steamTicketSize, newTicketEvent, loginUserMsg.steamAppId, conn.connectionId);
   }

   [ServerOnly]
   private static void processSteamAppOwnership (NetworkConnection conn, LogInUserMessage loginUserMsg, string steamId) {
      AppOwnershipEvent newAppOwnershipEvent = new AppOwnershipEvent();
      newAppOwnershipEvent.AddListener(_ => {
         if (_.appownership.ownersteamid.Length < SteamLoginManager.MIN_STEAM_ID_LENGTH || !_.appownership.ownsapp) {
            D.debug("Error! User does not own this game!" + " : " + _.appownership.ownersteamid);
            sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
            return;
         }

         // Extract user and password, encrypt the password using the steam id and the current date
         DateTime dateOfPurchase = DateTime.Parse(_.appownership.timestamp);

         // Generate random password for each steam user
         string randomCharacters = "";
         for (int i = 0; i < SteamLoginEncryption.PASSWORD_LENGTH; i++) {
            int randomIndex = UnityEngine.Random.Range(0, SteamLoginEncryption.ALPHA_NUMERIC.Length - 1);
            randomCharacters += SteamLoginEncryption.ALPHA_NUMERIC[randomIndex];
         }
         string rawPassword = _.appownership.ownersteamid + randomCharacters;
         string encryptedPassword = SteamLoginEncryption.Encrypt(rawPassword);
         string userName = _.appownership.ownersteamid;

         // Override login message
         loginUserMsg.accountName = userName;
         loginUserMsg.accountPassword = encryptedPassword;

         // Call On_LogInUserMessage again, this time with the user name and password
         On_LogInUserMessage(conn, loginUserMsg);

         // Clear event listeners
         SteamLoginManagerServer.self.disposeOwnershipEvent(newAppOwnershipEvent);
      });

      // Get ownership info using the fetched steamId
      SteamLoginManagerServer.self.getOwnershipInfo(steamId, newAppOwnershipEvent, loginUserMsg.steamAppId);
   }


   public static void sendConfirmation (ConfirmMessage.Type confirmType, NetEntity player, string customMessage = "") {
      ConfirmMessage confirmMessage = new ConfirmMessage(Global.netId, confirmType, System.DateTime.UtcNow.ToBinary(), customMessage);
      NetworkServer.SendToClientOfPlayer(player.netIdent, confirmMessage);
   }

   public static void sendConfirmation (ConfirmMessage.Type confirmType, int connectionId, string customMessage = "") {
      ConfirmMessage confirmMessage = new ConfirmMessage(Global.netId, confirmType, System.DateTime.UtcNow.ToBinary(), customMessage);
      NetworkServer.connections[connectionId].Send(confirmMessage);
   }

   public static void sendError (ErrorMessage.Type errorType, NetEntity player, string customMessage = "") {
      ErrorMessage errorMessage = new ErrorMessage(Global.netId, errorType, customMessage);
      NetworkServer.SendToClientOfPlayer(player.netIdent, errorMessage);
   }

   public static void sendError (ErrorMessage.Type errorType, int connectionId) {
      ErrorMessage errorMessage = new ErrorMessage(Global.netId, errorType);

      if (NetworkServer.connections.ContainsKey(connectionId)) {
         NetworkServer.connections[connectionId].Send(errorMessage);
      }
   }

   public static void sendError (ErrorMessage.Type errorType, int connectionId, string customMessage = "") {
      ErrorMessage errorMessage = new ErrorMessage(Global.netId, errorType, customMessage);
      NetworkServer.connections[connectionId].Send(errorMessage);
   }

   [ServerOnly]
   public static void On_CreateUserMessage (NetworkConnection conn, CreateUserMessage msg) {
      int accountId = MyNetworkManager.getAccountId(conn);
      UserInfo userInfo = msg.userInfo;

      // Clean up the casing of the username
      userInfo.username = Util.UppercaseFirst(msg.userInfo.username.ToLower());

      // Make sure we have a valid account ID for the connections ending this message
      if (accountId <= 0 || !NameUtil.isValid(userInfo.username)) {
         sendError(ErrorMessage.Type.InvalidUsername, conn.connectionId);
         return;
      }

      // If the account is already online, refuse
      /*if (ServerNetwork.self.isAccountOnline(accountId)) {
         sendError(ErrorMessage.Type.AlreadyOnline, netMsg.conn.connectionId);
         return;
      }*/

      // Look up the Area that we're going to place the users into
      Area area = AreaManager.self.getArea(Area.STARTING_TOWN);

      // Look up the Spawn position for the map associated with that area
      userInfo.localPos = SpawnManager.self.getLocalPosition(Area.STARTING_TOWN, Spawn.STARTING_SPAWN);

      // Make sure the name is available
      int existingUserId = -1;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         existingUserId = DB_Main.getUserId(userInfo.username);

         // Show a "Name Taken" panel on the client
         if (existingUserId > 0) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               sendError(ErrorMessage.Type.NameTaken, conn.connectionId);
            });
         } else {
            BKG_finishCreatingUser(msg, msg.steamUserId, accountId, userInfo, conn, area);
         }
      });
   }

   [ServerOnly]
   public static void On_DeleteUserMessage (NetworkConnection conn, DeleteUserMessage msg) {
      int accountId = MyNetworkManager.getAccountId(conn);

      // Make sure the connection owns the account
      if (msg.userId <= 0 || accountId <= 0) {
         D.warning(string.Format("Invalid account id {0} or user id {1} to delete.", accountId, msg.userId));
         return;
      }

      // If the account is already online, refuse
      /*if (ServerNetwork.self.isAccountOnline(accountId)) {
         sendError(ErrorMessage.Type.AlreadyOnline, netMsg.conn.connectionId);
         return;
      }*/

      // Do the deletion on the DB thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int userGuildId = DB_Main.getUserGuildId(msg.userId);

         DB_Main.deleteAllFromTable(accountId, msg.userId, "ships");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "items");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "crops");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "silo");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "perks");

         DB_Main.deleteUser(accountId, msg.userId);

         // Send confirmation to the client, so that they can request their user list again
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Delete the user's guild if it has no more members
            GuildManager.self.deleteGuildIfEmpty(userGuildId);

            sendConfirmation(ConfirmMessage.Type.DeletedUser, conn.connectionId);
         });
      });
   }

   [ServerOnly]
   protected static void BKG_finishCreatingUser (CreateUserMessage msg, string steamUserId, int accountId, UserInfo userInfo, NetworkConnection conn, Area area) {
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         CharacterCreationValidMessage characterValidMessage = new CharacterCreationValidMessage();
         conn.Send(characterValidMessage);
      });

      // Need to create their Armor first
      int armorId = DB_Main.insertNewArmor(0, msg.armorType, msg.armorPalettes);

      // Get search the database to determine whether or not the account has admin privileges
      int adminFlag = DB_Main.getUsrAdminFlag(accountId);

      // Assign the new armor to the user
      // msg.userInfo.armorId = armorId;

      // Then insert the User in the database
      userInfo.facingDirection = (int) Direction.West;
      int userId = DB_Main.createUser(accountId, adminFlag, userInfo, area);
      DB_Main.insertIntoJobs(userId);

      // Update the armor as belonging to this new user id, and equip it
      DB_Main.setItemOwner(userId, armorId);
      DB_Main.setArmorId(userId, armorId);

      // Create their starting ship and equip it
      ShipInfo shipInfo = DB_Main.createStartingShip(userId);
      DB_Main.setCurrentShip(userId, shipInfo.shipId);

      // Grab the latest version of their user objects
      UserObjects userObjects = DB_Main.getUserObjects(userId);

      // Create the default ability
      int abilitySlotIndex = 0;
      foreach (int abilityId in AbilityManager.STARTING_ABILITIES) {
         BasicAbilityData startingAbility = AbilityManager.getAbility(abilityId, AbilityType.Standard);
         AbilitySQLData startingAbilitySQL = AbilitySQLData.TranslateBasicAbility(startingAbility);

         // Make sure the ability is equipped
         startingAbilitySQL.equipSlotIndex = abilitySlotIndex;

         // Add the ability to the user
         DB_Main.updateAbilitiesData(userId, startingAbilitySQL);
         abilitySlotIndex++;
      }

      int assignedPoints = msg.perks.Sum(perk => perk.points);
      List<Perk> perks = msg.perks.ToList();

      // Make sure the player doesn't send an invalid number of points
      if (assignedPoints != CreationPerksGrid.AVAILABLE_POINTS) {
         perks = new List<Perk> { new Perk(0, CreationPerksGrid.AVAILABLE_POINTS) };
      }

      // Add the perks to the user
      DB_Main.addPerkPointsForUser(userId, perks);

      // Add the default starting items
      int slotNumber = 1;
      foreach (int itemTypeId in InventoryManager.STARTING_WEAPON_TYPE_IDS) {
         int itemId = DB_Main.insertNewWeapon(userId, itemTypeId, "");
         DB_Main.updateItemShortcut(userId, slotNumber, itemId);
         slotNumber++;
      }

      // Give some additional armor and weapons to test users
      /*if (true) {
         DB_Main.addGold(userId, 800);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_Steel, ColorType.SteelBlue, ColorType.SteelBlue);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Staff_Mage, ColorType.Teal, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Mace_Star, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Mace_Steel, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Lance_Steel, ColorType.SteelBlue, ColorType.SteelBlue);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_Rune, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_1, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_2, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_3, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_4, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_5, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_6, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_7, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Sword_8, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Gun_2, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Gun_3, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Gun_6, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewWeapon(userId, Weapon.Type.Gun_7, ColorType.SteelBlue, ColorType.Red);
         DB_Main.insertNewArmor(userId, Armor.Type.Casual, ColorType.None, ColorType.None);
         DB_Main.insertNewArmor(userId, Armor.Type.Plate, ColorType.SourceRed, ColorType.SourceRed);
         DB_Main.insertNewArmor(userId, Armor.Type.Wool, ColorType.White, ColorType.Brown);
         DB_Main.insertNewArmor(userId, Armor.Type.Strapped, ColorType.Brown, ColorType.White);
         DB_Main.insertNewArmor(userId, Armor.Type.Cloth, ColorType.Brown, ColorType.White);
         DB_Main.insertNewArmor(userId, Armor.Type.Formal, ColorType.Black, ColorType.White);
         DB_Main.insertNewArmor(userId, Armor.Type.Leather, ColorType.Brown, ColorType.Brown);
         DB_Main.insertNewArmor(userId, Armor.Type.Posh, ColorType.Teal, ColorType.White);
         DB_Main.insertNewArmor(userId, Armor.Type.Sash, ColorType.Blue, ColorType.White);
         DB_Main.insertNewArmor(userId, Armor.Type.Steel, ColorType.SteelGrey, ColorType.SteelGrey);
         DB_Main.insertNewArmor(userId, Armor.Type.Tunic, ColorType.Blue, ColorType.White);
      }*/

      // Switch back to the Unity Thread to let the client know the result
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         if (userId > 0) {
            // Keep track of the user ID that's been authenticated for this connection
            MyNetworkManager.noteUserIdForConnection(userId, steamUserId, conn);

            // Storing login info
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               if (conn != null) {
                  DB_Main.storeLoginInfo(userInfo.userId, accountId, userInfo.username, conn.address, msg.machineIdentifier, msg.deploymentId);
               }
            });

            // Now tell the client to move forward with the login process
            LogInCompleteMessage loginCompleteMsg = new LogInCompleteMessage(Global.netId, (Direction) userInfo.facingDirection,
               userObjects.accountEmail, userObjects.accountCreationTime);
            conn.Send(loginCompleteMsg);
         } else {
            sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
            return;
         }
      });
   }

   #region Private Variables

   #endregion
}
