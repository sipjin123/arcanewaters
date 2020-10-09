using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class TemporaryController : MonoBehaviour
{
   #region Public Variables

   // Current count of controlled entities, for testing
   public int puppetCount = 0;

   #endregion

   public void controlGranted (NetEntity entity) {
      if (_puppets.Any(p => p.entity == entity)) {
         D.error("Trying to grant control twice to a controller.");
         return;
      }

      // Register the entity as being controlled, set any data we might need for controlling it
      ControlData puppet = new ControlData {
         entity = entity,
         startTime = Time.time,
         startPos = entity.getRigidbody().position,
         mainEntityCollider = entity.GetComponents<CircleCollider2D>().FirstOrDefault(c => !c.isTrigger && c.enabled)
      };
      _puppets.Add(puppet);

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
      puppet.entity.giveBackControl(this);
   }

   protected virtual void controlUpdate (ControlData puppet) { }
   protected virtual void startControl (ControlData puppet) { }

   private void OnDestroy () {
      // Give back all control that we might have
      foreach (ControlData puppet in _puppets) {
         if (puppet.entity != null) {
            puppet.entity.giveBackControl(this);
         }
      }
      _puppets.Clear();
   }

   #region Private Variables

   // The entities we are currently controlling and information about it
   protected List<ControlData> _puppets = new List<ControlData>();

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

      // Main collider of the enitity, used for collisions
      public Collider2D mainEntityCollider;
   }
}
