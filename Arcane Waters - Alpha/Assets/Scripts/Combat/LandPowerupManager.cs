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
         powerupInfo = "+20% melee, ranged, and rum damage to attacks in land combat.",
         powerupName = "Arcane Power",
         powerupType = LandPowerupType.DamageBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.DamageBoost)
      });
      landPowerupInfo.Add(LandPowerupType.DefenseBoost, new LandPowerupInfo {
         powerupInfo = "Take 20% less damage in land combat.",
         powerupName = "Defense Boost",
         powerupType = LandPowerupType.DefenseBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.DefenseBoost)
      });
      landPowerupInfo.Add(LandPowerupType.SpeedBoost, new LandPowerupInfo {
         powerupInfo = "20% run speed increase.",
         powerupName = "Brisk Boots",
         powerupType = LandPowerupType.SpeedBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.SpeedBoost)
      });

      landPowerupInfo.Add(LandPowerupType.LootDropBoost, new LandPowerupInfo {
         powerupInfo = "Double loot drops for 5 minutes.",
         powerupName = "Loot Luck",
         powerupType = LandPowerupType.LootDropBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.LootDropBoost)
      });
      landPowerupInfo.Add(LandPowerupType.ExperienceBoost, new LandPowerupInfo {
         powerupInfo = "Increases experience gained by 10%.",
         powerupName = "Quick Learner",
         powerupType = LandPowerupType.ExperienceBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.ExperienceBoost)
      });
      landPowerupInfo.Add(LandPowerupType.RangeDamageBoost, new LandPowerupInfo {
         powerupInfo = "+20% ranged attack damage in land combat.",
         powerupName = "Bullseye",
         powerupType = LandPowerupType.RangeDamageBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.RangeDamageBoost)
      });
      landPowerupInfo.Add(LandPowerupType.MeleeDamageBoost, new LandPowerupInfo {
         powerupInfo = "+20% melee attack damage in land combat.",
         powerupName = "Enrage",
         powerupType = LandPowerupType.MeleeDamageBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.MeleeDamageBoost)
      });

      landPowerupInfo.Add(LandPowerupType.ClimbSpeedBoost, new LandPowerupInfo {
         powerupInfo = "Increases vine climbing speed.",
         powerupName = "Skilled Climber",
         powerupType = LandPowerupType.ClimbSpeedBoost,
         spriteRef = getLandPowerupSprite(LandPowerupType.ClimbSpeedBoost)
      });
      landPowerupInfo.Add(LandPowerupType.MiningBoost, new LandPowerupInfo {
         powerupInfo = "Gain one extra ore when mining.",
         powerupName = "Expert Excavator",
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
