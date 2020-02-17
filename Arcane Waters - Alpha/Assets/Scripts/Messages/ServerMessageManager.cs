using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ServerMessageManager : MonoBehaviour {
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

      // Make sure they have the required game version
      if (logInUserMessage.clientGameVersion < minClientGameVersion) {
         string msg = string.Format("Refusing login for {0}, client version {1}", logInUserMessage.accountName, logInUserMessage.clientGameVersion);
         D.debug(msg);
         sendError(ErrorMessage.Type.ClientOutdated, conn.connectionId);
         return;
      }

      // Grab the user info from the database for the relevant account ID
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<UserInfo> users = new List<UserInfo>();
         List<Armor> armorList = new List<Armor>();
         List<Weapon> weaponList = new List<Weapon>();

         // Look up the account ID corresponding to the provided account name and password
         string salt = Util.createSalt("arcane");
         string hashedPassword = Util.hashPassword(salt, logInUserMessage.accountPassword);
         int accountId = DB_Main.getAccountId(logInUserMessage.accountName, hashedPassword);

         // Get the user objects for the selected user ID
         UserObjects userObjects = null;
         if (selectedUserId > 0) {
            userObjects = DB_Main.getUserObjects(selectedUserId);
         }

         if (accountId > 0) {
            MyNetworkManager.noteAccountIdForConnection(accountId, conn);

            users = DB_Main.getUsersForAccount(accountId, selectedUserId);
            armorList = DB_Main.getArmorForAccount(accountId, selectedUserId);
            weaponList = DB_Main.getWeaponsForAccount(accountId, selectedUserId);
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // If there was a valid account ID and a specified user ID, tell the client we authenticated them
            if (accountId > 0 && logInUserMessage.selectedUserId > 0 && users.Count == 1) {
               // Keep track of the user ID that's been authenticated for this connection
               MyNetworkManager.noteUserIdForConnection(logInUserMessage.selectedUserId, conn);

               // Now tell the client to move forward with the login process
               LogInCompleteMessage msg = new LogInCompleteMessage(Global.netId, (Direction) users[0].facingDirection,
                  userObjects.accountEmail, userObjects.accountCreationTime);
               conn.Send(msg);

            } else if (accountId > 0 && logInUserMessage.selectedUserId == 0) {
               // We have to deal with these separately because of a bug in Unity
               int[] armorColors1 = new int[armorList.Count];
               int[] armorColors2 = new int[armorList.Count];
               for (int i = 0; i < armorList.Count; i++) {
                  armorColors1[i] = (int) armorList[i].color1;
                  armorColors2[i] = (int) armorList[i].color2;
               }

               // If there was an account ID but not user ID, send the info on all of their characters for display on the Character screen
               CharacterListMessage msg = new CharacterListMessage(Global.netId, users.ToArray(), armorList.ToArray(), weaponList.ToArray(), armorColors1, armorColors2);
               conn.Send(msg);

            } else {
               sendError(ErrorMessage.Type.FailedUserOrPass, conn.connectionId);
            }
         });
      });
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
      Area area = AreaManager.self.getArea(Area.HOUSE);

      // Look up the Spawn position for the map associated with that area
      Spawn spawn = SpawnManager.self.getSpawn(SpawnManager.self.startingLocationAreaKey, SpawnManager.self.startingLocationSpawnKey);
      userInfo.localPos = spawn.transform.position;

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
            BKG_finishCreatingUser(msg, accountId, userInfo, conn, area);
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
         DB_Main.deleteAllFromTable(accountId, msg.userId, "ships");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "items");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "crops");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "silo");
         DB_Main.deleteAllFromTable(accountId, msg.userId, "tutorial");
         DB_Main.deleteUser(accountId, msg.userId);

         // Send confirmation to the client, so that they can request their user list again
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            sendConfirmation(ConfirmMessage.Type.DeletedUser, conn.connectionId);
         });
      });
   }

   [ServerOnly]
   protected static void BKG_finishCreatingUser (CreateUserMessage msg, int accountId, UserInfo userInfo, NetworkConnection conn, Area area) {
      // Need to create their Armor first
      int armorId = DB_Main.insertNewArmor(0, msg.armorType, msg.armorColor1, msg.armorColor2);

      // Get search the database to determine whether or not the account has admin privileges
      int adminFlag = DB_Main.getUsrAdminFlag(accountId);

      // Assign the new armor to the user
      // msg.userInfo.armorId = armorId;

      // Then insert the User in the database
      userInfo.facingDirection = (int) Direction.West;
      int userId = DB_Main.createUser(accountId, adminFlag, userInfo, area);
      DB_Main.insertIntoJobs(userId);

      // Update the armor as belonging to this new user id
      DB_Main.setItemOwner(userId, armorId);

      // Grab the latest version of their user objects
      UserObjects userObjects = DB_Main.getUserObjects(userId);

      // Create the default ability
      BasicAbilityData startingAbility = AbilityManager.getAbility(AbilityManager.STARTING_ABILITY_ID, AbilityType.Standard);
      AbilitySQLData startingAbilitySQL = AbilitySQLData.TranslateBasicAbility(startingAbility);

      // Make sure the ability is equipped
      startingAbilitySQL.equipSlotIndex = 0;

      // Add the ability to the user
      DB_Main.updateAbilitiesData(userId, startingAbilitySQL);

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
            MyNetworkManager.noteUserIdForConnection(userId, conn);

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
