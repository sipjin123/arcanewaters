using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

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

   // Determines the anim group type
   public Anim.Group animGroup = Anim.Group.None;

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
   public string serializedBattlerAbilities;

   // Multiplier Sets
   public BaseDamageMultiplierSet baseDamageMultiplierSet = new BaseDamageMultiplierSet();
   public PerLevelDamageMultiplierSet perLevelDamageMultiplierSet = new PerLevelDamageMultiplierSet();
   public BaseDefenseMultiplierSet baseDefenseMultiplierSet = new BaseDefenseMultiplierSet();
   public PerLevelDefenseMultiplierSet perLevelDefenseMultiplierSet = new PerLevelDefenseMultiplierSet();

   // Sounds
   public int deathSoundEffectId = -1;
   public int jumpSoundEffectId = -1;

   // The amount of time our attack takes depends the type of Battler
   public float preContactLength;

   // The amount of time before the ground effect appears depends on the type of Battler
   public float preMagicLength;

   // Determines if this battler is a boss
   public bool isBossType;

   // Determines if this unit should be set to inactive when it dies in battle
   public bool disableOnDeath;

   // Contains the elemental resistance and weakness
   [XmlIgnore] public Element[] elementalWeakness;
   [XmlIgnore] public Element[] elementalResistance;

   // The reference id of the loot group data
   public int lootGroupId = 0;

   #endregion

   public BattlerData () { }

#if IS_SERVER_BUILD

   public BattlerData (MySqlDataReader dataReader) {
      this.enemyName = dataReader.GetString("enemyName");
      this.enemyType = (Enemy.Type) dataReader.GetInt32("enemyType");
      this.animGroup = (Anim.Group) dataReader.GetInt32("animGroup");

      this.baseHealth = dataReader.GetInt32("baseHealth");
      this.baseDefense = dataReader.GetInt32("baseDefense");
      this.baseDamage = dataReader.GetInt32("baseDamage");
      this.baseGoldReward = dataReader.GetInt32("baseGoldReward");
      this.baseXPReward = dataReader.GetInt32("baseXPReward");

      this.damagePerLevel = dataReader.GetInt32("damagePerLevel");
      this.defensePerLevel = dataReader.GetInt32("defensePerLevel");
      this.healthPerlevel = dataReader.GetInt32("healthPerlevel");
      this.preContactLength = dataReader.GetFloat("preContactLength");
      this.preMagicLength = dataReader.GetFloat("preMagicLength");

      this.deathSoundEffectId = dataReader.GetInt32("deathSoundEffectId");
      this.jumpSoundEffectId = dataReader.GetInt32("jumpSoundEffectId");
      this.imagePath = dataReader.GetString("imagePath");

      string baseDmg = dataReader.GetString("baseDamageMultiplierSet");
      string perlvlDmg = dataReader.GetString("perLevelDamageMultiplierSet");
      string baseDef = dataReader.GetString("baseDefenseMultiplierSet");
      string perlvlDef = dataReader.GetString("perLevelDefenseMultiplierSet");

      this.baseDamageMultiplierSet.stringTranslation = JsonUtility.FromJson<StringTranslation>(baseDmg);
      this.perLevelDamageMultiplierSet.stringTranslation = JsonUtility.FromJson<StringTranslation>(perlvlDmg);
      this.baseDefenseMultiplierSet.stringTranslation = JsonUtility.FromJson<StringTranslation>(baseDef);
      this.perLevelDefenseMultiplierSet.stringTranslation = JsonUtility.FromJson<StringTranslation>(perlvlDef);

      this.baseDamageMultiplierSet.translateStringValues();
      this.perLevelDamageMultiplierSet.translateStringValues();
      this.baseDefenseMultiplierSet.translateStringValues();
      this.perLevelDefenseMultiplierSet.translateStringValues();
   }

