﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipAbilityData
{
   // The name of the ability
   public string abilityName;

   // The info of the ability
   public string abilityDescription;

   // Type of ship
   public Ship.Type shipType;

   // If there are varieties of the ship type
   public int shipTypeVariety;

   // The level needed to unlock ability
   public int levelRequirement;

   // Damage of the ability
   public int damage;

   // FX per frame of the ability effect
   public float abilitySpriteFXPerFrame;

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