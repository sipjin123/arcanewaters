using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterEntity : SeaEntity
{
   #region Public Variables

   // The Type of NPC that is sailing this ship
   [SyncVar]
   public NPC.Type npcType;

   // The Name of the NPC that is sailing this ship
   [SyncVar]
   public string npcName;

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
      Die,
   }


   #endregion

   #region Private Variables

   #endregion
}
