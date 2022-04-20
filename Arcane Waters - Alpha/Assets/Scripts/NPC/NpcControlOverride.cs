using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class NpcControlOverride : MonoBehaviour {
   #region Public Variables

   // If overide is in process
   public bool isOverridingMovement = false;

   // Positions
   public Vector2 endPosition, startPosition, playerPosition;

   // Max move time
   public float maxTime = 1;

   // Time elapsed while moving
   public float elapsedTime = 0;

   // Event that determines if unit has reached destination
   public UnityEvent hasReachedDestination = new UnityEvent();

   // Components
   public Rigidbody2D rigidBody;
   public NPC npc;

   // If this npc is stationary, would mean player will approach it instead
   public bool isStationaryNPC;

   // Distance to trigger petting
   public const float PET_DISTANCE = .075f;
   public const float STATIONARY_PET_DISTANCE = .22f;
   public const float CLIENT_PET_DISTANCE = .25f;

   #endregion

   private void Start () {
      rigidBody = GetComponent<Rigidbody2D>();
      npc = GetComponent<NPC>();
   }

   private void Update () {
      if (isOverridingMovement) {
         float distanceGrap = Vector2.Distance((Vector2) transform.position, endPosition);
         if (distanceGrap > PET_DISTANCE && npc.isStationary == false) {
            // Move animal linearly to the player
            float t = elapsedTime / maxTime;
            rigidBody.MovePosition(Vector3.Lerp(startPosition, endPosition, t));
            elapsedTime += Time.deltaTime;

            Vector2 dir = endPosition - startPosition;
            dir.Normalize();

            // Calculate an angle for that direction
            float angle = Util.angle(dir);
            // Set our facing direction based on that angle
            npc.facing = npc.hasDiagonals ? Util.getFacingWithDiagonals(angle) : Util.getFacing(angle);
         } else {
            if (_entityReference != null) {
               _entityReference = null;
               isOverridingMovement = false;
               hasReachedDestination.Invoke();

               // Force the pet to look at the player upon arriving to destination
               Vector2 dir = playerPosition - (Vector2) transform.position;
               dir.Normalize();

               // Calculate an angle for that direction
               float angle = Util.angle(dir);

               // Set our facing direction based on that angle
               npc.facing = npc.hasDiagonals ? Util.getFacingWithDiagonals(angle) : Util.getFacing(angle);
            }
         }
      }
   }

   public void overridePosition (Vector2 endPos, Vector2 playerPos, NetEntity entity, bool isStationary) {
      // Force the pet to look at the direction where it is headed to
      Vector2 dir = endPos - playerPos;
      _entityReference = entity;
      isStationaryNPC = isStationary;
      dir.Normalize();

      // Calculate an angle for that direction
      float angle = Util.angle(dir);

      // Set our facing direction based on that angle
      npc.facing = npc.hasDiagonals ? Util.getFacingWithDiagonals(angle) : Util.getFacing(angle);

      elapsedTime = 0;
      startPosition = transform.position;
      endPosition = endPos;
      playerPosition = playerPos;

      isOverridingMovement = true;
      npc.isUnderExternalControl = true;

      // If npc is stationary, snap to pet nodes
      if (isStationary) {
         _entityReference.moveToWorldPosition(endPosition);
         playerPosition = _entityReference.transform.position;
      }
   }

   #region Private Variables

   // The entity reference
   [SerializeField]
   private NetEntity _entityReference;

   #endregion
}
