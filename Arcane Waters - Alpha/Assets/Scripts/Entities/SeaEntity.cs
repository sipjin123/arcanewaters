using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using DigitalRuby.LightningBolt;
using Mirror;
using System;

public class SeaEntity : NetEntity {
   #region Public Variables

   // The amount of damage we do
   [SyncVar]
   public int damage = 25;

   // How long we have to wait to reload
   [SyncVar]
   public float reloadDelay = 1f;

   // Keeps track of the consecutive attack count
   [SyncVar]
   public float attackCounter = 0f;

   // The left Attack Box
   public PolygonCollider2D leftAttackBox;

   // The right Attack Box
   public PolygonCollider2D rightAttackBox;

   // The prefab we use for creating Attack Circles
   public AttackCircle attackCirclePrefab;

   // The container for our sprites
   public GameObject spritesContainer;

   // The container for our ripples
   public GameObject ripplesContainer;

   // Convenient object references to the left and right side of our entity
   public GameObject leftSideTarget;
   public GameObject rightSideTarget;

   // Determines if this monster can be damaged
   public bool invulnerable;

   // The position data where the projectile starts
   public List<DirectionalTransform> projectileSpawnLocations;

   #endregion

   protected override void Start () {
      base.Start();

      // Keep track in our Sea Manager
      SeaManager.self.storeEntity(this);

      // Make sea entities clickable on the client
      if (isClient) {
         ClickTrigger clickTrigger = Instantiate(PrefabsManager.self.clickTriggerPrefab);
         clickTrigger.transform.SetParent(this.transform);
      }

      // Set our sprite sheets according to our types
      StartCoroutine(CO_UpdateAllSprites());
   }

