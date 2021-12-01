using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class LandPowerupManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static LandPowerupManager self;

   // The data collection containing info about users land powerups
   public Dictionary<int, List<LandPowerupData>> landPowerupDataSet = new Dictionary<int, List<LandPowerupData>>();

   // The info of the powerups for display
   public Dictionary<LandPowerupType, LandPowerupInfo> landPowerupInfo = new Dictionary<LandPowerupType, LandPowerupInfo>();

   // Log screen
   public bool toggleScreenLog;

   // The land powerup sprite collection
   public List<LandPowerupSpritePair> landPowerupSprite;

   #endregion

   public void Awake () {
      self = this;

      // TODO: Setup web tool to have a way to register information for powerups, hard code for now
      landPowerupInfo.Add(LandPowerupType.DamageBoost, new LandPowerupInfo {
         powerupInfo = "Increases damage by 20% for land combat",
         powerupName = "Damage Boost",
         powerupType = LandPowerupType.DamageBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.DamageBoost)
      });
      landPowerupInfo.Add(LandPowerupType.DefenseBoost, new LandPowerupInfo {
         powerupInfo = "Increases defense by 20% for land combat",
         powerupName = "Defense Boost",
         powerupType = LandPowerupType.DefenseBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.DefenseBoost)
      });
      landPowerupInfo.Add(LandPowerupType.SpeedBoost, new LandPowerupInfo {
         powerupInfo = "Increases speed of the user by 20% when traveling in land",
         powerupName = "Speed Boost",
         powerupType = LandPowerupType.SpeedBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.SpeedBoost)
      });

      landPowerupInfo.Add(LandPowerupType.LootDropBoost, new LandPowerupInfo {
         powerupInfo = "Increases the loots drop by 100% for 5 mins",
         powerupName = "Loot Drop Boost",
         powerupType = LandPowerupType.LootDropBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.LootDropBoost)
      });
      landPowerupInfo.Add(LandPowerupType.ExperienceBoost, new LandPowerupInfo {
         powerupInfo = "Increases experience gained by 10%",
         powerupName = "Experience Boost",
         powerupType = LandPowerupType.ExperienceBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.ExperienceBoost)
      });
      landPowerupInfo.Add(LandPowerupType.RangeDamageBoost, new LandPowerupInfo {
         powerupInfo = "Increases damage of ranged attacks by 20%",
         powerupName = "Range Damage Boost",
         powerupType = LandPowerupType.RangeDamageBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.RangeDamageBoost)
      });
      landPowerupInfo.Add(LandPowerupType.MeleeDamageBoost, new LandPowerupInfo {
         powerupInfo = "Increases damage of melee attacks by 20%",
         powerupName = "Melee Damage Boost",
         powerupType = LandPowerupType.MeleeDamageBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.MeleeDamageBoost)
      });

      landPowerupInfo.Add(LandPowerupType.ClimbSpeedBoost, new LandPowerupInfo {
         powerupInfo = "Increases climbing speed",
         powerupName = "Climb Speed Boost",
         powerupType = LandPowerupType.ClimbSpeedBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.ClimbSpeedBoost)
      });
      landPowerupInfo.Add(LandPowerupType.MiningBoost, new LandPowerupInfo {
         powerupInfo = "Increases total mine drops by 1",
         powerupName = "Mining Boost",
         powerupType = LandPowerupType.MiningBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.MiningBoost)
      });
   }

   public Sprite getLandPowerupSprite (LandPowerupType type) {
      if (landPowerupSprite.Exists(_ => _.type == type)) {
         return landPowerupSprite.Find(_ => _.type == type).sprite;
      }
      return null;
   }

   private void Start () {
      InvokeRepeating(nameof(tickPowerupState), 1, 1);
   }

   private void tickPowerupState () {
      if (landPowerupDataSet == null) {
         return;
      }

      // Process tick related powerup expiry
      foreach (KeyValuePair<int, List<LandPowerupData>> landPowerUpData in landPowerupDataSet) {
         List<LandPowerupData> listInfo = landPowerUpData.Value;
         if (listInfo != null && listInfo.Count > 0) {
            List<LandPowerupData> powerupListToRemove = new List<LandPowerupData>();
            foreach (LandPowerupData powerupData in listInfo) {
               switch (powerupData.expiryType) {
                  case LandPowerupExpiryType.Time: {
                        powerupData.counter -= 1;
                        updateNewPowerupData(landPowerUpData.Key, powerupData.landPowerupType, powerupData.expiryType, powerupData.counter, powerupData.value);
                     }
                     break;
                  default: {
                        // TODO: Default logic here
                     }
                     break;
               }

               // Remove powerup here
               if (powerupData.counter < 1) {
                  // Cache player and powerup data
                  NetEntity player = EntityManager.self.getEntity(landPowerUpData.Key);
                  LandPowerupData powerUpToRemove = landPowerupDataSet[landPowerUpData.Key].Find(_ => _.landPowerupType == powerupData.landPowerupType);
                  
                  // Tell the client to remove on their end and update their gui
                  if (player && player is NetEntity) {
                     player.rpc.Target_RemoveLandPowerup(player.connectionToClient, powerUpToRemove);
                  }
                  powerupListToRemove.Add(powerUpToRemove);
               }
            }

            // Remove entry from server collection
            foreach (LandPowerupData powerupData in powerupListToRemove) {
               landPowerupDataSet[landPowerUpData.Key].Remove(powerupData);
            }
         }
      }
   }

   public void updateNewPowerupData (int userId, LandPowerupType type, LandPowerupExpiryType expiryType, int count, int value) {
      if (!landPowerupDataSet.ContainsKey(userId)) {
         landPowerupDataSet.Add(userId, new List<LandPowerupData>());
      }

      if (!landPowerupDataSet[userId].Exists(_ => _.landPowerupType == type)) {
         // Add a new powerup entry to the user
         LandPowerupData powerUpToAdd = new LandPowerupData {
            counter = count,
            userId = userId,
            landPowerupType = type,
            expiryType = expiryType,
            value = value
         };

         // Cache player and powerup data
         NetEntity player = EntityManager.self.getEntity(userId);

         // Tell the client to remove on their end and update their gui
         if (player) {
            landPowerupDataSet[userId].Add(powerUpToAdd);
            D.debug("Add powerup data: {" + userId + "} T:{" + type + "} ET:{" + expiryType + "} C:{" + count + "} NC:{" + landPowerupDataSet[userId].Count + "}");
            player.rpc.Target_AddLandPowerup(player.connectionToClient, powerUpToAdd);
         }
      } else {
         // Update the powerup entry of the user
         landPowerupDataSet[userId].Find(_ => _.landPowerupType == type).counter = count;
         landPowerupDataSet[userId].Find(_ => _.landPowerupType == type).value = value;
      }
   }

   public bool hasPowerup (int userId, LandPowerupType type) {
      if (!landPowerupDataSet.ContainsKey(userId)) {
         return false;
      }

      if (landPowerupDataSet[userId].Exists(_ => _.landPowerupType == type)) {
         return true;
      }

      return false;
   }

   public int getPowerupValue (int userId, LandPowerupType type) {
      if (!landPowerupDataSet.ContainsKey(userId)) {
         return 0;
      }

      LandPowerupData powerupData = landPowerupDataSet[userId].Find(_ => _.landPowerupType == type);
      if (powerupData != null) {
         return powerupData.value;
      }

      return 0;
   }

   public List<LandPowerupData> getPowerupsForUser (int userId) {
      List<LandPowerupData> landPowerups = new List<LandPowerupData>();
      List<LandPowerupData> powerupToRemove = new List<LandPowerupData>();

      if (!landPowerupDataSet.ContainsKey(userId)) {
         return landPowerups;
      }

      List<LandPowerupData> userLandPowerupData = landPowerupDataSet[userId];
      foreach (LandPowerupData powerupData in userLandPowerupData) {
         if (powerupData.expiryType == LandPowerupExpiryType.OnWarp) {
            powerupToRemove.Add(powerupData);
         } else {
            landPowerups.Add(powerupData);
         }
      }

      foreach (LandPowerupData powerupData in powerupToRemove) {
         landPowerups.Remove(powerupData);
         landPowerupDataSet[userId].Remove(powerupData);
      }

      return landPowerups;
   }

   #region Private Variables

   #endregion
}
