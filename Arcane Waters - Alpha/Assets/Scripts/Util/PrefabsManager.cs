﻿using UnityEngine;
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
   public GameObject venomStickyPrefab;

   // The Prefab we use for creating lightning residue
   public GameObject lightningResiduePrefab;

   // The Prefab we use for creating lightning chain
   public LightningBoltScript lightningChainPrefab;

   // The Prefab we use for creating tentacle collision effects
   public GameObject tentacleCollisionPrefab;

   // The Prefab we use for creating venom residue
   public VenomResidue venomResiduePrefab;

   // The Prefab we use for treasureChest spawning
   public GameObject treasureChestPrefab;

   [Header("Network Dependent Prefabs")]
   // The Prefab we use for creating networked cannon balls
   public GameObject networkedCannonBallPrefab;

   // The Prefab we use for creating network projectiles
   public GameObject networkProjectilePrefab;

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

   // A prefab we can use for showing minor generic messages, ex. that an object is too far away
   public FloatingCanvas floatingCanvasPrefab;

   // The prefab we use to create a cancel icon
   public GameObject cancelIconPrefab;

   // The prefab we use for creating Enemies
   public Enemy enemyPrefab;

   // The prefab we use for creating treasure sites
   public TreasureSite treasureSitePrefab;

   // The prefab we use for creating Status effects
   public Status statusPrefab;

   [Header("Texture prefabs")]
   // Prefabs used to create new palette texture for material - using different sizes
   public Texture2D texturePrefab128;
   public Texture2D texturePrefab256;
   public Texture2D texturePrefab512;

   // Self
   public static PrefabsManager self;

   #endregion

   protected void Awake () {
      self = this;
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
