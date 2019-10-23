using UnityEngine;
using System.Collections.Generic;

// Data that a battler will hold, max health, xp, sounds, etc.
[System.Serializable]

[CreateAssetMenu(fileName = "NewBattlerData", menuName = "Data/Battlers", order = 1)]
public class BattlerData : ScriptableObject {
   #region Public Variables

   #endregion

   public static BattlerData CreateInstance (BattlerData datacopy) {
      BattlerData data = CreateInstance<BattlerData>();

      data.setAllBattlerData(datacopy);

      return data;
   }

   public static BattlerData CreateInstance (int xp, int apWhenDamaged, int baseHealth, int baseDef, int baseDmg, int baseGold, int dmgPerLevel,
      int defPerLevel, int healthPerLevel, List<BasicAbilityData> battlerAbilities, float physicalDefMultiplier, float fireDefMultiplier,
      float earthDefMultiplier, float airDefMultiplier, float waterDefMultiplier, float allDefMultiplier, float physicalAtkMultiplier,
      float fireAtkMultiplier, float earthAtkMultiplier, float airAtkMultiplier, float waterAtkMultiplier, float allAtkMultiplier,
      AudioClip deathSound, AudioClip jumpAtkSound, float preContactLength, float preMagicLength, int baseXPReward, GenericLootData lootData,
      Enemy.Type battlerID, BattlerBehaviour battlerObject) {

      BattlerData data = CreateInstance<BattlerData>();

      data.setAllBattlerData(xp, apWhenDamaged, baseHealth, baseDef, baseDmg, baseGold, dmgPerLevel,
       defPerLevel, healthPerLevel, battlerAbilities, physicalDefMultiplier, fireDefMultiplier,
       earthDefMultiplier, airDefMultiplier, waterDefMultiplier, allDefMultiplier, physicalAtkMultiplier,
       fireAtkMultiplier, earthAtkMultiplier, airAtkMultiplier, waterAtkMultiplier, allAtkMultiplier,
       deathSound, jumpAtkSound, preContactLength, preMagicLength, baseXPReward, lootData, battlerID, battlerObject);

      return data;
   }

   protected void setAllBattlerData (BattlerData datacopy) {

      setBattlerObject(datacopy.getBattlerObject());

      setCurrentXP(datacopy.getCurrentXP());
      setBattlerID((Enemy.Type) datacopy.getBattlerId());

      setApWhenDamaged(datacopy.getApWhenDamaged());

      setBaseHealth(datacopy.getBaseHealth());
      setBaseDefense(datacopy.getBaseDefense());
      setBaseDamage(datacopy.getBaseDamage());
      setBaseGoldReward(datacopy.getBaseGoldReward());
      setBaseXPReward(datacopy.getBaseXPReward());

      setDamagePerLevel(datacopy.getDamagePerLevel());
      setDefensePerLevel(datacopy.getDefensePerLevel());
      setHealthPerLevel(datacopy.getHealthPerLevel());

      setAbilities(datacopy.getAbilities());
      setLootData(datacopy.getLootData());

      setPhysicalDefMultiplier(datacopy.getPhysicalDefMultiplier());
      setFireDefMultiplier(datacopy.getFireDefMultiplier());
      setEarthDefMultiplier(datacopy.getEarthDefMultiplier());
      setAirDefMultiplier(datacopy.getAirDefMultiplier());
      setWaterDefMultiplier(datacopy.getWaterDefMultiplier());
      setAllDefMultiplier(datacopy.getAllDefMultiplier());

      setPhysicalAtkMultiplier(datacopy.getPhysicalAtkMultiplier());
      setFireAtkMultiplier(datacopy.getFireAtkMultiplier());
      setEarthAtkMultiplier(datacopy.getEarthAtkMultiplier());
      setAirAtkMultiplier(datacopy.getAirAtkMultiplier());
      setWaterAtkMultiplier(datacopy.getWaterAtkMultiplier());
      setAllAtkMultiplier(datacopy.getAllAtkMultiplier());

      setDeathSound(datacopy.getDeathSound());
      setAttackJumpSound(datacopy.getAttackJumpSound());

      setPreContactLength(datacopy.getPreContactLength());
      setPreMagicLength(datacopy.getPreMagicLength());
   }

