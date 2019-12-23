using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class DamageSimulatorPanel : MonoBehaviour {
   #region Public Variables

   // Player pref keys
   public const string Pref_Class = "Class",
      Pref_Specialty = "Specialty",
      Pref_Faction = "Faction",
      Pref_SkillName = "Skill",
      Pref_MonsterName = "Monster",
      Pref_PlayerLvl = "PlayerLevel",
      Pref_EnemyLvl = "EnemyLevel",
      Pref_WeaponDmg = "WeapnDmg",
      Pref_ArmorDef = "ArmorDef";

   // Reference to the popup
   public GenericSelectionPopup genericPopup;

   // Player class selection
   public Button classButton,
      factionButton,
      specialtyButton;
   public Text classText,
      factionText,
      specialtyText;

   // Monster selection
   public Button monsterButton;
   public Text monsterText;

   // Ability selection
   public Button abilityButton;
   public Text abilityText;

   // Defense and Attack UI
   public BattlerAttackUI playerAttackUI;
   public BattlerDefenseUI enemyDefenseUI;
   public BattlerAttackUI enemyAttackUI;
   public BattlerDefenseUI playerDefenseUI;

   // Weapon custom damage
   public InputField weaponDmgPhys, weaponDmgFire, weaponDmgEarth, weaponDmgAir, weaponDmgWater;
   public InputField armorDefPhys, armorDefFire, armorDefEarth, armorDefAir, armorDefWater;

   // Computed damage results
   public Text[] outputPhysText, outputFireText, outputEarthText, outputAirText, outputWaterText;
   public Text[] outputPhysDefText, outputFireDefText, outputEarthDefText, outputAirDefText, outputWaterDefText;
   public float outputPhys, outputFire, outputEarth, outputAir, outputWater;

   // Base stats
   public float baseDamage, baseDefense;
   public float baseDmgLvl, baseDefLvl;

   // Computes the combat damage
   public Button computePlayerDamageButton, computePlayerDefenseButton;

   // Holds list for togglers
   public List<TogglerClass> togglerList;

   // Base damage and defense indicators
   public Text baseSkillDmgText, baseAtkText , baseEnemyDefText, baseDmgAfterClassText;
   public Text baseDefText, baseEnemyAtkText, baseDefAfterClassText;

   // Level's for enemy and players
   public InputField playerLevel, enemyLevel;

   // Stat index for class iteration
   public int classStatIndex = 0;

   // Determines the generic decimal value used by output texts
   public const string decimalVal = "f2";

   #endregion

   private void Awake () {
      initButtonListeners();

      initializePrefs();

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private void processEnemyAttack () {
      BattlerData fetchedMonsterData = MonsterManager.self.getAllMonsterData().Find(_ => _.enemyType.ToString() == monsterText.text);
      int level = int.Parse(enemyLevel.text);
      float baseAttack = fetchedMonsterData.baseDamage + (fetchedMonsterData.damagePerLevel * level);
      baseEnemyAtkText.text = baseAttack.ToString();

      // Default value setup
      float defaultValue = 1;
      enemyAttackUI.physicalDamageMultiplier = defaultValue;
      enemyAttackUI.fireDamageMultiplier = defaultValue;
      enemyAttackUI.waterDamageMultiplier = defaultValue;
      enemyAttackUI.earthDamageMultiplier = defaultValue;
      enemyAttackUI.airDamageMultiplier = defaultValue;

      // Setup base attack
      enemyAttackUI.physicalDamageMultiplierBase = fetchedMonsterData.baseDamageMultiplierSet.physicalAttackMultiplier;
      enemyAttackUI.fireDamageMultiplierBase = fetchedMonsterData.baseDamageMultiplierSet.fireAttackMultiplier;
      enemyAttackUI.earthDamageMultiplierBase = fetchedMonsterData.baseDamageMultiplierSet.earthAttackMultiplier;
      enemyAttackUI.airDamageMultiplierBase = fetchedMonsterData.baseDamageMultiplierSet.airAttackMultiplier;
      enemyAttackUI.waterDamageMultiplierBase = fetchedMonsterData.baseDamageMultiplierSet.waterAttackMultiplier;

      // Setup initial attack per level
      enemyAttackUI.physicalDamageMultiplierPerLvl = fetchedMonsterData.perLevelDamageMultiplierSet.physicalAttackMultiplierPerLevel;
      enemyAttackUI.fireDamageMultiplierPerLvl = fetchedMonsterData.perLevelDamageMultiplierSet.fireAttackMultiplierPerLevel;
      enemyAttackUI.earthDamageMultiplierPerLvl = fetchedMonsterData.perLevelDamageMultiplierSet.earthAttackMultiplierPerLevel;
      enemyAttackUI.airDamageMultiplierPerLvl = fetchedMonsterData.perLevelDamageMultiplierSet.airAttackMultiplierPerLevel;
      enemyAttackUI.waterDamageMultiplierPerLvl = fetchedMonsterData.perLevelDamageMultiplierSet.waterAttackMultiplierPerLevel;

      // Compute total attack considering (base attack and attack per level)
      enemyAttackUI.physicalDamageMultiplier = (level * enemyAttackUI.physicalDamageMultiplierPerLvl) + Mathf.Abs(enemyAttackUI.physicalDamageMultiplierBase);
      enemyAttackUI.fireDamageMultiplier = (level * enemyAttackUI.fireDamageMultiplierPerLvl) + Mathf.Abs(enemyAttackUI.fireDamageMultiplierBase);
      enemyAttackUI.earthDamageMultiplier = (level * enemyAttackUI.earthDamageMultiplierPerLvl) + Mathf.Abs(enemyAttackUI.earthDamageMultiplierBase);
      enemyAttackUI.airDamageMultiplier = (level * enemyAttackUI.airDamageMultiplierPerLvl) + Mathf.Abs(enemyAttackUI.airDamageMultiplierBase);
      enemyAttackUI.waterDamageMultiplier = (level * enemyAttackUI.waterDamageMultiplierPerLvl) + Mathf.Abs(enemyAttackUI.waterDamageMultiplierBase);

      // Final output of each elemental attack
      enemyAttackUI.outputDamagePhys = enemyAttackUI.physicalDamageMultiplier * baseAttack;
      enemyAttackUI.outputDamageFire = enemyAttackUI.fireDamageMultiplier * baseAttack;
      enemyAttackUI.outputDamageEarth = enemyAttackUI.earthDamageMultiplier * baseAttack;
      enemyAttackUI.outputDamageAir = enemyAttackUI.airDamageMultiplier * baseAttack;
      enemyAttackUI.outputDamageWater = enemyAttackUI.waterDamageMultiplier * baseAttack;

      int lastIndex = 3;

      // Total base attack 
      enemyAttackUI.physicalDmgBreakdown.damageText[lastIndex].text = enemyAttackUI.physicalDamageMultiplierBase.ToString(decimalVal);
      enemyAttackUI.fireDmgBreakdown.damageText[lastIndex].text = enemyAttackUI.fireDamageMultiplierBase.ToString(decimalVal);
      enemyAttackUI.earthDmgBreakdown.damageText[lastIndex].text = enemyAttackUI.earthDamageMultiplierBase.ToString(decimalVal);
      enemyAttackUI.airDmgBreakdown.damageText[lastIndex].text = enemyAttackUI.airDamageMultiplierBase.ToString(decimalVal);
      enemyAttackUI.waterDmgBreakdown.damageText[lastIndex].text = enemyAttackUI.waterDamageMultiplierBase.ToString(decimalVal);

      string additiveString = level + " * ";

      // Total per level attack 
      enemyAttackUI.physicalDmgBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyAttackUI.physicalDamageMultiplierPerLvl.ToString(decimalVal);
      enemyAttackUI.fireDmgBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyAttackUI.fireDamageMultiplierPerLvl.ToString(decimalVal);
      enemyAttackUI.earthDmgBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyAttackUI.earthDamageMultiplierPerLvl.ToString(decimalVal);
      enemyAttackUI.airDmgBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyAttackUI.airDamageMultiplierPerLvl.ToString(decimalVal);
      enemyAttackUI.waterDmgBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyAttackUI.waterDamageMultiplierPerLvl.ToString(decimalVal);

      // Final output computation for all attack
      enemyAttackUI.physicalDmgBreakdown.totalDamage.text = enemyAttackUI.outputDamagePhys.ToString(decimalVal);
      enemyAttackUI.fireDmgBreakdown.totalDamage.text = enemyAttackUI.outputDamageFire.ToString(decimalVal);
      enemyAttackUI.earthDmgBreakdown.totalDamage.text = enemyAttackUI.outputDamageEarth.ToString(decimalVal);
      enemyAttackUI.airDmgBreakdown.totalDamage.text = enemyAttackUI.outputDamageAir.ToString(decimalVal);
      enemyAttackUI.waterDmgBreakdown.totalDamage.text = enemyAttackUI.outputDamageWater.ToString(decimalVal);

      // Final computation breakdown for all defense
      additiveString = baseAttack + " * ";
      enemyAttackUI.physicalDmgBreakdown.totalMultiplier.text = additiveString + enemyAttackUI.physicalDamageMultiplier.ToString(decimalVal);
      enemyAttackUI.fireDmgBreakdown.totalMultiplier.text = additiveString + enemyAttackUI.fireDamageMultiplier.ToString(decimalVal);
      enemyAttackUI.earthDmgBreakdown.totalMultiplier.text = additiveString + enemyAttackUI.earthDamageMultiplier.ToString(decimalVal);
      enemyAttackUI.airDmgBreakdown.totalMultiplier.text = additiveString + enemyAttackUI.airDamageMultiplier.ToString(decimalVal);
      enemyAttackUI.waterDmgBreakdown.totalMultiplier.text = additiveString + enemyAttackUI.waterDamageMultiplier.ToString(decimalVal);
   }

   private void processEnemyDefense () {
      BattlerData fetchedMonsterData = MonsterManager.self.getAllMonsterData().Find(_ => _.enemyType.ToString() == monsterText.text);
      int level = int.Parse(enemyLevel.text);
      float baseDef = fetchedMonsterData.baseDefense + (fetchedMonsterData.defensePerLevel * level);
      baseEnemyDefText.text = baseDef.ToString();

      // Default value setup
      float defaultValue = 1;
      enemyDefenseUI.physicalDefenseMultiplier = defaultValue;
      enemyDefenseUI.fireDefenseMultiplier = defaultValue;
      enemyDefenseUI.earthDefenseMultiplier = defaultValue;
      enemyDefenseUI.airDefenseMultiplier = defaultValue;
      enemyDefenseUI.waterDefenseMultiplier = defaultValue;

      // Setup base defense
      enemyDefenseUI.physicalDefenseMultiplierBase = fetchedMonsterData.baseDefenseMultiplierSet.physicalDefenseMultiplier;
      enemyDefenseUI.fireDefenseMultiplierBase = fetchedMonsterData.baseDefenseMultiplierSet.fireDefenseMultiplier;
      enemyDefenseUI.earthDefenseMultiplierBase = fetchedMonsterData.baseDefenseMultiplierSet.earthDefenseMultiplier;
      enemyDefenseUI.airDefenseMultiplierBase = fetchedMonsterData.baseDefenseMultiplierSet.airDefenseMultiplier;
      enemyDefenseUI.waterDefenseMultiplierBase = fetchedMonsterData.baseDefenseMultiplierSet.waterDefenseMultiplier;

      // Setup initial defense per level
      enemyDefenseUI.physicalDefenseMultiplierPerLvl = fetchedMonsterData.perLevelDefenseMultiplierSet.physicalDefenseMultiplierPerLevel;
      enemyDefenseUI.fireDefenseMultiplierPerLvl = fetchedMonsterData.perLevelDefenseMultiplierSet.fireDefenseMultiplierPerLevel;
      enemyDefenseUI.earthDefenseMultiplierPerLvl = fetchedMonsterData.perLevelDefenseMultiplierSet.earthDefenseMultiplierPerLevel;
      enemyDefenseUI.airDefenseMultiplierPerLvl = fetchedMonsterData.perLevelDefenseMultiplierSet.airDefenseMultiplierPerLevel;
      enemyDefenseUI.waterDefenseMultiplierPerLvl = fetchedMonsterData.perLevelDefenseMultiplierSet.waterDefenseMultiplierPerLevel;

      // Compute total defense considering (base defense and defense per level)
      enemyDefenseUI.physicalDefenseMultiplier = (level * enemyDefenseUI.physicalDefenseMultiplierPerLvl) + Mathf.Abs(enemyDefenseUI.physicalDefenseMultiplierBase);
      enemyDefenseUI.fireDefenseMultiplier = (level * enemyDefenseUI.fireDefenseMultiplierPerLvl) + Mathf.Abs(enemyDefenseUI.fireDefenseMultiplierBase);
      enemyDefenseUI.earthDefenseMultiplier = (level * enemyDefenseUI.earthDefenseMultiplierPerLvl) + Mathf.Abs(enemyDefenseUI.earthDefenseMultiplierBase);
      enemyDefenseUI.airDefenseMultiplier = (level * enemyDefenseUI.airDefenseMultiplierPerLvl) + Mathf.Abs(enemyDefenseUI.airDefenseMultiplierBase);
      enemyDefenseUI.waterDefenseMultiplier = (level * enemyDefenseUI.waterDefenseMultiplierPerLvl) + Mathf.Abs(enemyDefenseUI.waterDefenseMultiplierBase);

      // Final output of each elemental defense
      enemyDefenseUI.outputDefensePhys = enemyDefenseUI.physicalDefenseMultiplier * baseDef;
      enemyDefenseUI.outputDefenseFire = enemyDefenseUI.fireDefenseMultiplier * baseDef;
      enemyDefenseUI.outputDefenseEarth = enemyDefenseUI.earthDefenseMultiplier * baseDef;
      enemyDefenseUI.outputDefenseAir = enemyDefenseUI.airDefenseMultiplier * baseDef;
      enemyDefenseUI.outputDefenseWater = enemyDefenseUI.waterDefenseMultiplier * baseDef;

      int lastIndex = 3;

      // Total base defense 
      enemyDefenseUI.physicalDefBreakdown.damageText[lastIndex].text = enemyDefenseUI.physicalDefenseMultiplierBase.ToString(decimalVal);
      enemyDefenseUI.fireDefBreakdown.damageText[lastIndex].text = enemyDefenseUI.fireDefenseMultiplierBase.ToString(decimalVal);
      enemyDefenseUI.earthDefBreakdown.damageText[lastIndex].text = enemyDefenseUI.earthDefenseMultiplierBase.ToString(decimalVal);
      enemyDefenseUI.airDefBreakdown.damageText[lastIndex].text = enemyDefenseUI.airDefenseMultiplierBase.ToString(decimalVal);
      enemyDefenseUI.waterDefBreakdown.damageText[lastIndex].text = enemyDefenseUI.waterDefenseMultiplierBase.ToString(decimalVal);

      string additiveString = level+ " * ";

      // Total per level defense 
      enemyDefenseUI.physicalDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyDefenseUI.physicalDefenseMultiplierPerLvl.ToString(decimalVal);
      enemyDefenseUI.fireDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyDefenseUI.fireDefenseMultiplierPerLvl.ToString(decimalVal);
      enemyDefenseUI.earthDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyDefenseUI.earthDefenseMultiplierPerLvl.ToString(decimalVal);
      enemyDefenseUI.airDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyDefenseUI.airDefenseMultiplierPerLvl.ToString(decimalVal);
      enemyDefenseUI.waterDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + enemyDefenseUI.waterDefenseMultiplierPerLvl.ToString(decimalVal);

      // Final output computation for all defense
      enemyDefenseUI.physicalDefBreakdown.totalDamage.text = enemyDefenseUI.outputDefensePhys.ToString(decimalVal);
      enemyDefenseUI.fireDefBreakdown.totalDamage.text = enemyDefenseUI.outputDefenseFire.ToString(decimalVal);
      enemyDefenseUI.earthDefBreakdown.totalDamage.text = enemyDefenseUI.outputDefenseEarth.ToString(decimalVal);
      enemyDefenseUI.airDefBreakdown.totalDamage.text = enemyDefenseUI.outputDefenseAir.ToString(decimalVal);
      enemyDefenseUI.waterDefBreakdown.totalDamage.text = enemyDefenseUI.outputDefenseWater.ToString(decimalVal);

      // Final computation breakdown for all defense
      additiveString = baseDef + " * ";
      enemyDefenseUI.physicalDefBreakdown.totalMultiplier.text = additiveString + enemyDefenseUI.physicalDefenseMultiplier.ToString(decimalVal);
      enemyDefenseUI.fireDefBreakdown.totalMultiplier.text = additiveString + enemyDefenseUI.fireDefenseMultiplier.ToString(decimalVal);
      enemyDefenseUI.earthDefBreakdown.totalMultiplier.text = additiveString + enemyDefenseUI.earthDefenseMultiplier.ToString(decimalVal);
      enemyDefenseUI.airDefBreakdown.totalMultiplier.text = additiveString + enemyDefenseUI.airDefenseMultiplier.ToString(decimalVal);
      enemyDefenseUI.waterDefBreakdown.totalMultiplier.text = additiveString + enemyDefenseUI.waterDefenseMultiplier.ToString(decimalVal);
   }

   private void processPlayerTypeComputations () {
      classStatIndex = 0;

      Class.Type classType = (Class.Type) Enum.Parse(typeof(Class.Type), classText.text);
      Specialty.Type specialtyType = (Specialty.Type) Enum.Parse(typeof(Specialty.Type), specialtyText.text);
      Faction.Type factionType = (Faction.Type) Enum.Parse(typeof(Faction.Type), factionText.text);

      // Class Setup
      PlayerClassData classStat = ClassManager.self.getClassData(classType);
      if (classStat == null) {
         Debug.LogError("Class is Missing: " + specialtyType);
      } else {
         UserDefaultStats classStatDefault = classStat.playerStats.userDefaultStats;
         UserCombatStats classStatCombat = classStat.playerStats.userCombatStats;
         addDefaultStats(classStatDefault);
         addCombatStats(classStatCombat);
      }

      // Faction Setup
      PlayerFactionData factionStat = FactionManager.self.getFactionData(factionType);
      if (factionStat == null) {
         Debug.LogError("Faction is Missing: " + specialtyType);
      } else {
         UserDefaultStats factionStatDefault = factionStat.playerStats.userDefaultStats;
         UserCombatStats factionStatCombat = factionStat.playerStats.userCombatStats;
         addDefaultStats(factionStatDefault);
         addCombatStats(factionStatCombat);
      }

      // Specialty Setup
      PlayerSpecialtyData specialtyStat = SpecialtyManager.self.getSpecialtyData(specialtyType);
      if (specialtyStat == null) {
         Debug.LogError("Specialty is Missing: " + specialtyType);
      } else {
         UserDefaultStats specialtyStatDefault = specialtyStat.playerStats.userDefaultStats;
         UserCombatStats specialtyStatCombat = specialtyStat.playerStats.userCombatStats;
         addDefaultStats(specialtyStatDefault);
         addCombatStats(specialtyStatCombat);
      }
   }

   private void processPlayerOutputDamage () {
      AttackAbilityData fetchedAbility = AbilityManager.self.allGameAbilities.Find(_ => _.itemName == abilityText.text) as AttackAbilityData;
      BattlerData battleData = MonsterManager.self.getMonster(Enemy.Type.PlayerBattler);
      int level = int.Parse(playerLevel.text);

      baseDamage = battleData.baseDamage;
      baseDmgLvl = battleData.damagePerLevel;

      baseAtkText.text = baseDamage + " + (" + level + " * " + baseDmgLvl + ")";

      baseSkillDmgText.text = fetchedAbility.baseDamage.ToString();

      // Sets the values to default
      setupDefaultValues();

      // Sets up the computation attributed by the class / faction / specialization
      processPlayerTypeComputations();

      int lastIndex = 3;
      // Diplays the base multipliers
      playerAttackUI.physicalDmgBreakdown.damageText[lastIndex].text = playerAttackUI.physicalDamageMultiplierBase.ToString(decimalVal);
      playerAttackUI.fireDmgBreakdown.damageText[lastIndex].text = playerAttackUI.fireDamageMultiplierBase.ToString(decimalVal);
      playerAttackUI.waterDmgBreakdown.damageText[lastIndex].text = playerAttackUI.waterDamageMultiplierBase.ToString(decimalVal);
      playerAttackUI.airDmgBreakdown.damageText[lastIndex].text = playerAttackUI.airDamageMultiplierBase.ToString(decimalVal);
      playerAttackUI.earthDmgBreakdown.damageText[lastIndex].text = playerAttackUI.earthDamageMultiplierBase.ToString(decimalVal);

      // Diplays the per level multipliers
      playerAttackUI.physicalDmgBreakdown.damagePerLevelText[lastIndex].text = playerAttackUI.physicalDamageMultiplierPerLvl.ToString(decimalVal);
      playerAttackUI.fireDmgBreakdown.damagePerLevelText[lastIndex].text = playerAttackUI.fireDamageMultiplierPerLvl.ToString(decimalVal);
      playerAttackUI.waterDmgBreakdown.damagePerLevelText[lastIndex].text = playerAttackUI.waterDamageMultiplierPerLvl.ToString(decimalVal);
      playerAttackUI.airDmgBreakdown.damagePerLevelText[lastIndex].text = playerAttackUI.airDamageMultiplierPerLvl.ToString(decimalVal);
      playerAttackUI.earthDmgBreakdown.damagePerLevelText[lastIndex].text = playerAttackUI.earthDamageMultiplierPerLvl.ToString(decimalVal);

      savePrefs();
   }

   private void processPlayerOutputDefense () {
      BattlerData battleData = MonsterManager.self.getMonster(Enemy.Type.PlayerBattler);
      int level = int.Parse(playerLevel.text);

      baseDefense = battleData.baseDefense;
      baseDefLvl = battleData.defensePerLevel;

      float playerDef = baseDefense + (baseDefLvl * level);
      baseDefText.text = playerDef.ToString();

      // Sets the values to default
      setupDefaultValues();

      // Sets up the computation attributed by the class / faction / specialization
      processPlayerTypeComputations();

      int lastIndex = 3;
      // Diplays the base multipliers
      playerDefenseUI.physicalDefBreakdown.damageText[lastIndex].text = playerDefenseUI.physicalDefenseMultiplierBase.ToString(decimalVal);
      playerDefenseUI.fireDefBreakdown.damageText[lastIndex].text = playerDefenseUI.fireDefenseMultiplierBase.ToString(decimalVal);
      playerDefenseUI.waterDefBreakdown.damageText[lastIndex].text = playerDefenseUI.waterDefenseMultiplierBase.ToString(decimalVal);
      playerDefenseUI.airDefBreakdown.damageText[lastIndex].text = playerDefenseUI.airDefenseMultiplierBase.ToString(decimalVal);
      playerDefenseUI.earthDefBreakdown.damageText[lastIndex].text = playerDefenseUI.earthDefenseMultiplierBase.ToString(decimalVal);

      // Diplays the per level multipliers
      playerDefenseUI.physicalDefBreakdown.damagePerLevelText[lastIndex].text = playerDefenseUI.physicalDefenseMultiplierPerLvl.ToString(decimalVal);
      playerDefenseUI.fireDefBreakdown.damagePerLevelText[lastIndex].text = playerDefenseUI.fireDefenseMultiplierPerLvl.ToString(decimalVal);
      playerDefenseUI.waterDefBreakdown.damagePerLevelText[lastIndex].text = playerDefenseUI.waterDefenseMultiplierPerLvl.ToString(decimalVal);
      playerDefenseUI.airDefBreakdown.damagePerLevelText[lastIndex].text = playerDefenseUI.airDefenseMultiplierPerLvl.ToString(decimalVal);
      playerDefenseUI.earthDefBreakdown.damagePerLevelText[lastIndex].text = playerDefenseUI.earthDefenseMultiplierPerLvl.ToString(decimalVal);

      savePrefs();
   }

   private void logOutputPlayerDefense () {
      int level = int.Parse(playerLevel.text);
      float baseDef = baseDefense + (baseDefLvl * level);

      float physicalArmorDef = float.Parse(armorDefPhys.text);
      float fireArmorDef = float.Parse(armorDefFire.text);
      float waterArmorDef = float.Parse(armorDefWater.text);
      float airArmorDef = float.Parse(armorDefAir.text);
      float earthArmorDef = float.Parse(armorDefEarth.text);

      // Computes the output damage
      playerDefenseUI.outputDefenseAir = (baseDef + airArmorDef) * playerDefenseUI.airDefenseMultiplier;
      playerDefenseUI.outputDefenseFire = (baseDef + fireArmorDef) * playerDefenseUI.fireDefenseMultiplier;
      playerDefenseUI.outputDefenseEarth = (baseDef + earthArmorDef) * playerDefenseUI.earthDefenseMultiplier;
      playerDefenseUI.outputDefenseWater = (baseDef + waterArmorDef) * playerDefenseUI.waterDefenseMultiplier;
      playerDefenseUI.outputDefensePhys = (baseDef + physicalArmorDef) * playerDefenseUI.physicalDefenseMultiplier;

      playerDefenseUI.physicalDefBreakdown.totalDamage.text = playerDefenseUI.outputDefensePhys.ToString(decimalVal);
      playerDefenseUI.fireDefBreakdown.totalDamage.text = playerDefenseUI.outputDefenseFire.ToString(decimalVal);
      playerDefenseUI.waterDefBreakdown.totalDamage.text = playerDefenseUI.outputDefenseWater.ToString(decimalVal);
      playerDefenseUI.airDefBreakdown.totalDamage.text = playerDefenseUI.outputDefenseAir.ToString(decimalVal);
      playerDefenseUI.earthDefBreakdown.totalDamage.text = playerDefenseUI.outputDefenseEarth.ToString(decimalVal);

      string additiveString = "";
      string computedString = "";

      // Computes the total breakdown damage
      computedString = (playerDefenseUI.physicalDefenseMultiplierBase + playerDefenseUI.physicalDefenseMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDef + physicalArmorDef) + " * " + computedString;
      playerDefenseUI.physicalDefBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerDefenseUI.fireDefenseMultiplierBase + playerDefenseUI.fireDefenseMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDef + fireArmorDef) + " * " + computedString;
      playerDefenseUI.fireDefBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerDefenseUI.waterDefenseMultiplierBase + playerDefenseUI.waterDefenseMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDef + waterArmorDef) + " * " + computedString;
      playerDefenseUI.waterDefBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerDefenseUI.airDefenseMultiplierBase + playerDefenseUI.airDefenseMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDef + airArmorDef) + " * " + computedString;
      playerDefenseUI.airDefBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerDefenseUI.earthDefenseMultiplierBase + playerDefenseUI.earthDefenseMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDef + earthArmorDef) + " * " + computedString;
      playerDefenseUI.earthDefBreakdown.totalMultiplier.text = additiveString;

      float phyComputation = resistantModifier(playerDefenseUI.outputDefensePhys, playerDefenseUI.physicalDefenseMultiplierBase, Element.Physical);
      float fireComputation = resistantModifier(playerDefenseUI.outputDefenseFire, playerDefenseUI.fireDefenseMultiplierBase, Element.Fire);
      float waterComputation = resistantModifier(playerDefenseUI.outputDefenseWater, playerDefenseUI.waterDefenseMultiplierBase, Element.Water);
      float airComputation = resistantModifier(playerDefenseUI.outputDefenseAir, playerDefenseUI.airDefenseMultiplierBase, Element.Air);
      float earthComputation = resistantModifier(playerDefenseUI.outputDefenseEarth, playerDefenseUI.earthDefenseMultiplierBase, Element.Earth);

      outputPhysDefText[0].text = (enemyAttackUI.outputDamagePhys * phyComputation).ToString(decimalVal);
      outputFireDefText[0].text = (enemyAttackUI.outputDamageFire * fireComputation).ToString(decimalVal);
      outputEarthDefText[0].text = (enemyAttackUI.outputDamageEarth * earthComputation).ToString(decimalVal);
      outputAirDefText[0].text = (enemyAttackUI.outputDamageAir * airComputation).ToString(decimalVal);
      outputWaterDefText[0].text = (enemyAttackUI.outputDamageWater * waterComputation).ToString(decimalVal);

      float offensiveStanceMultiplier = 1.25f;
      outputPhysDefText[1].text = (offensiveStanceMultiplier * (playerDefenseUI.outputDefensePhys * phyComputation)).ToString(decimalVal);
      outputFireDefText[1].text = (offensiveStanceMultiplier * (playerDefenseUI.outputDefenseFire * fireComputation)).ToString(decimalVal);
      outputEarthDefText[1].text = (offensiveStanceMultiplier * (playerDefenseUI.outputDefenseEarth * earthComputation)).ToString(decimalVal);
      outputAirDefText[1].text = (offensiveStanceMultiplier * (playerDefenseUI.outputDefenseAir * airComputation)).ToString(decimalVal);
      outputWaterDefText[1].text = (offensiveStanceMultiplier * (playerDefenseUI.outputDefenseWater * waterComputation)).ToString(decimalVal);

      float defensiveStanceMultiplier = .75f;
      outputPhysDefText[2].text = (defensiveStanceMultiplier * (playerDefenseUI.outputDefensePhys * phyComputation)).ToString(decimalVal);
      outputFireDefText[2].text = (defensiveStanceMultiplier * (playerDefenseUI.outputDefenseFire * fireComputation)).ToString(decimalVal);
      outputEarthDefText[2].text = (defensiveStanceMultiplier * (playerDefenseUI.outputDefenseEarth * earthComputation)).ToString(decimalVal);
      outputAirDefText[2].text = (defensiveStanceMultiplier * (playerDefenseUI.outputDefenseAir * airComputation)).ToString(decimalVal);
      outputWaterDefText[2].text = (defensiveStanceMultiplier * (playerDefenseUI.outputDefenseWater * waterComputation)).ToString(decimalVal);
   }

   private void logOutputPlayerDamage () {
      int level = int.Parse(playerLevel.text);
      float baseDmg = baseDamage + (baseDmgLvl * level);

      float physicalWeaponDmg = float.Parse(weaponDmgPhys.text);
      float fireWeaponDmg = float.Parse(weaponDmgFire.text);
      float waterWeaponDmg = float.Parse(weaponDmgWater.text);
      float airWeaponDmg = float.Parse(weaponDmgAir.text);
      float earthWeaponDmg = float.Parse(weaponDmgEarth.text);

      // Computes the output damage
      playerAttackUI.outputDamageAir = (baseDmg + airWeaponDmg) * playerAttackUI.airDamageMultiplier;
      playerAttackUI.outputDamageFire = (baseDmg + fireWeaponDmg) * playerAttackUI.fireDamageMultiplier;
      playerAttackUI.outputDamageEarth = (baseDmg + earthWeaponDmg) * playerAttackUI.earthDamageMultiplier;
      playerAttackUI.outputDamageWater = (baseDmg + waterWeaponDmg) * playerAttackUI.waterDamageMultiplier;
      playerAttackUI.outputDamagePhys = (baseDmg + physicalWeaponDmg) * playerAttackUI.physicalDamageMultiplier;

      playerAttackUI.physicalDmgBreakdown.totalDamage.text = playerAttackUI.outputDamagePhys.ToString(decimalVal);
      playerAttackUI.fireDmgBreakdown.totalDamage.text = playerAttackUI.outputDamageFire.ToString(decimalVal);
      playerAttackUI.waterDmgBreakdown.totalDamage.text = playerAttackUI.outputDamageWater.ToString(decimalVal);
      playerAttackUI.airDmgBreakdown.totalDamage.text = playerAttackUI.outputDamageAir.ToString(decimalVal);
      playerAttackUI.earthDmgBreakdown.totalDamage.text = playerAttackUI.outputDamageEarth.ToString(decimalVal);

      string additiveString = "";
      string computedString = "";

      // Computes the total breakdown damage
      computedString = (playerAttackUI.physicalDamageMultiplierBase + playerAttackUI.physicalDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDmg + physicalWeaponDmg) + " * " + computedString;
      playerAttackUI.physicalDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerAttackUI.fireDamageMultiplierBase + playerAttackUI.fireDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDmg + fireWeaponDmg) + " * " + computedString;
      playerAttackUI.fireDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerAttackUI.waterDamageMultiplierBase + playerAttackUI.waterDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDmg + waterWeaponDmg) + " * " + computedString;
      playerAttackUI.waterDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerAttackUI.airDamageMultiplierBase + playerAttackUI.airDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDmg + airWeaponDmg) + " * " + computedString;
      playerAttackUI.airDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (playerAttackUI.earthDamageMultiplierBase + playerAttackUI.earthDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = (baseDmg + earthWeaponDmg) + " * " + computedString;
      playerAttackUI.earthDmgBreakdown.totalMultiplier.text = additiveString;

      float phyComputation = resistantModifier(enemyDefenseUI.outputDefensePhys, enemyDefenseUI.physicalDefenseMultiplierBase, Element.Physical);
      float fireComputation = resistantModifier(enemyDefenseUI.outputDefenseFire, enemyDefenseUI.fireDefenseMultiplierBase, Element.Fire);
      float waterComputation = resistantModifier(enemyDefenseUI.outputDefenseWater, enemyDefenseUI.waterDefenseMultiplierBase, Element.Water);
      float airComputation = resistantModifier(enemyDefenseUI.outputDefenseAir, enemyDefenseUI.airDefenseMultiplierBase, Element.Air);
      float earthComputation = resistantModifier(enemyDefenseUI.outputDefenseEarth, enemyDefenseUI.earthDefenseMultiplierBase, Element.Earth);
     
      outputPhysText[0].text = (playerAttackUI.outputDamagePhys * phyComputation).ToString(decimalVal);
      outputFireText[0].text = (playerAttackUI.outputDamageFire * fireComputation).ToString(decimalVal);
      outputEarthText[0].text = (playerAttackUI.outputDamageEarth * earthComputation).ToString(decimalVal);
      outputAirText[0].text = (playerAttackUI.outputDamageAir * airComputation).ToString(decimalVal);
      outputWaterText[0].text = (playerAttackUI.outputDamageWater * waterComputation).ToString(decimalVal);

      float offensiveStanceMultiplier = 1.25f;
      outputPhysText[1].text = (offensiveStanceMultiplier * (playerAttackUI.outputDamagePhys * phyComputation)).ToString(decimalVal);
      outputFireText[1].text = (offensiveStanceMultiplier * (playerAttackUI.outputDamageFire * fireComputation)).ToString(decimalVal);
      outputEarthText[1].text = (offensiveStanceMultiplier * (playerAttackUI.outputDamageEarth * earthComputation)).ToString(decimalVal);
      outputAirText[1].text = (offensiveStanceMultiplier * (playerAttackUI.outputDamageAir * airComputation)).ToString(decimalVal);
      outputWaterText[1].text = (offensiveStanceMultiplier * (playerAttackUI.outputDamageWater * waterComputation)).ToString(decimalVal);

      float defensiveStanceMultiplier = .75f;
      outputPhysText[2].text = (defensiveStanceMultiplier * (playerAttackUI.outputDamagePhys * phyComputation)).ToString(decimalVal);
      outputFireText[2].text = (defensiveStanceMultiplier * (playerAttackUI.outputDamageFire * fireComputation)).ToString(decimalVal);
      outputEarthText[2].text = (defensiveStanceMultiplier * (playerAttackUI.outputDamageEarth * earthComputation)).ToString(decimalVal);
      outputAirText[2].text = (defensiveStanceMultiplier * (playerAttackUI.outputDamageAir * airComputation)).ToString(decimalVal);
      outputWaterText[2].text = (defensiveStanceMultiplier * (playerAttackUI.outputDamageWater * waterComputation)).ToString(decimalVal);
   }

   #region Setup

   private void initButtonListeners () {
      computePlayerDamageButton.onClick.AddListener(() => {
         processPlayerOutputDamage();
         processEnemyDefense();
         logOutputPlayerDamage();
      });
      computePlayerDefenseButton.onClick.AddListener(() => {
         processEnemyAttack();
         processPlayerOutputDefense();
         logOutputPlayerDefense();
      });

      classButton.onClick.AddListener(() => {
         genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerClassType, classText);
      });
      factionButton.onClick.AddListener(() => {
         genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerFactionType, factionText);
      });
      specialtyButton.onClick.AddListener(() => {
         genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.PlayerSpecialtyType, specialtyText);
      });
      monsterButton.onClick.AddListener(() => {
         genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.MonsterType, monsterText);
      });
      abilityButton.onClick.AddListener(() => {
         genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.AbilityType, abilityText);
      });
   }

   private void initializePrefs () {
      // Setup cached info
      classText.text = PlayerPrefs.GetString(Pref_Class, classText.text);
      factionText.text = PlayerPrefs.GetString(Pref_Faction, factionText.text);
      specialtyText.text = PlayerPrefs.GetString(Pref_Specialty, specialtyText.text);
      abilityText.text = PlayerPrefs.GetString(Pref_SkillName, abilityText.text);
      monsterText.text = PlayerPrefs.GetString(Pref_MonsterName, monsterText.text);
      playerLevel.text = PlayerPrefs.GetString(Pref_PlayerLvl, playerLevel.text);
      enemyLevel.text = PlayerPrefs.GetString(Pref_EnemyLvl, enemyLevel.text);

      weaponDmgPhys.text = PlayerPrefs.GetString(Pref_WeaponDmg + 1, weaponDmgPhys.text);
      weaponDmgFire.text = PlayerPrefs.GetString(Pref_WeaponDmg + 2, weaponDmgFire.text);
      weaponDmgAir.text = PlayerPrefs.GetString(Pref_WeaponDmg + 3, weaponDmgAir.text);
      weaponDmgWater.text = PlayerPrefs.GetString(Pref_WeaponDmg + 4, weaponDmgWater.text);
      weaponDmgEarth.text = PlayerPrefs.GetString(Pref_WeaponDmg + 5, weaponDmgEarth.text);

      armorDefPhys.text = PlayerPrefs.GetString(Pref_ArmorDef + 1, armorDefPhys.text);
      armorDefFire.text = PlayerPrefs.GetString(Pref_ArmorDef + 2, armorDefFire.text);
      armorDefAir.text = PlayerPrefs.GetString(Pref_ArmorDef + 3, armorDefAir.text);
      armorDefWater.text = PlayerPrefs.GetString(Pref_ArmorDef + 4, armorDefWater.text);
      armorDefEarth.text = PlayerPrefs.GetString(Pref_ArmorDef + 5, armorDefEarth.text);
   }

   private void setupDefaultValues () {
      float defaultVal = 0;

      playerAttackUI.physicalDamageMultiplier = defaultVal;
      playerAttackUI.fireDamageMultiplier = defaultVal;
      playerAttackUI.waterDamageMultiplier = defaultVal;
      playerAttackUI.earthDamageMultiplier = defaultVal;
      playerAttackUI.airDamageMultiplier = defaultVal;

      playerAttackUI.physicalDamageMultiplierBase = defaultVal;
      playerAttackUI.fireDamageMultiplierBase = defaultVal;
      playerAttackUI.earthDamageMultiplierBase = defaultVal;
      playerAttackUI.airDamageMultiplierBase = defaultVal;
      playerAttackUI.waterDamageMultiplierBase = defaultVal;

      playerAttackUI.physicalDamageMultiplierPerLvl = defaultVal;
      playerAttackUI.fireDamageMultiplierPerLvl = defaultVal;
      playerAttackUI.earthDamageMultiplierPerLvl = defaultVal;
      playerAttackUI.airDamageMultiplierPerLvl = defaultVal;
      playerAttackUI.waterDamageMultiplierPerLvl = defaultVal;

      playerDefenseUI.physicalDefenseMultiplier = 1;
      playerDefenseUI.fireDefenseMultiplier = 1;
      playerDefenseUI.waterDefenseMultiplier = 1;
      playerDefenseUI.airDefenseMultiplier = 1;
      playerDefenseUI.earthDefenseMultiplier = 1;

      playerDefenseUI.physicalDefenseMultiplierBase = defaultVal;
      playerDefenseUI.fireDefenseMultiplierBase = defaultVal;
      playerDefenseUI.earthDefenseMultiplierBase = defaultVal;
      playerDefenseUI.airDefenseMultiplierBase = defaultVal;
      playerDefenseUI.waterDefenseMultiplierBase = defaultVal;

      playerDefenseUI.physicalDefenseMultiplierPerLvl = defaultVal;
      playerDefenseUI.fireDefenseMultiplierPerLvl = defaultVal;
      playerDefenseUI.earthDefenseMultiplierPerLvl = defaultVal;
      playerDefenseUI.airDefenseMultiplierPerLvl = defaultVal;
      playerDefenseUI.waterDefenseMultiplierPerLvl = defaultVal;
   }

   private void savePrefs () {
      PlayerPrefs.SetString(Pref_Class, classText.text);
      PlayerPrefs.SetString(Pref_Faction, factionText.text);
      PlayerPrefs.SetString(Pref_Specialty, specialtyText.text);
      PlayerPrefs.SetString(Pref_SkillName, abilityText.text);
      PlayerPrefs.SetString(Pref_MonsterName, monsterText.text);

      PlayerPrefs.SetString(Pref_PlayerLvl, playerLevel.text);
      PlayerPrefs.SetString(Pref_EnemyLvl, enemyLevel.text);

      PlayerPrefs.SetString(Pref_WeaponDmg + 1, weaponDmgPhys.text);
      PlayerPrefs.SetString(Pref_WeaponDmg + 2, weaponDmgFire.text);
      PlayerPrefs.SetString(Pref_WeaponDmg + 3, weaponDmgAir.text);
      PlayerPrefs.SetString(Pref_WeaponDmg + 4, weaponDmgWater.text);
      PlayerPrefs.SetString(Pref_WeaponDmg + 5, weaponDmgEarth.text);

      PlayerPrefs.SetString(Pref_ArmorDef + 1, armorDefPhys.text);
      PlayerPrefs.SetString(Pref_ArmorDef + 2, armorDefFire.text);
      PlayerPrefs.SetString(Pref_ArmorDef + 3, armorDefAir.text);
      PlayerPrefs.SetString(Pref_ArmorDef + 4, armorDefWater.text);
      PlayerPrefs.SetString(Pref_ArmorDef + 5, armorDefEarth.text);
   }

   #endregion

   #region Utils

   private void addDefaultStats (UserDefaultStats stat) {
      int level = int.Parse(playerLevel.text);
      baseDamage += stat.bonusATK;
      baseDmgLvl += stat.bonusATKPerLevel;

      baseDefense += stat.bonusArmor;
      baseDefLvl += stat.armorPerLevel;

      if (classStatIndex == 2) {
         float attackFormula = baseDamage + (level * baseDmgLvl);
         float defenseFormula = baseDefense + (level * baseDefLvl);

         baseDmgAfterClassText.text = baseDamage + " + ([" + level + "] * " + baseDmgLvl + ") = " + attackFormula;
         baseDefAfterClassText.text = baseDefense + " + ([" + level + "] * " + baseDefLvl + ") = " + defenseFormula;
      }
   }  

   private void addCombatStats (UserCombatStats stat) {
      int level = int.Parse(playerLevel.text);

      // Setup base stats
      playerAttackUI.physicalDamageMultiplierBase += stat.bonusDamagePhys;
      playerAttackUI.fireDamageMultiplierBase += stat.bonusDamageFire;
      playerAttackUI.waterDamageMultiplierBase += stat.bonusDamageWater;
      playerAttackUI.earthDamageMultiplierBase += stat.bonusDamageEarth;
      playerAttackUI.airDamageMultiplierBase += stat.bonusDamageAir;

      playerDefenseUI.physicalDefenseMultiplierBase += stat.bonusDamagePhys;
      playerDefenseUI.fireDefenseMultiplierBase += stat.bonusResistanceFire;
      playerDefenseUI.waterDefenseMultiplierBase += stat.bonusResistanceWater;
      playerDefenseUI.airDefenseMultiplierBase += stat.bonusResistanceAir;
      playerDefenseUI.earthDefenseMultiplierBase += stat.bonusResistanceEarth;

      // Setup per level stats
      playerAttackUI.physicalDamageMultiplierPerLvl += stat.bonusDamagePhysicalPerLevel * level;
      playerAttackUI.fireDamageMultiplierPerLvl += stat.bonusDamageFirePerLevel * level;
      playerAttackUI.waterDamageMultiplierPerLvl += stat.bonusDamageWaterPerLevel * level;
      playerAttackUI.earthDamageMultiplierPerLvl += stat.bonusDamageEarthPerLevel * level;
      playerAttackUI.airDamageMultiplierPerLvl += stat.bonusDamageAirPerLevel * level;

      playerDefenseUI.physicalDefenseMultiplierPerLvl += stat.bonusResistancePhysPerLevel * level;
      playerDefenseUI.fireDefenseMultiplierPerLvl += stat.bonusResistanceFirePerLevel * level;
      playerDefenseUI.waterDefenseMultiplierPerLvl += stat.bonusResistanceWaterPerLevel * level;
      playerDefenseUI.airDefenseMultiplierPerLvl += stat.bonusResistanceAirPerLevel * level;
      playerDefenseUI.earthDefenseMultiplierPerLvl += stat.bonusResistanceEarthPerLevel * level;

      // Setup Output computation adding the base stats
      playerAttackUI.physicalDamageMultiplier += stat.bonusDamagePhys;
      playerAttackUI.fireDamageMultiplier += stat.bonusDamageFire;
      playerAttackUI.waterDamageMultiplier += stat.bonusDamageWater;
      playerAttackUI.earthDamageMultiplier += stat.bonusDamageEarth;
      playerAttackUI.airDamageMultiplier += stat.bonusDamageAir;

      float initialWAterResistance = playerDefenseUI.waterDefenseMultiplier;

      playerDefenseUI.physicalDefenseMultiplier += stat.bonusResistancePhys;
      playerDefenseUI.fireDefenseMultiplier += stat.bonusResistanceFire;
      playerDefenseUI.waterDefenseMultiplier += stat.bonusResistanceWater;
      playerDefenseUI.earthDefenseMultiplier += stat.bonusResistanceEarth;
      playerDefenseUI.airDefenseMultiplier += stat.bonusResistanceAir;

      // Setup Output computation adding the per level stats
      playerAttackUI.physicalDamageMultiplier += stat.bonusDamagePhysicalPerLevel * level;
      playerAttackUI.fireDamageMultiplier += stat.bonusDamageFirePerLevel * level;
      playerAttackUI.waterDamageMultiplier += stat.bonusDamageWaterPerLevel * level;
      playerAttackUI.earthDamageMultiplier += stat.bonusDamageEarthPerLevel * level;
      playerAttackUI.airDamageMultiplier += stat.bonusDamageAirPerLevel * level;

      playerDefenseUI.physicalDefenseMultiplier += (stat.bonusResistancePhysPerLevel * level);
      playerDefenseUI.fireDefenseMultiplier += (stat.bonusResistanceFirePerLevel * level);
      playerDefenseUI.waterDefenseMultiplier += (stat.bonusResistanceWaterPerLevel * level);
      playerDefenseUI.earthDefenseMultiplier += (stat.bonusResistanceEarthPerLevel * level);
      playerDefenseUI.airDefenseMultiplier += (stat.bonusResistanceAirPerLevel * level);

      // Registers the total multipliers for base stats
      playerAttackUI.physicalDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamagePhys.ToString(decimalVal);
      playerAttackUI.fireDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageFire.ToString(decimalVal);
      playerAttackUI.waterDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageWater.ToString(decimalVal);
      playerAttackUI.airDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageAir.ToString(decimalVal);
      playerAttackUI.earthDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageEarth.ToString(decimalVal);

      playerDefenseUI.physicalDefBreakdown.damageText[classStatIndex].text = stat.bonusResistancePhys.ToString(decimalVal);
      playerDefenseUI.fireDefBreakdown.damageText[classStatIndex].text = stat.bonusResistanceFire.ToString(decimalVal);
      playerDefenseUI.waterDefBreakdown.damageText[classStatIndex].text = stat.bonusResistanceWater.ToString(decimalVal);
      playerDefenseUI.airDefBreakdown.damageText[classStatIndex].text = stat.bonusResistanceAir.ToString(decimalVal);
      playerDefenseUI.earthDefBreakdown.damageText[classStatIndex].text = stat.bonusResistanceEarth.ToString(decimalVal);

      string additiveText = level + " * ";

      // Registers the total multipliers for per level stats
      playerAttackUI.physicalDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamagePhysicalPerLevel * level).ToString(decimalVal);
      playerAttackUI.fireDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageFirePerLevel * level).ToString(decimalVal);
      playerAttackUI.waterDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageWaterPerLevel * level).ToString(decimalVal);
      playerAttackUI.airDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageAirPerLevel * level).ToString(decimalVal);
      playerAttackUI.earthDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageEarthPerLevel * level).ToString(decimalVal);

      playerDefenseUI.physicalDefBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusResistancePhysPerLevel * level).ToString(decimalVal);
      playerDefenseUI.fireDefBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusResistanceFirePerLevel * level).ToString(decimalVal);
      playerDefenseUI.waterDefBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusResistanceWaterPerLevel * level).ToString(decimalVal);
      playerDefenseUI.airDefBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusResistanceAirPerLevel * level).ToString(decimalVal);
      playerDefenseUI.earthDefBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusResistanceEarthPerLevel * level).ToString(decimalVal);

      classStatIndex++;
   }

   private float resistantModifier (float defenseValue, float negatingValue, Element element) {
      float resistantModifier = 0;
      if (negatingValue < 0) {
         // If unit is weak to the element
         resistantModifier = ((100f + defenseValue) / 100);
      } else {
         // If unit is resistant to the element
         resistantModifier = (100f / (100f + defenseValue));
      }

      return resistantModifier;
   }

   #endregion

   #region Private Variables

   #endregion
}
