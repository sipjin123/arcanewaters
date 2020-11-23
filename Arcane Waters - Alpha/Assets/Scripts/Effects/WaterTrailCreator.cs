using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WaterTrailCreator : ClientMonoBehaviour {
   #region Public Variables

   // How long we wait between spawning particles
   public float particleSpawnInterval = .10f;

   // Whether or not we want to randomize the start position
   public bool randomizeSpawnPos;

   // Whether or not we want to randomize the flip interval
   public bool randomizeFlipInterval;

   // Whether or not we want to randomize the frame length
   public bool randomizeFrameLength;

   // Whether or not to rotate the particle based on the source velocity
   public bool rotateFromVelocity;

   // The Z position for the particle
   public float zPos = 100f;

   // The minimum required distance between particle spawns
   public float minParticleDistance = -1f;

   // The prefab we use for creating water trail particles
   public WaterTrailParticle particlePrefab;

   #endregion

   void Start () {
      _entity = GetComponent<NetEntity>();

      // Routinely create water particles
      InvokeRepeating("createParticle", 0f, particleSpawnInterval);
   }

   protected void createParticle () {
      if (Global.player == null || Global.player.instanceId != _entity.instanceId) {
         return;
      }

      // We only create particles if we're in water
      if (_entity.waterChecker != null && !_entity.waterChecker.isInFullWater) {
         return;
      }

      // Only do it if we're moving
      if (!_entity.isMoving()) {
         return;
      }

      // If we have a required distance between particle spawns, check if it's too close to the last one
      if (minParticleDistance > 0f && Vector2.Distance(this.transform.position, _lastSpawnPos) < minParticleDistance) {
         return;
      }
      
      // Pick a randomized spawn position
      Vector3 spawnPos = (_entity is SeaEntity) ? _entity.sortPoint.transform.position : _entity.transform.position;
      if (randomizeSpawnPos) {
         spawnPos += new Vector3(Random.Range(-.05f, .05f), Random.Range(-.05f, .05f));
      }

      // Create the particle from the prefab
      WaterTrailParticle particle = Instantiate(particlePrefab, spawnPos, Quaternion.identity);
      particle.transform.SetParent(EffectManager.self.transform, true);

      if (rotateFromVelocity) {
         float angle = Util.AngleBetween(Vector2.up, _entity.getVelocity());
         particle.transform.Rotate(0f, 0f, angle);
      }

      // Set some random values
      if (randomizeFlipInterval) {
         particle.flipInterval = Random.Range(.25f, 5f);
      }
      if (randomizeFrameLength) {
         particle.simpleAnim.frameLengthOverride = Random.Range(.35f, .6f);
      }

      // Make the particle show up above the water, but below the land
      Util.setZ(particle.transform, zPos);

      // Give it a velocity at a fraction of the entity's velocity
      particle.body.AddForce(_entity.getVelocity() * 3f);

      // Note the position at which we spawned
      _lastSpawnPos = this.transform.position;
   }

   #region Private Variables

   // Our associated entity
   protected NetEntity _entity;

   // The position at which we last spawned a particle
   protected Vector2 _lastSpawnPos;

   #endregion
}
