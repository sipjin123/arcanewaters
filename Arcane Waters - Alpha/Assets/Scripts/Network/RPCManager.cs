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
using Store;
using Steam.Purchasing;
using Steam;

public class RPCManager : NetworkBehaviour
{
   #region Public Variables

   // Called when the client receives data about an item from the server
   public static event Action<Item> itemDataReceived;

   // Item class for queue
   public class ItemQueue
   {
      public int userId;
      public Item item;
      public bool wasPurchasedOnTheGemStore;
      public StoreItem gemStoreItem;
   }

   // List of items being created
   public List<ItemQueue> itemCreationList = new List<ItemQueue>();

   // Determines if the process is on going
   public bool isProcessing = false;

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
   public void Cmd_CreateAuction (Item item, int startingBid, bool isBuyoutAllowed, int buyoutPrice, long expiryDateBinary) {
      DateTime expiryDate = DateTime.FromBinary(expiryDateBinary);

      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (startingBid <= 0) {
         sendError("The starting bid must be higher than 0.");
         return;
      }

      if (isBuyoutAllowed && buyoutPrice <= 0) {
         sendError("The buyout price must be higher than 0.");
         return;
      }

      if (isBuyoutAllowed && buyoutPrice <= startingBid) {
         sendError("The buyout price must be higher than the starting bid!");
         return;
      }

      if (expiryDate <= DateTime.UtcNow) {
         sendError("The auction duration is inconsistent.");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         if (Bkg_ShouldBeSoulBound(item, isBeingEquipped: false)) {
            if (!Bkg_IsItemSoulBound(item)) {
               DB_Main.updateItemSoulBinding(item.id, isBound: true);
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendError(ErrorMessage.Type.ItemIsSoulBound, _player, $"Soul bound items can't be traded.");
            });

            return;
         }

         int gold = DB_Main.getGold(_player.userId);

         // The auction cost is computed as a fraction of the starting bid
         int auctionCost = AuctionManager.computeAuctionCost(startingBid);

         // Make sure they have enough gold and the price is positive
         if (gold < auctionCost || auctionCost < 0) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendError(ErrorMessage.Type.NotEnoughGold, _player, "You don't have " + auctionCost + " gold!");
            });
            return;
         }

         // Create a mail without recipient, with the auctioned item as attachment - this will also verify the item validity
         int mailId = createMailCommon(-1, "Auction House - Item Delivery", "", new int[] { item.id }, new int[] { item.count }, false, false);
         if (mailId < 0) {
            return;
         }

         // Create the auction
         int newId = DB_Main.createAuction(_player.userId, _player.nameText.text, mailId, expiryDate, isBuyoutAllowed, startingBid, buyoutPrice, item.category, EquipmentXMLManager.self.getItemName(item), item.count);

         // Make sure that we charge the player only if the auction was set up correctly
         DB_Main.addGold(_player.userId, -auctionCost);

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
         if (auction.isBuyoutAllowed) {
            bidAmount = Mathf.Clamp(bidAmount, 0, auction.buyoutPrice);
         }

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
         if (auction.isBuyoutAllowed && bidAmount >= auction.buyoutPrice) {
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
   public void Cmd_InteractTrigger () {
      Target_InteractTrigger(_player.connectionToClient);
   }

   [TargetRpc]
   public void Target_InteractTrigger (NetworkConnection connection) {
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      if (!_player.isInBattle()) {
         playerBody.interactionTrigger();
      }
   }

   [Command]
   public void Cmd_InteractAnimation (Anim.Type animType, Direction direction) {
      _player.Rpc_ForceLookat(direction);
      Rpc_InteractAnimation(animType, false);
      Target_PlayerInteract(_player.connectionToClient);
   }

   [Command]
   public void Cmd_FastInteractAnimation (Anim.Type animType, Direction direction, bool playedLocally) {
      _player.Rpc_ForceLookat(direction);
      Rpc_InteractAnimation(animType, playedLocally);
   }

   [TargetRpc]
   public void Target_PlayerInteract (NetworkConnection connection) {
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      if (!_player.isInBattle()) {
         playerBody.farmingTrigger.interactFarming();
         playerBody.miningTrigger.interactOres();
      }
   }

   [ClientRpc]
   public void Rpc_InteractAnimation (Anim.Type animType, bool playedLocally) {
      playInteractAnimation(animType, playedLocally);
   }

   public void playInteractAnimation (Anim.Type animType, bool playedLocally) {
      // If we are the local player, and the animation has already played locally, don't play it again in the Rpc
      if (isLocalPlayer && playedLocally) {
         return;
      }

      PlayerBodyEntity playerBody = _player.getPlayerBodyEntity();
      if (playerBody) {
         if (playerBody.isJumpGrounded() && !playerBody.isJumping()) {
            _player.requestAnimationPlay(animType);

            WeaponStatData weaponData = playerBody.weaponManager.cachedWeaponData;
            if (weaponData != null) {
               // Legacy support for previous implementation
               //SoundEffectManager.self.playLegacyInteractionOneShot(playerBody.weaponManager.equipmentDataId, playerBody.transform);
               // Playing FMOD SFX for interaction
               SoundEffectManager.self.playInteractionSfx(weaponData.actionType, weaponData.weaponClass, weaponData.sfxType, playerBody.transform.position);
            }
            playerBody.playInteractParticles();
         }
      }
   }

   [Command]
   public void Cmd_PlantTree (Vector2 localPosition) {
      PlantableTreeManager.self.plantTree(_player as BodyEntity, _player.areaKey, localPosition);
   }

   [Command]
   public void Cmd_TetherUntetherTree (int treeId) {
      //PlantableTreeManager.self.tetherUntetherTree(_player as BodyEntity, treeId);
   }

   [Command]
   public void Cmd_WaterTree (int treeId) {
      PlantableTreeManager.self.waterTree(_player as BodyEntity, treeId);
   }

   [Command]
   public void Cmd_ChopTree (int treeId) {
      PlantableTreeManager.self.chopTree(_player as BodyEntity, treeId);
   }

   [ClientRpc]
   public void Rpc_ReceiveChopTreeVisual (int treeId, int excludeUserId) {
      // Don't apply for the user that made the chop, he did it himself
      if (Global.player != null && Global.player.userId == excludeUserId) {
         return;
      }

      if (MapManager.self.tryGetPlantableTree(treeId, out PlantableTree tree)) {
         tree.receiveChop();
      }
   }

   [ClientRpc]
   public void Rpc_UpdatePlantableTrees (int id, string areaKey, PlantableTreeInstanceData data) {
      Area area = AreaManager.self.getArea(areaKey);
      if (area != null) {
         PlantableTreeManager.self.updatePlantableTrees(id, area, data);
      }
   }

   [TargetRpc]
   public void Target_UpdatePlantableTrees (NetworkConnection connection, string areaKey, PlantableTreeInstanceData[] data, PlantableTreeDefinition[] def) {
      PlantableTreeManager.self.applyTreeDefinitions(def);
      PlantableTreeManager.self.updatePlantableTrees(areaKey, data);
   }

   [ClientRpc]
   protected void Rpc_UpdateHair (HairLayer.Type newHairType, string newHairPalettes) {
      if (_player is BodyEntity) {
         BodyEntity body = (BodyEntity) _player;
         body.updateHair(newHairType, newHairPalettes);
      }
   }

   [ClientRpc]
   protected void Rpc_UpdateArmor (int armorType, string newArmorPalette) {
      if (_player is BodyEntity) {
         BodyEntity body = (BodyEntity) _player;
         body.armorManager.updateSprites(armorType, newArmorPalette);
      }
   }

   [ClientRpc]
   protected void Rpc_UpdateHat (int hatType, string newHatPalette) {
      if (_player is BodyEntity) {
         BodyEntity body = (BodyEntity) _player;
         body.hatsManager.updateSprites(hatType, newHatPalette);
      }
   }

   [ClientRpc]
   protected void Rpc_UpdateWeapon (int weaponType, string newWeaponPalette) {
      if (_player is BodyEntity) {
         BodyEntity body = (BodyEntity) _player;
         body.weaponManager.updateSprites(weaponType, newWeaponPalette);
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
      npc.insufficientQuestNotice.SetActive(false);
   }

   [TargetRpc]
   public void Target_ToggleInsufficientQuestNotice (NetworkConnection connection, int npcId, bool isActive) {
      NPC npc = NPCManager.self.getNPC(npcId);
      if (isActive) {
         npc.questNotice.SetActive(false);
      }
      npc.insufficientQuestNotice.SetActive(isActive);
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

   #region Experience and Leveling

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
   public void Cmd_ShowLevelUpEffect (Jobs.Type jobType) {
      Rpc_BroadcastShowLevelUpEffect(jobType);
   }

   [ClientRpc]
   private void Rpc_BroadcastShowLevelUpEffect (Jobs.Type jobType) {
      if (_player == null) {
         return;
      }

      _player.showLevelUpEffect(jobType);
   }

   #endregion

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
      D.adminLog("User {" + _player.userId + "} is trying to open lootbag {" + chestId + "}", D.ADMIN_LOG_TYPE.Treasure);

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
      Item item = chest.getContents(_player.userId);
      if (!Item.isValidItem(item)) {
         if (_player) {
            Instance currInstance = InstanceManager.self.getInstance(_player.instanceId);
            D.debug("Cannot process Loot bag rewards for chest {" + chestId + "}! Category is None for: " +
               _player.userId + " " + _player.areaKey + " " +
               chest.rarity + " " + (currInstance == null ? "No Instance" : currInstance.biome.ToString()));
         } else {
            D.debug("Cannot process Loot bag rewards for chest {" + chestId + "}! Category is None, NetEntityNotFound");
         }

         // Replace invalid item into default item
         item = Item.defaultLootItem();
      }

      // Grant the rewards to the user
      List<Item> rewardsToGive = new List<Item>();
      rewardsToGive.Add(item);
      if (LandPowerupManager.self.hasPowerup(_player.userId, LandPowerupType.LootDropBoost)) {
         rewardsToGive.Add(item);
      }

      giveItemRewardsToPlayer(_player.userId, rewardsToGive, false, chest.id);

      // Registers the interaction of loot bags to the achievement database for recording
      AchievementManager.registerUserAchievement(_player, ActionType.OpenedLootBag);
      AchievementManager.registerUserAchievement(_player, ActionType.LootGainTotal);
   }

   [TargetRpc]
   public void Target_GivePowerupToPlayer (NetworkConnection connection, Powerup.Type powerupType, Rarity.Type powerupRarity, int chestId) {
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

      PlayerShipEntity playerShip = _player.getPlayerShipEntity();
      chest.StartCoroutine(chest.CO_CreatingFloatingPowerupIcon(powerupType, powerupRarity, playerShip));

      if (chest.autoDestroy) {
         chest.disableChest();
      }
   }

   [Server]
   public void spawnBattlerMonsterChest (int instanceID, Vector3 position, int enemyID) {
      Instance currentInstance = InstanceManager.self.getInstance(instanceID);
      TreasureManager.self.createBattlerMonsterChest(currentInstance, position, enemyID, _player.userId);
   }

   [Server]
   public void endBattle (string battleId) {
      Target_ReceiveEndBattle(_player.connectionToClient, battleId);
   }

   [TargetRpc]
   public void Target_ReceiveEndBattle (NetworkConnection connection, string battleId) {
      // Remove after confirming fix for battle end
      StartCoroutine(CO_RecalibrateSpriteBody());
      BattleUIManager.self.abilitiesCG.Hide();

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.EndBattle);
   }

   private IEnumerator CO_RecalibrateSpriteBody () {
      // This coroutine recalibrates the sprite renderer of the player body so outlines will render properly after combat
      List<NetEntity> entityGroup = EntityManager.self.getEntitiesWithVoyageId(_player.voyageGroupId);
      if (entityGroup.Count > 0) {
         foreach (NetEntity entity in entityGroup) {
            if (entity is PlayerBodyEntity) {
               entity.getBodyRenderer().enabled = false;
               yield return new WaitForEndOfFrame();
               entity.getBodyRenderer().enabled = true;
            }
         }
      }
   }

   [TargetRpc]
   public void Target_OpenChest (NetworkConnection connection, Item item, int chestId) {
      TreasureChest chest = openChestCommon(chestId);

      if (item.category != Item.Category.None && item.itemTypeId > 0) {
         item = item.getCastItem();
         chest.StartCoroutine(chest.CO_CreatingFloatingIcon(item));
      }
   }

   [TargetRpc]
   public void Target_OpenMapFragmentChest (NetworkConnection connection, int chestId) {
      TreasureChest chest = openChestCommon(chestId);
      chest.StartCoroutine(chest.CO_CreatingFloatingMapFragmentIcon());
   }

   public TreasureChest openChestCommon (int chestId) {
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

      // Play some sounds
      switch (chest.chestType) {
         case ChestSpawnType.Sea:
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.LOOT_BAG, chest.transform.position);
            break;
         case ChestSpawnType.Land:
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.COLLECT_LOOT_LAND, chest.transform.position);
            break;
         case ChestSpawnType.Site:
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.OPEN_CHEST, chest.transform.position);
            break;
      }

      // Register chest id player pref data and set as true, except in treasure site areas since they can be visited again in voyages
      if (!VoyageManager.isTreasureSiteArea(chest.areaKey)) {
         PlayerPrefs.SetInt(TreasureChest.PREF_CHEST_STATE + "_" + Global.userObjects.userInfo.userId + "_" + _player.areaKey + "_" + chest.chestSpawnId, 1);
      }

      if (chest.autoDestroy) {
         chest.disableChest();
      }

      return chest;
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

   private void receiveOnEquipItem (Item equippedWeapon, Item equippedArmor, Item equippedHat, Item soulBoundItem) {
      // Update the equipped items cache
      Global.setUserEquipment(equippedWeapon, equippedArmor, equippedHat);

      // Refresh the inventory panel
      InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);
      if (panel.isShowing()) {
         panel.refreshPanel();
      }

      // Refresh the store panel if it's showing too
      StoreScreen storePanel = (StoreScreen) PanelManager.self.get(Panel.Type.Store);
      if (storePanel.isShowing()) {
         storePanel.refreshPanel();
      }

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStepByWeaponEquipped();

      // This value is not null, when one of the equipped items has been soul bound
      if (soulBoundItem != null) {
         PanelManager.self.noticeScreen.show($"The item {soulBoundItem.itemName} is now bound to you! You can't transfer it to anyone else, and only you can equip it.");
      }
   }

   [TargetRpc]
   public void Target_OnEquipSoulBoundItem (NetworkConnection connection, Item equippedWeapon, Item equippedArmor, Item equippedHat, Item soulBoundItem) {
      receiveOnEquipItem(equippedWeapon, equippedArmor, equippedHat, soulBoundItem);
   }

   [TargetRpc]
   public void Target_OnEquipItem (NetworkConnection connection, Item equippedWeapon, Item equippedArmor, Item equippedHat) {
      receiveOnEquipItem(equippedWeapon, equippedArmor, equippedHat, null);
   }

   [TargetRpc]
   public void Target_ReceiveItemShortcuts (NetworkConnection connection, ItemShortcutInfo[] shortcuts) {
      int shortcutLength = shortcuts.Length;
      for (int i = 0; i < shortcutLength; i++) {
         ItemShortcutInfo itemShortcut = shortcuts[i];
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(itemShortcut.item.itemTypeId);
         if (weaponData == null) {
            D.debug("Weapon data is null for item type: {" + itemShortcut.item.itemTypeId + "} disabling shortcut item, WeaponContentCount: {" + EquipmentXMLManager.self.weaponStatList.Count + "}");

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
   public void Target_ReceiveFriendVisitInfo (NetworkConnection connection, FriendshipInfo[] friendshipInfo, bool isCustomMapSet, int pageNumber, int totalFriendInfoCount) {
      List<FriendshipInfo> friendshipInfoList = new List<FriendshipInfo>(friendshipInfo);

      // Make sure the panel is showing
      VisitListPanel panel = (VisitListPanel) PanelManager.self.get(Panel.Type.VisitPanel);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithFriendshipInfo(isCustomMapSet, friendshipInfoList, totalFriendInfoCount);
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
   public void Target_ReceiveSentMailList (NetworkConnection connection,
   MailInfo[] mailArray, int pageNumber, int totalMailCount) {
      List<MailInfo> mailList = new List<MailInfo>(mailArray);

      // Make sure the panel is showing
      MailPanel panel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

      if (!panel.isShowing()) {
         PanelManager.self.linkPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updateWithSentMailList(mailList, pageNumber, totalMailCount);
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
   public void Target_ReceiveAdminVoyageInstanceList (NetworkConnection connection, Voyage[] voyageArray) {
      List<Voyage> voyageList = new List<Voyage>(voyageArray);

      // Make sure the panel is showing
      PanelManager.self.linkIfNotShowing(Panel.Type.AdminVoyage);

      // Pass the data to the panel
      AdminVoyagePanel panel = (AdminVoyagePanel) PanelManager.self.get(Panel.Type.AdminVoyage);
      panel.receiveVoyageInstancesFromServer(voyageList);
   }

   [TargetRpc]
   public void Target_ReceiveUserInfoListForAdminVoyagePanel (NetworkConnection connection, UserInfo[] userInfoArray) {
      List<UserInfo> userInfoList = new List<UserInfo>(userInfoArray);

      // Pass the data to the panel
      AdminVoyagePanel panel = (AdminVoyagePanel) PanelManager.self.get(Panel.Type.AdminVoyage);
      panel.adminVoyageInfoPanel.updatePanelWithUserList(userInfoList);
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
   public void Cmd_ComplaintTicket (string username, string details) {
      if (_player == null) {
         D.warning("Received a complaint, but the sender doesn't have a PlayerController");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserAccountInfo targetInfo = DB_Main.getUserAccountInfo(username);

         if (targetInfo != null) {
            if (targetInfo.accountId == _player.accountId) {
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  _player.Target_ReceiveNormalChat("You can't report yourself", ChatInfo.Type.Error);
               });
            } else {
               string ipAddress = Util.formatIpAddress(connectionToClient.address);

               SupportTicketInfo ticket = new SupportTicketInfo(details, string.Empty, _player.accountId, _player.userId, _player.entityName,
                  targetInfo.accountId, targetInfo.userId, targetInfo.username, ipAddress, WebToolsUtil.SupportTicketType.Complaint);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  Target_SubmitComplaint(ticket);
               });
            }
         } else {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               string message = string.Format("Could not find user {0}.", username);
               _player.Target_ReceiveNormalChat(message, ChatInfo.Type.Error);
            });
         }
      });
   }

   [TargetRpc]
   public void Target_SubmitComplaint (SupportTicketInfo ticket) {
      SupportTicketManager.self.submitComplaint(ticket);
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
      string guildName = "";

      // Is the player muted or stealth muted?
      bool muted = _player.isMuted();
      bool stealthMuted = muted && _player.isStealthMuted;

      if (_player.guildId > 0) {
         guildIconData = new GuildIconData(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);
         guildName = _player.guildName;
      }

      if (guildIconData != null) {
         guildIconDataString = GuildIconData.guildIconDataToString(guildIconData);
      }

      ChatInfo chatInfo = new ChatInfo(0, message, DateTime.UtcNow, chatType, _player.entityName, "", _player.userId, guildIconData, guildName, stealthMuted, _player.isAdmin());

      // Replace bad words
      message = BadWordManager.ReplaceAll(message);

      // The player is muted (normal) and can't send messages. We notify the player about it
      if (muted && !_player.isStealthMuted) {
         _player.Target_ReceiveNormalChat($"Your message could not be sent because you have been muted until {Util.getTimeInEST(new DateTime(_player.muteExpirationDate))} EST", ChatInfo.Type.System);
         return;
      }

      // Pass this message along to the relevant people
      if (chatType == ChatInfo.Type.Local) {
         _player.Rpc_ChatWasSent(chatInfo.chatId, message, chatInfo.chatTime.ToBinary(), chatType, guildIconDataString, guildName, stealthMuted, _player.isAdmin());
      } else if (chatType == ChatInfo.Type.Emote) {
         // Send the message to all the players in the current area
         List<NetEntity> entities = EntityManager.self.getAllEntities();

         foreach (NetEntity entity in entities) {
            if (entity.areaKey == _player.areaKey && entity.isPlayerEntity()) {
               entity.rpc.Target_ReceiveEmoteMessage(chatInfo);
            }
         }
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
                  _player.Target_ReceiveSpecialChat(_player.connectionToClient, chatInfo.chatId, errorMsg, "", "", chatInfo.chatTime.ToBinary(), ChatInfo.Type.Error, null, "", 0, stealthMuted);
                  return;
               }

               ServerNetworkingManager.self.sendSpecialChatMessage(destinationUserInfo.userId, chatInfo);
               _player.Target_ReceiveSpecialChat(_player.connectionToClient, chatInfo.chatId, message, chatInfo.sender, extractedUserName, chatInfo.chatTime.ToBinary(), chatInfo.messageType, chatInfo.guildIconData, chatInfo.guildName, chatInfo.senderId, stealthMuted);
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
         if (_player.guildId == 0) {
            return;
         }

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            GuildInfo guildInfo = DB_Main.getGuildInfo(_player.guildId);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (UserInfo userInfo in guildInfo.guildMembers) {
                  ServerNetworkingManager.self.sendSpecialChatMessage(userInfo.userId, chatInfo);
               }
            });
         });
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
      if (!stealthMuted) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.storeChatLog(_player.userId, _player.entityName, message, chatInfo.chatTime, chatType, connectionToClient.address);
         });
      }
   }

   [TargetRpc]
   public void Target_ReceiveMapInfo (Map map) {
      AreaManager.self.storeAreaInfo(map);
   }

   [TargetRpc]
   public void Target_ReceivePowerup (Powerup.Type powerupType, Rarity.Type rarity, Vector3 spawnSource) {
      StartCoroutine(PowerupManager.self.CO_CreatingFloatingPowerupIcon(powerupType, rarity, (PlayerShipEntity) _player, spawnSource));
   }

   [TargetRpc]
   public void Target_ReceiveAreaInfo (NetworkConnection connection, string areaKey, string baseMapAreaKey, int latestVersion, Vector3 mapPosition, MapCustomizationData customizations, Biome.Type biome) {
      // Check if we already have the Area created
      Area area = AreaManager.self.getArea(areaKey);
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.LeaveVoyageGroup);

      if (area != null && area.version == latestVersion) {
         if (area.isInterior) {
            if (Global.player != null) {
               SoundEffectManager.self.playDoorSfx(SoundEffectManager.DoorAction.Close, biome, Global.player.transform.position);
            }
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

            // There is no leader in this guild - choose new leader randomly, from highest ranking members
            if (_player.guildId > 0) {
               List<UserInfo> userInfos = info.guildMembers.ToList();
               List<int> userRankPriorities = new List<int>();
               List<int> userRankIDs = new List<int>();
               if (!userInfos.Exists(x => x.guildRankId == 0)) {
                  // Get rank priorities
                  UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                     foreach (UserInfo userInfo in userInfos) {
                        userRankPriorities.Add(DB_Main.getGuildMemberRankPriority(userInfo.userId));
                        userRankIDs.Add(DB_Main.getGuildMemberRankId(userInfo.userId));
                     }

                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        int minRankPriority = userRankPriorities.Min();
                        int gldRankId = userInfos[userRankPriorities.FindIndex(x => x == minRankPriority)].guildRankId;
                        List<UserInfo> highestRankUsers = userInfos.FindAll(x => x.guildRankId == gldRankId);

                        // Choose leader randomly
                        UserInfo newLeader = highestRankUsers.ChooseRandom();
                        int index = userInfos.FindIndex(x => x == newLeader);

                        // Set new leader in database
                        UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                           DB_Main.assignRankGuild(info.guildMembers[index].userId, 0);
                        });
                     });
                  });
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
   public void Cmd_NotifyOnlineStatusToFriends (bool isOnline, string customMessage) {
      if (_player == null) {
         return;
      }

      notifyOnlineStatusToFriends(_player.userId, isOnline, customMessage);
   }

   [Server]
   public static void notifyOnlineStatusToFriends (int userId, bool isOnline, string customMessage) {
      // Notify Friends
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         string userName = DB_Main.getUserName(userId);
         List<FriendshipInfo> friends = DB_Main.getFriendshipInfoList(userId, Friendship.Status.Friends, 1, 200);

         foreach (FriendshipInfo friend in friends) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               UserInfo friendUserInfo = DB_Main.getUserInfo(friend.friendName);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (friendUserInfo == null) {
                     D.error($"The player {friend.friendName} couldn't be found. online status notification wasn't sent.");
                     return;
                  }

                  // Adjust message if not valid
                  if (Util.isEmpty(customMessage)) {
                     if (isOnline) {
                        customMessage = $"{userName} is online!";
                     } else {
                        customMessage = $"{userName} went offline.";
                     }
                  }

                  ChatInfo chatInfo = new ChatInfo(0,
                     customMessage, DateTime.UtcNow,
                     isOnline ? ChatInfo.Type.UserOnline : ChatInfo.Type.UserOffline,
                     senderId: userId,
                     sender: userName,
                     receiver: friend.friendName);

                  ServerNetworkingManager.self.sendSpecialChatMessage(friendUserInfo.userId, chatInfo);
                  //D.debug($"Player {userName} changed online status. Friend '{friend.friendName}' ({friendUserInfo.userId}) has been notified!");
               });
            });
         }
      });
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

   private bool isStoreItemEnabled (StoreItem storeItem) {
      // A store item should be displayed if the store item itself is enabled and if the referred item is enabled
      bool storeItemIsEnabled = storeItem.isEnabled;
      bool referredItemIsEnabled = false;

      switch (storeItem.category) {
         case Item.Category.None:
            referredItemIsEnabled = false;
            break;
         case Item.Category.Weapon:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Armor:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Hats:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Potion:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Usable:
            referredItemIsEnabled = true;
            break;
         case Item.Category.CraftingIngredients:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Blueprint:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Currency:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Quest_Item:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Haircut:
            referredItemIsEnabled = (HaircutXMLManager.self.getHaircutData(storeItem.itemId) != null);
            break;
         case Item.Category.Gems:
            referredItemIsEnabled = (GemsXMLManager.self.getGemsData(storeItem.itemId) != null);
            break;
         case Item.Category.ShipSkin:
            referredItemIsEnabled = (ShipSkinXMLManager.self.getShipSkinData(storeItem.itemId) != null);
            break;
         case Item.Category.Dye:
            referredItemIsEnabled = (DyeXMLManager.self.getDyeData(storeItem.itemId) != null);
            break;
         case Item.Category.Pet:
            referredItemIsEnabled = true;
            break;
         case Item.Category.Consumable:
            referredItemIsEnabled = (ConsumableXMLManager.self.getConsumableData(storeItem.itemId) != null);
            break;
         default:
            break;
      }

      return storeItemIsEnabled && referredItemIsEnabled;
   }

   [Command]
   public void Cmd_RequestStoreResources (bool requestStoreItems) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<StoreItem> storeItems = null;

         if (requestStoreItems) {
            List<StoreItem> allStoreItems = DB_Main.getAllStoreItems();
            storeItems = allStoreItems.Where(isStoreItemEnabled).ToList();
         }

         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveStoreResources(storeItems, userObjects);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveStoreResources (List<StoreItem> items, UserObjects userObjects) {
      Global.userObjects = userObjects;
      StoreScreen.self.onReceiveStoreResources(items);
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
         UserInfo userInfo = DB_Main.getUserInfoById(_player.userId);

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

               AchievementManager.registerUserAchievement(_player, ActionType.TrashItem);

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
         Item swappedItem = slotNumber <= oldShortcutList.Count ? DB_Main.getItem(_player.userId, oldShortcutList[slotNumber - 1].itemId) : null;
         Item equippedItem = null;

         // Check if we have an equipped item of the type we are swapping with
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         if (swappedItem != null) {
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
         }

         // Clear old item from shortcut
         if (oldShortcutList.Exists(_ => _.itemId == itemId)) {
            List<ItemShortcutInfo> oldItems = oldShortcutList.FindAll(_ => _.itemId == itemId);
            foreach (ItemShortcutInfo oldItem in oldItems) {
               DB_Main.deleteItemShortcut(_player.userId, oldItem.slotNumber);
            }
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

   [Command]
   public void Cmd_SwapItemShortcut (int slotNumber1, int slotNumber2) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get shortcut list
         List<ItemShortcutInfo> shortcutList = DB_Main.getItemShortcutList(_player.userId);

         ItemShortcutInfo shortcut1 = shortcutList.FirstOrDefault(i => i.slotNumber == slotNumber1);
         ItemShortcutInfo shortcut2 = shortcutList.FirstOrDefault(i => i.slotNumber == slotNumber2);

         if (shortcut1 == null && shortcut2 == null) {
            return;
         }

         if (shortcut1 != null) {
            DB_Main.updateItemShortcut(_player.userId, slotNumber2, shortcut1.itemId);
         } else {
            DB_Main.deleteItemShortcut(_player.userId, slotNumber2);
         }

         if (shortcut2 != null) {
            DB_Main.updateItemShortcut(_player.userId, slotNumber1, shortcut2.itemId);
         } else {
            DB_Main.deleteItemShortcut(_player.userId, slotNumber1);
         }

         shortcutList = DB_Main.getItemShortcutList(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the updated shortcuts to the client
            Target_ReceiveItemShortcuts(_player.connectionToClient, shortcutList.ToArray());
         });
      });
   }

   [ClientRpc]
   public void Rpc_PlayWarpEffect (Vector3 position) {
      // Only play the effect locally if the player is in their ship
      if (!_player.isInvisible && !_player.isGhost && (!_player.isLocalPlayer || _player.getPlayerBodyEntity() == null)) {
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
   public void Cmd_SellCrops (int offerId, int amountToSell, Rarity.Type rarityToSellAt, int shopId) {
      _player.cropManager.sellCrops(offerId, amountToSell, rarityToSellAt, shopId);
   }

   #region Store

   [Command]
   public void Cmd_BuyStoreItem (ulong itemId, uint appId) {
      if (_player == null) {
         return;
      }

      // Off to the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         StoreItem storeItem = DB_Main.getStoreItem(itemId);

         // Check the store item
         if (storeItem == null) {
            D.error($"User {_player.userId} tried to purchase a missing store item (ID: '{itemId}').");
            reportStorePurchaseFailed();
            return;
         }

         if (!storeItem.isEnabled) {
            D.error($"User {_player.userId} tried to purchase a disabled store item (ID: '{itemId}').");
            reportStorePurchaseFailed();
            return;
         }

         // Check if the item should be paid with gems or with real money (e.g. through Steam)
         if (storeItem.currencyMode == StoreItem.CurrencyMode.Game) {
            buyItem(itemId);
         }

         if (storeItem.currencyMode == StoreItem.CurrencyMode.Real) {
            buySteamItem(itemId, appId);
         }
      });
   }

   [Server]
   private void buyItem (ulong itemId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Look up the item box for the specified item id
         StoreItem storeItem = DB_Main.getStoreItem(itemId);
         Item storeReferencedItem = null;

         int gems = DB_Main.getGems(_player.accountId);

         // Make sure they can afford it
         if (gems < storeItem.price) {
            reportStorePurchaseFailed("You don't have " + storeItem.price + " gems!");
            return;
         }

         if (storeItem.category == Item.Category.Haircut) {
            HaircutData haircutData = HaircutXMLManager.self.getHaircutData(storeItem.itemId);

            if (haircutData == null) {
               D.error($"Store Purchase failed for user '{_player.userId}' trying to purchase the Store Item '{storeItem.id}'. Couldn't find the haircut data.");
               reportStorePurchaseFailed();
               return;
            }

            storeReferencedItem = Haircut.createFromData(haircutData);
         }

         if (storeItem.category == Item.Category.Dye) {
            DyeData dyeData = DyeXMLManager.self.getDyeData(storeItem.itemId);

            if (dyeData == null) {
               D.error($"Store Purchase failed for user '{_player.userId}' trying to purchase the Store Item '{storeItem.id}'. Couldn't find the hair dye data.");
               reportStorePurchaseFailed();
               return;
            }

            storeReferencedItem = Dye.createFromData(dyeData);
         }

         if (storeItem.category == Item.Category.ShipSkin) {
            ShipSkinData shipSkinData = ShipSkinXMLManager.self.getShipSkinData(storeItem.itemId);

            if (shipSkinData == null) {
               D.error($"Store Purchase failed for user '{_player.userId}' trying to purchase the Store Item '{storeItem.id}'. Couldn't find the ship skin data.");
               reportStorePurchaseFailed();
               return;
            }

            storeReferencedItem = ShipSkin.createFromData(shipSkinData);
         }

         if (storeItem.category == Item.Category.Consumable) {
            ConsumableData consumableData = ConsumableXMLManager.self.getConsumableData(storeItem.itemId);

            if (consumableData == null) {
               D.error($"Store Purchase failed for user '{_player.userId}' trying to purchase the Store Item '{storeItem.id}'. Couldn't find the consumable data.");
               reportStorePurchaseFailed();
               return;
            }

            storeReferencedItem = Consumable.createFromData(consumableData);
         }

         if (storeItem.category == Item.Category.Hats) {
            HatStatData hatStatData = EquipmentXMLManager.self.getHatData(storeItem.itemId);

            if (hatStatData == null) {
               D.error($"Store Purchase failed for user '{_player.userId}' trying to purchase the Store Item '{storeItem.id}'. Couldn't find the consumable data.");
               reportStorePurchaseFailed();
               return;
            }

            storeReferencedItem = HatStatData.translateDataToHat(hatStatData);
         }

         if (storeReferencedItem == null) {
            D.error($"Store Purchase failed for user '{_player.userId}' trying to purchase the Store Item '{storeItem.id}'.");
            reportStorePurchaseFailed();
            return;
         }

         // New Method, needs observation
         //Item updatedItem = DB_Main.createItemOrUpdateItemCount(_player.userId, storeReferencedItem);
         processPurchasedItemCreation(_player.userId, storeReferencedItem, storeItem);
      });
   }

   [Server]
   private void buySteamItem (ulong itemId, uint appId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Look up the item box for the specified item id
         StoreItem storeItem = DB_Main.getStoreItem(itemId);

         try {
            if (_player == null) {
               D.debug($"The reference to the player is null.");
               return;
            }

            D.debug($"Steam Purchase Request Received from User {_player.userId}");

            D.debug($"The appID is {appId}");

            if (appId == 0) {
               D.debug($"The appId is not valid.");
               reportStorePurchaseFailed();
               return;
            }

            bool isSteamIdValid = ulong.TryParse(_player.steamId, out ulong steamId);

            if (isSteamIdValid) {
               D.debug($"Parsed steamId for user '{_player.userId}' is {steamId}");
            }

            if (!isSteamIdValid) {
#if UNITY_EDITOR
               // Try to use the debug Steam Id instead
               steamId = SteamPurchaseManagerServer.self.debugSteamId;
               D.debug($"Purchase Warning: Couldn't get the steamId for user '{_player.userId}'. Using the debug Steam ID...");
#endif
            }

            if (steamId == 0) {
               D.debug($"Steam Purchase Error: Steam ID '{steamId}' is not valid. Can't process the purchase for user '{_player.userId}'.");
               reportStorePurchaseFailed();
               return;
            }

            Task<GetUserInfoResult> t = SteamPurchaseManagerServer.self.getUserInfo(steamId);
            t.Wait();

            D.debug("Steam Purchase. GetUserInfo: " + JsonConvert.SerializeObject(t.Result));

            // Check if the user can make this purchase
            GetUserInfoParameters.UserStatus status = t.Result.parameters.getStatus();
            if (status == GetUserInfoParameters.UserStatus.LockedFromPurchasing) {
               D.debug($"Steam Purchase Error: User '{_player.userId}' is not allowed to make Steam purchases. Can't process the purchase.");
               reportStorePurchaseFailed();
               return;
            }

            D.debug("Steam Purchase. Creating new order...");

            // Create the Steam Order
            ulong newOrderId = DB_Main.createSteamOrder();

            if (newOrderId <= 0) {
               D.debug("Steam Purchase. Creating new order: Failed.");
               reportStorePurchaseFailed();
               return;
            }

            D.debug("Steam Purchase. Creating new order: OK");

            D.debug("Steam Purchase. Creating Purchase Information...");

            // Try to obtain the description of the item
            string itemDescription = "";

            if (storeItem.category == Item.Category.Haircut) {
               HaircutData haircutData = HaircutXMLManager.self.getHaircutData(storeItem.itemId);
               itemDescription = haircutData.itemDescription;
            } else if (storeItem.category == Item.Category.Gems) {
               GemsData gemsData = GemsXMLManager.self.getGemsData(storeItem.itemId);
               itemDescription = gemsData.itemDescription;
            }

            // Create a Steam Purchase Item from the selected item
            SteamPurchaseItem item = new SteamPurchaseItem() {
               quantity = 1,
               category = storeItem.category.ToString(),
               description = itemDescription,
               itemId = storeItem.id.ToString(),
               totalCost = storeItem.price
            };

            // Bundle the items into a Purchase Info
            SteamPurchaseInfo purchase = new SteamPurchaseInfo(newOrderId, steamId, appId, "en", "usd", new List<SteamPurchaseItem> { item });

            D.debug("Steam Purchase. Creating Purchase Information: OK");

            // Update the order
            D.debug("Steam Purchase. Updating Steam Order...");
            DB_Main.updateSteamOrder(newOrderId, _player.userId, "pending", JsonConvert.SerializeObject(purchase));
            D.debug("Steam Purchase. Updating Steam Order: OK");

            // Open order
            D.debug("Steam Purchase. Toggling Steam Order...");
            DB_Main.toggleSteamOrder(newOrderId, false);
            D.debug("Steam Purchase. Toggling Steam Order: OK");

            // Start the purchase workflow
            D.debug("Steam Purchase. Transferring Control to Steam...");
            Task<InitTxnResult> t2 = SteamPurchaseManagerServer.self.initTxn(purchase);
            t2.Wait();
            D.debug("Steam Purchase. InitTxn: " + JsonConvert.SerializeObject(t2.Result));

            if (!t2.Result.isSuccess()) {
               D.debug("Steam Purchase. Transferring Control to Steam: Failed");
               D.debug("Steam Purchase Error: Couldn't start the purchase transaction. Reverting...");
               DB_Main.deleteSteamOrder(newOrderId);
               reportStorePurchaseFailed();
               return;
            }

            D.debug("Steam Purchase. Transfer Control to Steam: OK");
         } catch (Exception ex) {
            D.error(ex.Message);
            D.error(ex.StackTrace);
            reportStorePurchaseFailed();
         }
      });
   }

   [Command]
   public void Cmd_CompleteSteamPurchase (ulong orderId, uint appId, bool purchaseAuthorized) {
      D.debug($"Steam Purchase: Received Authorization Response! orderId:'{orderId}' authorized:'{purchaseAuthorized}'");

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         try {
            // Check if the order exists
            SteamOrder order = DB_Main.getSteamOrder(orderId);

            if (order == null) {
               D.error($"Steam Purchase Error: Couldn't find order '{orderId}'. Can't complete the Steam purchase.");
               reportStorePurchaseFailed();
               return;
            }

            // Ensure the order belongs to the current user
            if (order.userId != _player.userId) {
               D.error($"Steam Purchase Error: The order '{orderId}' belongs to user '{order.userId}' and not to the current user '{_player.userId}'.");
               reportStorePurchaseFailed();
               return;
            }

            // If the order is already closed, skip the process
            if (order.closed) {
               D.error($"Steam Purchase Error: The order '{orderId}' is already closed.");
               reportStorePurchaseFailed();
               return;
            }

            if (!purchaseAuthorized) {
               // Update the order - canceled
               DB_Main.updateSteamOrder(orderId, _player.userId, "canceled", order.content);

               // Close the order
               DB_Main.toggleSteamOrder(orderId, true);

               reportStorePurchaseFailed("Purchase Canceled.");
               return;
            }

            // Update the order - finalizing
            DB_Main.updateSteamOrder(orderId, _player.userId, "finalizing", order.content);

            // Finalize the transaction
            var task = SteamPurchaseManagerServer.self.finalizeTxn(orderId, appId);
            task.Wait();
            var result = task.Result;

            if (!result.isSuccess()) {
               D.error("Steam Purchase Finalization failed. response: " + JsonConvert.SerializeObject(result.result));

               // Update the order - error
               DB_Main.updateSteamOrder(orderId, _player.userId, "error", order.content);

               // Report to the user - error
               reportStorePurchaseFailed();
               return;
            }

            // Update the order - applying
            DB_Main.updateSteamOrder(orderId, _player.userId, "applying", order.content);

            // Assign the purchased item
            SteamPurchaseInfo purchase = JsonConvert.DeserializeObject<SteamPurchaseInfo>(order.content);
            foreach (SteamPurchaseItem item in purchase.items) {
               StoreItem storeItem = DB_Main.getStoreItem(ulong.Parse(item.itemId));

               if (storeItem.category == Item.Category.Gems) {
                  // Assign the gems
                  int accountId = DB_Main.getAccountId(_player.userId);
                  DB_Main.addGems(accountId, storeItem.quantity);
               }
            }

            // Update the order - success
            DB_Main.updateSteamOrder(orderId, _player.userId, "success", order.content);

            // Close the order
            DB_Main.toggleSteamOrder(orderId, true);

            // Report to the user
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Target_OnStorePurchaseCompleted(_player.connectionToClient, Item.Category.Gems, "Gems", 0, false);
            });
         } catch (Exception ex) {
            D.error($"[{ex.GetHashCode()}] error while processing order {orderId} by player {_player.userId}");
            D.error($"[{ex.GetHashCode()}]" + ex.Message);
            D.error($"[{ex.GetHashCode()}]" + ex.StackTrace);
            reportStorePurchaseFailed();
         }
      });
   }

   [TargetRpc]
   public void Target_OnStorePurchaseCompleted (NetworkConnection connection, Item.Category itemCategory, string itemName, int itemId, bool soulBound) {
      string feedback = "Thank you for your purchase!";

      // Play the SFX for purchasing an item
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.PURCHASE_ITEM);

      bool canUseImmediately = Item.isUsable(itemCategory) || itemCategory == Item.Category.Hats;

      if (canUseImmediately) {
         string itemCategoryDisplayName = "item";

         if (itemCategory == Item.Category.ShipSkin) {
            itemCategoryDisplayName = "ship skin";
         } else if (itemCategory == Item.Category.Haircut) {
            itemCategoryDisplayName = "haircut";
         } else if (itemCategory == Item.Category.Dye) {
            itemCategoryDisplayName = "dye";
         } else if (itemCategory == Item.Category.Hats) {
            itemCategoryDisplayName = "hat";
         }

         if (!string.IsNullOrWhiteSpace(itemName)) {
            feedback = $"You have purchased the {itemCategoryDisplayName} '{itemName}'!";
         } else {
            feedback = $"You have purchased a new {itemCategoryDisplayName}!";
         }

         feedback += " You can find it in your inventory.\n\nDo you want to use it now?";
      }

      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
         if (canUseImmediately) {
            PanelManager.self.confirmScreen.hide();
            _player.rpc.Cmd_RequestUseItem(itemId, confirmed: false);
         } else {
            if (StoreScreen.self.isShowing()) {
               StoreScreen.self.refreshPanel();
            }
         }
      });

      if (canUseImmediately) {
         PanelManager.self.confirmScreen.cancelButton.onClick.RemoveAllListeners();
         PanelManager.self.confirmScreen.cancelButton.onClick.AddListener(() => {
            if (StoreScreen.self.isShowing()) {
               StoreScreen.self.refreshPanel();
            }

            if (soulBound) {
               PanelManager.self.noticeScreen.show($"The item is now bound to you! It can't be transferred and only you can use it.");
            }
         });

         PanelManager.self.confirmScreen.showYesNo(feedback);
      } else {
         PanelManager.self.confirmScreen.show(feedback);
      }
   }

   private void reportStorePurchaseFailed (string customMessage = null) {
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         string feedback = string.IsNullOrWhiteSpace(customMessage) ? "Purchase failed" : customMessage;
         ServerMessageManager.sendError(ErrorMessage.Type.StoreItemPurchaseFailed, _player, feedback);
      });
   }

   #endregion

   #region Usables

   [Command]
   public void Cmd_RequestUseItem (int itemId, bool confirmed) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Item item = DB_Main.getItem(_player.userId, itemId);

         if (item == null) {
            D.error($"Player {_player.userId} tried to use an item (id: {itemId}), but the item couldn't be found.");
            return;
         }

         if (!confirmed) {
            // Some consumables need confirmation from the player
            if (item.category == Item.Category.Consumable) {
               ConsumableData consumableData = ConsumableXMLManager.self.getConsumableData(item.itemTypeId);

               if (consumableData == null) {
                  reportUseItemFailure("");
                  return;
               }

               if (consumableData.consumableType == Consumable.Type.PerkResetter) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     Target_showUseItemConfirmationPrompt(_player.connectionToClient, "Do you really want to reset your perk points?", "Perks Points Unchanged.", itemId);
                  });

                  return;
               }
            }
         }

         useItem(itemId);
      });
   }

   [Server]
   private void useItem (int itemId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Item item = DB_Main.getItem(_player.userId, itemId);

         if (item == null) {
            D.error($"Player {_player.userId} tried to use an item (id: {itemId}), but the item couldn't be found.");
            return;
         }

         if (item.category == Item.Category.Dye) {
            useDye(item);
         } else if (item.category == Item.Category.ShipSkin) {
            useShipSkin(item);
         } else if (item.category == Item.Category.Haircut) {
            useHaircut(item);
         } else if (item.category == Item.Category.Consumable) {
            useConsumable(item);
         } else if (item.category == Item.Category.Hats) {
            requestSetHatId(item.id);
         }
      });
   }

   [Server]
   private void useHaircut (Item item) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         HaircutData haircutData = HaircutXMLManager.self.getHaircutData(item.itemTypeId);
         HairLayer.Type newHairType = haircutData.type;

         // Make sure the gender is right
         if (haircutData.getGender() != _player.gender) {
            reportUseItemFailure("This haircut is for a different head shape.");
            return;
         }

         // The player has this hairstyle
         if (haircutData.type == _player.hairType) {
            reportUseItemFailure("You already have this hairstyle!");
            return;
         }

         // Set the new hair in the database
         DB_Main.setHairType(_player.userId, newHairType);

         // Delete the item
         DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, 1);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.hairType = newHairType;

            // Update the hair but keep the color
            Rpc_UpdateHair(newHairType, _player.hairPalettes);

            // Let the client know the item was used
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedHaircut, _player, item.id.ToString());
         });
      });
   }

   [Server]
   private void useDye (Item item) {
      DyeData dyeData = DyeXMLManager.self.getDyeData(item.itemTypeId);

      if (dyeData == null) {
         D.error($"Player {_player.userId} tried to use the dye {item.itemTypeId} but failed. Couldn't find the dye data.");
         reportUseItemFailure("Unknown Dye");
         return;
      }

      PaletteToolData paletteToolData = PaletteSwapManager.self.getPalette(dyeData.paletteId);

      // Check if the palette was found
      if (paletteToolData == null) {
         D.error($"Player {_player.userId} tried to apply the dye {dyeData.paletteId} (palette: {paletteToolData.paletteName}) but failed. Couldn't find the palette.");
         reportUseItemFailure("Unknown Dye");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Hair Dye
         if (paletteToolData.paletteType == (int) PaletteToolManager.PaletteImageType.Hair) {
            if (paletteToolData.isPrimary()) {
               // Set the new hair color in the database
               DB_Main.setHairColor(_player.userId, paletteToolData.paletteName);

               // Delete the item
               DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, 1);

               // Fetch the updated user objects
               UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

               // Back to Unity
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  _player.hairPalettes = paletteToolData.paletteName;

                  // Refresh local store
                  Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);

                  // Update the hair color but keep the style
                  Rpc_UpdateHair(_player.hairType, _player.hairPalettes);

                  // Let the client know the item was used
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedHairDye, _player, item.id.ToString());
               });

               return;
            }
         }

         bool isHatDye = paletteToolData.paletteName.ToLower().StartsWith("hat"); // TODO: Refactor this line as soon as the Web Palette Tool supports hats

         // At the moment hat palettes are "flagged" as armors. Therefore, we check the palette name to tell armor dyes and hat dyes apart
         if (paletteToolData.paletteType == (int) PaletteToolManager.PaletteImageType.Armor && !isHatDye) {
            // Compute new palette
            string dyePalette = paletteToolData.paletteName;
            UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
            string currentArmorPalette = userObjects.armor.paletteNames;
            string mergedPalette = Item.parseItmPalette(Item.overridePalette(dyePalette, currentArmorPalette));

            // Set the new armor color
            bool updated = DB_Main.setArmorPalette(_player.userId, userObjects.armor.id, mergedPalette);

            if (!updated) {
               reportUseItemFailure("");
               return;
            }

            // Delete the dye
            DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, 1);

            Bkg_RequestSetArmorId(userObjects.armor.id);

            // Fetch the updated user objects
            userObjects = DB_Main.getUserObjects(_player.userId);

            // Back to Unity
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Refresh local store
               Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);

               // Report other clients
               ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(userObjects.armor.itemTypeId);
               Rpc_UpdateArmor(armorData.armorType, mergedPalette);

               // Let the client know the item was used
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedArmorDye, _player, item.id.ToString());
            });

            return;
         }

         // Hat Dye
         if (paletteToolData.paletteType == (int) PaletteToolManager.PaletteImageType.Hat || isHatDye) {
            UserObjects userObjects = DB_Main.getUserObjects(_player.userId);

            // A hat dye can only be applied if the player has a hat
            if (userObjects.hat == null || userObjects.hat.itemTypeId == 0) {
               reportUseItemFailure("You are not wearing a hat!");
               return;
            }

            // Compute new palette
            string dyePalette = paletteToolData.paletteName;
            string currentHatPalette = userObjects.hat.paletteNames;
            string mergedPalette = Item.parseItmPalette(Item.overridePalette(dyePalette, currentHatPalette));

            // Set the new hat color
            bool updated = DB_Main.setHatPalette(_player.userId, userObjects.hat.id, mergedPalette);

            if (!updated) {
               reportUseItemFailure("");
               return;
            }

            // Delete the dye
            DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, 1);

            Bkg_RequestSetHatId(userObjects.hat.id);

            // Back to Unity
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Let the client know the item was used
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedHatDye, _player, item.id.ToString());
            });

            return;
         }

         // Weapon Dye
         if (paletteToolData.paletteType == (int) PaletteToolManager.PaletteImageType.Weapon) {
            UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
            Item weapon = userObjects.weapon;

            if (weapon == null || weapon.itemTypeId == 0) {
               reportUseItemFailure("You are not wearing wielding any weapon!");
               return;
            }

            // Compute new palette
            string dyePalette = paletteToolData.paletteName;
            string currentWeaponPalette = weapon.paletteNames;
            string mergedPalette = Item.parseItmPalette(Item.overridePalette(dyePalette, currentWeaponPalette));

            // Set the new weapon color
            bool updated = DB_Main.setWeaponPalette(_player.userId, weapon.id, mergedPalette);

            if (!updated) {
               reportUseItemFailure("");
               return;
            }

            // Delete the dye
            DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, 1);

            Bkg_RequestSetWeaponId(weapon.id);

            // Back to Unity
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Let the client know the item was used
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedWeaponDye, _player, item.id.ToString());
            });

            return;
         }

         // Unknown or unsupported dye type
         D.error($"Player {_player.userId} tried to apply the dye {dyeData.paletteId} (palette: {paletteToolData.paletteName}). This dye type is currently unsupported.");
         reportUseItemFailure("Unsupported Dye");
         return;
      });
   }

   [Server]
   private void useShipSkin (Item item) {
      PlayerShipEntity playerShip = _player.getPlayerShipEntity();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         if (playerShip == null) {
            reportUseItemFailure("A ship skin can only be applied at sea!");
            return;
         }

         ShipSkinData shipSkinData = ShipSkinXMLManager.self.getShipSkinData(item.itemTypeId);
         ShipInfo currentShipInfo = DB_Main.getShipInfo(playerShip.shipId);

         if (shipSkinData.skinType == Ship.SkinType.None && currentShipInfo.skinType == Ship.SkinType.None) {
            reportUseItemFailure("Your ship doesn't have a skin applied!");
            return;
         }

         if (shipSkinData.skinType != Ship.SkinType.None && currentShipInfo.shipType != shipSkinData.shipType) {
            reportUseItemFailure("You can't use this skin on your current ship!");
            return;
         }

         if (shipSkinData.skinType != Ship.SkinType.None && currentShipInfo.skinType != Ship.SkinType.None && shipSkinData.skinType == currentShipInfo.skinType) {
            reportUseItemFailure("Your ship already has this skin!");
            return;
         }

         DB_Main.setShipSkin(playerShip.shipId, shipSkinData.skinType);

         // update the ship skin
         playerShip.skinType = shipSkinData.skinType;

         // Delete the item
         DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, 1);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            playerShip.Target_UpdateSkin();

            // Let the client know the item was used
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedShipSkin, _player, item.id.ToString());
         });
      });
   }

   [Server]
   private void useConsumable (Item item) {
      if (item.category == Item.Category.Consumable) {
         ConsumableData consumableData = ConsumableXMLManager.self.getConsumableData(item.itemTypeId);

         if (consumableData == null) {
            reportUseItemFailure("");
         }

         if (consumableData.consumableType == Consumable.Type.PerkResetter) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               List<Perk> perks = DB_Main.getPerkPointsForUser(_player.userId);
               int unassignedPoints = 0;
               int assignedPoints = 0;

               foreach (Perk perk in perks) {
                  if (perk.perkId == 0) {
                     unassignedPoints += perk.points;
                  } else {
                     assignedPoints += perk.points;
                  }
               }

               D.debug($"Player {_player.userId} requested to reset all the perk points (assigned points: {assignedPoints}, unassigned points: {unassignedPoints})");
               bool success = DB_Main.resetPerkPointsAll(_player.userId, assignedPoints + unassignedPoints);

               if (!success) {
                  D.error($"Player {_player.userId} requested to reset all the perk points: FAIL");
                  reportUseItemFailure("Couldn't reset your perk points.");
                  return;
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  D.debug($"Player {_player.userId} requested to reset all the perk points: OK");
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.UsedConsumable, _player, "You have reset your Perk Points!");
               });
            });
         }
      }
   }

   [Server]
   private void reportUseItemFailure (string customMessage) {
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         string feedback = string.IsNullOrWhiteSpace(customMessage) ? "Couldn't use the item." : customMessage;
         ServerMessageManager.sendError(ErrorMessage.Type.UseItemFailed, _player, feedback);
      });
   }

   [TargetRpc]
   public void Target_showUseItemConfirmationPrompt (NetworkConnection connection, string customMessage, string cancelMessage, int itemId) {
      if (PanelManager.self == null) {
         return;
      }

      PanelManager.self.confirmScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.cancelButton.onClick.AddListener(() => {
         PanelManager.self.confirmScreen.hide();
         PanelManager.self.noticeScreen.show(cancelMessage);

         if (InventoryPanel.self != null && InventoryPanel.self.isShowing()) {
            InventoryPanel.self.refreshPanel();
         }

         if (StoreScreen.self != null && StoreScreen.self.isShowing()) {
            StoreScreen.self.refreshPanel();
         }
      });

      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
         PanelManager.self.confirmScreen.hide();

         // The player confirmed, so now it is safe to use the item
         _player.rpc.Cmd_RequestUseItem(itemId, confirmed: true);
      });

      PanelManager.self.confirmScreen.showYesNo(customMessage);
   }

   #endregion

   [Command]
   public void Cmd_UpdateGems () {
      if (_player == null) {
         D.warning("Null player.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gems = DB_Main.getGems(_player.accountId);

         // Back to Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_UpdateGems(_player.connectionToClient, gems);
         });
      });
   }

   [Command]
   public void Cmd_GetCropOffersForShop (int shopId) {
      bool useShopName = shopId > 0 ? true : false;

      // Get the current list of offers for the area
      List<CropOffer> list = new List<CropOffer>();
      if (useShopName) {
         list = ShopManager.self.getOffersByShopId(shopId);
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
               shopData = ShopXMLManager.self.getShopDataById(shopId);
               greetingText = shopData.shopGreetingText;
            }

            _player.rpc.Target_ReceiveOffers(_player.connectionToClient, gold, list.ToArray(), greetingText);
         });
      });
   }

   [Command]
   public void Cmd_GetItemsForArea (int shopId) {
      getItemsForArea(shopId);
   }

   [Command]
   public void Cmd_GetShipsForArea (int shopId) {
      getShipsForArea(shopId);
   }

   #region NPCQuest

   [Command]
   public void Cmd_RequestNPCQuestSelectionListFromServer (int npcId) {
      float timeSinceTalkedToNPC = (float) NetworkTime.time - _player.lastNPCTalkTime;
      if (timeSinceTalkedToNPC > 20.0f) {
         _player.lastNPCTalkTime = (float) NetworkTime.time;
         AchievementManager.registerUserAchievement(_player, ActionType.TalkToNPC);
      }

      NPCData npcData = NPCManager.self.getNPCData(npcId);
      int questId = npcData.questId;
      QuestData questData = NPCQuestManager.self.getQuestData(questId);
      if (questData == null) {
         questData = NPCQuestManager.self.getQuestData(NPCQuestManager.BLANK_QUEST_ID);
         D.debug("Using a blank quest template due to npc data (" + npcId + ") having no assigned quest id");
      }
      D.adminLog("Player {" + _player.userId + "} requesting quest info from :{" + npcData.npcId + ":" + npcData.name + "} using quest {" + questData.questGroupName + "}", D.ADMIN_LOG_TYPE.Quest);

      List<QuestDataNode> xmlQuestNodeList = new List<QuestDataNode>(questData.questDataNodes);
      List<QuestDataNode> removeNodeList = new List<QuestDataNode>();
      List<QuestDataNode> insufficientLevelNodeList = new List<QuestDataNode>();

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

               D.adminLog("Total nodes to check :{" + xmlQuestNodeList.Count + "}", D.ADMIN_LOG_TYPE.Quest);
               foreach (QuestDataNode xmlQuestNode in xmlQuestNodeList) {
                  // Remove node if friendship level requirement is insufficient
                  if (xmlQuestNode.friendshipLevelRequirement > friendshipLevel) {
                     D.adminLog("Removing quest from list to send to player due to insufficient level:: " +
                        "NodeID:{" + xmlQuestNode.questDataNodeId + "} " +
                        "NodeTitle:{" + xmlQuestNode.questNodeTitle + "} " +
                        "Current:{" + friendshipLevel + "} " +
                        "Required:{" + xmlQuestNode.friendshipLevelRequirement + "}", D.ADMIN_LOG_TYPE.Quest);
                     removeNodeList.Add(xmlQuestNode);
                     insufficientLevelNodeList.Add(xmlQuestNode);
                  }

                  QuestStatusInfo databaseQuestStatus = databaseQuestStatusList.Find(_ => _.questNodeId == xmlQuestNode.questDataNodeId);
                  if (databaseQuestStatus == null) {
                     // Remove the quest that requires a certain level requirement
                     if (xmlQuestNode.questNodeLevelRequirement > highestQuestNodeValue.questNodeId) {
                        removeNodeList.Add(xmlQuestNode);
                        D.adminLog("Removed quest node {" + xmlQuestNode.questDataNodeId + "} due to status being null", D.ADMIN_LOG_TYPE.Quest);
                     }
                  } else {
                     // If the quest requires a different quest to be unlocked first, remove it from the list to be provided to the player
                     bool hasQuestNodeRequirement = xmlQuestNode.questNodeLevelRequirement > -1;
                     if (hasQuestNodeRequirement) {
                        QuestStatusInfo requiredNodeStatus = databaseQuestStatusList.Find(_ => _.questNodeId == xmlQuestNode.questNodeLevelRequirement);
                        QuestDataNode requiredQuestNode = xmlQuestNodeList.Find(_ => _.questDataNodeId == xmlQuestNode.questNodeLevelRequirement);
                        if (requiredQuestNode != null) {
                           D.adminLog("Found the right quest node: " + requiredQuestNode.questNodeTitle + " " + requiredQuestNode.questDataNodeId + " " + requiredQuestNode.questDialogueNodes.Length, D.ADMIN_LOG_TYPE.Quest);
                           if (requiredNodeStatus.questDialogueId < requiredQuestNode.questDialogueNodes.Length) {
                              removeNodeList.Add(xmlQuestNode);
                              D.adminLog("Removed quest node {" + xmlQuestNode.questDataNodeId + "} due to dialogue requirement not met " +
                                 "{" + requiredNodeStatus.questDialogueId + "/ " + xmlQuestNode.questDialogueNodes.Length + " (" + xmlQuestNode.questNodeTitle + ")}", D.ADMIN_LOG_TYPE.Quest);
                           }
                        }
                     }

                     // Remove the quest that has a dialogue id greater than the dialogue length, meaning the quest is completed
                     if (databaseQuestStatus.questDialogueId >= xmlQuestNode.questDialogueNodes.Length) {
                        removeNodeList.Add(xmlQuestNode);
                        D.adminLog("Removed quest node {" + xmlQuestNode.questDataNodeId + "} due to quest already completed " +
                           "{" + databaseQuestStatus.questDialogueId + "/ " + xmlQuestNode.questDialogueNodes.Length + "}", D.ADMIN_LOG_TYPE.Quest);
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
         bool hasFriendshipLevel = DB_Main.hasFriendshipLevel(npcId, _player.userId);
         int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Initialize the relationship if it is the first time the player talks to this NPC
         if (!hasFriendshipLevel) {
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
                        // When the user selects a quest, we show again the dialogues up to the previously completed reward or quest node
                        for (dialogueId--; dialogueId >= 0; dialogueId--) {
                           if (questDataNode.questDialogueNodes[dialogueId].hasItemRequirements()
                              || questDataNode.questDialogueNodes[dialogueId].hasItemRewards()
                              || questDataNode.questDialogueNodes[dialogueId].friendshipRewardPts > 0) {
                              // This node has already been rewarded/completed, so we continue from the next
                              dialogueId++;
                              break;
                           }
                        }
                        dialogueId = dialogueId < 0 ? 0 : dialogueId;

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

   [Server]
   public void modifyItemDurability (NetEntity playerBattler, int weaponId, int weaponDurabilityDeduction, int armorId, int armorDurabilityDeduction) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int updatedArmorDurability = 100;
         int updatedWeaponDurability = 100;
         bool isArmorUpdated = false;
         bool isWeaponUpdated = false;

         if (weaponId > 0) {
            int newWeaponDurability = DB_Main.getItemDurability(playerBattler.userId, weaponId);
            if (newWeaponDurability > 0) {
               isWeaponUpdated = true;
               updatedWeaponDurability = newWeaponDurability - weaponDurabilityDeduction;
               DB_Main.updateItemDurability(playerBattler.userId, weaponId, updatedWeaponDurability);
            }
         }

         if (armorId > 0) {
            int newArmorDurability = DB_Main.getItemDurability(playerBattler.userId, armorId);
            if (newArmorDurability > 0) {
               isArmorUpdated = true;
               updatedArmorDurability = newArmorDurability - armorDurabilityDeduction;
               DB_Main.updateItemDurability(playerBattler.userId, armorId, updatedArmorDurability);
            }
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (weaponId > 0 && isWeaponUpdated) {
               ((PlayerBodyEntity) _player).weaponManager.updateDurability(updatedWeaponDurability);
               if (updatedWeaponDurability <= 0) {
                  Target_ReceiveWeaponDurabilityNotification(playerBattler.connectionToClient, updatedWeaponDurability);
               }
            }

            if (armorId > 0 && isArmorUpdated) {
               ((PlayerBodyEntity) _player).armorManager.updateDurability(updatedArmorDurability);
               if (updatedArmorDurability <= 0) {
                  Target_ReceiveArmorDurabilityNotification(playerBattler.connectionToClient, updatedArmorDurability);
               }
            }
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveWeaponDurabilityNotification (NetworkConnection connection, int durability) {
      ChatManager.self.addChat("Your weapon has worn down and requires repair!", ChatInfo.Type.Warning);
   }

   [TargetRpc]
   public void Target_ReceiveArmorDurabilityNotification (NetworkConnection connection, int durability) {
      ChatManager.self.addChat("Your armor has worn down and requires repair!", ChatInfo.Type.Warning);
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
         bool deliveredItems = false;

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
            D.adminLog("1. Updating quest status into: " +
               questId + ":: " + newQuestNodeId + ": " + newDialogueId + "/" + questDataNode.questDialogueNodes.Length, D.ADMIN_LOG_TYPE.Quest);
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
               deliveredItems = true;
            }

            // If the dialogue node has required items, get the current items of the user
            if (itemRequirementList.Count > 0) {
               List<Item> currentItems = DB_Main.getRequiredItems(itemRequirementList, _player.userId);
               itemStock = getItemStock(itemRequirementList, currentItems);
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Target_ReceiveNPCQuestNode(_player.connectionToClient, questId, newQuestNodeId, newDialogueId, friendshipLevel, true, true, itemStock.ToArray(), newJobXP);
               if (deliveredItems) {
                  AchievementManager.registerUserAchievement(_player, ActionType.QuestDelivery);
               }
            });
         });
      } else {
         newDialogueId++;
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Update the quest status of the npc
            D.adminLog("2. Updating quest status into: " +
               questId + ":: " + questNodeId + ": " + newDialogueId + "/" + questDataNode.questDialogueNodes.Length, D.ADMIN_LOG_TYPE.Quest);
            DB_Main.updateQuestStatus(npcId, _player.userId, questId, newQuestNodeId, newDialogueId);

            int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);
            QuestDialogueNode questDialogue = questDataNode.questDialogueNodes[dialogueId];
            DB_Main.updateNPCRelationship(npcId, _player.userId, friendshipLevel + questDialogue.friendshipRewardPts);

            // Get the quest progress status of the user for this npc
            List<QuestStatusInfo> totalQuestStatus = DB_Main.getQuestStatuses(npcId, _player.userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               Target_ReceiveProcessRewardToggle(_player.connectionToClient);
               bool hasCompletedAllQuests = true;

               List<QuestStatusInfo> incompleteQuestList = new List<QuestStatusInfo>();
               List<QuestStatusInfo> insufficientQuestList = new List<QuestStatusInfo>();
               int totalQuestNodes = questData.questDataNodes.Length;
               if (totalQuestStatus.Count < totalQuestNodes) {
                  hasCompletedAllQuests = false;
               } else {
                  // If any of the quest is in progress (dialogue current index less than total dialogues in a Quest Node)
                  foreach (QuestStatusInfo questStat in totalQuestStatus) {
                     QuestDataNode questNodeReference = questData.questDataNodes.ToList().Find(_ => _.questDataNodeId == questStat.questNodeId);
                     if (questStat.questDialogueId < questNodeReference.questDialogueNodes.Length) {
                        D.adminLog("-->This quest is not complete yet", D.ADMIN_LOG_TYPE.Quest);
                        if (friendshipLevel >= questNodeReference.friendshipLevelRequirement) {
                           incompleteQuestList.Add(questStat);
                        } else {
                           insufficientQuestList.Add(questStat);
                           D.adminLog("Will not mark as incomplete, level requirement is not enough! " +
                              "Current: {" + friendshipLevel + "} " +
                              "Required: {" + questNodeReference.friendshipLevelRequirement + "}", D.ADMIN_LOG_TYPE.Quest);
                        }
                     }
                  }

                  if (incompleteQuestList.Count > 0) {
                     hasCompletedAllQuests = false;
                  }
               }

               if (hasCompletedAllQuests) {
                  D.adminLog("Player has completed all quests, no more remaining:{" + incompleteQuestList.Count + "}", D.ADMIN_LOG_TYPE.Quest);
                  Target_RemoveQuestNotice(_player.connectionToClient, npcId);
               } else {
                  D.adminLog("Player has NOT completed all quests, remaining:{" + incompleteQuestList.Count + "}", D.ADMIN_LOG_TYPE.Quest);
               }

               if (incompleteQuestList.Count < 1) {
                  if (insufficientQuestList.Count > 1) {
                     D.adminLog("Player has not completed all quests but does not meet the friendship level, remaining:{" + insufficientQuestList.Count + "}", D.ADMIN_LOG_TYPE.Quest);
                     Target_ToggleInsufficientQuestNotice(_player.connectionToClient, npcId, true);
                  } else {
                     D.adminLog("Player has no pending quest that exceeds friendship level, remaining:{" + insufficientQuestList.Count + "}", D.ADMIN_LOG_TYPE.Quest);
                     Target_ToggleInsufficientQuestNotice(_player.connectionToClient, npcId, false);
                  }
               }

               if (questDialogue.itemRewards != null) {
                  if (questDialogue.itemRewards.Length > 0) {
                     int validItemCount = 0;
                     foreach (Item item in questDialogue.itemRewards) {
                        if (Item.isValidItem(item)) {
                           validItemCount++;
                        }
                     }
                     if (validItemCount > 0) {
                        giveItemRewardsToPlayer(_player.userId, new List<Item>(questDialogue.itemRewards), true);
                     }
                  }
               }
               if (questDialogue.abilityIdReward > 0) {
                  giveAbilityToPlayer(_player.userId, new int[1] { questDialogue.abilityIdReward });
               }

               // Register quest completion with achievement manager
               if (newDialogueId != dialogueId && newDialogueId == (questDataNode.questDialogueNodes.Length)) {
                  AchievementManager.registerUserAchievement(_player, ActionType.QuestComplete);
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

            NPCFriendship.checkAchievementTrigger(_player, currentFriendship, newFriendshipLevel);
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
         UserInfo friendUserInfo = DB_Main.getUserInfoById(friendUserId);

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
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.FriendshipInvitationSent, _player, feedbackMessage);

               // Let the potential friend know that he has got invitation
               NetEntity friend = EntityManager.self.getEntity(friendUserId);

               if (friend) {
                  friend.rpc.addFriendshipInviteNotification(_player.userId);
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
      List<int> lockedNpcIdList = new List<int>();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (NPCData npcData in npcDataInArea) {
            List<QuestStatusInfo> databaseQuestStatus = DB_Main.getQuestStatuses(npcData.npcId, _player.userId);
            QuestData questData = NPCQuestManager.self.getQuestData(npcData.questId);
            int friendshipLevel = DB_Main.getFriendshipLevel(npcData.npcId, _player.userId);
            if (questData != null) {
               // If there is no registered quest in the database, and if there are quest nodes for this NPC, add to quest notice
               if (databaseQuestStatus.Count < 1 && questData.questDataNodes.Length > 0) {
                  if (!npcIdList.Contains(npcData.npcId)) {
                     // Enable Logs if there are quest process related issues
                     //D.adminLog("Add because status is less than nodes", D.ADMIN_LOG_TYPE.Quest);
                     npcIdList.Add(npcData.npcId);
                  } else {
                     //D.adminLog("Skip because status is less than nodes", D.ADMIN_LOG_TYPE.Quest);
                  }
               } else {
                  foreach (QuestStatusInfo questStatus in databaseQuestStatus) {
                     // If progressed dialogue is less than the end point dialogue
                     if (questStatus.questDialogueId < questData.questDataNodes[questStatus.questNodeId].questDialogueNodes.Length) {
                        // If friendship level meets, add to npc list
                        if (friendshipLevel >= questData.questDataNodes[questStatus.questNodeId].friendshipLevelRequirement) {
                           if (!npcIdList.Contains(npcData.npcId)) {
                              // Enable Logs if there are quest process related issues
                              //D.debug("Adding Npc {" + npcData.npcId + "} here because quest is not finished");
                              npcIdList.Add(npcData.npcId);
                           }
                        } else {
                           if (!lockedNpcIdList.Contains(npcData.npcId)) {
                              //D.debug("Adding InvalidNpc {"+ npcData.npcId + "} here because friendship level is not enough");
                              lockedNpcIdList.Add(npcData.npcId);
                           }
                           if (npcIdList.Contains(npcData.npcId)) {
                              //D.debug("Removing {" + npcData.npcId + "} from list because friendship level is not enough!");
                              npcIdList.Remove(npcData.npcId);
                           }
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
               // This displays all the npc's that have available quest for the player
               Target_ReceiveQuestNotifications(_player.connectionToClient, npcIdList.ToArray());

               // This displays all the npc's that have available quest for the player but needs to have conditions unlocked such as friendship level, etc...
               Target_ReceiveInsufficientQuestNotifications(_player.connectionToClient, lockedNpcIdList.ToArray());
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

   [TargetRpc]
   public void Target_ReceiveInsufficientQuestNotifications (NetworkConnection connection, int[] npcIds) {
      foreach (int npcId in npcIds) {
         NPC npcEntity = NPCManager.self.getNPC(npcId);
         npcEntity.insufficientQuestNotice.SetActive(true);
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
         UserInfo friendInfo = DB_Main.getUserInfoById(friendUserId);

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
   public void Cmd_RequestFriendshipVisitFromServer (int pageNumber, int itemsPerPage) {
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
         int totalFriendInfoCount = DB_Main.getFriendshipInfoCount(_player.userId, Friendship.Status.Friends);

         UserInfo userInfo = DB_Main.getUserInfoById(_player.userId);

         // Calculate the maximum page number
         int maxPage = Mathf.CeilToInt((float) totalFriendInfoCount / itemsPerPage);
         if (maxPage == 0) {
            maxPage = 1;
         }

         // Clamp the requested page number to the max page - the number of items could have changed
         pageNumber = Mathf.Clamp(pageNumber, 1, maxPage);

         // Get the items from the database
         List<FriendshipInfo> friendshipInfoList = DB_Main.getFriendshipInfoList(_player.userId, Friendship.Status.Friends, pageNumber, itemsPerPage);

         // Get the number of friends
         int friendCount = totalFriendInfoCount;

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Determine if the friends are online
            foreach (FriendshipInfo friend in friendshipInfoList) {
               friend.isOnline = ServerNetworkingManager.self.isUserOnline(friend.friendUserId);
            }

            Target_ReceiveFriendVisitInfo(_player.connectionToClient, friendshipInfoList.ToArray(), userInfo.customHouseBaseId > 0 && userInfo.customFarmBaseId > 0, pageNumber, totalFriendInfoCount);
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

   [Server]
   public void addFriendshipInviteNotification (int senderId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         if (senderId <= 0) {
            D.error($"Couldn't add the friendship invite notification to the chat of player {_player.entityName} ({_player.userId}). Invalid senderId: {senderId}");
            return;
         }

         string sender = DB_Main.getUserName(senderId);

         if (Util.isEmpty(sender)) {
            D.error($"Couldn't add the friendship invite notification to the chat of player {_player.entityName} ({_player.userId}). Invalid sender name: {sender}.");
            return;
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            string message = $"{sender} sent you a friend request!";
            _player.Target_ReceiveNormalChat(message, ChatInfo.Type.System);
         });
      });
   }

   [Server]
   public void receivePlayerAddedToServer () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      Target_ReceivePlayerAddedToServer(_player.connectionToClient);
   }

   [TargetRpc]
   private void Target_ReceivePlayerAddedToServer (NetworkConnection connection) {
      if (Global.isFirstSpawn) {
         Cmd_NotifyOnlineStatusToFriends(isOnline: true, null);
         D.debug($"First spawn!");
      }

      Global.isFirstSpawn = false;
   }

   [Command]
   public void Cmd_CreateMail (string recipientName, string mailSubject, string message, int[] attachedItemsIds, int[] attachedItemsCount, int price, bool autoDelete) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      bool sendBack = false;

      // If the mail has attachments and was sent by a player, the mail must be sent back and deleted
      if (!_player.isAdmin() && attachedItemsIds.Any()) {
         sendBack = true;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (int itemId in attachedItemsIds) {
            // Get the item
            Item item = DB_Main.getItem(_player.userId, itemId);

            if (item == null) {
               continue;
            }

            if (Bkg_ShouldBeSoulBound(item, isBeingEquipped: false)) {
               if (!Bkg_IsItemSoulBound(item)) {
                  DB_Main.updateItemSoulBinding(item.id, isBound: true);
               }

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  ServerMessageManager.sendError(ErrorMessage.Type.ItemIsSoulBound, _player, "One or more items are soul bound and can't be attached to the mail!");
               });

               return;
            }
         }

         // Try to retrieve the recipient info
         UserInfo recipientUserInfo = DB_Main.getUserInfo(recipientName);
         if (recipientUserInfo == null) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendError(ErrorMessage.Type.MailInvalidUserName, _player, "The player '" + recipientName + "' does not exist!");
            });
            return;
         }

         // Check if the recipient and the sender are the same person
         if (recipientUserInfo.userId == _player.userId && !MailManager.ALLOW_SELF_MAILING) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendError(ErrorMessage.Type.MailInvalidUserName, _player, "You can't send a mail to yourself!");
            });
            return;
         }

         int gold = DB_Main.getGold(_player.userId);

         // Make sure they have enough gold and the price is positive
         if (gold < price || price < 0) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendError(ErrorMessage.Type.NotEnoughGold, _player, "You don't have " + price + " gold!");
            });
            return;
         }

         // Create the mail
         int mailId = createMailCommon(recipientUserInfo.userId, mailSubject, message, attachedItemsIds, attachedItemsCount, autoDelete, sendBack);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (mailId >= 0) {
               // Let the player know that the mail was sent
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.MailSent, _player, "The mail has been successfully sent to " + recipientName + "!");

               // Make sure that we charge the player only if the message went through
               DB_Main.addGold(_player.userId, -price);

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
   private int createMailCommon (int recipientUserId, string mailSubject, string message, int[] attachedItemsIds, int[] attachedItemsCount, bool autoDelete, bool sendBack) {
      // Check soul binding
      foreach (int itemId in attachedItemsIds) {
         // Get the item
         Item item = DB_Main.getItem(_player.userId, itemId);

         if (item == null) {
            continue;
         }

         if (Bkg_IsItemSoulBound(item)) {
            return -1;
         }
      }

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
         UserInfo userInfo = DB_Main.getUserInfoById(_player.userId);

         if (userInfo.armorId == item.id || userInfo.weaponId == item.id) {
            sendError("You cannot select an equipped item!");
            return -1;
         }
      }

      MailInfo mail = null;

      // Create the mail
      mail = new MailInfo(-1, recipientUserId, _player.userId, DateTime.UtcNow, false, mailSubject, message, autoDelete, sendBack);
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
   public void Cmd_ProcessOldMails (int maxMailLifetimeDays) {

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the number of items
         int sentMailsCount = DB_Main.getSentMailInfoCount(_player.userId);

         // TODO: arbitrarily chosen number
         int itemsPerPage = 100;

         // Calculate the maximum page number
         int maxPage = Mathf.CeilToInt((float) sentMailsCount / itemsPerPage);
         if (maxPage == 0) {
            maxPage = 1;
         }

         List<MailInfo> droppedMails = new List<MailInfo>();
         List<MailInfo> sentMails = null;

         for (int pageNumber = 1; pageNumber < maxPage + 1; pageNumber++) {
            sentMails = DB_Main.getSentMailInfoList(_player.userId, pageNumber, itemsPerPage);

            if (sentMails == null) {
               continue;
            }

            foreach (MailInfo sentMail in sentMails) {
               bool isMailExpired = DateTime.FromBinary(sentMail.receptionDate).ToLocalTime() + TimeSpan.FromDays(maxMailLifetimeDays) < DateTime.Now.ToLocalTime();
               if (isMailExpired) {
                  if (sentMail.sendBack) {

                     // Get the attached items
                     List<Item> attachedItems = DB_Main.getItems(-sentMail.mailId, new Item.Category[] { Item.Category.None }, 1, MailManager.MAX_ATTACHED_ITEMS);

                     // Fetch the full data for the mail info
                     MailInfo fullSentMail = DB_Main.getMailInfo(sentMail.mailId);

                     // Create the send back mail
                     string mailSubject = fullSentMail.mailSubject + " [RE]";
                     string dormantUserName = DB_Main.getUserName(fullSentMail.recipientUserId);
                     string message = "The player '" + dormantUserName + "' didn't read your mail. The mail was sent back to you along with the included attachments. Original Message: <" + fullSentMail.message + ">";
                     MailInfo sendBackMail = new MailInfo(-1, _player.userId, fullSentMail.recipientUserId, DateTime.UtcNow, false, mailSubject, message, false, false);
                     sendBackMail.mailId = DB_Main.createMail(sendBackMail);

                     if (sendBackMail.mailId == -1) {
                        D.error(string.Format("Error when creating a mail from sender {0} to recipient {1}.", fullSentMail.recipientUserId, _player.userId));
                        continue;
                     }

                     // Send the items back to the original user through the mail.
                     foreach (var attachedItem in attachedItems) {

                        // Check if the item was correctly retrieved
                        if (attachedItem == null) {
                           D.warning(string.Format("Could not retrieve the item {0} attached to mail {1} of user {2} targeted to {3}.", attachedItem.id, fullSentMail.mailId, fullSentMail.recipientUserId, _player.userId));
                           continue;
                        }

                        // Transfer the item from the dormant player's mail to the send back mail
                        DB_Main.transferItem(attachedItem, -fullSentMail.mailId, -sendBackMail.mailId, attachedItem.count);
                     }

                     // Delete the email.
                     droppedMails.Add(fullSentMail);
                  } else {
                     // Check if the email should be deleted
                     if (sentMail.autoDelete) {
                        droppedMails.Add(sentMail);
                     }
                  }
               }
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (MailInfo droppedMail in droppedMails) {
               Cmd_DeleteMail(droppedMail.mailId);
            }
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

   [Command]
   public void Cmd_RequestSentMailListFromServer (int pageNumber, int itemsPerPage) {

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
         int totalMailCount = DB_Main.getSentMailInfoCount(_player.userId);

         // Calculate the maximum page number
         int maxPage = Mathf.CeilToInt((float) totalMailCount / itemsPerPage);
         if (maxPage == 0) {
            maxPage = 1;
         }

         // Clamp the requested page number to the max page - the number of items could have changed
         pageNumber = Mathf.Clamp(pageNumber, 1, maxPage);

         // Get the items from the database
         List<MailInfo> mailList = DB_Main.getSentMailInfoList(_player.userId, pageNumber, itemsPerPage);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveSentMailList(_player.connectionToClient, mailList.ToArray(), pageNumber, totalMailCount);
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
   public void Cmd_RequestPvpShopData (int shopId, int itemCategoryType) {
      PvpShopItem.PvpShopItemType itemType = (PvpShopItem.PvpShopItemType) itemCategoryType;
      PvpShopData shopData = PvpShopManager.self.getShopData(shopId);
      if (shopData == null) {
         D.adminLog("Invalid Shop Data! " + shopId, D.ADMIN_LOG_TYPE.PvpShop);
         return;
      }
      D.adminLog("User {" + _player.userId + ":" + _player.entityName + "} is requesting shop interaction from shop {" + shopData.shopName + "} Category {" + itemType + "}", D.ADMIN_LOG_TYPE.PvpShop);

      // Register the user to the stats manager if spawned in tutorial bay or the tutorial cemetery
      if ((_player.areaKey.ToLower().Contains(Area.STARTING_TOWN_SEA.ToLower()) || _player.areaKey.ToLower().Contains(Area.TUTORIAL_AREA.ToLower()))
         && !GameStatsManager.self.isUserRegistered(_player.userId)) {
         GameStatsManager.self.registerUser(_player.userId);
      }

      if (GameStatsManager.self.isUserRegistered(_player.userId)) {
         int totalCategoryTypes = Enum.GetValues(typeof(PvpShopItem.PvpShopItemType)).Length;
         List<PvpShopItem> shopItemList = new List<PvpShopItem>();
         int currentShopContentCount = shopData.shopItems.FindAll(_ => _.shopItemType == itemType).Count;

         // If the current category type has no existing shop items, iterate through the next options
         if (currentShopContentCount < 1 && itemCategoryType > 0 && itemCategoryType < totalCategoryTypes) {
            for (int i = itemCategoryType; i < totalCategoryTypes; i++) {
               if (shopData.shopItems.FindAll(_ => _.shopItemType == (PvpShopItem.PvpShopItemType) i).Count > 0) {
                  // Override the item category type to select if atleast one item exists
                  PvpShopItem.PvpShopItemType newItemType = (PvpShopItem.PvpShopItemType) i;
                  D.debug("Missing {" + itemType + "} Next available Category is {" + newItemType + "}");
                  itemType = newItemType;
                  break;
               }
            }
         }

         // Disabled shop items here if needed
         foreach (PvpShopItem shopItem in shopData.shopItems) {
            if (shopItem.shopItemType == itemType) {
               shopItemList.Add(PvpShopItem.generateItemCopy(shopItem));
            }
         }

         // Filter shop item categories that does not exist in this shop
         List<PvpShopItem.PvpShopItemType> shopItemTypeList = new List<PvpShopItem.PvpShopItemType>();
         foreach (PvpShopItem.PvpShopItemType typeId in Enum.GetValues(typeof(PvpShopItem.PvpShopItemType))) {
            if (shopData.shopItems.Exists(_ => _.shopItemType == typeId)) {
               shopItemTypeList.Add(typeId);
            }
         }

         // Disable land powerups that has already been purchased
         if (itemType == PvpShopItem.PvpShopItemType.LandPowerup) {
            foreach (PvpShopItem shopItem in shopItemList) {
               LandPowerupType landPowerup = (LandPowerupType) shopItem.itemId;
               bool isDisabled = LandPowerupManager.self.hasPowerup(_player.userId, landPowerup);
               shopItem.isDisabled = isDisabled;
            }
         }

         // Alter price of healing item based on ship type
         if (itemType == PvpShopItem.PvpShopItemType.Item && _player is PlayerShipEntity) {
            PlayerShipEntity playerShip = (PlayerShipEntity) _player;

            foreach (PvpShopItem shopItem in shopItemList) {
               if ((PvpConsumableItem) shopItem.itemId == PvpConsumableItem.RepairTool) {
                  // Weakest ship max is 10, strongest ship max is 52
                  // Increase item cost in percentage depending on sailor count
                  int roundDownVal = playerShip.sailors % 5;
                  int newItemCost = shopItem.itemCost + (int) (playerShip.sailors - roundDownVal);
                  shopItem.itemCost = newItemCost;
               }
            }
         }

         D.adminLog("Note: User {" + _player.userId + ":" + _player.entityName + "} " +
            "Successfully fetched shop info of shop {" + shopData.shopName + "} with total {" + shopItemList.Count + "} items", D.ADMIN_LOG_TYPE.PvpShop);
         int userSilver = GameStatsManager.self.getSilverAmount(_player.userId);
         Target_ProcessShopData(_player.connectionToClient, shopData.shopId, userSilver, shopData.shopName, shopData.shopDescription, Util.serialize(shopItemList), shopItemTypeList.ToArray());
      } else {
         D.debug("Warning, user {" + _player.userId + "} does not exist in the game stat manager");
      }
   }

   [Command]
   public void Cmd_InteractWindow (int windowId) {
      Area area = AreaManager.self.getArea(_player.areaKey);
      if (area != null) {
         WindowInteractable window = area.interactableWindows.Find(_ => _.id == windowId);
         if (window != null) {
            bool openState = window.isOpen;
            Rpc_InteractWindow(openState, _player.areaKey, windowId);
            window.isOpen = !openState;
         }
      }
   }

   [ClientRpc]
   protected void Rpc_InteractWindow (bool isOpen, string areaKey, int windowId) {
      Area area = AreaManager.self.getArea(areaKey);
      if (area != null) {
         WindowInteractable window = area.interactableWindows.Find(_ => _.id == windowId);
         if (window != null) {
            if (isOpen) {
               window.closeWindow();
            } else {
               window.openWindow();
            }
         }
      }
   }

   [TargetRpc]
   public void Target_ProcessShopData (NetworkConnection conn, int shopId, int userSilver, string shopName, string shopInfo, string[] serializedShopItems, PvpShopItem.PvpShopItemType[] shopItemTypes) {
      List<PvpShopItem> pvpShopList = Util.unserialize<PvpShopItem>(serializedShopItems);

      PvpShopPanel panel = PvpShopPanel.self;
      panel.showEntirePanel();
      panel.displayName.text = shopName;
      panel.description.text = shopInfo;
      panel.userSilver = userSilver;
      panel.shopId = shopId;
      panel.userSilverText.text = userSilver.ToString();
      panel.populateShop(pvpShopList, shopItemTypes.ToList());
   }

   [Command]
   public void Cmd_BuyPvpItem (int shopItemId, int shopId, int category) {
      PvpShopData shopData = PvpShopManager.self.getShopData(shopId);
      if (shopData == null) {
         D.debug("Shop data is null for id: " + shopId);
         return;
      }

      PvpShopItem shopItem = shopData.shopItems.Find(_ => _.itemId == shopItemId && _.shopItemType == (PvpShopItem.PvpShopItemType) category);
      if (shopItem == null) {
         D.debug("Shop item is null for id: " + shopItemId);
         return;
      }

      if (GameStatsManager.self.isUserRegistered(_player.userId)) {
         GameStatsManager.self.addSilverAmount(_player.userId, -shopItem.itemCost);
         int userSilver = GameStatsManager.self.getSilverAmount(_player.userId);
         _player.Target_ReceiveSilverCurrency(_player.connectionToClient, -shopItem.itemCost, SilverManager.SilverRewardReason.None);
         Target_ReceivePvpShopResult(_player.connectionToClient, userSilver, Util.serialize(new List<PvpShopItem> { shopItem }));

         switch (shopItem.shopItemType) {
            case PvpShopItem.PvpShopItemType.Powerup:
               if (_player is PlayerShipEntity) {
                  SeaEntity seaEntity = (SeaEntity) _player;
                  seaEntity.rpc.Target_ReceivePowerup((Powerup.Type) shopItem.itemId, shopItem.rarityType, seaEntity.transform.position);
                  PowerupManager.self.addPowerupServer(seaEntity.userId, new Powerup {
                     powerupRarity = shopItem.rarityType,
                     powerupType = (Powerup.Type) shopItem.itemId,
                     expiry = Powerup.Expiry.None
                  });
               }
               break;
            case PvpShopItem.PvpShopItemType.Ship:
               int shipSqlId = shopItem.itemId;
               ShipData shipData = ShipDataManager.self.getShipData(shipSqlId);
               if (shipData != null) {
                  if (_player is PlayerShipEntity) {
                     SeaEntity seaEntity = (SeaEntity) _player;
                     PlayerShipEntity playerShip = (PlayerShipEntity) seaEntity;
                     ShipInfo purchasedShip = Ship.generateNewShip(shipSqlId, Rarity.Type.Common);

                     // Update health to max value
                     playerShip.currentHealth = shipData.baseHealthMax;
                     playerShip.maxHealth = shipData.baseHealthMax;

                     // Update abilities
                     purchasedShip.shipAbilities = ShipDataManager.self.getShipAbilities(shipSqlId);

                     // Sprite updates
                     playerShip.changeShipInfo(purchasedShip);
                     playerShip.Rpc_RefreshSprites((int) shipData.shipType, (int) shipData.shipSize, (int) purchasedShip.skinType);
                  }
               } else {
                  D.debug("Cant process shop purchase: {" + shipSqlId + "} does not exist");
               }
               break;
            case PvpShopItem.PvpShopItemType.LandPowerup:
               LandPowerupData newPowerup = new LandPowerupData();
               switch ((LandPowerupType) shopItem.itemId) {
                  case LandPowerupType.DamageBoost:
                     newPowerup = new LandPowerupData {
                        counter = 1200, // Set as 1200 secs for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.DamageBoost,
                        userId = _player.userId,
                        value = 20, // Set as 20% damage boost for now
                     };
                     break;
                  case LandPowerupType.DefenseBoost:
                     newPowerup = new LandPowerupData {
                        counter = 1200, // Set as 1200 secs for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.DefenseBoost,
                        userId = _player.userId,
                        value = 20, // Set as 20% defense boost for now
                     };
                     break;
                  case LandPowerupType.SpeedBoost:
                     newPowerup = new LandPowerupData {
                        counter = 1200, // Set as 1200 secs for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.SpeedBoost,
                        userId = _player.userId,
                        value = 100, // Set as 100% speed boost for now
                     };
                     break;
                  case LandPowerupType.ExperienceBoost:
                     newPowerup = new LandPowerupData {
                        counter = 1200, // Set as 1200 secs for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.ExperienceBoost,
                        userId = _player.userId,
                        value = 20, // Set as 20% exp boost for now
                     };
                     break;
                  case LandPowerupType.LootDropBoost:
                     newPowerup = new LandPowerupData {
                        counter = 1200, // Set as 1200 secs for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.LootDropBoost,
                        userId = _player.userId,
                        value = 100, // Set as 100% loot drop rate boost for now
                     };
                     break;

                  case LandPowerupType.RangeDamageBoost:
                     newPowerup = new LandPowerupData {
                        counter = 360, // Set as 6 mins for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.RangeDamageBoost,
                        userId = _player.userId,
                        value = 20, // Set as 20% ranged damage boost for now
                     };
                     break;
                  case LandPowerupType.MeleeDamageBoost:
                     newPowerup = new LandPowerupData {
                        counter = 360, // Set as 6 mins for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.MeleeDamageBoost,
                        userId = _player.userId,
                        value = 20, // Set as 20% melee damage boost for now
                     };
                     break;

                  case LandPowerupType.ClimbSpeedBoost:
                     newPowerup = new LandPowerupData {
                        counter = 360, // Set as 6 mins for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.ClimbSpeedBoost,
                        userId = _player.userId,
                        value = 100, // Set as x2 climb speed now
                     };
                     break;
                  case LandPowerupType.MiningBoost:
                     newPowerup = new LandPowerupData {
                        counter = 360, // Set as 6 mins for now
                        expiryType = LandPowerupExpiryType.Time, // Set as timer for now
                        landPowerupType = LandPowerupType.MiningBoost,
                        userId = _player.userId,
                        value = 100, // Set as + 100% loot drops for mining
                     };
                     break;
               }

               if (_player.areaKey.ToLower().Contains(Area.TUTORIAL_AREA.ToLower())) {
                  newPowerup.expiryType = LandPowerupExpiryType.OnWarp;
               }

               LandPowerupManager.self.updateNewPowerupData(newPowerup.userId, newPowerup.landPowerupType, newPowerup.expiryType, newPowerup.counter, newPowerup.value);
               break;
            case PvpShopItem.PvpShopItemType.Item:
               if (_player is PlayerShipEntity) {
                  PlayerShipEntity playersShip = (PlayerShipEntity) _player;

                  // Repair 40% of the ships life bar
                  int repairValue = (int) (playersShip.maxHealth * PvpShopItem.REPAIR_VALUE);
                  playersShip.currentHealth = Mathf.Clamp(playersShip.currentHealth + repairValue, 0, playersShip.maxHealth);
                  Target_RepairShip(playersShip.connectionToClient, repairValue);
               }
               break;
         }
      } else {
         D.debug("Warning, user {" + _player.userId + "} is not a ship");
      }
   }

   [TargetRpc]
   public void Target_RepairShip (NetworkConnection conn, int repairValue) {
      // Show the damage text
      ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(Attack.Type.Heal), transform.position, Quaternion.identity);
      damageText.setDamage(repairValue);
   }

   [TargetRpc]
   public void Target_ReceivePvpShopResult (NetworkConnection conn, int remainingSilver, string[] serializedShopItems) {
      List<PvpShopItem> pvpShopList = Util.unserialize<PvpShopItem>(serializedShopItems);
      if (pvpShopList.Count < 1) {
         return;
      }

      PvpShopPanel panel = PvpShopPanel.self;
      panel.userSilver = remainingSilver;
      panel.userSilverText.text = remainingSilver.ToString();
      panel.updatedShopTemplates(remainingSilver);
      panel.receivePurchaseResult(pvpShopList[0]);
   }

   [Command]
   public void Cmd_BuyItem (int shopItemId, int shopId) {
      Item newItem = null;
      Item shopItem = ShopManager.self.getItem(shopItemId);

      if (shopItem == null) {
         D.warning("Couldn't find item for item id: " + shopItemId);
         return;
      }

      int price = shopItem.getSellPrice();
      float perkMultiplier = 1.0f - PerkManager.self.getPerkMultiplierAdditive(_player.userId, Perk.Category.ShopPriceReduction);
      price = (int) (price * perkMultiplier);

      // Make sure the player has enough money
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         // Make sure they have enough gold
         if (gold >= price) {
            DB_Main.addGold(_player.userId, -price);

            // Create a copy of the shop item
            Item shopItemCopy = shopItem.Clone();

            // Process the appropriate data for blueprint items
            if (shopItemCopy.category == Item.Category.Blueprint) {
               CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(shopItemCopy.itemTypeId);
               switch (craftingData.resultItem.category) {
                  case Item.Category.Weapon:
                     shopItemCopy.data = Blueprint.WEAPON_DATA_PREFIX;
                     shopItemCopy.itemTypeId = craftingData.resultItem.itemTypeId;
                     break;
                  case Item.Category.Armor:
                     shopItemCopy.data = Blueprint.ARMOR_DATA_PREFIX;
                     shopItemCopy.itemTypeId = craftingData.resultItem.itemTypeId;
                     break;
                  case Item.Category.Hats:
                     shopItemCopy.data = Blueprint.HAT_DATA_PREFIX;
                     shopItemCopy.itemTypeId = craftingData.resultItem.itemTypeId;
                     break;
               }
            }

            // Create a new instance of the item
            // New Method, needs observation
            //newItem = DB_Main.createItemOrUpdateItemCount(_player.userId, shopItemCopy);
            processItemCreation(_player.userId, shopItemCopy);
            newItem = shopItemCopy;

            if (newItem.category == Item.Category.None) {
               D.debug("Error Here! Category Cant be none for Shop Item");
            }

            // Update soul binding
            if (Bkg_ShouldBeSoulBound(newItem, isBeingEquipped: false)) {
               if (!Bkg_IsItemSoulBound(newItem)) {
                  if (DB_Main.updateItemSoulBinding(newItem.id, isBound: true)) {
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ItemSoulBound, _player, $"Item '{newItem.itemName}' is soul bound!");
                     });
                  }
               }
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
            } else if (shopItem.category == Item.Category.Blueprint) {
               CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(shopItem.itemTypeId);
               if (craftingData == null) {
                  itemName = AdventureShopScreen.UNKNOWN_ITEM;
               } else {
                  itemName = EquipmentXMLManager.self.getItemName(craftingData.resultItem);
               }
            } else {
               itemName = shopItem.getName();
            }

            // Let the client know that it was successful
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.StoreItemBought, _player, $"'{itemName}' purchased!");

            // Make sure their gold display gets updated
            getItemsForArea(shopId);

            // If the new item is stackable, update the item shortcuts in case the count has been updated
            if (newItem.canBeStacked()) {
               sendItemShortcutList();
            }
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
      float perkMultiplier = 1.0f - PerkManager.self.getPerkMultiplierAdditive(_player.userId, Perk.Category.ShopPriceReduction);
      price = (int) (price * perkMultiplier);

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
   public void Cmd_AddGuildAlly (int invitedUserId, int guildId, int allyId) {
      // Prevent spamming invitations
      if (VoyageGroupManager.self.isGuildAllianceInvitationSpam(guildId, allyId)) {
         string message = "You must wait " + VoyageGroupManager.GROUP_INVITE_MIN_INTERVAL.ToString() + " seconds before inviting this guild again!";
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, message);
         _player.Target_ReceiveNormalChat(message, ChatInfo.Type.System);
         return;
      }

      VoyageGroupManager.self.logGuildAllianceInvitation(guildId, allyId);
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         GuildInfo guildInfo = DB_Main.getGuildInfo(guildId);
         GuildInfo guildAllyInfo = DB_Main.getGuildInfo(allyId);
         List<int> guildAllies = DB_Main.getGuildAlliance(guildId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (guildAllies.Contains(allyId)) {
               string message = "Already allied with guild " + guildInfo.guildName + "!";
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, message);
               _player.Target_ReceiveNormalChat(message, ChatInfo.Type.System);
               return;
            }

            NetEntity invitedUserEntity = EntityManager.self.getEntity(invitedUserId);
            if (invitedUserEntity == null || guildInfo == null || guildAllyInfo == null) {
               _player.Target_ReceiveNormalChat("Failed to initialize guild alliance", ChatInfo.Type.System);
               return;
            }

            invitedUserEntity.rpc.Target_ReceiveGuildAllianceInvite(invitedUserEntity.connectionToClient, _player.userId, guildInfo, guildAllyInfo);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveGuildAllianceInvite (NetworkConnection connection, int inviterUserId, GuildInfo guildInfo, GuildInfo guildAllyInfo) {
      if (Global.ignoreGuildAllianceInvites) {
         Cmd_RejectGuildAlliance(inviterUserId, _player.userId);
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => GuildManager.self.acceptGuildAllianceInviteOnClient(inviterUserId, guildInfo.guildId, guildAllyInfo.guildId));

      // Show a confirmation panel with the user name
      string message = "The guild alliance proposal has been initiated between " + guildInfo.guildName+ " and " + guildAllyInfo.guildName + "!";
      PanelManager.self.confirmScreen.show(message);
   }

   [Command]
   public void Cmd_RejectGuildAlliance (int invitingUserId, int invitedUserId) {
      NetEntity invitingUserEntity = EntityManager.self.getEntity(invitingUserId);
      string message = "User is auto ignoring guild alliance invites!";
      if (invitingUserEntity) {
         D.debug("This user is rejecting the invite {" + _player.userId + ":" + _player.entityName + "} " +
            "of user {" + invitingUserEntity.userId + ":" + invitingUserEntity.entityName + "}");

         ServerMessageManager.sendError(ErrorMessage.Type.Misc, invitingUserEntity, message);
         invitingUserEntity.Target_ReceiveNormalChat(message, ChatInfo.Type.System);
      } else {
         D.debug("MISSING entity for rejecting guild alliance: ");
      }
   }

   [Command]
   public void Cmd_AcceptGuildAlliance (int invitingUserId, int guildId, int allyId) {
      // Initialize data
      List<int> inviterGuildAllyIds = new List<int>();
      List<int> invitedGuildAllyIds = new List<int>();
      List<GuildInfo> guildAlliesInfo = new List<GuildInfo>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Establish the guild alliance in the database
         DB_Main.addGuildAlliance(guildId, allyId);
         DB_Main.addGuildAlliance(allyId, guildId);

         // Fetch the guild information of the ally guild and the users guild
         GuildInfo guildInfo = DB_Main.getGuildInfo(guildId);
         GuildInfo guildAllyInfo = DB_Main.getGuildInfo(allyId);

         // Fetch all the guild id's of the allies of this user
         NetEntity invitingUserEntity = EntityManager.self.getEntity(invitingUserId);
         if (invitingUserEntity != null) {
            inviterGuildAllyIds = DB_Main.getGuildAlliance(invitingUserEntity.guildId);
         }

         // Gather all the guild ally information and send to player
         invitedGuildAllyIds = DB_Main.getGuildAlliance(_player.guildId);
         foreach (int guildAllyId in inviterGuildAllyIds) {
            GuildInfo newGuildAllyInfo = DB_Main.getGuildInfo(guildAllyId);
            if (newGuildAllyInfo != null) {
               guildAlliesInfo.Add(newGuildAllyInfo);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send guild information to the player
            Target_ReceiveGuildAllies(_player.connectionToClient, Util.serialize(guildAlliesInfo));
            Target_ReceiveGuildAllyAddResult(_player.connectionToClient, guildAllyInfo);

            // Notify the invite success for both inviter and invitee
            string message = "Guild alliance has been accepted between " +
               "Guild {" + guildInfo.guildName + "} and Guild {" + guildAllyInfo.guildName + "}";
            _player.Target_ReceiveNormalChat(message, ChatInfo.Type.System);
            invitingUserEntity.Target_ReceiveNormalChat(message, ChatInfo.Type.System);

            refreshGuildAllianceList(guildInfo, inviterGuildAllyIds, 0);
            refreshGuildAllianceList(guildAllyInfo, invitedGuildAllyIds, 0);
         });
      });
   }

   [Server]
   public void refreshGuildAllianceList (GuildInfo guildInfo, List<int> guildAllyIds, int removeAllyGuildId) {
      foreach (UserInfo userInfo in guildInfo.guildMembers) {
         NetEntity guildUser = EntityManager.self.getEntity(userInfo.userId);
         if (guildUser != null) {
            // Add Guild Ally
            if (guildAllyIds.Count > 0) {
               foreach (int currGuildId in guildAllyIds) {
                  if (!guildUser.guildAllies.Contains(currGuildId)) {
                     guildUser.guildAllies.Add(currGuildId);
                  }
               }
            }

            // Remove guild ally
            if (removeAllyGuildId > 0 && guildUser.guildAllies.Contains(removeAllyGuildId)) {
               guildUser.guildAllies.Remove(removeAllyGuildId);
            }
         }
      }
   }

   [Command]
   public void Cmd_RemoveGuildAlly (int guildId, int allyId) {
      List<GuildInfo> guildAlliesInfo = new List<GuildInfo>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Updated database contents, remove alliances
         DB_Main.removeGuildAlliance(guildId, allyId);
         DB_Main.removeGuildAlliance(allyId, guildId);

         // Get guild information of users guild and the ally guild to be removed
         GuildInfo guildInfo = DB_Main.getGuildInfo(guildId);
         GuildInfo guildAllyInfo = DB_Main.getGuildInfo(allyId);

         // Fetch all the information of the guild allies of the user
         List<int> userGuildAlliesId = DB_Main.getGuildAlliance(guildId);
         List<int> allyGuildAlliesId = DB_Main.getGuildAlliance(allyId);
         foreach (int currGuildId in userGuildAlliesId) {
            GuildInfo newGuildAllyInfo = DB_Main.getGuildInfo(currGuildId);
            if (newGuildAllyInfo != null) {
               guildAlliesInfo.Add(newGuildAllyInfo);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Refresh all guild allies
            refreshGuildAllianceList(guildInfo, userGuildAlliesId, allyId);
            refreshGuildAllianceList(guildAllyInfo, allyGuildAlliesId, guildId);

            Target_ReceiveGuildAllyDeleteResult(_player.connectionToClient, guildAllyInfo);
            Target_ReceiveGuildAllies(_player.connectionToClient, Util.serialize(guildAlliesInfo));
         });
      });
   }

   [Command]
   public void Cmd_GetGuildAllies (int guildId) {
      List<GuildInfo> guildAlliesInfo = new List<GuildInfo>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<int> guildAlliesId= DB_Main.getGuildAlliance(guildId);
         foreach (int guildAllyId in guildAlliesId) {
            GuildInfo guildAllyInfo = DB_Main.getGuildInfo(guildAllyId);
            if (guildAllyInfo != null) {
               guildAlliesInfo.Add(guildAllyInfo);
            }
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (_player != null) {
               Target_ReceiveGuildAllies(_player.connectionToClient, Util.serialize(guildAlliesInfo));
            }
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveGuildAllies (NetworkConnection connection, string[] guildAlliesInfo) {
      if (GuildPanel.self != null) {
         if (guildAlliesInfo.Length > 0) {
            List<GuildInfo> unserializedGuildInfo = Util.unserialize<GuildInfo>(guildAlliesInfo);
            GuildPanel.self.receiveGuildAlliesFromServer(unserializedGuildInfo);
         } else {
            GuildPanel.self.receiveGuildAlliesFromServer(new List<GuildInfo>());
         }
      } else {
         D.debug("Guild panel does not exist!");
      }
   }

   [TargetRpc]
   public void Target_ReceiveGuildAllyDeleteResult (NetworkConnection connection, GuildInfo allyInfo) {
      D.debug("-- Deleted ally {" + allyInfo.guildName + "}");
   }

   [TargetRpc]
   public void Target_ReceiveGuildAllyAddResult (NetworkConnection connection, GuildInfo allyInfo) {
      D.debug("-- Added ally {" + allyInfo.guildName + "}");
   }

   [Command]
   public void Cmd_GetGuildNameInGuildIconTooltip (int guildId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         GuildInfo guildInfo = DB_Main.getGuildInfo(guildId);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_GetGuildNameInGuildIconTooltip(_player.connectionToClient, guildInfo);
         });
      });
   }

   [TargetRpc]
   public void Target_GetGuildNameInGuildIconTooltip (NetworkConnection connection, GuildInfo guildInfo) {
      GuildIcon.tooltippedIconRef.setGuildName(guildInfo.guildName);
   }

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
         UserInfo userInfo = DB_Main.getUserInfoById(_player.userId);

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
               _player.Rpc_UpdateGuildIconSprites(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);
               refreshPvpStateForUser(userInfo, _player);
            });

         } else {
            // Let the player know
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "That guild name has been taken.");
            });
         }
      });
   }

   public void refreshPvpStateForUser (UserInfo userInfo, NetEntity player) {
      bool isInTown = AreaManager.self.isTownArea(_player.areaKey);
      bool isPvpEnabled = userInfo.pvpState == 1 ? true : false;
      PvpGameMode pvpGameMode = AreaManager.self.getAreaPvpGameMode(_player.areaKey);
      if (isInTown) {
         pvpGameMode = PvpGameMode.GuildWars;
      }

      // TODO: Finalize pvp game modes if group wars and free for all will be completely removed or not
      if (pvpGameMode == PvpGameMode.GroupWars || pvpGameMode == PvpGameMode.FreeForAll) {
         pvpGameMode = PvpGameMode.GuildWars;
      }

      if (player.guildId < 1) {
         pvpGameMode = PvpGameMode.None;
      }

      _player.Target_ReceiveOpenWorldStatus(pvpGameMode, isPvpEnabled, isInTown);
      _player.enablePvp = isPvpEnabled;
   }

   [Command]
   public void Cmd_LeaveGuild () {
      int guildId = _player.guildId;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserInfo userInfo = DB_Main.getUserInfoById(_player.userId);
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
            _player.guildMapBaseId = 0;
            _player.Rpc_UpdateGuildIconSprites(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);
            refreshPvpStateForUser(userInfo, _player);

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
               _player.guildMapBaseId = 0;
               _player.Rpc_UpdateGuildIconSprites(_player.guildIconBackground, _player.guildIconBackPalettes, _player.guildIconBorder, _player.guildIconSigil, _player.guildIconSigilPalettes);

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
   public void Cmd_RefineItem (int itemIdToRefine) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      RefinementData refinementData = CraftingManager.self.getRefinementData(0);
      List<CraftingIngredients.Type> ingredientList = new List<CraftingIngredients.Type>();
      foreach (Item refinementIngredient in refinementData.combinationRequirements) {
         if (refinementIngredient.category == Item.Category.CraftingIngredients) {
            ingredientList.Add((CraftingIngredients.Type) refinementIngredient.itemTypeId);
         } else {
            D.debug("Mismatch type: " + refinementIngredient.category + " : " + refinementIngredient.itemTypeId);
         }
      }

      bool hasMetRequirements = true;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Item itemToRefine = DB_Main.getItem(_player.userId, itemIdToRefine);
         List<Item> userInventoryIngredients = DB_Main.getCraftingIngredients(_player.userId, ingredientList);
         foreach (Item refinementRequirement in refinementData.combinationRequirements) {
            Item userItemData = userInventoryIngredients.Find(_ =>
               _.category == refinementRequirement.category
               && _.itemTypeId == refinementRequirement.itemTypeId
               && _.count >= refinementRequirement.count);

            int userItemCount = userItemData == null ? 0 : userItemData.count;
            if (userItemData != null) {
               D.adminLog("Refinement Ingredient match!"
                  + " Name: {" + EquipmentXMLManager.self.getItemName(refinementRequirement)
                  + "} Current:{" + userItemCount + "}"
                  + "} Required:{" + refinementRequirement.count + "}", D.ADMIN_LOG_TYPE.Refine);
            } else {
               hasMetRequirements = false;
               D.adminLog("Insufficient Refinement Ingredient!"
                  + " Name: {" + EquipmentXMLManager.self.getItemName(refinementRequirement)
                  + "} Current:{" + userItemCount + "}"
                  + "} Required:{" + refinementRequirement.count + "}", D.ADMIN_LOG_TYPE.Refine);
            }
         }

         if (hasMetRequirements) {
            // Deduct the player inventory items which is now consumed by the refinement process
            foreach (Item item in userInventoryIngredients) {
               Item requiredItemRef = refinementData.combinationRequirements.ToList().Find(_ => _.category == item.category && _.itemTypeId == item.itemTypeId);
               DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, requiredItemRef.count);
            }

            // Add item durability
            int newDurability = itemToRefine.durability + 1;
            DB_Main.updateItemDurability(_player.userId, itemToRefine.id, newDurability);
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_RefreshRefinementPanel(_player.connectionToClient, itemToRefine.id);
         });
      });
   }

   [TargetRpc]
   public void Target_RefreshRefinementPanel (NetworkConnection connection, int itemId) {
      // Get the crafting panel
      CraftingPanel panel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

      // Refresh the panel
      panel.receiveRefinementData.RemoveAllListeners();
      panel.receiveRefinementData.AddListener(() => {
         if (panel.refineableItemsHolder.childCount > 0) {
            ItemCell[] itemCells = panel.refineableItemsHolder.GetComponentsInChildren<ItemCell>();
            if (itemCells.Length > 0) {
               ItemCell itemSelected = itemCells.ToList().Find(_ => _.itemCache.id == itemId);
               if (itemSelected != null && itemSelected.itemCache != null) {
                  panel.onRefineableItemClicked(itemSelected.itemCache, itemSelected);
               }
            }
         }
      });
      panel.refreshRefinementList();

      //SoundEffectManager.self.playFmod2dSfxWithId(SoundEffectManager.REFINE_COMPLETE);
   }

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
         // New Method, needs observation
         //Item craftedItem = DB_Main.createItemOrUpdateItemCount(_player.userId, resultItem);
         Item craftedItem = resultItem;
         processItemCreation(_player.userId, resultItem);

         if (craftedItem.category == Item.Category.None) {
            D.debug("Error Here! Category Cant be none for Crafting Rewards");
         }

         // If the item could not be created, stop the process
         if (craftedItem == null) {
            D.warning(string.Format("Could not create the crafted item in the user inventory. Blueprint id: {0}, item category: {1}, item type id: {2}.",
               blueprint.itemTypeId, resultItem.category, resultItem.itemTypeId));
            return;
         }

         // Update soul binding
         if (Bkg_ShouldBeSoulBound(craftedItem, isBeingEquipped: false)) {
            if (!Bkg_IsItemSoulBound(craftedItem)) {
               if (DB_Main.updateItemSoulBinding(craftedItem.id, isBound: true)) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ItemSoulBound, _player, $"Item '{craftedItem.itemName}' is soul bound!");
                  });
               }
            }
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
            AchievementManager.registerUserAchievement(_player, ActionType.Craft);

            // Let them know they gained experience
            _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Crafter, 0, true);
            if (craftedItem.category == Item.Category.Armor) {
               AchievementManager.registerUserAchievement(_player, ActionType.CraftArmor);
            }

            if (craftedItem.category == Item.Category.Weapon) {
               AchievementManager.registerUserAchievement(_player, ActionType.CraftWeapon);
            }

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
            Target_ReceiveCraftedItem(_player.connectionToClient, craftedItem);
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
   public void Cmd_RequestAdminVoyageListFromServer () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.isAdmin()) {
         return;
      }

      // Get both the main voyage instances and the linked treasure site instances
      List<Voyage> voyages = VoyageManager.self.getAllVoyages();
      voyages.AddRange(VoyageManager.self.getAllTreasureSiteInstancesLinkedToVoyages());

      // Send the list to the client
      Target_ReceiveAdminVoyageInstanceList(_player.connectionToClient, voyages.ToArray());
   }

   [Command]
   public void Cmd_RequestUserListForAdminVoyageInfoPanelFromServer (int voyageId, int instanceId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.isAdmin()) {
         return;
      }

      // Ask the list of users in the instance to the server network
      ServerNetworkingManager.self.getUsersInInstanceForAdminVoyagePanel(voyageId, instanceId, _player.userId);
   }

   [Server]
   public void returnUsersInInstanceForAdminVoyagePanel (int[] userIdArray) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.isAdmin()) {
         return;
      }

      // Read the users info in DB and send it to the client
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<UserInfo> userInfoList = DB_Main.getUserInfoList(userIdArray);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveUserInfoListForAdminVoyagePanel(_player.connectionToClient, userInfoList.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_WarpAdminToVoyageInstanceAsGhost (int voyageId, string areaKey) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.isAdmin()) {
         return;
      }

      // Check the validity of the request
      if (!VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage)) {
         sendError("This voyage is not available anymore!");
         return;
      }

      // Always place the admin in a new group
      if (VoyageGroupManager.self.tryGetGroupById(_player.voyageGroupId, out VoyageGroupInfo voyageGroup)) {
         VoyageGroupManager.self.removeUserFromGroup(voyageGroup, _player);
      }
      VoyageGroupManager.self.createGroup(_player.userId, voyageId, true, true);

      // Warp the admin to the voyage map
      _player.spawnInNewMap(voyageId, areaKey, Direction.South);
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

         VoyageGroupManager.self.createGroup(_player.userId, voyageId, false);
      } else {
         // Add the user to the group
         VoyageGroupManager.self.addUserToGroup(voyageGroup, _player.userId, _player.entityName);
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
      VoyageGroupManager.self.createGroup(_player.userId, -1, true);
   }

   [Command]
   public void Cmd_WarpToCurrentVoyageMap () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         sendError("An error occurred when searching for the voyage group.");
         return;
      }

      // Retrieve the voyage data
      if (!VoyageManager.self.tryGetVoyage(voyageGroup.voyageId, out Voyage voyage, true)) {
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

      // Get a spawn close to the player and define it as the exit spawn
      Spawn exitSpawn = SpawnManager.self.getFirstSpawnAround(_player.areaKey, _player.transform.localPosition, Voyage.LEAGUE_EXIT_SPAWN_SEARCH_RADIUS);
      if (exitSpawn == null) {
         D.error($"Could not find an exit spawn next to the voyage entrance in area {_player.areaKey}");
      }

      if (!_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         // Create a new league with the same biome than the player's current instance
         if (exitSpawn == null) {
            VoyageManager.self.createLeagueInstanceAndWarpPlayer(_player, 0, instance.biome, -1, "", _player.areaKey);
         } else {
            VoyageManager.self.createLeagueInstanceAndWarpPlayer(_player, 0, instance.biome, -1, "", exitSpawn.AreaKey, exitSpawn.spawnKey, exitSpawn.arriveFacing);
         }
         return;
      }

      // Check if the user has already joined a voyage
      if (_player.tryGetVoyage(out Voyage voyage)) {
         Target_OnWarpFailed("Displaying current voyage panel instead of warping immediately");

         // Display a panel with the current voyage map details
         Target_ReceiveCurrentVoyageInstance(_player.connectionToClient, voyage);
      } else {
         // When entering a league with a group, all the group members must be nearby
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
               if (exitSpawn == null) {
                  VoyageManager.self.createLeagueInstanceAndWarpPlayer(_player, 0, instance.biome, -1, "", _player.areaKey);
               } else {
                  VoyageManager.self.createLeagueInstanceAndWarpPlayer(_player, 0, instance.biome, -1, "", exitSpawn.AreaKey, exitSpawn.spawnKey, exitSpawn.arriveFacing);
               }
            });
         });
      }
   }

   [Command]
   public void Cmd_ReturnToTownFromLeague () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      _player.spawnInBiomeHomeTown();
   }

   [Command]
   public void Cmd_RequestExitCompletedLeague () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.isInGroup()) {
         sendError("Could not find the voyage group");
         return;
      }

      Instance instance = _player.getInstance();

      // The instance must be a voyage
      if (instance.voyageId <= 0) {
         D.error($"User {_player.userId} is attempting to exit a non-voyage instance");
         return;
      }

      // There cannot be enemies in the player's current instance
      if (instance == null) {
         D.error($"Error when getting the current instance for user {_player.userId} in area {_player.areaKey}");
         return;
      }

      if (instance.aliveNPCEnemiesCount > 0) {
         sendError("You must defeat all the enemies before exiting the voyage");
         return;
      }

      if (string.IsNullOrEmpty(instance.leagueExitAreaKey)) {
         _player.spawnInBiomeHomeTown();
      } else if (string.IsNullOrEmpty(instance.leagueExitSpawnKey)) {
         D.error($"The voyage exit spawn was not defined for area {instance.leagueExitAreaKey}. Check that there is a spawn next to the voyage entrance in that map.");
         _player.spawnInNewMap(instance.leagueExitAreaKey);
      } else {
         _player.spawnInNewMap(instance.leagueExitAreaKey, instance.leagueExitSpawnKey, instance.leagueExitFacingDirection);
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
   public void Cmd_RequestPvpStatPanel () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      Instance userInstance = InstanceManager.self.getInstance(_player.instanceId);
      if (userInstance == null) {
         D.warning("Missing instance: " + _player.instanceId);
         return;
      }

      if (userInstance.isPvP) {
         List<GameStats> instanceStatData = GameStatsManager.self.getStatsForInstance(_player.instanceId);
         Target_OpenPvpStatPanel(_player.connectionToClient, instanceStatData.ToArray());
      }
   }

   [TargetRpc]
   private void Target_OpenPvpStatPanel (NetworkConnection conn, GameStats[] stats) {
      // Get the panel
      PvpStatPanel panel = (PvpStatPanel) PanelManager.self.get(Panel.Type.PvpScoreBoard);

      PanelManager.self.linkIfNotShowing(Panel.Type.PvpScoreBoard);

      SoundEffectManager.self.playGuiMenuOpenSfx();

      List<GameStats> pvpStatList = stats.ToList();
      GameStatsData pvpStatData = new GameStatsData {
         stats = pvpStatList,
         isInitialized = true
      };

      Instance instance = _player.getInstance();
      panel.setTitle("STATS");

      if (instance.isPvP) {
         panel.setTitle("SCOREBOARD");
      }

      panel.populatePvpPanelData(pvpStatData);
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
         return;
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
            return;
         }
      }

      // Check if the group is linked to a league that has already started
      if (destinationGroup.voyageId > 0 && VoyageManager.self.tryGetVoyage(destinationGroup.voyageId, out Voyage destinationVoyage) && destinationVoyage.isLeague) {
         int aliveNPCEnemyCount = destinationVoyage.aliveNPCEnemyCount;
         int playerCount = destinationVoyage.playerCount;
         bool hasTreasureSite = false;

         // Find the treasure site (if any) and add the npc enemies and players it contains
         if (VoyageManager.self.tryGetVoyage(destinationGroup.voyageId, out Voyage treasureSite, true)) {
            if (VoyageManager.isTreasureSiteArea(treasureSite.areaKey)) {
               hasTreasureSite = true;
            }
            aliveNPCEnemyCount += treasureSite.aliveNPCEnemyCount;
            playerCount += treasureSite.playerCount;
         }

         if (aliveNPCEnemyCount == 0 && !hasTreasureSite) {
            // When the current instance has been cleared of enemies, more members can join
            // Note that if there is an active treasure site, it will always be recreated to prevent inviting players only to open the chest
            VoyageGroupManager.self.addUserToGroup(destinationGroup, _player.userId, _player.entityName);
         } else if (playerCount <= 0) {
            // When the current instance has enemies left, but no players, we can recreate it and allow more members to join
            VoyageManager.self.recreateLeagueInstanceAndAddUserToGroup(destinationGroup.groupId, _player.userId, _player.entityName);
         } else {
            sendError("Cannot join the group while group members are close to danger!");
            return;
         }
      } else {
         // Add the user to the group
         VoyageGroupManager.self.addUserToGroup(destinationGroup, _player.userId, _player.entityName);
      }
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
         if (VoyageManager.isAnyLeagueArea(targetPlayerToKick.areaKey) || VoyageManager.isPvpArenaArea(targetPlayerToKick.areaKey) || VoyageManager.isTreasureSiteArea(targetPlayerToKick.areaKey)) {
            D.debug("This player {" + _player.userId + " " + _player.entityName + "} Has been kicked from Group, returning to Town");
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

      // If the player is in a voyage area or is in ghost mode, warp him to the closest town
      if (VoyageManager.isAnyLeagueArea(playerToRemove.areaKey) || VoyageManager.isPvpArenaArea(playerToRemove.areaKey) || VoyageManager.isTreasureSiteArea(playerToRemove.areaKey) || playerToRemove.isGhost) {
         if (VoyageManager.isPvpArenaArea(playerToRemove.areaKey)) {
            playerToRemove.spawnInBiomeHomeTown(Biome.Type.Forest);
         } else {
            playerToRemove.spawnInBiomeHomeTown();
         }
      }
   }

   [TargetRpc]
   private void Target_CleanUpVoyagePanelOnKick (NetworkConnection conn) {
      VoyageGroupPanel.self.cleanUpPanelOnKick();
   }

   [Server]
   public void sendVoyageGroupMembersInfo () {
      if (!VoyageGroupManager.self.tryGetGroupById(_player.voyageGroupId, out VoyageGroupInfo voyageGroup)) {
         return;
      }

      // Copy the group members to avoid concurrent modifications errors in the background thread
      List<int> groupMemberList = new List<int>(voyageGroup.members);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Read in DB the group members info needed to display their portrait
         List<VoyageGroupMemberCellInfo> groupMembersInfo = VoyageGroupManager.self.getGroupMembersInfo(groupMemberList);

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

   [Server]
   public void setNextBiomeUnlocked () {
      // Get the next biome
      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      Biome.Type nextBiome = Biome.getNextBiome(instance.biome);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Check if the user has unlocked the biome
         bool isNextBiomeUnlocked = DB_Main.isBiomeUnlockedForUser(_player.userId, nextBiome);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Set the value in the player entity
            _player.isNextBiomeUnlocked = isNextBiomeUnlocked;
         });
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
      if (!VoyageManager.isPvpArenaArea(_player.areaKey) && !VoyageManager.isOpenWorld(_player.areaKey)) {
         // If the player is not in a group, clear it from the netentity and redirect to the starting town
         if (!_player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
            _player.voyageGroupId = -1;

            // If player cant bypass restirctions, return them to town due to insufficient conditions being met
            if (!EntityManager.self.canUserBypassWarpRestrictions(_player.userId)) {
               D.adminLog("Returning player to town: Voyage Group does not Exist!", D.ADMIN_LOG_TYPE.Warp);
               return false;
            }
         }

         // If the voyage is not defined or doesn't exists, or the group is not linked to this instance, redirect to the starting town
         if (voyageGroup.voyageId <= 0 || !_player.tryGetVoyage(out Voyage voyage) || _player.getInstance().voyageId != voyageGroup.voyageId) {
            if (voyageGroup.voyageId <= 0) {
               D.adminLog("Returning player to town: Voyage id is Invalid!", D.ADMIN_LOG_TYPE.Warp);
            }
            if (_player.getInstance().voyageId != voyageGroup.voyageId) {
               D.adminLog("Returning player to town: Player voyage Id is incompatible with voyage group: {" + _player.getInstance().voyageId + "} : {" + voyageGroup.voyageId + "}", D.ADMIN_LOG_TYPE.Warp);
            }

            // If player cant bypass restirctions, return them to town due to insufficient conditions being met
            if (!EntityManager.self.canUserBypassWarpRestrictions(_player.userId)) {
               return false;
            }
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

      // If the area is a PvP area, a Voyage or a TreasureSite, add the player to the GameStats System
      if (_player.tryGetVoyage(out Voyage v) || VoyageManager.isAnyLeagueArea(_player.areaKey) || VoyageManager.isPvpArenaArea(_player.areaKey) || VoyageManager.isTreasureSiteArea(_player.areaKey)
         || _player.areaKey.Contains(Area.TUTORIAL_AREA) || _player.areaKey.Contains(Area.STARTING_TOWN_SEA)) {
         GameStatsManager.self.registerUser(_player.userId);
      } else {
         PvpAnnouncementHolder.self.clearAnnouncements();

         if (!_player.areaKey.Contains(Area.TUTORIAL_AREA)) {
            GameStatsManager.self.unregisterUser(_player.userId);
         }
      }

      // Remove the player from the game stats manager, when navigating open areas
      if (VoyageManager.isOpenWorld(_player.areaKey)) {
         GameStatsManager.self.unregisterUser(_player.userId);
      }

      Target_ResetPvpSilverPanel(_player.connectionToClient, GameStatsManager.self.getSilverAmount(_player.userId));
      Target_ResetPvpScoreIndicator(_player.connectionToClient, show: false);

      // Verify the voyage consistency only if the user is in a voyage group or voyage area
      if (!VoyageManager.isAnyLeagueArea(_player.areaKey) && !VoyageManager.isPvpArenaArea(_player.areaKey) && !VoyageManager.isTreasureSiteArea(_player.areaKey)) {
         return;
      }

      // If the player does not meet the voyage requirements, warp the player back to town
      if (!canPlayerStayInVoyage()) {
         D.debug("This player {" + _player.userId + " " + _player.entityName + "} Cannot stay in Voyage, returning to Town");
         _player.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.South);
         return;
      }

      if (playerShipEntity && playerShipEntity.tryGetVoyage(out Voyage voyage)) {
         if (voyage.isPvP) {
            playerShipEntity.hasEnteredPvP = true;
            PvpManager.self.assignPvpTeam(playerShipEntity, voyage.instanceId);
            PvpManager.self.assignPvpFaction(playerShipEntity, voyage.instanceId);
            PvpManager.self.onPlayerLoadedGameArea(_player.userId);

            if (AreaManager.self != null && AreaManager.self.getAreaPvpGameMode(_player.areaKey) == PvpGameMode.CaptureTheFlag) {
               Target_ResetPvpScoreIndicator(_player.connectionToClient, show: true);
            }
         } else {
            // Not a PvP Voyage
            Debug.LogWarning($"Player '{_player.entityName}' has entered a Voyage or League.");
         }
      }
   }

   [Command]
   public void Cmd_InteractWithEntity (int entityId, bool simulatePhysics) {
      if (_player == null) {
         return;
      }

      InteractableObjEntity interactableObject = InteractableObjManager.self.getObject(entityId);
      if (interactableObject == null) {
         return;
      }

      if (interactableObject.simulatePhysics) {
         return;
      }

      float archHeight = Random.Range(interactableObject.archHeightMin, interactableObject.archHeightMax) * InteractableBall.TILE_SIZE;
      float lifeTime = Random.Range(interactableObject.lifeTimeMin, interactableObject.lifeTimeMax);
      float totalRotation = Random.Range(1, 3) * 360.0f;
      double startTime = NetworkTime.time;

      Vector2 dir = (interactableObject.transform.position - transform.position).normalized;
      if (interactableObject is InteractableBox) {
         InteractableBox box = (InteractableBox) interactableObject;
         box.interactObject(dir);
      }
      if (interactableObject is InteractableBall) {
         InteractableBall ball = (InteractableBall) interactableObject;
         float distance = Random.Range(ball.distanceMin, ball.distanceMax) * InteractableBall.TILE_SIZE;

         if (ball.usesNetworkRigidBody) {
            ball.Rpc_BroadcastSimulationParameters(startTime, archHeight, lifeTime);
            ball.interactObject(dir, startTime, archHeight, lifeTime);
         } else {
            ball.init(ball.transform.position, dir, startTime, archHeight, lifeTime, distance, totalRotation);
            ball.Rpc_BroadInit(ball.transform.position, dir, startTime, archHeight, lifeTime, distance, totalRotation);
         }
      }
   }

   [Command]
   public void Cmd_AdminSpawnBox () {
      InteractableObjEntity spawnedBox = Instantiate(InteractableObjManager.self.interactableBox);
      Instance currInstance = InstanceManager.self.getInstance(_player.instanceId);
      InstanceManager.self.addInteractableToInstance(spawnedBox, currInstance);
      spawnedBox.transform.position = _player.transform.position;

      InteractableObjManager.self.registerObject(spawnedBox);
      NetworkServer.Spawn(spawnedBox.gameObject);
      _player.Target_ReceiveNormalChat("Spawned physics box", ChatInfo.Type.System);
   }

   [Command]
   public void Cmd_AdminSpawnBall () {
      InteractableObjEntity spawnedBall = Instantiate(InteractableObjManager.self.interactableBall);
      Instance currInstance = InstanceManager.self.getInstance(_player.instanceId);
      InstanceManager.self.addInteractableToInstance(spawnedBall, currInstance);
      spawnedBall.transform.position = _player.transform.position;

      InteractableObjManager.self.registerObject(spawnedBall);
      NetworkServer.Spawn(spawnedBall.gameObject);
      _player.Target_ReceiveNormalChat("Spawned simulated ball", ChatInfo.Type.System);
   }

   [Command]
   public void Cmd_AdminSpawnBall2 () {
      InteractableObjEntity spawnedBall = Instantiate(InteractableObjManager.self.interactableBallNetwork);
      Instance currInstance = InstanceManager.self.getInstance(_player.instanceId);
      InstanceManager.self.addInteractableToInstance(spawnedBall, currInstance);
      spawnedBall.transform.position = _player.transform.position;

      InteractableObjManager.self.registerObject(spawnedBall);
      NetworkServer.Spawn(spawnedBall.gameObject);
      _player.Target_ReceiveNormalChat("Spawned physics ball", ChatInfo.Type.System);
   }

   [Command]
   public void Cmd_RequestPvpToggle (bool isOn) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.changeUserPvpState(_player.userId, isOn ? 1 : 0);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.enablePvp = isOn;
            bool isInTown = AreaManager.self.isTownArea(_player.areaKey);
            Target_ReceivePvpToggle(_player.connectionToClient, isOn, isInTown);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceivePvpToggle (NetworkConnection connection, bool isOn, bool isInTown) {
      VoyageStatusPanel.self.togglePvpStatusInfo(isOn, isInTown);
   }

   [Command]
   public void Cmd_RequestRefinementRequirement (int itemId) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the item from the user inventory
         Item itemToRefine = DB_Main.getItem(_player.userId, itemId);
         if (itemToRefine == null) {
            D.debug("Error here! Item {" + itemId + "} does not exist for user {" + _player.userId + "}");
            // TODO: Process error handling here to be sent to the client
         }

         // TODO: Do this dynamically by setting up xml id as part of the equipment data in the web tool
         int xmlId = 0;

         // Fetch the refinement requirements for the item type
         RefinementData refinementData = CraftingManager.self.getRefinementData(xmlId);
         if (refinementData == null) {
            // If refinement data failed to fetch, fetch the default one and error log
            D.debug("There is no refinement data with {" + xmlId + "} id");
            refinementData = CraftingManager.self.getRefinementData(0);
         }

         // Filter all the items needed for the refinement before sending database query
         List<CraftingIngredients.Type> ingredientType = new List<CraftingIngredients.Type>();
         foreach (Item item in refinementData.combinationRequirements) {
            if (item.category == Item.Category.CraftingIngredients) {
               ingredientType.Add((CraftingIngredients.Type) item.itemTypeId);
            }
         }

         // Fetch the crafting ingredients from the users inventory if any
         List<Item> ingredients = DB_Main.getCraftingIngredients(_player.userId, ingredientType);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveRefinementRequirements(_player.connectionToClient, xmlId, ingredients.ToArray(), itemToRefine);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveRefinementRequirements (NetworkConnection connection, int xmlId, Item[] itemList, Item itemToRefine) {
      CraftingPanel panel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

      // Refresh the panel
      panel.receiveRefineRequirementsForItem(xmlId, itemList, itemToRefine);
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

   [Server]
   public void processBadgeReward (uint attackerNetId) {
      QuestItem questItem = EquipmentXMLManager.self.getQuestItemById(13);
      Item itmReward = new Item {
         count = 1,
         itemTypeId = 13,
         category = Item.Category.Quest_Item,
         iconPath = questItem != null ? questItem.iconPath : "",
         itemName = questItem != null ? questItem.itemName : "",
         itemDescription = questItem != null ? questItem.itemDescription : "",
      };

      NetEntity entity = EntityManager.self.getEntityByNetId(attackerNetId);
      if (entity != null) {
         Target_ReceiveBadges(entity.connectionToClient, itmReward);
      }
   }

   [TargetRpc]
   public void Target_ReceiveBadges (NetworkConnection connection, Item itemReward) {
      StartCoroutine(RewardManager.self.CO_CreatingFloatingIcon(itemReward, _player.transform.position));
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
   public void Target_ReceiveCraftedItem (NetworkConnection connection, Item item) {
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.CRAFT_SUCCESS);

      // Craft bone sword tutorial trigger
      if (item.category == Item.Category.Weapon && item.itemTypeId == CraftingManager.BONE_SWORD_RECIPE) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.Craft_Bone_Sword);
      }

      RewardManager.self.showItemInRewardPanel(item);
   }

   #endregion

   [Command]
   public void Cmd_RequestItemData (int itemId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Item item = DB_Main.getItem(itemId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveItemData(item);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveItemData (Item item) {
      itemDataReceived?.Invoke(item);
   }

   [TargetRpc]
   public void Target_UpdateInventory (NetworkConnection connection) {
      ((InventoryPanel) PanelManager.self.get(Panel.Type.Inventory)).refreshPanel();
   }

   [TargetRpc]
   public void Target_CollectOre (NetworkConnection connection, int oreId, int effectId) {
      OreNode oreNode = OreManager.self.getOreNode(oreId);
      if (oreNode.orePickupCollection.Count > 0) {
         if (oreNode.orePickupCollection.ContainsKey(effectId)) {
            oreNode.tryToMineNodeOnClient();

            Destroy(oreNode.orePickupCollection[effectId].gameObject);
            oreNode.orePickupCollection.Remove(effectId);
         }
      }
   }

   [Command]
   public void Cmd_ResetOreReference (int oreId) {
      OreNode oreNode = OreManager.self.getOreNode(oreId);
      if (oreNode == null) {
         D.debug("Ore node is missing! No ore with id:{" + oreId + "} registered to OreManager");
         return;
      }

      oreNode.userIds.Remove(_player.userId);
   }

   [TargetRpc]
   public void Target_MineOre (NetworkConnection connection, int oreId, Vector2 startingPosition, Vector2 endPosition) {
      OreNode oreNode = OreManager.self.getOreNode(oreId);

      if (oreNode == null) {
         D.debug("Ore node is missing! No ore with id:{" + oreId + "} registered to OreManager");
         return;
      }
      oreNode.incrementInteractCount();
      oreNode.updateSprite(oreNode.interactCount);
      ExplosionManager.createMiningParticle(oreNode.transform.position);

      // Start refresh timer for local instance
      if (oreNode.finishedMining() && oreNode.mapSpecialType != Area.SpecialType.TreasureSite) {
         oreNode.startResetTimer();
         Cmd_ResetOreReference(oreId);
      }

      // SFX
      //SoundEffectManager.self.playFmodWithPath(SoundEffectManager.MINING_ROCKS, oreNode.transform);

      if (oreNode.interactCount > OreNode.MAX_INTERACT_COUNT) {
         D.adminLog("Exceeded max interact count!", D.ADMIN_LOG_TYPE.Mine);
         return;
      }

      if (oreNode.finishedMining()) {
         D.adminLog("Player has finished mining, spawning collectable ores:{" + oreNode.id + "}", D.ADMIN_LOG_TYPE.Mine);
         int randomCount = Random.Range(1, 3);

         // Chance to spawn an extra ore
         if (Random.Range(0.0f, 1.0f) < PerkManager.self.getPerkMultiplierAdditive(Perk.Category.MiningDrops)) {
            randomCount++;
         }

         // Add extra ore spawn if has mining powerup
         if (LandPowerupManager.self.hasPowerup(_player.userId, LandPowerupType.MiningBoost)) {
            D.debug("User has Mining powerup, added ore from {" + randomCount + "} to {" + (randomCount + 1) + "}");
            randomCount++;
         }
         for (int i = 0; i < randomCount; i++) {
            float randomSpeed = Random.Range(.8f, 1.2f);
            float angleOffset = Random.Range(-25, 25);
            processClientMineEffect(oreId, oreNode.transform.position, DirectionUtil.getDirectionFromPoint(startingPosition, endPosition), angleOffset, randomSpeed, i, _player.userId, _player.voyageGroupId);
         }
      } else {
         D.adminLog("Player successfully interacted ore {" + oreNode.id + "} Mining is not Finished: {" + oreNode.interactCount + "}", D.ADMIN_LOG_TYPE.Mine);
      }
   }

   [ClientRpc]
   public void Rpc_PlayMineSfx (int oreId) {
      OreNode oreNode = OreManager.self.getOreNode(oreId);
      if (oreNode == null) {
         D.debug("Error! Missing Ore Node:{" + oreId + "}. Cannot play sfx");
         return;
      }
      // SFX
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.MINING_ROCKS, oreNode.transform.position);
   }

   [Command]
   public void Cmd_PlayOreSfx (int oreId) {
      Rpc_PlayMineSfx(oreId);
   }

   [Command]
   public void Cmd_InteractOre (int oreId, Vector2 startingPosition, Vector2 endPosition) {
      D.adminLog("Player {" + _player.userId + "} tried to interact ore {" + oreId + "}", D.ADMIN_LOG_TYPE.Mine);
      Target_MineOre(_player.connectionToClient, oreId, startingPosition, endPosition);
   }

   public void processItemCreation (int userId, ItemQueue itemQueue) {
      itemCreationList.Add(itemQueue);
      checkItemQueue();
   }

   public void processItemCreation (int userId, Item baseItem) {
      processItemCreation(userId, new ItemQueue {
         item = baseItem,
         userId = userId,
         wasPurchasedOnTheGemStore = false,
         gemStoreItem = null
      });
   }

   public void processPurchasedItemCreation (int userId, Item baseItem, StoreItem storeItem) {
      processItemCreation(userId, new ItemQueue {
         item = baseItem,
         userId = userId,
         wasPurchasedOnTheGemStore = true,
         gemStoreItem = storeItem
      });
   }

   private void checkItemQueue () {
      if (itemCreationList.Count > 0) {
         if (!isProcessing) {
            isProcessing = true;
            ItemQueue itemCache = null;
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               foreach (ItemQueue itemInfo in itemCreationList) {
                  itemCache = itemInfo;
                  break;
               }
               if (itemCache != null) {
                  Item itemBeingProcessed = DB_Main.createItemOrUpdateItemCount(itemCache.userId, itemCache.item);
                  onItemQueueProcessed(itemCache, itemBeingProcessed);

                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     itemCreationList.Remove(itemCache);
                     StopCoroutine(nameof(CO_ResetItemQueue));
                     StartCoroutine(nameof(CO_ResetItemQueue));
                  });
               }
            });
         }
      }
   }

   [ServerOnly]
   private void onItemQueueProcessed (ItemQueue itemQueue, Item item) {
      if (itemQueue.wasPurchasedOnTheGemStore && itemQueue.gemStoreItem != null) {
         StoreItem storeItem = itemQueue.gemStoreItem;

         // If the purchase was not successful
         if (item == null) {
            D.error($"Store Purchase failed for user '{_player.userId}' trying to purchase the Store Item '{storeItem.id}'.");
            reportStorePurchaseFailed();
            return;
         }

         // Fetch the newly created item for display
         string purchasedItemName = storeItem.overrideItemName ? storeItem.displayName : itemQueue.item.itemName;
         int purchasedItemId = item.id;

         // Remove the gems
         DB_Main.addGems(_player.accountId, -storeItem.price);

         // Update soul binding
         bool soulBound = false;

         if (Bkg_ShouldBeSoulBound(item, isBeingEquipped: false)) {
            soulBound = Bkg_IsItemSoulBound(item);

            if (!soulBound) {
               soulBound = DB_Main.updateItemSoulBinding(item.id, isBound: true);
            }
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Let the player know that we gave them their item
            Target_OnStorePurchaseCompleted(_player.connectionToClient, storeItem.category, purchasedItemName, purchasedItemId, soulBound);
         });
      }
   }

   private IEnumerator CO_ResetItemQueue () {
      yield return new WaitForSeconds(.01f);
      isProcessing = false;
      checkItemQueue();
   }

   public void processClientMineEffect (int oreId, Vector3 position, Direction direction, float angleOffset, float randomSpeed, int effectId, int ownerId, int voyageGroupId) {
      // Create object
      OreNode oreNode = OreManager.self.getOreNode(oreId);
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
   public void Cmd_PickupOre (int nodeId, int oreEffectId, int ownerId, int voyageGroupId) {
      OreNode oreNode = OreManager.self.getOreNode(nodeId);

      // Make sure we found the Node
      if (oreNode == null) {
         D.warning("Ore node not found: " + nodeId);
         return;
      }

      // Make sure the user is in the right instance
      if (_player.instanceId != oreNode.instanceId && oreNode.mapSpecialType == Area.SpecialType.TreasureSite) {
         D.warning("Player trying to open ore node from a different instance!");
         return;
      }

      // If the user has no voyage group check the owner id, if it has a voyage group check the owner group id of the ore
      if ((voyageGroupId < 0 && _player.userId != ownerId) || (voyageGroupId >= 0 && _player.voyageGroupId != voyageGroupId) && oreNode.mapSpecialType == Area.SpecialType.TreasureSite) {
         string message = "This does not belong to you!";
         _player.Target_FloatingMessage(_player.connectionToClient, message);
         return;
      }

      // Add the user ID to the list
      if (oreNode.mapSpecialType == Area.SpecialType.TreasureSite) {
         oreNode.userIds.Add(_player.userId);
      }

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
            AchievementManager.registerUserAchievement(_player, ActionType.GatherItem);

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
   private void giveItemRewardsToPlayer (int userID, List<Item> rewardList, bool showPanel, int chestId = -1) {
      // Create or update the database item
      List<Item> newDatabaseItemList = new List<Item>();
      foreach (Item item in rewardList) {
         Item newDatabaseItem = Item.defaultLootItem();
         if (item.category == Item.Category.Blueprint) {
            newDatabaseItem = new Item { category = Item.Category.Blueprint, count = item.count, data = "" };
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
            D.adminLog("Database item is Blueprint! " + itemCache.xmlId + " : " + EquipmentXMLManager.self.getItemName(itemCache.resultItem), D.ADMIN_LOG_TYPE.Blueprints);
            newDatabaseItem.itemTypeId = itemCache.resultItem.itemTypeId;
         } else {
            newDatabaseItem = item;
         }

         // Make sure that the loot bag armor has an assigned palette value
         if (item.category == Item.Category.Armor && string.IsNullOrEmpty(item.paletteNames)) {
            item.paletteNames = PaletteSwapManager.DEFAULT_ARMOR_PALETTE_NAMES;
         }

         if (!Item.isValidItem(newDatabaseItem)) {
            newDatabaseItem = Item.defaultLootItem();
         }

         // Add to list
         newDatabaseItemList.Add(newDatabaseItem);
      }

      // Calls Reward Popup
      if (showPanel) {
         Target_ReceiveItemList(_player.connectionToClient, rewardList.ToArray());
      }
      if (chestId > 0) {
         // Send it to the specific player that opened it
         Target_OpenChest(_player.connectionToClient, rewardList[0], chestId);
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (Item newDatabaseItem in newDatabaseItemList) {
            if (newDatabaseItem.category == Item.Category.None) {
               D.debug("Error Here! Category Cant be none for Group Item Rewards");
            }

            // New Method, needs observation
            //DB_Main.createItemOrUpdateItemCount(userID, newDatabaseItem);
            processItemCreation(userID, newDatabaseItem);

            // Update soul binding
            if (Bkg_ShouldBeSoulBound(newDatabaseItem, isBeingEquipped: false)) {
               if (!Bkg_IsItemSoulBound(newDatabaseItem)) {
                  if (DB_Main.updateItemSoulBinding(newDatabaseItem.id, isBound: true)) {
                     UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                        ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ItemSoulBound, _player, $"Item '{newDatabaseItem.itemName}' is soul bound!");
                     });
                  }
               }
            }
         }
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

      // Make sure we found the Treasure Chest
      if (chest == null) {
         D.warning("Treasure chest not found: " + chestId);
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         TreasureStateData interactedTreasure = DB_Main.getTreasureStateForChest(_player.userId, chest.chestSpawnId, _player.areaKey);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (interactedTreasure == null) {
               // Make sure the user is in the right instance
               if (_player.instanceId != chest.instanceId) {
                  D.warning("Player trying to open treasure from a different instance!");
                  return;
               }

               // Make sure they didn't already open it
               if (chest.userIds.Contains(_player.userId)) {
                  return;
               }

               // Check if the chest should reward a map fragment and unlock a new location
               Instance instance = InstanceManager.self.getInstance(_player.instanceId);
               if (VoyageManager.isTreasureSiteArea(instance.areaKey) && instance.voyageId > 0) {
                  D.adminLog("Player {" + _player.userId + "} is Receiving Treasure Chest rewards from Treasure site {" + _player.areaKey + "}", D.ADMIN_LOG_TYPE.Treasure);
                  processTreasureSiteChestReward(chest);
               } else {
                  D.adminLog("Player {" + _player.userId + "} is Receiving Treasure Chest rewards from NON Treasure site {" + _player.areaKey + "}", D.ADMIN_LOG_TYPE.Treasure);
                  processChestRewards(chest);
               }

            } else {
               chest.userIds.Add(_player.userId);

               // Display empty open chest
               D.adminLog("Player {" + _player.userId + "} is has a NULL chest for area {" + _player.areaKey + "}", D.ADMIN_LOG_TYPE.Treasure);
               Target_OpenChest(_player.connectionToClient, new Item { category = Item.Category.None, itemTypeId = -1 }, chest.id);
            }
         });
      });
   }

   [Server]
   private void processChestRewards (TreasureChest chest) {
      // Add the user ID to the list
      chest.userIds.Add(_player.userId);

      // Check what we're going to give the user
      Item item = chest.getContents(_player.userId);

      // Make sure that the treasure chest looted armor has an assigned palette value
      if (item.category == Item.Category.Armor && string.IsNullOrEmpty(item.paletteNames)) {
         item.paletteNames = PaletteSwapManager.DEFAULT_ARMOR_PALETTE_NAMES;
      }

      // Add it to their inventory
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Override item values if looted item is a blueprint, blueprints should reference their actual equipment sql id
         if (item.category == Item.Category.Blueprint) {
            CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(item.itemTypeId);
            if (craftingData != null) {
               Item itemCopy = new Item {
                  category = item.category,
                  itemTypeId = item.itemTypeId,
                  id = item.id,
                  count = item.count,
                  data = item.data,
               };
               itemCopy.itemTypeId = craftingData.resultItem.itemTypeId;

               // New Method, needs observation
               //itemCopy = DB_Main.createItemOrUpdateItemCount(_player.userId, itemCopy);
               processItemCreation(_player.userId, itemCopy);
            }
         } else {
            // New Method, needs observation
            //item = DB_Main.createItemOrUpdateItemCount(_player.userId, item);
            processItemCreation(_player.userId, item);
         }

         // Update Soul Binding
         if (Bkg_ShouldBeSoulBound(item, isBeingEquipped: false)) {
            if (!Bkg_IsItemSoulBound(item)) {
               if (DB_Main.updateItemSoulBinding(item.id, isBound: true)) {
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ItemSoulBound, _player, $"The item {item.itemName} is soul bound!");
                  });
               }
            }
         }

         if (item.category == Item.Category.None) {
            D.debug("Error Here! Category Cant be none for Chest Rewards");
         }

         // Update the treasure chest status if is opened, except in treasure site areas since they can be visited again in voyages
         if (!VoyageManager.isTreasureSiteArea(chest.areaKey)) {
            DB_Main.updateTreasureStatus(_player.userId, chest.chestSpawnId, _player.areaKey);
         }
      });

      // Registers the interaction of treasure chests to the achievement database for recording
      AchievementManager.registerUserAchievement(_player, ActionType.OpenTreasureChest);
      AchievementManager.registerUserAchievement(_player, ActionType.LootGainTotal);

      if (item.category == Item.Category.Blueprint) {
         D.debug("Sending Item Reward to player {" + _player.userId + "} item: " + item.category + " : " + item.itemTypeId);
      }

      // Send it to the specific player that opened it
      Target_OpenChest(_player.connectionToClient, item, chest.id);
   }

   [Server]
   private void processTreasureSiteChestReward (TreasureChest chest) {
      // All enemies must be defeated before opening the chest
      Instance instance = InstanceManager.self.getInstance(_player.instanceId);
      if (instance.aliveNPCEnemiesCount > 0) {
         D.warning("Player trying to open a map fragment treasure chest while there are still enemies in the instance!");
         return;
      }

      processChestRewards(chest);

      if (AreaManager.self.getAreaSpecialState(_player.areaKey) > 0) {
         // TODO: If POI has different completion effect, do logic here
      } else {
         // Notify the client that the voyage is complete
         Target_DisplayNotificationForVoyageCompleted(_player.connectionToClient, Notification.Type.VoyageCompleted);
      }

      // Check if the voyage group has opened all the chests in the treasure site
      if (_player.tryGetGroup(out VoyageGroupInfo groupInfo) && TreasureManager.self.areAllChestsOpenedForGroup(instance.id, groupInfo.members)) {
         // Unlink the group from this instance so that another voyage can be started without disbanding
         groupInfo.voyageId = -1;
         VoyageGroupManager.self.updateGroup(groupInfo);
      }
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

      bot.speed = Ship.getBaseSpeed(bot.dataXmlId);
      bot.attackRangeModifier = Ship.getBaseAttackRange(bot.dataXmlId);

      // Assign ship size to spawned ship
      ShipData shipData = ShipDataManager.self.getShipData(bot.dataXmlId);
      bot.shipSize = shipData.shipSize;
      bot.seaEntityData.seaMonsterType = SeaMonsterEntity.Type.PirateShip;

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
      bot.directionFromSpawnPoint = new Vector2(xVal, yVal);
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
         Cmd_RefusePvpInvite(inviterUserId, _player.entityName);
         PvpInviteScreen.self.hide();
         PvpInviteScreen.self.refuseButton.onClick.RemoveAllListeners();
      });
   }

   [Command]
   public void Cmd_RefusePvpInvite (int inviterUserId, string playerName) {
      BodyEntity inviterUser = BodyManager.self.getBody(inviterUserId);
      if (inviterUser != null) {
         inviterUser.Target_ReceiveNormalChat("{" + playerName + "} refused your invite.", ChatInfo.Type.System);
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

   private void processTeamBattle (BattlerInfo[] defenders, BattlerInfo[] attackers, uint netId, bool isGroupBattle, bool createNewEnemy = false, bool isShipBattle = false) {
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

      if (enemy.isDefeated) {
         // Reset player movement restriction if the battle engagement is invalid
         D.debug("Error here! {" + _player.userId + "} is Attempting to engage combat with a defeated enemy! Enemy battle id is {" + enemy.battleId + "}");
         Target_ResetMoveDisable(_player.connectionToClient, "NA");
         return;
      }

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
      int attackerCount = 1;
      List<BattlerInfo> modifiedDefenderList = defenders.ToList();

      // Cache the possible enemy roster
      List<Enemy> enemyRoster = new List<Enemy>();
      if (battleInstance == null) {
         D.debug("The battle instance does not exist anymore! Block process for Player: " + _player.userId + " Instance:" + _player.instanceId);
         return;
      }
      foreach (NetworkBehaviour enemyInstance in battleInstance.getEntities()) {
         if (enemyInstance is Enemy) {
            Enemy enemyInRoster = (Enemy) enemyInstance;
            if (!enemyRoster.Exists(_ => _.enemyType == enemyInRoster.enemyType) && !enemyInRoster.isBossType) {
               enemyRoster.Add(enemyInRoster);
            }
         }
      }

      // Process voyage groups
      foreach (int memberId in groupMembers) {
         NetEntity entity = EntityManager.self.getEntity(memberId);
         if (entity != null) {
            if (entity.userId != _player.userId && _player.instanceId == entity.instanceId) {
               attackerCount++;

               // If this is an admin group party, randomize delay before team member is forced to join
               if (isGroupBattle) {
                  entity.rpc.Target_JoinTeamCombat(entity.connectionToClient, netId, Random.RandomRange(.25f, 4));
               }
            }
         }
      }

      // This block will handle the randomizing of the enemy count 
      int maximumEnemyCount = attackerCount;
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

            if (randomizedSpawnChance < 5 && enemyRoster.Count > 0) {
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

            if (modifiedDefenderList.Count >= attackerCount) {
               break;
            }
         }
      }

      if (Battle.FORCE_COMPLETE_DEFENDING_TEAM) {
         int battlerToBeAddedCount = Battle.MAX_ENEMY_COUNT - modifiedDefenderList.Count;

         for (int i = 0; i < battlerToBeAddedCount; i++) {
            BattlerInfo battlerInfo = modifiedDefenderList.ChooseRandom();
            BattlerData battlerData = MonsterManager.self.getBattlerData(battlerInfo.enemyType);
            modifiedDefenderList.Add(new BattlerInfo {
               battlerName = battlerData.enemyName,
               battlerType = BattlerType.AIEnemyControlled,
               enemyType = battlerInfo.enemyType,
               battlerXp = battlerInfo.battlerXp,
               companionId = 0
            });
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
      Battle battle = isExistingBattle && !enemy.isDefeated ? (BattleManager.self.getBattle(enemy.battleId)) : BattleManager.self.createTeamBattle(area, instance, enemy, attackers, localBattler, modifiedDefenderList.ToArray(), isShipBattle);

      if (battle == null) {
         D.debug("Error here! Trying to engage battle but the Battle is NULL!! Battle id {" + enemy.battleId + "} is probably finished");

         // Reset player movement restriction if the battle engagement is invalid
         Target_ResetMoveDisable(_player.connectionToClient, "NA");
      } else {
         // If the Battle is full, we can't proceed
         if (!battle.hasRoomLeft(Battle.TeamType.Attackers)) {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The battle is already full!");
            return;
         }

         // Adds the player to the newly created or existing battle
         BattleManager.self.addPlayerToBattle(battle, localBattler, Battle.TeamType.Attackers);

         // After joining, the server will send all the queued rpc actions to the newly joined client
         foreach (QueuedRpcAction rpcBattleAction in battle.queuedRpcActionList) {
            Target_ReceiveCombatAction(_player.connectionToClient, enemy.battleId,
               rpcBattleAction.actionSerialized,
               rpcBattleAction.battleActionType,
               rpcBattleAction.isCancelAction);
         }

         // Handles ability related logic
         processPlayerAbilities(localBattler, bodyEntities);

         // Send Battle Bg data
         int bgXmlID = battle.battleBoard.xmlID;
         Target_ReceiveBackgroundInfo(_player.connectionToClient, bgXmlID, battle.isShipBattle);
      }
   }

   [TargetRpc]
   public void Target_ReceiveCombatAction (NetworkConnection connection, int battleId, string[] actionStrings, BattleActionType battleActionType, bool cancelAbility) {
      StartCoroutine(CO_ProcessBattleAction(battleId, actionStrings, battleActionType, cancelAbility));
   }

   private IEnumerator CO_ProcessBattleAction (int battleId, string[] actionStrings, BattleActionType battleActionType, bool cancelAbility) {
      while (BattleManager.self.getBattle(battleId) == null) {
         yield return 0;
      }

      Battle battle = BattleManager.self.getBattle(battleId);
      battle.processCombatAction(actionStrings, battleActionType, cancelAbility, true);
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
      if (localBattler == null || bodyEntities.Count < 1) {
         D.debug("Error here! Battler or body entities are insufficient!");
         return;
      }

      // Enter the background thread to determine if the user has at least one ability equipped
      bool hasAbilityEquipped = false;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the skill list from database
         List<AbilitySQLData> abilityDataList = DB_Main.userAbilities(localBattler.userId, AbilityEquipStatus.Equipped);

         // Determine if at least one ability is equipped
         hasAbilityEquipped = abilityDataList.Exists(_ => _.equipSlotIndex != -1);

         // If no ability is equipped, Add "Basic Attack" to player abilities in slot 0.
         if (!hasAbilityEquipped) {
            DB_Main.updateAbilitySlot(_player.userId, Global.BASIC_ATTACK_ID, 0);
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            finalizePlayerAbilityProcess(localBattler, bodyEntities, abilityDataList);
         });
      });
   }

   private void finalizePlayerAbilityProcess (PlayerBodyEntity localBattler, List<PlayerBodyEntity> bodyEntities, List<AbilitySQLData> abilityDataList) {
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
      int validOffenseAbilities = 0;
      int validBuffAbilities = 0;
      const int MAX_ABILITIES = 5;
      List<int> invalidSlot = new List<int> { 0, 1, 2, 3, 4, 5 };
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
               validOffenseAbilities++;
               D.adminLog("Valid Attack Ability:: " +
                  " Name: {" + basicAbilityData.itemName +
                  "} ID: {" + basicAbilityData.itemID +
                  "} Type: {" + basicAbilityData.abilityType +
                  "} Slot: {" + abilitySql.equipSlotIndex +
                  "} Class: {" + basicAbilityData.classRequirement + "}", D.ADMIN_LOG_TYPE.Ability);
               invalidSlot.Remove(abilitySql.equipSlotIndex);
            }
         } else if (basicAbilityData.abilityType == AbilityType.BuffDebuff) {
            BuffAbilityData buffAbilityData = AbilityManager.self.allBuffAbilities.Find(_ => _.itemID == abilitySql.abilityID);
            if (weaponClass == Weapon.Class.Rum && buffAbilityData.isRum()) {
               validBuffAbilities++;
               D.adminLog("Valid Buff Ability:: " +
                  " Name: {" + basicAbilityData.itemName +
                  "} ID: {" + basicAbilityData.itemID +
                  "} Type: {" + basicAbilityData.abilityType +
                  "} Slot: {" + abilitySql.equipSlotIndex +
                  "} Class: {" + basicAbilityData.classRequirement + "}", D.ADMIN_LOG_TYPE.Ability);
               invalidSlot.Remove(abilitySql.equipSlotIndex);
            }
         }
      }

      if (validOffenseAbilities < 1) {
         D.debug("No valid Attack ability assigned to player" + " : " + _player.userId);
      }

      if (validBuffAbilities < 1) {
         D.adminLog("No valid Buff ability assigned to player" + " : " + _player.userId, D.ADMIN_LOG_TYPE.Ability);
      }

      // If no abilities were fetched, create a clean new entry that will be overridden based on the user equipped weapon
      if (equippedAbilityList.Count < 1) {
         validOffenseAbilities = 0;
         equippedAbilityList = new List<AbilitySQLData>();
         equippedAbilityList.Add(new AbilitySQLData {
            abilityID = -1,
            name = "",
         });
      }
      bool overrideFirstAbility = validOffenseAbilities < 1;

      // Override ability if no ability matches the weapon type ex:{all melee abilities but user has gun weapon}
      if (weaponId < 1 && overrideFirstAbility) {
         D.adminLog("Overriding first ability slot as Punch Ability, No weapon equipped", D.ADMIN_LOG_TYPE.Ability);
         weaponCategory = WeaponCategory.None;
         equippedAbilityList[0] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getPunchAbility());
      } else {
         AbilitySQLData cachedAbility = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getPunchAbility());
         if (weaponData != null) {
            AbilitySQLData defaultAbilityCheck = getDefaultAbility(weaponClass, overrideFirstAbility, equippedAbilityList.Count < MAX_ABILITIES);
            if (defaultAbilityCheck != null) {
               cachedAbility = defaultAbilityCheck;
               equippedAbilityList[0] = cachedAbility;
            }
            weaponCategory = Weapon.GetWeaponCategoryByClass(weaponClass);
         } else {
            D.adminLog("Overriding first ability slot as Punch Ability, Missing weapon data for {" + weaponId + "}", D.ADMIN_LOG_TYPE.Ability);
            weaponCategory = WeaponCategory.None;
            cachedAbility = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getPunchAbility());
            equippedAbilityList[0] = cachedAbility;
         }

         if (overrideFirstAbility) {
            D.adminLog("New ability assigned to player: "
               + " Name: " + cachedAbility.name
               + " ID: " + cachedAbility.abilityID
               + " WepClass: " + weaponClass
               + " WepCateg: " + weaponCategory, D.ADMIN_LOG_TYPE.Ability);
         }
      }
      D.adminLog("ValidBuffs: {" + validBuffAbilities + "} ValidAttacks: {" + validOffenseAbilities + "} WeaponCategory: {" + weaponCategory + "}", D.ADMIN_LOG_TYPE.Ability);

      // Make sure that if the user is equipping a Rum weapon, assign both buff and attack ability
      if (validBuffAbilities < 1 && weaponClass == Weapon.Class.Rum) {
         int firstInvalidSlot = invalidSlot[0];
         string slots = "";
         foreach (int abilitySlot in invalidSlot) {
            slots += " : " + abilitySlot;
         }
         D.adminLog("Ability slots:: " + slots, D.ADMIN_LOG_TYPE.Ability);

         if (equippedAbilityList.Count < MAX_ABILITIES) {
            D.adminLog("Added Buff Ability in slots:: " + slots, D.ADMIN_LOG_TYPE.Ability);
            equippedAbilityList.Add(AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getHealingRumAbility()));
         } else {
            D.adminLog("Override Buff Ability in index {" + firstInvalidSlot + "} in slots:: " + slots, D.ADMIN_LOG_TYPE.Ability);
            AbilitySQLData abilityToOverride = equippedAbilityList.Find(_ => _.equipSlotIndex == firstInvalidSlot);
            int slotToOverride = equippedAbilityList.IndexOf(abilityToOverride);
            equippedAbilityList[slotToOverride] = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getHealingRumAbility());
         }
      }

      // Make sure no invalid abilities are part of the ability list to be sent
      foreach (AbilitySQLData ability in equippedAbilityList.FindAll(_ => _.abilityID < 1)) {
         D.adminLog("Removing Ability due to invaid slot id :: Name: " + ability.name
            + " ID: " + ability.abilityID
            + " Type: " + ability.abilityType, D.ADMIN_LOG_TYPE.Ability);
      }
      equippedAbilityList.RemoveAll(_ => _.abilityID < 1);

      // Cache the invalid abilities that will be discarded
      List<int> discardedAbilities = new List<int>();
      int equipmentCount = equippedAbilityList.Count;
      for (int i = 0; i < equipmentCount; i++) {
         try {
            int currEquipSlot = equippedAbilityList[i].equipSlotIndex;
            if (invalidSlot.Contains(currEquipSlot)) {
               discardedAbilities.Add(currEquipSlot);
            }
         } catch {
            // TODO: Remove after playtest
            D.debug("Problem with processing discarded abilities!");
         }
      }

      for (int i = 0; i < discardedAbilities.Count; i++) {
         try {
            AbilitySQLData invalidAbility = equippedAbilityList.Find(_ => _.equipSlotIndex == discardedAbilities[i]);
            if (invalidAbility != null) {
               D.adminLog("This is an invalid ability, removing now {" + invalidAbility.equipSlotIndex + "}", D.ADMIN_LOG_TYPE.Ability);
               equippedAbilityList.Remove(invalidAbility);
            } else {
               D.debug("Error here! Attempted to fetch discarded ability but none was found! {" + discardedAbilities[i] + "}");
            }
         } catch {
            // TODO: Remove after playtest
            D.debug("Problem with processing invalid abilities!");
         }
      }

      // Provides all the abilities for the players in the party
      setupAbilitiesForPlayers(bodyEntities, equippedAbilityList, weaponClass, validOffenseAbilities, validOffenseAbilities > 0, weaponCategory);
   }

   private AbilitySQLData getDefaultAbility (Weapon.Class weaponClass, bool overrideFirstAbility, bool isSufficientAbilityCount) {
      AbilitySQLData cachedAbility = null;
      switch (weaponClass) {
         case Weapon.Class.Melee:
            if (overrideFirstAbility) {
               D.adminLog("Overriding first ability slot as Slash Ability", D.ADMIN_LOG_TYPE.Ability);
               cachedAbility = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getSlashAbility());
            }
            break;
         case Weapon.Class.Ranged:
            if (overrideFirstAbility) {
               D.adminLog("Overriding first ability slot as Shoot Ability", D.ADMIN_LOG_TYPE.Ability);
               cachedAbility = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getShootAbility());
            }
            break;
         case Weapon.Class.Rum:
            if (overrideFirstAbility) {
               if (isSufficientAbilityCount) {
                  D.adminLog("Adding new ability slot, Rum Ability", D.ADMIN_LOG_TYPE.Ability);
                  cachedAbility = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getThrowRumAbility());
               } else {
                  D.adminLog("Overriding first ability slot as Rum Ability", D.ADMIN_LOG_TYPE.Ability);
                  cachedAbility = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getThrowRumAbility());
               }
            }
            break;
         default:
            if (overrideFirstAbility) {
               D.adminLog("Overriding first ability slot as Punch Ability, Unrecognized category", D.ADMIN_LOG_TYPE.Ability);
               cachedAbility = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getPunchAbility());
            } else {
               D.adminLog("Setup ability as Punch, weapon class unrecognized! {" + weaponClass + "}", D.ADMIN_LOG_TYPE.Ability);
            }
            break;
      }
      return cachedAbility;
   }

   private List<AbilitySQLData> modifiedUnarmedAbilityList () {
      List<AbilitySQLData> newAbilityList = new List<AbilitySQLData>();
      AbilitySQLData abilitySql = AbilitySQLData.TranslateBasicAbility(AbilityManager.self.getPunchAbility());
      abilitySql.equipSlotIndex = 0;
      newAbilityList.Add(abilitySql);

      return newAbilityList;
   }

   [Command]
   public void Cmd_StartNewBattle (uint enemyNetId, Battle.TeamType teamType, bool isGroupBattle, bool isShipBattle) {
      D.adminLog("Player is starting new battle, IsLeagueArea:{" + VoyageManager.isAnyLeagueArea(_player.areaKey) + "} IsPvpArena :{" + VoyageManager.isPvpArenaArea(_player.areaKey)
         + "} IsTreasureSite:{" + VoyageManager.isTreasureSiteArea(_player.areaKey) + "} CanPlayerStay{" + canPlayerStayInVoyage() + "}"
         + " SpecialType: {" + AreaManager.self.getAreaSpecialType(_player.areaKey) + "}", D.ADMIN_LOG_TYPE.Combat);

      if ((VoyageManager.isAnyLeagueArea(_player.areaKey) || VoyageManager.isPvpArenaArea(_player.areaKey) || VoyageManager.isTreasureSiteArea(_player.areaKey)) && !canPlayerStayInVoyage()) {
         string reason = "";
         if (VoyageManager.isTreasureSiteArea(_player.areaKey)) {
            reason += "This is not a treasure area\n";
         }
         if (!canPlayerStayInVoyage()) {
            reason += "Player cant stay in the voyage\n";
         }

         D.debug("Player {" + _player.userId + "}" + " attempted to engage in combat due invalid voyage conditions, returning to town" + " Reason: {" + reason + "}");
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
         processTeamBattle(battlerInfoList.ToArray(), new BattlerInfo[0], enemyNetId, isGroupBattle, isShipBattle: isShipBattle);
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
               processTeamBattle(battlerInfoList.ToArray(), allyInfoList.ToArray(), enemyNetId, false, isShipBattle: isShipBattle);
            });
         });
      }
   }

   [TargetRpc]
   public void Target_ResetMoveDisable (NetworkConnection connection, string battleId) {
      if (Global.player == null) {
         D.debug("Player is missing! Battle ID: " + battleId);
         return;
      }

      if (Global.player is PlayerBodyEntity) {
         PlayerBodyEntity playerBody = (PlayerBodyEntity) Global.player;
         playerBody.isWithinEnemyRadius = false;
         playerBody.playerBattleCollider.combatInitCollider.enabled = true;
      } else {
         D.debug("Failed to restore player movement! Battle ID: " + battleId);
      }
   }

   [TargetRpc]
   public void Target_RespawnAtTreasureSiteEntrance (NetworkConnection connection, Vector2 spawnLocalPosition) {
      if (Global.player == null) {
         return;
      }

      PlayerBodyEntity playerBody = Global.player.getPlayerBodyEntity();
      if (playerBody == null) {
         return;
      }

      playerBody.transform.localPosition = spawnLocalPosition;
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
         List<AbilitySQLData> abilityDataList = DB_Main.userAbilities(_player.userId, AbilityEquipStatus.Equipped);

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
            if (entity == null) {
               D.debug("Error here! Entity reference was severed!");
               continue;
            }

            // Retrieves skill list from database
            List<AbilitySQLData> abilityDataList = DB_Main.userAbilities(entity.userId, AbilityEquipStatus.ALL);
            //string rawAbilityData = DB_Main.userAbilities(entity.userId.ToString(), ((int) AbilityEquipStatus.ALL).ToString());
            //List<AbilitySQLData> abilityDataList = JsonConvert.DeserializeObject<List<AbilitySQLData>>(rawAbilityData);

            // Set user to only use skill if no weapon is equipped
            if (entity.weaponManager.weaponType == 0) {
               abilityDataList = modifiedUnarmedAbilityList();
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               List<AbilitySQLData> equippedAbilityDataList = abilityDataList.FindAll(_ => _.equipSlotIndex >= 0);
               Battler battler = BattleManager.self.getBattler(entity.userId);

               List<int> basicAbilityIds = new List<int>();
               foreach (AbilitySQLData ability in equippedAbilityList) {
                  if (ability != null) {
                     basicAbilityIds.Add(ability.abilityID);
                  } else {
                     D.debug("Error here! Missing ability SQL Data reference!");
                  }
               }

               if (battler != null) {
                  string abilityStrList = "";
                  foreach (int abilityId in basicAbilityIds) {
                     BasicAbilityData abilityInfo = AbilityManager.self.allGameAbilities.Find(_ => _.itemID == abilityId);
                     if (abilityInfo != null) {
                        abilityStrList += " : {ID: {" + abilityId + "} :: Name: {" + (abilityInfo == null ? "Null" : abilityInfo.itemName) + "}";
                     } else {
                        D.debug("Error here! Basic ability {" + abilityId + "} failed to fetch!");
                     }
                  }
                  D.adminLog("Sending Overridden Ability Data to Player: " + battler.userId + " :: " + abilityStrList, D.ADMIN_LOG_TYPE.Ability);
                  battler.setBattlerAbilities(basicAbilityIds, BattlerType.PlayerControlled);
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
   public void Target_ReceiveBackgroundInfo (NetworkConnection connection, int bgXmlId, bool isShipBattle) {
      BackgroundGameManager.self.activateBgContent(bgXmlId, isShipBattle);
   }

   #endregion

   [Command]
   public void Cmd_RequestAbility (int abilityTypeInt, uint netId, int abilityInventoryIndex, bool cancelAction) {
      AbilityType abilityType = (AbilityType) abilityTypeInt;

      if (_player == null || !(_player is PlayerBodyEntity)) {
         Target_ReceiveRefreshCasting(connectionToClient, false, "Missing Player");
         return;
      }

      // Look up the player's Battle object
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      Battle battle = BattleManager.self.getBattle(playerBody.battleId);
      if (battle == null) {
         Target_ReceiveRefreshCasting(connectionToClient, false, "Missing Battle");
         return;
      }

      Battler sourceBattler = battle.getBattler(_player.userId);
      Battler targetBattler = null;
      BasicAbilityData abilityData = new BasicAbilityData();

      if (abilityInventoryIndex > -1) {
         if (abilityType == AbilityType.Standard) {
            // Get the ability from the battler abilities.
            try {
               abilityData = sourceBattler.getAttackAbilities()[abilityInventoryIndex];
            } catch {
               abilityData = AbilityManager.self.getPunchAbility();
            }
         } else {
            // Get the ability from the battler abilities.
            abilityData = sourceBattler.getBuffAbilities()[abilityInventoryIndex];
         }
      } else {
         abilityData = AbilityManager.self.getPunchAbility();
      }

      foreach (Battler participant in battle.getParticipants()) {
         if (participant.netId == netId) {
            targetBattler = participant;
         }
      }

      // Ignore invalid or dead sources and targets
      if (sourceBattler == null || targetBattler == null || sourceBattler.isDead() || targetBattler.isDead()) {
         Target_ReceiveRefreshCasting(connectionToClient, false, "Invalid Source or Target Battler");
         return;
      }

      // Make sure the source battler can use that ability type
      if (!abilityData.isReadyForUseBy(sourceBattler) && !cancelAction) {
         Target_ReceiveRefreshCasting(connectionToClient, false, "Ability Not Ready");
         return;
      }

      if (abilityType == AbilityType.Standard) {
         // If it's a Melee Ability, make sure the target isn't currently protected
         if (((AttackAbilityData) abilityData).isMelee() && targetBattler.isProtected(battle)) {
            D.warning("Battler requested melee ability against protected target! Player: " + playerBody.entityName);
            Target_ReceiveRefreshCasting(connectionToClient, false, "Target Protected");
            return;
         }
      }

      // Let the Battle Manager handle executing the ability
      List<Battler> targetBattlers = new List<Battler>() { targetBattler };
      if (cancelAction) {
         BattleManager.self.cancelBattleAction(battle, sourceBattler, targetBattlers, abilityInventoryIndex, abilityType);
         Target_ReceiveRefreshCasting(connectionToClient, false, "Cancel Action");
      } else {
         if (!sourceBattler.battlerAbilitiesInitialized && sourceBattler.getBasicAbilities().Count < 1 && sourceBattler.enemyType == Enemy.Type.PlayerBattler) {
            Target_ReceiveRefreshCasting(connectionToClient, true, "Invalid Ability Set");
            return;
         }

         sourceBattler.canExecuteAction = false;
         BattleManager.self.executeBattleAction(battle, sourceBattler, targetBattlers, abilityInventoryIndex, abilityType);
      }
   }

   [TargetRpc]
   public void Target_ReceiveRefreshCasting (NetworkConnection connection, bool refreshAbilityCache, string reason) {
      D.adminLog("Battler can now cast again! After Refresh Casting: " + reason, D.ADMIN_LOG_TYPE.AbilityCast);
      BattleManager.self.getPlayerBattler().setBattlerCanCastAbility(true);
      if (refreshAbilityCache) {
         AttackPanel.self.clearCachedAbilityCast();
      }
   }

   [Command]
   public void Cmd_RequestStanceChange (Battler.Stance newStance) {
      requestStanceChange(newStance);
   }

   [Server]
   public void requestStanceChange (Battler.Stance newStance) {
      if (_player == null || !(_player is PlayerBodyEntity)) {
         return;
      }

      // Look up the player's Battle object
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      Battle battle = BattleManager.self.getBattle(playerBody.battleId);
      if (battle == null) {
         return;
      }

      Battler sourceBattler = battle.getBattler(_player.userId);
      if (sourceBattler == null) {
         return;
      }
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
   protected void getShipsForArea (int shopId) {
      bool findByShopName = shopId > 0 ? true : false;

      // Get the current list of ships for the area
      List<ShipInfo> list = new List<ShipInfo>();
      if (findByShopName) {
         list = ShopManager.self.getShipsByShopId(shopId);
      } else {
         list = ShopManager.self.getShips(_player.areaKey);
      }

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         ShopData shopData = new ShopData();
         if (findByShopName) {
            shopData = ShopXMLManager.self.getShopDataById(shopId);
         } else {
            shopData = ShopXMLManager.self.getShopDataByArea(_player.areaKey);
         }

         if (shopData == null) {
            D.debug("Shop data is missing for: " + shopId + " - " + _player.areaKey);
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
   protected void getItemsForArea (int shopId) {
      bool findByShopName = shopId > 0 ? true : false;

      // Get the current list of items for the area
      List<Item> list = new List<Item>();
      if (findByShopName) {
         list = ShopManager.self.getItemsByShopId(shopId);
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
            case Item.Category.Blueprint:
               // TODO: Do additional blueprint logic here if necessary
               break;
            default:
               D.debug("UNKNOWN data setup is " + " " + item.category + " " + item.itemTypeId + " " + item.data + " " + item.itemName);
               break;
         }
      }

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ShopData shopData = new ShopData();
            if (findByShopName) {
               shopData = ShopXMLManager.self.getShopDataById(shopId);
            } else {
               shopData = ShopXMLManager.self.getShopDataByArea(_player.areaKey);
            }

            if (shopData == null) {
               D.debug("Problem Fetching Shop Name!" + " : " + shopId);
            } else {
               string greetingText = shopData.shopGreetingText;
               _player.rpc.Target_ReceiveShopItems(_player.connectionToClient, gold, Util.serialize(sortedList), greetingText);
            }
         });
      });
   }

   [Server]
   private void Bkg_RequestSetArmorId (int armorId) {
      DB_Main.setArmorId(_player.userId, armorId);
      UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
      bool justSoulBound = false;

      if (Bkg_ShouldBeSoulBound(userObjects.armor, isBeingEquipped: true)) {
         if (!Bkg_IsItemSoulBound(userObjects.armor)) {
            justSoulBound = DB_Main.updateItemSoulBinding(armorId, isBound: true);
         }
      }

      // Back to Unity
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         Armor armor = Armor.castItemToArmor(userObjects.armor);

         if (armor.data.Length < 1 && armor.itemTypeId > 0 && armor.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
            armor.data = ArmorStatData.serializeArmorStatData(EquipmentXMLManager.self.getArmorDataBySqlId(armor.itemTypeId));
            userObjects.armor.data = armor.data;
         }

         PlayerBodyEntity body = _player.GetComponent<PlayerBodyEntity>();

         if (body != null) {
            body.armorManager.updateArmorSyncVars(armor.itemTypeId, armor.id, armor.paletteNames, armor.durability);

            // Update the battler entity if the user is in battle
            if (body.isInBattle()) {
               BattleManager.self.onPlayerEquipItem(body);
            }
         }

         PlayerShipEntity ship = _player.GetComponent<PlayerShipEntity>();

         if (ship != null) {
            ArmorStatData data = EquipmentXMLManager.self.getArmorDataBySqlId(armor.itemTypeId);
            ship.armorType = data != null ? data.armorType : 0;
            ship.armorColors = armor.paletteNames;
         }

         if (userObjects == null) {
            D.debug("Null user objects!");
         } else {
            if (justSoulBound) {
               Target_OnEquipSoulBoundItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat, userObjects.armor);
            } else {
               Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);
            }
         }
      });
   }

   [Server]
   public void requestSetArmorId (int armorId) {
      D.adminLog("Requesting new armor: " + armorId, D.ADMIN_LOG_TYPE.Equipment);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Bkg_RequestSetArmorId(armorId);
      });
   }

   [Server]
   private void Bkg_RequestSetHatId (int hatId) {
      DB_Main.setHatId(_player.userId, hatId);
      UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
      Item hat = userObjects.hat;
      bool justSoulBound = false;

      if (Bkg_ShouldBeSoulBound(userObjects.hat, isBeingEquipped: true)) {
         if (!Bkg_IsItemSoulBound(userObjects.hat)) {
            justSoulBound = DB_Main.updateItemSoulBinding(hatId, isBound: true);
         }
      }

      // Back to Unity
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {

         if (hat.data.Length < 1 && hat.itemTypeId > 0 && hat.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
            hat.data = HatStatData.serializeHatStatData(EquipmentXMLManager.self.getHatData(hat.itemTypeId));
            userObjects.hat.data = hat.data;
         }

         PlayerBodyEntity body = _player.GetComponent<PlayerBodyEntity>();

         if (body != null) {
            body.hatsManager.updateHatSyncVars(hat.itemTypeId, hat.id, hat.paletteNames);

            // Update the battler entity if the user is in battle
            if (body.isInBattle()) {
               BattleManager.self.onPlayerEquipItem(body);
            }
         }

         PlayerShipEntity ship = _player.GetComponent<PlayerShipEntity>();

         if (ship != null) {
            HatStatData data = EquipmentXMLManager.self.getHatData(hat.itemTypeId);
            ship.hatType = data != null ? data.hatType : 0;
            ship.hatColors = hat.paletteNames;
         }

         if (userObjects == null) {
            D.debug("Null user objects!");
         } else {
            if (justSoulBound) {
               Target_OnEquipSoulBoundItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat, userObjects.hat);
            } else {
               Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);
            }
         }
      });
   }

   [Server]
   public void Bkg_RequestSetWeaponId (int weaponId) {
      // Update the Weapon object here on the server based on what's in the database
      DB_Main.setWeaponId(_player.userId, weaponId);
      UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
      bool justSoulBound = false;

      if (Bkg_ShouldBeSoulBound(userObjects.weapon, isBeingEquipped: true)) {
         if (!Bkg_IsItemSoulBound(userObjects.weapon)) {
            justSoulBound = DB_Main.updateItemSoulBinding(weaponId, isBound: true);
         }
      }

      // Back to Unity Thread to call RPC functions
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         Weapon weapon = Weapon.castItemToWeapon(userObjects.weapon);

         if (weapon.data.Length < 1 && weapon.itemTypeId > 0 && weapon.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
            weapon.data = WeaponStatData.serializeWeaponStatData(EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId));
            userObjects.weapon.data = weapon.data;
         }

         PlayerBodyEntity body = _player.GetComponent<PlayerBodyEntity>();

         if (body != null) {
            body.weaponManager.updateWeaponSyncVars(weapon.itemTypeId, weapon.id, weapon.paletteNames, userObjects.weapon.durability, weapon.count);
            D.adminLog("Player {" + body.userId + "} is equipping item" +
               "} ID: {" + weapon.id +
               "} TypeId: {" + weapon.itemTypeId +
               "} Name: {" + weapon.getName() + "}", D.ADMIN_LOG_TYPE.Equipment);

            // Update the battler entity if the user is in battle
            if (body.isInBattle()) {
               BattleManager.self.onPlayerEquipItem(body);
            }
         } else {
            D.editorLog("Failed to cast to <PlayerBodyEntity>!", Color.magenta);
         }

         PlayerShipEntity ship = _player.GetComponent<PlayerShipEntity>();

         if (ship != null) {
            WeaponStatData data = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);
            ship.weaponType = data != null ? data.weaponType : 0;
            ship.weaponColors = weapon.paletteNames;
            ship.weaponCount = weapon.count;
         }

         if (_player != null) {
            if (userObjects == null) {
               D.debug("Null user objects!");
            } else {
               if (justSoulBound) {
                  Target_OnEquipSoulBoundItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat, userObjects.weapon);
               } else {
                  Target_OnEquipItem(_player.connectionToClient, userObjects.weapon, userObjects.armor, userObjects.hat);
               }
            }
         } else {
            D.debug("Cant change equipment while player is destroyed");
         }
      });
   }

   [Server]
   public void requestSetHatId (int hatId) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Bkg_RequestSetHatId(hatId);
      });
   }

   [Server]
   public void requestSetWeaponId (int weaponId) {
      D.adminLog("Requesting new weapon: " + weaponId, D.ADMIN_LOG_TYPE.Equipment);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Bkg_RequestSetWeaponId(weaponId);
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
      if (_player == null) {
         return;
      }

      if (DiscoveryManager.self.isDiscoveryFindingValid(discoveryId, _player, out Discovery discovery)) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Check if discovery is found in the database
            UserDiscovery d = DB_Main.getUserDiscovery(_player.userId, discoveryId);
            if (d == null || !d.discovered) {
               int gainedXP = discovery.getXPValue();

               // Add the experience to the player
               DB_Main.addJobXP(_player.userId, Jobs.Type.Explorer, gainedXP);

               // Set the discovery as found
               DB_Main.setUserDiscovery(new UserDiscovery { userId = _player.userId, discoveryId = discoveryId, discovered = true });

               Jobs newJobXP = DB_Main.getJobXP(_player.userId);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  _player.Target_GainedXP(netIdentity.connectionToClient, gainedXP, newJobXP, Jobs.Type.Explorer, 0, true);
               });
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               discovery.Target_RevealDiscovery(netIdentity.connectionToClient);
            });
         });
      } else {
         D.log($"Player {_player.nameText.text} reported an invalid discovery. Player Instance ID: {_player.instanceId}");
      }
   }

   [TargetRpc]
   public void Target_ReceiveFoundDiscoveryList (List<UserDiscovery> discoveries) {
      if (_player == null) {
         return;
      }

      foreach (UserDiscovery d in discoveries) {
         if (d.discovered) {
            if (!DiscoveryManager.self.revealedDiscoveriesClient.Contains(d.discoveryId)) {
               DiscoveryManager.self.revealedDiscoveriesClient.Add(d.discoveryId);
            }
         }
      }
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

      // Don't check if somone is already customising for guild maps
      if (!CustomMapManager.isGuildSpecificAreaKey(areaKey)) {
         // Check if someone is already customizing
         if (instance.playerMakingCustomizations != null && instance.playerMakingCustomizations.userId != _player.userId) {
            Target_DenyEnterMapCustomization("Someone else is already customizing the area");
            return;
         }

         // Allow player to enter customization
         instance.playerMakingCustomizations = _player;
      }

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
         foreach (Map relatedMap in manager.getRelatedMaps()) {
            D.adminLog("Client: Related custom map is" + " : " + relatedMap.name + " : " + relatedMap.displayName + " : " + relatedMap.id, D.ADMIN_LOG_TYPE.CustomMap);
         }
         PanelManager.self.get<CustomMapsPanel>(Panel.Type.CustomMaps).displayFor(manager, warpAfterSelecting);
      } else {
         D.error($"Cannot find custom map manager for key: { customMapType }");
      }
   }

   [Command]
   public void Cmd_RequestCustomMapPanelClient (string customMapType, bool warpAfterSelecting) {
      if (!AreaManager.self.tryGetCustomMapManager(customMapType, out CustomMapManager manager)) {
         D.adminLog("Custom map manager cant fetch custom map for: " + customMapType, D.ADMIN_LOG_TYPE.CustomMap);
         return;
      }
      foreach (Map relatedMap in manager.getRelatedMaps()) {
         D.adminLog("Server RpcMngr: Related custom map is" + " : " + relatedMap.name + " : " + relatedMap.displayName + " : " + relatedMap.id, D.ADMIN_LOG_TYPE.CustomMap);
      }

      _player.rpc.Target_ShowCustomMapPanel(customMapType, warpAfterSelecting, manager.getRelatedMaps());
   }

   [Command]
   public async void Cmd_SetCustomMapBaseMap (string customMapKey, int baseMapId, bool warpIntoAfterSetting) {
      D.adminLog("Player {" + _player.userId + "} has selected map {" + baseMapId + " : " + customMapKey + "} as custom map", D.ADMIN_LOG_TYPE.CustomMap);

      // Check if this is a custom map key
      if (!AreaManager.self.tryGetCustomMapManager(customMapKey, out CustomMapManager manager)) {
         string errorMsg = "Failed to get map key from custom map manager {" + customMapKey + "}";
         D.debug(errorMsg);
         _player.rpc.Target_ReceiveNoticeFromServer(_player.connectionToClient, errorMsg);
         return;
      }

      // Check if this type of custom map has this base map
      if (!manager.getRelatedMaps().Any(m => m.id == baseMapId)) {
         string errorMsg = "Failed to process map, map manager does not have map id {" + baseMapId + "}";
         D.debug(errorMsg);
         _player.rpc.Target_ReceiveNoticeFromServer(_player.connectionToClient, errorMsg);
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
      } else if (manager is CustomGuildMapManager) {
         await DB_Main.execAsync((cmd) => DB_Main.setCustomGuildMapBase(cmd, _player.guildId, baseMapId));

         GuildInfo playerGuildInfo = await DB_Main.execAsync((cmd) => DB_Main.getGuildInfo(_player.guildId));
         
         // Update the guildMapBaseId for any members online
         foreach (UserInfo memberInfo in playerGuildInfo.guildMembers) {
            NetEntity playerEntity = EntityManager.self.getEntity(memberInfo.userId);
            if (playerEntity != null) {
               playerEntity.guildMapBaseId = baseMapId;
            }
         }

      } else {
         D.error("Unrecognized custom map manager");
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
   public async void Cmd_AddPrefabCustomization (int areaOwnerId, string areaKey, PrefabState changes, int areaGuildId) {
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
      int baseMapId;

      // If the area has a valid owner id
      if (areaOwnerId != -1) {
         NetEntity areaOwner = EntityManager.self.getEntity(areaOwnerId);
         baseMapId = customMapManager.getBaseMapId(areaOwner);
      
      // If the area has a valid guild owner id
      } else if (areaGuildId != -1) {
         baseMapId = customMapManager.getBaseMapId(_player);
      } else {
         string errorString = "Couldn't find the 'owner' of the map we are customising. OwnerId: " + areaOwnerId + ", GuildId: " + areaGuildId;
         D.error(errorString);
         Target_FailAddPrefabCustomization(changes, errorString);
         return;
      }

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
            if (netIdentity != null) {
               Target_SetPerkPoints(netIdentity.connectionToClient, userPerks.ToArray());
            }
         });
      });
   }

   [TargetRpc]
   public void Target_SetPerkPoints (NetworkConnection conn, Perk[] perks) {
      PerkManager.self.setPlayerPerkPoints(perks);
      PerksPanel.self.isAssigningPerkPoint = false;
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
               PerkManager.self.updatePerkPointsForUser(_player.userId, userPerks);
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

      int index = 0;
      foreach (Warp warp in warps) {
         // Only warp the player if they're close enough to the warp. Check area and spawn targets are the ones player requested just in case two warps are too close together.
         float distanceToWarp = Vector2.Distance(warp.transform.position, transform.position);
         if (distanceToWarp < 2f && areaTarget == warp.areaTarget && spawnTarget == warp.spawnTarget && warp.canPlayerUseWarp(_player)) {
            D.adminLog(index + "::DistanceToWarp(Req:<2) {" + distanceToWarp + "}" +
               "AreaTarget: {" + areaTarget + "}={" + warp.areaTarget + "}" +
               "SpawnTarget: {" + spawnTarget + "}={" + warp.spawnTarget + "}" +
               "CanUseWarp? {" + warp.canPlayerUseWarp(_player) + "}", D.ADMIN_LOG_TYPE.Warp);
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
         index++;
      }

      D.adminLog("Player {" + _player.userId + "} Failed to warp since there is no warp nearby!", D.ADMIN_LOG_TYPE.Warp);

      // If no valid warp was found, let the player know so at least they're not stuck
      Target_OnWarpFailed("No valid warp nearby");
   }

   [Command]
   public void Cmd_TeleportToCurrentAreaSpawnLocation () {
      Vector2 spawnLocalPos = SpawnManager.self.getDefaultLocalPosition(_player.areaKey);

      if (spawnLocalPos == Vector2.zero) {
         return;
      }

      _player.transform.localPosition = spawnLocalPos;
      Target_ReceiveTeleportToCurrentAreaSpawnLocation(_player.connectionToClient, spawnLocalPos);
   }

   [TargetRpc]
   public void Target_ReceiveTeleportToCurrentAreaSpawnLocation (NetworkConnection connection, Vector3 pos) {
      Global.player.transform.localPosition = pos;
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

            AchievementManager.registerUserAchievement(_player, ActionType.PetAnimal);
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
      // If we are the local player, we don't do anything, the control was handled locally
      if (isLocalPlayer) {
         return;
      }

      TemporaryController con = AreaManager.self.getArea(_player.areaKey).getTemporaryControllerAtPosition(controllerLocalPosition);
      _player.noteWebBounce(con);
   }

   [Command]
   public void Cmd_RequestDBData (int requestId, string callInfoSerialized) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      NubisCallInfo callInfo = NubisCallInfo.deserialize(callInfoSerialized);
      string functionName = callInfo.functionName;
      object[] parameters = Array.ConvertAll(callInfo.arguments, _ => _.getTypedValue());

      // Make sure the function is allowed to be called by clients
      if (!NubisStatics.WhiteList.Contains(functionName)) {
         D.error(string.Format("Received a request to execute a forbidden function: {0}, from userId {1}.", functionName, _player.userId));
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         object result = "";

         if (string.Equals(functionName, "NubisDirect-getUserInventoryPage", StringComparison.OrdinalIgnoreCase)) {
            result = getUserInventoryPage((int) parameters[0],
               (Item.Category[]) parameters[1],
               (int) parameters[2],
               (int) parameters[3],
               (Item.DurabilityFilter) parameters[4]);
         } else {
            // Execute the DB query
            result = (object) typeof(DB_Main).GetMethod(functionName).Invoke(null, parameters);
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            NubisCallResult ncr = NubisCallResult.fromValue(result);
            string serializedResult = ncr.serialize();
            byte[] bytes = Encoding.ASCII.GetBytes(serializedResult);

            // Send the result to the client
            Target_ReceiveDBDataForClientDBRequest(_player.connectionToClient, requestId, bytes);
         });
      });
   }

   // Must be called from the background thread!
   [Server]
   public static string getUserInventoryPage (int userId, Item.Category[] categoryFilter,
      int currentPage, int itemsPerPage, Item.DurabilityFilter durabilityFilter) {

      if (itemsPerPage > 200) {
         D.debug("Requesting too many items per page.");
         return string.Empty;
      }

      // Process user info
      UserInfo newUserInfo = new UserInfo();
      try {
         newUserInfo = DB_Main.getUserInfoById(userId);
         if (newUserInfo == null) {
            D.debug("Something went wrong with Nubis Data Fetch!");
         }
      } catch {
         D.debug("Failed to deserialize user info! {" + userId + "}");
      }

      // Process guild info
      GuildInfo guildInfo = new GuildInfo();

      if (newUserInfo.guildId > 0) {
         string temp = DB_Main.getGuildInfoJSON(newUserInfo.guildId);
         try {
            guildInfo = JsonConvert.DeserializeObject<GuildInfo>(temp);
            if (guildInfo == null) {
               D.debug("Something went wrong with guild info data fetch for user {" + userId + "}");
            }
         } catch {
            D.debug("Failed to deserialize guild info! {" + userId + "}");
         }
      }

      // Process user equipped items
      string equippedItemContent = DB_Main.fetchEquippedItems(userId);
      EquippedItemData equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);
      Item equippedWeapon = equippedItemData.weaponItem;
      Item equippedArmor = equippedItemData.armorItem;
      Item equippedHat = equippedItemData.hatItem;

      // Create an empty item id filter
      int[] itemIdsToExclude = new int[0];

      int itemCount = DB_Main.userInventoryCount(userId, categoryFilter, itemIdsToExclude, true, durabilityFilter);

      string inventoryData = DB_Main.userInventory(userId, categoryFilter, itemIdsToExclude, true,
         currentPage, itemsPerPage, durabilityFilter);

      InventoryBundle bundle = new InventoryBundle {
         inventoryData = inventoryData,
         equippedHat = equippedHat,
         equippedArmor = equippedArmor,
         equippedWeapon = equippedWeapon,
         user = newUserInfo,
         guildInfo = guildInfo,
         totalItemCount = itemCount
      };

      return JsonConvert.SerializeObject(bundle, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
   }

   [TargetRpc]
   public void Target_ReceiveDBDataForClientDBRequest (NetworkConnection connection, int requestId, byte[] data) {
      ClientDBRequestManager.receiveRequestData(requestId, data);
   }

   [Command]
   public void Cmd_StoreServerLogForBugReport (int bugReportId) {
      byte[] serverLog = Encoding.ASCII.GetBytes(D.getLogString());

      // Send the server log to web tools
      BugReportManager.self.sendBugReportServerLog(bugReportId, serverLog);
   }

   [Server]
   public void sendNotification (Notification.Type notificationType) {
      Target_DisplayNotification(_player.connectionToClient, notificationType);
   }

   [TargetRpc]
   public void Target_DisplayNotification (NetworkConnection connection, Notification.Type notificationType) {
      NotificationManager.self.add(notificationType);
   }

   [TargetRpc]
   public void Target_DisplayNotificationForVoyageCompleted (NetworkConnection connection, Notification.Type notificationType) {
      NotificationManager.self.add(notificationType, () => VoyageManager.self.requestExitCompletedLeague(), false);
      VoyageManager.self.closeVoyageCompleteNotificationWhenLeavingArea();
   }

   [Command]
   public void Cmd_RequestUnlockedBiomeListFromServer () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<Biome.Type> unlockedBiomeList = DB_Main.getUnlockedBiomes(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // The forest biome is always accessible
            bool hasForestBiome = false;
            foreach (Biome.Type biome in unlockedBiomeList) {
               if (biome == Biome.Type.Forest) {
                  hasForestBiome = true;
                  break;
               }
            }

            if (!hasForestBiome) {
               unlockedBiomeList.Add(Biome.Type.Forest);
            }

            // Get the area key of the home towns in each biome
            string forestHomeTown = "";
            string desertHomeTown = "";
            string snowHomeTown = "";
            string pineHomeTown = "";
            string lavaHomeTown = "";
            string mushroomHomeTown = "";

            Area.homeTownForBiome.TryGetValue(Biome.Type.Forest, out forestHomeTown);
            Area.homeTownForBiome.TryGetValue(Biome.Type.Desert, out desertHomeTown);
            Area.homeTownForBiome.TryGetValue(Biome.Type.Snow, out snowHomeTown);
            Area.homeTownForBiome.TryGetValue(Biome.Type.Pine, out pineHomeTown);
            Area.homeTownForBiome.TryGetValue(Biome.Type.Lava, out lavaHomeTown);
            Area.homeTownForBiome.TryGetValue(Biome.Type.Mushroom, out mushroomHomeTown);

            // Send the result to the client
            Target_ReceiveUnlockedBiomeList(_player.connectionToClient, unlockedBiomeList.ToArray(), forestHomeTown,
               desertHomeTown, snowHomeTown, pineHomeTown, lavaHomeTown, mushroomHomeTown);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveUnlockedBiomeList (NetworkConnection connection, Biome.Type[] unlockedBiomeArray,
      string forestHomeTown, string desertHomeTown, string snowHomeTown, string pineHomeTown,
      string lavaHomeTown, string mushroomHomeTown) {

      List<Biome.Type> unlockedBiomeList = new List<Biome.Type>(unlockedBiomeArray);

      // Make sure the panel is showing
      PanelManager.self.linkIfNotShowing(Panel.Type.WorldMap);

      // Pass the data to the panel
      WorldMapPanel panel = (WorldMapPanel) PanelManager.self.get(Panel.Type.WorldMap);
      panel.updatePanelWithUnlockedBiomes(unlockedBiomeList, forestHomeTown, desertHomeTown,
         snowHomeTown, pineHomeTown, lavaHomeTown, mushroomHomeTown);
   }

   [Command]
   public void Cmd_RequestWarpToBiomeHomeTown (Biome.Type biome) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Don't allow users to warp if they are in battle
      if (_player.hasAttackers() && _player.isInCombat()) {
         int timeUntilCanLeave = (int) (NetEntity.IN_COMBAT_STATUS_DURATION - _player.getTimeSinceAttacked());
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "Cannot warp until out of combat for " + (int) NetEntity.IN_COMBAT_STATUS_DURATION + " seconds. \n(" + timeUntilCanLeave + " seconds left)");
         return;
      }

      // If
      Rpc_ReceiveRequestWarpToBiomeHomeTown(biome);
      return;
   }

   [ClientRpc]
   private void Rpc_ReceiveRequestWarpToBiomeHomeTown (Biome.Type biome) {
      if (isLocalPlayer) {
         if (!PanelManager.self.countdownScreen.isShowing()) {
            Area.homeTownForBiome.TryGetValue(biome, out string biomeHomeTownName);
            bool isHomeTownNameValid = !Util.isEmpty(biomeHomeTownName) && !Util.isEmpty(Area.getName(biomeHomeTownName));

            PanelManager.self.countdownScreen.customText.text = isHomeTownNameValid ? $"Warping to \"{Area.getName(biomeHomeTownName)}\" in:" : $"Warping in:";
            PanelManager.self.countdownScreen.seconds = 10;

            PanelManager.self.countdownScreen.onCountdownStep.RemoveAllListeners();
            PanelManager.self.countdownScreen.onCountdownStep.AddListener(() => {
               if (_player.hasAttackers() && _player.isInCombat()) {
                  _player.rpc.Cmd_CancelWarpToBiomeHomeTown(biome);
                  PanelManager.self.countdownScreen.hide();
               }
            });

            PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
            PanelManager.self.countdownScreen.onCountdownEndEvent.AddListener(() => {
               _player.rpc.Cmd_ExecuteWarpToBiomeHomeTown(biome);
               PanelManager.self.countdownScreen.hide();
            });

            PanelManager.self.countdownScreen.cancelButton.onClick.RemoveAllListeners();
            PanelManager.self.countdownScreen.cancelButton.onClick.AddListener(() => {
               _player.rpc.Cmd_CancelWarpToBiomeHomeTown(biome);
               PanelManager.self.countdownScreen.hide();
            });

            PanelManager.self.countdownScreen.show();
         }
      }

      _player.toggleWarpInProgressEffect(show: true);
   }

   [Command]
   public void Cmd_CancelWarpToBiomeHomeTown (Biome.Type biome) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      Rpc_ReceiveCancelWarpToBiomeHomeTown(biome);
   }

   [ClientRpc]
   private void Rpc_ReceiveCancelWarpToBiomeHomeTown (Biome.Type biome) {
      _player.toggleWarpInProgressEffect(show: false);
   }

   [Command]
   public void Cmd_ExecuteWarpToBiomeHomeTown (Biome.Type biome) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Don't allow users to warp if they are in battle
      if (_player.hasAttackers() && _player.isInCombat()) {
         int timeUntilCanLeave = (int) (NetEntity.IN_COMBAT_STATUS_DURATION - _player.getTimeSinceAttacked());
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "Cannot warp until out of combat for " + (int) NetEntity.IN_COMBAT_STATUS_DURATION + " seconds. \n(" + timeUntilCanLeave + " seconds left)");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Check if the user has unlocked the biome
         bool isBiomeUnlocked = DB_Main.isBiomeUnlockedForUser(_player.userId, biome);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (!isBiomeUnlocked) {
               sendError("The " + biome.ToString() + " biome is locked!");
            } else {
               if (Area.homeTownForBiome.TryGetValue(biome, out string homeTown)) {
                  if (Area.dockSpawnForBiome.TryGetValue(biome, out string dockSpawn)) {
                     _player.spawnInNewMap(homeTown, dockSpawn, Direction.South);
                  } else {
                     _player.spawnInNewMap(homeTown);
                  }
               } else {
                  sendError("Could not find the home town for the biome " + biome.ToString() + "!");
                  _player.spawnInNewMap(Area.STARTING_TOWN);
               }
            }
         });
      });
   }

   [TargetRpc]
   public void Target_JoinTeamCombat (NetworkConnection connection, uint enemyId, float delay) {
      StartCoroutine(CO_DelayJoinTeamCombat(enemyId, delay));
   }

   private IEnumerator CO_DelayJoinTeamCombat (uint enemyId, float delay) {
      yield return new WaitForSeconds(delay);
      Global.player.rpc.Cmd_StartNewBattle(enemyId, Battle.TeamType.Attackers, false, false);
   }

   [TargetRpc]
   public void Target_UpdateLandPowerups (NetworkConnection connection, List<LandPowerupData> landPowerups) {
      PowerupPanel.self.updateLandPowerups(landPowerups);
   }

   [TargetRpc]
   public void Target_UpdatePowerups (NetworkConnection connection, List<Powerup> powerups) {
      PowerupPanel.self.updatePowerups(powerups, _player);
   }

   [TargetRpc]
   public void Target_AddPowerup (NetworkConnection connection, Powerup newPowerup) {
      PowerupManager.self.addPowerupClient(newPowerup);
   }

   [TargetRpc]
   public void Target_RemovePowerup (NetworkConnection connection, Powerup newPowerup) {
      PowerupManager.self.removePowerupClient(newPowerup);
   }

   [TargetRpc]
   public void Target_AddLandPowerup (NetworkConnection connection, LandPowerupData newPowerup) {
      PowerupPanel.self.addLandPowerup(new LandPowerupData {
         counter = newPowerup.counter,
         expiryType = newPowerup.expiryType,
         landPowerupType = newPowerup.landPowerupType,
         userId = newPowerup.userId,
         value = newPowerup.value
      });

      TutorialManager3.self.tryCompletingStep(TutorialTrigger.Receive_Land_Powerup);
   }

   [TargetRpc]
   public void Target_RemoveLandPowerup (NetworkConnection connection, LandPowerupData newPowerup) {
      PowerupPanel.self.removePowerup(new LandPowerupData {
         counter = newPowerup.counter,
         expiryType = newPowerup.expiryType,
         landPowerupType = newPowerup.landPowerupType,
         userId = newPowerup.userId,
         value = newPowerup.value
      });
   }

   [Command]
   public void Cmd_RequestPowerupUpdate (LandPowerupType powerupType, LandPowerupExpiryType expiryType, int count) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // TODO: Add logic here that decrements powerup validity such as expiring powerup per boss kill
      // LandPowerupManager.self.updateNewPowerupData(_player.userId, powerupType, expiryType, )
   }

   [Command]
   public void Cmd_SetAdminGameSettings (AdminGameSettings settings) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (!_player.isAdmin()) {
         return;
      }

      AdminGameSettingsManager.self.updateAndStoreSettings(settings);
   }

   [Server]
   public void setAdminBattleParameters () {
      Target_ReceiveAdminBattleParameters(AdminGameSettingsManager.self.settings);
   }

   [TargetRpc]
   public void Target_ReceiveAdminBattleParameters (AdminGameSettings settings) {
      AdminGameSettingsManager.self.updateLocalSettings(settings);
   }

   [TargetRpc]
   public void Target_DisplayServerMessage (string message) {
      ChatPanel.self.addChatInfo(new ChatInfo(0, message, DateTime.Now, ChatInfo.Type.System));
   }

   [Command]
   public void Cmd_ReceiveClientPing (float ping) {
      LagMonitorManager.self.receiveClientPing(_player.userId, ping);
   }

   [Command]
   public void Cmd_RequestPvpArenaListFromServer () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      List<Voyage> voyages = VoyageManager.self.getAllPvpInstances();
      Target_ReceivePvpArenaList(_player.connectionToClient, voyages.ToArray());
   }

   [TargetRpc]
   public void Target_ReceivePvpArenaList (NetworkConnection connection, Voyage[] voyageArray) {
      List<Voyage> voyageList = new List<Voyage>(voyageArray);

      // Make sure the panel is showing
      PanelManager.self.linkIfNotShowing(Panel.Type.PvpArena);

      // Pass the data to the panel
      PvpArenaPanel panel = (PvpArenaPanel) PanelManager.self.get(Panel.Type.PvpArena);
      panel.receivePvpArenasFromServer(voyageList);
   }

   [Command]
   public void Cmd_RequestPvpArenaInfoFromServer (int voyageId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      if (VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage) && voyage.isPvP) {
         Target_ReceivePvpArenaInfo(_player.connectionToClient, voyage);
      } else {
         sendError("This pvp arena does not exist anymore!");
      }
   }

   [TargetRpc]
   public void Target_ReceivePvpArenaInfo (NetworkConnection connection, Voyage voyage) {
      // Pass the data to the panel
      PvpArenaPanel panel = (PvpArenaPanel) PanelManager.self.get(Panel.Type.PvpArena);
      panel.pvpArenaInfoPanel.updatePanelWithPvpArena(voyage);
   }

   [Command]
   public void Cmd_JoinPvpArena (int voyageId, PvpTeamType team) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Make sure the game instance (voyage) exists
      if (VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage)) {
         // Send the join request to the server hosting the game
         ServerNetworkingManager.self.joinPvpGame(voyageId, _player.userId, _player.entityName, team);
      }
   }

   [Server]
   public void broadcastPvPKill (NetEntity attackerEntity, NetEntity targetEntity) {
      if (attackerEntity == null) {
         return;
      }

      Instance instance = attackerEntity.getInstance();
      if (instance == null || !instance.isPvP) {
         return;
      }

      // Only Player vs Player kills are announced
      if (!attackerEntity.isPlayerShip() || !targetEntity.isPlayerShip()) {
         return;
      }

      int attackerSilverRank = GameStatsManager.self.getSilverRank(attackerEntity.userId);

      List<PlayerShipEntity> entities = instance.getPlayerShipEntities();
      foreach (NetEntity entity in entities) {
         Target_ReceiveBroadcastPvPKill(entity.connectionToClient, attackerEntity.netId, targetEntity.netId, attackerSilverRank);
      }
   }

   [TargetRpc]
   public void Target_ReceiveBroadcastPvPKill (NetworkConnection connection, uint attackerEntityNetId, uint targetEntityNetId, int attackerSilverRank) {
      NetEntity attackerEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerEntityNetId);
      NetEntity targetEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(targetEntityNetId);

      string attackerDisplayName = $"{attackerEntity.entityName}";
      string targetDisplayName = $"{targetEntity.entityName}";
      PvpStatusPanel.self.addKillEvent(attackerDisplayName, PvpGame.getColorForTeam(attackerEntity.pvpTeam), targetDisplayName, PvpGame.getColorForTeam(targetEntity.pvpTeam));

      // If the attacker's rank is high enough highlight the announcement
      PvpAnnouncementHolder.self.addKillAnnouncement(attackerEntity, targetEntity, attackerSilverRank > 2, attackerEntity.pvpTeam);
   }

   [TargetRpc]
   public void Target_ReceiveBroadcastPvpAnnouncement (string announcementText, PvpAnnouncement.Priority priority) {
      PvpAnnouncementHolder.self.addAnnouncement(announcementText, priority);
      ChatPanel.self.addChatInfo(new ChatInfo(0, announcementText, DateTime.Now, ChatInfo.Type.System));
   }

   [Server]
   public void broadcastPvpTowerDestruction (NetEntity attackerEntity, PvpTower targetEntity) {
      if (attackerEntity == null) {
         return;
      }

      Instance instance = attackerEntity.getInstance();
      if (instance == null || !instance.isPvP) {
         return;
      }

      int attackerSilverRank = GameStatsManager.self.getSilverRank(attackerEntity.userId);

      List<PlayerShipEntity> entities = instance.getPlayerShipEntities();
      foreach (NetEntity entity in entities) {
         Target_ReceiveBroadcastPvpTowerDestruction(entity.connectionToClient, attackerEntity.netId, targetEntity.netId, attackerSilverRank);
      }
   }

   [TargetRpc]
   public void Target_ReceiveBroadcastPvpTowerDestruction (NetworkConnection connection, uint attackerEntityNetId, uint targetEntityNetId, int attackerSilverRank) {
      NetEntity attackerEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerEntityNetId);
      NetEntity targetEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(targetEntityNetId);

      PvpTower targetTower = (PvpTower) targetEntity;
      PvpAnnouncementHolder.self.addTowerDestructionAnnouncement(attackerEntity, targetTower, attackerSilverRank > 2, attackerEntity.pvpTeam);
   }

   [Server]
   public void ResetPvpSilverPanel () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      Target_ResetPvpSilverPanel(_player.connectionToClient, GameStatsManager.self.getSilverAmount(_player.userId));
   }

   [Command]
   public void Cmd_RequestResetPvpSilverPanel () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      Target_ResetPvpSilverPanel(_player.connectionToClient, GameStatsManager.self.getSilverAmount(_player.userId));
   }

   [TargetRpc]
   private void Target_ResetPvpSilverPanel (NetworkConnection conn, int silverCount) {
      PvpStatusPanel.self.reset(silverCount);
   }

   [Command]
   public void Cmd_RequestPlayersCount () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int totalPlayersCount = DB_Main.getTotalPlayersCount();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceivePlayersCount(_player.connectionToClient, totalPlayersCount);
         });
      });
   }

   [TargetRpc]
   private void Target_ReceivePlayersCount (NetworkConnection conn, int playersCount) {
      OptionsPanel.self?.onPlayersCountReceived(playersCount);
   }

   [Command]
   public void Cmd_RequestRemoteSettings (string[] settingNames) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         RemoteSettingCollection collection = DB_Main.getRemoteSettings(settingNames);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveRemoteSettings(_player.connectionToClient, collection);
         });
      });
   }

   [TargetRpc]
   private void Target_ReceiveRemoteSettings (NetworkConnection conn, RemoteSettingCollection collection) {
      AdminPanel.self.onRemoteSettingsReceived(collection);
   }

   [Command]
   public void Cmd_SetRemoteSettings (RemoteSettingCollection collection) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         bool success = DB_Main.setRemoteSettings(collection);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_SetRemoteSettings(_player.connectionToClient, success);
         });
      });
   }

   [TargetRpc]
   private void Target_SetRemoteSettings (NetworkConnection conn, bool success) {
      AdminPanel.self.onSetRemoteSettings(success);
   }

   [Command]
   public void Cmd_RequestKickAllNonAdminUsers (string message) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      ServerNetworkingManager.self.forceDisconnectAllNonAdminUsers(_player.userId, message);
   }

   [Command]
   public void Cmd_RequestNetworkOverview () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      // Tell the client how many servers does he have to wait for
      Target_ReceiveServerCount(ServerNetworkingManager.self.servers.Count);

      ServerNetworkingManager.self.server.requestServerOverviews(this);
   }

   [TargetRpc]
   public void Target_ReceiveServerCount (int serverCount) {
      if (AdminPanel.self != null) {
         AdminPanel.self.receiveServerCount(serverCount);
      }
   }

   [TargetRpc]
   public void Target_ReceiveServerOverview (ServerOverview overview) {
      if (AdminPanel.self != null) {
         AdminPanel.self.receiveServerOverview(overview);
      }
   }

   [Command]
   public void Cmd_RequestPlayersCountMetrics () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Make sure this is an admin
      if (!_player.isAdmin()) {
         D.warning("Received admin command from non-admin!");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         MetricCollection metrics = DB_Main.getGameMetrics(MetricsManager.MetricNames.PLAYERS_COUNT);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceivePlayersCountMetrics(_player.connectionToClient, metrics);
         });
      });
   }

   [TargetRpc]
   private void Target_ReceivePlayersCountMetrics (NetworkConnection conn, MetricCollection metrics) {
      //AdminPanel.self.onPlayersCountMetricsReceived(metrics);
   }

   [TargetRpc]
   public void Target_UpdatePvpScore (NetworkConnection conn, int newScoreValue, PvpTeamType teamType) {
      PvpStatPanel.self.updateScoreForTeam(newScoreValue, teamType);
      PvpScoreIndicator.self.updateScore(teamType, newScoreValue);
   }

   [TargetRpc]
   public void Target_ShowPvpPostGameScreen (NetworkConnection conn, bool wonGame, PvpTeamType winningTeam, int gemReward) {
      PvpStatPanel.self.updateDisplayMode(Global.player.areaKey, true, wonGame, winningTeam, gemReward);
      BottomBar.self.enablePvpStatPanel();
   }

   [TargetRpc]
   public void Target_SetGameStartTime (NetworkConnection conn, float gameStartTime) {
      PvpStatPanel.self.setGameStartTime(gameStartTime);
   }

   [Command]
   public void Cmd_RequestPvpRespawnTimeout () {
      float timeout = PvpGame.RESPAWN_TIMEOUT;

      if (PvpManager.self == null) {
         return;
      }

      PvpGame pvpGame = PvpManager.self.getGameWithPlayer(_player);

      if (pvpGame == null) {
         return;
      }

      timeout = pvpGame.computeRespawnTimeoutFor(_player.userId);

      Target_ReceivePvpRespawnTimeout(timeout);
   }

   [TargetRpc]
   public void Target_ReceivePvpRespawnTimeout (float timeout) {
      RespawnScreen.self.onRespawnTimeoutReceived(timeout);
   }

   [Command]
   public void Cmd_RequestUserInfoForCharacterInfoPanelFromServer (int userId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserObjects userObjects = DB_Main.getUserObjects(userId);
         Jobs jobXP = DB_Main.getJobXP(userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the result to the client
            Target_ReceiveUserInfoForCharacterInfoPanel(_player.connectionToClient, userObjects, jobXP);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveUserInfoForCharacterInfoPanel (NetworkConnection connection, UserObjects userObjects, Jobs jobXP) {

      // Make sure the panel is showing
      PanelManager.self.linkIfNotShowing(Panel.Type.CharacterInfo);

      // Pass the data to the panel
      CharacterInfoPanel panel = (CharacterInfoPanel) PanelManager.self.get(Panel.Type.CharacterInfo);
      panel.receiveUserObjectsFromServer(userObjects, jobXP);
   }

   [Command]
   public void Cmd_RequestPvpGameFactions (int gameInstanceId) {
      PvpGame game = PvpManager.self.getGameWithInstance(gameInstanceId);
      if (game) {
         Target_SendPvpGameFaction(_player.connectionToClient, PvpTeamType.A, game.getFactionForTeam(PvpTeamType.A));
         Target_SendPvpGameFaction(_player.connectionToClient, PvpTeamType.B, game.getFactionForTeam(PvpTeamType.B));
      }
   }

   [TargetRpc]
   public void Target_SendPvpGameFaction (NetworkConnection connection, PvpTeamType teamType, Faction.Type factionType) {
      PvpStatPanel.self.assignFactionToTeam(teamType, factionType);
   }

   [TargetRpc]
   public void Target_SetPvpInstructionsPanelVisibility (NetworkConnection connection, bool isVisible) {
      if (isVisible) {
         PvpInstructionsPanel.self.show();
      } else {
         PvpInstructionsPanel.self.hide();
      }
   }

   [TargetRpc]
   public void Target_UpdatePvpInstructionsPanelPlayers (NetworkConnection connection, List<int> playerUserIds) {
      PvpInstructionsPanel.self.updatePlayers(playerUserIds);
   }

   [TargetRpc]
   public void Target_UpdatePvpInstructionsPanelGameStatusMessage (NetworkConnection connection, string newMessage) {
      PvpInstructionsPanel.self.updateGameStatusMessage(newMessage);
   }

   [TargetRpc]
   public void Target_InitPvpInstructionsPanel (NetworkConnection connection, int instanceId, List<Faction.Type> teamFactions) {
      PvpInstructionsPanel.self.init(teamFactions, instanceId);
   }

   [Server]
   private bool Bkg_IsItemSoulBound (Item item) {
      return SoulBindingManager.Bkg_IsItemSoulBound(item);
   }

   [Server]
   private bool Bkg_ShouldBeSoulBound (Item item, bool isBeingEquipped) {
      return SoulBindingManager.Bkg_ShouldBeSoulBound(item, isBeingEquipped);
   }

   [ClientRpc]
   public void Rpc_ShowWhirlpoolEffect (Vector3 position, float effectRadius) {
      WhirlpoolEffect whirlpoolEffect = Instantiate(PrefabsManager.self.whirlpoolEffectPrefab, position, Quaternion.identity);
      whirlpoolEffect.init(effectRadius);
   }

   [ClientRpc]
   public void Rpc_ShowKnockbackEffect (Vector3 position, float effectRadius) {
      KnockbackEffect knockbackEffect = Instantiate(PrefabsManager.self.knockbackEffectPrefab, position, Quaternion.identity);
      knockbackEffect.init(effectRadius);
   }

   [TargetRpc]
   public void Target_ResetPvpScoreIndicator (NetworkConnection conn, bool show) {
      PvpScoreIndicator.self.reset();
      PvpScoreIndicator.self.toggle(show);
   }

   [TargetRpc]
   public void Target_ReceiveEmoteMessage (ChatInfo chatInfo) {
      chatInfo.messageType = ChatInfo.Type.Emote;
      ChatPanel.self.addChatInfo(chatInfo);
   }

   #region User Search

   [Command]
   public void Cmd_SearchUser (UserSearchInfo searchInfo) {
      List<UserSearchResult> results = new List<UserSearchResult>();
      UserSearchResultCollection resultsCollection = new UserSearchResultCollection();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         if (searchInfo.filter == UserSearchInfo.FilteringMode.Name) {
            UserInfo userInfo = DB_Main.getUserInfo(searchInfo.input);

            if (userInfo != null) {
               bool isFriend = DB_Main.getFriendshipInfo(_player.userId, userInfo.userId) != null;

               UserSearchResult result = new UserSearchResult {
                  name = userInfo.username,
                  level = LevelUtil.levelForXp(userInfo.XP),
                  userId = _player.isAdmin() ? userInfo.userId : 0,
                  area = _player.isAdmin() ? userInfo.areaKey : "",
                  biome = AreaManager.self.getDefaultBiome(userInfo.areaKey),
                  lastTimeOnline = userInfo.lastLoginTime,
                  isOnline = ServerNetworkingManager.self.isUserOnline(userInfo.userId),
                  isSameServer = EntityManager.self.getEntity(userInfo.userId) != null,
                  isFriend = isFriend
               };

               results.Add(result);
            }
         }

         if (searchInfo.filter == UserSearchInfo.FilteringMode.Biome) {
            Biome.Type requestedBiome = Biome.fromName(searchInfo.input);

            if (requestedBiome != Biome.Type.None) {
               if (UserInfosCache.needsUpdate()) {
                  List<int> onlineUserIdsAll = ServerNetworkingManager.self.getAllOnlineUsers();
                  Dictionary<int, UserInfo> userInfoRegistry = DB_Main.getUserInfosByIds(onlineUserIdsAll);
                  UserInfosCache.updateCache(userInfoRegistry.Values);
               }

               IEnumerable<UserInfo> userInfos = UserInfosCache.getCache();
               foreach (UserInfo userInfo in userInfos) {
                  if (userInfo == null) {
                     continue;
                  }

                  Biome.Type userInfoBiome = AreaManager.self.getDefaultBiome(userInfo.areaKey);
                  if (userInfoBiome != requestedBiome) {
                     continue;
                  }

                  UserSearchResult result = new UserSearchResult {
                     name = userInfo.username,
                     level = LevelUtil.levelForXp(userInfo.XP),
                     userId = _player.isAdmin() ? userInfo.userId : 0,
                     area = _player.isAdmin() ? userInfo.areaKey : "",
                     biome = userInfoBiome,
                     lastTimeOnline = userInfo.lastLoginTime,
                     isOnline = true,
                     isSameServer = EntityManager.self.getEntity(userInfo.userId) != null,
                  };

                  results.Add(result);
               }
            }
         }

         if (searchInfo.filter == UserSearchInfo.FilteringMode.Level) {
            if (int.TryParse(searchInfo.input, out int requestedLevel)) {
               if (UserInfosCache.needsUpdate()) {
                  List<int> onlineUserIdsAll = ServerNetworkingManager.self.getAllOnlineUsers();
                  Dictionary<int, UserInfo> userInfoRegistry = DB_Main.getUserInfosByIds(onlineUserIdsAll);
                  UserInfosCache.updateCache(userInfoRegistry.Values);
               }

               IEnumerable<UserInfo> userInfos = UserInfosCache.getCache();
               foreach (UserInfo userInfo in userInfos) {
                  if (userInfo == null) {
                     continue;
                  }

                  int userInfoLevel = LevelUtil.levelForXp(userInfo.XP);
                  if (userInfoLevel != requestedLevel) {
                     continue;
                  }

                  UserSearchResult result = new UserSearchResult {
                     name = userInfo.username,
                     level = userInfoLevel,
                     userId = _player.isAdmin() ? userInfo.userId : 0,
                     area = _player.isAdmin() ? userInfo.areaKey : "",
                     biome = AreaManager.self.getDefaultBiome(userInfo.areaKey),
                     lastTimeOnline = userInfo.lastLoginTime,
                     isOnline = true,
                     isSameServer = EntityManager.self.getEntity(userInfo.userId) != null,
                  };

                  results.Add(result);
               }
            }
         }

         resultsCollection.searchInfo = searchInfo;
         resultsCollection.results = results.ToArray();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveUserSearchResults(_player.connectionToClient, resultsCollection);
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveUserSearchResults (NetworkConnection connection, UserSearchResultCollection resultCollection) {
      if (resultCollection == null || resultCollection.results == null || resultCollection.results.Length == 0) {
         ChatManager.self.addChat($"Search complete! No users found...", ChatInfo.Type.System);
         return;
      }

      string itemsStr = resultCollection.results.Length == 1 ? "user" : "users";
      ChatManager.self.addChat($"Search complete! {resultCollection.results.Length} {itemsStr} found:", ChatInfo.Type.System);

      if (resultCollection.results.Length >= 1) {
         FriendListPanel friendListPanel = PanelManager.self.get<FriendListPanel>(Panel.Type.FriendList);

         if (friendListPanel != null && !friendListPanel.isShowing()) {
            friendListPanel.showSearchResults(resultCollection);
         }
      } else {
         int counter = 1;

         // If the results list is small enough, show them in chat, otherwise show them in a specialized GUI panel
         foreach (UserSearchResult result in resultCollection.results) {
            bool isCurrentPlayer = Util.areStringsEqual(result.name, _player.entityName);
            string biome = Biome.getName(result.biome);
            string biomeStr = string.IsNullOrWhiteSpace(biome) ? "" : $" in {biome}";
            string areaAdminStr = (Util.isEmpty(result.area) ? "Somewhere" : $"{Area.getName(result.area)} ({result.area})");
            string areaStr = (Util.isEmpty(result.area) ? "Somewhere" : $"{Area.getName(result.area)}");
            string sameServerStr = (result.isSameServer && result.area == _player.areaKey && !isCurrentPlayer) ? "*" : "";
            string onlineStr = "Status: " + (result.isOnline ? "Online" : "Offline");
            string lastOnlineStr = !result.isOnline ? $", Last Seen: {result.lastTimeOnline.ToLocalTime()}" : "";
            string userIdStr = Global.player.isAdmin() ? $", UID: {result.userId}" : "";
            string computedAreaStr = "Location: " + (_player.isAdmin() ? areaAdminStr : areaStr);

            string msg = $"[{counter}/{resultCollection.results.Length}] {result.name}{userIdStr}, Lv: {result.level}, {computedAreaStr}{sameServerStr}{biomeStr}, {onlineStr}{lastOnlineStr}";
            ChatManager.self.addChat(msg, ChatInfo.Type.System);
            counter++;
         }
      }
   }

   #endregion

   #region Voyage Rating

   [Command]
   public void Cmd_RequestResetVoyageRatingPoints () {
      int points = VoyageRatingManager.getPointsMax();

      if (GameStatsManager.self.isUserRegistered(_player.userId)) {
         GameStats stats = GameStatsManager.self.getStatsForUser(_player.userId);
         points = stats.voyageRatingPoints;
      }

      Target_OnRequestResetVoyageRatingPoints(_player.connectionToClient, points);
   }

   [TargetRpc]
   public void Target_OnRequestResetVoyageRatingPoints (NetworkConnection connection, int points) {
      VoyageRatingIndicator.self.setRatingPoints(points);
   }

   [Server]
   public void assignVoyageRatingPoints (int points) {
      if (!GameStatsManager.self.isUserRegistered(_player.userId)) {
         return;
      }

      GameStats prevStats = GameStatsManager.self.getStatsForUser(_player.userId);
      int prevRatingPoints = prevStats.voyageRatingPoints;

      GameStatsManager.self.addVoyageRatingPoints(_player.userId, points);

      GameStats newStats = GameStatsManager.self.getStatsForUser(_player.userId);
      int newRatingPoints = newStats.voyageRatingPoints;

      if (newRatingPoints == prevRatingPoints) {
         return;
      }

      Target_OnVoyageRatingPointsAssigned(_player.connectionToClient, points, newRatingPoints, prevRatingPoints);
   }

   [TargetRpc]
   private void Target_OnVoyageRatingPointsAssigned (NetworkConnection connection, int pointsAssigned, int newRatingPoints, int prevRatingPoints) {
      VoyageRatingIndicator.self.setRatingPoints(newRatingPoints);
      int newRatingLevel = VoyageRatingManager.computeRatingLevelFromPoints(newRatingPoints);
      int prevRatingLevel = VoyageRatingManager.computeRatingLevelFromPoints(prevRatingPoints);
      D.debug($"VoyageRatingManager: new rating state for player {Global.player.userId}. earned points:{pointsAssigned}, current points:{newRatingPoints}, current rating: {newRatingLevel}, prev rating:{prevRatingLevel}");
   }

   #endregion

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   #endregion
}
