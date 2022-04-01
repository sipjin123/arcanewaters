﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;
using System;
using System.Globalization;
using System.Threading;
using Steamworks;
using SteamLoginSystem;

public class ClientMessageManager : MonoBehaviour {
   #region Public Variables

   // If the steam state has been logged in the playerlogs
   public static bool steamStateLogged = false;

   #endregion

   private void Awake () {
      _self = this;
   }

   public void Start () {
      // Set a default culture so we avoid potential deserialization bugs
      CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
      Thread.CurrentThread.CurrentCulture = culture;
      Thread.CurrentThread.CurrentUICulture = culture;
   }

   public static void On_Redirect (NetworkConnection conn, RedirectMessage msg) {
      D.debug($"Received redirect message for {msg.newIpAddress}:{msg.newPort}, currently connected port {MyNetworkManager.self.telepathy.port}");

      // If the address and port are the same, we just send another login request
      if (Util.isSameIpAddress(msg.newIpAddress, NetworkManager.singleton.networkAddress) && msg.newPort == MyNetworkManager.self.telepathy.port) {
         ClientManager.sendAccountNameAndUserId();
      } else {
         _self.StartCoroutine(CO_Reconnect(msg));
      }
   }

   protected static IEnumerator CO_Reconnect (RedirectMessage msg) {
      Global.isRedirecting = true;
      bool wasHost = MyNetworkManager.isHost;

      D.debug($"CO_Reconnect was called, so setting Global.isRedirecting to true, and wasHost: {wasHost}");

      // Disconnect from the current server
      if (wasHost) {
         MyNetworkManager.self.StopHost();
      } else {
         if (NetworkClient.active) {
            MyNetworkManager.self.StopClient();
         }
      }

      yield return new WaitForSeconds(.05f);

      // Plug the new address and port in
      MyNetworkManager.self.networkAddress = msg.newIpAddress;
      MyNetworkManager.self.telepathy.port = (ushort) msg.newPort;

      // Connect to the new address
      if (wasHost) {
         MyNetworkManager.self.StartHost();
      } else {
         MyNetworkManager.self.StartClient();
      }
   }

   public static void On_CharacterCreationValid (NetworkConnection conn, CharacterCreationValidMessage msg) {
      CharacterCreationPanel.self.onCharacterCreationValid();
   }

   public static void On_ErrorMessage (NetworkConnection conn, ErrorMessage msg) {
      if (msg.errorType == ErrorMessage.Type.None) {
         return;
      }

      switch (msg.errorType) {
         case ErrorMessage.Type.SteamWebOffline:
         case ErrorMessage.Type.FailedUserOrPass:
         case ErrorMessage.Type.AlreadyOnline:
         case ErrorMessage.Type.Banned:
         case ErrorMessage.Type.ClientOutdated:
         case ErrorMessage.Type.Kicked:
            PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.Login, LoadingScreen.LoadingType.CharacterCreation);
            TitleScreen.self.displayError(msg.errorType, msg);
            return;
         case ErrorMessage.Type.NameTaken:
            PanelManager.self.noticeScreen.show("The selected username is already taken.");
            CharacterCreationPanel.self.onCharacterCreationFailed();
            return;
         case ErrorMessage.Type.InvalidUsername:
            if (string.IsNullOrWhiteSpace(msg.customMessage)) {
               PanelManager.self.noticeScreen.show("That is not a valid username.");
            } else {
               PanelManager.self.noticeScreen.show(msg.customMessage);
            }
            CharacterCreationPanel.self.onCharacterCreationFailed();
            return;
         case ErrorMessage.Type.NoCropsOfThatType:
            PanelManager.self.noticeScreen.show("You don't have any of those crops to sell!");
            return;
         case ErrorMessage.Type.NotEnoughGold:
            PanelManager.self.noticeScreen.show(msg.customMessage);
            return;
         case ErrorMessage.Type.PvpJoinError:
            PanelManager.self.noticeScreen.show(msg.customMessage);
            return;
         case ErrorMessage.Type.MailInvalidUserName:
            if (string.IsNullOrWhiteSpace(msg.customMessage)) {
               PanelManager.self.noticeScreen.show("That is not a valid username.");
            } else {
               PanelManager.self.noticeScreen.show(msg.customMessage);
            }
            return;
         case ErrorMessage.Type.Disconnected:
         case ErrorMessage.Type.ServerDown:
         case ErrorMessage.Type.NotEnoughGems:
         case ErrorMessage.Type.Misc:
         case ErrorMessage.Type.ServerOffline:
            PanelManager.self.noticeScreen.show(msg.customMessage);
            return;
         case ErrorMessage.Type.PlayerCountLimitReached:
         case ErrorMessage.Type.UserNotAdmin:
            PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.Login);
            TitleScreen.self.displayError(msg.errorType, msg);
            return;
         case ErrorMessage.Type.PurchaseError:
            PanelManager.self.noticeScreen.show(msg.customMessage);
            return;
         case ErrorMessage.Type.Generic:
            PanelManager.self.noticeScreen.show(msg.customMessage);
            return;
         case ErrorMessage.Type.UseItemFailed:
            PanelManager.self.noticeScreen.show(msg.customMessage);

