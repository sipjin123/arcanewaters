using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InteractableObjEntity : NetworkBehaviour {
   #region Public Variables

   // Range of maximum height that projectile will reach at its peak
   public float archHeightMin;
   public float archHeightMax;

   // Range of life time (how long projectile will be in the air) of crop
   public float lifeTimeMin;
   public float lifeTimeMax;

   // The id of the instance that this object is in
   [SyncVar]
   public int instanceId;

   // The unique id of the object
   [SyncVar]
   public int objectId;

   // The default offset of the shadow
   public float defaultShadowPosY = .05f;

   // If the physics of this object is active
   public bool simulatePhysics = false;

   // Reference to the shadow
   public Transform shadow;

   // If this object uses networked rigid body
   public bool usesNetworkRigidBody;

   // If this object can rotate
   public bool canRotate;

   // The force to apply
   public float forceToApply = 300;

   // If rigid body is being simulated
   public bool isSimulatingRigidBody;
   
   // Divides the hit force into this value when just pushing
   public const float COLLISION_FORCE_FACTOR = 5;

   // The velocity indicating the rigid body has stopeed
   public const float RESTING_VELOCITY = .01f;

   // If velocity has gathered significant value
   public bool hasReachedPeakMagnitude = false;

   #endregion

   protected virtual void Awake () {
      _rigidBody = GetComponent<Rigidbody2D>();
   }

   [ClientRpc]
   public void Rpc_BroadcastSimulationParameters (double startTime, float archHeight, float lifeTime) {
      _startTime = startTime;
      _archHeight = archHeight;
      _lifeTime = lifeTime;
   }

   public void interactObject (Vector2 dir, double startTime, float archHeight, float lifeTime, float overrideForce = 0) {
      _startTime = startTime;
      _archHeight = archHeight;
      _lifeTime = lifeTime;
      interactObject(dir, overrideForce);
   }

   public void interactObject (Vector2 dir, float overrideForce = 0) {
      if (overrideForce < 1) {
         overrideForce = forceToApply;
      }

      if (canRotate) {
         _rigidBody.AddTorque(5);
         _rigidBody.constraints = RigidbodyConstraints2D.None;
      } else {
         _rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
      }
      hasReachedPeakMagnitude = false;
      isSimulatingRigidBody = true;
      shadow.position = new Vector3(transform.position.x, transform.position.y - defaultShadowPosY);
      shadow.eulerAngles = Vector3.zero;
      _rigidBody.AddForce(new Vector2(dir.x * overrideForce, dir.y * overrideForce));
   }

   protected virtual void FixedUpdate () {
      shadow.position = new Vector3(transform.position.x, transform.position.y - defaultShadowPosY);
      shadow.eulerAngles = Vector3.zero;

      if (usesNetworkRigidBody) {
         if (isSimulatingRigidBody) {
            if (!hasReachedPeakMagnitude) {
               if (_rigidBody.velocity.magnitude > RESTING_VELOCITY) {
                  hasReachedPeakMagnitude = true;
               }
            } else {
               if (_rigidBody.velocity.magnitude < RESTING_VELOCITY) {
                  isSimulatingRigidBody = false;
                  forceStopPhysics();
               }
            }
         } else {
            forceStopPhysics();
         }
      }
   }

   private void forceStopPhysics () {
      _rigidBody.velocity = Vector2.zero;
      _rigidBody.Sleep();
      _rigidBody.constraints = RigidbodyConstraints2D.FreezeAll;
   }

   public int getInstanceId () {
      return instanceId;
   }

   #region Private Variables

   // The rigid body component
   protected Rigidbody2D _rigidBody;

   // Time at which crop projectile started its life
   [SerializeField]
   protected double _startTime;

   // Maximum height that projectile will reach at its peak
   [SerializeField]
   protected float _archHeight;

   // Life time (how long projectile will be in the air) of crop
   [SerializeField]
   protected float _lifeTime;

   // Distance that projectile will move from initial position
   protected float _distance;

   // Rotation value for full flight
   protected float _totalRotation;

   // Initial position of projectile
   protected Vector2 _startPos;

   // End position of projectile - crop pickup will be spawned here
   protected Vector2 _endPos;

   #endregion
}