   protected void setAllBattlerData (int xp, int apWhenDamaged, int baseHealth, int baseDef, int baseDmg, int baseGold, int dmgPerLevel,
      int defPerLevel, int healthPerLevel, List<BasicAbilityData> battlerAbilities, float physicalDefMultiplier, float fireDefMultiplier,
      float earthDefMultiplier, float airDefMultiplier, float waterDefMultiplier, float allDefMultiplier, float physicalAtkMultiplier,
      float fireAtkMultiplier, float earthAtkMultiplier, float airAtkMultiplier, float waterAtkMultiplier, float allAtkMultiplier,
      AudioClip deathSound, AudioClip jumpAtkSound, float preContactLength, float preMagicLength, int baseXPReward, GenericLootData lootData,
      Enemy.Type battlerID, BattlerBehaviour battlerObject) {

      setBattlerObject(battlerObject);

      setCurrentXP(xp);
      setBattlerID(battlerID);

      setApWhenDamaged(apWhenDamaged);

      setBaseHealth(baseHealth);
      setBaseDefense(baseDef);
      setBaseDamage(baseDmg);
      setBaseGoldReward(baseGold);
      setBaseXPReward(baseXPReward);

      setDamagePerLevel(dmgPerLevel);
      setDefensePerLevel(defPerLevel);
      setHealthPerLevel(healthPerLevel);

      setAbilities(battlerAbilities);
      setLootData(lootData);

      setPhysicalDefMultiplier(physicalDefMultiplier);
      setFireDefMultiplier(fireDefMultiplier);
      setEarthDefMultiplier(earthDefMultiplier);
      setAirDefMultiplier(airDefMultiplier);
      setWaterDefMultiplier(waterDefMultiplier);
      setAllDefMultiplier(allDefMultiplier);

      setPhysicalAtkMultiplier(physicalAtkMultiplier);
      setFireAtkMultiplier(fireAtkMultiplier);
      setEarthAtkMultiplier(earthAtkMultiplier);
      setAirAtkMultiplier(airAtkMultiplier);
      setWaterAtkMultiplier(waterAtkMultiplier);
      setAllAtkMultiplier(allAtkMultiplier);

      setDeathSound(deathSound);
      setAttackJumpSound(jumpAtkSound);

      setPreContactLength(preContactLength);
      setPreMagicLength(preMagicLength);
   }

   #region Setters

   public void setEnemyName (string value) { _enemyName = value; } 
   public void setBattlerObject (BattlerBehaviour value) { _battlerPrefab = value; }

   public void setCurrentXP (int value) { _currentXP = value; }
   public void setBattlerID (Enemy.Type value) { _battlerID = value; }

   public void setApWhenDamaged (int value) { _apGainWhenDamaged = value; }

   public void setBaseHealth (int value) { _baseHealth = value; }
   public void setBaseDefense (int value) { _baseDefense = value; }
   public void setBaseDamage (int value) { _baseDamage = value; }
   public void setBaseGoldReward (int value) { _baseGoldReward = value; }
   public void setBaseXPReward (int value) { _baseXPReward = value; }

   public void setDamagePerLevel (int value) { _damagePerLevel = value; }
   public void setDefensePerLevel (int value) { _defensePerLevel = value; }
   public void setHealthPerLevel (int value) { _healthPerlevel = value; }

   public void setAbilities (List<BasicAbilityData> value) { _battlerAbilities = value; }
   public void setLootData (GenericLootData value) { _battlerLootData = value; }

   public void setPhysicalDefMultiplier (float value) { _physicalDefenseMultiplier = value; }
   public void setFireDefMultiplier (float value) { _fireDefenseMultiplier = value; }
   public void setEarthDefMultiplier (float value) { _earthDefenseMultiplier = value; }
   public void setAirDefMultiplier (float value) { _airDefenseMultiplier = value; }
   public void setWaterDefMultiplier (float value) { _waterDefenseMultiplier = value; }
   public void setAllDefMultiplier (float value) { _allDefenseMultiplier = value; }

   public void setPhysicalAtkMultiplier (float value) { _physicalAttackMultiplier = value; }
   public void setFireAtkMultiplier (float value) { _fireAttackMultiplier = value; }
   public void setEarthAtkMultiplier (float value) { _earthAttackMultiplier = value; }
   public void setAirAtkMultiplier (float value) { _airAttackMultiplier = value; }
   public void setWaterAtkMultiplier (float value) { _waterAttackMultiplier = value; }
   public void setAllAtkMultiplier (float value) { _allAttackMultiplier = value; }

   public void setDeathSound (AudioClip value) { _deathSound = value; }
   public void setAttackJumpSound (AudioClip value) { _attackJumpSound = value; }

