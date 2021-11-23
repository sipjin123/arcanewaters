using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DigitalRuby.LightningBolt;

public class PrefabsManager : MonoBehaviour {
   #region Public Variables

   [Header("Entity Prefabs")]
   // The prefab we use for creating player ships
   public GameObject playerShipPrefab;

   // The prefab we use for creating bot ships
   public BotShipEntity botShipPrefab;

   // The prefab we use for creating sea monster
   public SeaMonsterEntity seaMonsterPrefab;

   // The prefab we use for creating player bodies
   public GameObject playerBodyPrefab;

   // The prefab for spawning an npc
   public NPC npcPrefab;

   // The prefab for spawning secret entrace objects
   public SecretEntranceHolder secretEntrancePrefab;

   [Header("Harvesting Prefabs")]
   // The Prefab we use for creating ore drop effect for mining
   public GameObject oreDropPrefab;

   // The Prefab we use for creating collectable ore
   public GameObject orePickupPrefab;

   // The Prefab we use for creating collectable crops
   public GameObject cropPickupPrefab;

   // The Prefab we use for creating projectile crops that will spawn pickup at the end
   public GameObject cropProjectilePrefab;

   [Header("Sea Combat Projectiles")]
   // The Prefab we use for creating dynamic projectiles
   public GenericSeaProjectile seaEntityProjectile;

   // The Prefab we use for creating venom sticky residue
   public EffectResidue venomStickyPrefab;

   // The Prefab we use for creating lightning residue
   public GameObject lightningResiduePrefab;

   // The Prefab we use for creating lightning chain
   public LightningBoltScript lightningChainPrefab;

   // The Prefab we use for creating tentacle collision effects
   public GameObject tentacleCollisionPrefab;

   // The Prefab we use for creating venom residue
   public VenomResidue venomResiduePrefab;

   // The Prefab we use for creating venom residue for special boss attacks
   public VenomResidue bossVenomResiduePrefab;

   // The Prefab we use for treasureChest spawning
   public GameObject treasureChestPrefab;

   // The prefab we use to spawn sea projectiles
   public SeaProjectile seaProjectilePrefab;

   // The prefab we use to spawn projectile targeting indicators
   public ProjectileTargetingIndicator projectileTargetingIndicatorPrefab;

   [Header("Ship Explosion Effects")]
   // The prefab we use for the player ship sinking effect
   public GameObject playerShipExplosionEffect;

   // The prefab we use for the pirate ship sinking effect
   public GameObject pirateShipExplosionEffect;

   [Header("Network Dependent Prefabs")]
   // The Prefab we use for creating networked cannon balls
   public GameObject networkedCannonBallPrefab;

   // The Prefab we use for creating network projectiles
   public GameObject networkProjectilePrefab;

   // The prefab we use for creating server-controlled cannonballs
   public ServerCannonBall serverCannonBallPrefab;

   // The prefab we use for sea mines
   public SeaMine seaMinePrefab;

   [Header("Text Prefabs")]
   // The Prefab we use for creating Damage text
   public ShipDamageText shipDamageTextPrefab;

   // The Prefab we use for creating Heal text
   public ShipDamageText shipBuffTextPrefab;

   // The Prefab we use for creating Ice Damage text
   public ShipDamageText shipDamageTextIcePrefab;

   // The Prefab we use for creating Air Damage text
   public ShipDamageText shipDamageTextAirPrefab;

   // The Prefab we use for creating generic Effects
   public GameObject genericEffectPrefab;

   // The prefab we use for creating 3d sounds
   public AudioSource sound3dPrefab;

   // The prefab we use for creating 3d sounds FMOD version
   public FMODUnity.StudioEventEmitter fMod3dPrefab;

   // The prefab we use for creating name text that follows the players around
   public TargetFollow nameTextPrefab;

   // A prefab we can use to create floating damage numbers from
   public GameObject damageTextPrefab;

   // The prefab we use to create battle text
   public GameObject battleTextPrefab;

   // The prefab we use to create text to notify players
   public GameObject warningTextPrefab;

   // The prefab we use for showing XP gains
   public GameObject xpGainPrefab;

   // The prefab we use for showing item received
   public GameObject itemReceivedPrefab;

   // The prefab we use for showing level gains
   public GameObject levelGainPrefab;

   // A prefab we can use for showing that the requirements are not enough
   public GameObject insufficientPrefab;

   [Header("Generic Prefabs")]
   // A prefab we use for creating a figure eight route
   public Route figureEightRoutePrefab;

   // A prefab we use for creating waypoints
   public Waypoint waypointPrefab;

   // The prefab we use for adding click triggers to sea entities
   public ClickTrigger clickTriggerPrefab;

   // A prefab we can use for showing minor generic messages, ex. that an object is too far away
   public FloatingCanvas floatingCanvasPrefab;

   // A prefab we can use for showing a load icon above objects
   public FloatingLoadIcon floatingLoadIconPrefab;

   // The prefab we use to create a cancel icon
   public GameObject cancelIconPrefab;

