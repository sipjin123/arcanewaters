using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ProjectileSchedule
{
   #region Public Variables

   // The destination of the projectile
   public Vector2 targetLocation;

   // The origin of the projectile
   public Vector2 spawnLocation;

   // The attack type to process upon collision
   public Attack.Type attackType;

   // The time the server is scheduled to launch this projectile
   public float projectileLaunchTime;

   // The animation delay the clients will be processing upon receiving server command
   public float attackAnimationTime;

   // The estimated time the projectile will collide its destination
   public float impactTimestamp;

   // A flag to determine if the schedule should be disposed
   public bool dispose;

   // The ability id of the attack
   public int attackAbilityId;

   #endregion
}