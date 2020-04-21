using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class SecretsManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SecretsManager self;

   // The list of secrets data registered
   public List<SecretsData> secretsDataList = new List<SecretsData>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void enterUserToSecret (int userId, string areaName, int instanceId, SecretEntrance secretArea) {
      if (secretsDataList.Exists(_=>_.instanceId == instanceId && _.areaName == areaName)) {
         SecretsData existingData = secretsDataList.Find(_ => _.instanceId == instanceId && _.areaName == areaName);
         existingData.userIdList.Add(userId);
      } else {
         SecretsData newData = new SecretsData {
            instanceId = instanceId,
            areaName = areaName,
            userIdList = new List<int>(),
            secretArea = secretArea,
         };
         newData.userIdList.Add(userId);
         secretsDataList.Add(newData);
      }
   }

   public void checkIfUserIsInSecret (int userId) {
      // Checks if the user exists in any of the secret area
      if (secretsDataList.Exists(_=>_.userIdList.Exists(q=>q == userId))) {
         // Gathers all secret areas in an instance
         List<SecretsData> allSecretAreas = secretsDataList.FindAll(_ => _.userIdList.Exists(q=>q == userId));

         // Checks all secret areas if the user is existing in any
         foreach (SecretsData secretArea in allSecretAreas) {
            if (secretArea.userIdList.Exists(_=>_ == userId)) {
               // Remove the user from the secret area registry so they can enter the area again
               secretArea.secretArea.userIds.Remove(userId);
               secretArea.userIdList.Remove(userId);
            }
         }
      }
   }

   #region Private Variables

   #endregion
}

[Serializable]
public class SecretsData
{
   // The current players in the list
   public List<int> userIdList = new List<int>();

   // The name of the secret area
   public string areaName;

   // The instance id
   public int instanceId;

   // The secret node
   public SecretEntrance secretArea;
}