using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Threading.Tasks;

public class EntityLagMonitor : MonoBehaviour {
   #region Public Variables

   // Singleton instance
   public static EntityLagMonitor self;

   // Any deltaTime values above this value will trigger the collision detection
   public float maximumAllowedDeltaTime = 0.1f;

   // A list of ship entities we will check for collisions if there is a lag spike. We need to store different entity types in different lists, as they will need to use different layermasks to check collisions.
   public List<NetEntity> trackedShipEntities = new List<NetEntity>();

   // When set to false, the functions of this monitor will be disabled
   public bool monitoringEnabled = true;

   #endregion

   private void Awake () {
      // Only run on the server
      if (!NetworkServer.active) {
         this.enabled = false;
      }

      self = this;

      _shipsLayerMask = Util.getCollisionLayerMask(LayerMask.NameToLayer("Ships"));
   }

   private void Update () {
      if (!monitoringEnabled) {
         return;
      }

      // If the delta time value is too high, check for collisions
      if (Time.deltaTime > maximumAllowedDeltaTime) {
         D.warning("EntityLagMonitor detected a large deltaTime spike: " + Time.deltaTime + " seconds. Checking collisions for " + trackedShipEntities.Count + " entities.");
         onLagSpikeCheckCollisions();
      
      // Otherwise, update the positions of the entities
      } else {
         foreach (NetEntity trackedEntity in trackedShipEntities) {
            _trackedEntitiesLastPositions[trackedEntity.netId] = trackedEntity.transform.position;
         }
      }
   }

   private void onLagSpikeCheckCollisions () {
      foreach (NetEntity trackedEntity in trackedShipEntities) {
         if (trackedEntity != null) {
            checkLagSpikeCollisions(trackedEntity);
         }
      }
   }

   private void checkLagSpikeCollisions (NetEntity entity) {
      if (_trackedEntitiesLastPositions.ContainsKey(entity.netId)) {
         Vector3 entityLastPosition = _trackedEntitiesLastPositions[entity.netId];
         Vector2 rayDirection = entity.transform.position - entityLastPosition;
         float rayDistance = rayDirection.magnitude;
         RaycastHit2D rayHit = Physics2D.Raycast(entityLastPosition, rayDirection, rayDistance, _shipsLayerMask);
         if (rayHit.collider != null) {
            D.debug("EntityLagMonitor detected entity: " + entity.name + " (netId: " + entity.netId + " moved through a collider during a lagspike. Moving them back to their previous position.");
            entity.transform.position = entityLastPosition;
         }

      } else {
         D.debug("Lag Montior couldn't find previous position for entity: " + entity.name + ", with netId: " + entity.netId);
      }
   }

   #region Private Variables

   // Last position stored for each entity
   private Dictionary<uint, Vector3> _trackedEntitiesLastPositions = new Dictionary<uint, Vector3>();

   // Layermask used to check collisions for ships
   private int _shipsLayerMask;

   #endregion
}
