using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CropManager : NetworkBehaviour
{
   #region Public Variables

   // The Crop that we start out with
   public static Crop.Type STARTING_CROP = Crop.Type.Tomatoes;

   // The prefab we use for creating Crop instances
   public Crop cropPrefab;

   // The prefab we use for creating Crop harvest effects
   public GameObject cropHarvestEffectPrefab;

   // The Prefab we use to show an Icon inside of a Canvas when planting a crop
   public GameObject cropIconCanvasPrefab;

   // The number of crops that can be planted quickly during tutorial
   public const int TUTORIAL_CROP_COUNT = 24;

   #endregion

   void Awake () {
      _player = GetComponent<NetEntity>();
   }

   [Client]
   public void createCrop (CropInfo cropInfo, bool justGrew, bool showEffects, bool isQuickGrow) {
      CropSpot cropSpot = CropSpotManager.self.getCropSpot(cropInfo.cropNumber, cropInfo.areaKey);

      Crop crop = Instantiate(cropPrefab);
      if (cropSpot != null) {
         // If there was already a Crop here, delete it
         if (cropSpot.crop != null) {
            Destroy(cropSpot.crop.gameObject);
         }

         crop.transform.position = cropSpot.transform.position;
      }
      crop.growthLevel = cropInfo.growthLevel;
      crop.creationTime = cropInfo.creationTime;
      crop.setData(cropInfo.cropType, cropInfo.cropNumber, cropInfo.lastWaterTimestamp, cropInfo.areaKey);
      int growthLevel = Mathf.Min(cropInfo.growthLevel, Crop.getMaxGrowthLevel(crop.cropType));
      string spriteName = "crop_" + crop.cropType + "_" + growthLevel;
      crop.anim.setNewTexture(ImageManager.getTexture("Crops/" + spriteName));
      crop.name = "Crop " + crop.cropNumber + " [" + crop.cropType + "]";
      crop.waterInterval = cropInfo.waterInterval;
      crop.userId = cropInfo.userId;

      if (cropSpot == null) {
         crop.gameObject.SetActive(false);
         CropSpotManager.self.cropQueueList.Add(new CropQueueData {
            areaKey = cropInfo.areaKey,
            cropSpotNumber = cropInfo.cropNumber,
            crop = crop,
            showEffects = showEffects,
            justGrew = justGrew
         });
      } else {
         cropSpot.crop = crop;
         // Show some effects
         if (showEffects) {
            EffectManager.self.create(Effect.Type.Crop_Shine, cropSpot.transform.position);

            if (justGrew) {
               EffectManager.self.create(Effect.Type.Crop_Water, cropSpot.transform.position);

               // Play a sound
               //SoundManager.create3dSound("crop_water_", cropSpot.transform.position, 5);
            }
         }
      }
   }

   [Server]
   public static bool recentlySoldCrops (int userId) {
      // If there's no entry in our dictionary, they're fine
      if (!_lastSellTime.ContainsKey(userId)) {
         return false;
      }

      return (Time.time - _lastSellTime[userId] < 2.0f);
   }

   [Server]
   public void plantCrop (Crop.Type cropType, int cropNumber, string areaKey, int seedBagId, bool isSeedBagEquipped) {
      int userId = _player.userId;

      // Make sure there's not already a Crop in that spot
      foreach (CropInfo crop in _crops) {
         if (crop.cropNumber == cropNumber && crop.areaKey == areaKey) {
            // Do not print any error; Getting to this point means that crop is being currently registered in database in background thread
            return;
         }
      }

      // Make sure that it is farm map and this particular farm belongs to the user
      if (!AreaManager.self.isFarmOfUser(areaKey, userId) && !CustomGuildMapManager.canUserFarm(areaKey, _player)) {
         return;
      }

      // Make sure it's not already processing
      if (isCropProcessing(cropNumber)) {
         D.adminLog("Crop is already processing : " + cropNumber, D.ADMIN_LOG_TYPE.Crop);
         return;
      }

      // Prepare crop data
      long now = DateTime.UtcNow.ToBinary();
      CropsData cropData = CropsDataManager.self.getCropData(cropType);
      CropInfo cropInfo = new CropInfo(cropType, userId, cropNumber, now, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), (int) (cropData.minutesToRipe * 60));
      cropInfo.areaKey = areaKey;

      _cropsProcessing.Add(cropNumber);

      // Insert it into the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Remove a seed from the bag
         bool success = DB_Main.decreaseQuantityOrDeleteItem(userId, seedBagId, 1);

         // Stop the process if there were not enough seeds
         if (!success) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               onPlantCropEnd(cropNumber);
            });
            return;
         }

         // Plant the crop
         int newCropId = DB_Main.insertCrop(cropInfo, areaKey);
         if (newCropId <= 0) {
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               onPlantCropEnd(cropNumber);
            });
            return;
         }

         // If this was the last seed of an equipped bag, unequip it
         Item seedBag = DB_Main.getItem(userId, seedBagId);
         if (isSeedBagEquipped && (seedBag == null || seedBag.count == 0)) {
            _player.rpc.Bkg_RequestSetWeaponId(0);
         }

         // Add the farming XP
         int xp = Crop.getXP(cropType);
         DB_Main.addJobXP(userId, Jobs.Type.Farmer, xp);
         Jobs newJobXP = DB_Main.getJobXP(userId);
         List<AchievementData> harvestCropAchievements = DB_Main.getAchievementData(userId, ActionType.HarvestCrop);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Registers the planting action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player, ActionType.PlantCrop);

            // Checks achievements to determine if the plant will grow quickly for tutorial purposes
            bool quickGrow = false;
            if (harvestCropAchievements.Count < 1) {
               quickGrow = true;
            }
            foreach (AchievementData achievementData in harvestCropAchievements) {
               if (achievementData.count < TUTORIAL_CROP_COUNT) {
                  quickGrow = true;
               }
            }

            cropInfo = getUpdatedCropInfo(quickGrow, cropInfo);

            // Store the result
            _crops.Add(cropInfo);

            // Add the result to the crop manager of any other players in the instance
            if (_player != null) {
               Instance playerInstance = InstanceManager.self.getInstance(_player.instanceId);
               if (playerInstance != null) {
                  List<PlayerBodyEntity> players = playerInstance.getPlayerBodyEntities();
                  foreach (PlayerBodyEntity player in players) {
                     if (player.userId == _player.userId) {
                        continue;
                     }

                     player.cropManager.addNewCropInfo(cropInfo);
                  }
               }
            }

            if (_cropsProcessing.Contains(cropNumber)) {
               _cropsProcessing.Remove(cropNumber);
            }

            // Let them know they gained experience
            _player.Target_GainedFarmXp(_player.connectionToClient, xp, newJobXP);

            sendCropToPlayers(cropInfo, false, quickGrow);

            onPlantCropEnd(cropNumber);
         });
      });
   }

   [Server]
   public void addNewCropInfo (CropInfo cropInfo) {
      _crops.Add(cropInfo);
   }

   [Server]
   public void removeCropInfo (CropInfo cropInfo) {
      _crops.Remove(cropInfo);
   }

   private void onPlantCropEnd (int cropNumber) {
      if (_cropsProcessing.Contains(cropNumber)) {
         _cropsProcessing.Remove(cropNumber);
      }

      // Send the updated shortcuts to the client
      _player.rpc.sendItemShortcutList();
   }

   private void sendCropToPlayers (CropInfo cropInfo, bool justGrew, bool isQuickGrow = false) {
      D.adminLog("Player {" + _player.userId + "} just finished interacting with crop Level:{"
         + cropInfo.growthLevel + "} IsMax:{"
         + cropInfo.isMaxLevel() + "}", D.ADMIN_LOG_TYPE.Crop);

      // Send the new Crop to the players
      _player.Rpc_BroadcastUpdatedCrop(cropInfo, justGrew, isQuickGrow);
   }

   [Server]
   public void waterCrop (int cropNumber) {
      CropInfo cropToWater = new CropInfo();

      // Make sure there's a Crop in that spot
      foreach (CropInfo crop in _crops) {
         if (crop.cropNumber == cropNumber && crop.cropType != Crop.Type.None) {
            cropToWater = crop;
            break;
         }
      }

      if (cropToWater.cropType == Crop.Type.None) {
         D.adminLog("No crop in spot number: " + cropNumber, D.ADMIN_LOG_TYPE.Crop);
         return;
      }

      // Make sure it's ready for water
      if (!cropToWater.isReadyForWater()) {
         D.adminLog("Player {" + _player.userId + "} trying to water Crop, crop isn't ready for water: " + cropNumber, D.ADMIN_LOG_TYPE.Crop);
         return;
      }

      // Make sure it's not already maxed out
      if (cropToWater.isMaxLevel()) {
         D.adminLog("Crop can't grow any more: " + cropNumber, D.ADMIN_LOG_TYPE.Crop);
         return;
      }

      // Make sure it's not already processing
      if (isCropProcessing(cropNumber)) {
         D.adminLog("Crop is already processing : " + cropNumber, D.ADMIN_LOG_TYPE.Crop);
         return;
      }

      double startWaterTime = NetworkTime.time;
      D.adminLog("Processing water crop for player {" + _player.userId + "}", D.ADMIN_LOG_TYPE.Crop);

      _cropsProcessing.Add(cropNumber);

      // Update it in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.waterCrop(cropToWater);

         // Add the farming XP
         int xp = Crop.getXP(cropToWater.cropType);
         DB_Main.addJobXP(_player.userId, Jobs.Type.Farmer, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);
         List<AchievementData> farmPlayerHarvestCropAchievements = DB_Main.getAchievementData(cropToWater.userId, ActionType.HarvestCrop);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Registers the watering action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player, ActionType.WaterCrop);

            // Checks achievements to determine if the plant will grow quickly for tutorial purposes
            bool quickGrow = false;
            if (farmPlayerHarvestCropAchievements.Count < 1) {
               quickGrow = true;
            }
            foreach (AchievementData achievementData in farmPlayerHarvestCropAchievements) {
               if (achievementData.count < TUTORIAL_CROP_COUNT) {
                  quickGrow = true;
               }
            }
            if (quickGrow) {
               cropToWater.waterInterval = 3;
            }

            // Store the updated list
            CropInfo updatedCropToWater = increaseCropGrowthLevel(cropToWater);

            // Update the CropManagers of any other players currently in the instance
            if (_player != null) {
               Instance playerInstance = InstanceManager.self.getInstance(_player.instanceId);
               if (playerInstance != null) {
                  List<PlayerBodyEntity> players = playerInstance.getPlayerBodyEntities();
                  foreach (PlayerBodyEntity player in players) {
                     if (player.userId == _player.userId) {
                        continue;
                     }

                     player.cropManager.increaseCropGrowthLevel(cropToWater);
                  }
               }
            }

            if (_cropsProcessing.Contains(cropNumber)) {
               _cropsProcessing.Remove(cropNumber);
            }

            D.adminLog("Sending water crop for player {"
               + _player.userId
               + "} StartTime:{" + startWaterTime.ToString("f1")
               + "} EndTime:{" + NetworkTime.time.ToString("f1") + "} Duration is:{"
               + (NetworkTime.time - startWaterTime).ToString("f1") + "}", D.ADMIN_LOG_TYPE.Crop);

            // Let them know they gained experience
            _player.Target_GainedFarmXp(_player.connectionToClient, xp, newJobXP);

            sendCropToPlayers(updatedCropToWater, true, quickGrow);
         });
      });
   }

   private CropInfo increaseCropGrowthLevel (CropInfo cropToWater) {
      _crops.Remove(cropToWater);
      cropToWater.growthLevel++;
      cropToWater.lastWaterTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      _crops.Add(cropToWater);
      return cropToWater;
   }

   [Server]
   public void harvestCrop (int cropNumber) {
      int userId = _player.userId;
      CropInfo cropToHarvest = new CropInfo();

      // Make sure there's a Crop in that spot
      foreach (CropInfo crop in _crops) {
         if (crop.cropNumber == cropNumber && crop.cropType != Crop.Type.None) {
            cropToHarvest = crop;
         }
      }

      if (cropToHarvest.cropType == Crop.Type.None) {
         D.debug("No crop in spot number: " + cropNumber);
         return;
      }

      // Check that the player has permission to harvest the crop
      int guildId = CustomMapManager.getGuildId(cropToHarvest.areaKey);
      bool canHarvestCrop = (cropToHarvest.userId == _player.userId) || (guildId > 0 && guildId == _player.guildId);

      if (!canHarvestCrop) {
         return;
      }

      if (!cropToHarvest.isMaxLevel()) {
         D.error("Can't harvest crop that isn't fully grown: " + cropNumber);
         return;
      }

      if (!CropsDataManager.self.tryGetCropData(cropToHarvest.cropType, out CropsData cropData)) {
         D.error("Can't harvest crop, missing data: " + cropNumber + " " + cropToHarvest.cropType);
         return;
      }

      // Remove it from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteCrop(cropToHarvest.cropNumber, cropToHarvest.userId);

         // Add new crop to inventory
         Item itemToCreate = new Item {
            category = Item.Category.Crop,
            count = 1,
            itemTypeId = cropData.xmlId,
            durability = 100
         };

         DB_Main.createItemOrUpdateItemCount(userId, itemToCreate);

         // Add the farming XP
         int xp = Crop.getXP(cropToHarvest.cropType);
         DB_Main.addJobXP(_player.userId, Jobs.Type.Farmer, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Store the updated list
            _crops.Remove(cropToHarvest);

            // Remove the crop from the crop manager of any other players in the instance
            if (_player != null) {
               Instance playerInstance = InstanceManager.self.getInstance(_player.instanceId);
               if (playerInstance != null) {
                  List<PlayerBodyEntity> players = playerInstance.getPlayerBodyEntities();
                  foreach (PlayerBodyEntity player in players) {
                     if (player.userId == _player.userId) {
                        continue;
                     }

                     player.cropManager.removeCropInfo(cropToHarvest);
                  }
               }
            }

            // Registers the harvesting action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player, ActionType.HarvestCrop);
            AchievementManager.registerUserAchievement(_player, ActionType.GatherItem);

            // Let them know they gained experience
            _player.Target_GainedFarmXp(_player.connectionToClient, xp, newJobXP);

            // Let the player see the crop go away
            _player.Rpc_BroadcastHarvestedCrop(cropToHarvest);
         });
      });
   }

   [Server]
   public void sellCrops (int offerId, int amountToSell, Rarity.Type rarityToSellAt, int shopId) {
      // Make sure they aren't spamming requests
      if (recentlySoldCrops(_player.userId)) {
         D.log("Ignoring spam sell request from player: " + _player);
         return;
      }

      // Make sure the offer exists at the current area
      CropOffer offer = new CropOffer();

      if (shopId < 1) {
         D.error("A crop shop in area " + _player.areaKey + " has no defined shop name.");
      } else {
         foreach (CropOffer availableOffer in ShopManager.self.getOffersByShopId(shopId)) {
            if (availableOffer.id == offerId) {
               offer = availableOffer;
            }
         }
      }

      // Check if we found the specified offer
      if (offer.id <= 0) {
         D.warning("Couldn't find the requested crop offer: " + offerId);
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "This offer has expired!");
         return;
      }

      // Make sure the rarity hasn't changed
      if (offer.rarity != rarityToSellAt) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "The price of this crop has changed!");
         Target_CloseTradeScreenAndReloadCropOffers(_player.connectionToClient);
         return;
      }

      // Note the time
      _lastSellTime[_player.userId] = Time.time;

      int totalGoldMade = 0;

      // Look up some stuff before we hop into the background thread
      float xpModifier = Rarity.getXPModifier(offer.rarity);

      int earnedGold = 0;

      // To the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Grab their crop info
         List<Item> items = DB_Main.getItems(_player.userId, new Item.Category[] { Item.Category.Crop }, 0, 1000);

         foreach (Item item in items) {
            if (!CropsDataManager.self.tryGetCropData(item.itemTypeId, out CropsData cropsData)
            || (Crop.Type) cropsData.cropsType != offer.cropType || amountToSell <= 0 || amountToSell > item.count) {
               continue;
            }

            // Sell only up to the available demand in the offer
            if (!offer.isLowestRarity()) {
               amountToSell = amountToSell < offer.demand ? amountToSell : Mathf.CeilToInt(offer.demand);
            }

            int goldForThisCrop = offer.pricePerUnit * amountToSell;
            earnedGold = goldForThisCrop;

            // Add the gold to the database
            DB_Main.addGold(_player.userId, goldForThisCrop);

            // Remove the crops from their inventory
            DB_Main.decreaseQuantityOrDeleteItem(_player.userId, item.id, amountToSell);

            // Handle crop demand
            ShopManager.self.onUserSellCrop(shopId, offerId, amountToSell);

            // Keep a sum of the total gold
            totalGoldMade += goldForThisCrop;

            // Add experience
            int baseXP = Crop.getXP(offer.cropType) * amountToSell;
            int totalXP = (int) (baseXP * xpModifier);
            DB_Main.addJobXP(_player.userId, Jobs.Type.Trader, totalXP);
            Jobs jobs = DB_Main.getJobXP(_player.userId);

            // Find the flagship id
            UserInfo userInfo = DB_Main.getUserInfoById(_player.userId);
            int flagshipId = userInfo.flagshipId;

            // Add the exchange to the trade history
            TradeHistoryInfo tradeInfo = new TradeHistoryInfo(_player.userId, flagshipId, AreaManager.self.getArea(_player.areaKey).townAreaKey,
               offer.cropType, amountToSell, offer.pricePerUnit, goldForThisCrop, Crop.getXP(offer.cropType), totalXP, DateTime.UtcNow);
            DB_Main.addToTradeHistory(_player.userId, tradeInfo);

            // Back to Unity - add exp to the user
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               _player.Target_GainedXP(_player.connectionToClient, totalXP, jobs, Jobs.Type.Trader, 0, true);
            });
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (totalGoldMade > 0) {
               Target_JustSoldCrops(_player.connectionToClient, offer.cropType, totalGoldMade);

               // Registers the selling of crops action to the achievement database for recording
               AchievementManager.registerUserAchievement(_player, ActionType.SellCrop, amountToSell);

               // Registers the gold gains to the achievement database for recording
               AchievementManager.registerUserAchievement(_player, ActionType.EarnGold, earnedGold);
            } else {
               ErrorMessage errorMessage = new ErrorMessage(ErrorMessage.Type.NoCropsOfThatType);
               NetworkServer.SendToClientOfPlayer(_player.netIdentity, errorMessage);
            }
         });
      });
   }

   [Server]
   public void loadCrops (int userId) {

      // Get the crops for this player from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<CropInfo> cropList = DB_Main.getCropInfo(userId);
         List<AchievementData> harvestCropAchievements = DB_Main.getAchievementData(userId, ActionType.HarvestCrop);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

            // Checks achievements to determine if the plant will grow quickly for tutorial purposes
            bool quickGrow = false;
            if (harvestCropAchievements.Count < 1) {
               quickGrow = true;
            }
            foreach (AchievementData achievementData in harvestCropAchievements) {
               if (achievementData.count < TUTORIAL_CROP_COUNT) {
                  quickGrow = true;
                  break;
               }
            }

            // Modifies the water interval for crops
            cropList = getUpdatedCropArrayInfo(quickGrow, cropList);
            _crops = cropList;

            // Send it to the player
            this.Target_ReceiveCropArray(_player.connectionToClient, cropList.ToArray(), quickGrow);

            // Note that we're done loading them
            _cropsDoneLoading = true;
         });
      });
   }

   [Server]
   public void loadGuildCrops (int guildId) {

      // Get the crops for this player from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<CropInfo> cropList = DB_Main.getGuildCropInfo(guildId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _crops = cropList;

            // Send it to the player
            this.Target_ReceiveCropArray(_player.connectionToClient, cropList.ToArray(), false);

            // Note that we're done loading them
            _cropsDoneLoading = true;
         });
      });
   }

   private List<CropInfo> getUpdatedCropArrayInfo (bool quickGrow, List<CropInfo> cropList) {
      List<CropInfo> newCropInfoList = new List<CropInfo>();

      // Alter water interval for each crop info
      for (int i = 0; i < cropList.Count; i++) {
         CropInfo cropInfo = cropList[i];
         cropInfo = getUpdatedCropInfo(quickGrow, cropInfo);
         newCropInfoList.Add(cropInfo);
      }

      return newCropInfoList;
   }

   private CropInfo getUpdatedCropInfo (bool quickGrow, CropInfo cropInfo) {
      if (quickGrow) {
         // For tutorial purposes, set the water interval into 3 seconds
         cropInfo.waterInterval = 3;
      } else {
         // Crop water interval accepts seconds, the crop data registers minutes to ripe, so multiply by 60 to get the seconds 
         CropsData fetchedCropData = CropsDataManager.self.getCropData(cropInfo.cropType);
         cropInfo.waterInterval = fetchedCropData.minutesToRipe * 60;
      }
      return cropInfo;
   }

   public void receiveUpdatedCrop (CropInfo cropInfo, bool justGrew, bool isQuickGrow) {

      // Only try to trigger the tutorial for the player who owns the farm
      if (Global.player != null && _player.userId == Global.player.userId) {
         // Trigger the tutorial
         if (cropInfo.growthLevel == 0) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.PlantCrop);
         }

         if (cropInfo.isMaxLevel()) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.CropGrewToMaxLevel);
         }

         D.adminLog("Player {" + _player.userId + "} just finished interacting with crop Level:{"
        + cropInfo.growthLevel + "} IsMax:{"
        + cropInfo.isMaxLevel() + "}", D.ADMIN_LOG_TYPE.Crop);
      }

      createCrop(cropInfo, justGrew, true, isQuickGrow);
   }

   [TargetRpc]
   public void Target_ReceiveCropArray (NetworkConnection connection, CropInfo[] crops, bool quickGrow) {
      CropSpotManager.self.resetCropSpots();

      // Destroy any existing crops on the client
      foreach (Crop crop in FindObjectsOfType<Crop>()) {
         Destroy(crop.gameObject);
      }

      // Create the new list of crops
      foreach (CropInfo cropInfo in crops) {
         createCrop(cropInfo, false, false, quickGrow);
      }
   }

   [TargetRpc]
   public void Target_JustSoldCrops (NetworkConnection connection, Crop.Type cropType, int totalGold) {
      D.debug("You just sold your " + cropType + " for " + totalGold + " gold!");
      PanelManager.self.tradeConfirmScreen.hide();

      // Show a confirmation panel
      PanelManager.self.noticeScreen.show("You just sold your crops for " + totalGold + " gold!");

      // Play a sound (buy_sell was triggered here)
      SoundEffectManager.self.playBuySellSfx();

      // Updates the offers in the merchant panel
      Global.player.rpc.Cmd_GetCropOffersForShop(MerchantScreen.self.shopId);

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.SellCrops);
   }

   [TargetRpc]
   public void Target_CloseTradeScreenAndReloadCropOffers (NetworkConnection connection) {
      PanelManager.self.tradeConfirmScreen.hide();
      Global.player.rpc.Cmd_GetCropOffersForShop(MerchantScreen.self.shopId);
   }

   public static int getCropSellXP (Crop.Type cropType, string areaKey, int cropCount, CropOffer offer) {
      int baseXP = Crop.getXP(cropType) * cropCount;

      // For now, all Crops will be worth the same base XP, and the rarity of the offer will determine the final XP
      return (int) (baseXP * Rarity.getXPModifier(offer.rarity));

      /*int baseXP = Crop.getXP(cropType) * cropCount;

      switch (offer.rarity) {
         case Rarity.Type.Common:
            return baseXP;
         case Rarity.Type.Uncommon:
            return baseXP * 2;
         case Rarity.Type.Rare:
            return baseXP * 3;
         case Rarity.Type.Epic:
            return baseXP * 4;
         case Rarity.Type.Legendary:
            return baseXP * 5;
         default:
            return baseXP;
      }*/
   }

   public static int getBasePrice (Crop.Type cropType) {
      // For now, we'll have all crops equally valuable, and have the switching supply/demand be what adds variety and competition
      return 100;

      /*switch (cropType) {
         case Crop.Type.Carrots:
            return 4;
         case Crop.Type.Onions:
            return 12;
         case Crop.Type.Potatoes:
            return 20;
         default:
            return 1;
      }*/
   }

   private bool isCropProcessing (int cropNumber) {
      return _cropsProcessing.Contains(cropNumber);
   }

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   // Gets set to true once we've loaded our crops on the server
   protected bool _cropsDoneLoading = false;

   // The crops for this player
   protected List<CropInfo> _crops = new List<CropInfo>();

   // The time at which the specified user ID last sold something
   protected static Dictionary<int, float> _lastSellTime = new Dictionary<int, float>();

   // A list of crops that are currently processing watering or planting
   private List<int> _cropsProcessing = new List<int>();

   #endregion
}
