using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public enum SeaEntityAbilityType {
   None = 0,
   Neutral = 1,
   Ships = 2,
   SeaMonsters = 3,
}

[Serializable]
public class ShipAbilityData
{
   // The ability Id
   public int abilityId;

   // The name of the ability
   public string abilityName;

   // Determines the type of ability for filtering
   public SeaEntityAbilityType seaEntityAbilityType;

   // Sea Entity Attack Type
   public Attack.Type selectedAttackType;

   // The info of the ability
   public string abilityDescription;

   // If there are varieties of the ship type
   public int shipTypeVariety;

   // The level needed to unlock ability
   public int levelRequirement;

   // Damage of the ability
   public int damage;

   // FX per frame of the ability effect
   public float abilitySpriteFXPerFrame = 0.06f;

   // Ability Cooldown
   public float coolDown;

   // Speed of the projectile
   public float projectileSpeed;

   // The sprite of the skill
   public string skillIconPath = "";

   // The sprite of the projectile
   public string projectileSpritePath = "";

   // The sprite of the casting effect
   public string castSpritePath = "";

   // The sprite of the collision effect
   public string collisionSpritePath = "";

   // The sfx of the casting effect
   public string castSFXPath = "";

   // The sfx of the collision effect
   public string collisionSFXPath = "";

   // Type of casting if toward a target or self
   public ShipCastType shipCastType;

   // Type of effect that occurs upon collision
   public ShipCastCollisionType shipCastCollisionType;

   // Determines the ability effect
   public ShipAbilityEffect shipAbilityEffect;

   // The strength of the impact
   public Attack.ImpactMagnitude impactMagnitude = Attack.ImpactMagnitude.None;

   // Determines if the projectile is arching
   public bool hasArch;

   // Adjusts the height of the projectile sprite based in an arch
   public bool syncHeightToArch = true;

   // Determines if this projectile has a trail
   public bool hasTrail;

   // Maximum lifespan of this projectile
   public float lifeTime = 1;

   // Launches a split attack after a certain number of attack
   public bool splitsAfterAttackCap;

   // The number of attacks before the projectile launches a split attack
   public int splitAttackCap = -1;

   // The status effect per collision
   public int statusType;

   // The duration of the status
   public float statusDuration = 1;

   public static Attack.ImpactMagnitude getImpactType (float normalizedValue) {
      if (normalizedValue < .33f) {
         return Attack.ImpactMagnitude.Strong;
      }
      if (normalizedValue < .66f) {
         return Attack.ImpactMagnitude.Normal;
      }

      return Attack.ImpactMagnitude.Weak;
   }

   public enum ShipAbilityEffect
   {
      None = 0,
      Damage = 1,
      Heal = 2,
      StatBuff = 3,
      StatDebuff = 4,
   }

   public enum ShipCastType
   {
      None = 0,
      Self = 1,
      Target = 2,
   }

   public enum ShipCastCollisionType
   {
      None = 0,
      SingleHit = 1,
      AOEHit = 2,
      DirectionalCrossRegular = 3,
      DirectionalCrossDiagonal = 4,
   }
}