#endif

   public static BattlerData CreateInstance (BattlerData datacopy) {
      BattlerData data = new BattlerData();

      data.setAllBattlerData(datacopy);

      return data;
   }

   public static BattlerData BattleData (int xp, int apWhenDamaged, int baseHealth, int baseDef, int baseDmg, int baseGold, int dmgPerLevel,
      int defPerLevel, int healthPerLevel, AbilityDataRecord battlerAbilities, float physicalDefMultiplier, float fireDefMultiplier,
      float earthDefMultiplier, float airDefMultiplier, float waterDefMultiplier, float allDefMultiplier, float physicalAtkMultiplier,
      float fireAtkMultiplier, float earthAtkMultiplier, float airAtkMultiplier, float waterAtkMultiplier, float allAtkMultiplier,
      int deathSoundEffectId, int jumpSoundEffectId, float preContactLength, float preMagicLength, int baseXPReward,
      Enemy.Type enemyType, Battler battlerObject, string imagePath, Anim.Group animGroup, bool disableOnDeath, int lootGroupId) {

      BattlerData data = new BattlerData();

      data.setAllBattlerData(xp, apWhenDamaged, baseHealth, baseDef, baseDmg, baseGold, dmgPerLevel,
       defPerLevel, healthPerLevel, battlerAbilities, physicalDefMultiplier, fireDefMultiplier,
       earthDefMultiplier, airDefMultiplier, waterDefMultiplier, allDefMultiplier, physicalAtkMultiplier,
       fireAtkMultiplier, earthAtkMultiplier, airAtkMultiplier, waterAtkMultiplier, allAtkMultiplier,
       deathSoundEffectId, jumpSoundEffectId, preContactLength, preMagicLength, baseXPReward, enemyType, battlerObject, imagePath, animGroup, disableOnDeath, lootGroupId);

      return data;
   }

   protected void setAllBattlerData (BattlerData datacopy) {
      enemyName = datacopy.enemyName;

      currentXP = datacopy.currentXP;
      enemyType = datacopy.enemyType;
      animGroup = datacopy.animGroup;

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

      baseDefenseMultiplierSet.physicalDefenseMultiplier = datacopy.baseDefenseMultiplierSet.physicalDefenseMultiplier;
      baseDefenseMultiplierSet.fireDefenseMultiplier = datacopy.baseDefenseMultiplierSet.fireDefenseMultiplier;
      baseDefenseMultiplierSet.earthDefenseMultiplier = datacopy.baseDefenseMultiplierSet.earthDefenseMultiplier;
      baseDefenseMultiplierSet.airDefenseMultiplier = datacopy.baseDefenseMultiplierSet.airDefenseMultiplier;
      baseDefenseMultiplierSet.waterDefenseMultiplier = datacopy.baseDefenseMultiplierSet.waterDefenseMultiplier;
      baseDefenseMultiplierSet.allDefenseMultiplier = datacopy.baseDefenseMultiplierSet.allDefenseMultiplier;

      perLevelDefenseMultiplierSet.physicalDefenseMultiplierPerLevel = datacopy.perLevelDefenseMultiplierSet.physicalDefenseMultiplierPerLevel;
      perLevelDefenseMultiplierSet.fireDefenseMultiplierPerLevel = datacopy.perLevelDefenseMultiplierSet.fireDefenseMultiplierPerLevel;
      perLevelDefenseMultiplierSet.earthDefenseMultiplierPerLevel = datacopy.perLevelDefenseMultiplierSet.earthDefenseMultiplierPerLevel;
      perLevelDefenseMultiplierSet.airDefenseMultiplierPerLevel = datacopy.perLevelDefenseMultiplierSet.airDefenseMultiplierPerLevel;
      perLevelDefenseMultiplierSet.waterDefenseMultiplierPerLevel = datacopy.perLevelDefenseMultiplierSet.waterDefenseMultiplierPerLevel;
      perLevelDefenseMultiplierSet.allDefenseMultiplierPerLevel = datacopy.perLevelDefenseMultiplierSet.allDefenseMultiplierPerLevel;

      baseDamageMultiplierSet.physicalAttackMultiplier = datacopy.baseDamageMultiplierSet.physicalAttackMultiplier;
      baseDamageMultiplierSet.fireAttackMultiplier = datacopy.baseDamageMultiplierSet.fireAttackMultiplier;
      baseDamageMultiplierSet.earthAttackMultiplier = datacopy.baseDamageMultiplierSet.earthAttackMultiplier;
      baseDamageMultiplierSet.airAttackMultiplier = datacopy.baseDamageMultiplierSet.airAttackMultiplier;
      baseDamageMultiplierSet.waterAttackMultiplier = datacopy.baseDamageMultiplierSet.waterAttackMultiplier;
      baseDamageMultiplierSet.allAttackMultiplier = datacopy.baseDamageMultiplierSet.allAttackMultiplier;

      perLevelDamageMultiplierSet.physicalAttackMultiplierPerLevel = datacopy.perLevelDamageMultiplierSet.physicalAttackMultiplierPerLevel;
      perLevelDamageMultiplierSet.fireAttackMultiplierPerLevel = datacopy.perLevelDamageMultiplierSet.fireAttackMultiplierPerLevel;
      perLevelDamageMultiplierSet.earthAttackMultiplierPerLevel = datacopy.perLevelDamageMultiplierSet.earthAttackMultiplierPerLevel;
      perLevelDamageMultiplierSet.airAttackMultiplierPerLevel = datacopy.perLevelDamageMultiplierSet.airAttackMultiplierPerLevel;
      perLevelDamageMultiplierSet.waterAttackMultiplierPerLevel = datacopy.perLevelDamageMultiplierSet.waterAttackMultiplierPerLevel;
      perLevelDamageMultiplierSet.allAttackMultiplierPerLevel = datacopy.perLevelDamageMultiplierSet.allAttackMultiplierPerLevel;

      deathSoundEffectId = datacopy.deathSoundEffectId;
      jumpSoundEffectId = datacopy.jumpSoundEffectId;

      preContactLength = datacopy.preContactLength;
      preMagicLength = datacopy.preMagicLength;

      imagePath = datacopy.imagePath;
      disableOnDeath = datacopy.disableOnDeath;
      lootGroupId = datacopy.lootGroupId;
   }

   protected void setAllBattlerData (int xp, int apWhenDamaged, int baseHealth, int baseDef, int baseDmg, int baseGold, int dmgPerLevel,
      int defPerLevel, int healthPerLevel, AbilityDataRecord battlerAbilities, float physicalDefMultiplier, float fireDefMultiplier,
      float earthDefMultiplier, float airDefMultiplier, float waterDefMultiplier, float allDefMultiplier, float physicalAtkMultiplier,
      float fireAtkMultiplier, float earthAtkMultiplier, float airAtkMultiplier, float waterAtkMultiplier, float allAtkMultiplier,
      int deathSoundEffectId, int jumpSoundEffectId, float preContactLength, float preMagicLength, int baseXPReward,
      Enemy.Type enemyType, Battler battlerObject, string imagePath, Anim.Group animGroup, bool disableOnDeath, int lootGroupId) {

      this.currentXP = xp;
      this.enemyType = enemyType;
      this.animGroup = animGroup;

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

      this.baseDefenseMultiplierSet.physicalDefenseMultiplier = physicalDefMultiplier;
      this.baseDefenseMultiplierSet.fireDefenseMultiplier = fireDefMultiplier;
      this.baseDefenseMultiplierSet.earthDefenseMultiplier = earthDefMultiplier;
      this.baseDefenseMultiplierSet.airDefenseMultiplier = airDefMultiplier;
      this.baseDefenseMultiplierSet.waterDefenseMultiplier = waterDefMultiplier;
      this.baseDefenseMultiplierSet.allDefenseMultiplier = allDefMultiplier;

      this.baseDamageMultiplierSet.physicalAttackMultiplier = physicalAtkMultiplier;
      this.baseDamageMultiplierSet.fireAttackMultiplier = fireAtkMultiplier;
      this.baseDamageMultiplierSet.earthAttackMultiplier = earthAtkMultiplier;
      this.baseDamageMultiplierSet.airAttackMultiplier = airAtkMultiplier;
      this.baseDamageMultiplierSet.waterAttackMultiplier = waterAtkMultiplier;
      this.baseDamageMultiplierSet.allAttackMultiplier = allAtkMultiplier;

      this.deathSoundEffectId = deathSoundEffectId;
      this.jumpSoundEffectId = jumpSoundEffectId;

      this.preContactLength = preContactLength;
      this.preMagicLength = preMagicLength;
      this.imagePath = imagePath;

      this.disableOnDeath = disableOnDeath;
      this.lootGroupId = lootGroupId;
   }

   #region Helper Class

   [Serializable]
   public class BaseDamageMultiplierSet
   {
      // Element attack multiplier values
      public float physicalAttackMultiplier;
      public float fireAttackMultiplier;
      public float earthAttackMultiplier;
      public float airAttackMultiplier;
      public float waterAttackMultiplier;
      public float allAttackMultiplier;

      public StringTranslation stringTranslation = new StringTranslation();

      public void updateStringValues () {
         stringTranslation.updateStringValues(physicalAttackMultiplier, fireAttackMultiplier,
            earthAttackMultiplier, airAttackMultiplier,
            waterAttackMultiplier, allAttackMultiplier);
      }

      public void translateStringValues () {
         physicalAttackMultiplier = float.Parse(stringTranslation.attributeValue[0]);
         fireAttackMultiplier = float.Parse(stringTranslation.attributeValue[1]);
         earthAttackMultiplier = float.Parse(stringTranslation.attributeValue[2]);
         airAttackMultiplier = float.Parse(stringTranslation.attributeValue[3]);
         waterAttackMultiplier = float.Parse(stringTranslation.attributeValue[4]);
         allAttackMultiplier = float.Parse(stringTranslation.attributeValue[5]);
      }
   }

   [Serializable]
   public class PerLevelDamageMultiplierSet {
      // Element attack multiplier values PerLevel
      public float physicalAttackMultiplierPerLevel;
      public float fireAttackMultiplierPerLevel;
      public float earthAttackMultiplierPerLevel;
      public float airAttackMultiplierPerLevel;
      public float waterAttackMultiplierPerLevel;
      public float allAttackMultiplierPerLevel;

      public StringTranslation stringTranslation = new StringTranslation();

      public void updateStringValues () {
         stringTranslation.updateStringValues(physicalAttackMultiplierPerLevel, fireAttackMultiplierPerLevel,
            earthAttackMultiplierPerLevel, airAttackMultiplierPerLevel,
            waterAttackMultiplierPerLevel, allAttackMultiplierPerLevel);
      }

      public void translateStringValues () {
         physicalAttackMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[0]);
         fireAttackMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[1]);
         earthAttackMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[2]);
         airAttackMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[3]);
         waterAttackMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[4]);
         allAttackMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[5]); 
      }
   }

   [Serializable]
   public class BaseDefenseMultiplierSet {
      // Element defense multiplier values
      public float physicalDefenseMultiplier;
      public float fireDefenseMultiplier;
      public float earthDefenseMultiplier;
      public float airDefenseMultiplier;
      public float waterDefenseMultiplier;
      public float allDefenseMultiplier;

      public StringTranslation stringTranslation = new StringTranslation();

      public void updateStringValues () {
         stringTranslation.updateStringValues(physicalDefenseMultiplier, fireDefenseMultiplier,
            earthDefenseMultiplier, airDefenseMultiplier,
            waterDefenseMultiplier, allDefenseMultiplier);
      }

      public void translateStringValues () {
         physicalDefenseMultiplier = float.Parse(stringTranslation.attributeValue[0]);
         fireDefenseMultiplier = float.Parse(stringTranslation.attributeValue[1]);
         earthDefenseMultiplier = float.Parse(stringTranslation.attributeValue[2]);
         airDefenseMultiplier = float.Parse(stringTranslation.attributeValue[3]);
         waterDefenseMultiplier = float.Parse(stringTranslation.attributeValue[4]);
         allDefenseMultiplier = float.Parse(stringTranslation.attributeValue[5]);
      }
   }

   [Serializable]
   public class PerLevelDefenseMultiplierSet {
      // Element defense multiplier values PerLevel
      public float physicalDefenseMultiplierPerLevel;
      public float fireDefenseMultiplierPerLevel;
      public float earthDefenseMultiplierPerLevel;
      public float airDefenseMultiplierPerLevel;
      public float waterDefenseMultiplierPerLevel;
      public float allDefenseMultiplierPerLevel;

      public StringTranslation stringTranslation = new StringTranslation();

      public void updateStringValues () {
         stringTranslation.updateStringValues(physicalDefenseMultiplierPerLevel, fireDefenseMultiplierPerLevel,
            earthDefenseMultiplierPerLevel, airDefenseMultiplierPerLevel,
            waterDefenseMultiplierPerLevel, allDefenseMultiplierPerLevel);
      }

      public void translateStringValues () {
         physicalDefenseMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[0]);
         fireDefenseMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[1]);
         earthDefenseMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[2]);
         airDefenseMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[3]);
         waterDefenseMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[4]);
         allDefenseMultiplierPerLevel = float.Parse(stringTranslation.attributeValue[5]);
      }
   }

   public class StringTranslation
   {
      public string[] attributeValue;

      public void updateStringValues (float physical, float fire, float earth, float air, float water, float all) {
         List<string> stringList = new List<string>();
         stringList.Add(physical.ToString("f2"));
         stringList.Add(fire.ToString("f2"));
         stringList.Add(earth.ToString("f2"));
         stringList.Add(air.ToString("f2"));
         stringList.Add(water.ToString("f2"));
         stringList.Add(all.ToString("f2"));

         attributeValue = stringList.ToArray();
      }
   }

   #endregion
}
