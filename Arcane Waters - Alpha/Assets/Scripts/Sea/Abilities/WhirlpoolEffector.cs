using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WhirlpoolEffector : MonoBehaviour {
   #region Public Variables

   // When true this whirlpool will apply forces in a clockwise direction, when false, anti-clockwise
   public bool isClockwise = true;

   // How much force this whirlpool should apply each second
   public float radialForceAmount = 1.0f, tangentialForceAmount = 1.0f;

   // The radius of the effect of this whirlpool
   public float effectRadius = 1.0f;

   // A reference to the sprite renderer displaying the whirlpool
   public SpriteRenderer whirlpoolSprite;

   #endregion

   private void Awake () {
      _collider = GetComponent<CircleCollider2D>();
      _collider.radius = effectRadius;
      _pointEffector = GetComponent<PointEffector2D>();
      _pointEffector.forceMagnitude = -radialForceAmount;

      whirlpoolSprite.transform.localScale = Vector3.one * (effectRadius / SPRITE_RADIUS);
   }

   private void FixedUpdate () {
      if (!NetworkServer.active) {
         return;
      }
      applyWhirlpoolForces();
   }

   private void applyWhirlpoolForces () {
      foreach (Rigidbody2D body in _affectedBodies) {
         applyWhirlpoolForce(body);
      }
   }

   private void applyWhirlpoolForce (Rigidbody2D body) {
      if (body == null) {
         return;
      }

      Vector2 toBody = (body.transform.position - transform.position).normalized;
      float rotationAngle = (isClockwise) ? -90.0f : 90.0f;
      Vector2 forceDirection = Quaternion.Euler(0.0f, 0.0f, rotationAngle) * toBody;

      body.AddForce(forceDirection * tangentialForceAmount * Time.deltaTime, ForceMode2D.Impulse);
   }

   private void tryAddBody (Collider2D collider) {
      if (collider.attachedRigidbody != null && !_affectedBodies.Contains(collider.attachedRigidbody)) {
         _affectedBodies.Add(collider.attachedRigidbody);
      }
   }

   private void tryRemoveBody (Collider2D collider) {
      if (collider.attachedRigidbody != null && _affectedBodies.Contains(collider.attachedRigidbody)) {
         _affectedBodies.Remove(collider.attachedRigidbody);
      }
   }

   private void OnTriggerEnter2D (Collider2D collider) {
      tryAddBody(collider);
   }

   private void OnTriggerStay2D (Collider2D collider) {
      tryAddBody(collider);
   }

   private void OnTriggerExit2D (Collider2D collider) {
      tryRemoveBody(collider);
   }

   #region Private Variables

   // A list of all bodies that are currently in the whirlpool
   private List<Rigidbody2D> _affectedBodies = new List<Rigidbody2D>();

   // The collider that will detect collisions for this whirlpool
   private CircleCollider2D _collider;

   // A reference to the point effector that applies radial forces for this whirlpool
   private PointEffector2D _pointEffector;

   // The radius needed to match the sprite for this whirlpool
   private const float SPRITE_RADIUS = 0.62f;

   #endregion
}
