using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CropManager : NetworkBehaviour {
   #region Public Variables

   // The Crop that we start out with
   public static Crop.Type STARTING_CROP = Crop.Type.Tomatoes;

   // The prefab we use for creating Crop instances
   public Crop cropPrefab;

   // The prefab we use for creating Crop harvest effects
   public GameObject cropHarvestEffectPrefab;

   // The Prefab we use to show an Icon inside of a Canvas when planting a crop
   public GameObject cropIconCanvasPrefab;

   #endregion

   void Awake () {
      _player = GetComponent<NetEntity>();
   }

   [Client]
   public void createCrop (CropInfo cropInfo, bool justGrew, bool showEffects) {
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
      crop.waterInterval = cropInfo.waterInterval;
      crop.setData(cropInfo.cropType, cropInfo.cropNumber, cropInfo.lastWaterTimestamp);
      int growthLevel = Mathf.Min(cropInfo.growthLevel, Crop.getMaxGrowthLevel(crop.cropType));
      string spriteName = "crop_" + crop.cropType + "_" + growthLevel;
      crop.anim.setNewTexture(ImageManager.getTexture("Crops/" + spriteName));
      crop.name = "Crop " + crop.cropNumber + " [" + crop.cropType + "]";

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
               SoundManager.create3dSound("crop_water_", cropSpot.transform.position, 5);
            } else {
               EffectManager.self.create(Effect.Type.Crop_Harvest, cropSpot.transform.position);
               EffectManager.self.create(Effect.Type.Crop_Dirt_Large, cropSpot.transform.position);

               // Play a sound
               SoundManager.create3dSound("crop_plant_", cropSpot.transform.position, 5);
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
   public void plantCrop (Crop.Type cropType, int cropNumber, string areaKey) {
      int userId = _player.userId;
      int tutorialStep = TutorialManager.getHighestCompletedStep(userId);
      int waterInterval = getWaterIntervalSeconds(cropType, tutorialStep);

      // Make sure there's not already a Crop in that spot
      foreach (CropInfo crop in _crops) {
         if (crop.cropNumber == cropNumber && crop.areaKey == areaKey) {
            D.error("Already a crop in spot number: " + cropNumber);
            return;
         }
      }

      // Insert it into the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         long now = DateTime.UtcNow.ToBinary();

         CropInfo cropInfo = new CropInfo(cropType, userId, cropNumber, now, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), waterInterval);
         int newCropId = DB_Main.insertCrop(cropInfo, areaKey);
         cropInfo.areaKey = areaKey;

         // Add the farming XP
         int xp = Crop.getXP(cropType);
         DB_Main.addJobXP(userId, Jobs.Type.Farmer, xp);
         Jobs newJobXP = DB_Main.getJobXP(userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (newCropId > 0) {
               // Store the result
               _crops.Add(cropInfo);

               // Registers the planting action to the achievement database for recording
               AchievementManager.registerUserAchievement(_player.userId, ActionType.PlantCrop);

               // Send the new Crop to the player
               this.Target_ReceiveCrop(_player.connectionToClient, cropInfo, false);

               // Let them know they gained experience
               _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Farmer, cropNumber, true);
            } 
         });
      });
   }

   [Server]
   public void waterCrop (int cropNumber) {
      CropInfo cropToWater = new CropInfo();

      // Make sure there's a Crop in that spot
      foreach (CropInfo crop in _crops) {
         if (crop.cropNumber == cropNumber && crop.cropType != Crop.Type.None) {
            cropToWater = crop;
         }
      }

      if (cropToWater.cropType == Crop.Type.None) {
         D.error("No crop in spot number: " + cropNumber);
         return;
      }

      // Make sure it's ready for water
      if (!cropToWater.isReadyForWater()) {
         D.error("Crop isn't ready for water: " + cropNumber);
         return;
      }

      // Make sure it's not already maxed out
      if (cropToWater.isMaxLevel()) {
         D.error("Crop can't grow any more: " + cropNumber);
         return;
      }

      // Update it in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.waterCrop(cropToWater);

         // Add the farming XP
         int xp = Crop.getXP(cropToWater.cropType);
         DB_Main.addJobXP(_player.userId, Jobs.Type.Farmer, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Store the updated list
            _crops.Remove(cropToWater);
            cropToWater.growthLevel++;
            cropToWater.lastWaterTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _crops.Add(cropToWater);

            // Registers the watering action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player.userId, ActionType.WaterCrop);

            // Send the update Crop to the player
            this.Target_ReceiveCrop(_player.connectionToClient, cropToWater, true);

            // Let them know they gained experience
            _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Farmer, cropNumber, true);
         });
      });
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

      if (!cropToHarvest.isMaxLevel()) {
         D.error("Can't harvest crop that isn't fully grown: " + cropNumber);
         return;
      }

      // Remove it from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteCrop(cropToHarvest.cropNumber, cropToHarvest.userId);

         // Add it to their silo
         DB_Main.addToSilo(userId, cropToHarvest.cropType);

         // Grab their latest silo info
         List<SiloInfo> siloInfo = DB_Main.getSiloInfo(userId);

         // Add the farming XP
         int xp = Crop.getXP(cropToHarvest.cropType);
         DB_Main.addJobXP(_player.userId, Jobs.Type.Farmer, xp);
         Jobs newJobXP = DB_Main.getJobXP(_player.userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Store the updated list
            _crops.Remove(cropToHarvest);

            // Registers the harvesting action to the achievement database for recording
            AchievementManager.registerUserAchievement(_player.userId, ActionType.HarvestCrop);

            // Let the player see the crop go away
            this.Target_HarvestCrop(_player.connectionToClient, cropToHarvest);

            // Send their new silo info
            _player.Target_ReceiveSiloInfo(_player.connectionToClient, siloInfo.ToArray());

            // Let them know they gained experience
            _player.Target_GainedXP(_player.connectionToClient, xp, newJobXP, Jobs.Type.Farmer, cropNumber, true);
         });
      });
   }

   [Server]
   public void sellCrops (int offerId, int amountToSell) {
      // Make sure they aren't spamming requests
      if (recentlySoldCrops(_player.userId)) {
         D.log("Ignoring spam sell request from player: " + _player);
         return;
      }

      // Make sure the offer exists at the current area
      CropOffer offer = new CropOffer();

      foreach (CropOffer availableOffer in ShopManager.self.getOffers(_player.areaKey)) {
         if (availableOffer.id == offerId) {
            offer = availableOffer;
         }
      }

      // Check if we found the specified offer
      if (offer.id <= 0) {
         D.warning("Couldn't find the requested crop offer: " + offerId);
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "This offer has expired!");
         return;
      }

      // Make sure it hasn't sold out
      if (offer.amount <= 0) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, _player, "This offer has sold out!");
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
         // Grab their latest silo info
         List<SiloInfo> siloInfo = DB_Main.getSiloInfo(_player.userId);

         foreach (SiloInfo info in siloInfo) {
            if (info.cropType != offer.cropType || info.cropCount <= 0 || amountToSell <= 0 || amountToSell > info.cropCount) {
               continue;
            }

            // Sell only up to the available amount in the offer
            amountToSell = amountToSell < offer.amount ? amountToSell : offer.amount;

            int goldForThisCrop = offer.pricePerUnit * amountToSell;
            earnedGold = goldForThisCrop;

            // Add the gold to the database
            DB_Main.addGold(_player.userId, goldForThisCrop);

            // Remove the crops from their silo
            DB_Main.addToSilo(_player.userId, info.cropType, -amountToSell);

            // Remove the amount of crops from the offer
            ShopManager.self.decreaseOfferCount(offerId, amountToSell);

            // Keep a sum of the total gold
            totalGoldMade += goldForThisCrop;

            // Add experience
            int baseXP = Crop.getXP(offer.cropType) * amountToSell;
            int totalXP = (int) (baseXP * xpModifier);
            DB_Main.addJobXP(_player.userId, Jobs.Type.Trader, totalXP);
            Jobs jobs = DB_Main.getJobXP(_player.userId);
            _player.Target_GainedXP(_player.connectionToClient, totalXP, jobs, Jobs.Type.Trader, 0, true);

            // Find the flagship id
            string userInfoJson = DB_Main.getUserInfoJSON(_player.userId.ToString());
            int flagshipId = JsonUtility.FromJson<UserInfo>(userInfoJson).flagshipId;

            // Add the exchange to the trade history
            TradeHistoryInfo tradeInfo = new TradeHistoryInfo(_player.userId, flagshipId, AreaManager.self.getArea(_player.areaKey).townAreaKey,
               offer.cropType, amountToSell, offer.pricePerUnit, goldForThisCrop, Crop.getXP(offer.cropType), totalXP, DateTime.UtcNow);
            DB_Main.addToTradeHistory(_player.userId, tradeInfo);
         }
         
         // Send them the new info on what's in their silo
         sendSiloInfo();

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (totalGoldMade > 0) {
               Target_JustSoldCrops(_player.connectionToClient, offer.cropType, totalGoldMade);

               // Registers the selling of crops action to the achievement database for recording
               AchievementManager.registerUserAchievement(_player.userId, ActionType.SellCrop, amountToSell);

               // Registers the gold gains to the achievement database for recording
               AchievementManager.registerUserAchievement(_player.userId, ActionType.EarnGold, earnedGold);
            } else {
               ErrorMessage errorMessage = new ErrorMessage(_player.netId, ErrorMessage.Type.NoCropsOfThatType);
               NetworkServer.SendToClientOfPlayer(_player.netIdentity, errorMessage);
            }
         });
      });
   }

   [Server]
   public void loadCrops () {
      int userId = _player.userId;

      // Get the crops for this player from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<CropInfo> cropList = DB_Main.getCropInfo(userId);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Store the result
            _crops = cropList;

            // Send it to the player
            this.Target_ReceiveCrops(_player.connectionToClient, cropList.ToArray());

            // Note that we're done loading them
            _cropsDoneLoading = true;
         });
      });
   }

   [Server]
   public void sendSiloInfo () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<SiloInfo> siloInfo = DB_Main.getSiloInfo(_player.userId);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            _player.Target_ReceiveSiloInfo(_player.connectionToClient, siloInfo.ToArray());
         });
      });
   }

   [TargetRpc]
   public void Target_HarvestCrop (NetworkConnection connection, CropInfo cropInfo) {
      CropSpot cropSpot = CropSpotManager.self.getCropSpot(cropInfo.cropNumber, cropInfo.areaKey);
      Vector3 effectSpawnPos = cropSpot.cropPickupLocation;

      // Show some effects
      EffectManager.self.create(Effect.Type.Crop_Harvest, effectSpawnPos);
      EffectManager.self.create(Effect.Type.Crop_Shine, effectSpawnPos);
      EffectManager.self.create(Effect.Type.Crop_Dirt_Large, effectSpawnPos);

      // Play a sound
      SoundManager.create3dSound("crop_harvest_", effectSpawnPos, 5);

      // Show a floating icon
      GameObject harvestEffect = Instantiate(cropHarvestEffectPrefab, effectSpawnPos + new Vector3(0f, .16f), Quaternion.identity);
      harvestEffect.GetComponent<SimpleAnimation>().setNewTexture(ImageManager.getTexture("Crops/" + cropInfo.cropType + "_strip"));
      // GameObject iconCanvas = Instantiate(cropIconCanvasPrefab, cropSpot.transform.position + new Vector3(0f, .16f), Quaternion.identity);
      // iconCanvas.GetComponentInChildren<Image>().sprite = Resources.Load<Sprite>("Crops/" + cropInfo.cropType + "-Full");

      // Then delete the crop
      if (cropSpot.crop != null) {
         Destroy(cropSpot.crop.gameObject);
      }

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.HarvestCrop);
   }

   [TargetRpc]
   public void Target_ReceiveCrop (NetworkConnection connection, CropInfo cropInfo, bool justGrew) {
      // Trigger the tutorial
      if (cropInfo.growthLevel == 0) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.PlantCrop);
      }

      if (cropInfo.isMaxLevel()) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.CropGrewToMaxLevel);
      }

      createCrop(cropInfo, justGrew, true);
   }

   [TargetRpc]
   public void Target_ReceiveCrops (NetworkConnection connection, CropInfo[] crops) {
      CropSpotManager.self.resetCropSpots();

      // Destroy any existing crops on the client
      foreach (Crop crop in FindObjectsOfType<Crop>()) {
         Destroy(crop.gameObject);
      }

      // Create the new list of crops
      foreach (CropInfo cropInfo in crops) {
         createCrop(cropInfo, false, false);
      }
   }

   [TargetRpc]
   public void Target_JustSoldCrops (NetworkConnection connection, Crop.Type cropType, int totalGold) {
      D.debug("You just sold your " + cropType + " for " + totalGold + " gold!");
      PanelManager.self.tradeConfirmScreen.hide();

      // Show a confirmation panel
      PanelManager.self.noticeScreen.show("You just sold your crops for " + totalGold + " gold!");

      // Play a sound
      SoundManager.create3dSound("ui_buy_sell", Global.player.transform.position);

      // If the player was on the sell step, they just completed it
      if (TutorialManager.self.currentTutorialData().actionType == ActionType.SellCrop) {
         Global.player.Cmd_CompletedTutorialStep(TutorialManager.currentStep);
      }

      // Updates the offers in the merchant panel
      Global.player.rpc.Cmd_GetOffersForArea(ShopManager.DEFAULT_SHOP_NAME);
   }

   protected static int getWaterIntervalSeconds (Crop.Type cropType, int highestCompletedTutorialStep) {
      // When they're just getting started, make the crops grow fast
      try {
         TutorialData tutorialData = TutorialManager.self.fetchTutorialData(highestCompletedTutorialStep);
         if (tutorialData.actionType == ActionType.WaterCrop && tutorialData.countRequirement == 1) {
            return 6;
         }
      } catch {
         D.debug("Tutorial Manager failed to process");
      }

      CropsData cropData = CropsDataManager.self.getCropData(cropType);
      return (int)cropData.growthRate;
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

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   // Gets set to true once we've loaded our crops on the server
   protected bool _cropsDoneLoading = false;

   // The crops for this player
   protected List<CropInfo> _crops = new List<CropInfo>();

   // The time at which the specified user ID last sold something
   protected static Dictionary<int, float> _lastSellTime = new Dictionary<int, float>();

   #endregion
}
