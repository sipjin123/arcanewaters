using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using Pathfinding;
using DG.Tweening;

public class BotShipEntity : ShipEntity, IMapEditorDataReceiver
{
   #region Public Variables

   // The data edited in the sea monster tool
   public SeaMonsterEntityData seaEntityData;

   // The guild ids for the bot ship guilds
   public static int PRIVATEERS_GUILD_ID = 1;
   public static int PIRATES_GUILD_ID = 2;

   // A custom max force that we can optionally specify
   public float maxForceOverride = 0f;

   // Determines if this ship is spawned at debug mode
   public bool isDebug = false;

   // A reference to the animator used to show where this ship is aiming
   public Animator targetingIndicatorAnimator;

   // A reference to the transform of the parent of the targeting indicator
   public Transform targetingIndicatorParent;

   // A reference to the transform of the socket of the barrel of the targeting indicator, for spawning cannonballs
   public Transform targetingBarrelSocket;

   // A reference to the renderer used to show where this ship is aiming
   public SpriteRenderer targetingIndicatorRenderer;

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   protected override void Start () {
      base.Start();
	   
	   if (isServer) {
		getRandomPowerup();
	   }
   }

   protected override void Update () {
      base.Update();

      // If we're dead and have finished sinking, remove the ship
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(gameObject);
      }

