using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterEntity : SeaEntity
{
   #region Public Variables

   // The Type of NPC SeaMonster
   [SyncVar]
   public NPC.Type npcType;

   // The Name of the NPC Seamonster
   [SyncVar]
   public string monsterName;

   // The Route that this Bot should follow
   public Route route;

   // The current waypoint
   public Waypoint waypoint;

   // When set to true, we pick random waypoints
   public bool autoMove = false;

   // Animator
   public Animator animator;

   // A flag to determine if the object has died
   public bool hasDied = false;

   // Tentacle Animation
   public enum TentacleAnimType
   {
      Idle,
      Attack,
      EndAttack,
      Die,
      Move,
      MoveStop
   }

   #endregion

   [Server]
   public void callAnimation (TentacleAnimType anim) {
      Rpc_CallAnimation(anim);
   }

   [ClientRpc]
   public void Rpc_CallAnimation (TentacleAnimType anim) {
      switch (anim) {
         case TentacleAnimType.Attack:
            animator.SetBool("attacking", true);
            break;
         case TentacleAnimType.EndAttack:
            animator.SetBool("attacking", false);
            break;
         case TentacleAnimType.Die:
            animator.SetBool("die", true);
            break;
         case TentacleAnimType.Move:
            animator.SetBool("move", true);
            break;
         case TentacleAnimType.MoveStop:
            animator.SetBool("move", false);
            break;
      }
   }

   protected int lockToTarget (NetEntity attacker) {
      int horizontalDirection = 0;
      int verticalDirection = 0;

      float offset = .1f;

      // Check where the attacker currently is
      Vector2 spot = attacker.transform.position;
      if (spot.x > transform.position.x + offset) {
         horizontalDirection = (int) Direction.East;
      } else if (spot.x < transform.position.x - offset) {
         horizontalDirection = (int) Direction.West;
      } else {
         // Debug.LogError("Neither west or east");
         horizontalDirection = 0;
      }

      if (spot.y > transform.position.y + offset) {
         verticalDirection = (int) Direction.North;
      } else if (spot.y < transform.position.y - offset) {
         verticalDirection = (int) Direction.South;
      } else {
         // Debug.LogError("Neither north or southh");
         verticalDirection = 0;
      }

      int finalDirection = 0;
      if (horizontalDirection == (int) Direction.East) {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.NorthEast;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.SouthEast;
         }

         if (verticalDirection == 0) {
            finalDirection = (int) Direction.East;
         }
      } else if (horizontalDirection == (int) Direction.West) {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.NorthWest;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.SouthWest;
         }

         if (verticalDirection == 0) {
            finalDirection = (int) Direction.West;
         }
      } else {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.North;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.South;
         }
      }

      return finalDirection;
   }

   #region Private Variables

   #endregion
}
