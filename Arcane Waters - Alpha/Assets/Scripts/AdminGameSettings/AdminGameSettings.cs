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

   // A multiplier applied to the battler attack duration (sword swings, gun fire, etc)
   public float battleAttackDuration = 1.0f;

   // A multiplier applied to all battler animations time per frame
   public float battleTimePerFrame = 1.0f;

   // A multiplier applied to the enemy spawn count per spawner spot in sea maps
   public float seaSpawnsPerSpot = 1.0f;

   // A multiplier applied to enemy attack cooldowns in sea maps
   public float seaAttackCooldown = 1.0f;

   // A multiplier applied to enemy max health in sea maps
   public float seaMaxHealth = 1.0f;

   // The additional damage the land bosses receive depending on the party count, this value is set in %
   public float bossDamagePerMember = 10;

   // The additional health the land bosses receive depending on the party count, this value is set in %
   public float bossHealthPerMember = 20;

   // The multipler of health and damage of land battles depending on difficulty value
   public float landDifficultyScaling = 0.01f;

   #endregion

   public AdminGameSettings () { }

#if IS_SERVER_BUILD

   public AdminGameSettings (MySqlDataReader dataReader) {
      this.id = DataUtil.getInt(dataReader, "id");
      this.creationDate = DataUtil.getDateTime(dataReader, "creationDate").ToBinary();
      this.battleAttackCooldown = DataUtil.getFloat(dataReader, "battleAttackCooldown");
      this.battleJumpDuration = DataUtil.getFloat(dataReader, "battleJumpDuration");
      this.battleAttackDuration = DataUtil.getFloat(dataReader, "battleAttackDuration");
      this.battleTimePerFrame = DataUtil.getFloat(dataReader, "battleTimePerFrame");
      this.seaSpawnsPerSpot = DataUtil.getFloat(dataReader, "seaSpawnsPerSpot");
      this.seaAttackCooldown = DataUtil.getFloat(dataReader, "seaAttackCooldown");
      this.seaMaxHealth = DataUtil.getFloat(dataReader, "seaMaxHealth");
      this.bossDamagePerMember = DataUtil.getFloat(dataReader, "landBossAddedDamage");
      this.bossHealthPerMember = DataUtil.getFloat(dataReader, "landBossAddedHealth");
      this.landDifficultyScaling = DataUtil.getFloat(dataReader, "landDifficultyScaling");
   }

#endif

   public AdminGameSettings (int id, DateTime creationDate, float battleAttackCooldown, float battleJumpDuration, 
      float battleAttackDuration, float battleTimePerFrame, float seaSpawnsPerSpot, float seaAttackCooldown, float seaMaxHealth, 
      float landBossAddedHealth, float landBossAddedDamage, float landDifficultyScaling) {
      this.id = id;
      this.creationDate = creationDate.ToBinary();
      this.battleAttackCooldown = battleAttackCooldown;
      this.battleJumpDuration = battleJumpDuration;
      this.battleAttackDuration = battleAttackDuration;
      this.battleTimePerFrame = battleTimePerFrame;
      this.seaSpawnsPerSpot = seaSpawnsPerSpot;
      this.seaAttackCooldown = seaAttackCooldown;
      this.seaMaxHealth = seaMaxHealth;
      this.bossHealthPerMember = landBossAddedHealth;
      this.bossDamagePerMember = landBossAddedDamage;
      this.landDifficultyScaling = landDifficultyScaling;
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