   protected override void Update () {
      base.Update();

      if (NetworkServer.active && _projectileSched.Count > 0) {
         foreach (ProjectileSchedule sched in _projectileSched) {
            if (!sched.dispose && Util.netTime() > sched.projectileLaunchTime) {
               serverFireProjectile(sched.targetLocation, sched.attackType, sched.spawnLocation, sched.impactTimestamp);
               sched.dispose = true;
            }
         }
         _projectileSched.RemoveAll(item => item.dispose == true);
      }

      // If we've died, start slowing moving our sprites downward
      if (currentHealth <= 0) {
         _outline.Hide();

         Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .03f * Time.smoothDeltaTime);

         // Fade the sprites out
         if (!Application.isBatchMode) {
            foreach (SpriteRenderer renderer in _renderers) {
               if (renderer.enabled) {
                  float newAlpha = Mathf.Lerp(1f, 0f, spritesContainer.transform.localPosition.y * -10f);
                  Util.setMaterialBlockAlpha(renderer, newAlpha);
               }
            }
         }
      }
   }

   public virtual void playAttackSound () {
      // Let other classes override and implement this functionality
   }

   public bool hasRecentCombat () {
      // Did we recently attack?
      if (Time.time - _lastAttackTime < RECENT_COMBAT_COOLDOWN) {
         return true;
      }

      // Did we recently take damage?
      if (Time.time - _lastDamagedTime < RECENT_COMBAT_COOLDOWN) {
         return true;
      }

      return false;
   }

   public bool hasAnyCombat () {
      return (hasAttackers() || _lastAttackTime > 0f);
   }

   [TargetRpc]
   public void Target_CreateLocalAttackCircle (NetworkConnection connection, Vector2 startPos, Vector2 endPos, float startTime, float endTime) {
      // Create a new Attack Circle object from the prefab
      AttackCircle attackCircle = Instantiate(attackCirclePrefab, endPos, Quaternion.identity);
      attackCircle.creator = this;
      attackCircle.startPos = startPos;
      attackCircle.endPos = endPos;
      attackCircle.startTime = startTime;
      attackCircle.endTime = endTime;
      attackCircle.hasBeenPlaced = true;
   }

   [Server]
   public void chainLightning (Vector2 sourcePos, int primaryTargetID) {
      Collider2D[] hits = Physics2D.OverlapCircleAll(sourcePos, 1);
      Dictionary<NetEntity, Transform> collidedEntities = new Dictionary<NetEntity, Transform>();
      List<int> targetIDList = new List<int>();

      foreach (Collider2D collidedEntity in hits) {
         if (collidedEntity != null) {
            if (collidedEntity.GetComponent<PlayerShipEntity>() != null) {
               if (!collidedEntities.ContainsKey(collidedEntity.GetComponent<SeaEntity>())) {
                  int damage = (int) (this.damage * Attack.getDamageModifier(Attack.Type.Shock_Ball));

                  SeaEntity entity = collidedEntity.GetComponent<SeaEntity>();
                  entity.currentHealth -= damage;
                  entity.Rpc_ShowDamageText(damage, userId, Attack.Type.Shock_Ball);
                  entity.Rpc_ShowExplosion(collidedEntity.transform.position, 0, Attack.Type.None);

                  collidedEntities.Add(entity, collidedEntity.transform);
                  targetIDList.Add(entity.userId);
               }
            }
         }
      }

      Rpc_ChainLightning(targetIDList.ToArray(), primaryTargetID, sourcePos);
   }

   [ClientRpc]
   private void Rpc_ChainLightning (int[] targetIDList, int primaryTargetID, Vector2 sourcePos) {
      SeaEntity parentEntity = SeaManager.self.getEntity(primaryTargetID);

      GameObject shockResidue = Instantiate(PrefabsManager.self.lightningResiduePrefab);
      shockResidue.transform.SetParent(parentEntity.spritesContainer.transform, false);
      EffectManager.self.create(Effect.Type.Shock_Collision, sourcePos);

      foreach (int attackerID in targetIDList) {
         SeaEntity seaEntity = SeaManager.self.getEntity(attackerID);
         LightningBoltScript lightning = Instantiate(PrefabsManager.self.lightningChainPrefab);
         lightning.transform.SetParent(shockResidue.transform, false);

         lightning.StartObject.transform.position = lightning.transform.position;

         lightning.EndObject.transform.position = seaEntity.spritesContainer.transform.position;
         lightning.EndObject.transform.SetParent(seaEntity.spritesContainer.transform);

         lightning.GetComponent<LineRenderer>().enabled = true;

         GameObject subShockResidue = Instantiate(PrefabsManager.self.lightningResiduePrefab);
         subShockResidue.transform.SetParent(seaEntity.spritesContainer.transform, false);

         EffectManager.self.create(Effect.Type.Shock_Collision, seaEntity.transform.position);
      }
   }

   [ClientRpc]
   public void Rpc_CreateAttackCircle (Vector2 startPos, Vector2 endPos, float startTime, float endTime, Attack.Type attackType, bool showCircle) {
      if (showCircle) {
         // Create a new Attack Circle object from the prefab
         AttackCircle attackCircle = Instantiate(attackCirclePrefab, endPos, Quaternion.identity);
         attackCircle.creator = this;
         attackCircle.startPos = startPos;
         attackCircle.endPos = endPos;
         attackCircle.startTime = startTime;
         attackCircle.endTime = endTime;
         attackCircle.hasBeenPlaced = true;
      }

      if (attackType == Attack.Type.Boulder) {
         // Create a boulder
         BoulderProjectile boulder = Instantiate(PrefabsManager.self.getBoulderPrefab(attackType), startPos, Quaternion.identity);
         boulder.creator = this;
         boulder.startPos = startPos;
         boulder.endPos = endPos;
         boulder.startTime = startTime;
         boulder.endTime = endTime;
      } else if (attackType == Attack.Type.Shock_Ball) {
         // Create a shock ball
         ShockballProjectile shockBall = Instantiate(PrefabsManager.self.getShockballPrefab(attackType), startPos, Quaternion.identity);
         shockBall.creator = this;
         shockBall.startPos = startPos;
         shockBall.endPos = endPos;
         shockBall.startTime = startTime;
         shockBall.endTime = endTime;
         shockBall.setDirection((Direction) facing);
      } else {
         // Create a cannon smoke effect
         Vector2 direction = endPos - startPos;
         Vector2 offset = direction.normalized * .1f;
         Instantiate(PrefabsManager.self.cannonSmokePrefab, startPos + offset, Quaternion.identity);

         // Create a cannon ball
         CannonBall ball = Instantiate(PrefabsManager.self.getCannonBallPrefab(attackType), startPos, Quaternion.identity);
         ball.creator = this;
         ball.startPos = startPos;
         ball.endPos = endPos;
         ball.startTime = startTime;
         ball.endTime = endTime;
      }

      // Play an appropriate sound
      playAttackSound();

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   public void Rpc_FireHomingCannonBall (GameObject source, GameObject target, float startTime, float endTime) {
      // Create a cannon smoke effect
      Vector2 direction = target.transform.position - source.transform.position;
      Vector3 offset = direction.normalized * .1f;
      Vector3 startPos = source.transform.position + offset;
      Instantiate(PrefabsManager.self.cannonSmokePrefab, startPos, Quaternion.identity);

      // Create a cannon ball
      CannonBall ball = Instantiate(PrefabsManager.self.cannonBallPrefab, startPos, Quaternion.identity);
      ball.creator = this;
      ball.startPos = startPos;
      ball.endPos = target.transform.position;
      ball.targetObject = target;
      ball.startTime = startTime;
      ball.endTime = endTime;

      // Play an appropriate sound
      playAttackSound();
   }

   [ClientRpc]
   public void Rpc_ShowExplosion (Vector2 pos, int damage, Attack.Type attackType) {
      _lastDamagedTime = Time.time;

      if (attackType == Attack.Type.None) {
         List<Effect.Type> effectTypes = EffectManager.getEffects(attackType);
         EffectManager.show(effectTypes, pos);

         // Play the damage sound
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_1, pos);
      } else {
         // Show the explosion
         if (attackType != Attack.Type.Ice && attackType != Attack.Type.Venom) {
            Instantiate(PrefabsManager.self.explosionPrefab, pos, Quaternion.identity);
         }

         // If venom attack calls slime effect
         if (attackType == Attack.Type.Venom) {
            ExplosionManager.createSlimeExplosion(pos);
         }

         // Show the damage text
         ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(attackType), pos, Quaternion.identity);
         damageText.setDamage(damage);
      }

      // Play the damage sound
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_2, pos);
   }

   [ClientRpc]
   public void Rpc_AttachEffect (int damage, Attack.Type attackType) {
      Transform targetTransform = spritesContainer.transform;

      // If venom attack calls slime effect
      if (attackType == Attack.Type.Venom) {
         GameObject stickyInstance = Instantiate(PrefabsManager.self.venomStickyPrefab, targetTransform);
         stickyInstance.transform.localPosition = Vector3.zero;
      }
   }

   [ClientRpc]
   public void Rpc_ShowDamageText(int damage, int attackerId, Attack.Type attackType) {
      _lastDamagedTime = Time.time;

      // Note the attacker
      SeaEntity attacker = SeaManager.self.getEntity(attackerId);
      _attackers.Add(attacker);

      // Show some visual effects when the damage occurs
      List<Effect.Type> effectTypes = EffectManager.getEffects(attackType);
      EffectManager.show(effectTypes, this.transform.position);

      // Show the damage text
      ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(attackType), this.transform.position, Quaternion.identity);
      damageText.setDamage(damage);

      // Play the damage sound
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_1, this.transform.position);

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   protected void Rpc_NoteAttack () {
      // Note the time at which we last performed an attack
      _lastAttackTime = Time.time;
   }

   public float getLastAttackTime () {
      return _lastAttackTime;
   }

   public virtual void noteAttacker (NetEntity entity) {
      _attackers.Add(entity);
   }

   public bool hasReloaded () {
      float timeSinceAttack = Time.time - _lastAttackTime;

      return timeSinceAttack > this.reloadDelay;
   }

   protected IEnumerator CO_UpdateAllSprites () {
      // Wait until we receive data
      while (Util.isEmpty(this.entityName)) {
         yield return null;
      }

      // Set the new sprite
      if (this is ShipEntity) {
         ShipEntity ship = (ShipEntity) this;
         ship.spritesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(Ship.getSkinPath(ship.shipType, ship.skinType));
         ship.ripplesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(Ship.getRipplesPath(ship.shipType));
      }

      // Recolor our flags based on our Nation
      ColorKey colorKey = new ColorKey(Ship.Type.Brigantine, Layer.Flags);
      spritesContainer.GetComponent<RecoloredSprite>().recolor(colorKey, Nation.getColor1(nationType), Nation.getColor2(nationType));

      if (!Util.isEmpty(this.entityName)) {
         this.nameText.text = this.entityName;
      }
   }

   [Command]
   public void Cmd_MeleeAtSpot (Vector2 spot, Attack.Type attackType) {
      // We handle the logic in a non-Cmd function so that it can be called directly on the server if needed
      meleeAtSpot(spot, attackType);
   }

   [Server]
   public void meleeAtSpot (Vector2 spot, Attack.Type attackType) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // If the requested spot is not in the allowed area, reject the request
      if (!leftAttackBox.OverlapPoint(spot) && !rightAttackBox.OverlapPoint(spot)) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = Time.time;

      float distance = Vector2.Distance(this.transform.position, spot);
      float delay = Mathf.Clamp(distance, .5f, 1.5f);

      // Have the server check for collisions after the attack reaches the target
      StartCoroutine(CO_CheckCircleForCollisions(this, delay, spot, attackType, true));

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   [Command]
   public void Cmd_FireAtSpot (Vector2 spot, Attack.Type attackType, float attackDelay, float launchDelay) {
      // We handle the logic in a non-Cmd function so that it can be called directly on the server if needed
      fireAtSpot(spot, attackType, attackDelay, launchDelay);
   }

   [Server]
   public void fireAtSpot (Vector2 spot, Attack.Type attackType, float attackDelay, float launchDelay) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // If the requested spot is not in the allowed area, reject the request
      if (!leftAttackBox.OverlapPoint(spot) && !rightAttackBox.OverlapPoint(spot)) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = Time.time;

      // Tell all clients to display an attack circle at that position
      float distance = Vector2.Distance(this.transform.position, spot);
      float delay = Mathf.Clamp(distance, .5f, 1.5f);

      Vector3 spawnPosition = new Vector3(0, 0, 0);

      // Determines the origin of the projectile
      if (projectileSpawnLocations == null || projectileSpawnLocations.Count < 1) {
         spawnPosition = transform.position;
      } else {
         if (this.facing != 0) {
            _projectileSpawnLocation = projectileSpawnLocations.Find(_ => _.direction == (Direction) this.facing).spawnTransform;
            spawnPosition = _projectileSpawnLocation.position;
         }
      }

      if (attackDelay <= 0) {
         serverFireProjectile(spot, attackType, spawnPosition, delay);
      } else {
         // Speed modifiers for the projectile types
         delay /= Attack.getSpeedModifier(attackType);

         registerProjectileSchedule(spot, spawnPosition, attackType, attackDelay, Util.netTime() + launchDelay, delay);
      }

      attackCounter++;

      if (attackType != Attack.Type.Venom) {
         // Have the server check for collisions after the AOE projectile reaches the target
         StartCoroutine(CO_CheckCircleForCollisions(this, launchDelay + delay, spot, attackType, false));
      }

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   [Server]
   protected void registerProjectileSchedule (Vector2 targetposition, Vector2 spawnPosition, Attack.Type attackType, float animationTime, float projectileTime, float impactDelay) {
      ProjectileSchedule newSched = new ProjectileSchedule {
         attackAnimationTime = animationTime,
         projectileLaunchTime = projectileTime,
         attackType = attackType,
         spawnLocation = spawnPosition,
         targetLocation = targetposition,
         impactTimestamp = impactDelay
      };

      Rpc_RegisterAttackTime(animationTime);

      _projectileSched.Add(newSched);
   }
   
   [Server]
   protected void serverFireProjectile (Vector2 spot, Attack.Type attackType, Vector2 spawnPosition, float delay) {
      // Creates the projectile and the target circle
      if (GetComponent<PlayerShipEntity>() == null) {
         if (attackType != Attack.Type.Venom) {
            Rpc_CreateAttackCircle(spawnPosition, spot, Util.netTime(), Util.netTime() + delay, attackType, true);
         } else {
            // Create a venom
            fireTimedVenomProjectile(spawnPosition, spot);
         }
      } else {
         Target_CreateLocalAttackCircle(connectionToClient, this.transform.position, spot, Util.netTime(), Util.netTime() + delay);
         Rpc_CreateAttackCircle(spawnPosition, spot, Util.netTime(), Util.netTime() + delay, attackType, false);
      }
   }

   [Server]
   protected IEnumerator CO_CheckCircleForCollisions (SeaEntity attacker, float delay, Vector2 circleCenter, Attack.Type attackType, bool targetPlayersOnly) {
      // Wait until the cannon ball reaches the target
      yield return new WaitForSeconds(delay);

      List<NetEntity> enemyHitList = new List<NetEntity>();
      // Check for collisions inside the circle
      foreach (Collider2D hit in getHitColliders(circleCenter)) {
         if (hit != null) {
            SeaEntity entity = hit.GetComponent<SeaEntity>();

            if (!enemyHitList.Contains(entity)) {
               if (targetPlayersOnly && hit.GetComponent<ShipEntity>() == null) {
                  continue;
               }

               // Make sure the target is in our same instance
               if (entity != null && entity.instanceId == this.instanceId) {
                  if (!entity.invulnerable) {
                     int damage = (int) (this.damage * Attack.getDamageModifier(attackType));
                     entity.currentHealth -= damage;
                     entity.Rpc_ShowDamageText(damage, attacker.userId, attackType);
                     entity.Rpc_ShowExplosion(entity.transform.position, 0, Attack.Type.None);

                     if (attackType == Attack.Type.Shock_Ball) {
                        chainLightning(entity.transform.position, entity.userId);
                     }
                  } else {
                     entity.Rpc_ShowExplosion(entity.transform.position, 0, Attack.Type.None);
                  }
                  entity.noteAttacker(attacker);

                  // Apply any status effects from the attack
                  if (attackType == Attack.Type.Ice) {
                     StatusManager.self.create(Status.Type.Freeze, 2f, entity.userId);
                  } else if (attackType == Attack.Type.Tentacle) {
                     StatusManager.self.create(Status.Type.Slow, 1f, entity.userId);
                  } else if (attackType == Attack.Type.Venom) {
                     StatusManager.self.create(Status.Type.Slow, 1f, entity.userId);
                  }
                  enemyHitList.Add(entity);
               }
            }
         }
      }
   }

   public static Collider2D[] getHitColliders (Vector2 circleCenter) {
      // Check for collisions inside the circle
      Collider2D[] hits = new Collider2D[16];
      Physics2D.OverlapCircleNonAlloc(circleCenter, .20f, hits);

      return hits;
   }

   [Server]
   private void fireTimedVenomProjectile (Vector2 startPos, Vector2 targetPos) {
      if (isDead()) {
         return;
      }

      // Shoots 3 projectiles each 3 attacks
      int attackCount = 1;
      if (attackCounter % 3 == 0) {
         attackCount = 3;
      }

      // We either fire out the left or right side depending on which was clicked
      for (int i = 0; i < attackCount; i++) {
         Vector2 direction = targetPos - (Vector2) startPos;
         direction = direction.normalized;
         direction = direction.Rotate(i * 10f);

         // Figure out the desired velocity
         Vector2 velocity = direction.normalized * NetworkedVenomProjectile.MOVE_SPEED;

         // Delay the firing a little bit to compensate for lag
         float timeToStartFiring = TimeManager.self.getSyncedTime() + .150f;

         // Note the time at which we last successfully attacked
         _lastAttackTime = Time.time;

         // Make note on the clients that the ship just attacked
         Rpc_NoteAttack();

         // Tell all clients to fire the venom projectile at the same time
         Rpc_FireTimedVenomProjectile(timeToStartFiring, velocity, startPos, targetPos);

         // Standalone Server needs to call this as well
         if (!MyNetworkManager.isHost) {
            StartCoroutine(CO_FireTimedVenomProjectile(timeToStartFiring, velocity, startPos, targetPos));
         }
      }
   }

   [ClientRpc]
   public void Rpc_RegisterAttackTime (float delayTime) {
      _attackStartAnimateTime = Time.time + delayTime;
      _hasAttackAnimTriggered = false;
   }

   [ClientRpc]
   public void Rpc_FireTimedVenomProjectile (float startTime, Vector2 velocity, Vector3 startPos, Vector3 endPos) {
      StartCoroutine(CO_FireTimedVenomProjectile(startTime, velocity, startPos, endPos));
   }

   protected IEnumerator CO_FireTimedVenomProjectile (float startTime, Vector2 velocity, Vector3 startPos, Vector3 endpos) {
      float delay = startTime - TimeManager.self.getSyncedTime();

      yield return new WaitForSeconds(delay);

      // Create the venom projectile object from the prefab
      GameObject venomObject = Instantiate(PrefabsManager.self.networkedVenomProjectilePrefab, startPos, Quaternion.identity);
      NetworkedVenomProjectile netVenom = venomObject.GetComponent<NetworkedVenomProjectile>();
      netVenom.creatorUserId = this.userId;
      netVenom.instanceId = this.instanceId;
      netVenom.setDirection((Direction) facing, endpos);

      // Add velocity to the projectile
      netVenom.body.velocity = velocity;

      // Destroy the venom projectile after a couple seconds
      Destroy(venomObject, NetworkedVenomProjectile.LIFETIME);
   }

   #region Private Variables

   // Current Spawn Transform
   protected Transform _projectileSpawnLocation;

   // The time at which we last fired an attack
   protected float _lastAttackTime = float.MinValue;

   // The time at which we last took damage
   protected float _lastDamagedTime = float.MinValue;

   // How far back in time we check to see if this ship was recently involved in some combat action
   protected static float RECENT_COMBAT_COOLDOWN = 5f;

   // The list of projectiles scheduled to launch
   protected List<ProjectileSchedule> _projectileSched = new List<ProjectileSchedule>();

   // The time expected to play the animation
   protected float _attackStartAnimateTime = 100;

   // The time expected to reset the animation
   protected float _attackEndAnimateTime = 0;

   // A flag to check if the attack anim has been triggered
   protected bool _hasAttackAnimTriggered = false;

   #endregion
}