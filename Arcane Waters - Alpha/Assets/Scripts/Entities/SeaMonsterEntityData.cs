using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterEntityData : ScriptableObject {
   #region Public Variables

   // Determines the type of monster
   public Enemy.Type seaMonsterType;

   // Shows the gizmos indicating the ranges of each sea monster
   public bool showDebugGizmo;

   // Determines the aggression state of the unit
   public bool isAggressive;

   // When set to true, we pick random waypoints
   public bool autoMove = false;

   // Determines if this is a Melee Unit
   public bool isMelee = false;

   // Determines if this is a Ranged Unit
   public bool isRanged = true;

   // Determines if this is unit can drop treasures
   public bool shouldDropTreasure = true;

   // Determines if this is can take damage
   public bool isInvulnerable = false;

   // The max gap distance between target and this unit to perform a ranged attack
   public float maxProjectileDistanceGap = 2;

   // The max gap distance between target and this unit to perform a melee attack
   public float maxMeleeDistanceGap = .5f;

   // The max gap distance between target and this unit before it stops chasing
   public float maxDistanceGap = 3;

   // Determines the type of attack this unit does
   public Attack.Type attackType;

   // The radius that defines how far the monster will chase before it retreats
   public float territoryRadius = 4.5f;

   // The radius that defines how near the player ships are before this unit chases it
   public float detectRadius = 4;

   // Determines how often the unit attacks
   public float attackFrequency = 1;

   // Determines how quick the unit reloads
   public float reloadDelay = 1;

   // Determines how often the unit moves
   public float moveFrequency = 1;

   // Determines how often this unit checks its surroundings for targets
   public float findTargetsFrequency = .5f;

   // Holds the sprite texture to be used for the sea monster
   public Sprite defaultSprite;

   // Holds the other sprite texture option to be used for the sea monster
   public Sprite secondarySprite;

   // Holds the texture to be used for the ripples
   public Texture2D defaultRippleTexture;

   // Holds the sprite texture to be used for the ripples
   public Sprite defaultRippleSprite;

   // Overrides the scale of the monster
   public float scaleOverride = 1;

   // Overrides the scale of the monster
   public float outlineScaleOverride = 1;

   // Overrides the scale of the ripples
   public float rippleScaleOverride = 1;

   // Max health of the sea monster
   public int maxHealth = 25;

   // Determines the anim group used by the simple animation
   public Anim.Group animGroup;

   // Determines if this unit is a standalone/minion/master being
   public RoleType roleType;

   // Holds the value of the speed override of the simple animation
   public float animationSpeedOverride = -1;

   // Spawn Location Overrides may vary depending on the sea monster sprites
   public List<DirectionalPositions> projectileSpawnLocations;

   // Offset value for the position of the ripples
   public Vector3 rippleLocOffset;

   #endregion

   #region Private Variables

   #endregion
}