   public void setPreContactLength (float value) { _preContactLength = value; }
   public void setPreMagicLength (float value) { _preMagicLength = value; }

   #endregion

   #region Getters

   public BattlerBehaviour getBattlerObject () { return _battlerPrefab; }
   
   public string getEnemyName () { return _enemyName; }

   public int getCurrentXP () { return _currentXP; }
   public int getBattlerId () { return (int) _battlerID; }

   public int getApWhenDamaged () { return _apGainWhenDamaged; }

   public int getBaseHealth () { return _baseHealth; }
   public int getBaseDefense () { return _baseDefense; }
   public int getBaseDamage () { return _baseDamage; }
   public int getBaseGoldReward () { return _baseGoldReward; }
   public int getBaseXPReward () { return _baseXPReward; }

   public int getDamagePerLevel () { return _damagePerLevel; }
   public int getDefensePerLevel () { return _defensePerLevel; }
   public int getHealthPerLevel () { return _healthPerlevel; }

   public List<BasicAbilityData> getAbilities () { return _battlerAbilities; }
   public GenericLootData getLootData () { return _battlerLootData; }

   public float getPhysicalDefMultiplier () { return _physicalDefenseMultiplier; }
   public float getFireDefMultiplier () { return _fireDefenseMultiplier; }
   public float getEarthDefMultiplier () { return _earthDefenseMultiplier; }
   public float getAirDefMultiplier () { return _airDefenseMultiplier; }
   public float getWaterDefMultiplier () { return _waterDefenseMultiplier; }
   public float getAllDefMultiplier () { return _allDefenseMultiplier; }

   public float getPhysicalAtkMultiplier () { return _physicalAttackMultiplier; }
   public float getFireAtkMultiplier () { return _fireAttackMultiplier; }
   public float getEarthAtkMultiplier () { return _earthAttackMultiplier; }
   public float getAirAtkMultiplier () { return _airAttackMultiplier; }
   public float getWaterAtkMultiplier () { return _waterAttackMultiplier; }
   public float getAllAtkMultiplier () { return _allAttackMultiplier; }

   public AudioClip getDeathSound () { return _deathSound; }
   public AudioClip getAttackJumpSound () { return _attackJumpSound; }

   public float getPreContactLength () { return _preContactLength; }
   public float getPreMagicLength () { return _preMagicLength; }

   #endregion

   #region Private Variables

   [SerializeField] private string _enemyName;

   [SerializeField] private BattlerBehaviour _battlerPrefab;

   // Used for calculating the current level of this battler.
   [SerializeField] private int _currentXP;
   [SerializeField] private Enemy.Type _battlerID;

   // Ability Points gained when damaged
   [SerializeField] private int _apGainWhenDamaged;

   // Base battler parameters
   [SerializeField] private int _baseHealth;
   [SerializeField] private int _baseDefense;
   [SerializeField] private int _baseDamage;
   [SerializeField] private int _baseGoldReward;
   [SerializeField] private int _baseXPReward;

   // Increments in stats per level.
   [SerializeField] private int _damagePerLevel;
   [SerializeField] private int _defensePerLevel;
   [SerializeField] private int _healthPerlevel;

   // Attacks and abilities that the battler have
   [SerializeField] private List<BasicAbilityData> _battlerAbilities;
   [SerializeField] private GenericLootData _battlerLootData;

   // Element defense multiplier values
   [SerializeField] private float _physicalDefenseMultiplier;
   [SerializeField] private float _fireDefenseMultiplier;
   [SerializeField] private float _earthDefenseMultiplier;
   [SerializeField] private float _airDefenseMultiplier;
   [SerializeField] private float _waterDefenseMultiplier;
   [SerializeField] private float _allDefenseMultiplier;

   // Element attack multiplier values
   [SerializeField] private float _physicalAttackMultiplier;
   [SerializeField] private float _fireAttackMultiplier;
   [SerializeField] private float _earthAttackMultiplier;
   [SerializeField] private float _airAttackMultiplier;
   [SerializeField] private float _waterAttackMultiplier;
   [SerializeField] private float _allAttackMultiplier;

   // Sounds
   [SerializeField] private AudioClip _deathSound;
   [SerializeField] private AudioClip _attackJumpSound;

   // The amount of time our attack takes depends the type of Battler
   [SerializeField] private float _preContactLength;

   // The amount of time before the ground effect appears depends on the type of Battler
   [SerializeField] private float _preMagicLength;

   #endregion
}
