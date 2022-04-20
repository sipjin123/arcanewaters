using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class TemporaryController : MonoBehaviour
{
   #region Public Variables

   // Current count of controlled entities, for testing
   public int puppetCount = 0;

   #endregion

   public void controlGranted (NetEntity entity, bool overrideMovement = false) {
      if (_puppets.Any(p => p.entity == entity)) {
         D.error("Trying to grant control twice to a controller.");
         return;
      }

      // Check if this entity is an npc since npcs behave differently
      bool isNpc = entity is NPC;

      // Register the entity as being controlled, set any data we might need for controlling it
      ControlData puppet = new ControlData {
         entity = entity,
         startTime = Time.time,
         startPos = entity.getRigidbody().position,
         hasMovement = !isNpc,
         controlledMovementExternally = overrideMovement
      };
      _puppets.Add(puppet);

      // Disable collider for the entity
      if (puppet.entity.isLocalPlayer) {
         puppet.entity.getMainCollider().isTrigger = true;
      }

      // Override Controls
      if (puppet.entity is NPC) {
         puppet.entity.isUnderExternalControl = true;
      }

      // Allow child class to react to the beginning of control
      startControl(puppet);
   }

   private void FixedUpdate () {
      // We iterate backwards, so to not get disturbed by puppets being removed from the list
      for (int i = _puppets.Count - 1; i >= 0; i--) {
         // Update any data we need for controlling the entity
         _puppets[i].time += Time.deltaTime;

         // Allow the child class to control the entity
         controlUpdate(_puppets[i]);
      }

      puppetCount = _puppets.Count;
   }

   protected void endControl (ControlData puppet) {
      _puppets.Remove(puppet);

      // Reenable collider for entity
      if (puppet.entity.isLocalPlayer) {
         puppet.entity.getMainCollider().isTrigger = false;
      }

      // Remove override controls
      if (puppet.entity is NPC) {
         puppet.entity.isUnderExternalControl = false;
      }

      puppet.entity.giveBackControl(this);
   }

   public void forceFastForward (NetEntity entity) {
      ControlData puppet = _puppets.FirstOrDefault(p => p.entity == entity);
      if (puppet == null) {
         D.error("Cannot fast forward control of entity because we are not controlling it.");
         return;
      }

      onForceFastForward(puppet);
      endControl(puppet);
   }

   protected void tryTriggerController (BodyEntity entity) {
      if (!entity.isLocalPlayer) {
         D.error("Control can only be triggered by local player.");
         return;
      }

      if (!entity.hasScheduledController(this)) {
         entity.requestControl(this);
      }
   }

   protected abstract void onForceFastForward (ControlData puppet);
   protected virtual void controlUpdate (ControlData puppet) { }
   protected virtual void startControl (ControlData puppet) { }

   private void OnDestroy () {
      for (int i = _puppets.Count - 1; i >= 0; i--) {
         if (_puppets[i].entity != null) {
            forceFastForward(_puppets[i].entity);
         }
      }
   }

   #region Private Variables

   // The entities we are currently controlling and information about it
   protected List<ControlData> _puppets = new List<ControlData>();

   // Buffer we use for storing physics collision test results
   protected Collider2D[] _colliderBuffer = new Collider2D[1];

   #endregion

   // Class we use to track various data used for controlling entities
   protected class ControlData
   {
      // The entity we are controlling
      public NetEntity entity;

      // When did we start controlling the entity
      public float startTime;

      // Start position of the control
      public Vector2 startPos;

      // End position of control
      public Vector2 endPos;

      // For how long are we controlling the entity
      public float time = 0;

      // If this puppet has movement altering feature
      public bool hasMovement = true;

      // If movement is being controlled externally
      public bool controlledMovementExternally = false;
   }
}