            if (InventoryPanel.self != null && InventoryPanel.self.isShowing()) {
               InventoryPanel.self.refreshPanel();
            }

            if (StoreScreen.self != null && StoreScreen.self.isShowing()) {
               StoreScreen.self.refreshPanel();
            }

            return;
         case ErrorMessage.Type.StoreItemPurchaseFailed:
            PanelManager.self.noticeScreen.show(msg.customMessage);

            if (StoreScreen.self.isShowing()) {
               StoreScreen.self.refreshPanel();
            }
            return;
         case ErrorMessage.Type.ItemIsSoulBound:
            if (string.IsNullOrWhiteSpace(msg.customMessage)) {
               PanelManager.self.noticeScreen.show("The item is soul bound, and it can't be transferred.");
            } else {
               PanelManager.self.noticeScreen.show(msg.customMessage);
            }
            return;
         /*case ErrorMessage.Type.NoGoldForCargo:
         case ErrorMessage.Type.OutOfCargoSpace:
         case ErrorMessage.Type.PortOutOfCargo:
         case ErrorMessage.Type.PlayerNotEnoughCargo:
         case ErrorMessage.Type.TooManyCargoTypes:
         case ErrorMessage.Type.NoTradePermits:
            BuySellPanel.self.showErrorMessage(msg.errorType);
            return;
         case ErrorMessage.Type.UsernameNotFound:
            ChatManager.self.addChat(msg.customMessage, ChatInfo.Type.Error);
            return;
         case ErrorMessage.Type.ServerStartingUp:
            D.debug("Server is still starting up!");

            // Show the message in a different place, based on which panel is currently open
            if (LoginScreen.self.gameObject.activeSelf) {
               LoginScreen.self.displayError(msg.errorType);
            } else if (PanelManager.self.getModalPanel(Panel.Type.CharSelect).isShowing()) {
               // Redisplay the screen to re-enable the buttons
               PanelManager.self.getModalPanel(Panel.Type.CharSelect).show();
               PanelManager.self.noticeScreen.show("The server is still starting up!");
            }
            return;*/
         default:
            D.warning("Unhandled error type: " + msg.errorType);
            return;
      }
   }

   public static void On_ConfirmMessage (NetworkConnection conn, ConfirmMessage msg) {
      switch (msg.confirmType) {
         case ConfirmMessage.Type.DeletedUser:
            // Hide the confirm panel
            PanelManager.self.confirmScreen.hide();

            // Request a new character list
            ClientManager.sendAccountNameAndUserId();

            return;
         case ConfirmMessage.Type.CreatedGuild:
            // Close the panel
            ((GuildPanel) PanelManager.self.get(Panel.Type.Guild)).guildCreatePanel.hide();

            // Get updated info
            Global.player.rpc.Cmd_RequestGuildInfoFromServer();

            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);
            return;
         case ConfirmMessage.Type.EditGuildRanks:
            // Close the panel
            ((GuildPanel) PanelManager.self.get(Panel.Type.Guild)).guildRanksPanel.hide();

            // Get updated info
            Global.player.rpc.Cmd_RequestGuildInfoFromServer();

            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);

            return;
         case ConfirmMessage.Type.GuildActionLocal:
            // Show notice screen
            PanelManager.self.noticeScreen.show(msg.customMessage);

            // Get updated info
            Global.player.rpc.Cmd_RequestGuildInfoFromServer();

            return;
         case ConfirmMessage.Type.GuildActionGlobal:
            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);

            return;
         case ConfirmMessage.Type.GuildActionUpdate:
            // Get updated info
            Global.player.rpc.Cmd_RequestGuildInfoFromServer();

            return;
         case ConfirmMessage.Type.UsedConsumable:
         case ConfirmMessage.Type.UsedHairDye:
         case ConfirmMessage.Type.UsedShipSkin:
         case ConfirmMessage.Type.UsedArmorDye:
         case ConfirmMessage.Type.UsedHatDye:
         case ConfirmMessage.Type.UsedWeaponDye:
         case ConfirmMessage.Type.UsedHaircut:
            string chatMessage = msg.customMessage;

            if (msg.confirmType == ConfirmMessage.Type.UsedShipSkin) {
               chatMessage = "You have repainted your ship!";
            } else if (msg.confirmType == ConfirmMessage.Type.UsedHaircut) {
               chatMessage = "You have cut your hair!";
            } else if (msg.confirmType == ConfirmMessage.Type.UsedHairDye) {
               chatMessage = "You have dyed your hair!";
            } else if (msg.confirmType == ConfirmMessage.Type.UsedArmorDye) {
               chatMessage = "You have dyed your armor!";
            } else if (msg.confirmType == ConfirmMessage.Type.UsedConsumable) {
               Global.player.rpc.Cmd_FetchPerkPointsForUser();
            } else if (msg.confirmType == ConfirmMessage.Type.UsedHatDye) {
               chatMessage = "You have dyed your hat!";
            } else if (msg.confirmType == ConfirmMessage.Type.UsedWeaponDye) {
               chatMessage = "You have dyed your weapon!";
            }

            // Show the confirmation message in the notice screen
            PanelManager.self.noticeScreen.show(chatMessage);

            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(chatMessage, msg.timestamp, ChatInfo.Type.System);

            // Refresh the panel
            if (InventoryPanel.self != null && InventoryPanel.self.isShowing()) {
               InventoryPanel.self.refreshPanel();
            }

            if (StoreScreen.self != null && StoreScreen.self.isShowing()) {
               StoreScreen.self.refreshPanel();
            }

            return;
         case ConfirmMessage.Type.GeneralPopup:
            PanelManager.self.noticeScreen.show(msg.customMessage);
            break;
         case ConfirmMessage.Type.General:
            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);
            return;
         case ConfirmMessage.Type.StoreItemBought:
            // Show the confirmation message in the notice screen
            PanelManager.self.noticeScreen.show(msg.customMessage);

            if (StoreScreen.self != null && StoreScreen.self.isShowing()) {
               StoreScreen.self.refreshPanel();
            }

            return;
         case ConfirmMessage.Type.ShipBought:
            // Hide the ship panel
            PanelManager.self.unlinkPanel();

            // Show a confirmation panel
            PanelManager.self.noticeScreen.show("You purchased a new ship!");

            return;

         case ConfirmMessage.Type.ConfirmDeleteItem:
            // Hide the confirm panel
            PanelManager.self.confirmScreen.hide();

            // Get a reference to the Inventory Panel
            InventoryPanel panel2 = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

            // Refresh the panel
            panel2.refreshPanel();
            return;

         case ConfirmMessage.Type.FriendshipInvitationSent:
         case ConfirmMessage.Type.FriendshipInvitationAccepted:
         case ConfirmMessage.Type.FriendshipDeleted:
            // Hide the confirm panel
            PanelManager.self.confirmScreen.hide();

            // Get a reference to the Friend List panel
            FriendListPanel friendListPanel = (FriendListPanel) PanelManager.self.get(Panel.Type.FriendList);

            // Refresh the panel
            if (friendListPanel.isShowing()) {
               friendListPanel.refreshPanel(msg.confirmType == ConfirmMessage.Type.FriendshipInvitationSent);
            }

            // Add the confirmation message in the chat panel
            if (!"".Equals(msg.customMessage)) {
               ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);
            }
            return;

         case ConfirmMessage.Type.MailSent:
            // Hide the confirm panel
            PanelManager.self.confirmScreen.hide();

            // Get a reference to the Mail Panel
            MailPanel mailPanel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

            // Update the panel after the successful operation
            mailPanel.confirmMailSent();

            // Show a confirmation panel if there is a custom message to display
            if (!"".Equals(msg.customMessage)) {
               PanelManager.self.noticeScreen.show(msg.customMessage);
            }
            return;

         case ConfirmMessage.Type.MailDeleted:
            // Hide the confirm panel
            PanelManager.self.confirmScreen.hide();

            // Get a reference to the Mail Panel
            MailPanel mailPanel2 = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

            // Refresh the panel
            mailPanel2.refreshMailList();
            mailPanel2.clearSelectedMail();
            return;

         case ConfirmMessage.Type.ItemsAddedToInventory:
            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);

            // Get a reference to the Inventory Panel
            InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

            // If the panel is visible, refresh it
            if (inventoryPanel.isShowing()) {
               inventoryPanel.refreshPanel();
            }
            return;

         case ConfirmMessage.Type.BugReport:
            // Add the confirmation message in the chat panel
            //BugReportManager.self.sendBugReportScreenshotToServer(long.Parse(msg.customMessage));
            ChatManager.self.addChat("Your bug has been successfully submitted!", msg.timestamp, ChatInfo.Type.System);
            return;

         case ConfirmMessage.Type.ModifiedOwnAuction:
            AuctionPanel auctionPanel = (AuctionPanel) PanelManager.self.get(Panel.Type.Auction);
            auctionPanel.auctionInfoPanel.hide();
            auctionPanel.displayMyAuctions();

            // Show a confirmation panel
            PanelManager.self.noticeScreen.show(msg.customMessage);

            return;

         case ConfirmMessage.Type.BidOnAuction:
            AuctionPanel auctionPanel2 = (AuctionPanel) PanelManager.self.get(Panel.Type.Auction);
            auctionPanel2.auctionInfoPanel.hide();
            auctionPanel2.displayAllAuctions();

            // Show a confirmation panel
            PanelManager.self.noticeScreen.show(msg.customMessage);

            return;

         case ConfirmMessage.Type.SoldAuctionItem:
         case ConfirmMessage.Type.ReturnAuctionItem:
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);
            return;

         case ConfirmMessage.Type.CorrectClientVersion:
            TitleScreen.self.continueAfterCheckingClientVersion();
            return;
         case ConfirmMessage.Type.ItemSoulBound:
            if (string.IsNullOrWhiteSpace(msg.customMessage)) {
               PanelManager.self.noticeScreen.show("The item is now soul bound!");
            } else {
               PanelManager.self.noticeScreen.show(msg.customMessage);
            }
            return;
         case ConfirmMessage.Type.RestoredUser:
            // Hide the confirm panel
            PanelManager.self.confirmScreen.hide();

            // Request a new character list
            ClientManager.sendAccountNameAndUserId();
            return;
            /*case ConfirmMessage.Type.SeaWarp:
               // Pixelate the screen
               // PixelFadeEffect.self.fadeOut();

               return;
            case ConfirmMessage.Type.BoughtCargo:
               // Close the Buy Panel
               PanelManager.self.popPanel();
               return;
            case ConfirmMessage.Type.BugReport:
               // Add the confirmation message in the chat panel
               ChatManager.self.addChat("Bug report submitted.", msg.timestamp, ChatInfo.Type.System);
               return;
            case ConfirmMessage.Type.AddGold:
               // Add the confirmation message in the chat panel
               ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);
               return;
            case ConfirmMessage.Type.ContainerOpened:
               // Add the confirmation message in the chat panel
               ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);

               // Play a Sound
               if (!msg.customMessage.ToLower().Contains("empty")) {
                  SoundManager.play2DClip(SoundManager.Type.Container_Opened);
               }

               return;*/
      }
   }

   public static void On_CharacterList (NetworkConnection conn, CharacterListMessage msg) {
      try {
         D.adminLog("Received LoginMessage: " + msg.userArray.Length + " : "
            + msg.armorArray.Length + " : " + msg.weaponArray.Length + " : "
            + msg.hatArray.Length + " : " + msg.armorPalettes.Length + " : "
            + msg.equipmentIds.Length + " : " + msg.spriteIds.Length, D.ADMIN_LOG_TYPE.Client_AccountLogin);
      } catch {
         D.adminLog("Received LoginMessage: BLANK", D.ADMIN_LOG_TYPE.Client_AccountLogin);
      }
      
      // Remove fade and loading effects
      CameraFader.self.setLoadingIndicatorVisibility(false);
      CameraFader.self.fadeOut(0.5f);

      XmlVersionManagerClient.self.initializeClient();

      // Delete local map files if user has too many
      MapCache.pruneExcessMaps();

      // Clear the chat
      ChatPanel.self.clearChat();

      // Activate the Character screen camera
      Util.activateVirtualCamera(CharacterScreen.self.virtualCam);

      // Pass the info along to the Character screen
      CharacterScreen.self.initializeScreen(msg.userArray, msg.deletionStatusArray, msg.armorArray, msg.weaponArray, msg.hatArray, msg.armorPalettes, msg.equipmentIds, msg.spriteIds);
   }

   public static void On_LoginIsComplete (NetworkConnection conn, LogInCompleteMessage msg) {
      _self.StartCoroutine(CO_OnLoginIsComplete(conn, msg));
   }

   private static IEnumerator CO_OnLoginIsComplete (NetworkConnection conn, LogInCompleteMessage msg) {
      // Wait for any existing player to be destroyed
      while (ClientScene.localPlayer != null) {
         yield return null;
      }

      // Wait for any fades to complete before processing anything
      while (Global.isScreenTransitioning) {
         yield return null;
      }

      if (Global.isFirstLogin) {
         // Log the login the first time the player character spawns
         D.debug("-- Login is completed --");
      }

      // Set our initial facing direction
      Global.initialFacingDirection = msg.initialFacingDirection;
      Global.lastAccountEmail = msg.accountEmail;
      Global.lastAccountCreationTime = System.DateTime.FromBinary(msg.accountCreationTime);
      Global.currentlySelectedUserId = msg.userId;
      // We have already logged in successfully for the first time
      Global.isFirstLogin = false;

      if (msg.loginMessage != "") {
         ChatPanel.self.addChatInfo(new ChatInfo(0, msg.loginMessage, System.DateTime.Now, ChatInfo.Type.System));
      }

      // Send the account name and password to the server
      /*StringMessage msg = new StringMessage(
         Global.lastUsedAccountName + ":" + Global.lastUserAccountPassword + ":" +
         Global.currentlySelectedUserId + ":" + Global.GAME_VERSION
      );*/

      if (!steamStateLogged) {
         steamStateLogged = true;
         D.debug("BUILD_LOG :: Build Type:{" + SteamLoginManager.getSteamState() + "} : DeploymentId:{" + Util.getDeploymentId() + "} : BuildType:{" + Util.getBranchType() + "} : Distribution:{" + Util.getDistributionType() + "}");
      }

      ClientScene.AddPlayer(conn);

      TitleScreen.self.hideLoginPanels();
      PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.Login);
   }

   public static void On_FailedToConnectToServer (NetworkConnection conn, DisconnectMessage msg) {
      D.debug($"On_FailedToConnectToServer called, Global.isRedirecting: {Global.isRedirecting}");

      // Ignore this message if we're in the  middle of a redirect
      if (Global.isRedirecting) {
         return;
      }

      PanelManager.self.hideAllPanels();
      PanelManager.self.noticeScreen.show("Could not connect to server.");
      Util.stopHostAndReturnToTitleScreen();
      PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.Login);
      TitleScreen.self.showLoginPanels();
   }

   public static void On_Store (NetworkConnection conn, StoreMessage msg) {
      StoreScreen store = (StoreScreen)PanelManager.self.get(Panel.Type.Store);
      Global.userObjects = msg.userObjects;
      store.showPanel(msg.goldOnHand, msg.gemsOnHand);
   }

   public static void On_ClientDisconnected () {
      D.debug($"On_ClientDisconnected was triggered, Global.isRedirecting is set to: {Global.isRedirecting}");

      // Ignore this message if we're in the  middle of a redirect
      if (Global.isRedirecting) {
         return;
      }

      PanelManager.self.hideAllPanels();
      PanelManager.self.noticeScreen.show("Could not connect to server.");
      Util.stopHostAndReturnToTitleScreen();
      PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.Login);
      TitleScreen.self.showLoginPanels();
   }

   #region Private Variables

   // A reference to the single instance of this object
   private static ClientMessageManager _self;

   #endregion
}
