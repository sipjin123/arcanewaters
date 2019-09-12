using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ReefGiantEntity : SeaMonsterEntity
{
   #region Public Variables

   #endregion

   protected override void Start () {
      base.Start();

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Sometimes we want to generate random waypoints
      InvokeRepeating("handleAutoMove", 1f, seaMonsterData.moveFrequency);

      // Check if we can shoot at any of our attackers
      InvokeRepeating("checkForAttackers", 2f, seaMonsterData.attackFrequency);
   }

   protected override void Update () {
      base.Update();

      animator.SetFloat("facingF", (float) this.facing);
      
      // If we're dead and have finished sinking, remove the ship
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(this.gameObject);
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // Only the server updates waypoints and movement forces
      if (!isServer || isDead()) {
         return;
      }

      // Only change our movement if enough time has passed
      if (Time.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      handleFaceDirection();

      handleWaypoints();

      // If we don't have a waypoint, we're done
      if (this.waypoint == null || Vector2.Distance(this.transform.position, waypoint.transform.position) < .08f) {
         return;
      }

      // Move towards our current waypoint
      Vector2 waypointDirection = this.waypoint.transform.position - this.transform.position;
      waypointDirection = waypointDirection.normalized;
      _body.AddForce(waypointDirection.normalized * getMoveSpeed());

      // Make note of the time
      _lastMoveChangeTime = Time.time;
   }

   protected void checkForAttackers () {
      if (isDead() || !isServer) {
         return;
      }

      // If we haven't reloaded, we can't attack
      if (!hasReloaded()) {
         return;
      }

      // Check if any of our attackers are within range
      foreach (SeaEntity attacker in _attackers) {
         if (attacker == null || attacker.isDead() || attacker == this || attacker.GetComponent<SeaMonsterEntity>() != null) {
            continue;
         }

         // Check where the attacker currently is
         Vector2 spot = attacker.transform.position;

         // If the requested spot is not in the allowed area, reject the request
         if (leftAttackBox.OverlapPoint(spot) || rightAttackBox.OverlapPoint(spot)) {
            launchProjectile(spot, attacker, seaMonsterData.attackType, .2f, .4f);
            return;
         }
      }
   }
   
   #region Private Variables

   #endregion
}
