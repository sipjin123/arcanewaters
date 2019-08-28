using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PrefabsManager : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating player ships
   public GameObject playerShipPrefab;

   // The prefab we use for creating bot ships
   public BotShipEntity botShipPrefab;

   // The prefab we use for creating tentacle monsters
   public TentacleEntity tentaclePrefab;

   // The prefab we use for creating horror monster
   public HorrorEntity horrorPrefab;

   // The prefab we use for creating worm monster
   public WormEntity wormPrefab;

   // The prefab we use for creating giant monster
   public ReefGiantEntity giantPrefab;

   // The prefab we use for creating fishman monster
   public FishmanEntity fishmanPrefab; 

   // The prefab we use for creating player bodies
   public GameObject playerBodyPrefab;

   // The Prefab we use for creating cannon smoke
   public GameObject cannonSmokePrefab;

   // The Prefab we use for creating venom projectiles
   public VenomProjectile venomPrefab;

   // The Prefab we use for creating shockball projectiles
   public ShockballProjectile shockballPrefab;

   // The Prefab we use for creating boulder projectiles
   public BoulderProjectile boulderPrefab;
   
   // The Prefab we use for creating cannon balls
   public CannonBall cannonBallPrefab;

   // The Prefab we use for creating ice cannon balls
   public CannonBall cannonBallIcePrefab;

   // The Prefab we use for creating air cannon balls
   public CannonBall cannonBallAirPrefab;

   // The Prefab we use for creating cannon splashes
   public GameObject cannonSplashPrefab;

   // The Prefab we use for creating explosions
   public GameObject explosionPrefab;

   // The Prefab we use for creating networked cannon balls
   public GameObject networkedCannonBallPrefab;

   // The Prefab we use for creating Damage text
   public ShipDamageText shipDamageTextPrefab;

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

   // The prefab we use for showing XP gains
   public GameObject xpGainPrefab;

   // The prefab we use for showing level gains
   public GameObject levelGainPrefab;

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

   // A prefab we can use for showing that the requirements are not enough
   public GameObject insufficientPrefab;

   // A prefab we can use to create floating damage numbers from
   public GameObject damageTextPrefab;

   // The prefab we use to create battle text
   public GameObject battleTextPrefab;

   // The prefab we use to create a cancel icon
   public GameObject cancelIconPrefab;

   // The prefab we use for creating Enemies
   public Enemy enemyPrefab;

   // The prefab we use for creating Status effects
   public Status statusPrefab;

   // Self
   public static PrefabsManager self;

   #endregion

   protected void Awake () {
      self = this;
   }

   public CannonBall getCannonBallPrefab (Attack.Type attackType) {
      switch (attackType) {
         case Attack.Type.Ice:
            return cannonBallIcePrefab;
         case Attack.Type.Air:
            return cannonBallAirPrefab;
         default:
            return cannonBallPrefab;
      }
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

   public BoulderProjectile getBoulderPrefab (Attack.Type attackType) {
      switch (attackType) {
         default:
            return boulderPrefab;
      }
   }

   public ShipDamageText getTextPrefab (Attack.Type attackType) {
      switch (attackType) {
         case Attack.Type.Ice:
            return shipDamageTextIcePrefab;
         case Attack.Type.Air:
            return shipDamageTextAirPrefab;
         default:
            return shipDamageTextPrefab;
      }
   }

   #region Private Variables

   #endregion
}
