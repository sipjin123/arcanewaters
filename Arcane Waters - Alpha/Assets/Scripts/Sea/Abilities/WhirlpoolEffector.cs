using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class WhirlpoolEffector : MonoBehaviour, IMapEditorDataReceiver {
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
      _pointEffector = GetComponent<PointEffector2D>();
      _pointEffector.forceMagnitude = -radialForceAmount;

      updateRadius(effectRadius);
      updateIsClockwise(isClockwise);
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

   private void updateRadius (float newRadius) {
      effectRadius = newRadius;
      _collider.radius = effectRadius;
      whirlpoolSprite.transform.localScale = Vector3.one * (effectRadius / SPRITE_RADIUS);
   }

   private void updateIsClockwise (bool newValue) {
      isClockwise = newValue;
      whirlpoolSprite.flipX = !isClockwise;
   }

   private void updateRadialForce (float newForce) {
      radialForceAmount = newForce;
      _pointEffector.forceMagnitude = -radialForceAmount;
   }

   private void updateTangentialForce (float newForce) {
      tangentialForceAmount = newForce;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.WHIRLPOOL_RADIUS_KEY) == 0) {
            try {
               float newRadius = float.Parse(field.v);
               updateRadius(newRadius);
            } catch {

            }
         } else if (field.k.CompareTo(DataField.WHIRLPOOL_CLOCKWISE_KEY) == 0) {
            try {
               bool newValue = bool.Parse(field.v);
               updateIsClockwise(newValue);
            } catch {

            }
         } else if (field.k.CompareTo(DataField.WHIRLPOOL_RADIAL_FORCE_KEY) == 0) {
            try {
               float newRadialForce = float.Parse(field.v);
               updateRadialForce(newRadialForce);
            } catch {

            }
         } else if (field.k.CompareTo(DataField.WHIRLPOOL_TANGENTIAL_FORCE_KEY) == 0) {
            try {
               float newTangentialForce = float.Parse(field.v);
               updateTangentialForce(newTangentialForce);
            } catch {

            }
         }
      }
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
