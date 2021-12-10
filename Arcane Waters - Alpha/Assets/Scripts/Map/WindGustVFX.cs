using UnityEngine;

public class WindGustVFX : MonoBehaviour
{
   #region Public Variables

   [Tooltip("How many particles does it emit in relation to the area?")]
   public AnimationCurve particlesOverArea = AnimationCurve.Linear(0, 0.004f, 1f, 0.016f);

   [Tooltip("How fast do the particles move?")]
   public AnimationCurve translation = AnimationCurve.Linear(0, 0.1f, 1f, 0.6f);

   [Tooltip("What's the speed of animation?")]
   public AnimationCurve animationSpeed = AnimationCurve.Linear(0, 0.25f, 1f, 1f);

   #endregion

   private void Awake () {
      // Only visual, not needed in Batch Mode
      if (Util.isBatch()) {
         gameObject.SetActive(false);
         _particleSystems = new ParticleSystem[0];
      } else {
         _particleSystems = GetComponentsInChildren<ParticleSystem>();
      }
   }

   public void setSize (Vector2 size) {
      size = new Vector2(Mathf.Clamp(size.x, 1f, 10000f), Mathf.Clamp(size.y, 1f, 1000f));
      _size = size;

      foreach (ParticleSystem ps in _particleSystems) {
         ParticleSystem.ShapeModule shape = ps.shape;
         shape.scale = new Vector3(size.x, size.y, 1f);
      }

      setStrength(_strength);
   }

   public void setStrength (float strength) {
      strength = Mathf.Clamp(strength, 0.1f, 1f);
      _strength = strength;

      foreach (ParticleSystem ps in _particleSystems) {
         ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

         ParticleSystem.EmissionModule emission = ps.emission;
         emission.rateOverTime = _size.x * _size.y * particlesOverArea.Evaluate(strength);

         ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
         ParticleSystem.MinMaxCurve forceCurve = force.x;
         forceCurve.constant = translation.Evaluate(strength);
         force.x = forceCurve;

         float dur = 1f / animationSpeed.Evaluate(strength);
         ParticleSystem.MainModule main = ps.main;
         main.duration = dur;
         ParticleSystem.MinMaxCurve lifetimeCurve = main.startLifetime;
         lifetimeCurve.constant = dur;
         main.startLifetime = lifetimeCurve;

         ps.Play();
      }
   }

   #region Private Variables

   // All the particle systems this effect has
   private ParticleSystem[] _particleSystems = null;

   // Size of effect
   private Vector2 _size = new Vector2(30, 4);

   // Strength of effect
   private float _strength = 0.5f;

   #endregion
}
