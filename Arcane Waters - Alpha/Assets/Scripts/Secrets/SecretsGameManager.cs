using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class SecretsGameManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static SecretsGameManager self;

   // The list of secrets data registered
   public List<SecretsData> secretsDataList = new List<SecretsData>();

   // List of user id's inside secret areas
   public List<int> usersInSecretAreas = new List<int>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void enterUserToSecret (int userId, string areaName, int instanceId, SecretsNode secretNode) {
      if (secretsDataList.Exists(_=>_.instanceId == instanceId && _.areaName == areaName)) {
         D.editorLog("This secret area exists, adding player: " + userId, Color.green);
         
         SecretsData existingData = secretsDataList.Find(_ => _.instanceId == instanceId && _.areaName == areaName);
         existingData.userIdList.Add(userId);
         usersInSecretAreas.Add(userId);
      } else {
         D.editorLog("This secret does not exists, creating new and adding player player: " + userId, Color.green);
         SecretsData newData = new SecretsData {
            instanceId = instanceId,
            areaName = areaName,
            userIdList = new List<int>(),
            secretNode = secretNode,
         };
         usersInSecretAreas.Add(userId);
         newData.userIdList.Add(userId);
         secretsDataList.Add(newData);
      }
   }

   public void checkIfUserIsInSecret (int userId) {
      if (usersInSecretAreas.Exists(_=>_ == userId)) {
         // Gathers all secret areas in an instance
         List<SecretsData> allSecretAreas = secretsDataList.FindAll(_ => _.userIdList.Exists(q=>q == userId));

         // Checks all secret areas if the user is existing in any
         foreach (SecretsData secretArea in allSecretAreas) {
            if (secretArea.userIdList.Exists(_=>_ == userId)) {
               D.editorLog("The user exists in the area: " + secretArea.areaName + " - " + userId, Color.green);
               secretArea.secretNode.userIds.Remove(userId);
               secretArea.userIdList.Remove(userId);
               usersInSecretAreas.Remove(userId);
            }
         }
      } else {
         D.editorLog("The user: " + userId + " is not in any secret area", Color.green);
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
   public SecretsNode secretNode;
}