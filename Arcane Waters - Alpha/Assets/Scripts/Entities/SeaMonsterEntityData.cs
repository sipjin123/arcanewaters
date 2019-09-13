using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterEntityData : ScriptableObject {
   #region Public Variables

   // Determines the type of monster
   public Enemy.Type seaMonsterType;

   // Determines the aggression state of the unit
   public bool isAggressive;

   // When set to true, we pick random waypoints
   public bool autoMove = false;

   // Determines if this is a Melee Unit
   public bool isMelee = false;

   // The max gap distance between target and this unity
   public float maxProjectileDistanceGap = 2;

   // The max gap distance between target and this unity
   public float maxDistanceGap = 3;

   // Determines the type of attack this unit does
   public Attack.Type attackType;

   // The radius that defines how far the monster will chase before it retreats
   public float territoryRadius = 4.5f;

   // The radius that defines how near the player ships are before this unit chases it
   public float detectRadius = 4;

   // Determines how often the unit attacks
   public float attackFrequency = 1;

   // Determines how often the unit moves
   public float moveFrequency = 1;

   // Determines how often this unit checks its surroundings for targets
   public float findTargetsFrequency = .5f;

   #endregion

   #region Private Variables

   #endregion
}
