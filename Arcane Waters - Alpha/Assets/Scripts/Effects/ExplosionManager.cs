using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ExplosionManager : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static ExplosionManager self;

   // The prefab we use for creating explosion particles
   public ExplosionParticle explosionParticlePrefab;

   // The prefab we use for creating rock explosion particles
   public ExplosionParticle rockExplosionParticlePrefab;

   // The prefab we use for creating seed scatter effect
   public ExplosionParticle seedScatterParticlePrefab;

   // The prefab we use for creating water effect
   public ExplosionParticle waterScatterParticlePrefab;

   // The prefab we use for creating harvest effect
   public ExplosionParticle harvestParticlePrefab;

   // The prefab we use for creating slime explosion particles
   public ExplosionParticle slimeExplosionParticlePrefab;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   public static void createExplosion (Vector2 position, int particleCount = 12, float minForce = 60f, float maxForce = 90f) {
      // Create a bunch of particles from the prefab
      for (int i = 0; i < particleCount; i++) {
         ExplosionParticle particle = Instantiate(self.explosionParticlePrefab, position, Quaternion.identity);
         Vector2 direction = new Vector2(Random.Range(-.35f, .35f), 1f);
         float force = Random.Range(minForce, maxForce);
         particle.body.AddForce(force * direction);
         particle.body.AddTorque(Random.Range(-1000f, 1000f));
      }
   }

   public static void createRockExplosion (Vector2 position, int particleCount = 12, float minForce = 60f, float maxForce = 90f) {
      if (Application.isBatchMode) {
         return;
      }

      // Create a bunch of particles from the prefab
      for (int i = 0; i < particleCount; i++) {
         ExplosionParticle particle = Instantiate(self.rockExplosionParticlePrefab, position, Quaternion.identity);
         Vector2 direction = new Vector2(Random.Range(-.35f, .35f), 1f);
         float force = Random.Range(minForce, maxForce);
         particle.body.AddForce(force * direction);
         particle.body.AddTorque(Random.Range(-1000f, 1000f));
      }
   }

   public static void createFarmingParticle (Weapon.ActionType actionType, Vector2 position, float fadeSpeed, int particleCount = 12, bool hasTorque = true, float minForce = 60f, float maxForce = 90f) {
      if (Application.isBatchMode) {
         return;
      }
      ExplosionParticle selectedPrefab = self.seedScatterParticlePrefab;

      switch (actionType) {
         case Weapon.ActionType.PlantCrop:
            selectedPrefab = self.seedScatterParticlePrefab;
            break;
         case Weapon.ActionType.WaterCrop:
            selectedPrefab = self.waterScatterParticlePrefab;
            break;
         case Weapon.ActionType.HarvestCrop:
            selectedPrefab = self.harvestParticlePrefab;
            break;
      }

      // Create a bunch of particles from the prefab
      for (int i = 0; i < particleCount; i++) {
         ExplosionParticle particle = Instantiate(selectedPrefab, position, Quaternion.identity);
         particle.fadeSpeed = fadeSpeed;
         Vector2 direction = new Vector2(Random.Range(-.35f, .35f), 1f);
         float force = Random.Range(minForce, maxForce);
         particle.body.AddForce(force * direction);
         if (hasTorque) {
            particle.body.AddTorque(Random.Range(-1000f, 1000f));
         }
      }
   }

   public static void createSlimeExplosion (Vector2 position, int particleCount = 12, float minForce = 60f, float maxForce = 90f) {
      if (Application.isBatchMode) {
         return;
      }

      // Create a bunch of particles from the prefab
      for (int i = 0; i < particleCount; i++) {
         ExplosionParticle particle = Instantiate(self.slimeExplosionParticlePrefab, position, Quaternion.identity);
         Vector2 direction = new Vector2(Random.Range(-.35f, .35f), 1f);
         float force = Random.Range(minForce, maxForce);
         particle.body.AddForce(force * direction);
         particle.body.AddTorque(Random.Range(-1000f, 1000f));
      }
   }

   #region Private Variables

   #endregion
}
