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

   public Status create (Status.Type statusType, float strength, float length, uint targetNetId) {
      Status statusEffect = null;

      // If the target already has the status, refresh it
      if (hasStatus(targetNetId, statusType)) {
         statusEffect = getStatus(targetNetId, statusType);
         refreshStatus(statusEffect, targetNetId, length, strength);
      }
      // Otherwise, create a new one
      else {
         statusEffect = Instantiate(PrefabsManager.self.statusPrefab);
         statusEffect.transform.SetParent(self.transform, false);
         statusEffect.statusType = statusType;
         statusEffect.startTime = NetworkTime.time;
         statusEffect.endTime = statusEffect.startTime + length;
         statusEffect.strength = strength;

         // Keep track of the status effects
         addStatus(targetNetId, statusEffect);

         // Remove the Status after the delay
         statusEffect.removeStatusCoroutine = StartCoroutine(CO_removeStatus(targetNetId, statusEffect, length));
      }

      return statusEffect;
   }

   public GameObject getStatusIcon (Status.Type statusType, float length, Transform iconContainer) {
      GameObject icon = Instantiate(Resources.Load<GameObject>("Prefabs/StatusEffectIcon"), Vector3.zero, Quaternion.identity, iconContainer);

      // Tell the status icon which effect to play
      Animator iconAnimator = icon.GetComponent<Animator>();

      if (Util.animatorHasParameter(statusType.ToString(), iconAnimator)) {
         iconAnimator.SetTrigger(statusType.ToString());
      }

      return icon;
   }

   public void addStatus (uint netId, Status newStatus) {
      List<Status> list = new List<Status>();

      if (_statuses.ContainsKey(netId)) {
         list = _statuses[netId];         
      }

      list.Add(newStatus);
      _statuses[netId] = list;
   }

   private void refreshStatus (Status oldStatus, uint netId, float length, float strength) {
      oldStatus.isNew = false;
      oldStatus.startTime = NetworkTime.time;
      oldStatus.endTime = oldStatus.startTime + length;
      oldStatus.strength = strength;

      StopCoroutine(oldStatus.removeStatusCoroutine);

      oldStatus.removeStatusCoroutine = StartCoroutine(CO_removeStatus(netId, oldStatus, length));
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

   public float getStatusStrength (uint netId, Status.Type statusType) {
      if (_statuses.ContainsKey(netId)) {
         foreach (Status status in _statuses[netId]) {
            if (status.statusType == statusType) {
               return status.strength;
            }
         }
      }

      return 0.0f;
   }

   public Status getStatus (uint netId, Status.Type statusType) {
      if (_statuses.ContainsKey(netId)) {
         Status status = _statuses[netId].Find((x) => x.statusType == statusType);
         return status;
      }
      return null;
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
