using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;

public class MonsterRawData {

   #region Public Variables

   // Holds the path for the icon
   public string imagePath;

   // Used for calculating the current level of this battler.
   public int currentXP;
   [XmlElement(Namespace = "EnemyType")]
   public Enemy.Type battlerID;

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
   //public List<BasicAbilityData> battlerAbilities;
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
   public string deathSound;
   public string attackJumpSound;

   // The amount of time our attack takes depends the type of Battler
   public float preContactLength;

   // The amount of time before the ground effect appears depends on the type of Battler
   public float preMagicLength;

   #endregion
}
