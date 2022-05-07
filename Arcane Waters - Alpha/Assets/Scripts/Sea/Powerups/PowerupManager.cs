﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using System;
using System.Linq;

public class PowerupManager : MonoBehaviour {
   #region Public Variables

   // Powerup data populated in the inspector, to be replaced with fetching from the database eventually
   public List<PowerupData> powerupData;

   // Singleton instance
   public static PowerupManager self;

   // List of powerups that will expire
   public List<ExpiringPowerups> temporaryPowerups = new List<ExpiringPowerups>();

   // The auto incremented powerup index
   int expiredPowerupIndex = 0;

   [Serializable]
   public class ExpiringPowerups {
      // The autogenerated id
      public int id;

      // The user id
      public int userId;

      // The powerup type and rarity reference
      public Powerup.Type powerupType;
      public Rarity.Type rarityType;

      // Cache the remaining time instead of altering the time in the powerup class to prevent list search
      public float remainingTime;
   }

   #endregion

   private void Awake () {
      self = this;

      loadPowerupData();
      InvokeRepeating(nameof(checkExpiringPowerups), 0, 1);
   }

   private void checkExpiringPowerups () {
      List<int> clearedPowerupEntries = new List<int>();
      foreach (ExpiringPowerups powerupEntry in temporaryPowerups) {
         if (_serverPlayerPowerups.ContainsKey(powerupEntry.userId)) {
            if (powerupEntry.remainingTime < 1) {
               // Cache the player powerup data
               PlayerPowerups entry = _serverPlayerPowerups[powerupEntry.userId];

               // Check if the expiring powerup exists in the collection of the player data
               if (entry.ContainsKey(powerupEntry.powerupType)) {
                  // Cache the powerup list of the server player entry
                  List<Powerup> powerupList = entry[powerupEntry.powerupType];

                  // Attempt to fetch the powerup entry that matches the powerup type and rarity
                  Powerup powerup = powerupList.Find(_ => _.powerupType == powerupEntry.powerupType && _.powerupRarity == powerupEntry.rarityType);
                  if (powerup != null) {
                     clearedPowerupEntries.Add(powerupEntry.id);
                  }
               }
            } else {
               // Deduct the remaining time of the expiring powerup
               powerupEntry.remainingTime--;
            }
         }
      }

      // Remove powerups that have expired
      foreach (int expiredPowerupId in clearedPowerupEntries) {
         ExpiringPowerups discardablePowerups = temporaryPowerups.Find(_ => _.id == expiredPowerupId);
         if (discardablePowerups != null) {
            temporaryPowerups.Remove(discardablePowerups);

            NetEntity player = EntityManager.self.getEntity(discardablePowerups.userId);
            Powerup powerUp = new Powerup {
                  powerupType = discardablePowerups.powerupType, 
                  powerupRarity = discardablePowerups.rarityType
            };

            if (player.isLocalPlayer) {
               removePowerupServer(player.userId, powerUp);
               player.rpc.Target_RemovePowerup(player.connectionToClient, powerUp);
            } 
         }
      }
   }

   private void loadPowerupData () {
      foreach (PowerupData data in powerupData) {
         Powerup.Type type = (Powerup.Type) data.powerupType;
         _powerupData[type] = data;
      }
   }

   public float getPowerupMultiplier (Powerup.Type type) {
      if (!_localPlayerPowerups.ContainsKey(type)) {
         return 1.0f;
      }

      return getTotalBoostFactor(_localPlayerPowerups[type]);
   }
   
   public float getPowerupMultiplierAdditive (Powerup.Type type) {
      return getPowerupMultiplier(type) - 1.0f;
   }

   public float getPowerupMultiplier (int userId, Powerup.Type type) {
      // Print a warning if this is called on a non-server
      if (!NetworkServer.active) {
         D.warning("getPowerupMultiplier is being called on a non-server client, it should only be called on the server");
         return 1.0f;
      }

      // If we don't have the user in our dictionary, return 1
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         return 1.0f;
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];

      // If we don't have a value for this powerup, return 1
      if (!powerups.ContainsKey(type)) {
         return 1.0f;
      }