   // The prefab we use for creating Enemies
   public Enemy enemyPrefab;

   // The prefab we use for creating treasure sites
   public TreasureSite treasureSitePrefab;

   // The prefab we use for creating treasure sites in league instances
   public TreasureSite treasureSiteLeaguePrefab;

   // The prefab we use for creating shipyards in pvp maps
   public PvpShipyard pvpShipyardPrefab;

   // The prefab we use for creating towers in pvp maps
   public PvpTower pvpTowerPrefab;

   // The prefab we use for creating monster spawner in pvp maps
   public PvpMonsterSpawner pvpMonsterSpawnerPrefab;

   // The prefab used for spawning pvp loots
   public PvpLootSpawn pvpLootSpawnerPrefab;

   // The prefab we use for creating bases in pvp maps
   public PvpBase pvpBasePrefab;

   // The prefab we use for creating Pvp Capture Target Holders in pvp maps
   public PvpCaptureTargetHolder pvpCaptureTargetHolderPrefab;

   [Header("Texture prefabs")]
   // Prefab used to create new palette texture for material
   public Texture2D textureSquare256;

   [Header("Effects prefabs")]
   // The prefab we use to show effects after petting animal
   public GameObject pettingAnimatorPrefab;

   // A reference to the crit effect prefab
   public GameObject critPrefab;

   // A reference to the block effect prefab
   public GameObject blockPrefab;

   // A reference to the ship crit effect prefab
   public GameObject shipCritPrefab;

   // A reference to the poof effect prefab
   public GameObject poofPrefab;

   // A reference to the explosive shot projectile prefab
   public SeaProjectile explosiveShotProjectilePrefab;

   // A reference to the powerup pickup effect prefab
   public GameObject powerupPickupEffect;

   // The prefab we use for creating hat dyes
   public StoreHatDyeBox hatDyeBoxPrefab;

   // The prefab we use for creating hair dyes
   public StoreHairDyeBox hairDyeBoxPrefab;

   // The prefab we use for creating armor dyes
   public StoreArmorDyeBox armorDyeBoxPrefab;

   // The prefab we use for creating weapon dyes
   public StoreWeaponDyeBox weaponDyeBoxPrefab;

   // The prefab we use for creating ability orbs
   public BuffOrb buffOrbPrefab;

   // The prefab we use for creating whirlpool effects
   public WhirlpoolEffect whirlpoolEffectPrefab;

   // The prefab we use for creating knockback effects
   public KnockbackEffect knockbackEffectPrefab;

   // The prefab we use for some components that help to make up the knockback effect
   public GameObject knockbackEffectProjectilePrefab;

   // Self
   public static PrefabsManager self;

   [Header("Gem Store prefabs")]
   // The prefab we use for creating haircuts
   public StoreHaircutBox haircutBoxPrefab;

   // The prefab we use for creating ship skins
   public StoreShipSkinBox shipBoxPrefab;

   // The prefab we use for creating gem boxes
   public StoreGemBox gemBoxPrefab;

   // The prefab we use for consumables
   public StoreConsumableBox consumableBoxPrefab;

   [Header("Silver")]
   // The reference to the Silver Burst Effect
   public GameObject silverBurstEffectPrefab;

   #endregion

   protected void Awake () {
      self = this;
   }

   public ShipDamageText getTextPrefab (Attack.Type attackType, bool useNegative = false) {
      if (useNegative) {
         return shipDamageTextPrefab;
      }

      switch (attackType) {
         case Attack.Type.Ice:
            return shipDamageTextIcePrefab;
         case Attack.Type.Air:
            return shipDamageTextAirPrefab;
         case Attack.Type.Heal:
            return shipBuffTextPrefab;
         case Attack.Type.SpeedBoost:
            return shipBuffTextPrefab;
         default:
            return shipDamageTextPrefab;
      }
   }

   public GameObject requestCannonSmokePrefab (Attack.ImpactMagnitude impactType) {
      GameObject newPrefab = Instantiate(cannonSmokePrefab);
      newPrefab.GetComponent<MagnitudeHandler>().setSprite(impactType);
      return newPrefab;
   }

   public GameObject requestCannonExplosionPrefab (Attack.ImpactMagnitude impactType) {
      GameObject newPrefab = Instantiate(explosionPrefab);
      newPrefab.GetComponent<MagnitudeHandler>().setSprite(impactType);
      return newPrefab;
   }

   public GameObject requestCannonSplashPrefab (Attack.ImpactMagnitude impactType) {
      GameObject newPrefab = Instantiate(cannonSplashPrefab);
      newPrefab.GetComponent<MagnitudeHandler>().setSprite(impactType);
      return newPrefab;
   }

   #region Private Variables

   [Header("VFX Prefabs")]
   // The Prefab we use for creating cannon splashes
   [SerializeField] private GameObject cannonSplashPrefab;

   // The Prefab we use for creating explosions
   [SerializeField] private GameObject explosionPrefab;

   // The Prefab we use for creating cannon smoke
   [SerializeField] private GameObject cannonSmokePrefab;

   #endregion
}
