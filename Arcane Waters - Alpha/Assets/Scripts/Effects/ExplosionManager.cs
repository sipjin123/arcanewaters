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

   #region Private Variables

   #endregion
}
