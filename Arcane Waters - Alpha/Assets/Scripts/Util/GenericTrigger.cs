using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class GenericTrigger : MonoBehaviour {
   #region Public Variables

   // The event to invoke when the trigger activates
   public UnityEvent triggerEvent;

   // Whether this trigger should only happen on the client side
   public bool isClientSideOnly;

   // Whether this trigger should only happen for the local player
   public bool isLocalPlayerOnly;

   #endregion

   protected virtual void Awake () {
      // We don't want to waste time on Client scripts when the server is running in Batch Mode
      if (isClientSideOnly && Application.isBatchMode) {
         this.enabled = false;
      }
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      // Check whether it was our own player that entered the trigger
      bool isOurPlayer = (entity != null && entity == Global.player);

      // If this trigger is only for our own local player, then check for that
      if ((isLocalPlayerOnly && isOurPlayer) || !isLocalPlayerOnly) {
         triggerEvent.Invoke();
      }
   }

   #region Private Variables

   #endregion
}
