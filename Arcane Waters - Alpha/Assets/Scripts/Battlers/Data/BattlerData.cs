using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;

// Data that a battler will hold, max health, xp, sounds, etc.
[System.Serializable]

public class BattlerData {
   #region Public Variables

   // The name of this unit
   public string enemyName = "unknown";

   // Holds the path for the icon
   public string imagePath;

   // Used for calculating the current level of this battler.
   public int currentXP;

   // Determines the enemy type
   public Enemy.Type enemyType;

   // Ability Points gained when damaged
   public int apGainWhenDamaged;

   // Base battler parameters
   public int baseHealth;
   public int baseDefense;
   public int baseDamage;
   public int baseGoldReward;
   public int baseXPReward;

   // Increments in stats per level.
   public int damagePerLevel;
   public int defensePerLevel;
   public int healthPerlevel;

   // Attacks and abilities that the battler have
   public AbilityDataRecord battlerAbilities;
   public RawGenericLootData battlerLootData;

   // Element defense multiplier values
   public float physicalDefenseMultiplier;
   public float fireDefenseMultiplier;
   public float earthDefenseMultiplier;
   public float airDefenseMultiplier;
   public float waterDefenseMultiplier;
   public float allDefenseMultiplier;

   // Element attack multiplier values
   public float physicalAttackMultiplier;
   public float fireAttackMultiplier;
   public float earthAttackMultiplier;
   public float airAttackMultiplier;
   public float waterAttackMultiplier;
   public float allAttackMultiplier;

   // Sounds
   public string deathSoundPath;
   public string attackJumpSoundPath;

   // The amount of time our attack takes depends the type of Battler
   public float preContactLength;

   // The amount of time before the ground effect appears depends on the type of Battler
   public float preMagicLength;

   #endregion

   public BattlerData () { }

   public static BattlerData CreateInstance (BattlerData datacopy) {
      BattlerData data = new BattlerData();

      data.setAllBattlerData(datacopy);

      return data;
   }

   public static BattlerData BattleData (int xp, int apWhenDamaged, int baseHealth, int baseDef, int baseDmg, int baseGold, int dmgPerLevel,
      int defPerLevel, int healthPerLevel, AbilityDataRecord battlerAbilities, float physicalDefMultiplier, float fireDefMultiplier,
      float earthDefMultiplier, float airDefMultiplier, float waterDefMultiplier, float allDefMultiplier, float physicalAtkMultiplier,
      float fireAtkMultiplier, float earthAtkMultiplier, float airAtkMultiplier, float waterAtkMultiplier, float allAtkMultiplier,
      string deathSound, string jumpAtkSound, float preContactLength, float preMagicLength, int baseXPReward, RawGenericLootData lootData,
      Enemy.Type enemyType, Battler battlerObject) {

      BattlerData data = new BattlerData();

      data.setAllBattlerData(xp, apWhenDamaged, baseHealth, baseDef, baseDmg, baseGold, dmgPerLevel,
       defPerLevel, healthPerLevel, battlerAbilities, physicalDefMultiplier, fireDefMultiplier,
       earthDefMultiplier, airDefMultiplier, waterDefMultiplier, allDefMultiplier, physicalAtkMultiplier,
       fireAtkMultiplier, earthAtkMultiplier, airAtkMultiplier, waterAtkMultiplier, allAtkMultiplier,
       deathSound, jumpAtkSound, preContactLength, preMagicLength, baseXPReward, lootData, enemyType, battlerObject);

      return data;
   }

   protected void setAllBattlerData (BattlerData datacopy) {
      currentXP = datacopy.currentXP;
      enemyType = datacopy.enemyType;

      apGainWhenDamaged = datacopy.apGainWhenDamaged;

      baseHealth = datacopy.baseHealth;
      baseDefense = datacopy.baseDefense;
      baseDamage = datacopy.baseDamage;
      baseGoldReward = datacopy.baseGoldReward;
      baseXPReward = datacopy.baseXPReward;

      damagePerLevel = datacopy.damagePerLevel;
      defensePerLevel = datacopy.defensePerLevel;
      healthPerlevel = datacopy.healthPerlevel;

      battlerAbilities = datacopy.battlerAbilities;
      battlerLootData = datacopy.battlerLootData;

      physicalDefenseMultiplier = datacopy.physicalDefenseMultiplier;
      fireDefenseMultiplier = datacopy.fireDefenseMultiplier;
      earthDefenseMultiplier = datacopy.earthDefenseMultiplier;
      airDefenseMultiplier = datacopy.airDefenseMultiplier;
      waterDefenseMultiplier = datacopy.waterDefenseMultiplier;
      allDefenseMultiplier = datacopy.allDefenseMultiplier;

      physicalAttackMultiplier = datacopy.physicalAttackMultiplier;
      fireAttackMultiplier = datacopy.fireAttackMultiplier;
      earthAttackMultiplier = datacopy.earthAttackMultiplier;
      airAttackMultiplier = datacopy.airAttackMultiplier;
      waterAttackMultiplier = datacopy.waterAttackMultiplier;
      allAttackMultiplier = datacopy.allAttackMultiplier;

      deathSoundPath = datacopy.deathSoundPath;
      attackJumpSoundPath = datacopy.attackJumpSoundPath;

      preContactLength = datacopy.preContactLength;
      preMagicLength = datacopy.preMagicLength;
   }

   protected void setAllBattlerData (int xp, int apWhenDamaged, int baseHealth, int baseDef, int baseDmg, int baseGold, int dmgPerLevel,
      int defPerLevel, int healthPerLevel, AbilityDataRecord battlerAbilities, float physicalDefMultiplier, float fireDefMultiplier,
      float earthDefMultiplier, float airDefMultiplier, float waterDefMultiplier, float allDefMultiplier, float physicalAtkMultiplier,
      float fireAtkMultiplier, float earthAtkMultiplier, float airAtkMultiplier, float waterAtkMultiplier, float allAtkMultiplier,
      string deathSound, string jumpAtkSound, float preContactLength, float preMagicLength, int baseXPReward, RawGenericLootData lootData,
      Enemy.Type enemyType, Battler battlerObject) {

      this.currentXP = xp;
      this.enemyType = enemyType;

      this.apGainWhenDamaged = apWhenDamaged;

      this.baseHealth = baseHealth;
      this.baseDefense = baseDef;
      this.baseDamage = baseDmg;
      this.baseGoldReward = baseGold;
      this.baseXPReward = baseXPReward;

      this.damagePerLevel = dmgPerLevel;
      this.defensePerLevel = defPerLevel;
      this.healthPerlevel = healthPerLevel;

      this.battlerAbilities = battlerAbilities;
      this.battlerLootData = lootData;

      this.physicalDefenseMultiplier = physicalDefMultiplier;
      this.fireDefenseMultiplier = fireDefMultiplier;
      this.earthDefenseMultiplier = earthDefMultiplier;
      this.airDefenseMultiplier = airDefMultiplier;
      this.waterDefenseMultiplier = waterDefMultiplier;
      this.allDefenseMultiplier = allDefMultiplier;

      this.physicalAttackMultiplier = physicalAtkMultiplier;
      this.fireAttackMultiplier = fireAtkMultiplier;
      this.earthAttackMultiplier = earthAtkMultiplier;
      this.airAttackMultiplier = airAtkMultiplier;
      this.waterAttackMultiplier = waterAtkMultiplier;
      this.allAttackMultiplier = allAtkMultiplier;

      this.deathSoundPath = deathSound;
      this.attackJumpSoundPath = jumpAtkSound;

      this.preContactLength = preContactLength;
      this.preMagicLength = preMagicLength;
   }
}
