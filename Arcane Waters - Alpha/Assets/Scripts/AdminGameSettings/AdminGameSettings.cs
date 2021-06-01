using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class AdminGameSettings
{
   #region Public Variables

   // The entry id
   public int id = -1;

   // The entry creation date
   public long creationDate = DateTime.MinValue.ToBinary();

   // A multiplier applied to all battler attack cooldowns
   public float battleAttackCooldown = 1.0f;

   // A multiplier applied to all battler jumps duration (when battlers move across the screen)
   public float battleJumpDuration = 1.0f;

   // A multiplier applied to the enemy spawn count per spawner spot in sea leagues maps
   public float seaSpawnsPerSpot = 1.0f;

   // A multiplier applied to enemy attack cooldowns in sea league maps
   public float seaAttackCooldown = 1.0f;

   //// A multiplier applied to all battler idle animations
   //public float idleAnimationSpeedMultiplier = 1f;

   //// A multiplier applied to the battler attack duration (sword swings, gun fire, etc)
   //public float attackDurationMultiplier = 1f;

   #endregion

   public AdminGameSettings () { }

#if IS_SERVER_BUILD

   public AdminGameSettings (MySqlDataReader dataReader) {
      this.id = DataUtil.getInt(dataReader, "id");
      this.creationDate = DataUtil.getDateTime(dataReader, "creationDate").ToBinary();
      this.battleAttackCooldown = DataUtil.getFloat(dataReader, "battleAttackCooldown");
      this.battleJumpDuration = DataUtil.getFloat(dataReader, "battleJumpDuration");
      this.seaSpawnsPerSpot = DataUtil.getFloat(dataReader, "seaSpawnsPerSpot");
      this.seaAttackCooldown = DataUtil.getFloat(dataReader, "seaAttackCooldown");
   }

#endif

   public AdminGameSettings (int id, DateTime creationDate, float battleAttackCooldown, float battleJumpDuration, 
      float seaSpawnsPerSpot, float seaAttackCooldown) {
      this.id = id;
      this.creationDate = creationDate.ToBinary();
      this.battleAttackCooldown = battleAttackCooldown;
      this.battleJumpDuration = battleJumpDuration;
      this.seaSpawnsPerSpot = seaSpawnsPerSpot;
      this.seaAttackCooldown = seaAttackCooldown;
   }

   public override bool Equals (object rhs) {
      if (rhs is AdminGameSettings) {
         var other = rhs as AdminGameSettings;
         return (id == other.id);
      }
      return false;
   }

   public override int GetHashCode () {
      return 17 + 31 * id.GetHashCode();
   }

   #region Private Variables

   #endregion
}

