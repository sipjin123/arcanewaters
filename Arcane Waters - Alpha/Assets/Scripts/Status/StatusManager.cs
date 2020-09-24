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

   public Status create (Status.Type statusType, float length, uint targetNetId) {
      Status statusEffect = Instantiate(PrefabsManager.self.statusPrefab);
      statusEffect.transform.SetParent(self.transform, false);
      statusEffect.statusType = statusType;
      statusEffect.startTime = NetworkTime.time;
      statusEffect.endTime = statusEffect.startTime + length;

      // Keep track of the status effects
      addStatus(targetNetId, statusEffect);

      // Remove the Status after the delay
      StartCoroutine(CO_removeStatus(targetNetId, statusEffect, length));

      return statusEffect;
   }

   public void addStatus (uint netId, Status status) {
      List<Status> list = new List<Status>();

      if (_statuses.ContainsKey(netId)) {
         list = _statuses[netId];
      }

      list.Add(status);
      _statuses[netId] = list;
   }

   public void removeStatus (uint netId, Status oldStatus) {
      List<Status> newList = new List<Status>();

      // Remove it from the list
      if (_statuses.ContainsKey(netId)) {
         foreach (Status status in _statuses[netId]) {
            // Retain all statuses except the old one
            if (status != oldStatus) {
               newList.Add(status);
            }
         }
      }

      _statuses[netId] = newList;

      // Now we can destroy it
      Destroy(oldStatus.gameObject);
   }

   public bool hasStatus (uint netId, Status.Type statusType) {
      if (_statuses.ContainsKey(netId)) {
         foreach (Status status in _statuses[netId]) {
            if (status.statusType == statusType) {
               return true;
            }
         }
      }

      return false;
   }

   protected IEnumerator CO_removeStatus (uint netId, Status status, float delay) {
      yield return new WaitForSeconds(delay);

      removeStatus(netId, status);
   }

   #region Private Variables

   // A mapping of net ID to the statuses that affect them
   protected Dictionary<uint, List<Status>> _statuses = new Dictionary<uint, List<Status>>();

   #endregion
}
