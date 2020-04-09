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

   // The prefab for spawning secret objects
   public SecretsNode secretsPrefab;

   [Header("Harvesting Prefabs")]
   // The Prefab we use for creating ore drop effect for mining
   public GameObject oreDropPrefab;

   // The Prefab we use for creating collectable ore
   public GameObject orePickupPrefab;

   // The Prefab we use for creating crop bounce effect for harvesting
   public GameObject cropBouncePrefab;

   // The Prefab we use for creating collectable crops
   public GameObject cropPickupPrefab;

   [Header("Sea Combat Projectiles")]
   // The Prefab we use for creating venom projectiles
   public VenomProjectile venomPrefab;

   // The Prefab we use for creating venom sticky residue
   public GameObject venomStickyPrefab;

   // The Prefab we use for creating lightning residue
   public GameObject lightningResiduePrefab;

   // The Prefab we use for creating lightning chain
   public LightningBoltScript lightningChainPrefab;

   // The Prefab we use for creating shockball projectiles
   public ShockballProjectile shockballPrefab;

   // The Prefab we use for creating boulder projectiles
   public GameObject boulderPrefab;

   // The Prefab we use for creating mini boulder projectiles
   public BoulderProjectile miniBoulderPrefab;

   // The Prefab we use for creating cannon balls
   public CannonBall cannonBallPrefab;

   // The Prefab we use for creating ice cannon balls
   public CannonBall cannonBallIcePrefab;

   // The Prefab we use for creating air cannon balls
   public CannonBall cannonBallAirPrefab;

   // The Prefab we use for creating tentacle collision effects
   public GameObject tentacleCollisionPrefab;

   // The Prefab we use for creating venom residue
   public GameObject venomResiduePrefab;

   // The Prefab we use for treasureChest spawning
   public GameObject treasureChestPrefab;

   [Header("Network Dependent Prefabs")]
   // The Prefab we use for creating networked cannon balls
   public GameObject networkedCannonBallPrefab;

   // The Prefab we use for creating networked venom projectiles
   public GameObject networkedVenomProjectilePrefab;

   // The Prefab we use for creating tentacle projectiles
   public TentacleProjectile tentacleProjectilePrefab;

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

   // The prefab we use for creating name text that follows the players around
   public SmoothFollow nameTextPrefab;

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

   // The prefab we use for creating hair dyes
   public StoreHairDyeBox hairDyeBoxPrefab;

   // The prefab we use for creating haircuts
   public StoreHaircutBox haircutBoxPrefab;

   // The prefab we use for creating ship skins
   public StoreShipBox shipBoxPrefab;

   // A prefab we can use for showing that an object is too far away
   public GameObject tooFarPrefab;

   // The prefab we use to create a cancel icon
   public GameObject cancelIconPrefab;

   // The prefab we use for creating Enemies
   public Enemy enemyPrefab;

   // The prefab we use for creating treasure sites
   public TreasureSite treasureSitePrefab;

   // The prefab we use for creating Status effects
   public Status statusPrefab;

   // Self
   public static PrefabsManager self;

   #endregion

   protected void Awake () {
      self = this;
   }

   public CannonBall getCannonBallPrefab (Attack.Type attackType) {
      return cannonBallPrefab;

      // TODO: Temporary disable cannon ball variety
      /*
      switch (attackType) {
         case Attack.Type.Ice:
            return cannonBallIcePrefab;
         case Attack.Type.Air:
            return cannonBallAirPrefab;
         default:
            return cannonBallPrefab;
      }*/
   }

   public VenomProjectile getVenomPrefab (Attack.Type attackType) {
      switch (attackType) {
         default:
            return venomPrefab;
      }
   }

   public ShockballProjectile getShockballPrefab (Attack.Type attackType) {
      switch (attackType) {
         default:
            return shockballPrefab;
      }
   }

   public BoulderProjectile getMiniBoulderPrefab (Attack.Type attackType) {
      switch (attackType) {
         default:
            return miniBoulderPrefab;
      }
   }

   public TentacleProjectile getTentacleProjectilePrefab (Attack.Type attackType) {
      switch (attackType) {
         default:
            return tentacleProjectilePrefab;
      }
   }

   public ShipDamageText getTextPrefab (Attack.Type attackType) {
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