      // If the tutorial is waiting for a bot ship defeat, test if the conditions are met
      if (isClient && isDead() && TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.DefeatPirateShip) {
         if (Global.player != null && hasBeenAttackedBy(Global.player)) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.DefeatPirateShip);
         }
      }

      updateTargetingIndicator();
   }

   public override void onDeath () {
      if (_hasRunOnDeath) {
         return;
      }

      base.onDeath();

      NetEntity lastAttacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(_lastAttackerNetId);
      if (lastAttacker) {
         spawnChest(lastAttacker.userId);
      } else {
         D.warning("Bot ship couldn't drop a chest, due to not being able to locate last attacker");
      }
   }

   [Server]
   public void spawnChest (int killerUserId) {
      if (seaEntityData.shouldDropTreasure && killerUserId > 0) {
         Instance currentInstance = InstanceManager.self.getInstance(this.instanceId);
         TreasureManager.self.createSeaMonsterChest(currentInstance, sortPoint.transform.position, seaEntityData.seaMonsterType, killerUserId);
      }
   }

   [Server]
   protected override IEnumerator CO_AttackEnemiesInRange (float delayInSecondsWhenNotReloading) {
      while (!isDead()) {

         NetEntity target = getAttackerInRange();

         // Show the charging animation on clients
         if (target) {
            Rpc_NotifyChargeUp(target.netId);
            _aimTarget = target;
         }

         // Wait for the charge-up animation to play
         float chargeTimer = 1.0f;
         while (chargeTimer > 0.0f) {
            if (!_aimTarget || _aimTarget.isDead() || isDead()) {
               break;
            }

            float indicatorAngle = 360.0f - Util.angle(_aimTarget.transform.position - transform.position);
            targetingIndicatorParent.transform.rotation = Quaternion.Euler(0.0f, 0.0f, indicatorAngle);
            chargeTimer -= Time.deltaTime;
            yield return null;
         }
         
         float waitTimeToUse = delayInSecondsWhenNotReloading;

         // Fire a shot at our target, if we charge up fully
         if (chargeTimer <= 0.0f) {
            if (target) {
               fireCannonAtTarget(target);
               triggerPowerupsOnFire();
               Rpc_NotifyCannonFired();

               waitTimeToUse = reloadDelay;
            }
         }

         yield return new WaitForSeconds(waitTimeToUse);
      }
   }

   [ClientRpc]
   private void Rpc_NotifyChargeUp (uint targetNetId) {
      NetEntity target = MyNetworkManager.fetchEntityFromNetId<NetEntity>(targetNetId);
      if (target == null || target.isDead() || isDead()) {
         return;
      }

      _aimTarget = target;

      // Show the charging animation
      showTargetingEffects();
   }

   [ClientRpc]
   private void Rpc_NotifyCannonFired () {
      if (_isShowingTargetingIndicator) {
         hideTargetingEffects();
      }
   }

   private void updateTargetingIndicator () {
      // If targeting effects are showing, and either: we have no target, the target is dead, or we are dead, hide targeting effects
      if (_isShowingTargetingIndicator && (!_aimTarget || _aimTarget.isDead() || isDead())) {
         hideTargetingEffects();
      }

      // If we are showing targeting effects, update the rotation of our effects to point at our target
      if (_isShowingTargetingIndicator) {
         float indicatorAngle = 360.0f - Util.angle(_aimTarget.transform.position - transform.position);
         targetingIndicatorParent.transform.rotation = Quaternion.Euler(0.0f, 0.0f, indicatorAngle);
      }
   }

   private void showTargetingEffects () {
      targetingIndicatorAnimator.gameObject.SetActive(true);
      targetingIndicatorRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
      targetingIndicatorRenderer.DOFade(1.0f, 0.1f);
      targetingIndicatorAnimator.SetTrigger("ChargeAndFire");
      _isShowingTargetingIndicator = true;
   }

   private void hideTargetingEffects () {
      DOTween.Kill(targetingIndicatorAnimator);
      DOTween.Kill(targetingIndicatorRenderer);
      targetingIndicatorAnimator.transform.DOPunchScale(Vector3.up * 0.15f, 0.25f, 1);
      targetingIndicatorRenderer.DOFade(0.0f, 0.25f).OnComplete(() => targetingIndicatorAnimator.gameObject.SetActive(false));
      _isShowingTargetingIndicator = false;
      _aimTarget = null;
   }

   [Server]
   private void fireCannonAtTarget (NetEntity target) {
      Vector2 targetPosition = target.transform.position;
      Vector2 spawnPosition = targetingBarrelSocket.position;
      Vector2 toTarget = targetPosition - spawnPosition;
      float targetDistance = toTarget.magnitude;

      float range = getAttackRange();

      // If the target is out of range, fire a max range shot in their direction
      if (targetDistance > range) {
         targetPosition = spawnPosition + toTarget.normalized * range;
         targetDistance = range;
      }

      ShipAbilityData abilityData = null;
      if (primaryAbilityId > 0) {
         abilityData = ShipAbilityManager.self.getAbility(primaryAbilityId);
      } else {
         abilityData = ShipAbilityManager.self.getAbility(Attack.Type.Cannon);
      }

      // Create the cannon ball object from the prefab
      ServerCannonBall netBall = Instantiate(PrefabsManager.self.serverCannonBallPrefab, spawnPosition, Quaternion.identity);

      // Set up the cannonball
      float distanceModifier = Mathf.Clamp(targetDistance / range, 0.1f, 1.0f);
      float lobHeight = 0.25f * distanceModifier;
      float lifetime = targetDistance / Attack.getSpeedModifier(Attack.Type.Cannon);
      Vector2 velocity = toTarget.normalized * Attack.getSpeedModifier(Attack.Type.Cannon);

      netBall.init(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityData.abilityId, velocity, lobHeight, false, lifetime: lifetime);

      netBall.addEffectors(getCannonballEffectors());

      NetworkServer.Spawn(netBall.gameObject);

      Rpc_NoteAttack();
   }

   public static int fetchDataFieldID (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SHIP_DATA_KEY) == 0) {
            // Get Type from ship data field
            if (int.TryParse(field.v, out int shipId)) {
               return shipId;
            }
         }
      }
      return 0;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SHIP_DATA_KEY) == 0) {
            Area area = GetComponentInParent<Area>();
            areaKey = area.areaKey;

         } else if (field.k.CompareTo(DataField.SHIP_GUILD_ID) == 0) {
            int id = int.Parse(field.v.Split(':')[0]);
            guildId = id;
         }
      }
   }

   public void setShipData (int enemyXmlData, Ship.Type shipType, int instanceDifficulty) {
      ShipData shipData = ShipDataManager.self.getShipData(shipType);
      if (shipData != null && (int) shipType != -1) {
         if (shipData.spritePath != "") {
            spritesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getSprite(shipData.spritePath).texture;
         }
      } else {
         shipData = ShipDataManager.self.shipDataList[0];
         D.debug("Cant get ship data for: {" + shipType + "}");
      }
      SeaMonsterEntityData seaEnemyData = SeaMonsterManager.self.getMonster(enemyXmlData);
      if (seaEnemyData == null) {
         D.debug("Failed to get sea monster data");
      }

      initializeAsSeaEnemy(seaEnemyData, shipData, instanceDifficulty);

      // Assign ripple sprites
      _ripplesStillSprites = ImageManager.getTexture(Ship.getRipplesPath(shipType));
      _ripplesMovingSprites = ImageManager.getTexture(Ship.getRipplesMovingPath(shipType));
      if (shipData.rippleSpritePath != "") {
         ripplesContainer.GetComponent<SpriteSwap>().newTexture = _ripplesStillSprites;
      }
   }

   public override bool isBotShip () { return true; }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.botShipParent, worldPositionStays);
   }

   private void getRandomPowerup () {
      Powerup.Type[] allowedPowerups = { Powerup.Type.BouncingShots, Powerup.Type.ElectricShots, Powerup.Type.MultiShots, Powerup.Type.ExplosiveShots };
      _powerup = allowedPowerups[Random.Range(0, 4)];
   }

   private void triggerPowerupsOnFire () {
      if (_powerup != Powerup.Type.MultiShots) {
         return;
      }

      float activationChance = 0.5f;
      int maxExtraShots = 2;
      int extraShotsCounter = 0;

      List<SeaEntity> nearbyEnemies = Util.getEnemiesInCircle(this, transform.position, getAttackRange());
      foreach (SeaEntity enemy in nearbyEnemies) {
         // If we have reached the limit of extra shots, stop checking
         if (extraShotsCounter >= maxExtraShots) {
            break;
         }

         // Roll for powerup activation chance
         if (Random.Range(0.0f, 1.0f) <= activationChance) {
            fireCannonAtTarget(enemy);
            extraShotsCounter++;
         }
      }
   }

   private List<CannonballEffector> getCannonballEffectors () {
      List<CannonballEffector> effectors = new List<CannonballEffector>();
      
      switch (_powerup) {
         case Powerup.Type.BouncingShots:
            effectors.Add(new CannonballEffector(CannonballEffector.Type.Bouncing, 1.0f, range: 2.0f));
            break;
         case Powerup.Type.ElectricShots:
            effectors.Add(new CannonballEffector(CannonballEffector.Type.Electric, 25.0f, range: 0.75f));
            break;
         case Powerup.Type.ExplosiveShots:
            effectors.Add(new CannonballEffector(CannonballEffector.Type.Explosion, 30.0f, range: 0.6f));
            break;
      }

      return effectors;
   }

   #region Private Variables

   // A reference to the NetEntity we are aiming at
   private NetEntity _aimTarget;

   // Whether we are currently showing targeting effects
   private bool _isShowingTargetingIndicator = false;

   // What powerup this bot ship has
   private Powerup.Type _powerup;

   #endregion
}
