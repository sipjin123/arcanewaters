using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using Crosstales.BWF.Manager;
using System;

public class RPCManager : NetworkBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      _player = GetComponent<NetEntity>();
   }

   void Start () {
      // Initialized Inventory Cache
      if (_player.netIdent.isLocalPlayer) {
         InventoryCacheManager.self.fetchInventory();
      }
   }

   [ClientRpc]
   protected void Rpc_UpdateHair (HairLayer.Type newHairType, ColorType newHairColor1, ColorType newHairColor2) {
      if (_player is BodyEntity) {
         BodyEntity body = (BodyEntity) _player;
         body.updateHair(newHairType, newHairColor1, newHairColor2);

         // If the Inventory panel is showing, update it
         InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);
         if (inventoryPanel.isShowing() && _player.userId == Global.player.userId) {
            // inventoryPanel.previewPanel.charStack.setHair(newHairType);
            // inventoryPanel.previewPanel.charStack.setHairColor(Global.player.gender, newHairColor1, newHairColor2);
         }
      }
   }

   [ClientRpc]
   protected void Rpc_UpdateShipSkin (Ship.SkinType newSkinType) {
      if (_player is ShipEntity) {
         ShipEntity ship = (ShipEntity) _player;
         ship.updateSkin(newSkinType);
      }
   }

   [TargetRpc]
   public void Target_NPCPanelComplete (NetworkConnection connection, int npcLevel, int npcID) {
      NPC npc = NPCManager.self.getNPC(npcID);
      NPCPanel npcPanel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);
      npcPanel.npc = npc;
      PanelManager.self.pushIfNotShowing(npcPanel.type);

      // Send data to panel
      npcPanel.readyNPCPanel(npcLevel);

      // Call out quest options
      npcPanel.setClickableQuestOptions();
   }

   [TargetRpc]
   public void Target_GetNPCInfo (NetworkConnection connection, int npcID, int userID, int questType, int npcLevel, int npcQuestIndex, int npcProgress) {
      NPC npc = NPCManager.self.getNPC(npcID);
      NPCPanel npcPanel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);
      npcPanel.npc = npc;

      npcPanel.receiveIndividualNPCQuestData((QuestType) questType, npcQuestIndex, npcProgress);
   }

   [TargetRpc]
   public void Target_UpdateNPCRelation (NetworkConnection connection, int npcID, int npcRelation) {
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);
      panel.receiveNPCRelationDataFromServer(npcRelation);
   }

   [TargetRpc]
   public void Target_UpdateNPCQuestProgress (NetworkConnection connection, int npcID, int questProgress, int questIndex, string questType) {
      NPC npc = NPCManager.self.getNPC(npcID);
      QuestType typeOfQuest = (QuestType) Enum.Parse(typeof(QuestType), questType, true);
      switch (typeOfQuest) {
         case QuestType.Deliver:
            npc.npcData.npcQuestList[0].deliveryQuestList[questIndex].questState = (QuestState) questProgress;
            break;
         case QuestType.Hunt:
            npc.npcData.npcQuestList[0].huntQuestList[questIndex].questState = (QuestState) questProgress;
            break;
      }
   }

   [TargetRpc]
   public void Target_ReceiveItems (NetworkConnection connection, int gold, string[] itemArray) {
      AdventureShopScreen.self.updatePanelWithItems(gold, Item.unserializeAndCast(itemArray));
   }

   [TargetRpc]
   public void Target_ReceiveOffers (NetworkConnection connection, int gold, CropOffer[] offerArray, long lastCropRegenTime) {
      MerchantScreen.self.updatePanelWithOffers(gold, new List<CropOffer>(offerArray), lastCropRegenTime);
   }

   [TargetRpc]
   public void Target_ReceiveShipyard (NetworkConnection connection, int gold, string[] shipArray) {
      ShipyardScreen.self.updatePanelWithShips(gold, Util.unserialize<ShipInfo>(shipArray));
   }

   [TargetRpc]
   public void Target_ReceiveCharacterInfo (NetworkConnection connection, UserObjects userObjects, Stats stats, Jobs jobs, string guildName) {
      // Make sure the panel is showing
      CharacterInfoPanel panel = (CharacterInfoPanel) PanelManager.self.get(Panel.Type.CharacterInfo);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(Panel.Type.CharacterInfo);
      }

      // Update the Inventory Panel with the items we received from the server
      panel.receiveDataFromServer(userObjects, stats, jobs, guildName);
   }

   [TargetRpc]
   public void Target_ReceiveGuildInfo (NetworkConnection connection, GuildInfo info) {
      // Make sure the panel is showing
      GuildPanel panel = (GuildPanel) PanelManager.self.get(Panel.Type.Guild);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(Panel.Type.Guild);
      }

      // Update the Inventory Panel with the items we received from the server
      panel.receiveDataFromServer(info);
   }

   [TargetRpc]
   public void Target_ReceiveOptionsInfo (NetworkConnection connection, int instanceNumber, int instanceCount) {
      // Make sure the panel is showing
      OptionsPanel panel = (OptionsPanel) PanelManager.self.get(Panel.Type.Options);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(Panel.Type.Options);
      }

      // Update the Inventory Panel with the items we received from the server
      panel.receiveDataFromServer(instanceNumber, instanceCount);
   }

   [TargetRpc]
   public void Target_ReceiveShips (NetworkConnection connection, ShipInfo[] ships, int flagshipId) {
      List<ShipInfo> shipList = new List<ShipInfo>(ships);

      // Make sure the panel is showing
      FlagshipPanel panel = (FlagshipPanel) PanelManager.self.get(Panel.Type.Flagship);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass them along to the Flagship panel
      panel.updatePanelWithShips(shipList, flagshipId);
   }

   [TargetRpc]
   public void Target_ReceiveNewFlagshipId (NetworkConnection connection, int newFlagshipId) {
      // If we had the flagship panel open, we need to refresh
      FlagshipPanel panel = (FlagshipPanel) PanelManager.self.get(Panel.Type.Flagship);

      if (panel.isShowing()) {
         Global.player.rpc.Cmd_RequestShipsFromServer();
      }
   }

   [TargetRpc]
   public void Target_UpdateGems (NetworkConnection connection, int newGemsTotal) {
      StoreScreen store = (StoreScreen) PanelManager.self.get(Panel.Type.Store);
      store.gemsText.text = newGemsTotal + "";
   }

   [TargetRpc]
   public void Target_ReceiveGuildInvite (NetworkConnection connection, GuildInvite invite) {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => GuildManager.self.acceptInviteOnClient(invite));

      // Show a confirmation panel with the user name
      string message = "The player " + invite.senderName + " has invited you to join the guild " + invite.guildName + "!";
      PanelManager.self.confirmScreen.show(message);
   }

   [TargetRpc]
   public void Target_OpenChest (NetworkConnection connection, Item item, int chestId) {
      item = item.getCastItem();

      // Locate the Chest
      TreasureChest chest = null;
      foreach (TreasureChest existingChest in FindObjectsOfType<TreasureChest>()) {
         if (existingChest.id == chestId) {
            chest = existingChest;
         }
      }

      // Start the opening and burst animations
      chest.chestBurstAnimation.enabled = true;
      chest.chestOpeningAnimation.enabled = true;
      chest.StartCoroutine(chest.CO_CreatingFloatingIcon(item));

      // Play some sounds
      SoundManager.create3dSound("Door_open", Global.player.transform.position);
      SoundManager.create3dSound("tutorial_step", Global.player.transform.position);

      // Show a confirmation in chat
      string msg = string.Format("You found one <color=red>{0}</color>!", item.getName());
      ChatManager.self.addChat(msg, ChatInfo.Type.System);
   }

   [TargetRpc]
   public void Target_ReceiveClickableNPCRows (NetworkConnection connection, ClickableText.Type[] options, int npcId) {
      // Look up the NPC
      NPC npc = NPCManager.self.getNPC(npcId);

      // Show the panel with the specified options
      NPCPanel npcPanel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);
      npcPanel.npc = npc;
      npcPanel.setClickableRows(options.ToList());

      if (PanelManager.self.selectedPanel != Panel.Type.NPC_Panel) {
         PanelManager.self.pushIfNotShowing(npcPanel.type);
      }
   }

   [TargetRpc]
   public void Target_ReceiveNPCMessage (NetworkConnection connection, string response) {
      if (NPCPanel.self.isActiveAndEnabled) {
         // Set the new text message
         NPCPanel.self.SetMessage(response);
      }
   }

   [TargetRpc]
   public void Target_ReceiveTradeHistory (NetworkConnection connection, TradeHistoryInfo[] trades,
      int pageIndex, int totalTradeCount) {
      List<TradeHistoryInfo> tradeList = new List<TradeHistoryInfo>(trades);

      // Make sure the panel is showing
      TradeHistoryPanel panel = (TradeHistoryPanel) PanelManager.self.get(Panel.Type.TradeHistory);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass them along to the Trade History panel
      panel.updatePanelWithTrades(tradeList, pageIndex, totalTradeCount);
   }

   [Command]
   public void Cmd_BugReport (string subject, string message) {
      // We need a player object
      if (_player == null) {
         D.warning("Received bug report from sender with no PlayerController.");
         return;
      }

      // Pass things along to the Bug Report Manager to handle
      BugReportManager.self.storeBugReportOnServer(_player, subject, message);
   }

   [Command]
   public void Cmd_SendChat (string message, ChatInfo.Type chatType) {
      // Create a Chat Info for this message
      ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.UtcNow, chatType, _player.entityName, _player.userId);

      // Store this message in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         chatInfo.chatId = DB_Main.storeChatLog(_player.userId, message, chatInfo.chatTime, chatType);
      });

      // Replace bad words
      message = BadWordManager.ReplaceAll(message);

      // Pass this message along to the relevant people
      if (chatType == ChatInfo.Type.Local || chatType == ChatInfo.Type.Emote) {
         _player.Rpc_ChatWasSent(chatInfo.chatId, message, chatInfo.chatTime.ToBinary(), chatType);
      } else if (chatType == ChatInfo.Type.Global) {
         ServerNetwork.self.sendGlobalMessage(chatInfo);
      }
   }

   [Command]
   public void Cmd_RequestCharacterInfoFromServer (int userId) {
      if (userId == 0) {
         userId = _player.userId;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Stats stats = DB_Main.getStats(userId);
         Jobs jobs = DB_Main.getJobXP(userId);
         UserObjects userObjects = DB_Main.getUserObjects(userId);
         UserInfo userInfo = userObjects.userInfo;
         GuildInfo guildInfo = DB_Main.getGuildInfo(userInfo.guildId);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveCharacterInfo(_player.connectionToClient, userObjects, stats, jobs, guildInfo.guildName);
         });
      });
   }

   [Command]
   public void Cmd_RequestGuildInfoFromServer () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Default to an empty guild info
         GuildInfo info = new GuildInfo(_player.guildId);

         // Only look up info if they're in a guild
         if (_player.guildId > 0) {
            info = DB_Main.getGuildInfo(_player.guildId);
         }

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveGuildInfo(_player.connectionToClient, info);
         });
      });
   }

   [Command]
   public void Cmd_RequestOptionsInfoFromServer () {
      // Look up the info on the instances
      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      int totalInstances = InstanceManager.self.getInstanceCount(_player.areaType);

      // Send this along to the player
      _player.rpc.Target_ReceiveOptionsInfo(_player.connectionToClient, instance.numberInArea, totalInstances);
   }

   [Command]
   public void Cmd_RequestTradeHistoryInfoFromServer (int pageIndex, int itemsPerPage) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Enforce a reasonable max here
      if (itemsPerPage > 200) {
         D.warning("Requesting too many items per page.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Gets the number of items
         int totalTradeCount = DB_Main.getTradeHistoryCount(_player.userId);

         // Get the items from the database
         List<TradeHistoryInfo> tradeList = DB_Main.getTradeHistory(_player.userId, pageIndex, itemsPerPage);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveTradeHistory(_player.connectionToClient, tradeList.ToArray(), pageIndex, totalTradeCount);
         });
      });
   }

   [Command]
   public void Cmd_RequestItemsFromServer (int pageNumber, int itemsPerPage) {
      // Enforce a reasonable max here
      if (itemsPerPage > 200) {
         D.warning("Requesting too many items per page.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         UserInfo userInfo = userObjects.userInfo;
         int totalItemCount = DB_Main.getItemCount(_player.userId);

         // Get the items from the database
         List<Item> items = DB_Main.getItems(_player.userId, pageNumber, itemsPerPage);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            InventoryMessage inventoryMessage = new InventoryMessage(_player.netId, userObjects,
               pageNumber, userInfo.gold, userInfo.gems, totalItemCount, userInfo.armorId, userInfo.weaponId, items.ToArray());
            NetworkServer.SendToClientOfPlayer(_player.netIdent, inventoryMessage);
         });
      });
   }

   [Command]
   public void Cmd_RequestShipsFromServer () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         List<ShipInfo> ships = DB_Main.getShips(_player.userId, 1, 100);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveShips(_player.connectionToClient, ships.ToArray(), userObjects.shipInfo.shipId);
         });
      });
   }

   [Command]
   public void Cmd_RequestNewFlagship (int flagshipId) {
      requestNewFlagship(flagshipId);
   }

   [Command]
   public void Cmd_RequestStoreFromServer () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         UserInfo userInfo = userObjects.userInfo;

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            StoreMessage storeMessage = new StoreMessage(_player.netId, userObjects, userInfo.gold, userInfo.gems);
            NetworkServer.SendToClientOfPlayer(_player.netIdent, storeMessage);
         });
      });
   }

   [Command]
   public void Cmd_RequestSetWeaponId (int weaponId) {
      requestSetWeaponId(weaponId);
   }

   [Command]
   public void Cmd_RequestSetArmorId (int armorId) {
      requestSetArmorId(armorId);
   }

   [Command]
   public void Cmd_DeleteItem (int itemId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (itemId <= 0) {
         D.warning("Invalid item id: " + itemId);
         return;
      }

      // They might be in an island scene, or a ship scene
      BodyEntity body = _player.GetComponent<BodyEntity>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserInfo userInfo = DB_Main.getUserInfo(_player.userId);

         // Check if this is an equipped armor or weapon
         bool wasEquippedArmor = (itemId == userInfo.armorId);
         bool wasEquippedWeapon = (itemId == userInfo.weaponId);

         // Delete the item from the DB
         int rowsAffected = DB_Main.deleteItem(_player.userId, itemId);

         // If they deleted an equipped item, we need to update the users table
         if (wasEquippedArmor) {
            DB_Main.setArmorId(_player.userId, 0);
         } else if (wasEquippedWeapon) {
            DB_Main.setWeaponId(_player.userId, 0);
         }

         // Back to Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // If exactly one row was affected, then send confirmation back to the player
            if (rowsAffected == 1) {
               // If the item was equipped, we need to update the sync vars and everyone in the instance
               if (wasEquippedWeapon) {
                  // If they're in an island scene, we need to update the body object
                  if (body != null) {
                     requestSetWeaponId(0);
                  }
               } else if (wasEquippedArmor) {
                  // If they're in an island scene, we need to update the body object
                  if (body != null) {
                     requestSetArmorId(0);
                  }
               }

               // Send the confirmation message so the player updates their inventory panel
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ConfirmDeleteItem, _player, itemId + "");
            } else {
               D.warning(string.Format("Player {0} tried to delete item {1}, but the DB query affected {2} rows?!", _player, itemId, rowsAffected));
            }
         });
      });
   }

   [Command]
   public void Cmd_SellCrops (int offerId, int amountToSell) {
      _player.cropManager.sellCrops(offerId, amountToSell);
   }

   [Command]
   public void Cmd_BuyStoreItem (int itemId) {
      if (_player == null) {
         return;
      }

      // Look up the item box for the specified item id
      StoreItemBox itemBox = GemStoreManager.self.getItemBox(itemId);

      // Off to the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         bool success = false;
         int gems = DB_Main.getGems(_player.accountId);

         // Make sure they can afford it
         if (gems >= itemBox.itemCost) {
            // Remove the gems
            DB_Main.addGems(_player.accountId, -itemBox.itemCost);
            success = true;

            // Insert the different item types here
            if (itemBox is StoreHairDyeBox) {
               StoreHairDyeBox dyeBox = (StoreHairDyeBox) itemBox;
               DB_Main.insertNewUsableItem(_player.userId, UsableItem.Type.HairDye, dyeBox.colorType, dyeBox.colorType);
            } else if (itemBox is StoreShipBox) {
               StoreShipBox box = (StoreShipBox) itemBox;
               DB_Main.insertNewUsableItem(_player.userId, UsableItem.Type.ShipSkin, box.skinType);
            } else if (itemBox is StoreHaircutBox) {
               StoreHaircutBox box = (StoreHaircutBox) itemBox;
               DB_Main.insertNewUsableItem(_player.userId, UsableItem.Type.Haircut, box.hairType);
            }
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (success) {
               string feedback = "You have purchased " + itemBox.itemName + "!";
               if (itemBox is StoreHairDyeBox || itemBox is StoreHaircutBox || itemBox is StoreShipBox) {
                  feedback += "  It has been placed in your backpack as a usable item.";
               }

               // Let the player know that we gave them their item
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.StoreItemBought, _player, feedback);
            } else {
               ServerMessageManager.sendError(ErrorMessage.Type.NotEnoughGems, _player, "You don't have " + itemBox.itemCost + " gems!");
            }
         });
      });
   }

   [Command]
   public void Cmd_UseItem (int itemId) {
      if (_player == null) {
         return;
      }

      // They may be in an island scene, or at sea
      BodyEntity body = _player.GetComponent<BodyEntity>();
      PlayerShipEntity ship = _player.GetComponent<PlayerShipEntity>();

      // Off to the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Item item = DB_Main.getItem(_player.userId, itemId);

         if (item != null && item is UsableItem) {
            UsableItem usable = (UsableItem) item;

            if (usable.itemType == UsableItem.Type.HairDye) {
               ColorType newColor = usable.color1;

               // The player shouldn't be on a ship
               if (body == null) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You need to be on land to use this item.");
                  });
                  return;
               }

               // Set the new hair color in the database
               DB_Main.setHairColor(body.userId, newColor);

               // Delete the item now that we've used it
               DB_Main.deleteItem(body.userId, itemId);

               // Back to Unity
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  body.hairColor1 = newColor;

                  // Update the body sheets on all clients
                  body.rpc.Rpc_UpdateHair(body.hairType, body.hairColor1, body.hairColor2);

                  // Let the client know the item was used
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedHairDye, _player, itemId + "");
               });
            } else if (usable.itemType == UsableItem.Type.ShipSkin) {
               Ship.SkinType newSkinType = usable.getSkinType();

               // The player has to be on a ship
               if (ship == null) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You must be on a ship to use this item.");
                  });
                  return;
               }

               // It must be the right type of ship
               if (!ship.canUseSkin(newSkinType)) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "That ship skin can't be applied to the type of ship you're currently on.");
                  });
                  return;
               }

               // Set the new skin in the database
               DB_Main.setShipSkin(ship.shipId, newSkinType);

               // Delete the item now that we've used it
               DB_Main.deleteItem(_player.userId, itemId);

               // Back to Unity
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  ship.skinType = newSkinType;

                  // Update the ship on all clients
                  ship.rpc.Rpc_UpdateShipSkin(newSkinType);

                  // Let the client know the item was used
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedShipSkin, _player, itemId + "");
               });
            } else if (usable.itemType == UsableItem.Type.Haircut) {
               HairLayer.Type newHairType = usable.getHairType();

               // The player shouldn't be on a ship
               if (body == null) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You need to be on land to use this item.");
                  });
                  return;
               }

               // Make sure the gender is right
               bool isFemaleHaircut = newHairType.ToString().ToLower().Contains("female");
               if ((isFemaleHaircut && _player.isMale()) || (!isFemaleHaircut && !_player.isMale())) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "This haircut is for a different gender.");
                  });
                  return;
               }

               // Set the new hair in the database
               DB_Main.setHairType(_player.userId, newHairType);

               // Delete the item now that we've used it
               DB_Main.deleteItem(_player.userId, itemId);

               // Back to Unity
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  body.hairType = newHairType;

                  // Update the ship on all clients
                  body.rpc.Rpc_UpdateHair(newHairType, body.hairColor1, body.hairColor2);

                  // Let the client know the item was used
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedHaircut, _player, itemId + "");
               });
            }
         }
      });
   }

   [Command]
   public void Cmd_UpdateGems () {
      if (_player == null) {
         D.warning("Null player.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserInfo userInfo = DB_Main.getUserInfo(_player.userId);

         // Back to Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_UpdateGems(_player.connectionToClient, userInfo.gems);
         });
      });
   }

   [Command]
   public void Cmd_GetOffersForArea () {
      // Get the current list of offers for the area
      List<CropOffer> list = ShopManager.self.getOffers(_player.areaType);

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveOffers(_player.connectionToClient, gold, list.ToArray(),
               ShopManager.self.lastCropRegenTime.ToBinary());
         });
      });
   }

   [Command]
   public void Cmd_GetItemsForArea () {
      getItemsForArea();
   }

   [Command]
   public void Cmd_GetShipsForArea () {
      getShipsForArea();
   }

   [Command]
   public void Cmd_UpdateNPCRelation (int npcID, int relationLevel) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Update the setting in the database
         DB_Main.updateNPCRelation(_player.userId, npcID, relationLevel);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_UpdateNPCRelation(_player.connectionToClient, npcID, relationLevel);
         });
      });
   }

   [Command]
   public void Cmd_CreateNPCRelation (int npcID, string npcName) {
      NPC npc = NPCManager.self.getNPC(npcID);
      List<QuestInfo> questList = npc.npcData.npcQuestList[0].getAllQuests();
      int deliverIndex = 0;
      int huntIndex = 0;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int indexCount = 0;
         int indexMax = questList.Count;
         foreach (QuestInfo info in questList) {
            NPCRelationInfo npcRelation = new NPCRelationInfo(_player.userId, npcID, npcName, info.questType.ToString(), 0, 0, 0);
            switch (info.questType) {
               case QuestType.Deliver:
                  npcRelation.npcQuestIndex = deliverIndex;
                  deliverIndex++;
                  break;
               case QuestType.Hunt:
                  npcRelation.npcQuestIndex = huntIndex;
                  huntIndex++;
                  break;
            }
            DB_Main.createNPCRelation(npcRelation);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               int questType = (int) info.questType;
               Target_GetNPCInfo(_player.connectionToClient, npcRelation.npcID, npcRelation.userID, questType, npcRelation.npcRelationLevel, npcRelation.npcQuestIndex, npcRelation.npcQuestProgress);
               indexCount++;
               if (indexCount >= indexMax) {
                  Cmd_DispatchComplete(npcRelation.npcID, npcRelation.npcRelationLevel);
               }
            });
         }
      });
   }

   [Command]
   public void Cmd_DispatchComplete (int npcID, int relpId) {
      Target_NPCPanelComplete(_player.connectionToClient, relpId, npcID);
   }

   [Command]
   public void Cmd_UpdateNPCQuestProgress (int npcID, int questProgress, int questIndex, string questType) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Update the setting in the database
         DB_Main.updateNPCProgress(_player.userId, npcID, questProgress, questIndex, questType);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_UpdateNPCQuestProgress(_player.connectionToClient, npcID, questProgress, questIndex, questType);
         });
      });
   }

   [Command]
   public void Cmd_GetNPCRelation (int npcID, string npcName) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<NPCRelationInfo> npcRelationList = DB_Main.getNPCRelationInfo(_player.userId, npcID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (npcRelationList.Count == 0) {
               // Send command to create data for non existent npc
               Cmd_CreateNPCRelation(npcID, npcName);
               return;
            } else {
               // Fetches the info of the npc
               for (int i = 0; i < npcRelationList.Count; i++) {
                  NPCRelationInfo npcRelation = npcRelationList[i];

                  // Creates the npc incase it did not exist
                  if (npcRelation.userID == 0) {
                     D.log("NPC Relation does not exist, Adding...");
                     Cmd_CreateNPCRelation(npcID, npcName);
                     return;
                  }
                  int questType = (int) npcRelation.npcQuestType;
                  Target_GetNPCInfo(_player.connectionToClient, npcRelation.npcID, npcRelation.userID, questType, npcRelation.npcRelationLevel, npcRelation.npcQuestIndex, npcRelation.npcQuestProgress);
               }

               // Gives notification that the quest data is complete and can now popup npc panel
               Cmd_DispatchComplete(npcRelationList[0].npcID, npcRelationList[0].npcRelationLevel);
            }
         });
      });
   }

   [Command]
   public void Cmd_BuyItem (int shopItemId) {
      Item newItem = null;
      Item shopItem = ShopManager.self.getItem(shopItemId);

      if (shopItem == null) {
         D.warning("Couldn't find item for item id: " + shopItemId);
         return;
      }

      // Make sure it's still available
      if (shopItem.count <= 0) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "That item has sold out!");
         getItemsForArea();
         return;
      }

      int price = shopItem.getSellPrice();

      // Make sure the player has enough money
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         // Make sure they have enough gold
         if (gold >= price) {
            DB_Main.addGold(_player.userId, -price);

            // Create a new instance of the item
            newItem = DB_Main.createNewItem(_player.userId, shopItem);
         }

         // Back to Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (gold < shopItem.getSellPrice()) {
               ServerMessageManager.sendError(ErrorMessage.Type.NotEnoughGold, _player, "You don't have " + price + " gold!");
               return;
            }

            if (newItem.id <= 0) {
               D.warning("Couldn't create new item ID.");
               return;
            }

            // Decrease the item count
            ShopManager.self.decreaseItemCount(shopItemId);

            // Let the client know that it was successful
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.StoreItemBought, _player, "You have purchased a " + shopItem.getName() + "!");

            // Make sure their gold display gets updated
            getItemsForArea();
         });
      });
   }

   [Command]
   public void Cmd_BuyShip (int shipId) {
      ShipInfo ship = ShopManager.self.getShip(shipId);

      if (ship == null) {
         D.warning("Couldn't find ship for ship id: " + shipId);
         return;
      }

      int price = ship.price;

      // Make sure the player has enough money
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);
         ShipInfo newShipInfo = new ShipInfo();

         // Make sure they have enough gold
         if (gold >= price) {
            DB_Main.addGold(_player.userId, -price);

            // Create a new instance of the ship
            newShipInfo = DB_Main.createShipFromShipyard(_player.userId, ship);
         }

         // Back to Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (gold < price) {
               ServerMessageManager.sendError(ErrorMessage.Type.NotEnoughGold, _player, "You don't have " + price + " gold!");
               return;
            }

            if (newShipInfo.shipId <= 0) {
               D.warning("Couldn't create new ship.");
               return;
            }

            // Note that it's sold now
            ship.hasSold = true;

            // Set it as their new flagship
            requestNewFlagship(newShipInfo.shipId);

            // Show a popup panel for the player
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ShipBought, _player);

            // Make sure their gold display gets updated
            getShipsForArea();
         });
      });
   }

   [Command]
   public void Cmd_CreateGuild (string requestedName, Faction.Type factionType) {
      if (_player.guildId > 0) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "You are already in a guild.");
         return;
      }

      // Make sure the length is right
      if (requestedName.Length < GuildInfo.MIN_NAME_LENGTH) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "That name is too short.");
         return;
      }
      if (requestedName.Length > GuildInfo.MAX_NAME_LENGTH) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "That name is too long.");
         return;
      }

      // Make sure it doesn't contain bad words
      if (BadWordManager.Contains(requestedName) | !NameUtil.isValid(requestedName, true)) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "That name is not allowed.");
         return;
      }

      // Try and insert the name
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int guildId = DB_Main.createGuild(requestedName, factionType);

         // Assign the guild to the player
         if (guildId > 0) {
            DB_Main.assignGuild(_player.userId, guildId);
            _player.guildId = guildId;

            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.CreatedGuild, _player, "You have created the guild " + requestedName + "!");
            });

         } else {
            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "That guild name has been taken.");
            });
         }
      });
   }

   [Command]
   public void Cmd_LeaveGuild () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.assignGuild(_player.userId, 0);
         _player.guildId = 0;

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You have left your guild!");
         });
      });
   }

   [Command]
   public void Cmd_InviteToGuild (int recipientId) {
      // Look up the guild info
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         GuildInfo info = DB_Main.getGuildInfo(_player.guildId);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            GuildManager.self.handleInvite(_player, recipientId, info);
         });
      });
   }

   [Command]
   public void Cmd_AcceptInvite (GuildInvite invite) {
      GuildManager.self.acceptInviteOnServer(_player, invite);
   }

   [Command]
   public void Cmd_FinishedQuest (int npcID, int questIndex) {
      validateNPCRewards(_player.userId, npcID, questIndex);
   }

   [Command]
   public void Cmd_CraftItem (Blueprint.Type blueprintType) {
      validateCraftingRewards(_player.userId, blueprintType);
   }

   [Server]
   public void validateNPCRewards (int userID, int npcID, int questIndex) {
      // Retrieves the npc quest data on server side using npc id
      NPC npc = NPCManager.self.getNPC(npcID);

      // Checks the required items if it exists in the database
      Item requiredItem = npc.npcData.npcQuestList[0].deliveryQuestList[questIndex].deliveryQuest.itemToDeliver;
      List<CraftingIngredients.Type> requiredItemList = new List<CraftingIngredients.Type>();
      requiredItemList.Add((CraftingIngredients.Type) requiredItem.itemTypeId);

      // Fetches the needed items and checks if it is equivalent to the requirement
      List<Item> databaseItemList = new List<Item>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         databaseItemList = DB_Main.getRequiredIngredients(userID, requiredItemList);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Determines if npc is near player
            float distance = Vector2.Distance(npc.transform.position, _player.transform.position);

            if (distance > NPC.TALK_DISTANCE) {
               D.log("Too far away from the player!");
               return;
            }

            if (databaseItemList.Count <= 0) {
               D.log("You do not have the materials!");
               return;
            } else {
               for (int i = 0; i < databaseItemList.Count; i++) {
                  if (databaseItemList[i].count < requiredItem.count) {
                     D.log("Insufficient Materials");
                     return;
                  }
               }
            }
            processNPCReward(userID, npcID, questIndex, databaseItemList);
         });
      });
   }

   [Server]
   private void processNPCReward (int userID, int npcID, int questIndex, List<Item> databaseItems) {
      // Retrieves the npc quest data on server side using npc id
      NPC npc = NPCManager.self.getNPC(npcID);

      // Generates the item to be rewarded
      int rewardQuantity = npc.npcData.npcQuestList[0].deliveryQuestList[questIndex].rewardQuantity;
      CraftingIngredients questReward = new CraftingIngredients(0, (int) npc.npcData.npcQuestList[0].deliveryQuestList[questIndex].rewardType, ColorType.DarkGreen, ColorType.DarkPurple, "");
      questReward.itemTypeId = (int) questReward.type;
      questReward.count = rewardQuantity;

      // Fetch reward and database items for comparison
      Item rewardItem = questReward;
      List<Item> requiredItems = new List<Item>();
      Item requiredItem = npc.npcData.npcQuestList[0].deliveryQuestList[questIndex].deliveryQuest.itemToDeliver;
      requiredItems.Add(requiredItem);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fetches reward item id
         int rewardItemID = DB_Main.getItemID(userID, (int) rewardItem.category, rewardItem.itemTypeId);

         // Sends notification to player about the item received
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            rewardItem.id = rewardItemID;
            finalizeRewards(userID, rewardItem, databaseItems, requiredItems);
         });
      });
   }

   [Server]
   public void processEnemyRewards (Enemy.Type enemyType) {
      // Gets loots for enemy type
      EnemyLootLibrary lootLibrary = RewardManager.self.enemyLootList.Find(_ => _.enemyType == enemyType);
      List<LootInfo> processedLoots = lootLibrary.dropTypes.requestLootList();
      processGenericLoots(processedLoots);
   }

   [Server]
   public void validateCraftingRewards (int userId, Blueprint.Type blueprintType) {
      CombinationData data = RewardManager.self.combinationDataList.comboDataList.Find(_ => _.blueprintTypeID == (int) blueprintType);
  
      List<CraftingIngredients.Type> requiredItemList = new List<CraftingIngredients.Type>();
      for (int i = 0; i < data.combinationRequirements.Count; i++) {
         requiredItemList.Add((CraftingIngredients.Type) data.combinationRequirements[i].itemTypeId);
      }

      // Fetches the needed items and checks if it is equivalent to the requirement
      List<Item> databaseItemList = new List<Item>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         databaseItemList = DB_Main.getRequiredIngredients(userId, requiredItemList);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (databaseItemList.Count <= 0) {
               D.log("You do not have the materials!");
               return;
            } else {
               for (int i = 0; i < databaseItemList.Count; i++) {
                  if (databaseItemList[i].count < data.combinationRequirements[i].count) {
                     D.log("Insufficient Materials");
                     return;
                  }
               }
            }

            processCraftingRewards(userId, blueprintType, databaseItemList, data.combinationRequirements);
         });
      });
   }

   [Server]
   private void processCraftingRewards (int userId, Blueprint.Type blueprintType, List<Item> databaseItems, List<Item> requiredItems) {
      // Gets the crafting result using cached scriptable object combo data
      CombinationData data = RewardManager.self.combinationDataList.comboDataList.Find(_ => _.blueprintTypeID == (int) blueprintType);
      Item rewardItem = data.resultItem;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fetches reward item id
         int rewardItemID = DB_Main.getItemID(userId, (int) rewardItem.category, rewardItem.itemTypeId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            rewardItem.id = rewardItemID;
            finalizeRewards(userId, rewardItem, databaseItems, requiredItems);
         });
      });
   }

   [Server]
   private void finalizeRewards (int userId, Item rewardItem, List<Item> databaseItems, List<Item> requiredItems) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Creates or updates item database count
         DB_Main.createOrUpdateItemCount(userId, rewardItem.id, rewardItem);

         for(int i = 0; i < requiredItems.Count; i++) {
            int databaseCount = databaseItems.Find(_ => _.itemTypeId == requiredItems[i].itemTypeId && _.category == requiredItems[i].category).count;
            int requiredCount = requiredItems[i].count;
            int deductedQuantity = databaseCount - requiredCount;

            // Deduct quantity of each required ingredient or delete item if it hits zero count
            int databaseID = databaseItems.Find(_ => _.itemTypeId == requiredItems[i].itemTypeId && _.category == requiredItems[i].category).id;
            DB_Main.decreaseQuantityOrDeleteItem(userId, databaseID, deductedQuantity);
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveItem(_player.connectionToClient, rewardItem);
         });
      });
   }

   [Server]
   public void processGenericLoots(List<LootInfo> processedLoots) {
      // Item list handling
      List<Item> newItemList = new List<Item>();
      int processedLootCount = processedLoots.Count;

      // Generates new list to be passed to client
      for (int i = 0; i < processedLootCount; i++) {
         CraftingIngredients createdItem = new CraftingIngredients(0, (int) processedLoots[i].lootType, ColorType.None, ColorType.None, "");
         createdItem.itemTypeId = (int) processedLoots[i].lootType;
         newItemList.Add(createdItem.getCastItem());
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         for (int i = 0; i < processedLootCount; i++) {
            DB_Main.createNewItem(_player.userId, newItemList[i]);
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveItemList(_player.connectionToClient, newItemList.ToArray());
         });
      });
   }
   
   [TargetRpc]
   public void Target_ReceiveItemList(NetworkConnection connection, Item[] itemList) {
      RewardManager.self.showItemsInRewardPanel(itemList.ToList());

      // Tells the user to update their inventory cache to retrieve the updated items
      InventoryCacheManager.self.fetchInventory();
   }

   [TargetRpc]
   public void Target_ReceiveItem (NetworkConnection connection, Item item) {
      RewardManager.self.showItemInRewardPanel(item);

      // Tells the user to update their inventory cache to retrieve the updated items
      InventoryCacheManager.self.fetchInventory();
   }

   [TargetRpc]
   public void Target_UpdateInventory (NetworkConnection connection) {
      InventoryCacheManager.self.fetchInventory();
   }

   [Command]
   public void Cmd_MineNode (int nodeId) {
      OreNode oreNode = OreManager.self.getOreNode(nodeId);

      // Make sure we found the Node
      if (oreNode == null) {
         D.warning("Ore node not found: " + nodeId);
         return;
      }

      // Make sure the user is in the right instance
      if (_player.instanceId != oreNode.instanceId) {
         D.warning("Player trying to open ore node from a different instance!");
         return;
      }

      // Make sure they didn't already open it
      if (oreNode.userIds.Contains(_player.userId)) {
         D.warning("Player already mined this ore node!");
         return;
      }

      // Add the user ID to the list
      oreNode.userIds.Add(_player.userId);

      // Gathers the item rewards from the scriptable object
      List<LootInfo> lootInfoList = RewardManager.self.oreLootList.Find(_ => _.oreType == oreNode.oreType).dropTypes.requestLootList();

      // Registers list of ingredient types for data fetching
      List<CraftingIngredients.Type> itemLoots = new List<CraftingIngredients.Type>();
      for(int i = 0; i < lootInfoList.Count; i++) {
         itemLoots.Add(lootInfoList[i].lootType);
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<Item> databaseList = DB_Main.getRequiredIngredients(_player.userId, itemLoots);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            processMineReward(_player.userId, databaseList, lootInfoList, oreNode.oreType);
         });
      });
   }

   [Server]
   private void processMineReward(int userID, List<Item> databaseItems, List<LootInfo> rewardList, OreNode.Type oreType) {
      // Generate Item List to show in popup after data writing
      List<Item> itemRewardList = new List<Item>();
      for (int i = 0; i < rewardList.Count; i++) {
         Item itemToCreate = new CraftingIngredients(0, rewardList[i].lootType, ColorType.Black, ColorType.Black);
         Item databaseItemType = databaseItems.Find(_ => _.category == Item.Category.CraftingIngredients && _.itemTypeId == (int) rewardList[i].lootType);
         
         // Registers the quantity of each item
         itemToCreate.count = rewardList[i].quantity;
         if (databaseItemType != null) {
            itemToCreate.id = databaseItemType.id;
         }
         itemRewardList.Add(itemToCreate);
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Creates or updates database item
         DB_Main.createOrUpdateItemListCount(userID, itemRewardList);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Calls Reward Popup
            Target_ReceiveItemList(_player.connectionToClient, itemRewardList.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_OpenChest (int chestId) {
      TreasureChest chest = TreasureManager.self.getChest(chestId);

      // Make sure we found the Treasure Chest
      if (chest == null) {
         D.warning("Treasure chest not found: " + chestId);
         return;
      }

      // Make sure the user is in the right instance
      if (_player.instanceId != chest.instanceId) {
         D.warning("Player trying to open treasure from a different instance!");
         return;
      }

      // Make sure they didn't already open it
      if (chest.userIds.Contains(_player.userId)) {
         D.warning("Player already opened this chest!");
         return;
      }

      // Add the user ID to the list
      chest.userIds.Add(_player.userId);

      // Check what we're going to give the user
      Item item = chest.getContents();

      // Add it to their inventory
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         item = DB_Main.createNewItem(_player.userId, item);
      });

      // Send it to the specific player that opened it
      Target_OpenChest(_player.connectionToClient, item, chest.id);
   }

   [Command]
   public void Cmd_GetClickableRows (int npcId, int questType, int questProgress, int questIndex) {
      processClickableRows(_player.userId, npcId, questType, questProgress, questIndex);
   }

   [Server]
   public void processClickableRows(int userId, int npcId, int questType, int questProgress, int questIndex) {
      // Look up the NPC
      NPC npc = NPCManager.self.getNPC(npcId);
      Item requiredItem = npc.npcData.npcQuestList[0].deliveryQuestList[questIndex].deliveryQuest.itemToDeliver;
      List<CraftingIngredients.Type> requiredItemList = new List<CraftingIngredients.Type>();
      requiredItemList.Add((CraftingIngredients.Type) requiredItem.itemTypeId);

      NPCPanel.DialogueData dialogueData = new NPCPanel.DialogueData();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<NPCRelationInfo> npcRelationList = DB_Main.getNPCRelationInfo(_player.userId, npcId);
         List<Item> databaseItemList = DB_Main.getRequiredIngredients(userId, requiredItemList);

         bool hasMaterials = true;
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if ((QuestType) questType == QuestType.Deliver) {
               if (databaseItemList.Count <= 0) {
                  D.log("You do not have the materials!");
                  hasMaterials = false;
               } else {
                  for (int i = 0; i < databaseItemList.Count; i++) {
                     if (databaseItemList[i].count < requiredItem.count) {
                        D.log("Insufficient Materials");
                        hasMaterials = false;
                     }
                  }
               }
               dialogueData = NPCPanel.getDialogueInfo(questProgress, npc.npcData.npcQuestList[0].deliveryQuestList[questIndex], hasMaterials);
               Target_ReceiveClickableNPCRows(_player.connectionToClient, dialogueData.answerList.ToArray(), npcId);
               Target_ReceiveNPCMessage(_player.connectionToClient, dialogueData.npcDialogue);
            }
         });
      });
   }

   [Command]
   public void Cmd_SpawnPirateShip (Vector2 spawnPosition) {
      BotShipEntity bot = Instantiate(PrefabsManager.self.botShipPrefab, spawnPosition, Quaternion.identity);
      bot.instanceId = _player.instanceId;
      bot.facing = Util.randomEnum<Direction>();
      bot.areaType = _player.areaType;
      bot.npcType = NPC.Type.Blackbeard;
      bot.faction = NPC.getFaction(bot.npcType);
      bot.route = null;
      bot.autoMove = true;
      bot.nationType = Nation.Type.Pirate;
      bot.speed = Ship.getBaseSpeed(Ship.Type.Caravel);
      bot.entityName = "Pirate";

      // Set up the movement route
      // Area area = AreaManager.self.getArea(bot.areaType);
      // bot.route = Instantiate(PrefabsManager.self.figureEightRoutePrefab, spawnPosition, Quaternion.identity, area.transform);

      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      instance.entities.Add(bot);

      // Spawn the bot on the Clients
      NetworkServer.Spawn(bot.gameObject);
   }

   [Command]
   public void Cmd_StartNewBattle (uint enemyNetId) {
      // We need a Player Body object to proceed
      if (!(_player is PlayerBodyEntity)) {
         D.warning("Player object is not a Player Body, so can't start a Battle: " + _player);
         return;
      }

      // Make sure we're not already in a Battle
      if (BattleManager.self.isInBattle(_player.userId)) {
         D.warning("Can't start new Battle for player that's already in a Battle: " + _player.userId);
         return;
      }

      // Get references to the Player and Enemy objects
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      NetworkIdentity enemyIdent = NetworkIdentity.spawned[enemyNetId];
      Enemy enemy = enemyIdent.GetComponent<Enemy>();
      Instance instance = InstanceManager.self.getInstance(playerBody.instanceId);

      // Look up the player's Area
      Area area = AreaManager.self.getArea(_player.areaType);

      // Get or create the Battle instance
      Battle battle = (enemy.battleId > 0) ? BattleManager.self.getBattle(enemy.battleId) :
         BattleManager.self.createBattle(area, instance, enemy, playerBody);

      // If the Battle is full, we can't proceed
      if (!battle.hasRoomLeft(Battle.TeamType.Attackers)) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The battle is already full!");
         return;
      }

      // Add the player to the Battle
      BattleManager.self.addPlayerToBattle(battle, playerBody, Battle.TeamType.Attackers);
   }

   [Command]
   public void Cmd_RequestAttack (uint netId, Ability.Type abilityType) {
      if (_player == null || !(_player is PlayerBodyEntity)) {
         return;
      }

      // Look up the player's Battle object
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      Battle battle = BattleManager.self.getBattle(playerBody.battleId);
      Ability ability = AbilityManager.getAbility(abilityType);
      Battler sourceBattler = battle.getBattler(_player.userId);

      Battler targetBattler = null;

      foreach (Battler participant in battle.getParticipants()) {
         if (participant.netId == netId) {
            targetBattler = participant;
         }
      }

      // Ignore invalid or dead sources and targets
      if (sourceBattler == null || targetBattler == null || sourceBattler.isDead() || targetBattler.isDead()) {
         return;
      }

      // Make sure the source battler can use that ability type
      if (!ability.isReadyForUseBy(sourceBattler)) {
         D.warning("Battler requested to use ability they're not allowed: " + playerBody.entityName + ", " + abilityType);
         return;
      }

      // If it's a Melee Ability, make sure the target isn't currently protected
      if (ability is MeleeAbility && targetBattler.isProtected(battle)) {
         D.warning("Battler requested melee ability against protected target! Player: " + playerBody.entityName);
         return;
      }

      // Let the Battle Manager handle executing the attack
      List<Battler> targetBattlers = new List<Battler>() { targetBattler };
      BattleManager.self.executeAttack(battle, sourceBattler, targetBattlers, Ability.Type.Basic_Attack);
   }

   [Server]
   protected void getShipsForArea () {
      // Get the current list of ships for the area
      List<ShipInfo> list = ShopManager.self.getShips(_player.areaType);

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveShipyard(_player.connectionToClient, gold, Util.serialize(list));
         });
      });
   }

   [Server]
   protected void requestNewFlagship (int flagshipId) {
      // The player can't change flagship while at sea
      if (_player is SeaEntity) {
         D.warning("Can't change flagship at sea!");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Make sure they own that ship
         ShipInfo shipInfo = DB_Main.getShipInfo(flagshipId);

         if (shipInfo.userId != _player.userId) {
            D.warning("User " + _player + " can not set flagship: " + flagshipId);
            return;
         }

         // Update the setting in the database
         DB_Main.setCurrentShip(_player.userId, flagshipId);

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveNewFlagshipId(_player.connectionToClient, flagshipId);
         });
      });
   }

   [Server]
   protected void getItemsForArea () {
      // Get the current list of items for the area
      List<Item> list = ShopManager.self.getItems(_player.areaType);

      // Sort by rarity
      List<Item> sortedList = list.OrderBy(x => x.getSellPrice()).ToList();

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveItems(_player.connectionToClient, gold, Util.serialize(sortedList));
         });
      });
   }

   [Server]
   protected void requestSetArmorId (int armorId) {
      // They may be in an island scene, or at sea
      BodyEntity body = _player.GetComponent<BodyEntity>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setArmorId(_player.userId, armorId);
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Armor armor = userObjects.armor;

            if (body != null) {
               body.armorManager.updateArmorSyncVars(armor);
            }

            // Let the client know that we're done
            EquipMessage equipMessage = new EquipMessage(_player.netId, userObjects.armor.id, userObjects.weapon.id);
            NetworkServer.SendToClientOfPlayer(_player.netIdent, equipMessage);
         });
      });
   }

   [Server]
   protected void requestSetWeaponId (int weaponId) {
      // They may be in an island scene, or at sea
      BodyEntity body = _player.GetComponent<BodyEntity>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Update the Weapon object here on the server based on what's in the database
         DB_Main.setWeaponId(_player.userId, weaponId);
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

         // Back to Unity Thread to call RPC functions
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Weapon weapon = userObjects.weapon;

            if (body != null) {
               body.weaponManager.updateWeaponSyncVars(weapon);
            }

            // Let the client know that we're done
            EquipMessage equipMessage = new EquipMessage(_player.netId, userObjects.armor.id, userObjects.weapon.id);
            NetworkServer.SendToClientOfPlayer(_player.netIdent, equipMessage);
         });
      });
   }

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   #endregion
}
