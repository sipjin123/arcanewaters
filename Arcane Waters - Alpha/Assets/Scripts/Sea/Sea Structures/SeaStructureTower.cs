﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class SeaStructureTower : SeaStructure
{
   #region Public Variables

   // The position which projectiles are fired from
   public Transform targetingBarrelSocket;

   // How much faster our projectiles will move, compared to the normal cannonballs
   public float projectileSpeedModifier = 2.0f;

   // A reference to the renderer that is displaying our attack range
   public MeshRenderer attackRangeRenderer;

   // A reference to the animator used to show where this tower is aiming
   public Animator targetingIndicatorAnimator;

   // A reference to the transform of the parent of the targeting indicator
   public Transform targetingIndicatorParent;

   // A reference to the renderer used to show where this tower is aiming
   public SpriteRenderer targetingIndicatorRenderer;

   // References to the colors used for the attack range circle, when the player is being targeted, or safe
   public Color dangerColor, safeColor, warningColor;

   // A child object that will pass on onTriggerEnter2D events, to avoid collision issues with having a trigger attached to the main seaentity object.
   public TriggerDetector attackTriggerDetector;

   // A reference to the transform used to for the aiming reticle
   public Transform aimTransform;

   // A reference to the dotted parabola showing where this ship is aiming
   public DottedParabola targetingParabola;

   // References to the sprites used for targeting
   public Sprite aimingReticle, lockedReticle;

   // Defines what tower style each pvp faction will have
   public static readonly int[] towerStylesByFaction = { 0, 1, 0, 1, 0, 1, 0, 1 };

   #endregion

   protected override void Awake () {
      base.Awake();

      attackTriggerDetector.onTriggerEnter += onAttackTriggerEnter2D;
      attackTriggerDetector.onTriggerExit += onAttackTriggerExit2D;
      attackTriggerDetector.GetComponent<CircleCollider2D>().radius = getAttackRange();

      attackRangeRenderer.material.SetFloat("_Radius", getAttackRange());
      attackRangeRenderer.material.SetFloat("_FillAmount", 1.0f);
      attackRangeRenderer.material.SetColor("_Color", new Color(0.0f, 0.0f, 0.0f, 0.0f));
   }

   protected override void Start () {
      base.Start();

      _reticleRenderer = aimTransform.GetComponent<SpriteRenderer>();
   }

   protected override void onActivated () {
      base.onActivated();
      if (isServer) {
         StartCoroutine(CO_AttackEnemiesInRange(0.25f));
      }

      StartCoroutine(CO_SetAttackRangeCirclePosition());
   }

   protected override void onDeactivated () {
      base.onDeactivated();
      StopAllCoroutines();
   }

   protected override void Update () {
      if (!_isActivated) {
         return;
      }

      base.Update();

      updateAttackRangeCircle();
      updateTargetingIndicator();
   }

   [Server]
   protected override IEnumerator CO_AttackEnemiesInRange (float delayInSecondsWhenNotReloading) {
      while (!isDead()) {

         while (shouldIgnoreAttackers()) {
            yield return null;
         }

         NetEntity target = getAttackerInRange();

         // Show the charging animation on clients
         if (target) {
            Rpc_NotifyChargeUp(target.netId);
            _aimTarget = target;
            aimTransform.position = target.transform.position;
         }

         // Wait for the charge-up animation to play
         float chargeTimer = ATTACK_CHARGE_TIME;
         while (chargeTimer > 0.0f) {
            if (!_aimTarget || _aimTarget.isDead() || isDead()) {
               Rpc_NotifyCancelCharge();
               break;
            }

            float indicatorAngle = 360.0f - Util.angle(aimTransform.position - transform.position);
            targetingIndicatorParent.transform.rotation = Quaternion.Euler(0.0f, 0.0f, indicatorAngle);
            chargeTimer -= Time.deltaTime;
            yield return null;
         }

         if (isDead()) {
            Rpc_NotifyCancelCharge();
            yield break;
         }

         float waitTimeToUse = delayInSecondsWhenNotReloading;

         // Fire a shot at our target, if we charge up fully
         if (chargeTimer <= 0.0f) {
            if (target) {
               fireCannonAtTarget(aimTransform.position);
               Rpc_NotifyCannonFired();

               waitTimeToUse = reloadDelay;
            } else {
               Rpc_NotifyCancelCharge();
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
      _attackChargeStartTime = (float) NetworkTime.time;
      aimTransform.position = _aimTarget.transform.position;

      // Show the charging animation
      showTargetingEffects();
   }

   [ClientRpc]
   private void Rpc_NotifyCancelCharge () {
      hideTargetingEffects();
      if (_reticleRenderer != null) {
         _reticleRenderer.enabled = false;
      }
   }

   [ClientRpc]
   private void Rpc_NotifyCannonFired () {
      if (_isShowingTargetingIndicator) {
         hideTargetingEffects();
      }
   }

   private void showTargetingEffects () {
      targetingParabola.gameObject.SetActive(true);
      targetingIndicatorAnimator.gameObject.SetActive(true);
      targetingIndicatorRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
      targetingIndicatorRenderer.DOFade(1.0f, 0.1f);
      targetingIndicatorAnimator.SetTrigger("ChargeAndFire");
      _isShowingTargetingIndicator = true;

      aimTransform.gameObject.SetActive(true);
      aimTransform.localScale = Vector3.one * 1.25f;
      aimTransform.DOScale(1.0f, 0.25f);
      _reticleRenderer.enabled = true;
      _reticleRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
      _reticleRenderer.DOFade(1.0f, 0.25f);
   }

   private void hideTargetingEffects () {
      targetingParabola.gameObject.SetActive(false);
      DOTween.Kill(targetingIndicatorAnimator);
      DOTween.Kill(targetingIndicatorRenderer);
      targetingIndicatorAnimator.transform.localScale = Vector3.one;
      targetingIndicatorAnimator.transform.DOPunchScale(Vector3.up * 0.15f, 0.25f, 1);
      targetingIndicatorRenderer.DOFade(0.0f, 0.25f).OnComplete(() => targetingIndicatorAnimator.gameObject.SetActive(false));
      _isShowingTargetingIndicator = false;
   }

   public override void onDeath () {
      base.onDeath();
      hideTargetingEffects();

      if (isServer) {
         Rpc_NotifyCancelCharge();
      }
   }

   private void enemyAttackedPlayer (PlayerShipEntity attacker, PvpTeamType hitPlayerTeam) {
      if (pvpTeam == hitPlayerTeam && attacker && isInRange(attacker.transform.position)) {
         _aimTarget = attacker;
      }
   }

   protected override bool isInRange (Vector2 position, bool logData = false) {
      Vector2 toTarget = position - (Vector2) transform.position;
      return (toTarget.sqrMagnitude < getAttackRange() * getAttackRange());
   }

   protected override Sprite getSprite () {
      int towerStyle = towerStylesByFaction[(int) faction];
      string spritePath = (towerStyle == 0) ? "Sprites/SeaStructures/pvp_tower_style_1" : "Sprites/SeaStructures/pvp_tower_style_2";
      Sprite[] towerSprites = ImageManager.getSprites(spritePath);
      return towerSprites[getSpriteIndex()];
   }

   protected void updateTargetingIndicator () {
      // If targeting effects are showing, and either: we have no target, the target is dead, or we are dead, hide targeting effects
      if (_isShowingTargetingIndicator && (!_aimTarget || _aimTarget.isDead() || isDead())) {
         hideTargetingEffects();
         return;
      }

      // If we are showing targeting effects, update the rotation of our effects to point at our target
      if (_isShowingTargetingIndicator) {
         float indicatorAngle = 360.0f - Util.angle(aimTransform.position - transform.position);
         targetingIndicatorParent.transform.rotation = Quaternion.Euler(0.0f, 0.0f, indicatorAngle);

         // Update the targeting parabola
         Vector2 targetPosition = aimTransform.position;
         Vector2 barrelSocketPosition = targetingBarrelSocket.position;
         float targetDistance = (targetPosition - barrelSocketPosition).magnitude;
         float distanceModifier = Mathf.Clamp(targetDistance / getAttackRange(), 0.1f, 1.0f);
         float parabolaHeight = 0.25f * distanceModifier;
         targetingParabola.parabolaHeight = parabolaHeight;

         float timeSpentCharging = Mathf.Clamp01(((float) NetworkTime.time - _attackChargeStartTime) / ATTACK_CHARGE_TIME);
         Color parabolaColor = ColorCurveReferences.self.botShipTargetingParabolaColor.Evaluate(timeSpentCharging);
         targetingParabola.setParabolaColor(parabolaColor);
         targetingParabola.parabolaStart.position = targetingBarrelSocket.position;
         targetingParabola.parabolaEnd.position = aimTransform.position;
         targetingParabola.updateParabola();

         // If we haven't locked on yet, update the aim transform
         if ((timeSpentCharging / ATTACK_CHARGE_TIME) < AIM_TARGET_LOCK_TIME_NORMALISED) {
            // Find a point slightly ahead of the player's movement
            Vector2 projectedPosition = _aimTarget.getProjectedPosition(1.0f * distanceModifier);
            Vector2 toProjectedPosition = projectedPosition - (Vector2) _aimTarget.transform.position;
            float maxReticleDistanceFromTarget = 1.0f;

            // Clamp it so it doesn't extend too far when the player dashes
            if (toProjectedPosition.sqrMagnitude > maxReticleDistanceFromTarget * maxReticleDistanceFromTarget) {
               toProjectedPosition = toProjectedPosition.normalized * maxReticleDistanceFromTarget;
            }

            // Smoothly move the reticle to this position
            Vector2 reticleTargetPosition = (Vector2) _aimTarget.transform.position + toProjectedPosition;

            // If the reticle target position has moved out of range, clamp it in-range
            Vector2 toReticleTargetPosition = reticleTargetPosition - (Vector2) transform.position;
            if (toReticleTargetPosition.sqrMagnitude > getAttackRange() * getAttackRange()) {
               reticleTargetPosition = (Vector2) transform.position + toReticleTargetPosition.normalized * getAttackRange();
            }

            aimTransform.position = Vector2.Lerp(aimTransform.position, reticleTargetPosition, Time.deltaTime * AIM_TARGET_SPEED);
         }

         // If we've charged up enough to show the target lock effect, and haven't played it yet, play it
         if ((timeSpentCharging / ATTACK_CHARGE_TIME) >= AIM_TARGET_LOCK_TIME_NORMALISED && !hasPlayedTargetLockEffect()) {
            playTargetLockEffect();
         }
      }
   }

   protected void updateAttackRangeCircle () {
      PlayerShipEntity playerShipEntity = getGlobalPlayerShip();
      if (isClient && isValidTarget(playerShipEntity)) {
         float distanceToGlobalPlayerShip = Vector2.Distance(transform.position, playerShipEntity.transform.position);
         bool isInWarningRange = (distanceToGlobalPlayerShip <= getWarningRange());
         bool isInAttackRange = (distanceToGlobalPlayerShip <= getAttackRange());
         float lerpTargetAlpha = (isInWarningRange) ? 0.5f : 0.0f;

         // Fade circle out as we are dying
         if (isDead()) {
            lerpTargetAlpha = 0.0f;
         }
         _attackRangeCircleAlpha = Mathf.Lerp(_attackRangeCircleAlpha, lerpTargetAlpha, Time.deltaTime);

         Color targetColor = warningColor;
         // If the player is within attack range, show danger color if they're being targeted, otherwise show safe color
         if (isInAttackRange) {
            targetColor = (_aimTarget == playerShipEntity) ? dangerColor : safeColor;

            // If the player is within warning range, show warning color if no one is being targeted, otherwise show safe color
         } else if (isInWarningRange) {
            bool playerWillBeAttacked = (_aimTarget == null || _aimTarget.userId == playerShipEntity.userId);
            targetColor = (playerWillBeAttacked) ? warningColor : safeColor;
         }

         targetColor.a = _attackRangeCircleAlpha;
         attackRangeRenderer.material.SetColor("_Color", targetColor);
      }
   }

   protected void playTargetLockEffect () {
      _reticleRenderer.sprite = lockedReticle;

      Sequence sequence = DOTween.Sequence();
      sequence.Append(aimTransform.DOBlendableLocalRotateBy(Vector3.forward * -45.0f, 0.25f).SetEase(Ease.InOutQuint));
      sequence.AppendInterval(0.25f);
      sequence.Append(_reticleRenderer.DOFade(0.0f, 0.25f).OnComplete(() => {
         aimTransform.gameObject.SetActive(false);
         aimTransform.rotation = Quaternion.identity;
         _reticleRenderer.sprite = aimingReticle;
         _reticleRenderer.color = Color.white;
      }));
   }

   protected virtual bool isValidTarget (NetEntity entity) {
      return entity != null && entity.pvpTeam != pvpTeam && entity.pvpTeam != PvpTeamType.None;
   }

   [Server]
   protected void fireCannonAtTarget (Vector2 targetPosition) {
      Vector2 spawnPosition = targetingBarrelSocket.position;
      Vector2 toTarget = targetPosition - spawnPosition;
      float targetDistance = toTarget.magnitude;

      // If the target is out of range, fire a max range shot in their direction
      if (targetDistance > getAttackRange()) {
         targetPosition = spawnPosition + toTarget.normalized * getAttackRange();
         targetDistance = getAttackRange();
      }

      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(Attack.Type.Cannon);

      // Create the cannon ball object from the prefab
      ServerCannonBall netBall = Instantiate(PrefabsManager.self.serverCannonBallPrefab, spawnPosition, Quaternion.identity);

      // Set up the cannonball
      float distanceModifier = Mathf.Clamp(targetDistance / getAttackRange(), 0.1f, 1.0f);
      float lobHeight = 0.25f * distanceModifier;
      float lifetime = targetDistance / (Attack.getSpeedModifier(Attack.Type.Cannon) * projectileSpeedModifier);
      Vector2 velocity = toTarget.normalized * Attack.getSpeedModifier(Attack.Type.Cannon) * projectileSpeedModifier;

      netBall.initAbilityProjectile(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityData.abilityId, velocity, lobHeight, lifetime: lifetime);
      netBall.setPlayFiringSound(true);

      NetworkServer.Spawn(netBall.gameObject);

      Rpc_NoteAttack();
   }

   protected virtual float getAttackRange () {
      return PvpTower.ATTACK_RANGE;
   }

   protected virtual float getWarningRange () {
      return PvpTower.WARNING_RANGE;
   }

   protected override NetEntity getAttackerInRange (bool logData = false) {
      // If we're targeting a player ship that's in our range, don't find a new target
      if (_aimTarget && _aimTarget.isPlayerShip() && isInRange(_aimTarget.transform.position)) {
         return _aimTarget;
      }

      // First check for non-players
      // Check if any of our attackers are within range
      foreach (uint attackerId in _attackers.Keys) {
         NetEntity attacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerId);
         if (attacker == null || attacker.isDead() || attacker.isPlayerShip()) {
            continue;
         }

         Vector2 attackerPosition = attacker.transform.position;
         if (!isInRange(attackerPosition)) {
            continue;
         }

         // If we have found an attacker, exit early
         if (attacker) {
            return attacker;
         }
      }

      // If no non-players are in range, target a player
      foreach (uint attackerId in _attackers.Keys) {
         NetEntity attacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerId);
         if (attacker == null || attacker.isDead() || !attacker.isPlayerShip()) {
            continue;
         }

         Vector2 attackerPosition = attacker.transform.position;
         if (!isInRange(attackerPosition)) {
            continue;
         }

         // If we have found an attacker, exit early
         if (attacker) {
            return attacker;
         }
      }

      return null;
   }

   protected void onAttackTriggerEnter2D (Collider2D collision) {
      if (!_isActivated) {
         return;
      }

      // If we detected an enemy sea entity, add it to our attackers list
      SeaEntity seaEntity = collision.GetComponent<SeaEntity>();
      if (!seaEntity) {
         seaEntity = collision.GetComponentInParent<SeaEntity>();
      }
      if (seaEntity && seaEntity.pvpTeam != PvpTeamType.None && seaEntity.pvpTeam != pvpTeam && seaEntity.instanceId == this.instanceId) {
         _attackers[seaEntity.netId] = NetworkTime.time;
      }

      PlayerShipEntity playerShip = collision.GetComponent<PlayerShipEntity>();
      if (!playerShip) {
         playerShip = collision.GetComponentInParent<PlayerShipEntity>();
      }

      // If an enemy ship comes within our detection range, subscribe to their 'onDamagedPlayer' event
      if (playerShip && playerShip.pvpTeam != pvpTeam && playerShip.pvpTeam != PvpTeamType.None) {
         playerShip.onDamagedPlayer += enemyAttackedPlayer;
      }
   }

   protected void onAttackTriggerExit2D (Collider2D collision) {
      if (!_isActivated) {
         return;
      }

      PlayerShipEntity playerShip = collision.GetComponent<PlayerShipEntity>();
      if (!playerShip) {
         playerShip = collision.GetComponentInParent<PlayerShipEntity>();
      }

      // If an enemy ship leaves our detection range, unsubscribe from their 'onDamagedPlayer' event
      if (playerShip && playerShip.pvpTeam != pvpTeam && playerShip.pvpTeam != PvpTeamType.None) {
         playerShip.onDamagedPlayer -= enemyAttackedPlayer;
      }
   }

   protected bool hasPlayedTargetLockEffect () {
      return _reticleRenderer.sprite == lockedReticle;
   }

   protected IEnumerator CO_SetAttackRangeCirclePosition () {
      while (!isDead()) {
         yield return new WaitForSeconds(1.0f);
         attackRangeRenderer.material.SetVector("_Position", transform.position);
      }
   }

   protected PlayerShipEntity getGlobalPlayerShip () {
      if (!_globalPlayerShip && Global.player) {
         _globalPlayerShip = Global.player.getPlayerShipEntity();
      }
      return _globalPlayerShip;
   }

   #region Private Variables

   // The entity we are aiming at, and intending to fire at
   protected NetEntity _aimTarget = null;

   // The transparency value for our attack range circle
   protected float _attackRangeCircleAlpha = 0.0f;

   // A reference to the global player's ship
   protected PlayerShipEntity _globalPlayerShip = null;

   // Whether we are currently showing targeting effects
   protected bool _isShowingTargetingIndicator = false;

   // When we last started charging our attack
   protected float _attackChargeStartTime = -1.0f;

   // The ship's aim target will stop tracking the target after being this far through the charge up
   protected const float AIM_TARGET_LOCK_TIME_NORMALISED = 0.9f;

   // The speed at which the aiming reticle will track the player
   protected const float AIM_TARGET_SPEED = 2.0f;

   // A reference to the renderer for our targeting reticle
   protected SpriteRenderer _reticleRenderer;

   // How long the attack takes to charge up
   protected const float ATTACK_CHARGE_TIME = 0.75f;

   #endregion
}