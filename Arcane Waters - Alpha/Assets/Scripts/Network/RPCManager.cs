using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using Crosstales.BWF.Manager;
using System;
using System.Text;
using Random = UnityEngine.Random;
using MapCustomization;
using NubisDataHandling;
using MapCreationTool.Serialization;
using MapCreationTool;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;

public class RPCManager : NetworkBehaviour
{
   #region Public Variables

   #endregion

   private void Awake () {
      _player = GetComponent<NetEntity>();
   }

   #region Auction Features

   [Command]
   public void Cmd_CancelAuction (int auctionId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         AuctionItemData auction = DB_Main.getAuction(auctionId, false);
         DateTime expiryDate = DateTime.FromBinary(auction.expiryDate);

         if (expiryDate <= DateTime.UtcNow) {
            sendError("This auction has ended!");
            return;
         }

         if (auction.sellerId != _player.userId) {
            sendError("You do not own this auction!");
            return;
         }

         // Return his gold to the previous highest bidder - if any
         if (auction.highestBidUser > 0) {
            DB_Main.addGold(auction.highestBidUser, auction.highestBidPrice);
         }

         // Deliver the item to the seller and delete the auction
         DB_Main.deliverAuction(auction.auctionId, auction.mailId, auction.sellerId);
         DB_Main.deleteAuction(auctionId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ModifiedOwnAuction, _player, "Your auction has been cancelled and the item delivered to you by mail!");
         });
      });
   }

   [Command]
   public void Cmd_CreateAuction (Item item, int startingBid, int buyoutPrice, long expiryDateBinary) {
      DateTime expiryDate = DateTime.FromBinary(expiryDateBinary);

      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (startingBid <= 0) {
         sendError("The starting bid must be higher than 0.");
         return;
      }

      if (buyoutPrice <= 0) {
         sendError("The buyout price must be higher than 0.");
         return;
      }

      if (buyoutPrice <= startingBid) {
         sendError("The buyout price must be higher than the starting bid!");
         return;
      }

      if (expiryDate <= DateTime.UtcNow) {
         sendError("The auction duration is inconsistent.");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Create a mail without recipient, with the auctioned item as attachment - this will also verify the item validity
         int mailId = createMailCommon(-1, "Auction House - Item Delivery", "", new int[] { item.id }, new int[] { item.count });

         // Create the auction
         int newId = DB_Main.createAuction(_player.userId, _player.nameText.text, mailId, expiryDate, startingBid,
            buyoutPrice, item.category, EquipmentXMLManager.self.getItemName(item), item.count);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ModifiedOwnAuction, _player, "Your item has been successfully submitted for auction!");
         });
      });
   }

   [Command]
   public void Cmd_BidOnAuction (int auctionId, int bidAmount) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         AuctionItemData auction = DB_Main.getAuction(auctionId, false);
         DateTime expiryDate = DateTime.FromBinary(auction.expiryDate);

         if (expiryDate <= DateTime.UtcNow) {
            sendError("This auction has ended!");
            return;
         }

         if (auction.sellerId == _player.userId) {
            sendError("You cannot bid on your auctions!");
            return;
         }

         if (bidAmount <= auction.highestBidPrice) {
            sendError("You must set an amount higher than the highest bid.");
            return;
         }

         // Clamp the bid amount to the buyout price
         bidAmount = Mathf.Clamp(bidAmount, 0, auction.buyoutPrice);

         // Withdraw the gold from the user
         int gold = DB_Main.getGold(_player.userId);
         if (gold < bidAmount) {
            sendError("You do not have enough gold!");
            return;
         }
         DB_Main.addGold(_player.userId, -bidAmount);

         // Return his gold to the previous highest bidder - if any
         if (auction.highestBidUser > 0) {
            DB_Main.addGold(auction.highestBidUser, auction.highestBidPrice);
         }

         // Add the user to the list of bidders for this auction
         DB_Main.addBidderOnAuction(auctionId, _player.userId);

         string resultMessage = "";
         if (bidAmount >= auction.buyoutPrice) {
            // Set this user as the highest bidder and close the auction. The item is delivered by the auction manager.
            DB_Main.updateAuction(auctionId, _player.userId, bidAmount, DateTime.UtcNow);
            resultMessage = "You have won the auction! The item will be delivered to you shortly by mail.";
         } else {
            // Set this user as the highest bidder
            DB_Main.updateAuction(auctionId, _player.userId, bidAmount, expiryDate);
            resultMessage = "Your bid has been successfully registered";
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.BidOnAuction, _player, resultMessage);
         });
      });
   }

   [Command]
   public void Cmd_GetAuction (int auctionId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         AuctionItemData auction = DB_Main.getAuction(auctionId, true);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveAuctionInfo(_player.connectionToClient, auction);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveAuctionInfo (NetworkConnection connection, AuctionItemData auctionInfo) {
      PanelManager.self.linkIfNotShowing(Panel.Type.Auction);
      ((AuctionPanel) PanelManager.self.get(Panel.Type.Auction)).auctionInfoPanel.receiveAuctionFromServer(auctionInfo);
   }

   #endregion

   [TargetRpc]
   public void Target_GrantAdminAccess (NetworkConnection connection, bool isEnabled) {
      OptionsPanel panel = (OptionsPanel) PanelManager.self.get(Panel.Type.Options);
      panel.enableAdminButtons(isEnabled);
   }

   [TargetRpc]
   public void Target_ReceiveSoundEffects (NetworkConnection connection, string[] rawInfo) {
      // Deserialize data
      SoundEffect[] soundEffectsList = Util.unserialize<SoundEffect>(rawInfo).ToArray();

      // Cache to SoundEffectManager
      SoundEffectManager.self.receiveListFromServer(soundEffectsList);
   }

   [Command]
   public void Cmd_RequestTeamCombatData () {
      // Checks if the user is an admin
      if (_player.isAdmin()) {
         Target_ReceiveTeamCombatData(_player.connectionToClient, MonsterManager.self.getAllEnemyTypes().ToArray());
      } else {
         D.warning("You are not at admin! Denying access to team combat simulation");
      }
   }

   [TargetRpc]
   public void Target_ReceiveTeamCombatData (NetworkConnection connection, int[] enemyTypes) {
      TeamCombatPanel panel = (TeamCombatPanel) PanelManager.self.get(Panel.Type.Team_Combat);

      // Pass the data to the panel
      panel.receiveDataFromServer(enemyTypes);

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }
   }

   [Command]
   public void Cmd_InteractAnimation (Anim.Type animType, Direction direction) {
      _player.Rpc_ForceLookat(direction);
      Rpc_InteractAnimation(animType);
   }

   [ClientRpc]
   public void Rpc_InteractAnimation (Anim.Type animType) {
      _player.requestAnimationPlay(animType);

      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      playerBody.farmingTrigger.interactFarming();
      playerBody.miningTrigger.interactOres();
   }

   [ClientRpc]
   protected void Rpc_UpdateHair (HairLayer.Type newHairType, string newHairPalettes) {
      if (_player is BodyEntity) {
         BodyEntity body = (BodyEntity) _player;
         body.updateHair(newHairType, newHairPalettes);

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

   #region NPC QUEST

   [TargetRpc]
   public void Target_RemoveQuestNotice (NetworkConnection connection, int npcId) {
      NPC npc = NPCManager.self.getNPC(npcId);
      npc.questNotice.SetActive(false);
   }

   [TargetRpc]
   public void Target_ReceiveProcessRewardToggle (NetworkConnection connection) {
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);
      if (panel.isShowing()) {
         PanelManager.self.unlinkPanel();
         panel.hide();
      }
   }

   [TargetRpc]
   public void Target_ReceiveNPCQuestNode (NetworkConnection connection, int questId,
      int questNodId, int dialogueId, int friendshipLevel, bool areObjectivesCompleted, bool isEnabled, int[] itemStock, Jobs newJobs) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Pass the data to the panel
      panel.updatePanelWithQuestNode(friendshipLevel, questId, questNodId, dialogueId, areObjectivesCompleted, isEnabled, itemStock, newJobs);

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }
   }

   [TargetRpc]
   public void Target_ReceiveNPCQuestList (NetworkConnection connection, int npcId,
      string npcName, int friendshipLevel,
      string greetingText, bool canOfferGift, bool hasTradeGossipDialogue, bool hasGoodbyeDialogue,
      bool isHireable, int landMonsterId, int questId, int questNodeId, int questDialogueId, int[] itemStock, Jobs newJobsXp) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Pass the data to the panel
      panel.updatePanelWithQuestSelection(npcId, npcName, friendshipLevel, greetingText,
         canOfferGift, hasGoodbyeDialogue, isHireable, landMonsterId, questId, questNodeId, questDialogueId, itemStock, newJobsXp);
   }

   [TargetRpc]
   public void Target_ReceiveNPCCustomDialogue (int friendshipLevel, string npcText,
      ClickableText.Type userTextType, string userText) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Pass the data to the panel
      panel.updatePanelWithCustomDialogue(friendshipLevel, npcText, userTextType, userText);

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }
   }

   [TargetRpc]
   public void Target_ReceiveGiftOfferNPCText (string npcText) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Pass the data to the panel
      panel.updatePanelWithGiftOffer(npcText);

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }
   }

   [TargetRpc]
   public void Target_EndNPCDialogue (NetworkConnection connection) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Close the panel
      if (panel.isShowing()) {
         PanelManager.self.unlinkPanel();
      }
   }

   #endregion

   [Server]
   public void checkForLevelUp (int userId, int oldXp, int newXp) {
      // This function handles the different type of logic that will take place when a user reaches a certain level
      int playerLevel = LevelUtil.levelForXp(newXp);
      if (playerLevel == 2) {
         addAbilityByLevel(userId, 2);
      }
   }

   [Server]
   public void addAbilityByLevel (int userId, int currentLevel) {
      switch (currentLevel) {
         case 2:
            // The player receives a new ability at level 2
            int windSlashId = 6;
            AttackAbilityData attackAbilitydata = AbilityManager.self.allAttackbilities.Find(_ => _.itemID == windSlashId);
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               bool hasAbility = DB_Main.hasAbility(userId, windSlashId);
               if (!hasAbility) {
                  DB_Main.updateAbilitiesData(userId, new AbilitySQLData {
                     abilityID = windSlashId,
                     abilityLevel = 1,
                     abilityType = AbilityType.Standard,
                     description = "",
                     equipSlotIndex = -1,
                     name = attackAbilitydata.itemName
                  });
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     D.editorLog("Added player ability! " + attackAbilitydata.itemName, Color.green);
                  });
               } else {
                  D.editorLog("Already has this ability! ", Color.red);
               }
            });
            break;
      }
   }

   [Server]
   public void updateCompanionExp (int companionId, int exp) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateCompanionExp(companionId, _player.userId, exp);
      });
   }

   [Command]
   public void Cmd_UpdateCompanionRoster (int companionId, int equipSlot) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateCompanionRoster(companionId, _player.userId, equipSlot);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveCompanionUpdate(_player.connectionToClient);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveCompanionUpdate (NetworkConnection connection) {
      CompanionPanel panel = (CompanionPanel) PanelManager.self.get(Panel.Type.Companion);
      panel.canvasBlocker.SetActive(false);
   }

   [Command]
   public void Cmd_HireCompanion (int landMonsterId) {
      BattlerData battlerData = MonsterManager.self.getBattlerData(landMonsterId);

      CompanionInfo companionInfo = new CompanionInfo {
         companionName = battlerData.enemyName,
         companionType = (int) battlerData.enemyType,
         companionLevel = 1,
         equippedSlot = 0,
         companionId = -1,
         companionExp = 0,
         iconPath = battlerData.imagePath
      };
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Hire the companion and write to sql database
         DB_Main.updateCompanions(-1, _player.userId, companionInfo.companionName, companionInfo.companionLevel, companionInfo.companionType, companionInfo.equippedSlot, companionInfo.iconPath, companionInfo.companionExp);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveHiringResult(_player.connectionToClient, companionInfo);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveHiringResult (NetworkConnection connection, CompanionInfo companionInfo) {
      // Disable npc panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Close the panel if it is showing
      if (panel.isShowing()) {
         PanelManager.self.unlinkPanel();
      }
      RewardManager.self.showRecruitmentNotice(companionInfo.companionName, companionInfo.iconPath);
   }

   [Command]
   public void Cmd_RequestCompanionData () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<CompanionInfo> companionInfoList = DB_Main.getCompanions(_player.userId);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveCompanionData(_player.connectionToClient, Util.serialize(companionInfoList));
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveCompanionData (NetworkConnection connection, string[] companionDataArray) {
      // Translate data
      List<CompanionInfo> companionInfoList = Util.unserialize<CompanionInfo>(companionDataArray);
      CompanionPanel panel = (CompanionPanel) PanelManager.self.get(Panel.Type.Companion);

      // Pass the data to the panel
      panel.receiveCompanionData(companionInfoList);

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }
   }

   [TargetRpc]
   public void Target_ReceiveShopItems (NetworkConnection connection, int gold, string[] itemArray, string greetingText) {
      List<Item> newCastedItems = Util.unserialize<Item>(itemArray);

      PanelManager.self.linkIfNotShowing(Panel.Type.Adventure);

      AdventureShopScreen.self.updateGreetingText(greetingText);
      AdventureShopScreen.self.updatePanelWithItems(gold, newCastedItems);
   }

   [TargetRpc]
   public void Target_ReceiveOffers (NetworkConnection connection, int gold, CropOffer[] offerArray, string greetingText) {
      PanelManager.self.linkIfNotShowing(Panel.Type.Merchant);
      MerchantScreen.self.updatePanelWithOffers(gold, new List<CropOffer>(offerArray), greetingText);
   }

   [TargetRpc]
   public void Target_ReceiveShipyard (NetworkConnection connection, int gold, string[] shipArray, string greetingText) {
      List<ShipInfo> newShipInfo = new List<ShipInfo>();
      if (shipArray.Length != 0) {
         // Translate Abilities
         newShipInfo = Util.unserialize<ShipInfo>(shipArray);
         foreach (ShipInfo info in newShipInfo) {
            info.shipAbilities = Util.xmlLoad<ShipAbilityInfo>(info.shipAbilityXML);
         }
      }

      PanelManager.self.linkIfNotShowing(Panel.Type.Shipyard);

      ShipyardScreen.self.updatePanelWithShips(gold, newShipInfo, greetingText);
   }

   [TargetRpc]
   public void Target_ReceiveGuildInfo (NetworkConnection connection, GuildInfo info, GuildRankInfo[] guildRanks) {
      // Make sure the panel is showing
      GuildPanel panel = (GuildPanel) PanelManager.self.get(Panel.Type.Guild);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(Panel.Type.Guild);
      }

      // Update the Inventory Panel with the items we received from the server
      panel.receiveDataFromServer(info, guildRanks);
   }

   [TargetRpc]
   public void Target_ReceiveOptionsInfo (NetworkConnection connection, int instanceNumber, int instanceCount) {
      // Make sure the panel is showing
      OptionsPanel panel = (OptionsPanel) PanelManager.self.get(Panel.Type.Options);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(Panel.Type.Options);
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
         PanelManager.self.linkPanel(panel.type);
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

   [Command]
   public void Cmd_OpenLootBag (int chestId) {
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
         return;
      }

      // Add the user ID to the list
      chest.userIds.Add(_player.userId);

      processLootBagRewards(chestId);
   }

   [Server]
   private void processLootBagRewards (int chestId) {
      TreasureChest chest = TreasureManager.self.getChest(chestId);

      // Check what we're going to give the user
      Item item = chest.getContents();

      // Grant the rewards to the user
      giveItemRewardsToPlayer(_player.userId, new List<Item>() { item }, false);

      // Registers the interaction of loot bags to the achievement database for recording
      AchievementManager.registerUserAchievement(_player, ActionType.OpenedLootBag);

      // Send it to the specific player that opened it
      Target_OpenChest(_player.connectionToClient, item, chest.id);
   }

   [Server]
   public void spawnBattlerMonsterChest (int instanceID, Vector3 position, int enemyID) {
      Instance currentInstance = InstanceManager.self.getInstance(instanceID);
      TreasureManager.self.createBattlerMonsterChest(currentInstance, position, enemyID, _player.userId);
   }

   [Server]
   public void endBattle () {
      Target_ReceiveEndBattle(_player.connectionToClient);
   }

   [TargetRpc]
   public void Target_ReceiveEndBattle (NetworkConnection connection) {
      BattleUIManager.self.abilitiesCG.Hide();

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.EndBattle);
   }

   [TargetRpc]
   public void Target_OpenChest (NetworkConnection connection, Item item, int chestId) {

      // Locate the Chest
      TreasureChest chest = null;
      foreach (TreasureChest existingChest in FindObjectsOfType<TreasureChest>()) {
         if (existingChest.id == chestId) {
            chest = existingChest;
         }
      }

      // Start the opening and burst animations
      chest.chestBurstAnimation.enabled = true;
      if (chest.chestType == ChestSpawnType.Site) {
         chest.chestOpeningAnimation.enabled = true;
      } else {
         chest.spriteRenderer.sprite = chest.openedChestSprite;
         chest.chestOpeningAnimation.enabled = false;
      }

      if (item.category != Item.Category.None && item.itemTypeId > 0) {
         item = item.getCastItem();
         chest.StartCoroutine(chest.CO_CreatingFloatingIcon(item));
      }

      // Play some sounds
      SoundManager.create3dSound("Door_open", Global.player.transform.position);
      SoundManager.create3dSound("tutorial_step", Global.player.transform.position);

      // Register chest id player pref data and set as true
      PlayerPrefs.SetInt(TreasureChest.PREF_CHEST_STATE + "_" + Global.userObjects.userInfo.userId + "_" + _player.areaKey + "_" + chest.chestSpawnId, 1);

      if (chest.autoDestroy) {
         chest.disableChest();
      }
   }

   [TargetRpc]
   public void Target_ReceiveTradeHistoryInfo (NetworkConnection connection, TradeHistoryInfo[] trades,
      int pageIndex, int totalTradeCount) {
      List<TradeHistoryInfo> tradeList = new List<TradeHistoryInfo>(trades);

      // Make sure the panel is showing
      TradeHistoryPanel panel = (TradeHistoryPanel) PanelManager.self.get(Panel.Type.TradeHistory);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Pass them along to the Trade History panel
      panel.updatePanelWithTrades(tradeList, pageIndex, totalTradeCount);
   }

   [TargetRpc]
   public void Target_ReceiveLeaderBoards (NetworkConnection connection, LeaderBoardsManager.Period period,
      double secondsLeftUntilRecalculation, LeaderBoardInfo[] farmingEntries,
      LeaderBoardInfo[] sailingEntries, LeaderBoardInfo[] exploringEntries, LeaderBoardInfo[] tradingEntries,
      LeaderBoardInfo[] craftingEntries, LeaderBoardInfo[] miningEntries) {

      // Make sure the panel is showing
      LeaderBoardsPanel panel = (LeaderBoardsPanel) PanelManager.self.get(Panel.Type.LeaderBoards);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Pass them along to the Leader Boards panel
      panel.updatePanelWithLeaderBoardEntries(period, secondsLeftUntilRecalculation, farmingEntries, sailingEntries,
         exploringEntries, tradingEntries, craftingEntries, miningEntries);
   }

   [TargetRpc]
   public void Target_OnEquipItem (NetworkConnection connection, Item equippedWeapon, Item equippedArmor, Item equippedHat) {
      // Refresh the inventory panel
      InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);
      if (panel.isShowing()) {
         panel.refreshPanel();
      }

      // Update the equipped items cache
      Global.setUserEquipment(equippedWeapon, equippedArmor, equippedHat);

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStepByWeaponEquipped();
   }

   [TargetRpc]
   public void Target_ReceiveItemShortcuts (NetworkConnection connection, ItemShortcutInfo[] shortcuts) {
      int shortcutLenght = shortcuts.Length;
      for (int i = 0; i < shortcutLenght; i++) {
         ItemShortcutInfo itemShortcut = shortcuts[i];
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(itemShortcut.item.itemTypeId);
         if (weaponData == null) {
            D.debug("Weapon data is null for item type: {" + itemShortcut.item.itemTypeId + "} disabling shortcut item");

            // Override item to blank if it does not exist in the xml managers, meaning its probably disabled in the database
            shortcuts[i].item.itemTypeId = 0;
            shortcuts[i].item.id = -1;
         }
      }
      PanelManager.self.itemShortcutPanel.updatePanelWithShortcuts(shortcuts);
   }

   [TargetRpc]
   public void Target_ReceiveFriendshipInfo (NetworkConnection connection, FriendshipInfo[] friendshipInfo,
      Friendship.Status friendshipStatus, int pageNumber, int totalFriendInfoCount, int friendCount, int pendingRequestCount) {
      List<FriendshipInfo> friendshipInfoList = new List<FriendshipInfo>(friendshipInfo);

      // Make sure the panel is showing
      FriendListPanel panel = (FriendListPanel) PanelManager.self.get(Panel.Type.FriendList);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithFriendshipInfo(friendshipInfoList, friendshipStatus, pageNumber, totalFriendInfoCount, friendCount, pendingRequestCount);
   }

   [TargetRpc]
   public void Target_ReceiveFriendsList (NetworkConnection connection, FriendshipInfo[] friendshipInfo) {
      List<FriendshipInfo> friendshipInfoList = new List<FriendshipInfo>(friendshipInfo);
      if (friendshipInfoList == null) {
         friendshipInfoList = new List<FriendshipInfo>();
      }

      FriendListManager.self.cachedFriendshipInfoList = friendshipInfoList;
   }

   [TargetRpc]
   public void Target_ReceiveUserIdForFriendshipInvite (NetworkConnection connection, int friendUserId, string friendName) {
      // Make sure the panel is showing
      FriendListPanel panel = (FriendListPanel) PanelManager.self.get(Panel.Type.FriendList);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Send the invitation with the received userId
      FriendListManager.self.sendFriendshipInvite(friendUserId, friendName);
   }

   [TargetRpc]
   public void Target_ReceiveSingleMail (NetworkConnection connection, MailInfo mail, Item[] attachedItems, bool hasUnreadMail) {
      List<Item> attachedItemList = new List<Item>(attachedItems);

      // Make sure the panel is showing
      MailPanel panel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithSingleMail(mail, attachedItemList, hasUnreadMail);
   }

   [TargetRpc]
   public void Target_ReceiveMailList (NetworkConnection connection,
      MailInfo[] mailArray, int pageNumber, int totalMailCount) {
      List<MailInfo> mailList = new List<MailInfo>(mailArray);

      // Make sure the panel is showing
      MailPanel panel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithMailList(mailList, pageNumber, totalMailCount);
   }

   [TargetRpc]
   public void Target_RefreshCraftingPanel (NetworkConnection connection) {
      // Get the crafting panel
      CraftingPanel panel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

      // Refresh the panel
      panel.refreshCurrentlySelectedBlueprint();
   }

   [TargetRpc]
   public void Target_ReceiveVoyageInstanceList (NetworkConnection connection, Voyage[] voyageArray) {
      List<Voyage> voyageList = new List<Voyage>(voyageArray);

      // Make sure the panel is showing
      PanelManager.self.linkIfNotShowing(Panel.Type.Voyage);

      // Pass the data to the panel
      VoyagePanel panel = (VoyagePanel) PanelManager.self.get(Panel.Type.Voyage);
      panel.updatePanelWithVoyageList(voyageList);
   }

   [TargetRpc]
   public void Target_ReceiveCurrentVoyageInstance (NetworkConnection connection, Voyage voyage) {
      // Make sure the panel is showing
      PanelManager.self.linkIfNotShowing(Panel.Type.ReturnToCurrentVoyagePanel);

      // Pass the data to the panel
      ReturnToCurrentVoyagePanel panel = (ReturnToCurrentVoyagePanel) PanelManager.self.get(Panel.Type.ReturnToCurrentVoyagePanel);
      panel.updatePanelWithCurrentVoyage(voyage);
   }

   [TargetRpc]
   public void Target_ReceiveVoyageGroupMembers (NetworkConnection connection, VoyageGroupMemberCellInfo[] groupMembers) {
      // Get the panel
      VoyageGroupPanel panel = VoyageGroupPanel.self;

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         panel.show();
      }

      // Update the panel info
      panel.updatePanelWithGroupMembers(groupMembers);
   }

   [TargetRpc]
   public void Target_ReceiveVoyageGroupMemberPartialUpdate (NetworkConnection connection, int userId, string userName, int XP, string areaKey) {
      // Get the panel
      VoyageGroupPanel panel = VoyageGroupPanel.self;

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         panel.show();
      }

      // Update the panel info
      panel.updateCellTooltip(userId, userName, XP, areaKey);
   }

   [Command]
   public void Cmd_BugReport (string subject, string message, int ping, int fps, byte[] screenshotBytes, string screenResolution, string operatingSystem, string steamState, int deploymentId) {
      // We need a player object
      if (_player == null) {
         D.warning("Received bug report from sender with no PlayerController.");
         return;
      }

      // Pass things along to the Bug Report Manager to handle
      BugReportManager.self.storeBugReportOnServer(_player, subject, message, ping, fps, screenshotBytes, screenResolution, operatingSystem, steamState, connectionToClient.address, deploymentId);
   }

   [Command]
   public void Cmd_BugReportScreenshot (long bugId, byte[] screenshotBytes) {
      // We need a player object
      if (_player == null) {
         D.warning("Received bug report from sender with no PlayerController.");
         return;
      }

      // Pass things along to the Bug Report Manager to handle
      BugReportManager.self.storeBugReportOnServerScreenshot(_player, bugId, screenshotBytes);
   }

   [Command]
   public void Cmd_SubmitComplaint (string username, string details, string chatLog, byte[] screenshotBytes, string machineIdentifier, int deploymentId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserInfo reportedUserInfo = DB_Main.getUserInfo(username);
         UserObjects sourceObjects = DB_Main.getUserObjects(_player.userId);

         if (reportedUserInfo == null) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.Target_ReceiveNormalChat("The player name is not valid", ChatInfo.Type.System);
            });
         } else if (reportedUserInfo.accountId == sourceObjects.accountId || reportedUserInfo.userId == sourceObjects.userInfo.userId) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.Target_ReceiveNormalChat("Report has not been submitted. You cannot report yourself.", ChatInfo.Type.System);
            });
         } else {
            DB_Main.saveComplaint(_player.userId, _player.accountId, _player.entityName, sourceObjects.accountEmail, connectionToClient.address,
               reportedUserInfo.userId, reportedUserInfo.accountId, username, details, reportedUserInfo.localPos.ToString(), reportedUserInfo.areaKey, chatLog, screenshotBytes, machineIdentifier, deploymentId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.Target_ReceiveNormalChat("Your report was submitted successfully", ChatInfo.Type.System);
            });
         }
      });
   }

   [Command]
   public void Cmd_SendRollOutput (int min, int max) {
      int random = Random.Range(min, max + 1);
      string message = _player.entityName + " has rolled " + random + " (" + min + " - " + max + ")";

      if (_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         // Send the result to all group members
         foreach (int userId in voyageGroup.members) {
            ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.General, userId, message);
         }
      } else {
         // If the player is not in a group, the result is sent only to him
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, message);
      }
   }

   [Command]
   public void Cmd_SendChat (string message, ChatInfo.Type chatType) {
      GuildIconData guildIconData = null;
      string guildIconDataString = "";
      bool muted = _player.isMuted();

      if (_player.guildId > 0) {
         guildIconData = new GuildIconData(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);
      }

      if (guildIconData != null) {
         guildIconDataString = GuildIconData.guildIconDataToString(guildIconData);
      }

      ChatInfo chatInfo = new ChatInfo(0, message, DateTime.UtcNow, chatType, _player.entityName, "", _player.userId, guildIconData, _player.isStealthMuted);

      // Replace bad words
      message = BadWordManager.ReplaceAll(message);

      // The player is muted (normal) and can't send messages. We notify the player about it
      if (muted && !_player.isStealthMuted) {
         _player.Target_ReceiveNormalChat($"Your message could not be sent because you have been muted until {Util.getTimeInEST(_player.muteExpirationDate)} EST.", ChatInfo.Type.System);
         return;
      }

      // Pass this message along to the relevant people
      if (chatType == ChatInfo.Type.Local || chatType == ChatInfo.Type.Emote) {
         _player.Rpc_ChatWasSent(chatInfo.chatId, message, chatInfo.chatTime.ToBinary(), chatType, guildIconDataString, _player.isStealthMuted);
      } else if (chatType == ChatInfo.Type.Global) {
         ServerNetworkingManager.self.sendGlobalChatMessage(chatInfo);
      } else if (chatType == ChatInfo.Type.Whisper) {
         string[] inputParts = message.Split(' ');
         if (inputParts.Length < 2) {
            _player.Target_ReceiveNormalChat($"Your whisper does not contain a userName and a message", ChatInfo.Type.System);
            return;
         }

         // Get the user name and message from the chat message
         string extractedUserName = inputParts[0];
         message = message.Remove(0, extractedUserName.Length + 1);

         chatInfo.text = message;
         chatInfo.recipient = extractedUserName;

         // Background thread
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Try to retrieve the destination user info
            UserInfo destinationUserInfo = DB_Main.getUserInfo(extractedUserName);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (destinationUserInfo == null) {
                  string errorMsg = "Recipient does not exist!";
                  _player.Target_ReceiveSpecialChat(_player.connectionToClient, chatInfo.chatId, errorMsg, "", "", chatInfo.chatTime.ToBinary(), ChatInfo.Type.Error, null, 0, _player.isStealthMuted);
                  return;
               }

               ServerNetworkingManager.self.sendSpecialChatMessage(destinationUserInfo.userId, chatInfo);
               _player.Target_ReceiveSpecialChat(_player.connectionToClient, chatInfo.chatId, message, chatInfo.sender, extractedUserName, chatInfo.chatTime.ToBinary(), chatInfo.messageType, chatInfo.guildIconData, chatInfo.senderId, _player.isStealthMuted);
            });
         });
      } else if (chatType == ChatInfo.Type.Group) {
         if (!_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
            return;
         }

         foreach (int userId in voyageGroup.members) {
            ServerNetworkingManager.self.sendSpecialChatMessage(userId, chatInfo);
         }
      } else if (chatType == ChatInfo.Type.Guild) {
         ServerNetworkingManager.self.sendGuildChatMessage(_player.guildId, chatInfo);

         if (_player.guildId == 0) {
            return;
         }
      } else if (chatType == ChatInfo.Type.Officer) {
         if (!_player.canPerformAction(GuildPermission.OfficerChat)) {
            return;
         }
         ServerNetworkingManager.self.sendSpecialChatMessage(_player.userId, chatInfo);

         // Background thread
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            GuildInfo guildInfo = DB_Main.getGuildInfo(_player.guildId);
            List<GuildRankInfo> rankInfo = DB_Main.getGuildRankInfo(_player.guildId);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (UserInfo userInfo in guildInfo.guildMembers) {
                  int permissions = userInfo.guildRankId == 0 ? int.MaxValue : rankInfo.Find(x => x.id == userInfo.guildRankId).permissions;
                  if (GuildRankInfo.canPerformAction(permissions, GuildPermission.OfficerChat) && _player.userId != userInfo.userId) {
                     ServerNetworkingManager.self.sendSpecialChatMessage(userInfo.userId, chatInfo);
                  }
               }
            });
         });
      }

      // Store chat message in database, if the player is not muted
      if (!_player.isStealthMuted) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.storeChatLog(_player.userId, _player.entityName, message, chatInfo.chatTime, chatType, connectionToClient.address);
         });
      }
   }

   [Command]
   public void Cmd_RequestShipAbilities (int shipID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         ShipInfo shipInfo = DB_Main.getShipInfo(shipID);
         if (shipInfo == null) {
            D.editorLog("Ship info does not exist for ship: " + shipID, Color.red);
            return;
         }
         if (shipInfo.shipAbilityXML == null || shipInfo.shipAbilityXML == "") {
            D.error("XML Data does not exist for Ship ID: " + shipID + " Creating new Data with Default Ability: " + ShipAbilityInfo.DEFAULT_ABILITY);
            int[] shipArray = new int[] { ShipAbilityInfo.DEFAULT_ABILITY };

            shipInfo.shipAbilities = new ShipAbilityInfo { ShipAbilities = shipArray };

            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(shipInfo.shipAbilities.GetType());
            var sb = new StringBuilder();
            using (var writer = System.Xml.XmlWriter.Create(sb)) {
               ser.Serialize(writer, shipInfo.shipAbilities);
            }

            string xmlString = sb.ToString();
            shipInfo.shipAbilityXML = xmlString;
            DB_Main.updateShipAbilities(shipID, xmlString);
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ShipAbilityInfo shipAbility = Util.xmlLoad<ShipAbilityInfo>(shipInfo.shipAbilityXML);
            Target_SetShipAbilities(_player.connectionToClient, shipAbility.ShipAbilities);
         });
      });
   }

   [TargetRpc]
   public void Target_SetShipAbilities (NetworkConnection connection, int[] abilityIds) {
      CannonPanel.self.setAbilityTab(abilityIds);
   }

   [TargetRpc]
   public void Target_ReceiveMapInfo (Map map) {
      AreaManager.self.storeAreaInfo(map);
   }

   [TargetRpc]
   public void Target_ReceiveAreaInfo (NetworkConnection connection, string areaKey, string baseMapAreaKey, int latestVersion, Vector3 mapPosition, MapCustomizationData customizations, Biome.Type biome) {
      // Check if we already have the Area created
      Area area = AreaManager.self.getArea(areaKey);

      if (area != null && area.version == latestVersion) {
         if (area.isInterior) {
            WeatherManager.self.setWeatherSimulation(WeatherEffectType.None);
         }

         // If we have the map already, hide loading screen
         PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.MapCreation);
         return;
      }

      // If our version is outdated (and we're not a server/host), then delete the old version
      if (area != null && area.version != latestVersion && !NetworkServer.active) {
         Destroy(area.gameObject);
      }

      // Check if we have stored map data for that area and version
      if (MapCache.hasMap(baseMapAreaKey, latestVersion)) {
         string mapData = MapCache.getMapData(baseMapAreaKey, latestVersion);

         // Update the file access time if it exists
         string path = MapCache.getMapPath(areaKey, latestVersion);
         if (File.Exists(path) && !string.IsNullOrEmpty(path)) {
            StartCoroutine(CO_UpdateFileTimestamp(path));
         }

         if (string.IsNullOrWhiteSpace(mapData)) {
            D.error($"MapCache has an empty entry: { baseMapAreaKey }-{latestVersion}");
         } else {
            MapManager.self.createLiveMap(areaKey, new MapInfo(baseMapAreaKey, mapData, latestVersion), mapPosition, customizations, biome);
            return;
         }
      }

      // If we don't have the latest version of the map, download it
      MapManager.self.downloadAndCreateMap(areaKey, baseMapAreaKey, latestVersion, mapPosition, customizations, biome);
   }

   private IEnumerator CO_UpdateFileTimestamp (string path) {
      bool successfullyWritten = false;
      while (!successfullyWritten) {
         try {
            // Write a white space to file to update write date so we know which files were used recently
            File.AppendAllText(path, " ");
            successfullyWritten = true;
         } catch {
            D.debug("File path currently being used, delaying write");
         }

         if (!successfullyWritten) {
            yield return 0;
         }
      }
   }

   [Command]
   public void Cmd_RequestGuildInfoFromServer () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Default to an empty guild info
         GuildInfo info = new GuildInfo();
         List<GuildRankInfo> rankInfo = null;

         // Only look up info if they're in a guild
         if (_player.guildId > 0) {
            info = DB_Main.getGuildInfo(_player.guildId);
            rankInfo = DB_Main.getGuildRankInfo(_player.guildId);
         }

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Determine if the members are online
            if (_player.guildId > 0) {
               foreach (UserInfo member in info.guildMembers) {
                  member.isOnline = ServerNetworkingManager.self.isUserOnline(member.userId);
               }
            }

            _player.rpc.Target_ReceiveGuildInfo(_player.connectionToClient, info, rankInfo != null ? rankInfo.ToArray() : null);
         });
      });
   }

   [Command]
   public void Cmd_RequestOptionsInfoFromServer () {
      // Look up the info on the instances
      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      int totalInstances = InstanceManager.self.getInstanceCount(_player.areaKey);

      // Send this along to the player
      _player.rpc.Target_ReceiveOptionsInfo(_player.connectionToClient, instance.numberInArea, totalInstances);
   }

   [Command]
   public void Cmd_OnPlayerLogOutSafely () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Destroy the player object right away
      MyNetworkManager.self.disconnectClient(_player.accountId);
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
            _player.rpc.Target_ReceiveTradeHistoryInfo(_player.connectionToClient, tradeList.ToArray(), pageIndex, totalTradeCount);
         });
      });
   }

   [Command]
   public void Cmd_RequestLeaderBoardsFromServer (LeaderBoardsManager.Period period) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      List<LeaderBoardInfo> farmingEntries;
      List<LeaderBoardInfo> sailingEntries;
      List<LeaderBoardInfo> exploringEntries;
      List<LeaderBoardInfo> tradingEntries;
      List<LeaderBoardInfo> craftingEntries;
      List<LeaderBoardInfo> miningEntries;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the leader boards from the database
         DB_Main.getLeaderBoards(period, out farmingEntries, out sailingEntries, out exploringEntries,
            out tradingEntries, out craftingEntries, out miningEntries);

         // Get the last calculation date of this period
         DateTime lastCalculationDate = DB_Main.getLeaderBoardEndDate(period);

         // Get the time left until recalculation
         TimeSpan timeLeftUntilRecalculation = LeaderBoardsManager.self.getTimeLeftUntilRecalculation(period, lastCalculationDate);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveLeaderBoards(_player.connectionToClient, period, timeLeftUntilRecalculation.TotalSeconds,
               farmingEntries.ToArray(), sailingEntries.ToArray(), exploringEntries.ToArray(), tradingEntries.ToArray(),
               craftingEntries.ToArray(), miningEntries.ToArray());
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
   public void Cmd_RequestSetHatId (int hatId) {
      requestSetHatId(hatId);
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
         UserInfo userInfo = JsonUtility.FromJson<UserInfo>(DB_Main.getUserInfoJSON(_player.userId.ToString()));

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

               // Update the shortcuts panel in case the deleted item was set in a slot
               sendItemShortcutList();
            } else {
               D.warning(string.Format("Player {0} tried to delete item {1}, but the DB query affected {2} rows?!", _player, itemId, rowsAffected));
            }
         });
      });
   }

   [Command]
   public void Cmd_UpdateItemShortcut (int slotNumber, int itemId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Check that the player has the item in his inventory
         Item inventoryItem = DB_Main.getItem(_player.userId, itemId);
         if (inventoryItem == null) {
            return;
         }

         // Get item about to be swapped
         List<ItemShortcutInfo> oldShortcutList = DB_Main.getItemShortcutList(_player.userId);
         Item swappedItem = DB_Main.getItem(_player.userId, oldShortcutList[slotNumber - 1].itemId);
         Item equippedItem = null;

         // Check if we have an equipped item of the type we are swapping with
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         switch (swappedItem.category) {
            case Item.Category.Armor:
               equippedItem = userObjects.armor;
               break;
            case Item.Category.Weapon:
               equippedItem = userObjects.weapon;
               break;
            case Item.Category.Hats:
               equippedItem = userObjects.hat;
               break;
         }

         // Get updated list of shortcuts
         DB_Main.updateItemShortcut(_player.userId, slotNumber, itemId);
         List<ItemShortcutInfo> shortcutList = DB_Main.getItemShortcutList(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the updated shortcuts to the client
            Target_ReceiveItemShortcuts(_player.connectionToClient, shortcutList.ToArray());

            // If the previous item was equipped, equip the new item
            if (equippedItem != null && equippedItem.id == swappedItem.id && equippedItem.category == swappedItem.category) {
               switch (swappedItem.category) {
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
            }
         });
      });
   }

   [ClientRpc]
   public void Rpc_PlayWarpEffect (Vector3 position) {
      // The local player plays this effect locally, no need to play it twice
      if (!_player.isLocalPlayer) {
         EffectManager.self.create(Effect.Type.Cannon_Smoke, position);
      }
   }

   [Command]
   public void Cmd_DeleteItemShortcut (int slotNumber) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteItemShortcut(_player.userId, slotNumber);
         List<ItemShortcutInfo> shortcutList = DB_Main.getItemShortcutList(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the updated shortcuts to the client
            Target_ReceiveItemShortcuts(_player.connectionToClient, shortcutList.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_RequestItemShortcutListFromServer () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      sendItemShortcutList();
   }

   [Server]
   public void sendItemShortcutList () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<ItemShortcutInfo> shortcutList = DB_Main.getItemShortcutList(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the updated shortcuts to the client
            Target_ReceiveItemShortcuts(_player.connectionToClient, shortcutList.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_SellCrops (int offerId, int amountToSell, Rarity.Type rarityToSellAt, string shopName) {
      _player.cropManager.sellCrops(offerId, amountToSell, rarityToSellAt, shopName);
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
               DB_Main.insertNewUsableItem(_player.userId, UsableItem.Type.HairDye, dyeBox.paletteName);
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
               string newPalette = usable.paletteNames;

               // The player shouldn't be on a ship
               if (body == null) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You need to be on land to use this item.");
                  });
                  return;
               }

               // Set the new hair color in the database
               DB_Main.setHairColor(body.userId, newPalette);

               // Delete the item now that we've used it
               DB_Main.deleteItem(body.userId, itemId);

               // Back to Unity
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  body.hairPalettes = newPalette;

                  // Update the body sheets on all clients
                  body.rpc.Rpc_UpdateHair(body.hairType, body.hairPalettes);

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
                  body.rpc.Rpc_UpdateHair(newHairType, body.hairPalettes);

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
         UserInfo userInfo = JsonUtility.FromJson<UserInfo>(DB_Main.getUserInfoJSON(_player.userId.ToString()));

         // Back to Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_UpdateGems(_player.connectionToClient, userInfo.gems);
         });
      });
   }

   [Command]
   public void Cmd_GetCropOffersForShop (string shopName) {
      bool useShopName = shopName == ShopManager.DEFAULT_SHOP_NAME ? false : true;

      // Get the current list of offers for the area
      List<CropOffer> list = new List<CropOffer>();
      if (useShopName) {
         list = ShopManager.self.getOffersByShopName(shopName);
      } else {
         D.error("A crop shop in area " + _player.areaKey + " has no defined shopName");
      }

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            string greetingText = "";
            if (useShopName) {
               ShopData shopData = new ShopData();
               shopData = ShopXMLManager.self.getShopDataByName(shopName);
               greetingText = shopData.shopGreetingText;
            }

            _player.rpc.Target_ReceiveOffers(_player.connectionToClient, gold, list.ToArray(), greetingText);
         });
      });
   }

   [Command]
   public void Cmd_GetItemsForArea (string shopName) {
      getItemsForArea(shopName);
   }

   [Command]
   public void Cmd_GetShipsForArea (string shopName) {
      getShipsForArea(shopName);
   }

   #region NPCQuest

   [Command]
   public void Cmd_RequestNPCQuestSelectionListFromServer (int npcId) {
      NPCData npcData = NPCManager.self.getNPCData(npcId);
      int questId = npcData.questId;
      QuestData questData = NPCQuestManager.self.getQuestData(questId);
      if (questData == null) {
         questData = NPCQuestManager.self.getQuestData(NPCQuestManager.BLANK_QUEST_ID);
         D.debug("Using a blank quest template due to npc data (" + npcId + ") having no assigned quest id");
      }
      List<QuestDataNode> xmlQuestNodeList = new List<QuestDataNode>(questData.questDataNodes);
      List<QuestDataNode> removeNodeList = new List<QuestDataNode>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);
         ShipInfo shipInfo = DB_Main.getShipInfoForUser(_player.userId);
         List<QuestStatusInfo> databaseQuestStatusList = DB_Main.getQuestStatuses(npcId, _player.userId);

         if (databaseQuestStatusList.Count < 1) {
            for (int i = 0; i < questData.questDataNodes.Length; i++) {
               DB_Main.updateQuestStatus(npcId, _player.userId, questId, i, 0);
            }
            databaseQuestStatusList = DB_Main.getQuestStatuses(npcId, _player.userId);
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (databaseQuestStatusList.Count > 0) {
               QuestStatusInfo highestQuestNodeValue = databaseQuestStatusList.OrderByDescending(_ => _.questNodeId).ToList()[0];
               foreach (QuestDataNode xmlQuestNode in xmlQuestNodeList) {
                  // Remove node if friendship level requirement is insufficient
                  if (xmlQuestNode.friendshipLevelRequirement > friendshipLevel) {
                     removeNodeList.Add(xmlQuestNode);
                  }

                  QuestStatusInfo databaseQuestStatus = databaseQuestStatusList.Find(_ => _.questNodeId == xmlQuestNode.questDataNodeId);
                  if (databaseQuestStatus == null) {
                     // Remove the quest that requires a certain level requirement
                     if (xmlQuestNode.questNodeLevelRequirement > highestQuestNodeValue.questNodeId) {
                        removeNodeList.Add(xmlQuestNode);
                     }
                  } else {
                     // If the quest requires a different quest to be unlocked first, remove it from the list to be provided to the player
                     bool hasQuestNodeRequirement = xmlQuestNode.questNodeLevelRequirement > -1;
                     if (hasQuestNodeRequirement) {
                        QuestStatusInfo requiredNodeStatus = databaseQuestStatusList.Find(_ => _.questNodeId == xmlQuestNode.questNodeLevelRequirement);
                        if (requiredNodeStatus.questDialogueId < xmlQuestNode.questDialogueNodes.Length) {
                           removeNodeList.Add(xmlQuestNode);
                        }
                     }

                     // Remove the quest that has a dialogue id greater than the dialogue length, meaning the quest is completed
                     if (databaseQuestStatus.questDialogueId >= xmlQuestNode.questDialogueNodes.Length) {
                        removeNodeList.Add(xmlQuestNode);
                     }

                     // Remove the quest that requires a certain level requirement
                     if (xmlQuestNode.questNodeLevelRequirement >= highestQuestNodeValue.questNodeId) {
                        removeNodeList.Add(xmlQuestNode);
                     }
                  }
               }
            }

            // Clear the invalid quests
            for (int i = 0; i < removeNodeList.Count; i++) {
               xmlQuestNodeList.Remove(removeNodeList[i]);
            }
            removeNodeList.Clear();

            // Retrieve other data only available server-side
            string greetingText = NPCManager.self.getGreetingText(npcId, friendshipLevel);
            Target_ReceiveNPCQuestTitles(_player.connectionToClient, questId, Util.serialize(xmlQuestNodeList), npcId, npcData.name, friendshipLevel, greetingText, shipInfo.shipName);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveNPCQuestTitles (NetworkConnection connection, int questId, string[] rawQuestNodeList, int npcId, string npcName, int friendshipLevel, string greetingText, string userFlagshipName) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      List<QuestDataNode> questDataNodes = Util.unserialize<QuestDataNode>(rawQuestNodeList);

      // Pass the data to the panel
      PanelManager.self.linkIfNotShowing(panel.type);
      panel.updatePanelWithQuestSelection(questId, questDataNodes.ToArray(), npcId, npcName, friendshipLevel, greetingText, userFlagshipName);
   }

   [Command]
   public void Cmd_SelectQuestTitle (int npcId, int questId, int questNodeId) {
      // Initialize key indexes
      int dialogueId = 0;

      // Initialize item dependencies
      List<int> itemStock = new List<int>();
      List<Item> itemRequirementList = new List<Item>();

      // Retrieve the full list of quests for this npc
      NPCData npcData = NPCManager.self.getNPCData(npcId);
      QuestData questData = new QuestData();
      if (npcData.questId > 0) {
         questData = NPCQuestManager.self.getQuestData(npcData.questId);
      } else {
         D.editorLog("Npc has no valid quest id", Color.red);
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the friendship level
         int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Initialize the relationship if it is the first time the player talks to this NPC
         if (friendshipLevel == -1) {
            friendshipLevel = 0;
            DB_Main.createNPCRelationship(npcId, _player.userId, friendshipLevel);
         }

         //List<QuestStatusInfo> questStatuses = DB_Main.getQuestStatuses(npcId, _player.userId);
         QuestStatusInfo statusInfo = DB_Main.getQuestStatus(npcId, _player.userId, questId, questNodeId);
         List<AchievementData> playerAchievements = DB_Main.getAchievementDataList(_player.userId);

         if (questData != null) {
            questId = questData.questId;
            if (questData.questDataNodes != null) {
               questNodeId = questData.questDataNodes[0].questDataNodeId;
               dialogueId = questData.questDataNodes[0].questDialogueNodes[0].dialogueIdIndex;

               if (statusInfo != null) {
                  questNodeId = statusInfo.questNodeId;
                  dialogueId = statusInfo.questDialogueId;

                  if (questData.questDataNodes.Length > questNodeId) {
                     QuestDataNode questDataNode = questData.questDataNodes[questNodeId];
                     if (questDataNode.questDialogueNodes.Length > dialogueId) {
                        QuestDialogueNode questDialogue = questDataNode.questDialogueNodes[dialogueId];
                        if (questDialogue.itemRequirements != null) {
                           foreach (Item item in questDialogue.itemRequirements) {
                              itemRequirementList.Add(item);
                           }
                        }
                     }
                  }
               }
            }
         }

         if (itemRequirementList.Count > 0) {
            List<Item> currentItems = DB_Main.getRequiredItems(itemRequirementList, _player.userId);
            itemStock = getItemStock(itemRequirementList, currentItems);
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Retrieve other data only available server-side
            string greetingText = NPCManager.self.getGreetingText(npcId, friendshipLevel);
            bool canOfferGift = NPCManager.self.canOfferGift(friendshipLevel);
            bool hasTradeGossipDialogue = NPCManager.self.hasTradeGossipDialogue(npcId);
            bool hasGoodbyeDialogue = NPCManager.self.hasGoodbyeDialogue(npcId);
            bool isHireable = NPCManager.self.isHireable(npcId);
            int landMonsterId = NPCManager.self.getNPCData(npcId).landMonsterId;
            NPC npc = NPCManager.self.getNPC(npcId);

            // Check if the player has completed the achievement requirement to hire this companion
            if (isHireable) {
               int hiringIdRequirement = NPCManager.self.getNPCData(npcId).achievementIdHiringRequirement;
               AchievementData achievementData = AchievementManager.self.getAchievementData(hiringIdRequirement);
               if (playerAchievements.Exists(_ => _.actionType == achievementData.actionType && _.tier == achievementData.tier)) {
                  D.editorLog("The user has attained the achievement: " + achievementData.achievementName, Color.blue);
               } else {
                  isHireable = false;
                  D.editorLog("The user has yet to attain the achievement: " + achievementData.achievementName, Color.red);
               }
            }

            // Send the data to the client
            Target_ReceiveNPCQuestList(_player.connectionToClient, npcId, npc.getName(),
               friendshipLevel, greetingText, canOfferGift, hasTradeGossipDialogue, hasGoodbyeDialogue,
               isHireable, landMonsterId, questId, questNodeId, dialogueId, itemStock.ToArray(), newJobXP);
         });
      });
   }

   [Command]
   public void Cmd_SelectNextNPCDialogue (int npcId, int questId, int questNodeId, int dialogueId) {
      int newQuestNodeId = questNodeId;
      int newDialogueId = dialogueId;

      List<int> itemStock = new List<int>();
      List<Item> itemRequirementList = new List<Item>();
      List<Item> itemsToDeduct = new List<Item>();

      QuestData questData = NPCQuestManager.self.getQuestData(questId);
      QuestDataNode questDataNode = questData.questDataNodes[questNodeId];
      if (questDataNode.questDialogueNodes.Length > dialogueId + 1) {
         // Gather the item list to deduct from the preview node
         QuestDialogueNode questDialogue = questDataNode.questDialogueNodes[newDialogueId];
         if (questDialogue.itemRequirements != null) {
            foreach (Item item in questDialogue.itemRequirements) {
               itemsToDeduct.Add(item);
            }
         }

         // Iterate to the next dialogue and extract the required item list
         newDialogueId++;
         questDialogue = questDataNode.questDialogueNodes[newDialogueId];
         if (questDialogue.itemRequirements != null) {
            foreach (Item item in questDialogue.itemRequirements) {
               itemRequirementList.Add(item);
            }
         }

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Update the quest status of the npc
            DB_Main.updateQuestStatus(npcId, _player.userId, questId, newQuestNodeId, newDialogueId);
            int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);
            Jobs newJobXP = DB_Main.getJobXP(_player.userId);

            // Deduct the items from the user id from the database
            if (itemsToDeduct.Count > 0) {
               List<Item> deductableItems = DB_Main.getRequiredItems(itemsToDeduct, _player.userId);

               // Override crafting sql id into the equipment id it will result into
               foreach (Item item in itemsToDeduct) {
                  if (item.category == Item.Category.Blueprint) {
                     CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(item.itemTypeId);
                     item.itemTypeId = craftingData.resultItem.itemTypeId;
                  }
               }

               foreach (Item item in deductableItems) {
                  Item targetItem = itemsToDeduct.Find(_ => _.category == item.category && _.itemTypeId == item.itemTypeId);
                  if (targetItem == null) {
                     D.debug("Missing item!: " + item.category + " : " + item.itemTypeId);
                     return;
                  }
                  int deductCount = targetItem.count;
                  DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, deductCount);
               }
            }

            // If the dialogue node has required items, get the current items of the user
            if (itemRequirementList.Count > 0) {
               List<Item> currentItems = DB_Main.getRequiredItems(itemRequirementList, _player.userId);
               itemStock = getItemStock(itemRequirementList, currentItems);
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Target_ReceiveNPCQuestNode(_player.connectionToClient, questId, newQuestNodeId, newDialogueId, friendshipLevel, true, true, itemStock.ToArray(), newJobXP);
            });
         });
      } else {
         newDialogueId++;
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Update the quest status of the npc
            DB_Main.updateQuestStatus(npcId, _player.userId, questId, newQuestNodeId, newDialogueId);

            int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);
            QuestDialogueNode questDialogue = questDataNode.questDialogueNodes[dialogueId];
            DB_Main.updateNPCRelationship(npcId, _player.userId, friendshipLevel + questDialogue.friendshipRewardPts);

            // Get the quest progress status of the user for this npc
            List<QuestStatusInfo> totalQuestStatus = DB_Main.getQuestStatuses(npcId, _player.userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Target_ReceiveProcessRewardToggle(_player.connectionToClient);
               bool hasCompletedAllQuests = true;

               int totalQuestNodes = questData.questDataNodes.Length;
               if (totalQuestStatus.Count < totalQuestNodes) {
                  hasCompletedAllQuests = false;
               } else {
                  // If any of the quest is in progress (dialogue current index less than total dialogues in a Quest Node)
                  foreach (QuestStatusInfo questStat in totalQuestStatus) {
                     QuestDataNode questNodeReference = questData.questDataNodes.ToList().Find(_ => _.questDataNodeId == questStat.questNodeId);
                     if (questStat.questDialogueId < questNodeReference.questDialogueNodes.Length) {
                        hasCompletedAllQuests = false;
                        break;
                     } 
                  }
               }

               if (hasCompletedAllQuests) {
                  Target_RemoveQuestNotice(_player.connectionToClient, npcId);
               }

               if (questDialogue.itemRewards != null) {
                  if (questDialogue.itemRewards.Length > 0) {
                     giveItemRewardsToPlayer(_player.userId, new List<Item>(questDialogue.itemRewards), true);
                  }
               }
               if (questDialogue.abilityIdReward > 0) {
                  giveAbilityToPlayer(_player.userId, new int[1] { questDialogue.abilityIdReward });
               }
            });
         });
      }
   }

   private List<int> getItemStock (List<Item> itemRequirements, List<Item> currentItems) {
      List<int> newItemStockList = new List<int>();
      foreach (Item item in itemRequirements) {
         Item currentItem = currentItems.Find(_ => _.category == item.category && _.itemTypeId == item.itemTypeId);

         if (item.category == Item.Category.Blueprint) {
            CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(item.itemTypeId);
            currentItem = currentItems.Find(_ => _.category == craftingData.resultItem.category && _.itemTypeId == craftingData.resultItem.itemTypeId);
         }
         if (currentItem == null) {
            newItemStockList.Add(0);
         } else {
            newItemStockList.Add(currentItem.count);
         }
      }
      return newItemStockList;
   }

   [Command]
   public void Cmd_RequestNPCTradeGossipFromServer (int npcId) {
      // TODO: Confirm if this is still necessary
      string gossip = "Empty Msg";

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Retrieve the friendship level
         int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the gossip text to the client
            Target_ReceiveNPCCustomDialogue(friendshipLevel, gossip, ClickableText.Type.ThankYou, null);
         });
      });
   }

   [Command]
   public void Cmd_RequestGiftOfferNPCTextFromServer (int npcId) {
      string npcText = NPCManager.self.getGiftOfferNPCText(npcId);

      // Send the gossip text to the client
      Target_ReceiveGiftOfferNPCText(npcText);
   }

   [Command]
   public void Cmd_GiftItemToNPC (int npcId, int itemId, int count) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the current friendship
         int currentFriendship = DB_Main.getFriendshipLevel(npcId, _player.userId);

         // Verify that the player has enough friendship to offer a gift
         if (!NPCManager.self.canOfferGift(currentFriendship)) {
            D.error(string.Format("User {0} does not have enough friendship with NPC {1} to offer a gift.", _player.userId, npcId));
            return;
         }

         // Retrieve the item from the player's inventory
         Item giftedItem = DB_Main.getItem(_player.userId, itemId);

         // Verify if the player has the item in his inventory
         if (giftedItem == null) {
            D.error(string.Format("User {0} does not have item {1} in his inventory.", _player.userId, itemId));
            return;
         }

         // Verify that there are enough items in the stack
         if (count > giftedItem.count) {
            D.error(string.Format("User {0} does not have enough quantity of item {1} in his inventory {2} < {3}.", _player.userId, itemId, giftedItem.count, count));
            return;
         }

         // Verify that the item is not equipped
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         UserInfo userInfo = userObjects.userInfo;

         if (userInfo.armorId == itemId || userInfo.weaponId == itemId) {
            D.error(string.Format("User {0} has item {1} equipped and cannot gift it.", _player.userId, itemId));
            return;
         }

         // Remove the item from the player's inventory
         DB_Main.decreaseQuantityOrDeleteItem(_player.userId, itemId, count);

         // Retrieve the friendship rewarded for this item
         int rewardedFriendship = NPCManager.self.getRewardedFriendshipForGift(npcId, giftedItem) * count;

         // Calculate the new friendship value
         int newFriendshipLevel = NPCFriendship.addToFriendship(currentFriendship, rewardedFriendship);

         // Update the npc relationship with the new friendship value
         if (newFriendshipLevel != currentFriendship) {
            DB_Main.updateNPCRelationship(npcId, _player.userId, newFriendshipLevel);
         }

         // Retrieve the correct npc text after being gifted the item
         string npcText;
         if (rewardedFriendship > 0) {
            npcText = NPCManager.self.getGiftLikedText(npcId);
         } else {
            npcText = NPCManager.self.getGiftNotLikedText(npcId);
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the npc answer to the client
            Target_ReceiveNPCCustomDialogue(newFriendshipLevel, npcText, ClickableText.Type.YouAreWelcome, null);

            // Registers the npc gift action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player, ActionType.NPCGift);
         });
      });
   }

   #endregion

   [Command]
   public void Cmd_RequestUserIdForFriendshipInvite (string friendUserName) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      string feedbackMessage = "";
      bool success = true;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Try to retrieve the user info
         UserInfo friendUserInfo = DB_Main.getUserInfo(friendUserName);
         if (friendUserInfo == null) {
            feedbackMessage = "The player " + friendUserName + " does not exist!";
            success = false;
         }

         // Check the request validity
         if (success) {
            success = isFriendshipRequestValid(friendUserInfo, ref feedbackMessage);
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (success) {
               // Send the user ID to the client
               Target_ReceiveUserIdForFriendshipInvite(_player.connectionToClient, friendUserInfo.userId, friendUserInfo.username);
            } else {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, feedbackMessage);
            }
         });
      });
   }

   [Command]
   public void Cmd_SendFriendshipInvite (int friendUserId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      string feedbackMessage = "";
      bool success = true;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Retrieve the friend user info
         UserInfo friendUserInfo = JsonUtility.FromJson<UserInfo>(DB_Main.getUserInfoJSON(friendUserId.ToString()));
         if (friendUserInfo == null) {
            D.error(string.Format("The user {0} does not exist.", friendUserId));
            return;
         }

         // Check the request validity
         success = isFriendshipRequestValid(friendUserInfo, ref feedbackMessage);

         if (success) {
            // Create the friendship in status 'invited', in both directions
            DB_Main.createFriendship(_player.userId, friendUserId, Friendship.Status.InviteSent, DateTime.UtcNow);
            DB_Main.createFriendship(friendUserId, _player.userId, Friendship.Status.InviteReceived, DateTime.UtcNow);

            // Prepare a feedback message
            feedbackMessage = "A friendship invitation has been sent to " + friendUserInfo.username + "!";
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (success) {
               // Let the player know that a friendship invitation has been sent
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, feedbackMessage);

               // Let the potential friend know that he has got invitation
               NetEntity friend = EntityManager.self.getEntity(friendUserId);
               if (friend) {
                  friend.rpc.checkForPendingFriendshipRequests();
               }
            } else {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, feedbackMessage);
            }
         });
      });
   }

   [Server]
   public bool isFriendshipRequestValid (UserInfo friendUserInfo, ref string feedbackMessage) {
      feedbackMessage = "";

      // Verify that the friend is not the user himself
      if (friendUserInfo.userId == _player.userId) {
         feedbackMessage = "You cannot invite yourself!";
         return false;
      }

      // Verify that there is no relationship with this friend
      FriendshipInfo info = DB_Main.getFriendshipInfo(_player.userId, friendUserInfo.userId);
      if (info != null) {
         switch (info.friendshipStatus) {
            case Friendship.Status.InviteSent:
               feedbackMessage = "You have already sent an invitation to " + friendUserInfo.username + "!";
               break;
            case Friendship.Status.InviteReceived:
               feedbackMessage = "You already have a pending invitation from " + friendUserInfo.username + "!";
               break;
            case Friendship.Status.Friends:
               feedbackMessage = "You are already friends with " + friendUserInfo.username + "!";
               break;
            default:
               D.warning(string.Format("The case {0} is not managed.", info.friendshipStatus));
               break;
         }
         return false;
      }

      // Verify that the number of friends is below the maximum
      if (!hasFreeFriendSlot(ref feedbackMessage)) {
         return false;
      }

      return true;
   }

   [Command]
   public void Cmd_RequestNPCQuestInArea () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }
      IEnumerable<NPCData> npcDataInArea = NPCManager.self.getNPCDataInArea(_player.areaKey).Where(c => c.questId > 0);
      List<int> npcIdList = new List<int>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (NPCData npcData in npcDataInArea) {
            List<QuestStatusInfo> databaseQuestStatus = DB_Main.getQuestStatuses(npcData.npcId, _player.userId);
            QuestData questData = NPCQuestManager.self.getQuestData(npcData.questId);

            if (questData != null) {
               // If there is no registered quest in the database, and if there are quest nodes for this NPC, add to quest notice
               if (databaseQuestStatus.Count < 1 && questData.questDataNodes.Length > 0) {
                  if (!npcIdList.Contains(npcData.npcId)) {
                     npcIdList.Add(npcData.npcId);
                  }
               } else {
                  foreach (QuestStatusInfo questStatus in databaseQuestStatus) {
                     if (questStatus.questDialogueId < questData.questDataNodes[questStatus.questNodeId].questDialogueNodes.Length) {
                        if (!npcIdList.Contains(npcData.npcId)) {
                           npcIdList.Add(npcData.npcId);
                        }
                     }
                  }
               }
            } else {
               D.debug("Error here! No quest data for quest" + " : " + npcData.questId);
            }
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (_player != null && _player.connectionToClient != null) {
               Target_ReceiveQuestNotifications(_player.connectionToClient, npcIdList.ToArray());
            }
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveQuestNotifications (NetworkConnection connection, int[] npcIds) {
      foreach (int npcId in npcIds) {
         NPC npcEntity = NPCManager.self.getNPC(npcId);
         npcEntity.questNotice.SetActive(true);
      }
   }

   [Command]
   public void Cmd_AcceptFriendshipInvite (int friendUserId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      string feedbackMessage = "";
      bool success = true;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Retrieve the user info of the friend
         UserInfo friendInfo = JsonUtility.FromJson<UserInfo>(DB_Main.getUserInfoJSON(friendUserId.ToString()));

         // Verify that there is a pending invite
         FriendshipInfo friendshipInfo = DB_Main.getFriendshipInfo(friendUserId, _player.userId);
         if (friendshipInfo == null) {
            feedbackMessage = "The friendship invitation does not exist.";
            success = false;
         }

         // Verify that the number of friends is below the maximum
         if (success) {
            success = hasFreeFriendSlot(ref feedbackMessage);
         }

         if (success) {
            // Verify that an invitation exists in the other direction
            friendshipInfo = DB_Main.getFriendshipInfo(_player.userId, friendUserId);
            if (friendshipInfo == null) {
               D.error(string.Format("Inconsistency in the friendship table: invitation from user {1} to {0} doesn't have an opposite from {0} to {1}.", _player.userId, friendUserId));
               return;
            }

            // Upgrade both existing records to a 'friend' status
            DB_Main.updateFriendship(friendUserId, _player.userId, Friendship.Status.Friends, DateTime.UtcNow);
            DB_Main.updateFriendship(_player.userId, friendUserId, Friendship.Status.Friends, DateTime.UtcNow);

            // Prepare a feedback message
            feedbackMessage = "You are now friends with " + friendInfo.username + "!";
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (success) {
               // Let the player know that he has a new friend
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.FriendshipInvitationAccepted, _player, feedbackMessage);

               // Let the new friend know that he was accepted
               NetEntity friend = EntityManager.self.getEntity(friendUserId);
               if (friend) {
                  requestFriendsListFromServer(friend);
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, friend, "You are now friends with " + _player.entityName + "!");
               }
            } else {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, feedbackMessage);
            }
         });
      });
   }

   [Server]
   public bool hasFreeFriendSlot (ref string feedbackMessage) {
      feedbackMessage = "";
      int friendCount;

      // Get the number of current friends for the user
      friendCount = DB_Main.getFriendshipInfoCount(_player.userId, Friendship.Status.Friends);

      // Verify if the user has reached the friend count limit
      if (friendCount >= Friendship.MAX_FRIENDS) {
         feedbackMessage = "You have reached the maximum number of friends!";
         return false;
      }

      return true;
   }

   [Command]
   public void Cmd_DeleteFriendship (int friendUserId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Delete the relationship: player -> friend
         DB_Main.deleteFriendship(_player.userId, friendUserId);

         // Delete the relationship: friend -> player
         DB_Main.deleteFriendship(friendUserId, _player.userId);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send confirmation of the deletion to the client
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.FriendshipDeleted, _player);

            // If friend is logged in, update his friends list
            NetEntity friend = EntityManager.self.getEntity(friendUserId);
            if (friend) {
               requestFriendsListFromServer(friend);
            }

         });
      });
   }

   [Command]
   public void Cmd_RequestFriendshipInfoFromServer (int pageNumber, int itemsPerPage, Friendship.Status friendshipStatus) {
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

         // Get the number of items
         int totalFriendInfoCount = DB_Main.getFriendshipInfoCount(_player.userId, friendshipStatus);

         // Calculate the maximum page number
         int maxPage = Mathf.CeilToInt((float) totalFriendInfoCount / itemsPerPage);
         if (maxPage == 0) {
            maxPage = 1;
         }

         // Clamp the requested page number to the max page - the number of items could have changed
         pageNumber = Mathf.Clamp(pageNumber, 1, maxPage);

         // Get the items from the database
         List<FriendshipInfo> friendshipInfoList = DB_Main.getFriendshipInfoList(_player.userId, friendshipStatus, pageNumber, itemsPerPage);

         // Get the number of friends
         int friendCount = 0;
         if (friendshipStatus != Friendship.Status.Friends) {
            friendCount = DB_Main.getFriendshipInfoCount(_player.userId, Friendship.Status.Friends);
         } else {
            friendCount = totalFriendInfoCount;
         }

         // Get the number of received (pending) friendship requests
         int pendingRequestCount = 0;
         if (friendshipStatus != Friendship.Status.InviteReceived) {
            pendingRequestCount = DB_Main.getFriendshipInfoCount(_player.userId, Friendship.Status.InviteReceived);
         } else {
            pendingRequestCount = totalFriendInfoCount;
         }

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Determine if the friends are online
            foreach (FriendshipInfo friend in friendshipInfoList) {
               friend.isOnline = ServerNetworkingManager.self.isUserOnline(friend.friendUserId);
            }

            Target_ReceiveFriendshipInfo(_player.connectionToClient, friendshipInfoList.ToArray(), friendshipStatus, pageNumber, totalFriendInfoCount, friendCount, pendingRequestCount);
         });
      });
   }

   [Server]
   public void requestFriendsListFromServer (NetEntity player) {
      if (player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the items from the database
         List<FriendshipInfo> friendshipInfoList = DB_Main.getFriendshipInfoList(player.userId, Friendship.Status.Friends, 1, 200);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Determine if the friends are online
            Target_ReceiveFriendsList(player.connectionToClient, friendshipInfoList.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_RequestFriendsListFromServer () {
      requestFriendsListFromServer(_player);
   }

   [Server]
   public void checkForPendingFriendshipRequests () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int pendingRequestCount = DB_Main.getFriendshipInfoCount(_player.userId, Friendship.Status.InviteReceived);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (pendingRequestCount > 0) {
               _player.Target_ReceiveFriendshipRequestNotification(_player.connectionToClient);
            }
         });
      });
   }

   [Command]
   public void Cmd_CreateMail (string recipientName, string mailSubject, string message, int[] attachedItemsIds, int[] attachedItemsCount) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Try to retrieve the recipient info
         UserInfo recipientUserInfo = DB_Main.getUserInfo(recipientName);
         if (recipientUserInfo == null) {
            sendError("The player " + recipientName + " does not exist!");
            return;
         }

         // Create the mail
         int mailId = createMailCommon(recipientUserInfo.userId, mailSubject, message, attachedItemsIds, attachedItemsCount);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (mailId >= 0) {
               // Let the player know that the mail was sent
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.MailSent, _player, "The mail has been successfully sent to " + recipientName + "!");

               if (attachedItemsIds.Length > 0) {
                  // Update the shortcuts panel in case the transferred items were set in slots
                  sendItemShortcutList();
               }
            }
         });
      });
   }

   // This function must be called from the background thread!
   [Server]
   private int createMailCommon (int recipientUserId, string mailSubject, string message, int[] attachedItemsIds, int[] attachedItemsCount) {
      // Verify that the number of attached items is below the maximum
      if (attachedItemsIds.Length > MailManager.MAX_ATTACHED_ITEMS) {
         D.error(string.Format("The mail from user {0} to recipient {1} has too many attached items ({2}).", _player.userId, recipientUserId, attachedItemsIds.Length));
         return -1;
      }

      // Verify the validity of the attached items
      for (int i = 0; i < attachedItemsIds.Length; i++) {
         Item item = DB_Main.getItem(_player.userId, attachedItemsIds[i]);

         if (item == null) {
            sendError("An item is not present in your inventory!");
            return -1;
         }

         if (item.count < attachedItemsCount[i]) {
            sendError("You don't have enough " + item.getName() + "!");
            return -1;
         }

         // Verify that the item is not equipped
         string userInfoString = DB_Main.getUserInfoJSON(_player.userId.ToString());
         UserInfo userInfo = JsonUtility.FromJson<UserInfo>(userInfoString);

         if (userInfo.armorId == item.id || userInfo.weaponId == item.id) {
            sendError("You cannot select an equipped item!");
            return -1;
         }
      }

      MailInfo mail = null;

      // Create the mail
      mail = new MailInfo(-1, recipientUserId, _player.userId, DateTime.UtcNow, false, mailSubject, message);
      mail.mailId = DB_Main.createMail(mail);

      if (mail.mailId == -1) {
         D.error(string.Format("Error when creating a mail from sender {0} to recipient {1}.", _player.userId, recipientUserId));
         sendError("An error occurred when creating a mail!");
         return -1;
      }

      // Attach the items
      for (int i = 0; i < attachedItemsIds.Length; i++) {
         // Retrieve the item from the player's inventory
         Item item = DB_Main.getItem(_player.userId, attachedItemsIds[i]);

         // Check if the item was correctly retrieved
         if (item == null) {
            D.warning(string.Format("Could not retrieve the item {0} attached to mail {1} of user {2}.", attachedItemsIds[i], mail.mailId, _player.userId));
            continue;
         }

         // Transfer the item from the user inventory to the mail
         DB_Main.transferItem(item, _player.userId, -mail.mailId, attachedItemsCount[i]);
      }

      return mail.mailId;
   }

   [Command]
   public void Cmd_RequestSingleMailFromServer (int mailId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Get the mail content
         MailInfo mail = DB_Main.getMailInfo(mailId);

         // Get the attached items
         List<Item> attachedItems = DB_Main.getItems(-mail.mailId, new Item.Category[] { Item.Category.None }, 1, MailManager.MAX_ATTACHED_ITEMS);

         // If the mail had not been read before, set it as read
         if (!mail.isRead) {
            DB_Main.updateMailReadStatus(mailId, true);
         }

         // Check if the user has unread mail
         bool hasUnreadMail = DB_Main.hasUnreadMail(_player.userId);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveSingleMail(_player.connectionToClient, mail, attachedItems.ToArray(), hasUnreadMail);
         });
      });
   }

   [Command]
   public void Cmd_PickUpAttachedItemFromMail (int mailId, int itemId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Verify that the mail has that item attached
         Item attachedItem = DB_Main.getItem(-mailId, itemId);

         if (attachedItem == null) {
            D.error(string.Format("Could not retrieve the item {0} from mail {1} for user {2}.", itemId, mailId, _player.userId));
            return;
         }

         // Transfer the item from the mail to the user inventory
         DB_Main.transferItem(attachedItem, -mailId, _player.userId, attachedItem.count);

         // Get the mail content
         MailInfo mail = DB_Main.getMailInfo(mailId);

         // Get the attached items
         List<Item> attachedItems = DB_Main.getItems(-mail.mailId, new Item.Category[] { Item.Category.None }, 1, MailManager.MAX_ATTACHED_ITEMS);

         // Check if the user has unread mail
         bool hasUnreadMail = DB_Main.hasUnreadMail(_player.userId);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveSingleMail(_player.connectionToClient, mail, attachedItems.ToArray(), hasUnreadMail);
         });
      });
   }

   [Command]
   public void Cmd_DeleteMail (int mailId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Delete the mail
         DB_Main.deleteMail(mailId);

         // Get the attached items
         List<Item> attachedItems = DB_Main.getItems(-mailId, new Item.Category[] { Item.Category.None }, 1, MailManager.MAX_ATTACHED_ITEMS);

         // Delete the attached items
         foreach (Item item in attachedItems) {
            DB_Main.deleteItem(-mailId, item.id);
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send confirmation of the deletion to the client
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.MailDeleted, _player);
         });
      });
   }

   [Command]
   public void Cmd_RequestMailListFromServer (int pageNumber, int itemsPerPage) {
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

         // Get the number of items
         int totalMailCount = DB_Main.getMailInfoCount(_player.userId);

         // Calculate the maximum page number
         int maxPage = Mathf.CeilToInt((float) totalMailCount / itemsPerPage);
         if (maxPage == 0) {
            maxPage = 1;
         }

         // Clamp the requested page number to the max page - the number of items could have changed
         pageNumber = Mathf.Clamp(pageNumber, 1, maxPage);

         // Get the items from the database
         List<MailInfo> mailList = DB_Main.getMailInfoList(_player.userId, pageNumber, itemsPerPage);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveMailList(_player.connectionToClient, mailList.ToArray(), pageNumber, totalMailCount);
         });
      });
   }

   [Server]
   public void checkForUnreadMails () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         bool hasUnreadMail = DB_Main.hasUnreadMail(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (hasUnreadMail) {
               _player.Target_ReceiveUnreadMailNotification(_player.connectionToClient);
            }
         });
      });
   }

   [Command]
   public void Cmd_BuyItem (int shopItemId, string shopName) {
      Item newItem = null;
      Item shopItem = ShopManager.self.getItem(shopItemId);

      if (shopItem == null) {
         D.warning("Couldn't find item for item id: " + shopItemId);
         return;
      }

      int price = shopItem.getSellPrice();

      // Make sure the player has enough money
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         // Make sure they have enough gold
         if (gold >= price) {
            DB_Main.addGold(_player.userId, -price);

            // Create a copy of the shop item and set its count to 1
            Item singleShopItem = shopItem.Clone();
            singleShopItem.count = 1;

            // Create a new instance of the item
            if (shopItem.category == Item.Category.CraftingIngredients) {
               newItem = DB_Main.createItemOrUpdateItemCount(_player.userId, singleShopItem);
            } else {
               newItem = DB_Main.createNewItem(_player.userId, singleShopItem);
            }
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

            // Registers the purchasing of generic item action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player, ActionType.BuyItem);

            // Registers the purchasing of equipment action to the achievement database for recording
            if (shopItem.category == Item.Category.Weapon) {
               AchievementManager.registerUserAchievement(_player, ActionType.WeaponBuy);
            }
            if (shopItem.category == Item.Category.Armor) {
               AchievementManager.registerUserAchievement(_player, ActionType.ArmorBuy);
            }
            if (shopItem.category == Item.Category.Hats) {
               AchievementManager.registerUserAchievement(_player, ActionType.HeadgearBuy);
            }

            string itemName = "";
            if (shopItem.category == Item.Category.CraftingIngredients) {
               itemName = CraftingIngredients.getName((CraftingIngredients.Type) shopItem.itemTypeId);
            } else {
               itemName = shopItem.getName();
            }

            // Let the client know that it was successful
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.StoreItemBought, _player, "You have purchased a " + itemName + "!");

            // Make sure their gold display gets updated
            getItemsForArea(shopName);
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

            // Registers the purchasing of ship action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player, ActionType.BuyShip);

            // Show a popup panel for the player
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ShipBought, _player);

            // TODO: Check if this is still necessary
            // Make sure their gold display gets updated
            //getShipsForArea(ShopManager.DEFAULT_SHOP_NAME);
         });
      });
   }

   #region Guilds

   [Command]
   public void Cmd_CreateGuild (string guildName, string iconBorder, string iconBackground, string iconSigil,
      string iconBackPalettes, string iconSigilPalettes) {
      if (_player.guildId > 0) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "You are already in a guild.");
         return;
      }

      if (!GuildManager.self.isGuildNameValid(guildName, out string errorMessage)) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, errorMessage);
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to create the guild in the database
         GuildInfo guildInfo = new GuildInfo(guildName, iconBorder, iconBackground, iconSigil,
            iconBackPalettes, iconSigilPalettes);
         int guildId = DB_Main.createGuild(guildInfo);

         // Assign the guild to the player
         if (guildId > 0) {
            DB_Main.assignGuild(_player.userId, guildId);
            DB_Main.assignRankGuild(_player.userId, 0);

            // Create basic ranks and assign "Guild Leader" position to player
            DB_Main.createRankGuild(GuildRankInfo.getDefaultOfficer(guildId));
            DB_Main.createRankGuild(GuildRankInfo.getDefaultMember(guildId));

            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.CreatedGuild, _player, "You have created the guild " + guildName + "!");

               // Assign the guild ID to the player
               _player.guildId = guildId;

               // Net entity _player are where the syncvars are stored for the player's guild icon
               _player.guildIconBackground = iconBackground;
               _player.guildIconBorder = iconBorder;
               _player.guildIconBackPalettes = iconBackPalettes;
               _player.guildIconSigil = iconSigil;
               _player.guildIconSigilPalettes = iconSigilPalettes;
               _player.Rpc_UpdateGuildIconDisplay(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);
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
      int guildId = _player.guildId;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int rankId = DB_Main.getGuildMemberRankId(_player.userId);
         if (rankId == 0 && DB_Main.getGuildInfo(guildId).guildMembers.Length > 1) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "Leader cannot leave guild if there are any members left!");
            });
            return;
         }

         // Close panel for guild leader after checking conditions
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (rankId == 0) {
               Target_ClosePanelAfterLeaveGuild();
            }
         });

         // Remove the player from the guild
         DB_Main.assignGuild(_player.userId, 0);
         DB_Main.assignRankGuild(_player.userId, -1);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Set player guild id as no guild
            _player.guildId = 0;

            // Set the syncvars to null and then update the guild icon
            _player.guildIconBackground = null;
            _player.guildIconBorder = null;
            _player.guildIconBackPalettes = null;
            _player.guildIconSigil = null;
            _player.guildIconSigilPalettes = null;
            _player.Rpc_UpdateGuildIconDisplay(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);

            // Delete the guild if it has no more members
            GuildManager.self.deleteGuildIfEmpty(guildId);

            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You have left your guild!");
         });
      });
   }

   [TargetRpc]
   public void Target_ClosePanelAfterLeaveGuild () {
      PanelManager.self.unlinkPanel();
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
   public void Cmd_UpdateRanksGuild (GuildRankInfo[] rankInfo) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<GuildRankInfo> guildRanks = DB_Main.getGuildRankInfo(_player.guildId);
         int permissions = DB_Main.getGuildMemberPermissions(_player.userId);
         int rankId = DB_Main.getGuildMemberRankId(_player.userId);
         int userRankPriority = rankId == 0 ? 0 : guildRanks.Find(rank => rank.rankId == rankId).rankPriority;

         // Check if user has sufficient permissions to edit ranks
         if (GuildRankInfo.canPerformAction(permissions, GuildPermission.EditRanks)) {
            List<GuildRankInfo> guildRanksInDB = DB_Main.getGuildRankInfo(rankInfo[0].guildId);

            foreach (GuildRankInfo info in rankInfo) {
               bool foundRank = false;
               foreach (GuildRankInfo rankInDB in guildRanksInDB) {
                  if (rankInDB.id == info.id) {
                     if (userRankPriority < rankInDB.rankPriority) {
                        DB_Main.updateRankGuild(info);
                     }
                     foundRank = true;
                     break;
                  }
               }

               if (!foundRank) {
                  DB_Main.createRankGuild(info);
               }
            }

            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // If user is currently online - update his permissions
               foreach (NetEntity player in MyNetworkManager.getPlayers()) {
                  if (player.guildId == _player.guildId) {
                     GuildRankInfo rank = guildRanks.Find(x => x.rankPriority == player.guildRankPriority);
                     if (rank != null) {
                        GuildRankInfo newRank = rankInfo.ToList().Find(x => x.id == rank.id);
                        if (newRank != null) {
                           player.guildRankPriority = newRank.rankPriority;
                           player.guildPermissions = newRank.permissions;
                        }
                     }
                  }
               }

               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.EditGuildRanks, _player, "You have modified guild ranks!");
            });
         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You have insufficient permissions to edit ranks!");
            });
         }
      });
   }

   [Command]
   public void Cmd_PromoteGuildMember (int userToPromoteId) {
      int promoterId = _player.userId;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<GuildRankInfo> guildRanks = DB_Main.getGuildRankInfo(_player.guildId);
         int rankId = DB_Main.getGuildMemberRankId(userToPromoteId);
         int promoterRankId = DB_Main.getGuildMemberRankId(promoterId);
         int permissions = DB_Main.getGuildMemberPermissions(promoterId);
         int userRankPriority = rankId == 0 ? 0 : guildRanks.Find(rank => rank.rankId == rankId).rankPriority;
         int promoterRankPriority = promoterRankId == 0 ? 0 : guildRanks.Find(x => x.rankId == promoterRankId).rankPriority;

         if (promoterRankPriority >= userRankPriority - 1) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot promote guild member to rank equal or higher than yours!");
            });
            return;
         }

         // Check if user has permissions to promote members
         if (GuildRankInfo.canPerformAction(permissions, GuildPermission.Promote)) {
            GuildRankInfo guildRankInfo = null;
            bool success = false;
            // Check if user to promote hasn't reached maximum rank
            if (userRankPriority > 1) {
               success = true;
               guildRankInfo = guildRanks.Find(rank => rank.rankPriority == userRankPriority - 1);
               int newRankId = guildRankInfo.id;
               DB_Main.assignRankGuild(userToPromoteId, newRankId);
            }

            string otherName = DB_Main.getUserName(userToPromoteId);

            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (success) {
                  // If user is currently online - update his permissions
                  ServerNetworkingManager.self.updateGuildMemberPermissions(userToPromoteId, guildRankInfo.rankPriority, guildRankInfo.permissions);

                  // Send the confirmation to all online guild members
                  ServerNetworkingManager.self.sendConfirmationMessageToGuild(ConfirmMessage.Type.GuildActionGlobal, _player.guildId, _player.entityName + " has promoted " + otherName + "!");

                  // Send a special confirmation to the user that performed the action
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionUpdate, _player, "");
               } else {
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "Guild member that you want to promote, has already reached maximum rank!");
               }
            });
         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You have insufficient rights to promote guild member!");
            });
         }
      });
   }

   [Command]
   public void Cmd_DemoteGuildMember (int userToDemoteId) {
      int promoterId = _player.userId;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<GuildRankInfo> guildRanks = DB_Main.getGuildRankInfo(_player.guildId);
         int rankId = DB_Main.getGuildMemberRankId(userToDemoteId);
         int promoterRankId = DB_Main.getGuildMemberRankId(promoterId);
         int permissions = DB_Main.getGuildMemberPermissions(promoterId);
         int userRankPriority = rankId == 0 ? 0 : guildRanks.Find(rank => rank.rankId == rankId).rankPriority;
         int promoterRankPriority = promoterRankId == 0 ? 0 : guildRanks.Find(x => x.rankId == promoterRankId).rankPriority;

         // Action on guild leader cannot be performed
         if (rankId == 0) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot perform action on Guild Leader!");
            });
            return;
         } else if (promoterRankPriority >= userRankPriority) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot demote guild member with higher or equal rank!");
            });
            return;
         }

         // Check if user has permissions to demote members
         if (GuildRankInfo.canPerformAction(permissions, GuildPermission.Demote)) {
            GuildRankInfo guildRankInfo = null;
            bool success = false;
            int lowestRank = int.MinValue;
            foreach (GuildRankInfo rankInfo in guildRanks) {
               if (rankInfo.rankPriority > lowestRank) {
                  lowestRank = rankInfo.rankPriority;
               }
            }

            // Check if user to demote hasn't reached lowest rank
            if (userRankPriority < lowestRank) {
               success = true;
               guildRankInfo = guildRanks.Find(rank => rank.rankPriority == userRankPriority + 1);
               int newRankId = guildRankInfo.id;
               DB_Main.assignRankGuild(userToDemoteId, newRankId);
            }

            string otherName = DB_Main.getUserName(userToDemoteId);

            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (success) {
                  // If user is currently online - update his permissions
                  ServerNetworkingManager.self.updateGuildMemberPermissions(userToDemoteId, guildRankInfo.rankPriority, guildRankInfo.permissions);

                  // Send the confirmation to all online guild members
                  ServerNetworkingManager.self.sendConfirmationMessageToGuild(ConfirmMessage.Type.GuildActionGlobal, _player.guildId, _player.entityName + " has demoted " + otherName + "!");

                  // Send a special confirmation to the user that performed the action
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionUpdate, _player, "");
               } else {
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "Guild member that you want to demote, has already reached lowest rank!");
               }
            });
         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You have insufficient rights to demote guild member!");
            });
         }
      });
   }

   [Command]
   public void Cmd_KickGuildMember (int userToKickId) {
      int kickerId = _player.userId;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<GuildRankInfo> guildRanks = DB_Main.getGuildRankInfo(_player.guildId);
         int rankId = DB_Main.getGuildMemberRankId(userToKickId);
         int kickerRankId = DB_Main.getGuildMemberRankId(kickerId);
         int permissions = DB_Main.getGuildMemberPermissions(kickerId);
         int userRankPriority = rankId == 0 ? 0 : guildRanks.Find(rank => rank.rankId == rankId).rankPriority;
         int kickerRankPriority = kickerRankId == 0 ? 0 : guildRanks.Find(x => x.rankId == kickerRankId).rankPriority;

         if (kickerRankPriority >= userRankPriority) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot kick guild member with higher or equal rank!");
            });
            return;
         }

         // Check if user has permissions to demote members
         if (GuildRankInfo.canPerformAction(permissions, GuildPermission.Kick)) {

            bool success = false;
            // Check if user to demote hasn't reached lowest rank
            if (rankId > 0) {
               success = true;
               DB_Main.assignGuild(userToKickId, 0);
               DB_Main.assignRankGuild(userToKickId, -1);

               // Set the syncvars to null and then update the guild icon
               _player.guildIconBackground = null;
               _player.guildIconBorder = null;
               _player.guildIconBackPalettes = null;
               _player.guildIconSigil = null;
               _player.guildIconSigilPalettes = null;
               _player.Rpc_UpdateGuildIconDisplay(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);

               // Remove past invite from kicked player. If he was kicked by accident, he can be invited back immediately
               GuildManager.self.removePastInvite(_player, userToKickId, DB_Main.getGuildInfo(_player.guildId));
            }

            string otherName = DB_Main.getUserName(userToKickId);

            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (success) {
                  // If user is currently online - update his guildId
                  ServerNetworkingManager.self.updateUserGuildId(userToKickId, 0);

                  // Send the confirmation to all online guild members
                  ServerNetworkingManager.self.sendConfirmationMessageToGuild(ConfirmMessage.Type.GuildActionGlobal, _player.guildId, _player.entityName + " has kicked " + otherName + " from guild!");

                  // Send a special confirmation to the user that performed the action
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionUpdate, _player, "");
               } else {
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot kick Guild Leader!");
               }
            });
         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You have insufficient rights to kick guild member!");
            });
         }
      });
   }

   [Command]
   public void Cmd_DeleteGuildRank (int rankId) {
      int deletingUserId = _player.userId;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<GuildRankInfo> guildRanks = DB_Main.getGuildRankInfo(_player.guildId);
         int userRankPriority = rankId == 0 ? 0 : guildRanks.Find(rank => rank.rankId == rankId).rankPriority;
         int deletingUserRankId = DB_Main.getGuildMemberRankId(deletingUserId);
         int deletingUserPriority = deletingUserRankId == 0 ? 0 : guildRanks.Find(x => x.rankId == deletingUserRankId).rankPriority;

         if (guildRanks.Count == 1) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot delete the only rank!");
            });
            return;
         }

         if (deletingUserPriority >= userRankPriority) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot delete rank higher or equal to yours!");
            });
            return;
         }

         int permissions = DB_Main.getGuildMemberPermissions(deletingUserId);
         // Check if user has permissions to demote members
         if (GuildRankInfo.canPerformAction(permissions, GuildPermission.EditRanks)) {
            int lowestPriority = int.MinValue;
            foreach (GuildRankInfo info in guildRanks) {
               if (info.rankPriority > lowestPriority) {
                  lowestPriority = info.rankPriority;
               }
            }

            string deletedRankName = guildRanks.Find(x => x.rankId == rankId).rankName;
            int oldId = guildRanks.Find(x => x.rankId == rankId).id;
            int newId = -1;

            // Deleted lowest rank
            if (userRankPriority != lowestPriority) {
               foreach (GuildRankInfo info in guildRanks) {
                  if (info.rankPriority > userRankPriority) {
                     info.rankPriority -= 1;
                  }
               }
            }
            for (int i = guildRanks.Count - 1; i > rankId - 1; i--) {
               guildRanks[i].rankId -= 1;
            }

            guildRanks.RemoveAt(rankId - 1);

            if (userRankPriority != lowestPriority) {
               newId = guildRanks.Find(x => x.rankPriority == userRankPriority).id;
            } else {
               newId = guildRanks.Find(x => x.rankPriority == lowestPriority - 1).id;
            }

            GuildRankInfo newRank = guildRanks.Find(x => x.id == newId);
            GuildInfo guildInfo = DB_Main.getGuildInfo(_player.guildId);
            foreach (UserInfo userInfo in guildInfo.guildMembers) {
               if (userInfo.guildRankId == oldId) {
                  DB_Main.assignRankGuild(userInfo.userId, newId);
               }
            }

            DB_Main.deleteGuildRank(_player.guildId, rankId);

            foreach (GuildRankInfo info in guildRanks) {
               DB_Main.updateRankGuildByID(info, info.id);
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Update the ranks and permissions of all online members
               ServerNetworkingManager.self.replaceGuildMembersRankPriority(_player.guildId, userRankPriority, newRank.rankPriority, newRank.permissions);

               // Send the confirmation to all online guild members
               ServerNetworkingManager.self.sendConfirmationMessageToGuild(ConfirmMessage.Type.GuildActionGlobal, _player.guildId, _player.entityName + " has deleted guild rank: " + deletedRankName);

               // Send a special confirmation to the user that performed the action
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionUpdate, _player, "");
            });

         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You have insufficient rights to delete rank!");
            });
         }
      });
   }

   [Command]
   public void Cmd_AppointGuildLeader (int newLeaderUserId) {
      int oldLeaderUserId = _player.userId;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         if (DB_Main.getGuildMemberRankId(oldLeaderUserId) != 0) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionLocal, _player, "You cannot appoint new guild leader!");
            });
            return;
         }

         List<GuildRankInfo> guildRanks = DB_Main.getGuildRankInfo(_player.guildId);
         string newLeaderName = DB_Main.getGuildInfo(_player.guildId).guildMembers.ToList().Find(x => x.userId == newLeaderUserId).username;
         DB_Main.assignRankGuild(newLeaderUserId, 0);
         DB_Main.assignRankGuild(oldLeaderUserId, guildRanks.Find(x => x.rankPriority == 1).id);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the confirmation to all online guild members
            ServerNetworkingManager.self.sendConfirmationMessageToGuild(ConfirmMessage.Type.GuildActionGlobal, _player.guildId, _player.entityName + " has appointed new leader: " + newLeaderName);

            // Send a special confirmation to the user that performed the action
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.GuildActionUpdate, _player, "");
         });
      });
   }

   #endregion

   [Command]
   public void Cmd_CraftItem (int blueprintItemId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the blueprint from the player's inventory
         Blueprint blueprint = (Blueprint) DB_Main.getItem(_player.userId, blueprintItemId);

         // Verify if the player has the blueprint in his inventory
         if (blueprint == null) {
            sendError("The blueprint is not present in your inventory!");
            return;
         }

         // Get the resulting item
         Item resultItem = new Item();
         if (blueprint.data.StartsWith(Blueprint.WEAPON_DATA_PREFIX)) {
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(blueprint.itemTypeId);
            resultItem = WeaponStatData.translateDataToWeapon(weaponData);
            resultItem.data = "";
         } else if (blueprint.data.StartsWith(Blueprint.ARMOR_DATA_PREFIX)) {
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(blueprint.itemTypeId);
            resultItem = ArmorStatData.translateDataToArmor(armorData);
            resultItem.data = "";
         }

         // Blueprint ID is now same to a craftable equipment ID
         resultItem.itemTypeId = blueprint.itemTypeId;

         // Initialize pallete names to empty string
         resultItem.paletteNames = "";

         // Get the crafting requirement data
         CraftableItemRequirements craftingRequirements = CraftingManager.self.getCraftableData(
               resultItem.category, resultItem.itemTypeId);

         // Verify if the crafting data exists
         if (craftingRequirements == null) {
            D.error(string.Format("The craftable data for item (category: {0}, item type: {1}) is missing.", resultItem.category, resultItem.itemTypeId));
            sendError("The blueprint data is missing!");
            return;
         }

         // Build the list of ingredients
         List<CraftingIngredients.Type> requiredIngredients = new List<CraftingIngredients.Type>();
         foreach (Item item in craftingRequirements.combinationRequirements) {
            requiredIngredients.Add((CraftingIngredients.Type) item.itemTypeId);
         }

         // Get the ingredients present in the user inventory
         List<Item> inventoryIngredients = DB_Main.getCraftingIngredients(_player.userId, requiredIngredients);

         // Compare the ingredients present in the inventory with the requisites
         foreach (Item requiredIngredient in craftingRequirements.combinationRequirements) {
            // Get the inventory ingredient, if there is any
            Item inventoryIngredient = inventoryIngredients.Find(s =>
               s.itemTypeId == requiredIngredient.itemTypeId);

            // Verify that there are enough items in the inventory stack
            if (inventoryIngredient == null || inventoryIngredient.count < requiredIngredient.count) {
               sendError("There are not enough ingredients to craft this item!");
               return;
            }
         }

         // Make sure that the crafted armor has an assigned palette value
         if (resultItem.category == Item.Category.Armor && string.IsNullOrEmpty(resultItem.paletteNames)) {
            resultItem.paletteNames = PaletteSwapManager.DEFAULT_ARMOR_PALETTE_NAMES;
         }

         // Add the result item to the user inventory
         Item craftedItem = DB_Main.createItemOrUpdateItemCount(_player.userId, resultItem);

         // If the item could not be created, stop the process
         if (craftedItem == null) {
            D.warning(string.Format("Could not create the crafted item in the user inventory. Blueprint id: {0}, item category: {1}, item type id: {2}.",
               blueprint.itemTypeId, resultItem.category, resultItem.itemTypeId));
            return;
         }

         // Decrease the quantity of each used ingredient in the user inventory
         foreach (Item requiredIngredient in craftingRequirements.combinationRequirements) {

            // Get the inventory ingredient
            Item inventoryIngredient = inventoryIngredients.Find(s =>
               s.itemTypeId == requiredIngredient.itemTypeId);

            // Decrease the quantity in the user inventory
            DB_Main.decreaseQuantityOrDeleteItem(_player.userId, inventoryIngredient.id, requiredIngredient.count);
         }

         // Add the crafting xp
         int xp = 10;
         DB_Main.addJobXP(_player.userId, Jobs.Type.Crafter, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // Registers the crafting action to the achievement database
            // AchievementManager.registerUserAchievement(_player, ActionType.Craft);

            // Let them know they gained experience
            _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Crafter, 0, true);

            // Update the available ingredients for the displayed blueprint
            Target_RefreshCraftingPanel(_player.connectionToClient);

            switch (craftedItem.category) {
               case Item.Category.Weapon:
                  WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(craftedItem.itemTypeId);
                  if (weaponData != null) {
                     craftedItem.data = WeaponStatData.serializeWeaponStatData(weaponData);
                  }
                  break;
               case Item.Category.Armor:
                  ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(craftedItem.itemTypeId);
                  if (armorData != null) {
                     craftedItem.data = ArmorStatData.serializeArmorStatData(armorData);
                  }
                  break;
            }

            // Display the reward panel
            Target_ReceiveItem(_player.connectionToClient, craftedItem);
         });
      });
   }

   [Command]
   public void Cmd_RequestVoyageListFromServer () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // If the user has already joined a voyage map, display a panel with the current map
      if (_player.tryGetVoyage(out Voyage voyage)) {
         Target_ReceiveCurrentVoyageInstance(_player.connectionToClient, voyage);
         return;
      }

      // Send the list to the client
      Target_ReceiveVoyageInstanceList(_player.connectionToClient, VoyageManager.self.getAllOpenVoyageInstances().ToArray());
   }

   [Command]
   public void Cmd_JoinVoyageAlone (int voyageId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Check the validity of the request
      if (!VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage)) {
         sendError("This voyage is not available anymore!");
         return;
      }

      if (VoyageGroupManager.isInGroup(_player)) {
         sendError("You must leave your group to join another voyage map!");
         return;
      }

      // Find the oldest incomplete quickmatch group in the given area
      VoyageGroupInfo voyageGroup = VoyageGroupManager.self.getBestGroupForQuickmatch(voyageId);

      // If no group is available, create a new one if possible
      if (voyageGroup == null) {
         // Check that the voyage is open for new groups
         if (!VoyageManager.isVoyageOpenToNewGroups(voyage)) {
            sendError("This voyage cannot be joined anymore!");
            return;
         }

         VoyageGroupManager.self.createGroup(_player, voyageId, false);
      } else {
         // Add the user to the group
         VoyageGroupManager.self.addUserToGroup(voyageGroup, _player);
      }

      // Warp the player to the voyage map immediately
      _player.spawnInNewMap(voyage.voyageId, voyage.areaKey, Direction.South);
   }

   [Command]
   public void Cmd_JoinVoyageWithGroup (int voyageId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Retrieve the voyage data
      if (!VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage)) {
         sendError("This voyage is not available anymore!");
         return;
      }

      // Verify the validity of the request
      if (!_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         sendError("Error when retrieving the voyage group!");
         return;
      }

      if (voyageGroup.members.Count > Voyage.getMaxGroupSize(voyage.difficulty)) {
         sendError("The maximum group size for this map difficulty is " + Voyage.getMaxGroupSize(voyage.difficulty) + "!");
         return;
      }

      if (voyageGroup.voyageId > 0) {
         sendError("You must leave your group to join another voyage map!");
         return;
      }

      if (!VoyageManager.isVoyageOpenToNewGroups(voyage)) {
         sendError("This voyage is full and cannot be joined anymore!");
         return;
      }

      // Set the voyage id in the group
      voyageGroup.voyageId = voyageId;
      VoyageGroupManager.self.updateGroup(voyageGroup);

      // Warp the player to the voyage map immediately
      _player.spawnInNewMap(voyage.voyageId, voyage.areaKey, Direction.South);
   }

   [Command]
   public void Cmd_CreatePrivateVoyageGroup () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Verify that the user is not already in a group
      if (VoyageGroupManager.isInGroup(_player)) {
         sendError("You must leave your current group before creating another!");
         return;
      }

      // Create a new private group and add the user to it
      VoyageGroupManager.self.createGroup(_player, -1, true);
   }

   [Command]
   public void Cmd_WarpToCurrentVoyageMap () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Retrieve the voyage data
      if (!_player.tryGetVoyage(out Voyage voyage)) {
         D.error(string.Format("Could not find the voyage for user {0}.", _player.userId));
         sendError("An error occurred when searching for the voyage.");
         return;
      }

      // Warp to the voyage area
      _player.spawnInNewMap(voyage.voyageId, voyage.areaKey, Direction.South);
   }

   [Command]
   public void Cmd_WarpToLeague () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Get the current instance
      Instance instance = InstanceManager.self.getInstance(_player.instanceId);

      // Check if the user has already joined a voyage
      if (_player.tryGetGroup(out VoyageGroupInfo voyageGroup) && _player.tryGetVoyage(out Voyage voyage)) {
         // Check if it is a league and we are in the lobby
         if (voyage.isLeague && VoyageManager.isLobbyArea(_player.areaKey)) {
            if (voyage.voyageId != instance.voyageId) {
               // If the group is already assigned to another voyage map, the next instance has already been created and we simply warp the player to it
               _player.spawnInNewMap(voyage.voyageId, voyage.areaKey, Direction.South);
            } else {
               // When entering the first league map (from the lobby), all the group members must be nearby
               List<string> missingMembersNames = new List<string>();
               List<int> missingMembersUserIds = new List<int>();
               foreach (int memberUserId in voyageGroup.members) {
                  // Try to find the entity
                  NetEntity memberEntity = EntityManager.self.getEntity(memberUserId);
                  if (memberEntity == null) {
                     missingMembersUserIds.Add(memberUserId);
                  } else {
                     if (Vector3.Distance(memberEntity.transform.position, _player.transform.position) > Voyage.LEAGUE_START_MEMBERS_MAX_DISTANCE) {
                        missingMembersNames.Add(memberEntity.entityName);
                     }
                  }
               }

               // Background thread
               UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                  // If some user entities are connected to another server or offline, read their names in the DB
                  foreach (int userId in missingMembersUserIds) {
                     missingMembersNames.Add(DB_Main.getUserName(userId));
                  }

                  // Back to the Unity thread
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     if (missingMembersNames.Count > 0) {
                        string allNames = "";
                        foreach (string name in missingMembersNames) {
                           allNames = allNames + name + ", ";
                        }
                        allNames = allNames.Substring(0, allNames.Length - 2);

                        ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "Some group members are missing: " + allNames);
                        Target_OnWarpFailed("Missing group members");
                        return;
                     }

                     // Create the first league map and warp the player to it
                     VoyageManager.self.createLeagueInstanceAndWarpPlayer(_player, instance.leagueIndex + 1, instance.biome);
                  });
               });
            }
         } else {
            Target_OnWarpFailed("League voyage and Lobby Area not Synced");

            // Display a panel with the current voyage map details
            Target_ReceiveCurrentVoyageInstance(_player.connectionToClient, voyage);
         }
         return;
      }

      // Create a new lobby with the same biome than the player's current instance (usually the town)
      VoyageManager.self.createLeagueInstanceAndWarpPlayer(_player, 0, instance.biome);
   }

   [Command]
   public void Cmd_ReturnToTownFromLeague () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.tryGetVoyage(out Voyage voyage)) {
         sendError("Error when exiting a league area. Could not find the voyage.");
         _player.spawnInNewMap(Area.STARTING_TOWN);
         return;
      }

      // Get the home town for the current voyage biome
      if (Area.homeTownForBiome.TryGetValue(voyage.biome, out string townAreaKey)) {
         if (Area.dockSpawnForBiome.TryGetValue(voyage.biome, out string dockSpawn)) {
            _player.spawnInNewMap(townAreaKey, dockSpawn, Direction.South);
         } else {
            _player.spawnInNewMap(townAreaKey);
         }
      } else {
         _player.spawnInNewMap(Area.STARTING_TOWN);
      }
   }

   [Command]
   public void Cmd_SendVoyageGroupInvitationToUser (string inviteeName) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      VoyageGroupManager.self.inviteUserToGroup(_player, inviteeName);
   }

   [Command]
   public void Cmd_AcceptGroupInvitation (int destinationGroupId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Verify the validity of the request
      if (!VoyageGroupManager.self.tryGetGroupById(destinationGroupId, out VoyageGroupInfo destinationGroup)) {
         sendError("The group does not exist!");
      }

      if (VoyageGroupManager.isGroupFull(destinationGroup)) {
         sendError("The group is full!");
         return;
      }

      if (_player.tryGetGroup(out VoyageGroupInfo currentGroup)) {
         if (currentGroup.groupId == destinationGroup.groupId) {
            // If the user already belongs to the destination group, do nothing
            return;
         } else {
            sendError("You must leave your current group before joining another!");
         }
      }

      // Add the user to the group
      VoyageGroupManager.self.addUserToGroup(destinationGroup, _player);
   }

   [Command]
   public void Cmd_RemoveUserFromGroup () {
      // Remove user from group
      removeUserFromGroup(_player);
   }

   [Command]
   public void Cmd_KickUserFromGroup (int playerToKick) {
      NetEntity targetPlayerToKick = EntityManager.self.getEntity(playerToKick);

      // If the player performing kick operation is not in a group, do nothing
      if (!_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         return;
      }

      // Remove user from group
      if (targetPlayerToKick) {
         if (targetPlayerToKick.hasAttackers()) {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "Failure to kick!");
            return;
         }
         VoyageGroupManager.self.removeUserFromGroup(voyageGroup, targetPlayerToKick);
      } else {
         VoyageGroupManager.self.removeUserFromGroup(voyageGroup, playerToKick);
      }

      // Warp player to other area and clean up panel only if player is currently online
      if (targetPlayerToKick) {
         // If the player is in a voyage area, warp him to the starting town
         if (VoyageManager.isVoyageOrLeagueArea(targetPlayerToKick.areaKey) || VoyageManager.isTreasureSiteArea(targetPlayerToKick.areaKey)) {
            targetPlayerToKick.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.South);
         }

         // Clean up panel on client
         Target_CleanUpVoyagePanelOnKick(targetPlayerToKick.connectionToClient);
      }
   }

   [Server]
   private void removeUserFromGroup (NetEntity playerToRemove) {
      if (playerToRemove == null) {
         D.warning("No player object found.");
         return;
      }

      // If the player is not in a group, do nothing
      if (!playerToRemove.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         return;
      }

      VoyageGroupManager.self.removeUserFromGroup(voyageGroup, playerToRemove);

      // If the player is in a voyage area, warp him to the starting town
      if (VoyageManager.isVoyageOrLeagueArea(playerToRemove.areaKey) || VoyageManager.isTreasureSiteArea(playerToRemove.areaKey)) {
         playerToRemove.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.South);
      }
   }

   [TargetRpc]
   private void Target_CleanUpVoyagePanelOnKick (NetworkConnection conn) {
      VoyageGroupPanel.self.cleanUpPanelOnKick();
   }

   [Server]
   public void sendVoyageGroupMembersInfo() {
      if (!VoyageGroupManager.self.tryGetGroupById(_player.voyageGroupId, out VoyageGroupInfo voyageGroup)) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Read in DB the group members info needed to display their portrait
         List<VoyageGroupMemberCellInfo> groupMembersInfo = VoyageGroupManager.self.getGroupMembersInfo(voyageGroup);

         if (groupMembersInfo == null) {
            D.debug("Error here! Group member info is missing for voyage group" + " : " + voyageGroup.voyageId + " : " + voyageGroup.creationDate);
         } else {
            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Target_ReceiveVoyageGroupMembers(_player.connectionToClient, groupMembersInfo.ToArray());

               // Also send updated info of this user to the other group members
               foreach (VoyageGroupMemberCellInfo cellInfo in groupMembersInfo) {
                  if (cellInfo.userId == _player.userId) {
                     ServerNetworkingManager.self.sendMemberPartialUpdateToGroup(_player.voyageGroupId, cellInfo.userId, cellInfo.userName, cellInfo.userXP, cellInfo.areaKey);
                  }
               }
            });
         }
      });
   }

   [ServerOnly]
   private bool canPlayerStayInVoyage () {
      // TODO: Setup a better solution for allowing users to bypass warping back to town
      // If player cant bypass restirctions, return them to town due to insufficient conditions being met
      if (EntityManager.self.canUserBypassWarpRestrictions(_player.userId)) {
         // Make sure the bypass warp can only be used once, admin command warp_anywhere must be triggered again for the bypass to be repeated
         EntityManager.self.removeBypassForUser(_player.userId);
         return true;
      }

      // If the player is not in a group, clear it from the netentity and redirect to the starting town
      if (!_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         _player.voyageGroupId = -1;

         // If player cant bypass restirctions, return them to town due to insufficient conditions being met
         if (!EntityManager.self.canUserBypassWarpRestrictions(_player.userId)) {
            D.debug("Returning player to town: Voyage Group does not Exist!");
            return false;
         }
      }

      // If the voyage is not defined or doesn't exists, or the group is not linked to this instance, redirect to the starting town
      if (voyageGroup.voyageId <= 0 || !_player.tryGetVoyage(out Voyage voyage) || _player.getInstance().voyageId != voyageGroup.voyageId) {
         if (voyageGroup.voyageId <= 0) {
            D.debug("Returning player to town: Voyage id is Invalid!");
         }
         if (!_player.tryGetVoyage(out voyage)) {
            D.debug("Returning player to town: Unable to fetch Voyage!");
         }
         if (_player.getInstance().voyageId != voyageGroup.voyageId) {
            D.debug("Returning player to town: Player voyage Id is incompatible with voyage group: {" + _player.getInstance().voyageId + "} : {" + voyageGroup.voyageId + "}");
         }

         // If player cant bypass restirctions, return them to town due to insufficient conditions being met
         if (!EntityManager.self.canUserBypassWarpRestrictions(_player.userId)) {
            return false;
         }
      }

      return true;
   }

   [Command]
   public void Cmd_OnClientFinishedLoadingArea () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Now that the area is loaded in the client, enable the player ship
      PlayerShipEntity playerShipEntity = _player.GetComponent<PlayerShipEntity>();
      if (playerShipEntity != null) {
         playerShipEntity.isDisabled = false;
      }

      // Verify the voyage consistency only if the user is in a voyage group or voyage area
      if (!VoyageManager.isVoyageOrLeagueArea(_player.areaKey) && !VoyageManager.isTreasureSiteArea(_player.areaKey)) {
         return;
      }

      // If the player does not meet the voyage requirements, warp the player back to town
      if (!canPlayerStayInVoyage()) {
         _player.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.South);
      }
   }

   [TargetRpc]
   public void Target_callInsufficientNotification (NetworkConnection connection, int npcID) {
      NPC npc = NPCManager.self.getNPC(npcID);
      Instantiate(PrefabsManager.self.insufficientPrefab, npc.transform.position + new Vector3(0f, .24f), Quaternion.identity);
   }

   #region Item Rewards for Combat and Crafting

   [TargetRpc]
   public void Target_ReceiveAbilityRewards (NetworkConnection connection, int[] abilityIds) {
      if (abilityIds.Length == 1) {
         BasicAbilityData abilityInfo = AbilityManager.self.allGameAbilities.Find(_ => _.itemID == abilityIds[0]);
         RewardManager.self.showAbilityRewardNotice(abilityInfo.itemName, abilityInfo.itemIconPath);
      }
   }

   [TargetRpc]
   public void Target_ReceiveItemList (NetworkConnection connection, Item[] itemList) {
      foreach (Item item in itemList) {
         if (item.category == Item.Category.Blueprint) {
            CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(item.itemTypeId);
            if (craftingData != null) {
               item.itemTypeId = craftingData.resultItem.itemTypeId;
            } else {
               D.debug("Failed to override crafting item type id: " + item.itemTypeId);
            }
         }
      }
      RewardManager.self.showItemsInRewardPanel(itemList.ToList());
   }

   [TargetRpc]
   public void Target_ReceiveItem (NetworkConnection connection, Item item) {
      RewardManager.self.showItemInRewardPanel(item);
   }

   #endregion

   [TargetRpc]
   public void Target_UpdateInventory (NetworkConnection connection) {
      ((InventoryPanel) PanelManager.self.get(Panel.Type.Inventory)).refreshPanel();
   }

   [TargetRpc]
   public void Target_CollectOre (NetworkConnection connection, int oreId, int effectId) {
      OreNode oreNode = OreManager.self.getOreNode(_player.areaKey, oreId);
      if (oreNode.orePickupCollection.Count > 0) {
         if (oreNode.orePickupCollection.ContainsKey(effectId)) {
            oreNode.tryToMineNodeOnClient();

            Destroy(oreNode.orePickupCollection[effectId].gameObject);
            oreNode.orePickupCollection.Remove(effectId);
         }
      }
   }

   [TargetRpc]
   public void Target_MineOre (NetworkConnection connection, int oreId, Vector2 startingPosition, Vector2 endPosition) {
      OreNode oreNode = OreManager.self.getOreNode(_player.areaKey, oreId);

      if (oreNode == null) {
         D.debug("Ore node is missing! No ore with id:{" + oreId + "} registered to OreManager");
         return;
      }
      oreNode.interactCount++;
      oreNode.updateSprite(oreNode.interactCount);
      ExplosionManager.createMiningParticle(oreNode.transform.position);

      if (oreNode.finishedMining()) {
         D.adminLog("Player has finished mining, spawning collectable ores:{" + oreNode.id + "}", D.ADMIN_LOG_TYPE.Mine);
         int randomCount = Random.Range(1, 3);

         for (int i = 0; i < randomCount; i++) {
            float randomSpeed = Random.Range(.8f, 1.2f);
            float angleOffset = Random.Range(-25, 25);
            processClientMineEffect(oreId, oreNode.transform.position, DirectionUtil.getDirectionFromPoint(startingPosition, endPosition), angleOffset, randomSpeed, i, _player.userId, _player.voyageGroupId);
         }
      }
   }

   [Command]
   public void Cmd_InteractOre (int oreId, Vector2 startingPosition, Vector2 endPosition) {
      Target_MineOre(_player.connectionToClient, oreId, startingPosition, endPosition);
   }

   public void processClientMineEffect (int oreId, Vector3 position, Direction direction, float angleOffset, float randomSpeed, int effectId, int ownerId, int voyageGroupId) {
      // Create object
      OreNode oreNode = OreManager.self.getOreNode(_player.areaKey, oreId);
      if (oreNode == null) {
         D.debug("Error! Missing Ore Node:{" + oreId + "}");
         return;
      }
      D.adminLog("Generating mine effect for ore:{" + oreNode.id + "}", D.ADMIN_LOG_TYPE.Mine);

      GameObject oreBounce = Instantiate(PrefabsManager.self.oreDropPrefab, oreNode.transform);
      OreMineEffect oreMine = oreBounce.GetComponent<OreMineEffect>();

      // Modify object transform
      oreBounce.transform.position = position;
      Vector3 currentAngle = oreBounce.transform.localEulerAngles;
      oreBounce.transform.localEulerAngles = new Vector3(currentAngle.x, currentAngle.y, currentAngle.z + angleOffset);
      oreMine.spriteRender.sprite = OreManager.self.getSprite(oreNode.oreType);

      // Modify object direction
      if (direction == Direction.East) {
         oreBounce.transform.localScale = new Vector3(-1, 1, 1);
      } else if (direction == Direction.North || direction == Direction.South) {
         oreMine.animator.SetFloat(MiningTrigger.FACING_KEY, (float) direction);
      }

      // Data setup
      oreMine.initData(ownerId, voyageGroupId, effectId, oreNode, randomSpeed);
   }

   [Command]
   public void Cmd_MineNode (int nodeId, int oreEffectId, int ownerId, int voyageGroupId) {
      OreNode oreNode = OreManager.self.getOreNode(_player.areaKey, nodeId);

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

      // If the user has no voyage group check the owner id, if it has a voyage group check the owner group id of the ore
      if ((voyageGroupId < 0 && _player.userId != ownerId) || (voyageGroupId >= 0 && _player.voyageGroupId != voyageGroupId)) {
         string message = "This does not belong to you!";
         _player.Target_FloatingMessage(_player.connectionToClient, message);
         return;
      }

      // Add the user ID to the list
      oreNode.userIds.Add(_player.userId);

      // Build the list of rewarded items
      List<Item> rewardedItems = new List<Item>();
      CraftingIngredients.Type ingredientType = CraftingIngredients.Type.None;
      switch (oreNode.oreType) {
         case OreNode.Type.Gold:
            ingredientType = CraftingIngredients.Type.Gold_Ore;
            break;
         case OreNode.Type.Silver:
            ingredientType = CraftingIngredients.Type.Silver_Ore;
            break;
         case OreNode.Type.Iron:
            ingredientType = CraftingIngredients.Type.Iron_Ore;
            break;
      }

      Item oreItem = new Item {
         category = Item.Category.CraftingIngredients,
         count = 1,
         itemTypeId = (int) ingredientType
      };
      oreItem.itemName = CraftingIngredients.getName(ingredientType);
      oreItem.iconPath = CraftingIngredients.getIconPath(ingredientType);
      rewardedItems.Add(oreItem);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Add the mining xp
         int xp = 10;
         DB_Main.addJobXP(_player.userId, Jobs.Type.Miner, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Gather voyage group info
         List<NetworkBehaviour> networkBehaviorList = InstanceManager.self.getInstance(_player.instanceId).getEntities();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Registers the Ore mining success action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player, ActionType.MineOre);
            AchievementManager.registerUserAchievement(_player, ActionType.OreGain, 1);

            // Grant the rewards to the user without showing reward panel
            giveItemRewardsToPlayer(_player.userId, rewardedItems, false);

            // Provide item data reward to the player
            _player.Target_GainedItem(_player.connectionToClient, rewardedItems[0].iconPath, rewardedItems[0].itemName, Jobs.Type.Miner, xp, rewardedItems[0].count);

            // Provide exp to the user
            _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Miner, 0, false);

            // Let them know they gained experience
            Target_CollectOre(_player.connectionToClient, nodeId, oreEffectId);
         });
      });
   }

   [Server]
   public void giveAbilityToPlayer (int userID, int[] abilityIds) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Create or update the database ability
         foreach (int abilityId in abilityIds) {
            if (!DB_Main.hasAbility(_player.userId, abilityId)) {
               BasicAbilityData cachedAbilityData = AbilityManager.self.allGameAbilities.Find(_ => _.itemID == abilityId);
               AbilitySQLData abilityData = new AbilitySQLData {
                  abilityID = cachedAbilityData.itemID,
                  abilityLevel = 1,
                  abilityType = cachedAbilityData.abilityType,
                  description = cachedAbilityData.itemDescription,
                  equipSlotIndex = -1,
                  name = cachedAbilityData.itemName
               };
               DB_Main.updateAbilitiesData(userID, abilityData);
            } else {
               D.editorLog("The player already has the ability: " + abilityId, Color.red);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveAbilityRewards(_player.connectionToClient, abilityIds);
         });
      });
   }

   [Server]
   private void giveItemRewardsToPlayer (int userID, List<Item> rewardList, bool showPanel) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Create or update the database item
         foreach (Item item in rewardList) {
            Item newDatabaseItem = new Item { category = Item.Category.Blueprint, count = 1, data = "" };
            if (item.category == Item.Category.Blueprint) {
               CraftableItemRequirements itemCache = CraftingManager.self.getCraftableData(item.itemTypeId);
               if (itemCache == null) {
                  D.debug("Failed to get crafting data of itemType: " + item.itemTypeId);
                  return;
               }

               switch (itemCache.resultItem.category) {
                  case Item.Category.Weapon:
                     newDatabaseItem.data = Blueprint.WEAPON_DATA_PREFIX;
                     item.data = Blueprint.WEAPON_DATA_PREFIX;
                     break;
                  case Item.Category.Armor:
                     newDatabaseItem.data = Blueprint.ARMOR_DATA_PREFIX;
                     item.data = Blueprint.ARMOR_DATA_PREFIX;
                     break;
                  case Item.Category.Hats:
                     newDatabaseItem.data = Blueprint.HAT_DATA_PREFIX;
                     item.data = Blueprint.HAT_DATA_PREFIX;
                     break;
               }
               newDatabaseItem.itemTypeId = itemCache.resultItem.itemTypeId;
            } else {
               newDatabaseItem = item;
            }

            // Make sure that the loot bag armor has an assigned palette value
            if (item.category == Item.Category.Armor && string.IsNullOrEmpty(item.paletteNames)) {
               item.paletteNames = PaletteSwapManager.DEFAULT_ARMOR_PALETTE_NAMES;
            }
            DB_Main.createItemOrUpdateItemCount(userID, newDatabaseItem);
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Calls Reward Popup
            if (showPanel) {
               Target_ReceiveItemList(_player.connectionToClient, rewardList.ToArray());
            }
         });
      });
   }

   [Server]
   private async Task giveItemRewardsToPlayer (int userID, List<ItemInstance> rewardList, bool showPanel) {
      // Assign item id for the reward
      foreach (ItemInstance item in rewardList) {
         item.ownerUserId = userID;
      }

      // Add all items to database in parallel
      await Task.WhenAll(rewardList.Select(async item => await DB_Main.execAsync((cmd) => DB_Main.createOrAppendItemInstance(cmd, item))));

      // Show panel to player if needed
      if (showPanel) {
         Target_ReceiveItemRewardList(rewardList.ToArray());
      }
   }

   [Command]
   public void Cmd_OpenChest (int chestId) {
      TreasureChest chest = TreasureManager.self.getChest(chestId);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         TreasureStateData interactedTreasure = DB_Main.getTreasureStateForChest(_player.userId, chest.chestSpawnId, _player.areaKey);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (interactedTreasure == null) {
               processChestRewards(chestId);
            } else {
               chest.userIds.Add(_player.userId);

               // Display empty open chest
               Target_OpenChest(_player.connectionToClient, new Item { category = Item.Category.None, itemTypeId = -1 }, chest.id);
            }
         });
      });
   }

   [Server]
   private void processChestRewards (int chestId) {
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
         return;
      }

      // Add the user ID to the list
      chest.userIds.Add(_player.userId);

      // Check what we're going to give the user
      Item item = chest.getContents();

      // Make sure that the treasure chest looted armor has an assigned palette value
      if (item.category == Item.Category.Armor && string.IsNullOrEmpty(item.paletteNames)) {
         item.paletteNames = PaletteSwapManager.DEFAULT_ARMOR_PALETTE_NAMES;
      }

      // Add it to their inventory
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         item = DB_Main.createItemOrUpdateItemCount(_player.userId, item);

         // Update the treasure chest status if is opened
         DB_Main.updateTreasureStatus(_player.userId, chest.chestSpawnId, _player.areaKey);
      });

      // Registers the interaction of treasure chests to the achievement database for recording
      AchievementManager.registerUserAchievement(_player, ActionType.OpenTreasureChest);

      // Send it to the specific player that opened it
      Target_OpenChest(_player.connectionToClient, item, chest.id);
   }

   #region Spawn Sea Entities

   [Command]
   public void Cmd_SpawnPirateShip (Vector2 spawnPosition, int guildID) {
      BotShipEntity bot = Instantiate(PrefabsManager.self.botShipPrefab, spawnPosition, Quaternion.identity);
      bot.instanceId = _player.instanceId;
      bot.facing = Util.randomEnum<Direction>();
      bot.areaKey = _player.areaKey;

      // Choose ship type at random
      List<Ship.Type> shipTypes = Enum.GetValues(typeof(Ship.Type)).Cast<Ship.Type>().ToList();
      shipTypes.Remove(Ship.Type.None);
      bot.shipType = shipTypes[Random.Range(0, shipTypes.Count)];

      bot.speed = Ship.getBaseSpeed(bot.shipType);
      bot.attackRangeModifier = Ship.getBaseAttackRange(bot.shipType);

      // Assign ship size to spawned ship
      ShipData shipData = ShipDataManager.self.getShipData(bot.shipType);
      bot.shipSize = shipData.shipSize;

      // Add ship to pirate guild or privateer guild
      if (guildID == BotShipEntity.PIRATES_GUILD_ID) {
         bot.entityName = "Pirate";
         bot.guildId = BotShipEntity.PIRATES_GUILD_ID;
      } else if (guildID == BotShipEntity.PRIVATEERS_GUILD_ID) {
         bot.entityName = "Privateer";
         bot.guildId = BotShipEntity.PRIVATEERS_GUILD_ID;
      }

      // Set up the movement route
      // Area area = AreaManager.self.getArea(bot.areaType);
      // bot.route = Instantiate(PrefabsManager.self.figureEightRoutePrefab, spawnPosition, Quaternion.identity, area.transform);

      InstanceManager.self.addSeaMonsterToInstance(bot, InstanceManager.self.getInstance(_player.instanceId));

      // Spawn the bot on the Clients
      NetworkServer.Spawn(bot.gameObject);
   }

   [Server]
   public void SpawnBossChild (Vector2 spawnPosition, uint parentEntityID, int xVal, int yVal, int variety, SeaMonsterEntity.Type enemyType) {
      SeaMonsterEntity bot = Instantiate(PrefabsManager.self.seaMonsterPrefab, spawnPosition, Quaternion.identity);
      bot.instanceId = _player.instanceId;
      bot.facing = Util.randomEnum<Direction>();
      bot.areaKey = _player.areaKey;
      bot.entityName = enemyType.ToString();
      bot.monsterType = enemyType;
      bot.distanceFromSpawnPoint = new Vector2(xVal, yVal);
      bot.variety = (variety);

      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      SeaMonsterEntity parentEntity = null;
      foreach (var temp in instance.entities) {
         if (temp != null) {
            if (temp.netId == parentEntityID) {
               parentEntity = temp.GetComponent<SeaMonsterEntity>();
            }
         }
      }

      bot.seaMonsterParentEntity = parentEntity;
      parentEntity.seaMonsterChildrenList.Add(bot);

      instance.entities.Add(bot);

      // Spawn the bot on the Clients
      NetworkServer.Spawn(bot.gameObject);
   }

   [Command]
   public void Cmd_SpawnBossParent (Vector2 spawnPosition, SeaMonsterEntity.Type enemyType) {
      Area area = AreaManager.self.getArea(_player.areaKey);
      if (area == null) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "GraphPath is not ready on server yet!");
         return;
      }

      if (area.getGraph() == null) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "GraphPath is not ready on server yet!");
         return;
      }

      SeaMonsterEntity bot = Instantiate(PrefabsManager.self.seaMonsterPrefab, spawnPosition, Quaternion.identity);
      bot.instanceId = _player.instanceId;
      bot.facing = Util.randomEnum<Direction>();
      bot.areaKey = _player.areaKey;
      bot.entityName = enemyType.ToString();
      bot.monsterType = enemyType;

      // Spawn the bot on the Clients
      NetworkServer.Spawn(bot.gameObject);

      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      instance.entities.Add(bot);

      float distanceGap = .25f;
      float diagonalDistanceGap = .35f;
      uint parentID = bot.netIdent.netId;

      SpawnBossChild(spawnPosition + new Vector2(distanceGap, -distanceGap), parentID, 1, -1, 1, SeaMonsterEntity.Type.Horror_Tentacle);
      SpawnBossChild(spawnPosition + new Vector2(-distanceGap, -distanceGap), parentID, -1, -1, 0, SeaMonsterEntity.Type.Horror_Tentacle);

      SpawnBossChild(spawnPosition + new Vector2(distanceGap, distanceGap), parentID, 1, 1, 1, SeaMonsterEntity.Type.Horror_Tentacle);
      SpawnBossChild(spawnPosition + new Vector2(-distanceGap, distanceGap), parentID, -1, 1, 0, SeaMonsterEntity.Type.Horror_Tentacle);

      SpawnBossChild(spawnPosition + new Vector2(-diagonalDistanceGap, 0), parentID, -1, 0, 1, SeaMonsterEntity.Type.Horror_Tentacle);
      SpawnBossChild(spawnPosition + new Vector2(diagonalDistanceGap, 0), parentID, 1, 0, 0, SeaMonsterEntity.Type.Horror_Tentacle);
   }

   [Command]
   public void Cmd_SpawnSeaMonster (Vector2 spawnPosition, SeaMonsterEntity.Type enemyType) {
      SeaMonsterEntity bot = Instantiate(PrefabsManager.self.seaMonsterPrefab, spawnPosition, Quaternion.identity);
      bot.instanceId = _player.instanceId;
      bot.facing = Util.randomEnum<Direction>();
      bot.areaKey = _player.areaKey;
      bot.monsterType = enemyType;
      bot.entityName = enemyType.ToString();

      // Spawn the bot on the Clients
      NetworkServer.Spawn(bot.gameObject);

      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      instance.entities.Add(bot);
   }

   [Command]
   public void Cmd_SpawnSeaMine (Vector2 spawnPosition) {
      SeaMine seaMine = Instantiate(PrefabsManager.self.seaMinePrefab, spawnPosition, Quaternion.identity);
      seaMine.instanceId = _player.instanceId;

      NetworkServer.Spawn(seaMine.gameObject);
   }

   #endregion

   [Command]
   public void Cmd_InviteToPvp (int inviteeUserId) {
      BodyEntity inviteeEntity = BodyManager.self.getBody(inviteeUserId);
      if (inviteeEntity != null) {
         if (inviteeEntity.areaKey == _player.areaKey) {
            inviteeEntity.rpc.Target_ReceivePvpInvite(inviteeEntity.connectionToClient, _player.userId, _player.entityName);
         } else {
            _player.Target_ReceiveNormalChat("Failed to invite player, player is in a different area!", ChatInfo.Type.System);
         }
      } else {
         D.debug("Cant find Invitee user: " + inviteeUserId);
      }
   }

   [TargetRpc]
   public void Target_ReceivePvpInvite (NetworkConnection connection, int inviterUserId, string inviterName) {
      PvpInviteScreen.self.activate(inviterName);
      PvpInviteScreen.self.acceptButton.onClick.AddListener(() => {
         Global.player.rpc.Cmd_AcceptPvpInvite(inviterUserId);
         PvpInviteScreen.self.hide();
         PvpInviteScreen.self.acceptButton.onClick.RemoveAllListeners();
      });

      PvpInviteScreen.self.refuseButton.onClick.AddListener(() => {
         Cmd_RefusePvpInvite(inviterUserId);
         PvpInviteScreen.self.hide();
         PvpInviteScreen.self.refuseButton.onClick.RemoveAllListeners();
      });
   }

   [Command]
   public void Cmd_RefusePvpInvite (int inviterUserId) {
      BodyEntity inviterUser = BodyManager.self.getBody(inviterUserId);
      if (inviterUser != null) {
         inviterUser.Target_ReceiveNormalChat("{" + inviterUser.entityName + "} refused your invite.", ChatInfo.Type.System);
      }
   }

   [Command]
   public void Cmd_AcceptPvpInvite (int inviterUserId) {
      List<BattlerInfo> rightBattlersInfo = new List<BattlerInfo>();
      List<BattlerInfo> leftBattlersInfo = new List<BattlerInfo>();

      BodyEntity inviterEntity = BodyManager.self.getBody(inviterUserId);
      if (inviterEntity == null) {
         _player.Target_ReceiveNormalChat("Failed to initiate battle, players are in a different area!", ChatInfo.Type.System);
         return;
      }
      if (inviterEntity.areaKey != _player.areaKey) {
         _player.Target_ReceiveNormalChat("Failed to initiate battle, players are in a different area!", ChatInfo.Type.System);
         inviterEntity.Target_ReceiveNormalChat("Failed to initiate battle, players are in a different area!", ChatInfo.Type.System);
         return;
      }

      BattlerInfo newInfo = new BattlerInfo {
         enemyType = Enemy.Type.PlayerBattler,
         battlerName = inviterEntity.entityName,
         battlerType = BattlerType.PlayerControlled
      };
      leftBattlersInfo.Add(newInfo);
      processTeamBattle(leftBattlersInfo.ToArray(), rightBattlersInfo.ToArray(), 0, false);
   }

   [Command]
   public void Cmd_StartNewTeamBattle (BattlerInfo[] defenders, BattlerInfo[] attackers) {
      // Checks if the user is an admin  
      if (!_player.isAdmin()) {
         D.warning("You are not at admin! Denying access to team combat simulation");
         return;
      }
      processTeamBattle(defenders, attackers, 0, true);
   }

   private void processTeamBattle (BattlerInfo[] defenders, BattlerInfo[] attackers, uint netId, bool createNewEnemy = false) {
      bool isPvpBattle = true;

      foreach (BattlerInfo defenderInfo in defenders) {
         if (defenderInfo.enemyType != Enemy.Type.PlayerBattler) {
            isPvpBattle = false;
            break;
         }
      }

      if (isPvpBattle) {
         processPvp(defenders, attackers);
         return;
      }

      if (createNewEnemy) {
         // Hard code enemy type if combat is accessed in team combat panel
         Enemy.Type enemyToSpawn = Enemy.Type.Lizard;

         // Create an Enemy in this instance
         Enemy spawnedEnemy = Instantiate(PrefabsManager.self.enemyPrefab);
         spawnedEnemy.enemyType = enemyToSpawn;

         // Add it to the Instance
         Instance newInstance = InstanceManager.self.getInstance(_player.instanceId);
         InstanceManager.self.addEnemyToInstance(spawnedEnemy, newInstance);

         spawnedEnemy.areaKey = newInstance.areaKey;
         spawnedEnemy.transform.position = _player.transform.position;
         NetworkServer.Spawn(spawnedEnemy.gameObject);

         netId = spawnedEnemy.netId;
      }

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
      PlayerBodyEntity localBattler = (PlayerBodyEntity) _player;
      NetworkIdentity enemyIdent = NetworkIdentity.spawned[netId];
      Enemy enemy = enemyIdent.GetComponent<Enemy>();
      BattlerData enemyData = MonsterManager.self.getBattlerData(enemy.enemyType);
      Instance instance = InstanceManager.self.getInstance(localBattler.instanceId);
      List<PlayerBodyEntity> bodyEntities = new List<PlayerBodyEntity>();

      // Register the Host as the first entry for the party entities
      bodyEntities.Add(localBattler);

      // Look up the player's Area
      Area area = AreaManager.self.getArea(_player.areaKey);

      // Get the group the player belongs to
      List<int> groupMembers = _player.tryGetGroup(out VoyageGroupInfo voyageGroup) ? voyageGroup.members : new List<int>();

      foreach (PlayerBodyEntity playerBody in getPlayerBodies(attackers, defenders)) {
         bodyEntities.Add(playerBody);
      }

      Instance battleInstance = InstanceManager.self.getInstance(_player.instanceId);
      int attackerCount = attackers.Length < 1 ? 1 : attackers.Length;
      List<BattlerInfo> modifiedDefenderList = defenders.ToList();

      // Cache the possible enemy roster
      List<Enemy> enemyRoster = new List<Enemy>();
      if (battleInstance == null) {
         D.debug("The battle instance does not exist anymore! Block process for Player: " + _player.userId + " Instance:" + _player.instanceId);
         return;
      }
      foreach (NetworkBehaviour enemyInstance in battleInstance.getEntities()) {
         if (enemyInstance is Enemy) {
            if (enemy.animGroupType == ((Enemy) enemyInstance).animGroupType) {
               enemyRoster.Add((Enemy) enemyInstance);
            }
         }
      }

      // Process voyage groups
      foreach (int memberId in groupMembers) {
         NetEntity entity = EntityManager.self.getEntity(memberId);
         if (entity != null) {
            if (entity.userId != _player.userId && _player.instanceId == entity.instanceId) {
               attackerCount++;
            }
         }
      }

      // This block will handle the randomizing of the enemy count 
      int maximumEnemyCount = (attackerCount * 2);
      if (maximumEnemyCount > Battle.MAX_ENEMY_COUNT) {
         maximumEnemyCount = Battle.MAX_ENEMY_COUNT;
      }

      int combatantCount = 0;
      bool forceCombatantEntry = false;
      if (!enemy.isBossType && enemy.enemyType != Enemy.Type.Skelly_Captain_Tutorial) {
         for (int i = 0; i < maximumEnemyCount; i++) {
            float randomizedSpawnChance = 0;

            // Chance to spawn additional enemies more than the attackers
            if (modifiedDefenderList.Count >= attackerCount) {
               // Make sure that if the encountered enemy is a support type, there is atleast 1 combatant ally
               if (enemyData.isSupportType && i == maximumEnemyCount - 1 && combatantCount < 1) {
                  randomizedSpawnChance = 0;
                  forceCombatantEntry = true;
               } else {
                  randomizedSpawnChance = Random.Range(0.0f, 10.0f);
               }
            }

            if (randomizedSpawnChance < 5) {
               Enemy backupEnemy = forceCombatantEntry ? enemyRoster.FindAll(_ => !_.isSupportType).ChooseRandom() : enemyRoster.ChooseRandom();
               BattlerData battlerData = MonsterManager.self.getBattlerData(backupEnemy.enemyType);
               modifiedDefenderList.Add(new BattlerInfo {
                  battlerName = battlerData.enemyName,
                  battlerType = BattlerType.AIEnemyControlled,
                  enemyType = backupEnemy.enemyType,
                  battlerXp = backupEnemy.XP,
                  companionId = 0
               });

               if (!battlerData.isSupportType) {
                  combatantCount++;
               }
            }
         }
      }

      // Declare the voyage group engaging the enemy
      if (enemy.battleId < 1) {
         if (_player.voyageGroupId == -1) {
            enemy.voyageGroupId = 0;
         } else {
            enemy.voyageGroupId = _player.voyageGroupId;
         }
      }

      // If this is a new team battle
      bool isExistingBattle = (enemy.battleId > 0);

      // Get or create the Battle instance
      Battle battle = isExistingBattle ? BattleManager.self.getBattle(enemy.battleId) : BattleManager.self.createTeamBattle(area, instance, enemy, attackers, localBattler, modifiedDefenderList.ToArray());

      // If the Battle is full, we can't proceed
      if (!battle.hasRoomLeft(Battle.TeamType.Attackers)) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The battle is already full!");
         return;
      }

      // Adds the player to the newly created or existing battle
      BattleManager.self.addPlayerToBattle(battle, localBattler, Battle.TeamType.Attackers);

      // Handles ability related logic
      processPlayerAbilities(localBattler, bodyEntities);

      // Send Battle Bg data
      int bgXmlID = battle.battleBoard.xmlID;
      Target_ReceiveBackgroundInfo(_player.connectionToClient, bgXmlID);
   }

   private void processPvp (BattlerInfo[] defenders, BattlerInfo[] attackers) {
      // Look up the player's Area
      Area area = AreaManager.self.getArea(_player.areaKey);
      PlayerBodyEntity localBattler = (PlayerBodyEntity) _player;
      Instance instance = InstanceManager.self.getInstance(localBattler.instanceId);

      // Get or create the Battle instance
      Battle battle = BattleManager.self.createTeamBattle(area, instance, null, attackers, localBattler, null);
      battle.isPvp = true;

      // Handles ability related logic
      List<PlayerBodyEntity> totalPlayerBodies = new List<PlayerBodyEntity>();

      // Compile all defender players
      foreach (PlayerBodyEntity playerBody in getPlayerBodies(null, defenders)) {
         BattleManager.self.addPlayerToBattle(battle, playerBody, Battle.TeamType.Defenders);
         totalPlayerBodies.Add(playerBody);
      }

      // Compile all Attacker players
      foreach (PlayerBodyEntity playerBody in getPlayerBodies(attackers, null)) {
         BattleManager.self.addPlayerToBattle(battle, playerBody, Battle.TeamType.Attackers);
         totalPlayerBodies.Add(playerBody);
      }

      // Adds the player to the newly created or existing battle
      totalPlayerBodies.Add(localBattler);
      BattleManager.self.addPlayerToBattle(battle, localBattler, Battle.TeamType.Attackers);

      // Process all abilities for each player
      foreach (PlayerBodyEntity playerBody in totalPlayerBodies) {
         processPlayerAbilities(playerBody, new List<PlayerBodyEntity> { playerBody });
      }
   }

   private List<PlayerBodyEntity> getPlayerBodies (BattlerInfo[] attackers, BattlerInfo[] defenders) {
      List<PlayerBodyEntity> bodyEntities = new List<PlayerBodyEntity>();

      // Cache Attackers Info
      if (attackers != null) {
         foreach (BattlerInfo battlerInfo in attackers) {
            // Since the body manager has the collection of user net entities, it will search for the body entity with the user name provided
            if (battlerInfo.enemyType == Enemy.Type.PlayerBattler) {
               BodyEntity rawEntity = BodyManager.self.getBodyWithName(battlerInfo.battlerName);
               if (rawEntity != null) {
                  PlayerBodyEntity bodyEntity = (PlayerBodyEntity) rawEntity;
                  if (bodyEntity != null) {
                     bodyEntities.Add(bodyEntity);
                  } else {
                     D.warning("Server cant find this ID: " + rawEntity.userId);
                  }
               }
            }
         }
      }

      // Cache Defenders Info
      if (defenders != null) {
         foreach (BattlerInfo battlerInfo in defenders) {
            // Since the body manager has the collection of user net entities, it will search for the body entity with the user name provided
            if (battlerInfo.enemyType == Enemy.Type.PlayerBattler) {
               BodyEntity rawEntity = BodyManager.self.getBodyWithName(battlerInfo.battlerName);
               if (rawEntity != null) {
                  PlayerBodyEntity bodyEntity = (PlayerBodyEntity) rawEntity;
                  if (bodyEntity != null) {
                     bodyEntities.Add(bodyEntity);
                  } else {
                     D.warning("Server cant find this ID: " + rawEntity.userId);
                  }
               }
            }
         }
      }

      return bodyEntities;
   }

   [Server]
   public void processPlayerAbilities (PlayerBodyEntity localBattler, List<PlayerBodyEntity> bodyEntities) {
      // Enter the background thread to determine if the user has at least one ability equipped
      bool hasAbilityEquipped = false;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the skill list from database
         string rawUserAbilityData = DB_Main.userAbilities(localBattler.userId.ToString(), ((int) AbilityEquipStatus.Equipped).ToString());
         List<AbilitySQLData> abilityDataList = JsonConvert.DeserializeObject<List<AbilitySQLData>>(rawUserAbilityData);

         // Determine if at least one ability is equipped
         hasAbilityEquipped = abilityDataList.Exists(_ => _.equipSlotIndex != -1);

         // If no ability is equipped, Add "Basic Attack" to player abilities in slot 0.
         if (!hasAbilityEquipped) {
            DB_Main.updateAbilitySlot(_player.userId, Global.BASIC_ATTACK_ID, 0);
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Set user to only use skill if no weapon is equipped
            if (localBattler.weaponManager.equipmentDataId == 0) {
               abilityDataList = modifiedUnarmedAbilityList();
            }

            // Provides the client with the info of the Equipped Abilities
            int weaponId = localBattler.weaponManager.equipmentDataId;
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponId);
            Weapon.Class weaponClass = weaponData == null ? Weapon.Class.Melee : weaponData.weaponClass;
            WeaponCategory weaponCategory = WeaponCategory.None;

            int sqlId = weaponData == null ? 0 : weaponData.sqlId;
            string weaponName = weaponData == null ? "none" : weaponData.equipmentName;
            D.adminLog("Player {" + _player.userId + "} weapon is" +
               " Sql: " + sqlId +
               " Name: " + weaponName +
               " Class: " + weaponClass, D.ADMIN_LOG_TYPE.Ability);

            // Determine if the abilities match the current weapon type
            int validAbilities = 0;
            List<AbilitySQLData> equippedAbilityList = abilityDataList.FindAll(_ => _.equipSlotIndex >= 0);
            foreach (AbilitySQLData abilitySql in equippedAbilityList) {
               BasicAbilityData basicAbilityData = AbilityManager.self.allGameAbilities.Find(_ => _.itemID == abilitySql.abilityID);

               // Overwrite the ability type of the fetched ability sql
               abilitySql.abilityType = basicAbilityData.abilityType;
               if (basicAbilityData.abilityType == AbilityType.Standard) {
                  AttackAbilityData attackAbilityData = AbilityManager.self.allAttackbilities.Find(_ => _.itemID == abilitySql.abilityID);
                  if ((weaponClass == Weapon.Class.Melee && attackAbilityData.isMelee())
                  || (weaponClass == Weapon.Class.Ranged && attackAbilityData.isProjectile())
                  || (weaponClass == Weapon.Class.Rum && attackAbilityData.isRum())) {
                     validAbilities++;
                     D.adminLog("Valid Ability: " + basicAbilityData.itemName +
                        " : " + basicAbilityData.itemID +
                        " : " + basicAbilityData.abilityType +
                        " : " + basicAbilityData.classRequirement, D.ADMIN_LOG_TYPE.Ability);
                  }
               } 
            }

            if (validAbilities < 1) {
               D.adminLog("No valid ability assigned to player" + " : " + _player.userId, D.ADMIN_LOG_TYPE.Ability);
            }

            // If no abilities were fetched, create a clean new entry that will be overridden based on the user equipped weapon
            if (equippedAbilityList.Count < 1) {
               validAbilities = 0;
               equippedAbilityList = new List<AbilitySQLData>();
               equippedAbilityList.Add(new AbilitySQLData {
                  abilityID = -1,
                  name = "",
               });
            }

            // Override ability if no ability matches the weapon type ex:{all melee abilities but user has gun weapon}
            if (validAbilities < 1) {
               if (weaponId < 1) {
                  weaponCategory = WeaponCategory.None;
                  equippedAbilityList[0] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.punchAbility());
               } else {
                  if (weaponData != null) {
                     switch (weaponClass) {
                        case Weapon.Class.Melee:
                           weaponCategory = WeaponCategory.Blade;
                           equippedAbilityList[0] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.slashAbility());
                           break;
                        case Weapon.Class.Ranged:
                           weaponCategory = WeaponCategory.Gun;
                           equippedAbilityList[0] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.shootAbility());
                           break;
                        case Weapon.Class.Rum:
                           weaponCategory = WeaponCategory.Rum;
                           equippedAbilityList[0] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.throwRum());
                           break;
                        default:
                           weaponCategory = WeaponCategory.None;
                           equippedAbilityList[0] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.punchAbility());
                           break;
                     }
                  } else {
                     weaponCategory = WeaponCategory.None;
                     equippedAbilityList[0] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.punchAbility());
                  }

                  D.adminLog("New ability assigned to player: "
                     + " Name: " + equippedAbilityList[0].name
                     + " ID: " + equippedAbilityList[0].abilityID
                     + " WepClass: " + weaponClass
                     + " WepCateg: " + weaponCategory, D.ADMIN_LOG_TYPE.Ability);
               }
            }

            // Provides all the abilities for the players in the party
            setupAbilitiesForPlayers(bodyEntities, equippedAbilityList, weaponClass, validAbilities, validAbilities > 0, weaponCategory);
         });
      });
   }

   private List<AbilitySQLData> modifiedUnarmedAbilityList () {
      List<AbilitySQLData> newAbilityList = new List<AbilitySQLData>();
      AbilitySQLData abilitySql = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.punchAbility());
      abilitySql.equipSlotIndex = 0;
      newAbilityList.Add(abilitySql);

      return newAbilityList;
   }

   [Command]
   public void Cmd_StartNewBattle (uint enemyNetId, Battle.TeamType teamType) {
      if (VoyageManager.isVoyageOrLeagueArea(_player.areaKey) && (!VoyageManager.isTreasureSiteArea(_player.areaKey) || !canPlayerStayInVoyage())) {
         string reason = "";
         if (VoyageManager.isTreasureSiteArea(_player.areaKey)) {
            reason += "This is not a treasure area\n";
         }
         if (!canPlayerStayInVoyage()) {
            reason += "Player cant stay in the voyage\n";
         }

         D.debug("Player {" + _player.userId + "}" + " attempted to engage in combat due invalid voyage conditions, returning to town" + " : " + reason);
         _player.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.South);
         return;
      }

      // We need a Player Body object to proceed
      if (!(_player is PlayerBodyEntity)) {
         D.warning("Player object is not a Player Body, so can't start a Battle: " + _player);
         return;
      }

      // Gather Enemy Data
      NetworkIdentity enemyIdent = NetworkIdentity.spawned[enemyNetId];
      Enemy enemy = enemyIdent.GetComponent<Enemy>();
      if (_player.transform.position.x > enemy.transform.position.x) {
         enemy.facing = Direction.East;
      } else {
         enemy.facing = Direction.West;
      }

      BattlerData enemyData = MonsterManager.self.getBattlerData(enemy.enemyType);
      List<BattlerInfo> battlerInfoList = new List<BattlerInfo>();
      battlerInfoList.Add(new BattlerInfo {
         battlerName = enemyData.enemyName,
         enemyType = enemy.enemyType,
         battlerType = BattlerType.AIEnemyControlled,
         battlerXp = enemy.XP
      });

      if (!_player.isSinglePlayer) {
         processTeamBattle(battlerInfoList.ToArray(), new BattlerInfo[0], enemyNetId);
      } else {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Fetch the list of companions
            List<CompanionInfo> companionInfoList = DB_Main.getCompanions(_player.userId);
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Gather companion data
               List<BattlerInfo> allyInfoList = new List<BattlerInfo>();
               foreach (CompanionInfo companionInfo in companionInfoList) {
                  if (companionInfo.equippedSlot > 0) {
                     allyInfoList.Add(new BattlerInfo {
                        enemyType = (Enemy.Type) companionInfo.companionType,
                        battlerName = companionInfo.companionName,
                        battlerType = BattlerType.AIEnemyControlled,
                        companionId = companionInfo.companionId,
                        battlerXp = companionInfo.companionExp
                     });
                  }
               }

               // Continue to process team battle
               processTeamBattle(battlerInfoList.ToArray(), allyInfoList.ToArray(), enemyNetId);
            });
         });
      }
   }

   [TargetRpc]
   public void Target_ReceiveMsgFromServer (NetworkConnection connection, string message) {
      Debug.LogError("Server Msg is: " + message);
   }

   [TargetRpc]
   public void Target_ReceiveNoticeFromServer (NetworkConnection connection, string message) {
      D.debug("Server: " + message);
   }

   [TargetRpc]
   public void Target_ReceiveCombatLogFromServer (NetworkConnection connection, string message) {
      D.adminLog("Combat: " + message, D.ADMIN_LOG_TYPE.Combat);
   }

   #region Abilities

   [Command]
   public void Cmd_UpdateAbility (int abilityID, int equipSlot) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Update the slot number of the specific ability
         DB_Main.updateAbilitySlot(_player.userId, abilityID, equipSlot);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_FinishedUpdatingAbility(_player.connectionToClient);
         });
      });
   }

   [Command]
   public void Cmd_SwapAbility (int abilityID, int equipSlot) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         string rawAbilityData = DB_Main.userAbilities(_player.userId.ToString(), ((int) AbilityEquipStatus.Equipped).ToString());
         List<AbilitySQLData> abilityDataList = JsonConvert.DeserializeObject<List<AbilitySQLData>>(rawAbilityData);
         AbilitySQLData abilityInSlot = abilityDataList.Find(_ => _.equipSlotIndex == equipSlot);
         AbilitySQLData abilityReplacement = abilityDataList.Find(_ => _.abilityID == abilityID);

         // Update the slot number of the specific abilities to swap them out
         DB_Main.updateAbilitySlot(_player.userId, abilityID, abilityInSlot.equipSlotIndex);
         DB_Main.updateAbilitySlot(_player.userId, abilityInSlot.abilityID, abilityReplacement.equipSlotIndex);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_FinishedUpdatingAbility(_player.connectionToClient);
         });
      });
   }

   [Command]
   public void Cmd_UpdateAbilities (AbilitySQLData[] abilities) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (AbilitySQLData ability in abilities) {
            DB_Main.updateAbilitiesData(_player.userId, ability);
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_FinishedUpdatingAbility(_player.connectionToClient);
         });
      });
   }

   [Server]
   public void setupAbilitiesForPlayers (List<PlayerBodyEntity> playerEntities, List<AbilitySQLData> equippedAbilityList, Weapon.Class weaponClass, int validAbilities, bool playerHasValidAbilities = true, WeaponCategory weaponCategory = WeaponCategory.None) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (PlayerBodyEntity entity in playerEntities) {
            // Retrieves skill list from database
            string rawAbilityData = DB_Main.userAbilities(entity.userId.ToString(), ((int) AbilityEquipStatus.ALL).ToString());
            List<AbilitySQLData> abilityDataList = JsonConvert.DeserializeObject<List<AbilitySQLData>>(rawAbilityData);

            // Set user to only use skill if no weapon is equipped
            if (entity.weaponManager.weaponType == 0) {
               abilityDataList = modifiedUnarmedAbilityList();
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               List<AbilitySQLData> equippedAbilityDataList = abilityDataList.FindAll(_ => _.equipSlotIndex >= 0);
               Battler battler = BattleManager.self.getBattler(entity.userId);
               if (equippedAbilityDataList.Count < 1) {

                  List<int> basicAbilityIds = new List<int>();
                  foreach (AbilitySQLData temp in equippedAbilityList) {
                     basicAbilityIds.Add(temp.abilityID);
                  }

                  if (battler != null) {
                     foreach (int abilityId in basicAbilityIds) {
                        D.adminLog("Sending Overridden Ability Data to Player: " + battler.userId
                           + " ID: " + abilityId, D.ADMIN_LOG_TYPE.Ability);
                     }
                     battler.setBattlerAbilities(basicAbilityIds, BattlerType.PlayerControlled);
                  }
               } else {
                  // Sort by equipment slot index
                  equippedAbilityDataList = equippedAbilityDataList.OrderBy(_ => _.equipSlotIndex).ToList();

                  // Set server data
                  List<BasicAbilityData> basicAbilityList = getAbilityRecord(equippedAbilityDataList.ToArray());
                  List<int> basicAbilityIds = new List<int>();
                  foreach (BasicAbilityData abilityData in basicAbilityList) {
                     if (abilityData != null) {
                        basicAbilityIds.Add(abilityData.itemID);
                     }
                  }

                  // Set the default ability if weapon mismatches the abilities
                  if (!playerHasValidAbilities) {
                     switch (weaponCategory) {
                        case WeaponCategory.None:
                           basicAbilityIds[0] = AbilityManager.self.punchAbility().itemID;
                           break;
                        case WeaponCategory.Blade:
                           basicAbilityIds[0] = AbilityManager.self.slashAbility().itemID;
                           break;
                        case WeaponCategory.Gun:
                           basicAbilityIds[0] = AbilityManager.self.shootAbility().itemID;
                           break;
                        case WeaponCategory.Rum:
                           basicAbilityIds[0] = AbilityManager.self.throwRum().itemID;
                           break;
                        default:
                           D.debug("Invalid Weapon Category!");
                           break;
                     }
                  }

                  try {
                     string abilityString = "";
                     foreach (int abilityId in basicAbilityIds) {
                        abilityString += abilityId + " : ";
                     }
                     D.adminLog("Sending RAW Ability Data to Player: {" + battler.userId + "} ID: {" + abilityString+ "}", D.ADMIN_LOG_TYPE.Ability);
                     battler.setBattlerAbilities(basicAbilityIds, BattlerType.PlayerControlled);
                  } catch {
                     D.debug("Cant add battler abilities for: " + battler);
                  }
               }
            });
         }
      });
   }

   protected List<BasicAbilityData> getAbilityRecord (AbilitySQLData[] equippedAbilities) {
      List<BasicAbilityData> basicAbilityList = new List<BasicAbilityData>();

      foreach (AbilitySQLData abilitySQL in equippedAbilities) {
         BasicAbilityData abilityData = AbilityManager.getAbility(abilitySQL.abilityID, AbilityType.Undefined);
         if (abilityData != null) {
            basicAbilityList.Add(abilityData);
         }
      }

      return basicAbilityList;
   }

   [TargetRpc]
   public void Target_FinishedUpdatingAbility (NetworkConnection connection) {
      NubisDataFetcher.self.fetchUserAbilities();
   }

   #endregion

   #region Basic Info Fetching

   [TargetRpc]
   public void Target_ReceiveBackgroundInfo (NetworkConnection connection, int bgXmlId) {
      BackgroundGameManager.self.activateBgContent(bgXmlId);
   }

   #endregion

   [Command]
   public void Cmd_RequestAbility (int abilityTypeInt, uint netId, int abilityInventoryIndex, bool cancelAction) {
      AbilityType abilityType = (AbilityType) abilityTypeInt;

      if (_player == null || !(_player is PlayerBodyEntity)) {
         D.editorLog("Invalid Source!");
         Target_ReceiveRefreshCasting(connectionToClient, false);
         return;
      }

      // Look up the player's Battle object
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      Battle battle = BattleManager.self.getBattle(playerBody.battleId);
      Battler sourceBattler = battle.getBattler(_player.userId);
      Battler targetBattler = null;
      BasicAbilityData abilityData = new BasicAbilityData();

      if (abilityInventoryIndex > -1) {
         if (abilityType == AbilityType.Standard) {
            // Get the ability from the battler abilities.
            try {
               abilityData = sourceBattler.getAttackAbilities()[abilityInventoryIndex];
            } catch {
               abilityData = AbilityManager.self.punchAbility();
            }
         } else {
            // Get the ability from the battler abilities.
            abilityData = sourceBattler.getBuffAbilities()[abilityInventoryIndex];
         }
      } else {
         abilityData = AbilityManager.self.punchAbility();
      }

      foreach (Battler participant in battle.getParticipants()) {
         if (participant.netId == netId) {
            targetBattler = participant;
         }
      }

      // Ignore invalid or dead sources and targets
      if (sourceBattler == null || targetBattler == null || sourceBattler.isDead() || targetBattler.isDead()) {
         Target_ReceiveRefreshCasting(connectionToClient, false);
         return;
      }
      // Make sure the source battler can use that ability type
      if (!abilityData.isReadyForUseBy(sourceBattler) && !cancelAction) {
         Target_ReceiveRefreshCasting(connectionToClient, false);
         return;
      }

      if (abilityType == AbilityType.Standard) {
         // If it's a Melee Ability, make sure the target isn't currently protected
         if (((AttackAbilityData) abilityData).isMelee() && targetBattler.isProtected(battle)) {
            D.warning("Battler requested melee ability against protected target! Player: " + playerBody.entityName);
            Target_ReceiveRefreshCasting(connectionToClient, false);
            return;
         }
      }

      // Let the Battle Manager handle executing the ability
      List<Battler> targetBattlers = new List<Battler>() { targetBattler };
      if (cancelAction) {
         BattleManager.self.cancelBattleAction(battle, sourceBattler, targetBattlers, abilityInventoryIndex, abilityType);
      } else {
         if (!sourceBattler.battlerAbilitiesInitialized && sourceBattler.getBasicAbilities().Count < 1 && sourceBattler.enemyType == Enemy.Type.PlayerBattler) {
            Target_ReceiveRefreshCasting(connectionToClient, true);
            return;
         }
         BattleManager.self.executeBattleAction(battle, sourceBattler, targetBattlers, abilityInventoryIndex, abilityType);
      }
   }

   [TargetRpc]
   public void Target_ReceiveRefreshCasting (NetworkConnection connection, bool refreshAbilityCache) {
      BattleManager.self.getPlayerBattler().setBattlerCanCastAbility(true);
      if (refreshAbilityCache) {
         AttackPanel.self.clearCachedAbilityCast();
      }
   }

   [Command]
   public void Cmd_RequestStanceChange (Battler.Stance newStance) {
      if (_player == null || !(_player is PlayerBodyEntity)) {
         return;
      }

      // Look up the player's Battle object
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      Battle battle = BattleManager.self.getBattle(playerBody.battleId);
      Battler sourceBattler = battle.getBattler(_player.userId);
      BasicAbilityData abilityData = null;

      // Get the correct stance ability data.
      switch (newStance) {
         case Battler.Stance.Balanced:
            abilityData = sourceBattler.getBalancedStance();
            break;
         case Battler.Stance.Attack:
            abilityData = sourceBattler.getOffenseStance();
            break;
         case Battler.Stance.Defense:
            abilityData = sourceBattler.getDefensiveStance();
            break;
      }

      // Ignore invalid or dead sources and targets
      if (sourceBattler == null || sourceBattler.isDead()) {
         return;
      }

      if (abilityData != null) {
         // Make sure the source battler can use that ability type
         if (!abilityData.isReadyForUseBy(sourceBattler)) {
            D.debug("Battler requested to use ability they're not allowed: " + playerBody.entityName + ", " + abilityData.itemName);
            return;
         }

         BattleManager.self.executeStanceChangeAction(battle, sourceBattler, newStance);
      } else {
         D.debug("Ability data is null!");
      }
   }

   [Server]
   protected void getShipsForArea (string shopName) {
      bool findByShopName = shopName == ShopManager.DEFAULT_SHOP_NAME ? false : true;

      // Get the current list of ships for the area
      List<ShipInfo> list = new List<ShipInfo>();
      if (findByShopName) {
         list = ShopManager.self.getShipsByShopName(shopName);
      } else {
         list = ShopManager.self.getShips(_player.areaKey);
      }

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         ShopData shopData = new ShopData();
         if (findByShopName) {
            shopData = ShopXMLManager.self.getShopDataByName(shopName);
         } else {
            shopData = ShopXMLManager.self.getShopDataByArea(_player.areaKey);
         }

         if (shopData == null) {
            D.debug("Shop data is missing for: " + shopName + " - " + _player.areaKey);
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.rpc.Target_ReceiveShipyard(_player.connectionToClient, gold, new string[0], "");
            });
         } else {
            string greetingText = shopData.shopGreetingText;
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.rpc.Target_ReceiveShipyard(_player.connectionToClient, gold, Util.serialize(list), greetingText);
            });
         }
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
   protected void getItemsForArea (string shopName) {
      bool findByShopName = shopName == ShopManager.DEFAULT_SHOP_NAME ? false : true;

      // Get the current list of items for the area
      List<Item> list = new List<Item>();
      if (findByShopName) {
         list = ShopManager.self.getItemsByShopName(shopName);
      } else {
         list = ShopManager.self.getItems(_player.areaKey);
      }

      // Sort by rarity
      List<Item> sortedList = list.OrderBy(x => x.getSellPrice()).ToList();
      foreach (Item item in sortedList) {
         switch (item.category) {
            case Item.Category.Weapon:
               WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
               if (weaponData != null) {
                  item.setBasicInfo(weaponData.equipmentName, weaponData.equipmentDescription, weaponData.equipmentIconPath, item.paletteNames);
               }
               break;
            case Item.Category.Armor:
               ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
               if (armorData != null) {
                  item.setBasicInfo(armorData.equipmentName, armorData.equipmentDescription, armorData.equipmentIconPath, item.paletteNames);
               }
               break;
            case Item.Category.Hats:
               HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
               if (hatData != null) {
                  item.setBasicInfo(hatData.equipmentName, hatData.equipmentDescription, hatData.equipmentIconPath, item.paletteNames);
               }
               break;
            case Item.Category.CraftingIngredients:
               CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
               item.setBasicInfo(CraftingIngredients.getName(ingredientType), "", CraftingIngredients.getIconPath(ingredientType));
               break;
         }
      }

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ShopData shopData = new ShopData();
            if (findByShopName) {
               shopData = ShopXMLManager.self.getShopDataByName(shopName);
            } else {
               shopData = ShopXMLManager.self.getShopDataByArea(_player.areaKey);
            }

            if (shopData == null) {
               D.debug("Problem Fetching Shop Name!" + " : " + shopName);
            } else {
               string greetingText = shopData.shopGreetingText;
               _player.rpc.Target_ReceiveShopItems(_player.connectionToClient, gold, Util.serialize(sortedList), greetingText);
            }
         });
      });
   }

   [Server]
   public void requestSetArmorId (int armorId) {
      // They may be in an island scene, or at sea
      PlayerBodyEntity body = _player.GetComponent<PlayerBodyEntity>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setArmorId(_player.userId, armorId);
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Armor armor = Armor.castItemToArmor(userObjects.armor);
            if (armor.data.Length < 1 && armor.itemTypeId > 0 && armor.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
               armor.data = ArmorStatData.serializeArmorStatData(EquipmentXMLManager.self.getArmorDataByType(armor.itemTypeId));
               userObjects.armor.data = armor.data;
            }
            if (body != null) {
               body.armorManager.updateArmorSyncVars(armor.itemTypeId, armor.id, armor.paletteNames);

               // Update the battler entity if the user is in battle
               if (body.isInBattle()) {
                  BattleManager.self.onPlayerEquipItem(body);
               }
            }

            Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);
         });
      });
   }

   [Server]
   public void requestSetHatId (int hatId) {
      // They may be in an island scene, or at sea
      PlayerBodyEntity body = _player.GetComponent<PlayerBodyEntity>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setHatId(_player.userId, hatId);
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Hat hat = Hat.castItemToHat(userObjects.hat);
            if (hat.data.Length < 1 && hat.itemTypeId > 0 && hat.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
               hat.data = HatStatData.serializeHatStatData(EquipmentXMLManager.self.getHatData(hat.itemTypeId));
               userObjects.hat.data = hat.data;
            }
            if (body != null) {
               body.hatsManager.updateHatSyncVars(hat.itemTypeId, hat.id);

               // Update the battler entity if the user is in battle
               if (body.isInBattle()) {
                  BattleManager.self.onPlayerEquipItem(body);
               }
            }

            Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);
         });
      });
   }

   [Server]
   public void requestSetWeaponId (int weaponId) {
      // They may be in an island scene, or at sea
      PlayerBodyEntity body = _player.GetComponent<PlayerBodyEntity>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Update the Weapon object here on the server based on what's in the database
         DB_Main.setWeaponId(_player.userId, weaponId);
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

         // Back to Unity Thread to call RPC functions
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Weapon weapon = Weapon.castItemToWeapon(userObjects.weapon);
            if (weapon.data.Length < 1 && weapon.itemTypeId > 0 && weapon.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
               weapon.data = WeaponStatData.serializeWeaponStatData(EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId));
               userObjects.weapon.data = weapon.data;
            }
            if (body != null) {
               body.weaponManager.updateWeaponSyncVars(weapon.itemTypeId, weapon.id, weapon.paletteNames);
               D.adminLog("Player {" + body.userId + "} is equipping item" + 
                  "} ID: {" + weapon.id + 
                  "} TypeId: {" + weapon.itemTypeId + 
                  "} Name: {" + weapon.getName()+ "}", D.ADMIN_LOG_TYPE.Equipment);

               // Update the battler entity if the user is in battle
               if (body.isInBattle()) {
                  BattleManager.self.onPlayerEquipItem(body);
               }
            } else {
               D.editorLog("Failed to cast!", Color.magenta);
            }

            if (_player != null) {
               Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);
            } else {
               D.debug("Cant change equipment while player is destroyed");
            }
         });
      });
   }

   [Server]
   public void sendError (string message) {
      if (UnityThreadHelper.IsMainThread) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, message);
      } else {
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, message);
         });
      }
   }

   [Command]
   public void Cmd_RetrieveBook (int bookId) {
      retrieveBook(bookId);
   }

   [TargetRpc]
   private void Target_ShowBook (NetworkConnection conn, string title, string content) {
      // Get the book reader panel
      PanelManager.self.selectedPanel = Panel.Type.BookReader;
      BookReaderPanel bookPanel = (BookReaderPanel) PanelManager.self.get(Panel.Type.BookReader);

      BookData book = new BookData(title, content);

      // Show the book
      bookPanel.show(book);
   }

   [Server]
   private void retrieveBook (int bookId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         BookData book = DB_Main.getBookById(bookId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ShowBook(netIdentity.connectionToClient, book.title, book.content);
         });
      });
   }

   [Command]
   public void Cmd_FoundDiscovery (int discoveryId) {
      Discovery discovery = DiscoveryManager.self.getSpawnedDiscoveryById(discoveryId);

      if (isDiscoveryFindingValid(discovery) && discovery.instanceId == _player.instanceId) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            int gainedXP = discovery.getXPValue();

            // Add the experience to the player
            DB_Main.addJobXP(_player.userId, Jobs.Type.Explorer, gainedXP);

            Jobs newJobXP = DB_Main.getJobXP(_player.userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.Target_GainedXP(netIdentity.connectionToClient, gainedXP, newJobXP, Jobs.Type.Explorer, 0, true);
               discovery.Target_RevealDiscovery(netIdentity.connectionToClient);
            });
         });
      } else {
         D.log($"Player {_player.nameText.text} reported an invalid discovery.\nPlayer Instance ID: {_player.instanceId}\n" +
            $"Discovery Instance ID: {discovery.instanceId}\nDistance from discovery: {getDistanceFromDiscovery(discovery)}\nDistance is valid: {isDiscoveryFindingValid(discovery)}");
      }
   }

   [Server]
   private bool isDiscoveryFindingValid (Discovery discovery) {
      return getDistanceFromDiscovery(discovery) <= Discovery.MAX_VALID_DISTANCE;
   }

   private float getDistanceFromDiscovery (Discovery discovery) {
      return Vector2.Distance(discovery.transform.position, _player.transform.position);
   }

   [Command]
   public async void Cmd_RequestEnterMapCustomization (string areaKey) {
      // Find the player
      if (_player == null) {
         Target_DenyEnterMapCustomization("Player doesn't exist");
         return;
      }

      // Check if player is in the area
      if (!areaKey.Equals(_player.areaKey)) {
         Target_DenyEnterMapCustomization("Player must be in the area to customize it");
         return;
      }

      // Get the instance
      Instance instance = _player.getInstance();

      // Check if someone is already customizing
      if (instance.playerMakingCustomizations != null && instance.playerMakingCustomizations.userId != _player.userId) {
         Target_DenyEnterMapCustomization("Someone else is already customizing the area");
         return;
      }

      // Allow player to enter customization
      instance.playerMakingCustomizations = _player;

      // Get props that can be used for customization
      ItemInstance[] remainingProps = await DB_Main.execAsync((cmd) => DB_Main.getItemInstances(cmd, _player.userId, ItemDefinition.Category.Prop).ToArray());

      Target_AllowEnterMapCustomization(remainingProps);
   }

   [TargetRpc]
   private void Target_AllowEnterMapCustomization (ItemInstance[] remainingProps) {
      MapCustomizationManager.serverAllowedEnterCustomization(remainingProps);
   }

   [TargetRpc]
   private void Target_DenyEnterMapCustomization (string message) {
      MapCustomizationManager.serverDeniedEnterCustomization(message);
   }

   [Command]
   public void Cmd_ExitMapCustomization (string areaKey) {
      if (_player != null) {
         // Check if player is in the area
         if (areaKey.Equals(_player.areaKey)) {
            // Get the instance
            Instance instance = _player.getInstance();

            // Check if someone is already customizing
            if (instance.playerMakingCustomizations != null && instance.playerMakingCustomizations.userId == _player.userId) {
               instance.playerMakingCustomizations = null;
            }
         }
      }
   }

   [TargetRpc]
   public void Target_ShowCustomMapPanel (string customMapType, bool warpAfterSelecting, Map[] baseMaps) {
      // If we received information about base maps that will be required by the panel, store it
      if (baseMaps != null) {
         foreach (Map map in baseMaps) {
            AreaManager.self.storeAreaInfo(map);
         }
      }

      if (AreaManager.self.tryGetCustomMapManager(customMapType, out CustomMapManager manager)) {
         PanelManager.self.get<CustomMapsPanel>(Panel.Type.CustomMaps).displayFor(manager, warpAfterSelecting);
      } else {
         D.error($"Cannot find custom map manager for key: { customMapType }");
      }
   }

   [Command]
   public void Cmd_RequestCustomMapPanelClient (string customMapType, bool warpAfterSelecting) {
      if (!AreaManager.self.tryGetCustomMapManager(customMapType, out CustomMapManager manager)) {
         D.debug("Custom map manager cant fetch custom map for: " + customMapType);
         return;
      }

      _player.rpc.Target_ShowCustomMapPanel(customMapType, warpAfterSelecting, manager.getRelatedMaps());
   }

   [Command]
   public async void Cmd_SetCustomMapBaseMap (string customMapKey, int baseMapId, bool warpIntoAfterSetting) {
      // Check if this is a custom map key
      if (!AreaManager.self.tryGetCustomMapManager(customMapKey, out CustomMapManager manager)) {
         return;
      }

      // Check if this type of custom map has this base map
      if (!manager.getRelatedMaps().Any(m => m.id == baseMapId)) {
         return;
      }

      // If player did not have a base map before, give him some props as a reward
      List<ItemInstance> rewards = new List<ItemInstance>();

      if (manager is CustomHouseManager) {
         await DB_Main.execAsync((cmd) => DB_Main.setCustomHouseBase(cmd, _player.userId, baseMapId));

         if (_player.customHouseBaseId == 0) {
            rewards = ItemInstance.getFirstHouseRewards(_player.userId);
         }

         _player.customHouseBaseId = baseMapId;
      } else if (manager is CustomFarmManager) {
         await DB_Main.execAsync((cmd) => DB_Main.setCustomFarmBase(cmd, _player.userId, baseMapId));

         if (_player.customFarmBaseId == 0) {
            rewards = ItemInstance.getFirstFarmRewards(_player.userId);
         }

         _player.customFarmBaseId = baseMapId;
      } else {
         D.error("Unrecoginzed custom map manager");
         return;
      }

      Target_BaseMapUpdated(customMapKey, baseMapId);

      if (warpIntoAfterSetting) {
         _player.spawnInNewMap(customMapKey, null, _player.facing);
      }

      await giveItemRewardsToPlayer(_player.userId, rewards, false);
   }

   [TargetRpc]
   public void Target_BaseMapUpdated (string customMapKey, int baseMapId) {
      PanelManager.self.get<CustomMapsPanel>(Panel.Type.CustomMaps).baseMapUpdated(customMapKey, baseMapId);
   }

   [Command]
   public async void Cmd_AddPrefabCustomization (int areaOwnerId, string areaKey, PrefabState changes) {
      Area area = AreaManager.self.getArea(areaKey);
      Instance instance = _player.getInstance();

      // Get list of props of the user
      List<ItemInstance> remainingProps = await DB_Main.execAsync((cmd) => DB_Main.getItemInstances(cmd, _player.userId, ItemDefinition.Category.Prop).ToList());

      // Check if changes are valid
      if (!MapCustomizationManager.validatePrefabChanges(area, instance.biome, remainingProps, changes, true, out string errorMessage)) {
         Target_FailAddPrefabCustomization(changes, errorMessage);
         return;
      }

      // Figure out the base map of area
      AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager);
      NetEntity areaOwner = EntityManager.self.getEntity(areaOwnerId);
      int baseMapId = customMapManager.getBaseMapId(areaOwner);

      // Find the customizable prefab that is being targeted
      CustomizablePrefab prefab = AssetSerializationMaps.tryGetPrefabGame(changes.serializationId, instance.biome).GetComponent<CustomizablePrefab>();

      // Set changes in the database
      PrefabState currentState = await DB_Main.execAsync((cmd) => DB_Main.getMapCustomizationChanges(cmd, baseMapId, areaOwnerId, changes.id));
      bool createdByUser = currentState.created;
      currentState = currentState.id == -1 ? changes : currentState.add(changes);
      await DB_Main.execAsync((cmd) => DB_Main.setMapCustomizationChanges(cmd, baseMapId, areaOwnerId, currentState));

      // If creating a new prefab, remove an item from the inventory
      if (changes.created) {
         int itemId = remainingProps.FirstOrDefault(i => i.itemDefinitionId == prefab.propDefinitionId)?.id ?? -1;
         await DB_Main.execAsync((cmd) => DB_Main.decreaseOrDeleteItemInstance(cmd, itemId, 1));
      }

      // If deleting a prefab and it's not placed in map editor, return to inventory
      if (changes.deleted && createdByUser) {
         await DB_Main.execAsync((cmd) => DB_Main.createOrAppendItemInstance(cmd,
                  new ItemInstance { count = 1, itemDefinitionId = prefab.propDefinitionId, ownerUserId = _player.userId, id = -1 }));
      }

      // Set changes in the server
      MapManager.self.addCustomizations(area, instance.biome, changes);

      // Notify all clients about them
      Rpc_AddPrefabCustomizationSuccess(areaKey, instance.biome, changes);
   }

   [TargetRpc]
   public void Target_FailAddPrefabCustomization (PrefabState failedChanges, string message) {
      MapCustomizationManager.serverAddPrefabChangeFail(failedChanges, message);
   }

   [ClientRpc]
   public void Rpc_AddPrefabCustomizationSuccess (string areaKey, Biome.Type biome, PrefabState changes) {
      MapCustomizationManager.serverAddPrefabChangeSuccess(areaKey, biome, changes);
   }

   #region Item Instances

   [TargetRpc]
   public void Target_ReceiveItemRewardList (ItemInstance[] items) {
      RewardManager.self.showItemsInRewardPanel(items);
   }

   #endregion

   [Command]
   public void Cmd_FetchPerkPointsForUser () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<Perk> userPerks = DB_Main.getPerkPointsForUser(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_SetPerkPoints(netIdentity.connectionToClient, userPerks.ToArray());
         });
      });
   }

   [TargetRpc]
   private void Target_SetPerkPoints (NetworkConnection conn, Perk[] perks) {
      PerkManager.self.setPlayerPerkPoints(perks);
   }

   [Command]
   public void Cmd_AssignPerkPoint (int perkId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int assignedPoints = DB_Main.getAssignedPointsByPerkId(_player.userId, perkId);

         if (assignedPoints < Perk.MAX_POINTS_BY_PERK) {
            int availablePoints = DB_Main.getUnassignedPerkPoints(_player.userId);

            if (availablePoints > 0) {
               DB_Main.assignPerkPoint(_player.userId, perkId);
            }

            // Refresh player's perk points
            List<Perk> userPerks = DB_Main.getPerkPointsForUser(_player.userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Target_SetPerkPoints(netIdentity.connectionToClient, userPerks.ToArray());
            });
         } else {
            D.log("This perk has already reached its maximum level");
         }
      });
   }

   [TargetRpc]
   public void Target_OnWarpFailed (string msg) {
      D.debug("Warp failed: " + msg);
      _player.onWarpFailed();
      PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.MapCreation);
   }

   [Command]
   public void Cmd_RequestWarp (string areaTarget, string spawnTarget) {
      D.adminLog("Player {" + _player.userId + "} Warping to {" + spawnTarget + "}", D.ADMIN_LOG_TYPE.Warp);
      
      if (_player == null || _player.connectionToClient == null) {
         D.adminLog("Player {" + _player.userId + "} Failed to warp due to missing reference", D.ADMIN_LOG_TYPE.Warp);
         Target_OnWarpFailed("Missing player or player connection");
      }
      
      StartCoroutine(CO_WaitForAreaLoad(areaTarget, spawnTarget));
   }

   private IEnumerator CO_WaitForAreaLoad (string areaTarget, string spawnTarget) {
      // TODO: Confirm if this is needed
      // If the area load exceeds this time, assume there was network related issue and trigger warp failure
      float maxLoadingTime = 30;
      float currentLoadTime = 0;

      while (AreaManager.self.getArea(_player.areaKey) == null) {
         if (currentLoadTime < maxLoadingTime) {
            currentLoadTime += Time.deltaTime;
            yield return 0;
         } else {
            D.debug("Area failed to load: " + _player.areaKey + " wait time was " + currentLoadTime + " seconds");
            D.adminLog("Player {" + _player.userId + "} Failed to warp due to missing area: {" + " : " + _player.areaKey + "}", D.ADMIN_LOG_TYPE.Warp);
            Target_OnWarpFailed("Missing Area: " + _player.areaKey);
            yield break;
         }
      }
      D.adminLog("Waiting time for area{" + _player.areaKey + "} to load is: " + currentLoadTime + " seconds", D.ADMIN_LOG_TYPE.Warp);
      Area area = AreaManager.self.getArea(_player.areaKey);

      // Get the warps for the area the player is currently in
      List<Warp> warps = area.getWarps();

      foreach (Warp warp in warps) {
         // Only warp the player if they're close enough to the warp. Check area and spawn targets are the ones player requested just in case two warps are too close together.
         float distanceToWarp = Vector2.Distance(warp.transform.position, transform.position);
         if (distanceToWarp < 2f && areaTarget == warp.areaTarget && spawnTarget == warp.spawnTarget && warp.canPlayerUseWarp(_player)) {
            if (warp.gameObject.activeInHierarchy) {
               warp.startWarpForPlayer(_player);
            } else {
               // Inactive warps are children of secret entrance, the warp will be activated in the server side only, the warp game object will be automatically disabled after warping the player since secret entrance warps have a boolean "isSecretWarp"
               warp.gameObject.SetActive(true);
               warp.startWarpForPlayer(_player);
            }
            yield break;
         }

         D.adminLog("Checking " +
            "WarpTo: {" + warp.areaTarget + "} " +
            "IsNear: {" + (distanceToWarp < 2f) + "} " +
            "AreaTarget: {" + areaTarget + "} " +
            "WarpTarget: {" + warp.spawnTarget + "} " +
            "CanUse?: {" + warp.canPlayerUseWarp(_player) + "}", D.ADMIN_LOG_TYPE.Warp);
      }

      D.adminLog("Player {" + _player.userId + "} Failed to warp since there is no warp nearby!", D.ADMIN_LOG_TYPE.Warp);

      // If no valid warp was found, let the player know so at least they're not stuck
      Target_OnWarpFailed("No valid warp nearby");
   }

   [Command]
   public void Cmd_StartPettingAnimal (uint netEntityId, int facing) {
      NPC npc = MyNetworkManager.fetchEntityFromNetId<NPC>(netEntityId);

      GameObject closestSpot = null;
      Vector3 playerPos = _player.transform.position;

      // Choose spot to start petting
      switch ((Direction) facing) {
         case Direction.North:
            closestSpot = npc.animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Bottom"));
            break;
         case Direction.East:
            closestSpot = npc.animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Left"));
            break;
         case Direction.West:
            closestSpot = npc.animalPettingPositions.Find((GameObject obj) => obj.name.Contains("Right"));
            break;
      }

      float distance = Vector2.Distance(closestSpot.transform.position, playerPos);
      Vector2 distToMoveAnimal = playerPos - closestSpot.transform.position;
      Vector2 animalEndPos = new Vector2(npc.transform.position.x, npc.transform.position.y) + distToMoveAnimal;
      float maxTime = Mathf.Lerp(0.0f, 0.75f, distance / NPC.ANIMAL_PET_DISTANCE);

      // Override Begin
      NpcControlOverride npcontroller = npc.GetComponent<NpcControlOverride>();
      npcontroller.hasReachedDestination.AddListener(() => {
         if (_player is PlayerBodyEntity) {
            ((PlayerBodyEntity) _player).weaponManager.Rpc_HideWeapons(true);
         }

         // Handle pet animation on server side without altering pet movement
         npc.triggerPetAnimation(_player.netId, animalEndPos, maxTime);

         // Server will wait for the animation time to finish and un hide the weapon layers for the player
         npc.finishedPetting.AddListener(() => {
            if (_player is PlayerBodyEntity) {
               ((PlayerBodyEntity) _player).weaponManager.Rpc_HideWeapons(false);
            }

            npc.finishedPetting.RemoveAllListeners();
         });

         // Handle pet animation and animated movement on client side
         npc.Rpc_ContinuePettingAnimal(_player.netId, animalEndPos, maxTime);
         npcontroller.hasReachedDestination.RemoveAllListeners();
      });
      npcontroller.overridePosition(animalEndPos, _player.transform.position);
      npc.Rpc_ContinuePetMoveControl(animalEndPos, maxTime);
   }


   [ClientRpc]
   public void Rpc_TemporaryControlRequested (Vector2 controllerLocalPosition) {
      D.debug(string.Format("Client: {0} noted bounce time of client: {1}, isLocalPlayer = {2}", Global.player.userId, _player.userId, isLocalPlayer));

      // If we are the local player, we don't do anything, the control was handled locally
      if (isLocalPlayer) {
         return;
      }

      TemporaryController con = AreaManager.self.getArea(_player.areaKey).getTemporaryControllerAtPosition(controllerLocalPosition);
      _player.noteWebBounce(con);
   }

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   #endregion
}