      return getTotalBoostFactor(powerups[type]);
   }

   public float getPowerupMultiplierAdditive (int userId, Powerup.Type type) {
      return getPowerupMultiplier(userId, type) - 1.0f;
   }

   private float getTotalBoostFactor (List<Powerup> powerups) {
      float boostFactor = 1.0f;

      if (powerups == null || powerups.Count < 1) {
         return boostFactor;
      }
      PowerupData data = getPowerupData(powerups[0].powerupType);

      foreach (Powerup powerup in powerups) {
         boostFactor += data.rarityBoostFactors[(int)powerup.powerupRarity];
      }

      return boostFactor;
   }

   private IEnumerator CO_GrabPowerupEffect (Powerup.Type powerupType, Rarity.Type powerupRarity, PlayerShipEntity player, Vector3 spawnSource) {
      // Create the popup icon, make it scale up in size
      PowerupPopupIcon popupIcon = Instantiate(TreasureManager.self.powerupPopupIcon, Vector3.zero, Quaternion.identity).GetComponent<PowerupPopupIcon>();
      popupIcon.transform.SetParent(AreaManager.self.getArea(player.areaKey).transform);
      popupIcon.transform.position = new Vector3(spawnSource.x, spawnSource.y, player.transform.position.z);
      popupIcon.init(powerupType, powerupRarity);
      popupIcon.transform.localScale = Vector3.one * 0.25f;
      popupIcon.transform.DOScale(1.0f, 0.8f).SetEase(Ease.InElastic);
      yield return new WaitForSeconds(0.4f);

      // Play sfx
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.PICKUP_POWERUP, position: spawnSource);
      
      // After a delay, have the popup icon move upwards
      popupIcon.transform.DOBlendableLocalMoveBy(Vector3.up * 0.3f, 0.4f).SetEase(Ease.OutSine);
      yield return new WaitForSeconds(1.4f);

      // After another delay, have the popup icon move towards the player
      popupIcon.gravitateToPlayer(player, 1.0f);

      // Show a confirmation in chat
      string powerupName = PowerupManager.self.getPowerupData(powerupType).powerupName;
      string msg = string.Format("You received the <color=red>{0}</color> powerup!", powerupName);
      ChatManager.self.addChat(msg, ChatInfo.Type.System);
   }

   public IEnumerator CO_CreatingUniqueFloatingPowerupIcon (Powerup.Type powerupType, Rarity.Type rarity, PlayerShipEntity entity, Vector3 spawnSource) {
      // Spawn powerup and do a vacuum effect going to player ship
      yield return CO_GrabPowerupEffect(powerupType, rarity, entity, spawnSource);
      
      // Remove existing powerup type to ensure powerup with same type don't exist 
      removePowerupTypeClient(powerupType);
      
      // Add new powerup to client
      addPowerupClient(new Powerup {
         powerupRarity = rarity,
         powerupType = powerupType
      });
   }

   public IEnumerator CO_CreatingFloatingPowerupIcon (Powerup.Type powerupType, Rarity.Type powerupRarity, PlayerShipEntity player, Vector3 spawnSource) {
      // Spawn powerup and do a vacuum effect going to player ship
      yield return CO_GrabPowerupEffect(powerupType, powerupRarity, player, spawnSource);

      // Add new powerup to client
      addPowerupClient(new Powerup {
         powerupRarity = powerupRarity,
         powerupType = powerupType
      });
   }

   private float getBoostFactor (Powerup.Type type, Rarity.Type rarity) {
      return getPowerupData(type).rarityBoostFactors[(int)rarity] + 1.0f;
   }

   public PowerupData getPowerupData (Powerup.Type type) {
      if (!_powerupData.ContainsKey(type)) {
         D.error("Couldn't find local data for powerup of type: " + type.ToString());
      }

      return _powerupData[type];
   }

   // Remove entire powerup added with type
   public void removePowerupTypeClient (Powerup.Type powerupType) {
      if (!NetworkClient.active) {
         D.error("addPowerupClient should only be called on a client");
         return;
      }

      // If we don't have a list for that powerup type yet, create it
      if (!_localPlayerPowerups.ContainsKey(powerupType)) {
         return;
      }

      var powerups = _localPlayerPowerups[powerupType];
      foreach (var powerup in powerups) {
         // Remove powerup icon added in panel
         PowerupPanel.self.removePowerup(powerup.powerupType, powerup.powerupRarity);
      }
      
      // Clear entire list of powerup with type
      powerups.Clear();
   }
   
   public void removePowerupClient (Powerup powerup) {
      if (!NetworkClient.active) {
         D.error("addPowerupClient should only be called on a client");
         return;
      }

      // If we don't have a list for that powerup type yet, create it
      Powerup.Type powerupType = powerup.powerupType;
      if (!_localPlayerPowerups.ContainsKey(powerupType)) {
         return;
      }

      if (_localPlayerPowerups[powerupType].Count > 0) {
         Powerup powerupToRemove = _localPlayerPowerups[powerupType].Find(item => item.powerupRarity == powerup.powerupRarity);
         _localPlayerPowerups[powerupType].Remove(powerupToRemove);
         PowerupPanel.self.removePowerup(powerup.powerupType, powerup.powerupRarity);
      }
   }

   public void addPowerupClient (Powerup newPowerup) {
      if (!NetworkClient.active) {
         return;
      }

      // If we don't have a list for that powerup type yet, create it
      Powerup.Type powerupType = newPowerup.powerupType;
      if (!_localPlayerPowerups.ContainsKey(powerupType)) {
         _localPlayerPowerups.Add(powerupType, new List<Powerup>());
      }

      _localPlayerPowerups[powerupType].Add(newPowerup);
      PowerupPanel.self.addPowerup(newPowerup.powerupType, newPowerup.powerupRarity, Global.player.getPlayerShipEntity());

      if (newPowerup.powerupType == Powerup.Type.IncreasedHealth) {
         PlayerShipEntity playerShip = Global.player.getPlayerShipEntity();
         if (playerShip) {
            playerShip.shipBars.initializeHealthBar();
         }
      }
   }

   public void removePowerupServer (int userId, Powerup powerup) {
      if (!NetworkServer.active) {
         return;
      }

      // If we haven't created an entry for this player yet, create one
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         return;
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];

      // If we don't have a list for that powerup type yet, create it;
      if (!powerups.ContainsKey(powerup.powerupType)) {
         return;
      }

      // If the player has health bonus powerup deduction, deduct their health immediately
      if (powerup.powerupType == Powerup.Type.IncreasedHealth) {
         NetEntity player = EntityManager.self.getEntity(userId);
         if (player) {
            PlayerShipEntity playerShip = player.getPlayerShipEntity();
            if (playerShip) {
               Rarity.Type rarity = powerup.powerupRarity;
               int healthBlockTier = playerShip.shipBars.getHealthBlockTier();
               int hpPerBlock = ShipHealthBlock.HP_PER_BLOCK[healthBlockTier];
               playerShip.applyBonusHealth(- ShipBars.getHealthBlockPerRarity(rarity, hpPerBlock), false);
               playerShip.shipBars.initializeHealthBar();
            }
         }
      }

      // If there is an existing powerup entry, deduct one entry
      if (powerups[powerup.powerupType].Count > 0) {
         var targetPowerup = powerups[powerup.powerupType].Find(item => item.powerupRarity == powerup.powerupRarity);
         powerups[powerup.powerupType].Remove(targetPowerup);
      }
      
      updatePowerupSyncListForUser(userId);
   }

   public void addReplacePowerupServer (int userId, Powerup powerup) {
      if (!NetworkServer.active) {
         D.error("addPowerupServer should only be called on the server");
         return;
      }
      
      // If we haven't created an entry for this player yet, use addPowerupServer method instead
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         addPowerupServer(userId, powerup);
         return;
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];
      
      // If we don't have a list for that powerup type yet, use addPowerupServer method instead
      if (!powerups.ContainsKey(powerup.powerupType)) {
         addPowerupServer(userId, powerup);
         return;
      }

      // If player has received a health bonus powerup, use addPowerupServer method instead
      if (powerup.powerupType == Powerup.Type.IncreasedHealth) {
         addPowerupServer(userId, powerup);
         return;
      }
      
      // If there is an existing powerup entry, clear entry
      if (powerups[powerup.powerupType].Count > 0) {
         powerups[powerup.powerupType].Clear();
      }

      powerups[powerup.powerupType].Add(powerup);

      // Add temporary powerup to list
      if (powerup.powerupDuration > 0) {
         // If powerup type doesn't exist create an entry of powerup to expiring powerup list
         if (!temporaryPowerups.Exists(item => item.powerupType == powerup.powerupType)) {
            expiredPowerupIndex++;
            temporaryPowerups.Add(new ExpiringPowerups {
                  id = expiredPowerupIndex,
                  userId = userId,
                  powerupType = powerup.powerupType,
                  rarityType = powerup.powerupRarity,
                  remainingTime = powerup.powerupDuration
            });            
         } else {
            // If powerup type exist edit existing entry rarity and duration instead
            ExpiringPowerups tempPowerup = temporaryPowerups.Find(item => item.powerupType == Powerup.Type.SpeedUp);
            tempPowerup.remainingTime = powerup.powerupDuration;
            tempPowerup.rarityType = powerup.powerupRarity;
         }
      }

      updatePowerupSyncListForUser(userId);
   }

   public void addPowerupServer (int userId, Powerup newPowerup) {
      if (!NetworkServer.active) {
         D.error("addPowerupServer should only be called on the server");
         return;
      }

      // If we haven't created an entry for this player yet, create one
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         _serverPlayerPowerups.Add(userId, new PlayerPowerups());
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];

      // If we don't have a list for that powerup type yet, create it;
      if (!powerups.ContainsKey(newPowerup.powerupType)) {
         powerups.Add(newPowerup.powerupType, new List<Powerup>());
      }

      // If the player has received a health bonus powerup, change their health immediately
      if (newPowerup.powerupType == Powerup.Type.IncreasedHealth) {
         NetEntity player = EntityManager.self.getEntity(userId);
         if (player) {
            PlayerShipEntity playerShip = player.getPlayerShipEntity();
            if (playerShip) {
               Rarity.Type rarity = newPowerup.powerupRarity;
               int healthBlockTier = playerShip.shipBars.getHealthBlockTier();
               int hpPerBlock = ShipHealthBlock.HP_PER_BLOCK[healthBlockTier];
               playerShip.applyBonusHealth(ShipBars.getHealthBlockPerRarity(rarity, hpPerBlock), ShipBars.ifRarityAddsCurrentHealth(rarity));
               playerShip.shipBars.initializeHealthBar();
            }
         }
      }

      powerups[newPowerup.powerupType].Add(newPowerup);

      // Add temporary powerup to list
      if (newPowerup.powerupDuration > 0) {
         expiredPowerupIndex++;
         temporaryPowerups.Add(new ExpiringPowerups {
               id = expiredPowerupIndex,
               userId = userId,
               powerupType = newPowerup.powerupType,
               rarityType = newPowerup.powerupRarity,
               remainingTime = newPowerup.powerupDuration
         });
      }

      updatePowerupSyncListForUser(userId);
   }

   public bool powerupActivationRoll (int userId, Powerup.Type type) {
      // Performs a random roll to see if a powerup will activate, based on the points the user has in the powerup
      return (UnityEngine.Random.Range(0.0f, 1.0f) <= getPowerupMultiplierAdditive(userId, type));
   }

   public List<CannonballEffector> getEffectors (int userId) {
      List<CannonballEffector> effectors = new List<CannonballEffector>();

      // If this user doesn't have powerups stored, return an empty list
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         return effectors;
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];

      // Get an effector for each type of powerup
      foreach (List<Powerup> powerupsOfType in powerups.Values) {
         CannonballEffector newEffector = getEffector(powerupsOfType);
         if (newEffector != null) {
            effectors.Add(newEffector);
         }
      }

      return effectors;
   }

   private CannonballEffector getEffector (List<Powerup> powerups) {
      if (powerups.Count < 1) {
         return null;
      }

      Powerup.Type type = powerups[0].powerupType;
      float totalBoostFactor = getTotalBoostFactor(powerups);
      float totalBoostFactorAdditive = totalBoostFactor - 1.0f;
      
      switch (type) {
         case Powerup.Type.FireShots:
            return new CannonballEffector(CannonballEffector.Type.Fire, FIRE_BASE_DAMAGE * totalBoostFactor, duration: FIRE_BASE_DURATION);
         case Powerup.Type.ElectricShots:
            return new CannonballEffector(CannonballEffector.Type.Electric, ELECTRIC_BASE_DAMAGE, range: ELECTRIC_BASE_RANGE * totalBoostFactor);
         case Powerup.Type.IceShots:
            return new CannonballEffector(CannonballEffector.Type.Ice, ICE_BASE_STRENGTH * totalBoostFactorAdditive, duration: ICE_BASE_DURATION);
         case Powerup.Type.ExplosiveShots:
            return new CannonballEffector(CannonballEffector.Type.Explosion, EXPLOSIVE_BASE_DAMAGE * totalBoostFactor, range: EXPLOSIVE_BASE_RANGE * totalBoostFactor);
         case Powerup.Type.BouncingShots:
            return new CannonballEffector(CannonballEffector.Type.Bouncing, totalBoostFactorAdditive, range: BOUNCING_BASE_RANGE + totalBoostFactorAdditive);
         default:
            return null;
      }
   }

   public void clearPowerupsForUser (int userId) {
      if (_serverPlayerPowerups.ContainsKey(userId)) {
         _serverPlayerPowerups.Remove(userId);
         updatePowerupSyncListForUser(userId);
      } else if (EntityManager.self.getEntity(userId) == null) {
         // If the user is not connected to this server, try to find him in another
         ServerNetworkingManager.self.clearPowerupsForUser(userId);
      }
   }

   public List<Powerup> getPowerupsForUser (int userId) {
      List<Powerup> powerups = new List<Powerup>();

      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         return powerups;
      }

      foreach (List<Powerup> powerupList in _serverPlayerPowerups[userId].Values) {
         powerups.AddRange(powerupList);
      }

      return powerups;
   }

   public void awardRandomPowerupToUser (int userId) {
      Powerup.Type type = Util.randomEnumStartAt<Powerup.Type>(1);
      Rarity.Type rarity = Rarity.getRandom();

      Powerup newPowerup = new Powerup(type, rarity, Powerup.Expiry.None);
      addPowerupServer(userId, newPowerup);
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player) {
         player.rpc.Target_AddPowerup(player.connectionToClient, newPowerup);
      }
   }

   public void notePlayerUsedPowerup (int userId, Powerup.Type powerupType) {      
      // If the user isn't present in the dictionary yet, add them
      if (!_lastPowerupActivationTimesByUser.ContainsKey(userId)) {
         _lastPowerupActivationTimesByUser.Add(userId, new Dictionary<Powerup.Type, float>());
      }
      Dictionary<Powerup.Type, float> lastPowerupActivationTimes = _lastPowerupActivationTimesByUser[userId];

      lastPowerupActivationTimes[powerupType] = (float) NetworkTime.time;
   }

   public bool canPlayerUsePowerup (int userId, Powerup.Type powerupType) {
      // If this isn't a player, return true
      if (userId <= 0) {
         return true;
      }

      // If the user isn't present in the dictionary yet, then they can use the powerup
      if (!_lastPowerupActivationTimesByUser.ContainsKey(userId)) {
         return true;
      }

      Dictionary<Powerup.Type, float> lastPowerupActivationTimes = _lastPowerupActivationTimesByUser[userId];

      // If the powerup isn't present in the dictionary yet, then they can use the powerup
      if (!lastPowerupActivationTimes.ContainsKey(powerupType)) {
         return true;
      }

      float minimumTimeBetweenActivations = _powerupData[powerupType].minimumTimeBetweenActivations;
      float timeSincePowerupUsed = ((float) NetworkTime.time) - lastPowerupActivationTimes[powerupType];
      return timeSincePowerupUsed > minimumTimeBetweenActivations;
   }

   private void updatePowerupSyncListForUser (int userId) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player) {
         PlayerShipEntity playerShip = player.getPlayerShipEntity();
         if (playerShip) {
            playerShip.setPowerups(getPowerupsForUser(userId));
         }
      }
   }

   public static Powerup.Type getPowerupTypeFromEffectorType (CannonballEffector.Type effectorType) {
      switch (effectorType) {
         case CannonballEffector.Type.Bouncing:
            return Powerup.Type.BouncingShots;
         case CannonballEffector.Type.Electric:
            return Powerup.Type.ElectricShots;
         case CannonballEffector.Type.Explosion:
            return Powerup.Type.ExplosiveShots;
         case CannonballEffector.Type.Fire:
            return Powerup.Type.FireShots;
         case CannonballEffector.Type.Ice:
            return Powerup.Type.IceShots;
         default:
            return Powerup.Type.None;
      }
   }

   #region Private Variables

   // Information for each existing powerup type, sorted by powerup type
   private Dictionary<Powerup.Type, PowerupData> _powerupData = new Dictionary<Powerup.Type, PowerupData>();

   // The organised powerups of the local player
   private PlayerPowerups _localPlayerPowerups = new PlayerPowerups();

   // A cache of powerups for each player
   private Dictionary<int, PlayerPowerups> _serverPlayerPowerups = new Dictionary<int, PlayerPowerups>();

   private class PlayerPowerups : Dictionary<Powerup.Type, List<Powerup>> { }

   // Stores the last time a user triggered each type of powerup
   private Dictionary<int, Dictionary<Powerup.Type, float>> _lastPowerupActivationTimesByUser = new Dictionary<int, Dictionary<Powerup.Type, float>>();

   // The base damage for the fire effect
   private const float FIRE_BASE_DAMAGE = 10.0f;

   // The base duration for the fire effect
   private const float FIRE_BASE_DURATION = 5.0f;

   // The base damage for the electric effect
   private const float ELECTRIC_BASE_DAMAGE = 10.0f;

   // The base range for the electric effect
   private const float ELECTRIC_BASE_RANGE = 1.0f;

   // The base strength for the ice effect
   private const float ICE_BASE_STRENGTH = 0.1f;

   // The base duration for the ice effect
   private const float ICE_BASE_DURATION = 4.0f;

   // The base damage for the explosive effect
   private const float EXPLOSIVE_BASE_DAMAGE = 30.0f;

   // The base range for the explosive effect
   private const float EXPLOSIVE_BASE_RANGE = 0.6f;

   // The base range for the bouncing effect
   private const float BOUNCING_BASE_RANGE = 2.0f;

   #endregion
}