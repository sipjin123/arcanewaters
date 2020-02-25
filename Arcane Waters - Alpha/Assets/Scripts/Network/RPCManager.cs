using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using Crosstales.BWF.Manager;
using System;
using System.Text;
using BackgroundTool;

public class RPCManager : NetworkBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      _player = GetComponent<NetEntity>();
   }

   [TargetRpc]
   public void Target_ReceiveNPCsForCurrentArea (NetworkConnection connection, string[] npcDataRaw) {
      // Deserialize data
      NPCData[] npcDataList = Util.unserialize<NPCData>(npcDataRaw).ToArray();

      // Cache to npc manager 
      NPCManager.self.initializeNPCClientData(npcDataList);
   }

   [TargetRpc]
   public void Target_ReceiveEquipmentData (NetworkConnection connection, string[] rawData, EquipmentToolManager.EquipmentType equipType) {
      switch (equipType) {
         case EquipmentToolManager.EquipmentType.Weapon:
            WeaponStatData[] weaponDataList = Util.unserialize<WeaponStatData>(rawData).ToArray();
            EquipmentXMLManager.self.receiveWeaponDataFromServer(new List<WeaponStatData>(weaponDataList));
            break;
         case EquipmentToolManager.EquipmentType.Armor:
            ArmorStatData[] armorDataList = Util.unserialize<ArmorStatData>(rawData).ToArray();
            EquipmentXMLManager.self.receiveArmorDataFromServer(new List<ArmorStatData>(armorDataList));
            break;
         case EquipmentToolManager.EquipmentType.Helm:
            HelmStatData[] helmDataList = Util.unserialize<HelmStatData>(rawData).ToArray();
            EquipmentXMLManager.self.receiveHelmDataFromServer(new List<HelmStatData>(helmDataList));
            break;
      }
   }

   [TargetRpc]
   public void Target_ReceiveAllShipInfo (NetworkConnection connection, string[] rawShipInfo) {
      // Deserialize data
      ShipData[] shipDataList = Util.unserialize<ShipData>(rawShipInfo).ToArray();

      // Cache to ship data manager 
      ShipDataManager.self.receiveShipDataFromServer(new List<ShipData>(shipDataList));
   }

   [TargetRpc]
   public void Target_ReceiveAllShipAbilityInfo (NetworkConnection connection, string[] rawShipAbilityInfo) {
      // Deserialize data
      ShipAbilityData[] shipDataList = Util.unserialize<ShipAbilityData>(rawShipAbilityInfo).ToArray();

      // Cache to ship data manager 
      ShipAbilityManager.self.receiveDataFromServer(shipDataList);
   }

   [TargetRpc]
   public void Target_ReceiveAllSeaMonsterInfo (NetworkConnection connection, string[] rawInfo) {
      // Deserialize data
      SeaMonsterEntityData[] seaMonsterList = Util.unserialize<SeaMonsterEntityData>(rawInfo).ToArray();

      // Cache to sea monster manager 
      SeaMonsterManager.self.receiveListFromServer(seaMonsterList);
   }

   [TargetRpc]
   public void Target_GrantAdminAccess (NetworkConnection connection, bool isEnabled) {
      OptionsPanel panel = (OptionsPanel) PanelManager.self.get(Panel.Type.Options);
      panel.enableAdminButtons(isEnabled);
   }

   [TargetRpc]
   public void Target_ReceiveTutorialData (NetworkConnection connection, string[] rawInfo) {
      // Deserialize data
      TutorialData[] tutorialDataList = Util.unserialize<TutorialData>(rawInfo).ToArray();

      // Cache to tutorial manager 
      TutorialManager.self.receiveListFromServer(tutorialDataList);
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
         PanelManager.self.pushPanel(panel.type);
      }
   }

   [Command]
   public void Cmd_InteractAnimation (Anim.Type animType) {
      Rpc_InteractAnimation(animType);
   }

   [ClientRpc]
   public void Rpc_InteractAnimation (Anim.Type animType) {
      _player.requestAnimationPlay(animType);
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

   #region NPC QUEST

   [TargetRpc]
   public void Target_ReceiveNPCQuestNode (NetworkConnection connection, int questId,
      QuestNode questNode, int friendshipLevel, bool areObjectivesCompleted, int[] objectivesProgress) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Pass the data to the panel
      panel.updatePanelWithQuestNode(friendshipLevel, questId, questNode, areObjectivesCompleted,
         objectivesProgress);

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }
   }

   [TargetRpc]
   public void Target_ReceiveNPCQuestList (NetworkConnection connection, int npcId,
      string npcName, Faction.Type faction, Specialty.Type specialty, int friendshipLevel,
      string greetingText, bool canOfferGift, bool hasTradeGossipDialogue, bool hasGoodbyeDialogue,
      Quest[] quests) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Pass the data to the panel
      panel.updatePanelWithQuestSelection(npcId, npcName, faction, specialty, friendshipLevel, greetingText,
         canOfferGift, hasTradeGossipDialogue, hasGoodbyeDialogue, quests);

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }
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
         PanelManager.self.pushPanel(panel.type);
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
         PanelManager.self.pushPanel(panel.type);
      }
   }

   [TargetRpc]
   public void Target_EndNPCDialogue (NetworkConnection connection) {
      // Get the NPC panel
      NPCPanel panel = (NPCPanel) PanelManager.self.get(Panel.Type.NPC_Panel);

      // Close the panel
      if (panel.isShowing()) {
         PanelManager.self.popPanel();
      }
   }

   #endregion

   [TargetRpc]
   public void Target_ReceiveShopItems (NetworkConnection connection, int gold, string[] itemArray, string greetingText) {
      // TODO: Determine if casting is still necessary
      List<Item> newCastedItems = Util.unserialize<Item>(itemArray); //Item.unserializeAndCast(itemArray)

      AdventureShopScreen.self.updateGreetingText(greetingText);
      AdventureShopScreen.self.updatePanelWithItems(gold, newCastedItems);
   }

   [TargetRpc]
   public void Target_ReceiveOffers (NetworkConnection connection, int gold, CropOffer[] offerArray, long lastCropRegenTime, string greetingText) {
      MerchantScreen.self.updatePanelWithOffers(gold, new List<CropOffer>(offerArray), lastCropRegenTime, greetingText);
   }

   [TargetRpc]
   public void Target_ReceiveShipyard (NetworkConnection connection, int gold, string[] shipArray, string greetingText) {
      // Translate Abilities
      List<ShipInfo> newShipInfo = Util.unserialize<ShipInfo>(shipArray);
      foreach (ShipInfo info in newShipInfo) {
         info.shipAbilities = Util.xmlLoad<ShipAbilityInfo>(info.shipAbilityXML);
      }

      ShipyardScreen.self.updatePanelWithShips(gold, newShipInfo, greetingText);
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
         D.warning("Player already opened this chest!");
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
      AchievementManager.registerUserAchievement(_player.userId, ActionType.OpenedLootBag);

      // Send it to the specific player that opened it
      Target_OpenChest(_player.connectionToClient, item, chest.id);
   }

   [Server]
   public void spawnBattlerMonsterChest (int instanceID, Vector3 position, int enemyID) {
      Instance currentInstance = InstanceManager.self.getInstance(instanceID);
      TreasureManager.self.createBattlerMonsterChest(currentInstance, position, enemyID);
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
      if (chest.chestType == ChestSpawnType.Site) {
         chest.chestOpeningAnimation.enabled = true;
      } else {
         chest.spriteRenderer.sprite = chest.openedChestSprite;
         chest.chestOpeningAnimation.enabled = false;
      }
      chest.StartCoroutine(chest.CO_CreatingFloatingIcon(item));

      // Play some sounds
      SoundManager.create3dSound("Door_open", Global.player.transform.position);
      SoundManager.create3dSound("tutorial_step", Global.player.transform.position);

      // Show a confirmation in chat
      string msg = string.Format("You found one <color=red>{0}</color>!", item.getName());
      ChatManager.self.addChat(msg, ChatInfo.Type.System);

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
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass them along to the Trade History panel
      panel.updatePanelWithTrades(tradeList, pageIndex, totalTradeCount);
   }

   [TargetRpc]
   public void Target_ReceiveLeaderBoards (NetworkConnection connection, LeaderBoardsManager.Period period,
      Faction.Type boardFaction, double secondsLeftUntilRecalculation, LeaderBoardInfo[] farmingEntries,
      LeaderBoardInfo[] sailingEntries, LeaderBoardInfo[] exploringEntries, LeaderBoardInfo[] tradingEntries,
      LeaderBoardInfo[] craftingEntries, LeaderBoardInfo[] miningEntries) {

      // Make sure the panel is showing
      LeaderBoardsPanel panel = (LeaderBoardsPanel) PanelManager.self.get(Panel.Type.LeaderBoards);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass them along to the Leader Boards panel
      panel.updatePanelWithLeaderBoardEntries(period, boardFaction, secondsLeftUntilRecalculation, farmingEntries, sailingEntries,
         exploringEntries, tradingEntries, craftingEntries, miningEntries);
   }

   [TargetRpc]
   public void Target_ReceiveInventoryItemsForItemSelection (NetworkConnection connection, InventoryMessage msg) {
      // Only if the screen is showing
      if (PanelManager.self.itemSelectionScreen.isShowing()) {
         PanelManager.self.itemSelectionScreen.receiveItemsFromServer(msg.userObjects, msg.pageNumber, msg.totalItemCount,
            msg.equippedArmorId, msg.equippedWeaponId, msg.itemArray);
      }
   }

   [TargetRpc]
   public void Target_ReceiveInventoryItemsForInventory (NetworkConnection connection, InventoryMessage msg) {
      // Get the inventory panel
      InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

      // Make sure the inventory panel is showing
      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(Panel.Type.Inventory);
      }

      // Update the Inventory Panel with the items we received from the server
      panel.receiveItemsFromServer(msg.userObjects, msg.categories, msg.pageNumber, msg.gold, msg.gems, msg.totalItemCount, msg.equippedWeaponId, msg.equippedArmorId, msg.itemArray);
   }

   [TargetRpc]
   public void Target_ReceiveFriendshipInfo (NetworkConnection connection, FriendshipInfo[] friendshipInfo,
      Friendship.Status friendshipStatus, int pageNumber, int totalFriendInfoCount, int friendCount, int pendingRequestCount) {
      List<FriendshipInfo> friendshipInfoList = new List<FriendshipInfo>(friendshipInfo);

      // Make sure the panel is showing
      FriendListPanel panel = (FriendListPanel) PanelManager.self.get(Panel.Type.FriendList);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithFriendshipInfo(friendshipInfoList, friendshipStatus, pageNumber, totalFriendInfoCount, friendCount, pendingRequestCount);
   }

   [TargetRpc]
   public void Target_ReceiveUserIdForFriendshipInvite (NetworkConnection connection, int friendUserId, string friendName) {
      // Make sure the panel is showing
      FriendListPanel panel = (FriendListPanel) PanelManager.self.get(Panel.Type.FriendList);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass the data to the panel
      panel.receiveUserIdForFriendshipInvite(friendUserId, friendName);
   }

   [TargetRpc]
   public void Target_ReceiveSingleMail (NetworkConnection connection, MailInfo mail, Item[] attachedItems) {
      List<Item> attachedItemList = new List<Item>(attachedItems);

      // Make sure the panel is showing
      MailPanel panel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithSingleMail(mail, attachedItemList);
   }

   [TargetRpc]
   public void Target_ReceiveMailList (NetworkConnection connection,
      MailInfo[] mailArray, int pageNumber, int totalMailCount) {
      List<MailInfo> mailList = new List<MailInfo>(mailArray);

      // Make sure the panel is showing
      MailPanel panel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithMailList(mailList, pageNumber, totalMailCount);
   }

   [TargetRpc]
   public void Target_ReceiveSingleBlueprint (NetworkConnection connection, Item blueprint,
      Item[] equippedItems, Item[] inventoryIngredientsArray, Item[] requiredIngredientsArray) {
      List<Item> inventoryIngredients = new List<Item>(inventoryIngredientsArray);
      List<Item> requiredIngredients = new List<Item>(requiredIngredientsArray);

      // Get the crafting panel
      CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

      // Get the reward panel
      RewardScreen rewardScreen = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);

      // Make sure the crafting panel is showing, except if the reward panel is showing
      if (!craftingPanel.isShowing() && !rewardScreen.isShowing()) {
         PanelManager.self.pushPanel(craftingPanel.type);
      }

      // Pass the data to the panel
      craftingPanel.updatePanelWithSingleBlueprint(blueprint, equippedItems,
         inventoryIngredients, requiredIngredients);
   }

   [TargetRpc]
   public void Target_ReceiveBlueprintList (NetworkConnection connection,
      Item[] blueprintArray, Blueprint.Status[] blueprintStatusesArray, int pageNumber, int totalBlueprintCount) {

      // Get the panel
      CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

      // Get the reward panel
      RewardScreen rewardScreen = (RewardScreen) PanelManager.self.get(Panel.Type.Reward);

      // Show the crafting panel, except if the reward panel is showing
      if (!craftingPanel.isShowing() && !rewardScreen.isShowing()) {
         PanelManager.self.pushPanel(craftingPanel.type);
      }

      // Pass the data to the panel
      craftingPanel.updatePanelWithBlueprintList(blueprintArray, blueprintStatusesArray, pageNumber, totalBlueprintCount);
   }

   [TargetRpc]
   public void Target_RefreshCraftingPanel (NetworkConnection connection) {
      // Get the crafting panel
      CraftingPanel panel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

      // Refresh the panel
      panel.refreshCurrentlySelectedBlueprint();
   }

   [TargetRpc]
   public void Target_ReceiveVoyageMapList (NetworkConnection connection, Voyage[] voyageArray) {
      List<Voyage> voyageList = new List<Voyage>(voyageArray);

      // Make sure the panel is showing
      VoyagePanel panel = (VoyagePanel) PanelManager.self.get(Panel.Type.Voyage);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(panel.type);
      }

      // Pass the data to the panel
      panel.updatePanelWithVoyageList(voyageList);
   }

   [TargetRpc]
   public void Target_CloseVoyagePanel (NetworkConnection connection) {
      // Get the panel
      VoyagePanel panel = (VoyagePanel) PanelManager.self.get(Panel.Type.Voyage);

      // Close the panel if it is showing
      if (panel.isShowing()) {
         PanelManager.self.popPanel();
      }
   }

   [TargetRpc]
   public void Target_ReceiveVoyageGroupMembers (NetworkConnection connection, int[] groupMembersArray) {
      List<int> groupMembers = new List<int>(groupMembersArray);

      // Get the panel
      VoyageGroupPanel panel = VoyageGroupPanel.self;

      // Make sure the panel is showing
      if (!panel.isShowing()) {
         panel.show();
      }

      // Update the panel info
      panel.updatePanelWithGroupMembers(groupMembers);
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
   public void Cmd_RequestShipAbilities (int shipID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         ShipInfo shipInfo = DB_Main.getShipInfo(shipID);
         if (shipInfo.shipAbilityXML == null || shipInfo.shipAbilityXML == "") {
            D.error("XML Data does not exist for Ship ID: " + shipID + " Creating new Data with Default Ability: " + ShipAbilityInfo.DEFAULT_ABILITY);
            string[] shipArray = new string[] { ShipAbilityInfo.DEFAULT_ABILITY };

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
   public void Target_SetShipAbilities (NetworkConnection connection, string[] rawAbilityData) {
      CannonPanel.self.setAbilityTab(rawAbilityData);
   }

   [TargetRpc]
   public void Target_ReceiveAreaInfo (NetworkConnection connection, string areaKey, int latestVersion, Vector3 mapPosition) {
      D.log($"Client received areaKey {areaKey} with latest version {latestVersion} from the server.");

      // Check if we already have the Area created
      Area area = AreaManager.self.getArea(areaKey);

      if (area != null && area.version == latestVersion) {
         return;
      }

      // If our version is outdated (and we're not a server/host), then delete the old version
      if (area != null && area.version != latestVersion && !NetworkServer.active) {
         D.log($"Found outdated {areaKey} with version {area.version}, so destroying it.");
         Destroy(area.gameObject);
      }

      // Check if we have stored map data for that area and version
      if (MapCache.hasMap(areaKey, latestVersion)) {
         D.log($"Found cached map data for {areaKey} and version {latestVersion}, using that.");
         string mapData = MapCache.getMapData(areaKey, latestVersion);
         MapManager.self.createLiveMap(areaKey, new MapInfo(areaKey, mapData, latestVersion), mapPosition);

         return;
      }

      // If we don't have the latest version of the map, download it
      D.log($"Client did not have area or map data for {areaKey} with latest version {latestVersion}, so downloading and storing it.");
      StartCoroutine(MapManager.self.CO_DownloadAndCreateMap(areaKey, latestVersion, mapPosition));
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
      int totalInstances = InstanceManager.self.getInstanceCount(_player.areaKey);

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
            _player.rpc.Target_ReceiveTradeHistoryInfo(_player.connectionToClient, tradeList.ToArray(), pageIndex, totalTradeCount);
         });
      });
   }

   [Command]
   public void Cmd_RequestLeaderBoardsFromServer (LeaderBoardsManager.Period period, Faction.Type boardFaction) {
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

         // Get the leader boards
         LeaderBoardsManager.self.getLeaderBoards(period, boardFaction, out farmingEntries, out sailingEntries, out exploringEntries,
            out tradingEntries, out craftingEntries, out miningEntries);

         // Get the last calculation date of this period
         DateTime lastCalculationDate = DB_Main.getLeaderBoardEndDate(period);

         // Get the time left until recalculation
         TimeSpan timeLeftUntilRecalculation = LeaderBoardsManager.self.getTimeLeftUntilRecalculation(period, lastCalculationDate);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveLeaderBoards(_player.connectionToClient, period, boardFaction, timeLeftUntilRecalculation.TotalSeconds,
               farmingEntries.ToArray(), sailingEntries.ToArray(), exploringEntries.ToArray(), tradingEntries.ToArray(),
               craftingEntries.ToArray(), miningEntries.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_RequestItemsFromServerForInventory (int pageNumber, int itemsPerPage, Item.Category[] categories) {
      // Enforce a reasonable max here
      if (itemsPerPage > 200) {
         D.warning("Requesting too many items per page.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         UserInfo userInfo = userObjects.userInfo;
         int totalItemCount = DB_Main.getItemCount(_player.userId, categories, new List<int> { userInfo.weaponId, userInfo.armorId },
            new List<Item.Category>() { Item.Category.Blueprint });

         // Calculate the maximum page number
         int maxPage = Mathf.CeilToInt((float) totalItemCount / itemsPerPage);
         if (maxPage == 0) {
            maxPage = 1;
         }

         // Clamp the requested page number to the max page - the number of items could have changed
         pageNumber = Mathf.Clamp(pageNumber, 1, maxPage);

         // Get the items from the database
         List<Item> items = DB_Main.getItems(_player.userId, categories, pageNumber, itemsPerPage, new List<int> { userInfo.weaponId, userInfo.armorId },
            new List<Item.Category>() { Item.Category.Blueprint });

         // Get the equipped weapon, independently of the category filters
         if (userInfo.weaponId != 0) {
            items.Add(DB_Main.getItem(_player.userId, userInfo.weaponId));
         }

         // Get the equipped armor, independently of the category filters
         if (userInfo.armorId != 0) {
            items.Add(DB_Main.getItem(_player.userId, userInfo.armorId));
         }

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            List<Item> processedItemList = EquipmentXMLManager.setEquipmentData(items);

            InventoryMessage inventoryMessage = new InventoryMessage(_player.netId, userObjects, categories,
               pageNumber, userInfo.gold, userInfo.gems, totalItemCount, userInfo.armorId, userInfo.weaponId, processedItemList.ToArray());
            Target_ReceiveInventoryItemsForInventory(_player.connectionToClient, inventoryMessage);
         });
      });
   }

   [Command]
   public void Cmd_RequestItemsFromServerForItemSelection (Item.Category category, int pageNumber, int itemsPerPage,
      bool filterEquippedItems, int[] itemIdsToFilterArray) {
      // Enforce a reasonable max here
      if (itemsPerPage > 200) {
         D.warning("Requesting too many items per page.");
         return;
      }

      Item.Category[] categoryArray = new Item.Category[1];
      categoryArray[0] = category;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserObjects userObjects = DB_Main.getUserObjects(_player.userId);
         UserInfo userInfo = userObjects.userInfo;

         int totalItemCount;
         List<Item> items;

         // Add equipped items to the list of item ids to filter
         List<int> itemIdsToFilter = itemIdsToFilterArray.ToList();
         if (filterEquippedItems) {
            itemIdsToFilter.Add(userInfo.weaponId);
            itemIdsToFilter.Add(userInfo.armorId);
         }

         // Get the item count and item list from the database
         totalItemCount = DB_Main.getItemCount(_player.userId, categoryArray, itemIdsToFilter, new List<Item.Category>());
         items = DB_Main.getItems(_player.userId, categoryArray, pageNumber, itemsPerPage, itemIdsToFilter, new List<Item.Category>());

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            List<Item> processedItemList = EquipmentXMLManager.setEquipmentData(items);

            InventoryMessage inventoryMessage = new InventoryMessage(_player.netId, userObjects, new Item.Category[] { category },
               pageNumber, userInfo.gold, userInfo.gems, totalItemCount, userInfo.armorId, userInfo.weaponId, processedItemList.ToArray());
            Target_ReceiveInventoryItemsForItemSelection(_player.connectionToClient, inventoryMessage);
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
   public void Cmd_GetOffersForArea (string shopName) {
      bool useShopName = shopName == ShopManager.DEFAULT_SHOP_NAME ? false : true;

      // Get the current list of offers for the area
      List<CropOffer> list = new List<CropOffer>();
      if (useShopName) {
         list = ShopManager.self.getOffersByShopName(shopName);
      } else {
         list = ShopManager.self.getOffers(_player.areaKey);
      }

      // Get the last offer regeneration time
      long lastCropRegenTime = ShopManager.self.lastCropRegenTime.ToBinary();

      // Look up their current gold in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int gold = DB_Main.getGold(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ShopData shopData = new ShopData();
            if (useShopName) {
               shopData = ShopXMLManager.self.getShopDataByName(shopName);
            } else {
               shopData = ShopXMLManager.self.getShopDataByArea(_player.areaKey);
            }
            string greetingText = shopData.shopGreetingText;

            _player.rpc.Target_ReceiveOffers(_player.connectionToClient, gold, list.ToArray(), lastCropRegenTime, greetingText);
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
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the friendship level
         int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);

         // Initialize the relationship if it is the first time the player talks to this NPC
         if (friendshipLevel == -1) {
            friendshipLevel = 0;
            DB_Main.createNPCRelationship(npcId, _player.userId, friendshipLevel);
         }

         // Retrieve the status of all running and completed quests
         List<QuestStatusInfo> questStatuses = DB_Main.getQuestStatuses(npcId, _player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Save in a hashset the quests that the player has already completed
            HashSet<int> completedQuestIds = new HashSet<int>();
            foreach (QuestStatusInfo status in questStatuses) {
               if (status.questNodeId == -1) {
                  completedQuestIds.Add(status.questId);
               }
            }

            // Retrieve the full list of quests for this npc
            List<Quest> allQuests = NPCManager.self.getQuests(npcId);

            // Create a list of new quest objects, containing only information useful to the client
            List<Quest> questList = new List<Quest>();
            foreach (Quest quest in allQuests) {
               // Skip the quest if the player has already completed it
               if (!completedQuestIds.Contains(quest.questId)) {
                  Quest q = new Quest(quest.questId, quest.title, quest.friendshipRankRequired, quest.isRepeatable, quest.lastUsedNodeId, null);
                  questList.Add(q);

                  QuestStatusInfo questStatInfo = questStatuses.Find(_ => _.questId == quest.questId);
                  if (questStatInfo == null) {
                     q.questProgress = 0;
                  } else {
                     q.questProgress = questStatInfo.questNodeId;
                  }
               }
            }

            // Retrieve other data only available server-side
            string greetingText = NPCManager.self.getGreetingText(npcId, friendshipLevel);
            bool canOfferGift = NPCManager.self.canOfferGift(friendshipLevel);
            bool hasTradeGossipDialogue = NPCManager.self.hasTradeGossipDialogue(npcId);
            bool hasGoodbyeDialogue = NPCManager.self.hasGoodbyeDialogue(npcId);
            NPC npc = NPCManager.self.getNPC(npcId);

            // Send the data to the client
            Target_ReceiveNPCQuestList(_player.connectionToClient, npcId, npc.getName(), npc.getFaction(), npc.getSpecialty(),
               friendshipLevel, greetingText, canOfferGift, hasTradeGossipDialogue, hasGoodbyeDialogue,
               questList.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_SelectNPCQuest (int npcId, int questId) {
      // Get the selected quest
      Quest quest = NPCManager.self.getQuest(npcId, questId);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Retrieve the friendship level
         int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);

         // Verifies if the user has enough friendship to start the quest
         if (!NPCFriendship.isRankAboveOrEqual(friendshipLevel, quest.friendshipRankRequired)) {
            return;
         }

         // Get the quest status
         QuestStatusInfo status = DB_Main.getQuestStatus(npcId, _player.userId, questId);

         // Determine the destination node
         int destinationNodeId;
         if (status == null) {
            // If it is the first time the player enters this quest, the destination node is the first node
            destinationNodeId = quest.getFirstNode().nodeId;

            // Initialize the quest status in the DB
            DB_Main.createQuestStatus(npcId, _player.userId, questId, destinationNodeId);
         } else {
            // If the conversation is ongoing, retrieve the last reached node
            destinationNodeId = status.questNodeId;
         }

         // If there is no destination node, throw an error
         if (destinationNodeId == -1) {
            D.error("The next node of this quest is undefined " + npcId + "/" + questId);
            return;
         }

         // Retrieve the destination node
         QuestNode destinationNode = NPCManager.self.getQuestNode(npcId, questId, destinationNodeId);

         // Manage the objectives if they exist
         List<int> objectivesProgress = new List<int>();
         bool areObjectivesCompleted = true;
         foreach (QuestObjective o in destinationNode.getAllQuestObjectives()) {
            // Retrieve the objective progress
            int progress = o.getObjectiveProgress(_player.userId);
            objectivesProgress.Add(progress);

            // Check if the objective can be completed
            if (!o.canObjectiveBeCompleted(progress)) {
               areObjectivesCompleted = false;
            }
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the new quest status to the client
            Target_ReceiveNPCQuestNode(_player.connectionToClient, questId, destinationNode,
               friendshipLevel, areObjectivesCompleted, objectivesProgress.ToArray());
         });
      });
   }

   [Command]
   public void Cmd_MoveToNextNPCQuestNode (int npcId, int questId) {
      // Select the quest
      Quest currentQuest = NPCManager.self.getQuest(npcId, questId);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Get the quest status
         QuestStatusInfo status = DB_Main.getQuestStatus(npcId, _player.userId, currentQuest.questId);

         // If there is no quest status, throw an error
         if (status == null) {
            D.error("The quest status for this quest and user does not exist " + _player.userId + "/" + npcId + "/" + questId);
            return;
         }

         // Get the last reached node in this conversation
         QuestNode currentNode = NPCManager.self.getQuestNode(npcId, questId, status.questNodeId);

         // Make sure that the quest objectives can be completed
         foreach (QuestObjective objective in currentNode.getAllQuestObjectives()) {
            if (!objective.canObjectiveBeCompletedDB(_player.userId)) {
               return;
            }
         }

         // Complete each quest objective
         foreach (QuestObjective objective in currentNode.getAllQuestObjectives()) {
            objective.completeObjective(_player.userId);
         }

         // Keeps track of all the rewarded items
         List<Item> rewardedItems = new List<Item>();

         // Grants the rewards
         foreach (QuestReward r in currentNode.getAllQuestRewards()) {
            Item item = r.giveRewardToUser(npcId, _player.userId);
            if (item != null) {
               rewardedItems.Add(item);
            }
         }

         // If this is the end of the conversation and the quest is repeatable, reset its progress
         if (currentNode.nextNodeId == -1 && currentQuest.isRepeatable) {
            DB_Main.updateQuestStatus(npcId, _player.userId, questId, currentQuest.getFirstNode().nodeId);
         } else {
            // Update the DB with the quest status after advancing to the next node
            DB_Main.updateQuestStatus(npcId, _player.userId, questId, currentNode.nextNodeId);
         }

         // Retrieve the next node
         QuestNode nextNode = null;
         if (currentNode.nextNodeId != -1) {
            nextNode = NPCManager.self.getQuestNode(npcId, questId, currentNode.nextNodeId);
         }

         // Determine the objectives progress status of the next node
         List<int> objectivesProgress = new List<int>();
         bool areObjectivesCompleted = true;
         if (currentNode.nextNodeId != -1) {
            foreach (QuestObjective o in nextNode.getAllQuestObjectives()) {
               // Retrieve the objective progress
               int progress = o.getObjectiveProgress(_player.userId);
               objectivesProgress.Add(progress);

               // Check if the objective can be completed
               if (!o.canObjectiveBeCompleted(progress)) {
                  areObjectivesCompleted = false;
               }
            }
         }

         // Retrieve the friendship level
         int friendshipLevel = DB_Main.getFriendshipLevel(npcId, _player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // If this is the end of the conversation, close the dialogue panel
            if (currentNode.nextNodeId == -1) {
               Target_EndNPCDialogue(_player.connectionToClient);
            } else {
               // Send the new quest status to the client
               Target_ReceiveNPCQuestNode(_player.connectionToClient, questId, nextNode,
                  friendshipLevel, areObjectivesCompleted, objectivesProgress.ToArray());
            }

            // If items have been rewarded, show the reward panel
            if (rewardedItems.Count > 0) {
               // Registers the quest completion to the achievement data
               AchievementManager.registerUserAchievement(_player.userId, ActionType.QuestComplete);
               Target_ReceiveItemList(_player.connectionToClient, rewardedItems.ToArray());
            }
         });
      });
   }

   [Command]
   public void Cmd_RequestNPCTradeGossipFromServer (int npcId) {
      string gossip = NPCManager.self.getNPC(npcId).tradeGossip;

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
            AchievementManager.registerUserAchievement(_player.userId, ActionType.NPCGift);
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
         UserInfo friendUserInfo = DB_Main.getUserInfo(friendUserId);
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
         UserInfo friendInfo = DB_Main.getUserInfo(friendUserId);

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

         // Determine if the friends are online
         foreach (FriendshipInfo friend in friendshipInfoList) {
            friend.isOnline = ServerNetwork.self.isUserOnline(friend.friendUserId);
         }

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
            Target_ReceiveFriendshipInfo(_player.connectionToClient, friendshipInfoList.ToArray(), friendshipStatus, pageNumber, totalFriendInfoCount, friendCount, pendingRequestCount);
         });
      });
   }

   [Command]
   public void Cmd_CreateMail (string recipientName, string mailSubject, string message, int[] attachedItemsIds, int[] attachedItemsCount) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      string feedbackMessage = "";
      bool success = true;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Try to retrieve the recipient info
         UserInfo recipientUserInfo = DB_Main.getUserInfo(recipientName);
         if (recipientUserInfo == null) {
            feedbackMessage = "The player " + recipientName + " does not exist!";
            success = false;
         }

         // Verify the attached items validity
         if (success) {
            // Verify that the number of attached items is below the maximum
            if (attachedItemsIds.Length > MailManager.MAX_ATTACHED_ITEMS) {
               D.error(string.Format("The mail from user {0} to recipient {1} has too many attached items ({2}).", _player.userId, recipientUserInfo.userId, attachedItemsIds.Length));
               return;
            }

            for (int i = 0; i < attachedItemsIds.Length; i++) {
               // Retrieve the item from the player's inventory
               Item item = DB_Main.getItem(_player.userId, attachedItemsIds[i]);

               // Verify if the player has the item in his inventory
               if (item == null) {
                  feedbackMessage = "One of the items is not present in your inventory!";
                  success = false;
                  break;
               }

               // Verify that there are enough items in the stack
               if (item.count < attachedItemsCount[i]) {
                  feedbackMessage = "You don't have enough " + item.getName() + " to send!";
                  success = false;
                  break;
               }

               // Verify that the item is not equipped
               UserInfo userInfo = DB_Main.getUserInfo(_player.userId);

               if (userInfo.armorId == item.id || userInfo.weaponId == item.id) {
                  feedbackMessage = "You cannot send an equipped item!";
                  success = false;
                  break;
               }
            }
         }

         MailInfo mail = null;

         if (success) {
            // Create the mail object
            mail = new MailInfo(-1, recipientUserInfo.userId, _player.userId, DateTime.UtcNow,
               false, mailSubject, message);

            // Write the mail in the database
            mail.mailId = DB_Main.createMail(mail);

            if (mail.mailId == -1) {
               D.error(string.Format("Error when creating a mail from sender {0} to recipient {1}.", _player.userId, recipientUserInfo.userId));
               feedbackMessage = "An error occurred when sending the mail!";
               success = false;
            }
         }

         if (success) {
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

            // Prepare a feedback message
            feedbackMessage = "The mail has been successfully sent to " + recipientName + "!";
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (success) {
               // Let the player know that the mail was sent
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.MailSent, _player, feedbackMessage);
            } else {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, feedbackMessage);
            }
         });
      });
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

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveSingleMail(_player.connectionToClient, mail, attachedItems.ToArray());
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

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveSingleMail(_player.connectionToClient, mail, attachedItems.ToArray());
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
         getItemsForArea(ShopManager.DEFAULT_SHOP_NAME);
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
            newItem = DB_Main.createNewItem(_player.userId, singleShopItem);
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
            AchievementManager.registerUserAchievement(_player.userId, ActionType.BuyItem);

            // Registers the purchasing of equipment action to the achievement database for recording
            if (shopItem.category == Item.Category.Weapon) {
               AchievementManager.registerUserAchievement(_player.userId, ActionType.WeaponBuy);
            }
            if (shopItem.category == Item.Category.Armor) {
               AchievementManager.registerUserAchievement(_player.userId, ActionType.ArmorBuy);
            }
            if (shopItem.category == Item.Category.Helm) {
               AchievementManager.registerUserAchievement(_player.userId, ActionType.HeadgearBuy);
            }

            // Decrease the item count
            ShopManager.self.decreaseItemCount(shopItemId);

            // Let the client know that it was successful
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.StoreItemBought, _player, "You have purchased a " + shopItem.getName() + "!");

            // Make sure their gold display gets updated
            getItemsForArea(ShopManager.DEFAULT_SHOP_NAME);
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
            AchievementManager.registerUserAchievement(_player.userId, ActionType.BuyShip);

            // Show a popup panel for the player
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.ShipBought, _player);

            // Make sure their gold display gets updated
            getShipsForArea(ShopManager.DEFAULT_SHOP_NAME);
         });
      });
   }

   #region Guilds

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

   #endregion

   [Command]
   public void Cmd_RequestBlueprintsFromServer (int pageNumber, int itemsPerPage) {
      // Enforce a reasonable max here
      if (itemsPerPage > 200) {
         D.warning("Requesting too many items per page.");
         return;
      }

      // Set the category filter
      Item.Category[] categories = new Item.Category[] { Item.Category.Blueprint };

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the item count
         int totalItemCount = DB_Main.getItemCount(_player.userId, categories);

         // Calculate the maximum page number
         int maxPage = Mathf.CeilToInt((float) totalItemCount / itemsPerPage);
         if (maxPage == 0) {
            maxPage = 1;
         }

         // Clamp the requested page number to the max page - the number of items could have changed
         pageNumber = Mathf.Clamp(pageNumber, 1, maxPage);

         // Get the blueprints from the database
         List<Item> blueprints = DB_Main.getItems(_player.userId, categories, pageNumber, itemsPerPage);

         // Build the list of ingredients needed for all the blueprints in the page
         List<CraftingIngredients.Type> ingredientsList = new List<CraftingIngredients.Type>();
         foreach (Item blueprint in blueprints) {
            // Get the resulting item
            Item resultItem = Blueprint.getItemData(((Blueprint) blueprint).bpTypeID);

            // Get the crafting requirement data
            CraftableItemRequirements craftingRequirements = CraftingManager.self.getCraftableData(
               resultItem.category, resultItem.itemTypeId);

            // Add the required ingredients to the common list
            if (craftingRequirements != null) {
               foreach (Item item in craftingRequirements.combinationRequirements) {
                  ingredientsList.Add((CraftingIngredients.Type) item.itemTypeId);
               }
            }
         }

         // Get the ingredients present in the user inventory
         List<Item> inventoryIngredients = DB_Main.getCraftingIngredients(_player.userId, ingredientsList);

         // Determine if each blueprint can be crafted, not crafted or if the data is missing
         List<Blueprint.Status> blueprintStatuses = new List<Blueprint.Status>();
         foreach (Item blueprint in blueprints) {
            Blueprint.Status status = Blueprint.Status.Craftable;

            // Get the resulting item
            Item resultItem = Blueprint.getItemData(((Blueprint) blueprint).bpTypeID);

            // Get the crafting requirement data
            CraftableItemRequirements craftingRequirements = CraftingManager.self.getCraftableData(
               resultItem.category, resultItem.itemTypeId);

            if (craftingRequirements == null) {
               status = Blueprint.Status.MissingRecipe;
            } else {
               // Compare the ingredients present in the inventory with the requisites
               foreach (Item requiredIngredient in craftingRequirements.combinationRequirements) {

                  // Get the inventory ingredient, if there is any
                  Item inventoryIngredient = inventoryIngredients.Find(s =>
                     s.itemTypeId == requiredIngredient.itemTypeId);

                  // Verify that there are enough items in the inventory stack
                  if (inventoryIngredient == null || inventoryIngredient.count < requiredIngredient.count) {
                     status = Blueprint.Status.NotCraftable;
                     break;
                  }
               }
            }

            blueprintStatuses.Add(status);
         }
         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveBlueprintList(_player.connectionToClient, blueprints.ToArray(), blueprintStatuses.ToArray(), pageNumber, totalItemCount);
         });
      });
   }

   [Command]
   public void Cmd_RequestSingleBlueprintFromServer (int itemId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Retrieve the blueprint from the player's inventory
         Item blueprint = DB_Main.getItem(_player.userId, itemId);

         // Verify if the player has the blueprint in his inventory
         if (blueprint == null) {
            string feedbackMessage = "The blueprint is not present in your inventory!";
            sendError(feedbackMessage);
            return;
         }

         // Get the user equipped items
         UserInfo userInfo = DB_Main.getUserInfo(_player.userId);
         List<Item> equippedItems = new List<Item>(2);

         // Get the equipped weapon
         if (userInfo.weaponId != 0) {
            equippedItems.Add(DB_Main.getItem(_player.userId, userInfo.weaponId));
         }

         // Get the equipped armor
         if (userInfo.armorId != 0) {
            equippedItems.Add(DB_Main.getItem(_player.userId, userInfo.armorId));
         }

         // Get the resulting item
         Item resultItem = Blueprint.getItemData(((Blueprint) blueprint).bpTypeID);

         // Get the crafting requirement data
         CraftableItemRequirements craftingRequirements = CraftingManager.self.getCraftableData(
               resultItem.category, resultItem.itemTypeId);

         // Build the list of ingredients
         List<CraftingIngredients.Type> ingredientsList = new List<CraftingIngredients.Type>();
         if (craftingRequirements != null) {
            foreach (Item item in craftingRequirements.combinationRequirements) {
               ingredientsList.Add((CraftingIngredients.Type) item.itemTypeId);
            }
         }

         // Get the ingredients present in the user inventory
         List<Item> inventoryIngredients = DB_Main.getCraftingIngredients(_player.userId, ingredientsList);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            Target_ReceiveSingleBlueprint(_player.connectionToClient, blueprint, equippedItems.ToArray(),
               inventoryIngredients.ToArray(), craftingRequirements.combinationRequirements);
         });
      });
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
         Item resultItem = Blueprint.getItemData(blueprint.bpTypeID);

         // Randomize the colors
         resultItem.color1 = Util.randomEnum<ColorType>();
         resultItem.color2 = Util.randomEnum<ColorType>();

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
         List<CraftingIngredients.Type> ingredientsList = new List<CraftingIngredients.Type>();
         foreach (Item item in craftingRequirements.combinationRequirements) {
            ingredientsList.Add((CraftingIngredients.Type) item.itemTypeId);
         }

         // Get the ingredients present in the user inventory
         List<Item> inventoryIngredients = DB_Main.getCraftingIngredients(_player.userId, ingredientsList);

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

         // Add the result item to the user inventory
         Item craftedItem = DB_Main.createItemOrUpdateItemCount(_player.userId, resultItem);

         // If the item could not be created, stop the process
         if (craftedItem == null) {
            D.warning(string.Format("Could not create the crafted item in the user inventory. Blueprint id: {0}, item category: {1}, item type id: {2}.",
               blueprint.bpTypeID, resultItem.category, resultItem.itemTypeId));
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
         DB_Main.addJobXP(_player.userId, Jobs.Type.Crafter, _player.faction, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // Registers the crafting action to the achievement database
            AchievementManager.registerUserAchievement(_player.userId, ActionType.Craft);

            // Let them know they gained experience
            _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Crafter, 0);

            // Update the available ingredients for the displayed blueprint
            Target_RefreshCraftingPanel(_player.connectionToClient);

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

      // Retrieve the list of voyages
      List<Voyage> voyageList = VoyageManager.self.getAllVoyages();

      // Send the list to the client
      Target_ReceiveVoyageMapList(_player.connectionToClient, voyageList.ToArray());
   }

   [Command]
   public void Cmd_AddUserToQuickmatchVoyageGroup (string areaKey) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Retrieve the voyage data
      Voyage voyage = VoyageManager.self.getVoyage(areaKey);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Verify that the user is not already in a group
         int previousGroupId = DB_Main.getVoyageGroupForMember(_player.userId);

         if (previousGroupId >= 0) {
            // Remove the player from its previous group
            DB_Main.deleteMemberFromVoyageGroup(previousGroupId, _player.userId);

            // Update its voyage group in the net entity
            _player.voyageGroupId = -1;
         }

         // Find the oldest incomplete group in the given area
         VoyageGroupInfo voyageGroup = DB_Main.getBestVoyageGroupForQuickmatch(areaKey);

         // If no group is available, create a new one
         if (voyageGroup == null) {
            voyageGroup = new VoyageGroupInfo(-1, areaKey, DateTime.UtcNow, true, false, 0);
            voyageGroup.groupId = DB_Main.createVoyageGroup(voyageGroup);
         }

         // Add the user to the group
         DB_Main.addMemberToVoyageGroup(voyageGroup.groupId, _player.userId);

         // Update its voyage group in the net entity
         _player.voyageGroupId = voyageGroup.groupId;

         // Increase the member group count for later calculations
         voyageGroup.memberCount++;

         // If the group is now complete, disable its quickmatch
         if (voyageGroup.memberCount >= Voyage.getMaxGroupSize(voyage.difficulty)) {
            DB_Main.updateVoyageGroupQuickmatchStatus(voyageGroup.groupId, false);
         }

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Close the voyage panel
            Target_CloseVoyagePanel(_player.connectionToClient);

            // Get the area and spawn of the voyage map
            Area area = AreaManager.self.getArea(areaKey);
            Spawn spawn = SpawnManager.self.getSpawn(areaKey);

            // Warp to the voyage area
            _player.spawnInNewMap(areaKey, spawn, Direction.South);
         });
      });
   }

   [Command]
   public void Cmd_CreatePrivateVoyageGroup (string areaKey) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Retrieve the voyage data
      Voyage voyage = VoyageManager.self.getVoyage(areaKey);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Verify that the user is not already in a group
         int previousGroupId = DB_Main.getVoyageGroupForMember(_player.userId);

         if (previousGroupId >= 0) {
            // Remove the player from its previous group
            DB_Main.deleteMemberFromVoyageGroup(previousGroupId, _player.userId);

            // Update its voyage group in the net entity
            _player.voyageGroupId = -1;
         }

         // Create a new private group
         VoyageGroupInfo voyageGroup = new VoyageGroupInfo(-1, areaKey, DateTime.UtcNow, false, true, 0);
         voyageGroup.groupId = DB_Main.createVoyageGroup(voyageGroup);

         // Add the user to the group
         DB_Main.addMemberToVoyageGroup(voyageGroup.groupId, _player.userId);

         // Update its voyage group in the net entity
         _player.voyageGroupId = voyageGroup.groupId;

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Close the voyage panel
            Target_CloseVoyagePanel(_player.connectionToClient);

            // Get the area and spawn of the voyage map
            Area area = AreaManager.self.getArea(areaKey);
            Spawn spawn = SpawnManager.self.getSpawn(areaKey);

            // Warp to the voyage area
            _player.spawnInNewMap(areaKey, spawn, Direction.South);
         });
      });
   }

   [Command]
   public void Cmd_SendVoyageGroupInvitationToUser (string inviteeName) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // If the player is not in a group, send an error
      if (!VoyageManager.isInVoyage(_player)) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "You must belong to a voyage group to invite someone!");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Try to retrieve the invitee info
         UserInfo inviteeInfo = DB_Main.getUserInfo(inviteeName);
         if (inviteeInfo == null) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The player " + inviteeName + " does not exists!");
            });
            return;
         }

         // Get the voyage group info
         VoyageGroupInfo voyageGroup = DB_Main.getVoyageGroup(_player.voyageGroupId);

         // Check that the group is private
         if (!voyageGroup.isPrivate) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "Only private groups allow inviting players!");
            });
            return;
         }

         // Get the invitee voyage group id, if any
         int inviteeCurrentGroupId = DB_Main.getVoyageGroupForMember(inviteeInfo.userId);

         // Check if the invitee already belongs to the group
         if (inviteeCurrentGroupId == voyageGroup.groupId) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, inviteeName + " already belongs to the group!");
            });
            return;
         }

         // Get the voyage data
         Voyage voyage = VoyageManager.self.getVoyage(voyageGroup.areaKey);

         // Check that there is an empty slot in the group
         if (voyageGroup.memberCount >= Voyage.getMaxGroupSize(voyage.difficulty)) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "Your voyage group is full!");
            });
            return;
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Find the server where the invitee is located
            Server inviteeServer = ServerNetwork.self.getServerContainingUser(inviteeInfo.userId);

            if (inviteeServer == null) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "Could not find the player " + inviteeName);
               return;
            }

            // Send the invitation
            inviteeServer.photonView.RPC("HandleVoyageGroupInvite", inviteeServer.view.owner, voyageGroup.groupId, _player.entityName, inviteeInfo.userId);

            // Write in the inviter chat that the invitation has been sent
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The voyage invitation has been sent to " + inviteeName);
         });
      });
   }

   [Command]
   public void Cmd_AddUserToVoyageGroup (int voyageGroupId) {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the group info
         VoyageGroupInfo voyageGroup = DB_Main.getVoyageGroup(voyageGroupId);

         // Check that the group exists
         if (voyageGroup == null) {
            sendError("The voyage group does not exist!");
            return;
         }

         // Get the voyage data
         Voyage voyage = VoyageManager.self.getVoyage(voyageGroup.areaKey);

         // Check that there is an empty slot in the group
         if (voyageGroup.memberCount >= Voyage.getMaxGroupSize(voyage.difficulty)) {
            sendError("The voyage group is full!");
            return;
         }

         // Get the current voyage group the user belongs to, if any
         int currentGroupId = DB_Main.getVoyageGroupForMember(_player.userId);

         // If the user already belongs to the destination group, do nothing
         if (currentGroupId == voyageGroupId) {
            return;
         }

         if (currentGroupId >= 0) {
            // Remove the player from its previous group
            DB_Main.deleteMemberFromVoyageGroup(currentGroupId, _player.userId);

            // Update its voyage group in the net entity
            _player.voyageGroupId = -1;
         }

         // Add the user to the group
         DB_Main.addMemberToVoyageGroup(voyageGroup.groupId, _player.userId);

         // Update its voyage group in the net entity
         _player.voyageGroupId = voyageGroup.groupId;

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Get the area and spawn of the voyage map
            Area area = AreaManager.self.getArea(voyageGroup.areaKey);
            Spawn spawn = SpawnManager.self.getSpawn(voyageGroup.areaKey);

            // Warp to the voyage area
            _player.spawnInNewMap(voyageGroup.areaKey, spawn, Direction.South);
         });
      });
   }

   [Command]
   public void Cmd_RemoveUserFromVoyageGroup () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // If the player is not in a group, do nothing
      if (!VoyageManager.isInVoyage(_player)) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Retrieve the group info
         VoyageGroupInfo voyageGroup = DB_Main.getVoyageGroup(_player.voyageGroupId);

         // Remove the player from its group
         DB_Main.deleteMemberFromVoyageGroup(_player.voyageGroupId, _player.userId);

         // Update the player voyage group id in the net entity
         _player.voyageGroupId = -1;

         // Update the group member count
         voyageGroup.memberCount--;

         // If the group is now empty, delete it
         if (voyageGroup.memberCount <= 0) {
            DB_Main.deleteVoyageGroup(voyageGroup.groupId);
         }

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // If the player is in a voyage area, warp him to the starting town
            if (VoyageManager.self.isVoyageArea(_player.areaKey)) {
               
               // Get the area and spawn
               Area area = AreaManager.self.getArea(VoyageManager.RETURN_AREA_KEY);
               Spawn spawn = SpawnManager.self.getSpawn(VoyageManager.RETURN_AREA_KEY, 
                  VoyageManager.RETURN_SPAWN_KEY);

               // Warp to the destination area
               _player.spawnInNewMap(VoyageManager.RETURN_AREA_KEY, spawn, Direction.South);
            }
         });
      });
   }

   [Command]
   public void Cmd_RequestVoyageGroupMembersFromServer () {
      if (_player == null) {
         D.warning("No player object found.");
         return;
      }

      // If the player is not in a group, send an empty group
      if (!VoyageManager.isInVoyage(_player)) {
         Target_ReceiveVoyageGroupMembers(_player.connectionToClient, new int[0]);
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {

         // Retrieve all the group members user info
         List<int> groupMembers = DB_Main.getVoyageGroupMembers(_player.voyageGroupId);

         // Back to the Unity thread to send the results back to the client
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the data to the client
            Target_ReceiveVoyageGroupMembers(_player.connectionToClient, groupMembers.ToArray());
         });
      });
   }

   [TargetRpc]
   public void Target_callInsufficientNotification (NetworkConnection connection, int npcID) {
      NPC npc = NPCManager.self.getNPC(npcID);
      Instantiate(PrefabsManager.self.insufficientPrefab, npc.transform.position + new Vector3(0f, .24f), Quaternion.identity);
   }

   #region Item Rewards for Combat and Crafting

   [Server]
   public void processEnemyRewards (Enemy.Type enemyType) {
      // Gets loots for enemy type
      EnemyLootLibrary lootLibrary = RewardManager.self.fetchLandMonsterLootData(enemyType);
      List<LootInfo> processedLoots = lootLibrary.dropTypes.requestLootList();

      // Build the list of rewarded items
      List<Item> rewardedItems = new List<Item>();
      foreach (LootInfo lootInfo in processedLoots) {
         Item item = new Item { category = lootInfo.lootType.category, itemTypeId = lootInfo.lootType.itemTypeId };
         item.count = lootInfo.quantity;
         rewardedItems.Add(item);
      }

      // Grant the rewards to the user
      giveItemRewardsToPlayer(_player.userId, rewardedItems, true);
   }

   [TargetRpc]
   public void Target_ReceiveItemList (NetworkConnection connection, Item[] itemList) {
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

      // Build the list of rewarded items
      List<Item> rewardedItems = new List<Item>();
      foreach (LootInfo lootInfo in lootInfoList) {
         Item item = new Item { category = lootInfo.lootType.category, itemTypeId = lootInfo.lootType.itemTypeId };
         item.count = lootInfo.quantity;
         rewardedItems.Add(item);
      }

      // Grant the rewards to the user
      giveItemRewardsToPlayer(_player.userId, rewardedItems, true);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Add the mining xp
         int xp = 10;
         DB_Main.addJobXP(_player.userId, Jobs.Type.Miner, _player.faction, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Registers the Ore mining success action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player.userId, ActionType.MineOre);
            AchievementManager.registerUserAchievement(_player.userId, ActionType.OreGain, lootInfoList.Count);

            // Let them know they gained experience
            _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Miner, 0);
         });
      });
   }

   [Server]
   private void giveItemRewardsToPlayer (int userID, List<Item> rewardList, bool showPanel) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Create or update the database item
         foreach (Item item in rewardList) {
            DB_Main.createItemOrUpdateItemCount(userID, item);
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Calls Reward Popup
            if (showPanel) {
               Target_ReceiveItemList(_player.connectionToClient, rewardList.ToArray());
            }
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

      // Registers the interaction of treasure chests to the achievement database for recording
      AchievementManager.registerUserAchievement(_player.userId, ActionType.OpenTreasureChest);

      // Send it to the specific player that opened it
      Target_OpenChest(_player.connectionToClient, item, chest.id);
   }

   #region Spawn Sea Entities

   [Command]
   public void Cmd_SpawnPirateShip (Vector2 spawnPosition) {
      BotShipEntity bot = Instantiate(PrefabsManager.self.botShipPrefab, spawnPosition, Quaternion.identity);
      bot.instanceId = _player.instanceId;
      bot.facing = Util.randomEnum<Direction>();
      bot.areaKey = _player.areaKey;
      bot.npcType = NPC.Type.Blackbeard;
      bot.faction = NPC.getFactionFromType(bot.npcType);
      bot.route = null;
      bot.autoMove = true;
      bot.nationType = Nation.Type.Pirate;
      bot.speed = Ship.getBaseSpeed(Ship.Type.Caravel);
      bot.attackRangeModifier = Ship.getBaseAttackRange(Ship.Type.Caravel);
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
   public void Cmd_SpawnBossChild (Vector2 spawnPosition, uint parentEntityID, int xVal, int yVal, int variety, SeaMonsterEntity.Type enemyType) {
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

      Cmd_SpawnBossChild(spawnPosition + new Vector2(distanceGap, -distanceGap), parentID, 1, -1, 1, SeaMonsterEntity.Type.Tentacle);
      Cmd_SpawnBossChild(spawnPosition + new Vector2(-distanceGap, -distanceGap), parentID, -1, -1, 0, SeaMonsterEntity.Type.Tentacle);

      Cmd_SpawnBossChild(spawnPosition + new Vector2(distanceGap, distanceGap), parentID, 1, 1, 1, SeaMonsterEntity.Type.Tentacle);
      Cmd_SpawnBossChild(spawnPosition + new Vector2(-distanceGap, distanceGap), parentID, -1, 1, 0, SeaMonsterEntity.Type.Tentacle);

      Cmd_SpawnBossChild(spawnPosition + new Vector2(-diagonalDistanceGap, 0), parentID, -1, 0, 1, SeaMonsterEntity.Type.Tentacle);
      Cmd_SpawnBossChild(spawnPosition + new Vector2(diagonalDistanceGap, 0), parentID, 1, 0, 0, SeaMonsterEntity.Type.Tentacle);
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

   #endregion

   [Command]
   public void Cmd_StartNewTeamBattle (Enemy.Type[] defenderBattlers, Enemy.Type[] attackerBattlers, string[] defendersPlayerNames, string[] attackerPlayerNames) {
      // Checks if the user is an admin  
      if (!_player.isAdmin()) {
         D.warning("You are not at admin! Denying access to team combat simulation");
         return;
      }

      // Hard code enemy type spawn for now
      Enemy.Type enemyToSpawn = Enemy.Type.Lizard;

      // Create an Enemy in this instance
      Enemy spawnedEnemy = Instantiate(PrefabsManager.self.enemyPrefab);
      spawnedEnemy.enemyType = enemyToSpawn;

      // Add it to the Instance
      Instance newInstance = InstanceManager.self.getInstance(_player.instanceId);
      InstanceManager.self.addEnemyToInstance(spawnedEnemy, newInstance);

      spawnedEnemy.transform.position = _player.transform.position;
      spawnedEnemy.desiredPosition = spawnedEnemy.transform.position;
      NetworkServer.Spawn(spawnedEnemy.gameObject);

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
      NetworkIdentity enemyIdent = NetworkIdentity.spawned[spawnedEnemy.netId];
      Enemy enemy = enemyIdent.GetComponent<Enemy>();
      Instance instance = InstanceManager.self.getInstance(localBattler.instanceId);

      List<PlayerBodyEntity> bodyEntities = new List<PlayerBodyEntity>();

      // Register the Host as the first entry for the party entities
      bodyEntities.Add(localBattler);

      // Look up the player's Area
      Area area = AreaManager.self.getArea(_player.areaKey);

      // Enter the background thread to determine if the user has at least one ability equipped
      bool hasAbilityEquipped = false;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the skill list from database
         List<AbilitySQLData> abilityDataList = DB_Main.getAllAbilities(localBattler.userId);

         // Cache Attackers Info
         foreach (string playerName in attackerPlayerNames) {
            // Since the body manager has the collection of user net entities, it will search for the body entity with the user name provided
            BodyEntity rawEntity = BodyManager.self.getBodyWithName(playerName);

            if (rawEntity != null) {
               PlayerBodyEntity bodyEntity = (PlayerBodyEntity) rawEntity;
               if (bodyEntity != null) {
                  bodyEntities.Add(bodyEntity);
               } else {
                  D.warning("Server cant find this ID: " + rawEntity.userId);
               }
            }
         }

         // Cache Defenders Info
         foreach (string playerName in defendersPlayerNames) {
            // Since the body manager has the collection of user net entities, it will search for the body entity with the user name provided
            BodyEntity rawEntity = BodyManager.self.getBodyWithName(playerName);

            if (rawEntity != null) {
               PlayerBodyEntity bodyEntity = (PlayerBodyEntity) rawEntity;
               if (bodyEntity != null) {
                  bodyEntities.Add(bodyEntity);
               } else {
                  D.warning("Server cant find this ID: " + rawEntity.userId);
               }
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Determine if at least one ability is equipped
            hasAbilityEquipped = abilityDataList.Exists(_ => _.equipSlotIndex != -1);

            // If no ability is equipped, send an error to the client
            if (!hasAbilityEquipped) {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "You must equip at least one ability!");
               return;
            }

            // Get or create the Battle instance
            Battle battle = (enemy.battleId > 0) ? BattleManager.self.getBattle(enemy.battleId) : BattleManager.self.createTeamBattle(area, instance, enemy, attackerBattlers, localBattler, defenderBattlers);

            // If the Battle is full, we can't proceed
            if (!battle.hasRoomLeft(Battle.TeamType.Attackers)) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The battle is already full!");
               return;
            }

            // Add the player to the Battle
            BattleManager.self.addPlayerToBattle(battle, localBattler, Battle.TeamType.Attackers);

            // Provides all the abilities for the players in the party
            setupAbilitiesForPlayers(bodyEntities);

            // Provides the client with the info of the Equipped Abilities
            Target_UpdateBattleAbilityUI(_player.connectionToClient, Util.serialize(abilityDataList.FindAll(_ => _.equipSlotIndex >= 0)));

            // Send Battle Bg data
            int bgXmlID = battle.battleBoard.xmlID;
            BackgroundContentData fetchedBGData = BackgroundGameManager.self.backgroundContentList.Find(_ => _.xmlId == bgXmlID);
            Target_ReceiveBackgroundInfo(_player.connectionToClient, Util.serialize(new List<BackgroundContentData>() { fetchedBGData }));

            // Send class info to client
            PlayerClassData currentClassData = ClassManager.self.getClassData(localBattler.classType);
            Target_ReceiveClassInfo(_player.connectionToClient, JsonUtility.ToJson(currentClassData));

            // Send faction info to client
            PlayerFactionData currentFactionData = FactionManager.self.getFactionData(localBattler.faction);
            Target_ReceiveFactionInfo(_player.connectionToClient, JsonUtility.ToJson(currentFactionData));

            // Send specialty info to client
            PlayerSpecialtyData currentSpecialtyData = SpecialtyManager.self.getSpecialtyData(localBattler.specialty);
            Target_ReceiveSpecialtyInfo(_player.connectionToClient, JsonUtility.ToJson(currentSpecialtyData));

            foreach (PlayerBodyEntity inviteeBattlers in bodyEntities) {
               // Only invite other players, host should not receive this invite
               if (inviteeBattlers.userId != localBattler.userId) {
                  Battle.TeamType teamType = new List<string>(attackerPlayerNames).Find(_ => _ == inviteeBattlers.entityName) != null ? Battle.TeamType.Attackers : Battle.TeamType.Defenders;
                  inviteeBattlers.rpc.Target_InvitePlayerToCombat(inviteeBattlers.connectionToClient, spawnedEnemy.netId, teamType);
               }
            }
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveMsgFromServer (NetworkConnection connection, string message) {
      Debug.LogError("Server Msg is: " + message);
   }

   [TargetRpc]
   public void Target_InvitePlayerToCombat (NetworkConnection connection, uint enemyNetId, Battle.TeamType teamType) {
      Cmd_StartNewBattle(enemyNetId, teamType);
   }

   [Command]
   public void Cmd_StartNewBattle (uint enemyNetId, Battle.TeamType teamType) {
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
      Area area = AreaManager.self.getArea(_player.areaKey);

      // Enter the background thread to determine if the user has at least one ability equipped
      bool hasAbilityEquipped = false;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieve the skill list from database
         List<AbilitySQLData> abilityDataList = DB_Main.getAllAbilities(playerBody.userId);

         // Determine if at least one ability is equipped
         hasAbilityEquipped = abilityDataList.Exists(_ => _.equipSlotIndex != -1);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // If no ability is equipped, send an error to the client
            if (!hasAbilityEquipped) {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "You must equip at least one ability!");
               return;
            }

            // Get or create the Battle instance
            Battle battle = (enemy.battleId > 0) ? BattleManager.self.getBattle(enemy.battleId) : BattleManager.self.createBattle(area, instance, enemy, playerBody);

            // If the Battle is full, we can't proceed
            if (!battle.hasRoomLeft(Battle.TeamType.Attackers)) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, _player, "The battle is already full!");
               return;
            }

            // Add the player to the Battle
            BattleManager.self.addPlayerToBattle(battle, playerBody, teamType);

            // Host Battler Should know their own ability
            setupAbilitiesForPlayers(new List<PlayerBodyEntity>() { playerBody });

            // Send Battle Bg data
            int bgXmlID = battle.battleBoard.xmlID;
            BackgroundContentData fetchedBGData = BackgroundGameManager.self.backgroundContentList.Find(_ => _.xmlId == bgXmlID);
            Target_ReceiveBackgroundInfo(playerBody.connectionToClient, Util.serialize(new List<BackgroundContentData>() { fetchedBGData }));

            // Provides the client with the info of the Equipped Abilities
            Target_UpdateBattleAbilityUI(playerBody.connectionToClient, Util.serialize(abilityDataList.FindAll(_ => _.equipSlotIndex >= 0)));

            // Send class info to client
            PlayerClassData currentClassData = ClassManager.self.getClassData(playerBody.classType);
            Target_ReceiveClassInfo(playerBody.connectionToClient, JsonUtility.ToJson(currentClassData));

            // Send faction info to client
            PlayerFactionData currentFactionData = FactionManager.self.getFactionData(playerBody.faction);
            Target_ReceiveFactionInfo(playerBody.connectionToClient, JsonUtility.ToJson(currentFactionData));

            // Send specialty info to client
            PlayerSpecialtyData currentSpecialtyData = SpecialtyManager.self.getSpecialtyData(playerBody.specialty);
            Target_ReceiveSpecialtyInfo(playerBody.connectionToClient, JsonUtility.ToJson(currentSpecialtyData));
         });
      });
   }

   [Command]
   public void Cmd_UpdateAbility (int abilityID, int equipSlot) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fetch abilities of the user from the database
         List<AbilitySQLData> abilityDataList = DB_Main.getAllAbilities(_player.userId);

         // Checks if the user actually has the ability
         if (abilityDataList.Exists(_ => _.abilityID == abilityID)) {
            AbilitySQLData sqlData = abilityDataList.Find(_ => _.abilityID == abilityID);

            // Modifies equipment slot of the ability (Slots 0 -> 4 are Equipped and Slots -1 are Unequipped)
            sqlData.equipSlotIndex = equipSlot;

            // Make sure that the requested ability update is saved
            DB_Main.updateAbilitiesData(_player.userId, sqlData);
         }
         fetchAbilitiesForUIPanel();
      });
   }

   [Command]
   public void Cmd_UpdateAbilities (AbilitySQLData[] abilities) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (AbilitySQLData ability in abilities) {
            DB_Main.updateAbilitiesData(_player.userId, ability);
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            fetchAbilitiesForUIPanel();
         });
      });
   }

   [Command]
   public void Cmd_RequestAbility () {
      fetchAbilitiesForUIPanel();
   }

   [Server]
   public void setupAbilitiesForPlayers (List<PlayerBodyEntity> playerEntities) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         foreach (PlayerBodyEntity entity in playerEntities) {
            // Retrieves skill list from database
            List<AbilitySQLData> abilityDataList = DB_Main.getAllAbilities(entity.userId);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               List<AbilitySQLData> equippedAbilityDataList = abilityDataList.FindAll(_ => _.equipSlotIndex >= 0);

               // Sort by equipment slot index
               equippedAbilityDataList = equippedAbilityDataList.OrderBy(_ => _.equipSlotIndex).ToList();

               // Set server data
               Battler battler = BattleManager.self.getBattler(entity.userId);
               battler.setBattlerAbilities(getAbilityRecord(equippedAbilityDataList.ToArray()), BattlerType.PlayerControlled);
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

   [Server]
   public void fetchAbilitiesForUIPanel () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Retrieves skill list from database
         List<AbilitySQLData> abilityDataList = DB_Main.getAllAbilities(_player.userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Provides skill panel with all the abilities of the player 
            Target_ReceiveSkillsForSkillPanel(_player.connectionToClient, abilityDataList.ToArray());
         });
      });
   }

   [TargetRpc]
   public void Target_ReceiveSkillsForSkillPanel (NetworkConnection connection, AbilitySQLData[] abilityList) {
      // Make sure the panel is showing
      AbilityPanel panel = (AbilityPanel) PanelManager.self.get(Panel.Type.Ability_Panel);

      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(Panel.Type.Ability_Panel);
      }

      // Update the Skill Panel with the abilities we received from the server
      panel.receiveDataFromServer(abilityList);
   }

   [TargetRpc]
   public void Target_UpdateBattleAbilityUI (NetworkConnection connection, string[] rawAttackAbilities) {
      // Translate data
      List<AbilitySQLData> attackAbilityDataList = Util.unserialize<AbilitySQLData>(rawAttackAbilities);

      // Updates the combat UI 
      BattleUIManager.self.SetupAbilityUI(attackAbilityDataList.OrderBy(_ => _.equipSlotIndex).ToArray());
   }

   [TargetRpc]
   public void Target_ReceiveBackgroundInfo (NetworkConnection connection, string[] rawBGData) {
      List<BackgroundContentData> backgroundContentList = Util.unserialize<BackgroundContentData>(rawBGData); 
      BackgroundGameManager.self.receiveNewContent(backgroundContentList.ToArray());
   }

   [TargetRpc]
   public void Target_ReceiveClassInfo (NetworkConnection connection, string rawClassData) {
      PlayerClassData classData = JsonUtility.FromJson<PlayerClassData>(rawClassData);
      ClassManager.self.addClassInfo(classData);
   }

   [TargetRpc]
   public void Target_ReceiveJobInfo (NetworkConnection connection, string[] rawData) {
      List<PlayerJobData> jobData = Util.unserialize<PlayerJobData>(rawData);
      JobManager.self.receiveDataFromServer(jobData);
   }

   [TargetRpc]
   public void Target_ReceiveSpecialtyInfo (NetworkConnection connection, string rawSpecialtyData) {
      PlayerSpecialtyData specialtyData = JsonUtility.FromJson<PlayerSpecialtyData>(rawSpecialtyData);
      SpecialtyManager.self.addSpecialtyInfo(specialtyData);
   }

   [TargetRpc]
   public void Target_ReceiveFactionInfo (NetworkConnection connection, string rawFactionData) {
      PlayerFactionData factionData = JsonUtility.FromJson<PlayerFactionData>(rawFactionData);
      FactionManager.self.addFactionInfo(factionData);
   }

   [Command]
   public void Cmd_ProcessMonsterData (Enemy.Type[] enemyTypes) {
      List<BattlerData> battlerDataList = new List<BattlerData>();

      foreach (Enemy.Type enemyType in enemyTypes) {
         BattlerData battData = MonsterManager.self.requestBattler(enemyType);
         if (battData != null) {
            battData.battlerAbilities.basicAbilityRawData = Util.serialize(new List<BasicAbilityData>(battData.battlerAbilities.basicAbilityDataList));
            battData.battlerAbilities.attackAbilityRawData = Util.serialize(new List<AttackAbilityData>(battData.battlerAbilities.attackAbilityDataList));
            battData.battlerAbilities.buffAbilityRawData = Util.serialize(new List<BuffAbilityData>(battData.battlerAbilities.buffAbilityDataList));
            battlerDataList.Add(battData);
         }
      }

      if (battlerDataList.Count > 0) {
         Target_ReceiveMonsterData(_player.connectionToClient, Util.serialize(battlerDataList));
      }

      processEnemySkills(battlerDataList);
   }

   #region Skills and Abilities

   [Server]
   private void processEnemySkills (List<BattlerData> battlersData) {
      List<BuffAbilityData> returnBuffAbilityList = new List<BuffAbilityData>();
      List<AttackAbilityData> returnAttackAbilityList = new List<AttackAbilityData>();
      List<BasicAbilityData> serverAbilities = new List<BasicAbilityData>(AbilityManager.self.allGameAbilities);

      foreach (BattlerData data in battlersData) {
         List<AttackAbilityData> attackAbilities = new List<AttackAbilityData>(data.battlerAbilities.attackAbilityDataList);
         foreach (AttackAbilityData abilityData in attackAbilities) {
            if (serverAbilities.Exists(_ => _.itemName == abilityData.itemName)) {
               returnAttackAbilityList.Add(abilityData);
            }
         }
         List<BuffAbilityData> buffAbilities = new List<BuffAbilityData>(data.battlerAbilities.buffAbilityDataList);
         foreach (BuffAbilityData abilityData in buffAbilities) {
            if (serverAbilities.Exists(_ => _.itemName == abilityData.itemName)) {
               returnBuffAbilityList.Add(abilityData);
            }
         }
      }

      Target_ReceiveAbilities(_player.connectionToClient, Util.serialize(returnAttackAbilityList), Util.serialize(returnBuffAbilityList));
   }

   [TargetRpc]
   public void Target_ReceiveAbilities (NetworkConnection connection, string[] rawAttackAbilities, string[] rawDebuffAbilities) {
      List<AttackAbilityData> attackAbilityDataList = Util.unserialize<AttackAbilityData>(rawAttackAbilities);
      List<BuffAbilityData> buffAbilityDataList = Util.unserialize<BuffAbilityData>(rawDebuffAbilities);

      AbilityManager.self.addNewAbilities(attackAbilityDataList.ToArray());
      AbilityManager.self.addNewAbilities(buffAbilityDataList.ToArray());
   }

   #endregion

   [TargetRpc]
   public void Target_ReceiveMonsterData (NetworkConnection connection, string[] rawData) {
      List<BattlerData> dataList = Util.unserialize<BattlerData>(rawData);
      MonsterManager.self.receiveListFromServer(dataList.ToArray());
   }

   [Command]
   public void Cmd_RequestBuff (uint netId, int abilityInventoryIndex) {
      if (_player == null || !(_player is PlayerBodyEntity)) {
         return;
      }

      // Look up the player's Battle object
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      Battle battle = BattleManager.self.getBattle(playerBody.battleId);
      Battler sourceBattler = battle.getBattler(_player.userId);

      // Get the ability from the battler abilities.
      BuffAbilityData abilityData = sourceBattler.getBuffAbilities()[abilityInventoryIndex];
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
      if (!abilityData.isReadyForUseBy(sourceBattler)) {
         D.debug("Battler requested to use ability they're not allowed: " + playerBody.entityName + ", " + abilityData.itemName);
         return;
      }

      // Let the Battle Manager handle executing the buff
      List<Battler> targetBattlers = new List<Battler>() { targetBattler };
      BattleManager.self.executeBattleAction(battle, sourceBattler, targetBattlers, abilityInventoryIndex, AbilityType.BuffDebuff);
   }

   [Command]
   public void Cmd_RequestAttack (uint netId, int abilityInventoryIndex) {
      if (_player == null || !(_player is PlayerBodyEntity)) {
         return;
      }

      // Look up the player's Battle object
      PlayerBodyEntity playerBody = (PlayerBodyEntity) _player;
      Battle battle = BattleManager.self.getBattle(playerBody.battleId);
      Battler sourceBattler = battle.getBattler(_player.userId);

      // Get the ability from the battler abilities.
      AttackAbilityData abilityData = sourceBattler.getAttackAbilities()[abilityInventoryIndex];
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
      if (!abilityData.isReadyForUseBy(sourceBattler)) {
         D.debug("Battler requested to use ability they're not allowed: " + playerBody.entityName + ", " + abilityData.itemName);
         return;
      }

      // If it's a Melee Ability, make sure the target isn't currently protected
      if (abilityData.isMelee() && targetBattler.isProtected(battle)) {
         D.warning("Battler requested melee ability against protected target! Player: " + playerBody.entityName);
         return;
      }

      // Let the Battle Manager handle executing the attack
      List<Battler> targetBattlers = new List<Battler>() { targetBattler };
      BattleManager.self.executeBattleAction(battle, sourceBattler, targetBattlers, abilityInventoryIndex, AbilityType.Standard);
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

      // Make sure the source battler can use that ability type
      if (!abilityData.isReadyForUseBy(sourceBattler)) {
         D.debug("Battler requested to use ability they're not allowed: " + playerBody.entityName + ", " + abilityData.itemName);
         return;
      }

      BattleManager.self.executeStanceChangeAction(battle, sourceBattler, newStance);
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

         string greetingText = shopData.shopGreetingText;

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.rpc.Target_ReceiveShipyard(_player.connectionToClient, gold, Util.serialize(list), greetingText);
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
                  item.setBasicInfo(weaponData.equipmentName, weaponData.equipmentDescription, weaponData.equipmentIconPath);
               }
               break;
            case Item.Category.Armor:
               ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
               if (armorData != null) {
                  item.setBasicInfo(armorData.equipmentName, armorData.equipmentDescription, armorData.equipmentIconPath);
               }
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

            string greetingText = shopData.shopGreetingText;
            _player.rpc.Target_ReceiveShopItems(_player.connectionToClient, gold, Util.serialize(sortedList), greetingText);
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
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);

            if (body != null && weaponData != null) {
               body.weaponManager.updateWeaponSyncVars(weapon, weaponData.actionType);
            }

            // Let the client know that we're done
            EquipMessage equipMessage = new EquipMessage(_player.netId, userObjects.armor.id, userObjects.weapon.id);
            NetworkServer.SendToClientOfPlayer(_player.netIdent, equipMessage);
         });
      });
   }

   [Server]
   private void sendError (string message) {
      if (UnityThreadHelper.IsMainThread) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, message);
      } else {
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, message);
         });
      }
   }

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   #endregion
}
