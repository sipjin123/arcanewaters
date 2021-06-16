using UnityEngine;
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

   // The position which projectiels are fired from
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
   public Color dangerColor, safeColor;

   #endregion

   protected override void Awake () {
      base.Awake();

      _triggerDetector = GetComponentInChildren<TriggerDetector>();
      _triggerDetector.onTriggerEnter += onTriggerEnter2D;
      _triggerDetector.onTriggerExit += onTriggerExit2D;

      attackRangeRenderer.material.SetFloat("_Radius", ATTACK_RANGE);
      attackRangeRenderer.material.SetFloat("_FillAmount", 1.0f);
      attackRangeRenderer.material.SetColor("_Color", new Color(0.0f, 0.0f, 0.0f, 0.0f));
   }

   protected override void Start () {
      base.Start();

      if (isServer) {
         StartCoroutine(CO_AttackEnemiesInRange(0.25f));
      }

      StartCoroutine(CO_SetAttackRangeCirclePosition());
   }

   protected override void Update () {
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
         }

         // Wait for the charge-up animation to play
         float chargeTimer = 0.5f;
         while (chargeTimer > 0.0f) {
            if (!_aimTarget || _aimTarget.isDead() || isDead()) {
               break;
            }

            float indicatorAngle = 360.0f - Util.angle(_aimTarget.transform.position - transform.position);
            targetingIndicatorParent.transform.rotation = Quaternion.Euler(0.0f, 0.0f, indicatorAngle);
            chargeTimer -= Time.deltaTime;
            yield return null;
         }

         if (isDead()) {
            yield break;
         }

         float waitTimeToUse = delayInSecondsWhenNotReloading;

         // Fire a shot at our target, if we charge up fully
         if (chargeTimer <= 0.0f) {
            if (target) {
               fireCannonAtTarget(target);
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
      // _aimTarget = null;
   }

   public override void onDeath () {
      base.onDeath();
      hideTargetingEffects();
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
   private void fireCannonAtTarget (NetEntity target) {
      Vector2 targetPosition = target.transform.position;
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

   private void onTriggerEnter2D (Collider2D collision) {
      PlayerShipEntity playerShip;
      
      // On the client, activate the attack range circle when we are in-range
      if (!isServer) {
         playerShip = collision.GetComponent<PlayerShipEntity>();
         if (!playerShip) {
            playerShip = collision.GetComponentInParent<PlayerShipEntity>();
         }
         if (playerShip) {
            _showAttackRangeCircle = true;
         }

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

      playerShip = collision.GetComponent<PlayerShipEntity>();
      if (!playerShip) {
         playerShip = collision.GetComponentInParent<PlayerShipEntity>();
      }

      // If an enemy ship comes within our detection range, subscribe to their 'onDamagedPlayer' event
      if (playerShip && playerShip.pvpTeam != pvpTeam && playerShip.pvpTeam != PvpTeamType.None) {
         playerShip.onDamagedPlayer += enemyAttackedPlayer;
      }
   }

   public void receiveData (DataField[] fields) {
      base.receiveData(fields);

      foreach (DataField field in fields) {
         if (field.k.CompareTo(DataField.PVP_TOWER_RANGE) == 0) {
            try {
               // TODO: Update range of tower here
               float towerRange = float.Parse(field.v);
            } catch {

            }
         }
      }
   }

   private void onTriggerExit2D (Collider2D collision) {
      PlayerShipEntity playerShip;

      // On the client, deactivate the attack range circle when we are out of range
      if (!isServer) {
         playerShip = collision.GetComponent<PlayerShipEntity>();
         if (!playerShip) {
            playerShip = collision.GetComponentInParent<PlayerShipEntity>();
         }
         if (playerShip) {
            _showAttackRangeCircle = true;
         }

         return;
      }

      playerShip = collision.GetComponent<PlayerShipEntity>();
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
      if (isClient && getGlobalPlayerShip().pvpTeam != pvpTeam) {
         float lerpTargetAlpha = (isInRange(getGlobalPlayerShip().transform.position)) ? 0.5f : 0.0f;
         _attackRangeCircleAlpha = Mathf.Lerp(_attackRangeCircleAlpha, lerpTargetAlpha, Time.deltaTime);
         Color targetColor = (_aimTarget == getGlobalPlayerShip()) ? dangerColor : safeColor;
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
      yield return null;
      attackRangeRenderer.material.SetVector("_Position", transform.position);
   }

   private void OnDrawGizmosSelected () {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, ATTACK_RANGE);
   }


   #region Private Variables

   // The entity we are aiming at, and intending to fire at
   private NetEntity _aimTarget = null;

   // A child object that will pass on onTriggerEnter2D events, to avoid collision issues with having a trigger attached to the main seaentity object
   private TriggerDetector _triggerDetector;

   // When set to true, we will show the attack range circle
   private bool _showAttackRangeCircle = false;

   // The transparency value for our attack range circle
   private float _attackRangeCircleAlpha = 0.0f;

   // A reference to the global player's ship
   private PlayerShipEntity _globalPlayerShip = null;

   // Whether we are currently showing targeting effects
   private bool _isShowingTargetingIndicator = false;

   #endregion
}
