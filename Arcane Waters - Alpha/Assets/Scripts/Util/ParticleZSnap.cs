using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static UnityEngine.ParticleSystem;

public class ParticleZSnap : MonoBehaviour {
   #region Public Variables

   // The amount of vertical offset to apply for this object
   public float offsetZ;

   #endregion

   private void Start () {
      _particleSystem = GetComponent<ParticleSystem>();
   }

   private void LateUpdate () {
      Particle[] particles = new Particle[_particleSystem.main.maxParticles];
      int nbParticles = _particleSystem.GetParticles(particles);

      for (int i = 0; i < nbParticles; i++) {
         Particle particle = particles[i];

         // Initialize our new Z position to a truncated version of the Y position
         float newZ = particle.position.y;
         newZ = Util.TruncateTo100ths(newZ);

         particle.position = new Vector3(
             particle.position.x,
             particle.position.y,
             (newZ + offsetZ) / 100f
         );

         particles[i] = particle;
      }

      _particleSystem.SetParticles(particles, nbParticles);
   }

   #region Private Variables

   // Our Particle System
   protected ParticleSystem _particleSystem;
      
   #endregion
}
