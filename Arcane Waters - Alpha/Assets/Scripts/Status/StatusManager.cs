using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StatusManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static StatusManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public Status create (Status.Type statusType, float length, int targetUserId) {
      Status statusEffect = Instantiate(PrefabsManager.self.statusPrefab);
      statusEffect.transform.SetParent(self.transform, false);
      statusEffect.statusType = statusType;
      statusEffect.startTime = Util.netTime();
      statusEffect.endTime = statusEffect.startTime + length;

      // Keep track of the status effects
      addStatus(targetUserId, statusEffect);

      // Remove the Status after the delay
      StartCoroutine(CO_removeStatus(targetUserId, statusEffect, length));

      return statusEffect;
   }

   public void addStatus (int userId, Status status) {
      List<Status> list = new List<Status>();

      if (_statuses.ContainsKey(userId)) {
         list = _statuses[userId];
      }

      list.Add(status);
      _statuses[userId] = list;
   }

   public void removeStatus (int userId, Status oldStatus) {
      List<Status> newList = new List<Status>();

      // Remove it from the list
      if (_statuses.ContainsKey(userId)) {
         foreach (Status status in _statuses[userId]) {
            // Retain all statuses except the old one
            if (status != oldStatus) {
               newList.Add(status);
            }
         }
      }

      _statuses[userId] = newList;

      // Now we can destroy it
      Destroy(oldStatus.gameObject);
   }

   public bool hasStatus (int userId, Status.Type statusType) {
      if (_statuses.ContainsKey(userId)) {
         foreach (Status status in _statuses[userId]) {
            if (status.statusType == statusType) {
               return true;
            }
         }
      }

      return false;
   }

   protected IEnumerator CO_removeStatus (int userId, Status status, float delay) {
      yield return new WaitForSeconds(delay);

      removeStatus(userId, status);
   }

   #region Private Variables

   // A mapping of user ID to the statuses that affect them
   protected Dictionary<int, List<Status>> _statuses = new Dictionary<int, List<Status>>();

   #endregion
}
