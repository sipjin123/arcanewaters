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

   #endregion

   public void Awake () {
      self = this;
   }

   private void Start () {
      // TODO: Remove after playtest
      // InvokeRepeating(nameof(tickPowerupState), 1, 1);
   }

   // TODO: Remove after playtest
   /*
   private void Update () {
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.X)) {
         PowerupPanel.self.addLandPowerup(new LandPowerupData {
            counter = 100,
            expiryType = LandPowerupExpiryType.BossKills,
            landPowerupType = LandPowerupType.DamageBoost,
            userId = Global.player.userId,
            value = 10
         });
      }
   }*/

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
         D.debug("Add powerup data: {" + userId + "} T:{" + type + "} ET:{" + expiryType + "} C:{" + count + "}");
         landPowerupDataSet[userId].Add(new LandPowerupData {
            counter = count,
            userId = userId,
            landPowerupType = type,
            expiryType = expiryType,
            value = value
         });

         // Cache player and powerup data
         NetEntity player = EntityManager.self.getEntity(userId);
         LandPowerupData powerUpToAdd = landPowerupDataSet[userId].Find(_ => _.landPowerupType == type);

         // Tell the client to remove on their end and update their gui
         if (player && player is NetEntity) {
            player.rpc.Target_AddLandPowerup(player.connectionToClient, powerUpToAdd);
         }

         // Remove entry from server collection
         landPowerupDataSet[userId].Add(powerUpToAdd);
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

   #region Private Variables

   #endregion
}
