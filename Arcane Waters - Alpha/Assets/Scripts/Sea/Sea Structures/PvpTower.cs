﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using MapCreationTool.Serialization;

public class PvpTower : SeaStructure {
   #region Public Variables

   // How far away this unit can target and attack enemies
   public static float ATTACK_RANGE = 3.5f;

   // The range at which the attack range circle will be displayed
   public static float WARNING_RANGE = 4.5f;

   // The position which projectiles are fired from
   public Transform targetingBarrelSocket;

   // How much faster our projectiles will move, compared to the normal cannonballs
   public float projectileSpeedModifier = 2.0f;

   // A reference to the renderer that is displaying our attack range
   public MeshRenderer attackRangeRenderer;

   // A reference to the animator used to show where this ship is aiming
   public Animator targetingIndicatorAnimator;

   // A reference to the transform of the parent of the targeting indicator
   public Transform targetingIndicatorParent;

   // A reference to the renderer used to show where this ship is aiming
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

   #endregion

   protected override void Awake () {
      base.Awake();

      attackTriggerDetector.onTriggerEnter += onAttackTriggerEnter2D;
      attackTriggerDetector.onTriggerExit += onAttackTriggerExit2D;
      attackTriggerDetector.GetComponent<CircleCollider2D>().radius = ATTACK_RANGE;

      attackRangeRenderer.material.SetFloat("_Radius", ATTACK_RANGE);
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
      _reticleRenderer.enabled = false;
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

   private void updateTargetingIndicator () {
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
         float distanceModifier = Mathf.Clamp(targetDistance / ATTACK_RANGE, 0.1f, 1.0f);
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
            Vector2 toReticleTargetPosition = reticleTargetPosition - (Vector2)transform.position;
            if (toReticleTargetPosition.sqrMagnitude > ATTACK_RANGE * ATTACK_RANGE) {
               reticleTargetPosition = (Vector2)transform.position + toReticleTargetPosition.normalized * ATTACK_RANGE;
            }

            aimTransform.position = Vector2.Lerp(aimTransform.position, reticleTargetPosition, Time.deltaTime * AIM_TARGET_SPEED);
         }

         // If we've charged up enough to show the target lock effect, and haven't played it yet, play it
         if ((timeSpentCharging / ATTACK_CHARGE_TIME) >= AIM_TARGET_LOCK_TIME_NORMALISED && !hasPlayedTargetLockEffect()) {
            playTargetLockEffect();
         }
      }
   }

   private void playTargetLockEffect () {
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

   private bool hasPlayedTargetLockEffect () {
      return _reticleRenderer.sprite == lockedReticle;
   }

   protected override NetEntity getAttackerInRange () {
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

   [Server]
   private void fireCannonAtTarget (Vector2 targetPosition) {
      Vector2 spawnPosition = targetingBarrelSocket.position;
      Vector2 toTarget = targetPosition - spawnPosition;
      float targetDistance = toTarget.magnitude;

      // If the target is out of range, fire a max range shot in their direction
      if (targetDistance > ATTACK_RANGE) {
         targetPosition = spawnPosition + toTarget.normalized * ATTACK_RANGE;
         targetDistance = ATTACK_RANGE;
      }

      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(Attack.Type.Cannon);

      // Create the cannon ball object from the prefab
      ServerCannonBall netBall = Instantiate(PrefabsManager.self.serverCannonBallPrefab, spawnPosition, Quaternion.identity);

      // Set up the cannonball
      float distanceModifier = Mathf.Clamp(targetDistance / ATTACK_RANGE, 0.1f, 1.0f);
      float lobHeight = 0.25f * distanceModifier;
      float lifetime = targetDistance / (Attack.getSpeedModifier(Attack.Type.Cannon) * projectileSpeedModifier);
      Vector2 velocity = toTarget.normalized * Attack.getSpeedModifier(Attack.Type.Cannon) * projectileSpeedModifier;

      netBall.init(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityData.abilityId, velocity, lobHeight, false, lifetime: lifetime);

      NetworkServer.Spawn(netBall.gameObject);

      Rpc_NoteAttack();
   }

   private void onAttackTriggerEnter2D (Collider2D collision) {
      if (!_isActivated) {
         return;
      }

      // If we detected an enemy sea entity, add it to our attackers list
      SeaEntity seaEntity = collision.GetComponent<SeaEntity>();
      if (!seaEntity) {
         seaEntity = collision.GetComponentInParent<SeaEntity>();
      }
      if (seaEntity && seaEntity.pvpTeam != PvpTeamType.None && seaEntity.pvpTeam != pvpTeam) {
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

   private void onAttackTriggerExit2D (Collider2D collision) {
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

   private void enemyAttackedPlayer (PlayerShipEntity attacker, PvpTeamType hitPlayerTeam) {
      if (pvpTeam == hitPlayerTeam && attacker && isInRange(attacker.transform.position)) {
         _aimTarget = attacker;
      }
   }

   protected override bool isInRange (Vector2 position) {
      Vector2 toTarget = position - (Vector2)transform.position;
      return (toTarget.sqrMagnitude < ATTACK_RANGE * ATTACK_RANGE);
   }

   private void updateAttackRangeCircle () {
      PlayerShipEntity playerShipEntity = getGlobalPlayerShip();
      if (isClient && playerShipEntity != null && playerShipEntity.pvpTeam != pvpTeam && playerShipEntity.pvpTeam != PvpTeamType.None) {
         float distanceToGlobalPlayerShip = Vector2.Distance(transform.position, playerShipEntity.transform.position);
         bool isInWarningRange = (distanceToGlobalPlayerShip <= WARNING_RANGE);
         bool isInAttackRange = (distanceToGlobalPlayerShip <= ATTACK_RANGE);
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

   private PlayerShipEntity getGlobalPlayerShip () {
      if (!_globalPlayerShip && Global.player) {
         _globalPlayerShip = Global.player.getPlayerShipEntity();
      }
      return _globalPlayerShip;
   }

   private IEnumerator CO_SetAttackRangeCirclePosition () {
      while (!isDead()) {
         yield return new WaitForSeconds(1.0f);
         attackRangeRenderer.material.SetVector("_Position", transform.position);
      }
   }

   private void OnDrawGizmosSelected () {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, ATTACK_RANGE);
   }


   #region Private Variables

   // The entity we are aiming at, and intending to fire at
   private NetEntity _aimTarget = null;

   // The transparency value for our attack range circle
   private float _attackRangeCircleAlpha = 0.0f;

   // A reference to the global player's ship
   private PlayerShipEntity _globalPlayerShip = null;

   // Whether we are currently showing targeting effects
   private bool _isShowingTargetingIndicator = false;

   // When we last started charging our attack
   private float _attackChargeStartTime = -1.0f;

   // The ship's aim target will stop tracking the target after being this far through the charge up
   private const float AIM_TARGET_LOCK_TIME_NORMALISED = 0.9f;

   // The speed at which the aiming reticle will track the player
   private const float AIM_TARGET_SPEED = 2.0f;

   // A reference to the renderer for our targeting reticle
   private SpriteRenderer _reticleRenderer;

   // How long the attack takes to charge up
   private const float ATTACK_CHARGE_TIME = 0.75f;

   #endregion
}