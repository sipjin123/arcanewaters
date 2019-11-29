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
      Pref_EnemyLvl = "EnemyLevel";

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

   // Damage stat display
   public StatBreakdownTemplate physicalDmgBreakdown, fireDmgBreakdown, earthDmgBreakdown, airDmgBreakdown, waterDmgBreakdown;
   [HideInInspector] public float physicalDamageMultiplier, fireDamageMultiplier, earthDamageMultiplier, airDamageMultiplier, waterDamageMultiplier;
   [HideInInspector] public float physicalDamageMultiplierBase, fireDamageMultiplierBase, earthDamageMultiplierBase, airDamageMultiplierBase, waterDamageMultiplierBase;
   [HideInInspector] public float physicalDamageMultiplierPerLvl, fireDamageMultiplierPerLvl, earthDamageMultiplierPerLvl, airDamageMultiplierPerLvl, waterDamageMultiplierPerLvl;
   public float outputDamagePhys, outputDamageFire, outputDamageEarth, outputDamageAir, outputDamageWater;

   // Defense stat display
   public StatBreakdownTemplate physicalDefBreakdown, fireDefBreakdown, earthDefBreakdown, airDefBreakdown, waterDefBreakdown;
   [HideInInspector] public float physicalDefenseMultiplier, fireDefenseMultiplier, earthDefenseMultiplier, airDefenseMultiplier, waterDefenseMultiplier;
   [HideInInspector] public float physicalDefenseMultiplierBase, fireDefenseMultiplierBase, earthDefenseMultiplierBase, airDefenseMultiplierBase, waterDefenseMultiplierBase;
   [HideInInspector] public float physicalDefenseMultiplierPerLvl, fireDefenseMultiplierPerLvl, earthDefenseMultiplierPerLvl, airDefenseMultiplierPerLvl, waterDefenseMultiplierPerLvl;
   public float outputDefensePhys, outputDefenseFire, outputDefenseEarth, outputDefenseAir, outputDefenseWater;

   // Computed damage results
   public Text outputPhysText, outputFireText, outputEarthText, outputAirText, outputWaterText;
   public float outputPhys, outputFire, outputEarth, outputAir, outputWater;

   // Base stats
   public float baseDamage, baseDefense;
   public float baseDmgLvl, baseDefLvl;

   // Computes the combat damage
   public Button computeButton;

   // Holds list for togglers
   public List<TogglerClass> togglerList;

   // Base damage and defense indicators
   public Text baseSkillDmgText, baseEnemyDefText;

   // Level's for enemy and players
   public InputField playerLevel, enemyLevel;

   // Stat index for class iteration
   public int classStatIndex = 0;

   // Determines the generic decimal value used by output texts
   public const string decimalVal = "f2";

   #endregion

   private void Awake () {
      computeButton.onClick.AddListener(() => {
         processDamageSimulation();
         processEnemyDefense();

         float phyComputation = resistantModifier(outputDefensePhys, physicalDefenseMultiplierBase);
         float fireComputation = resistantModifier(outputDefenseFire, fireDefenseMultiplierBase);
         float waterComputation = resistantModifier(outputDefenseWater, waterDefenseMultiplierBase);
         float airComputation = resistantModifier(outputDefenseAir, airDefenseMultiplierBase);
         float earthComputation = resistantModifier(outputDefenseEarth, earthDefenseMultiplierBase);

         outputPhysText.text = (outputDamagePhys * phyComputation).ToString(decimalVal);
         outputFireText.text = (outputDamageFire * fireComputation).ToString(decimalVal);
         outputEarthText.text = (outputDamageEarth * earthComputation).ToString(decimalVal);
         outputAirText.text = (outputDamageAir * airComputation).ToString(decimalVal);
         outputWaterText.text = (outputDamageWater * waterComputation).ToString(decimalVal);
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

      // Setup cached info
      classText.text = PlayerPrefs.GetString(Pref_Class, classText.text);
      factionText.text = PlayerPrefs.GetString(Pref_Faction, factionText.text);
      specialtyText.text = PlayerPrefs.GetString(Pref_Specialty, specialtyText.text);
      abilityText.text = PlayerPrefs.GetString(Pref_SkillName, abilityText.text);
      monsterText.text = PlayerPrefs.GetString(Pref_MonsterName, monsterText.text);
      playerLevel.text = PlayerPrefs.GetString(Pref_PlayerLvl, playerLevel.text);
      enemyLevel.text = PlayerPrefs.GetString(Pref_EnemyLvl, enemyLevel.text);

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private void processEnemyDefense () {
      BattlerData fetchedMonsterData = MonsterManager.self.getAllMonsterData().Find(_ => _.enemyType.ToString() == monsterText.text);
      int level = int.Parse(enemyLevel.text);
      float baseDef = fetchedMonsterData.baseDefense + (fetchedMonsterData.defensePerLevel * level);
      baseEnemyDefText.text = baseDef.ToString();

      // Default value setup
      float defaultValue = 1;
      physicalDefenseMultiplier = defaultValue;
      fireDefenseMultiplier = defaultValue;
      earthDefenseMultiplier = defaultValue;
      airDefenseMultiplier = defaultValue;
      waterDefenseMultiplier = defaultValue;

      // Setup base defense
      physicalDefenseMultiplierBase = fetchedMonsterData.physicalDefenseMultiplier;
      fireDefenseMultiplierBase = fetchedMonsterData.fireDefenseMultiplier;
      earthDefenseMultiplierBase = fetchedMonsterData.earthDefenseMultiplier;
      airDefenseMultiplierBase = fetchedMonsterData.airDefenseMultiplier;
      waterDefenseMultiplierBase = fetchedMonsterData.waterDefenseMultiplier;

      // Setup initial defense per level
      physicalDefenseMultiplierPerLvl = fetchedMonsterData.physicalDefenseMultiplierPerLevel;
      fireDefenseMultiplierPerLvl = fetchedMonsterData.fireDefenseMultiplierPerLevel;
      earthDefenseMultiplierPerLvl = fetchedMonsterData.earthDefenseMultiplierPerLevel;
      airDefenseMultiplierPerLvl = fetchedMonsterData.airDefenseMultiplierPerLevel;
      waterDefenseMultiplierPerLvl = fetchedMonsterData.waterDefenseMultiplierPerLevel;

      // Compute total defense considering (base defense and defense per level)
      physicalDefenseMultiplier = (level * physicalDefenseMultiplierPerLvl) + Mathf.Abs(physicalDefenseMultiplierBase);
      fireDefenseMultiplier = (level * fireDefenseMultiplierPerLvl) + Mathf.Abs(fireDefenseMultiplierBase);
      earthDefenseMultiplier = (level * earthDefenseMultiplierPerLvl) + Mathf.Abs(earthDefenseMultiplierBase);
      airDefenseMultiplier = (level * airDefenseMultiplierPerLvl) + Mathf.Abs(airDefenseMultiplierBase);
      waterDefenseMultiplier = (level * waterDefenseMultiplierPerLvl) + Mathf.Abs(waterDefenseMultiplierBase);

      // Final output of each elemental defense
      outputDefensePhys = physicalDefenseMultiplier * baseDef;
      outputDefenseFire = fireDefenseMultiplier * baseDef;
      outputDefenseEarth = earthDefenseMultiplier * baseDef;
      outputDefenseAir = airDefenseMultiplier * baseDef;
      outputDefenseWater = waterDefenseMultiplier * baseDef;

      int lastIndex = 3;

      // Total base defense 
      physicalDefBreakdown.damageText[lastIndex].text = physicalDefenseMultiplierBase.ToString(decimalVal);
      fireDefBreakdown.damageText[lastIndex].text = fireDefenseMultiplierBase.ToString(decimalVal);
      earthDefBreakdown.damageText[lastIndex].text = earthDefenseMultiplierBase.ToString(decimalVal);
      airDefBreakdown.damageText[lastIndex].text = airDefenseMultiplierBase.ToString(decimalVal);
      waterDefBreakdown.damageText[lastIndex].text = waterDefenseMultiplierBase.ToString(decimalVal);

      string additiveString = level+ " * ";

      // Total per level defense 
      physicalDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + physicalDefenseMultiplierPerLvl.ToString(decimalVal);
      fireDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + fireDefenseMultiplierPerLvl.ToString(decimalVal);
      earthDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + earthDefenseMultiplierPerLvl.ToString(decimalVal);
      airDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + airDefenseMultiplierPerLvl.ToString(decimalVal);
      waterDefBreakdown.damagePerLevelText[lastIndex].text = additiveString + waterDefenseMultiplierPerLvl.ToString(decimalVal);

      // Final output computation for all defense
      physicalDefBreakdown.totalDamage.text = outputDefensePhys.ToString(decimalVal);
      fireDefBreakdown.totalDamage.text = outputDefenseFire.ToString(decimalVal);
      earthDefBreakdown.totalDamage.text = outputDefenseEarth.ToString(decimalVal);
      airDefBreakdown.totalDamage.text = outputDefenseAir.ToString(decimalVal);
      waterDefBreakdown.totalDamage.text = outputDefenseWater.ToString(decimalVal);

      // Final computation breakdown for all defense
      additiveString = baseDef + " * ";
      physicalDefBreakdown.totalMultiplier.text = additiveString + physicalDefenseMultiplier.ToString(decimalVal);
      fireDefBreakdown.totalMultiplier.text = additiveString + fireDefenseMultiplier.ToString(decimalVal);
      earthDefBreakdown.totalMultiplier.text = additiveString + earthDefenseMultiplier.ToString(decimalVal);
      airDefBreakdown.totalMultiplier.text = additiveString + airDefenseMultiplier.ToString(decimalVal);
      waterDefBreakdown.totalMultiplier.text = additiveString + waterDefenseMultiplier.ToString(decimalVal);
   }

   private void processDamageSimulation () {
      AttackAbilityData fetchedAbility = AbilityManager.self.allGameAbilities.Find(_ => _.itemName == abilityText.text) as AttackAbilityData;
      BattlerData battleData = MonsterManager.self.getMonster(Enemy.Type.Humanoid, 0);

      baseDamage = battleData.baseDamage;
      baseDmgLvl = battleData.damagePerLevel;

      setupDefaultValues();

      processAdditiveComputations();

      int level = int.Parse(playerLevel.text);
      float baseDmg = baseDamage + (baseDmgLvl * level);
      baseSkillDmgText.text = baseDmg.ToString();

      int lastIndex = 3;
      physicalDmgBreakdown.damageText[lastIndex].text = physicalDamageMultiplierBase.ToString(decimalVal);
      fireDmgBreakdown.damageText[lastIndex].text = fireDamageMultiplierBase.ToString(decimalVal);
      waterDmgBreakdown.damageText[lastIndex].text = waterDamageMultiplierBase.ToString(decimalVal);
      airDmgBreakdown.damageText[lastIndex].text = airDamageMultiplierBase.ToString(decimalVal);
      earthDmgBreakdown.damageText[lastIndex].text = earthDamageMultiplierBase.ToString(decimalVal);

      physicalDmgBreakdown.damagePerLevelText[lastIndex].text = physicalDamageMultiplierPerLvl.ToString(decimalVal);
      fireDmgBreakdown.damagePerLevelText[lastIndex].text = fireDamageMultiplierPerLvl.ToString(decimalVal);
      waterDmgBreakdown.damagePerLevelText[lastIndex].text = waterDamageMultiplierPerLvl.ToString(decimalVal);
      airDmgBreakdown.damagePerLevelText[lastIndex].text = airDamageMultiplierPerLvl.ToString(decimalVal);
      earthDmgBreakdown.damagePerLevelText[lastIndex].text = earthDamageMultiplierPerLvl.ToString(decimalVal);

      outputDamageAir = baseDmg * airDamageMultiplier;
      outputDamageFire = baseDmg * fireDamageMultiplier;
      outputDamageEarth = baseDmg * earthDamageMultiplier;
      outputDamageWater = baseDmg * waterDamageMultiplier;
      outputDamagePhys = baseDmg * physicalDamageMultiplier;

      physicalDmgBreakdown.totalDamage.text = outputDamagePhys.ToString(decimalVal);
      fireDmgBreakdown.totalDamage.text = outputDamageFire.ToString(decimalVal);
      waterDmgBreakdown.totalDamage.text = outputDamageWater.ToString(decimalVal);
      airDmgBreakdown.totalDamage.text = outputDamageAir.ToString(decimalVal);
      earthDmgBreakdown.totalDamage.text = outputDamageEarth.ToString(decimalVal);

      string additiveString = ""; 
      string computedString = "";
      computedString = (physicalDamageMultiplierBase + physicalDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = baseDmg + " * " + computedString;
      physicalDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (fireDamageMultiplierBase + fireDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = baseDmg + " * " + computedString;
      fireDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (waterDamageMultiplierBase + waterDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = baseDmg + " * " + computedString;
      waterDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (airDamageMultiplierBase + airDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = baseDmg + " * " + computedString;
      airDmgBreakdown.totalMultiplier.text = additiveString;

      computedString = (earthDamageMultiplierBase + earthDamageMultiplierPerLvl).ToString(decimalVal);
      additiveString = baseDmg + " * " + computedString;
      earthDmgBreakdown.totalMultiplier.text = additiveString;

      setupPrefs();
   }

   private void setupDefaultValues () {
      float defaultVal = 0;

      physicalDamageMultiplier = defaultVal;
      fireDamageMultiplier = defaultVal;
      waterDamageMultiplier = defaultVal;
      earthDamageMultiplier = defaultVal;
      airDamageMultiplier = defaultVal;

      physicalDamageMultiplierBase = defaultVal;
      fireDamageMultiplierBase = defaultVal;
      earthDamageMultiplierBase = defaultVal;
      airDamageMultiplierBase = defaultVal;
      waterDamageMultiplierBase = defaultVal;

      physicalDamageMultiplierPerLvl = defaultVal;
      fireDamageMultiplierPerLvl = defaultVal;
      earthDamageMultiplierPerLvl = defaultVal;
      airDamageMultiplierPerLvl = defaultVal;
      waterDamageMultiplierPerLvl = defaultVal;
   }

   private void processAdditiveComputations () {
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

   private void setupPrefs () {
      PlayerPrefs.SetString(Pref_Class, classText.text);
      PlayerPrefs.SetString(Pref_Faction, factionText.text);
      PlayerPrefs.SetString(Pref_Specialty, specialtyText.text);
      PlayerPrefs.SetString(Pref_SkillName, abilityText.text);
      PlayerPrefs.SetString(Pref_MonsterName, monsterText.text);

      PlayerPrefs.SetString(Pref_PlayerLvl, playerLevel.text);
      PlayerPrefs.SetString(Pref_EnemyLvl, enemyLevel.text);
   }

   private void addDefaultStats (UserDefaultStats stat) {
      baseDamage += stat.bonusATK;
      baseDmgLvl += stat.bonusATKPerLevel;
   }

   private void addCombatStats (UserCombatStats stat) {
      int level = int.Parse(playerLevel.text);

      // Setup base stats
      physicalDamageMultiplierBase += stat.bonusDamagePhys;
      fireDamageMultiplierBase += stat.bonusDamageFire;
      waterDamageMultiplierBase += stat.bonusDamageWater;
      earthDamageMultiplierBase += stat.bonusDamageEarth;
      airDamageMultiplierBase += stat.bonusDamageAir;

      // Setup per level stats
      physicalDamageMultiplierPerLvl += stat.bonusDamagePhysPerLevel * level;
      fireDamageMultiplierPerLvl += stat.bonusDamageFirePerLevel * level;
      waterDamageMultiplierPerLvl += stat.bonusDamageWaterPerLevel * level;
      earthDamageMultiplierPerLvl += stat.bonusDamageEarthPerLevel * level;
      airDamageMultiplierPerLvl += stat.bonusDamageAirPerLevel * level;

      // Setup Output computation adding the base stats
      physicalDamageMultiplier += stat.bonusDamagePhys;
      fireDamageMultiplier += stat.bonusDamageFire;
      waterDamageMultiplier += stat.bonusDamageWater;
      earthDamageMultiplier += stat.bonusDamageEarth;
      airDamageMultiplier += stat.bonusDamageAir;

      // Setup Output computation adding the per level stats
      physicalDamageMultiplier += stat.bonusDamagePhysPerLevel * level;
      fireDamageMultiplier += stat.bonusDamageFirePerLevel * level;
      waterDamageMultiplier += stat.bonusDamageWaterPerLevel * level;
      earthDamageMultiplier += stat.bonusDamageEarthPerLevel * level;
      airDamageMultiplier += stat.bonusDamageAirPerLevel * level;

      // Registers the total multipliers for base stats
      physicalDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamagePhys.ToString(decimalVal);
      fireDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageFire.ToString(decimalVal);
      waterDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageWater.ToString(decimalVal);
      airDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageAir.ToString(decimalVal);
      earthDmgBreakdown.damageText[classStatIndex].text = stat.bonusDamageEarth.ToString(decimalVal);

      string additiveText = level + " * ";

      // Registers the total multipliers for per level stats
      physicalDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamagePhysPerLevel * level).ToString(decimalVal);
      fireDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageFirePerLevel * level).ToString(decimalVal);
      waterDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageWaterPerLevel * level).ToString(decimalVal);
      airDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageAirPerLevel * level).ToString(decimalVal);
      earthDmgBreakdown.damagePerLevelText[classStatIndex].text = additiveText + (stat.bonusDamageEarthPerLevel * level).ToString(decimalVal);

      classStatIndex++;
   }

   private float resistantModifier (float defenseValue, float negatingValue) {
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

   #region Private Variables

   #endregion
}
