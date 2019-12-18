using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class ClientMessageManager : MonoBehaviour {
   #region Public Variables

   #endregion

   public static void On_Redirect (NetworkConnection conn, RedirectMessage msg) {
      D.debug("Received redirect message to " + msg.newIpAddress + ":" + msg.newPort);

      // If the address and port are the same, we just send another login request
      if (msg.newIpAddress == NetworkManager.singleton.networkAddress && msg.newPort == MyNetworkManager.self.telepathy.port) {
         ClientManager.sendAccountNameAndUserId();
      } else {
         NetworkManager.singleton.StartCoroutine(CO_Reconnect(msg));
      }
   }

   protected static IEnumerator CO_Reconnect (RedirectMessage msg) {
      Global.isRedirecting = true;
      bool wasHost = MyNetworkManager.isHost;

      // Disconnect from the current server
      if (wasHost) {
         MyNetworkManager.self.StopHost();
      } else {
         MyNetworkManager.self.StopClient();
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

   public static void On_ErrorMessage (NetworkConnection conn, ErrorMessage msg) {
      if (msg.errorType == ErrorMessage.Type.None) {
         return;
      }

      if (!Util.isEmpty(msg.customMessage)) {
         PanelManager.self.noticeScreen.show(msg.customMessage);
         return;
      }

      switch (msg.errorType) {
         case ErrorMessage.Type.FailedUserOrPass:
         case ErrorMessage.Type.AlreadyOnline:
         case ErrorMessage.Type.Banned:
         case ErrorMessage.Type.ClientOutdated:
            TitleScreen.self.displayError(msg.errorType);
            return;
         case ErrorMessage.Type.NameTaken:
            PanelManager.self.noticeScreen.show("The selected username is already taken.");
            return;
         case ErrorMessage.Type.InvalidUsername:
            PanelManager.self.noticeScreen.show("That is not a valid username.");
            return;
         case ErrorMessage.Type.NoCropsOfThatType:
            PanelManager.self.noticeScreen.show("You don't have any of those crops to sell!");
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
            PanelManager.self.popPanel();

            // Get updated info
            Global.player.rpc.Cmd_RequestGuildInfoFromServer();

            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);
            return;
         case ConfirmMessage.Type.UsedHairDye:
         case ConfirmMessage.Type.UsedShipSkin:
         case ConfirmMessage.Type.UsedHaircut:
            string chatMessage = "You have dyed your hair!";

            if (msg.confirmType == ConfirmMessage.Type.UsedShipSkin) {
               chatMessage = "You have repainted your ship!";
            } else if (msg.confirmType == ConfirmMessage.Type.UsedHaircut) {
               chatMessage = "You have cut your hair!";
            }

            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(chatMessage, msg.timestamp, ChatInfo.Type.System);

            // Get a reference to the Inventory Panel
            InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

            // Refresh the panel
            panel.refreshPanel();

            // Because the character appearance changed, let's just close the panel for now
            PanelManager.self.popPanel();

            return;
         case ConfirmMessage.Type.General:
         case ConfirmMessage.Type.StoreItemBought:
            // Add the confirmation message in the chat panel
            ChatManager.self.addChat(msg.customMessage, msg.timestamp, ChatInfo.Type.System);
            return;
         case ConfirmMessage.Type.ShipBought:
            // Hide the ship panel
            PanelManager.self.popPanel();

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
            friendListPanel.refreshPanel();

            // Show a confirmation panel if there is a custom message to display
            if (!"".Equals(msg.customMessage)) {
               PanelManager.self.noticeScreen.show(msg.customMessage);
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
      // Activate the Character screen camera
      Util.activateVirtualCamera(CharacterScreen.self.virtualCam);

      // Pass the info along to the Character screen
      CharacterScreen.self.initializeScreen(msg.userArray, msg.armorArray, msg.weaponArray, msg.armorColors1, msg.armorColors2);
   }

   public static void On_LoginIsComplete (NetworkConnection conn, LogInCompleteMessage msg) {
      // Set our initial facing direction
      Global.initialFacingDirection = msg.initialFacingDirection;
      Global.lastAccountEmail = msg.accountEmail;
      Global.lastAccountCreationTime = System.DateTime.FromBinary(msg.accountCreationTime);

      // Send the account name and password to the server
      /*StringMessage msg = new StringMessage(
         Global.lastUsedAccountName + ":" + Global.lastUserAccountPassword + ":" +
         Global.currentlySelectedUserId + ":" + Global.GAME_VERSION
      );*/

      ClientScene.AddPlayer(conn);
   }

   public static void On_FailedToConnectToServer (NetworkConnection conn, DisconnectMessage msg) {
      // Ignore this message if we're in the  middle of a redirect
      if (Global.isRedirecting) {
         return;
      }

      PanelManager.self.noticeScreen.show("Could not connect to server.");
      MyNetworkManager.self.StopClient();
   }

   public static void On_Inventory (NetworkConnection conn, InventoryMessage msg) {
      if (PanelManager.self.selectedPanel == Panel.Type.Craft) {
         CraftingPanel craftPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);
         if (!craftPanel.isShowing()) {
            // Make sure the inventory panel is showing
            PanelManager.self.pushPanel(Panel.Type.Craft);
         }
         // Update the Crafting Panel with the items we received from the server
         craftPanel.receiveItemsFromServer(msg.userObjects, msg.pageNumber, msg.gold, msg.gems, msg.totalItemCount, msg.equippedArmorId, msg.equippedWeaponId, msg.itemArray);
      }
   }

   public static void On_Store (NetworkConnection conn, StoreMessage msg) {
      // Show the Store panel after updating the gems on hand
      StoreScreen store = (StoreScreen) PanelManager.self.get(Panel.Type.Store);
      store.showPanel(msg.userObjects, msg.goldOnHand, msg.gemsOnHand);
   }

   public static void On_Equip (NetworkConnection conn, EquipMessage msg) {
      // Get a reference to the Inventory Panel
      InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

      // Refresh the panel
      panel.refreshPanel();
   }

   #region Private Variables

   #endregion
}
