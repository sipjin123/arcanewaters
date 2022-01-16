using UnityEngine;
using System.Collections;
using Mirror;

public class DroppedItem : NetworkBehaviour, IObserver
{
   #region Public Variables

   // The instance ID of this mine
   public int instanceId;

   // If not 0, this item will only be able to be picked up by this user
   [SyncVar]
   public int limitToUserId;

   // After what time in seconds will this item despawn
   [SyncVar]
   public float lifetimeSeconds;

   // How much will the projectile travel from appearance
   [SyncVar]
   public Vector2 appearDirection;

   // The item we represent
   [SyncVar]
   public Item targetItem;

   // Range of maximum height that item will reach at its peak
   [Space(10)]
   public float appearArchHeightMin;
   public float appeararchHeightMax;

   // Range of how long will item's appearance last
   public float appearLifeTimeMin;
   public float appearLifeTimeMax;

   // Range of distance that item will move from initial position
   public float appearDistanceMin;
   public float appearDistanceMax;

   // Range of rotations during appearance
   public int appearRotationsMin = 1;
   public int appearRotationsMax = 3;

   // How many seconds to flash for before despawning
   public float flashBeforeDespawnDuration = 5;

   // Despawn flashing animation
   public AnimationCurve despawnAlphaFlashCurve;

   // How long will the item fly during pick up
   public float pickupFlyDuration = 1f;

   // Reference to the collider component
   public Collider2D colliderComponent;

   // Blocks the projectile movement
   public bool blockMovement = false;

   #endregion

   private void Update () {
      if (netId == 0) {
         return;
      }

      // If we have a time limit, control it
      if (lifetimeSeconds > 0) {
         float time = Time.time;

         // If we are on server, check if we need to destroy this (timed out)
         if (NetworkServer.active) {
            if (time - _startTime > lifetimeSeconds) {
               NetworkServer.Destroy(gameObject);
               return;
            }
         }

         // If we are the client, do flashing effect before despawning
         if (NetworkClient.active) {
            if (time - _startTime > lifetimeSeconds - flashBeforeDespawnDuration) {
               float rem = lifetimeSeconds - (time - _startTime);
               float animLength = despawnAlphaFlashCurve.keys[despawnAlphaFlashCurve.length - 1].time;
               float t = rem % animLength;
               if (t > 0) {
                  Util.setAlpha(_sr, despawnAlphaFlashCurve.Evaluate(t));
               } else {
                  Util.setAlpha(_sr, 0);
               }
            }
         }
      }

      if (_appearing) {
         animateAppearance();
      } else if (_pickedUpBy != null) {
         if (NetworkClient.active) {
            animatePickup();
         }
      }
   }

   private void animateAppearance () {
      double timeAlive = Time.time - _startTime;
      float lerpTime = (float) (timeAlive / _appearLifeTime);

      if (lerpTime >= 1f) {
         _appearing = false;
      }

      float angleInDegrees = lerpTime * 180f;
      float height = Util.getSinOfAngle(angleInDegrees) * _appearArchHeight;

      Vector3 rot = _sr.transform.localRotation.eulerAngles;

      // Calculating -log(x + 0.1) + 1; There is no physical basis to this equation
      float rotLerp = -Mathf.Log(lerpTime + 0.1f) + 1.0f;
      _sr.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(rot.x, rot.y, _appearTotalRotation * rotLerp));

      Util.setLocalY(_sr.transform, height);

      Util.setXY(this.transform, Vector2.Lerp(_appearStartPos, _appearEndPos, lerpTime));

      if (!blockMovement) {
         Util.setXY(this.transform, Vector2.Lerp(_appearStartPos, _appearEndPos, lerpTime));
      }

      // This prevents the item from bounding over obstructed areas
      ContactFilter2D contactFilter = new ContactFilter2D();
      int colliderCount = colliderComponent.OverlapCollider(contactFilter, _collisionCheckBuffer);
      if (colliderCount > 0) {
         for (int i = 0; i < colliderCount; i++) {
            if (_collisionCheckBuffer[i] != null) {
               if (_collisionCheckBuffer[i].GetComponent<CompositeCollider2D>() != null) {
                  blockMovement = true;
                  colliderComponent.enabled = false;
               }
            }
         }
      }
   }

   [Client]
   private void animatePickup () {
      float lerpTime = Mathf.Pow((float) ((Time.time - _pickupTime) / pickupFlyDuration), 2);

      // Target the waist of the player
      Vector3 targetPos = _pickedUpBy.transform.position + Vector3.up * 0.08f;
      targetPos.z = _sr.transform.position.z;
      _pickedUpAt.z = _sr.transform.position.z;

      _sr.transform.position = Vector3.Lerp(_pickedUpAt, targetPos, lerpTime);

      if (Vector3.Distance(_sr.transform.position, targetPos) < 0.16f) {
         // End it
         EffectManager.self.create(Effect.Type.Pickup_Effect, _sr.transform.position);
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.TRIUMPH_HARVEST, transform.position);
         if (_pickedUpBy == Global.player) {
            FloatingCanvas.instantiateAt(_sr.transform.position).asPickedUpItem(targetItem.getCastItem());
         }
         _sr.enabled = false;

         _pickedUpBy = null;
      }
   }

   public void setNoRotationsDuringAppearance () {
      appearRotationsMin = 0;
      appearRotationsMax = 0;
      _appearTotalRotation = 0;
   }

   private void setAnimationProperties () {
      _startTime = Time.time;

      // Synchronize by using a known seed for the random
      Random.InitState((int) netId);

      _startTime = Time.time;
      _appearArchHeight = Random.Range(appearArchHeightMin, appeararchHeightMax);
      _appearLifeTime = Random.Range(appearLifeTimeMin, appearLifeTimeMax);

      // Use one or two full rotation (360 degree) when crop is in the air
      if (appearRotationsMin < appearRotationsMax) {
         _appearTotalRotation = Random.Range(appearRotationsMin, appearRotationsMax) * 360.0f;
      }

      _appearStartPos = transform.position;
      _appearEndPos = _appearStartPos + appearDirection * Random.Range(appearDistanceMin, appearDistanceMax);
   }

   public override void OnStartClient () {
      if (targetItem == null) {
         D.error("A dropped item spawned without representing any item.");
         return;
      }

      Sprite sprite = ImageManager.getSprite(targetItem.getCastItem().getBorderlessIconPath());

      _sr.sprite = sprite;

      setAnimationProperties();
   }

   public override void OnStartServer () {
      setAnimationProperties();
   }

   private void OnTriggerStay2D (Collider2D collision) {
      // We will only check for collisions on the server
      if (!NetworkServer.active) {
         return;
      }

      // If we are appearing, we can't be picked up yet
      if (_appearing) {
         return;
      }

      // Check if we are already picked up (might still be active for visual reasons)
      if (_isPickedUp) {
         return;
      }

      // Check if it was a player that collided
      NetEntity entity = collision.GetComponent<NetEntity>();
      if (entity == null || !entity.isPlayerEntity()) {
         return;
      }

      // Check if this item is bound to this player
      if (limitToUserId != 0 && limitToUserId != entity.userId) {
         return;
      }

      // We are ready to picked up
      _isPickedUp = true;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Item createdItem = DB_Main.createItemOrUpdateItemCount(entity.userId, targetItem);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Check if we successfully created the item
            if (createdItem.id == 0) {
               D.error("Failed to save picked up item to the database");
            }
         });
      });

      Rpc_DoPickUpEffect(entity.userId);

      // Get rid of this after a while
      StartCoroutine(CO_NetworkDestroyAfter(5f));
   }

   private IEnumerator CO_NetworkDestroyAfter (float delay) {
      yield return new WaitForSeconds(delay);

      if (gameObject != null) {
         NetworkServer.Destroy(gameObject);
      }
   }


   [ClientRpc]
   private void Rpc_DoPickUpEffect (int userId) {
      _pickedUpAt = _sr.transform.position;
      _pickupTime = Time.time;
      _pickedUpBy = EntityManager.self.getEntity(userId);

      // Make it above other things during pickup animation
      _zSnap.offsetZ = -2f;
   }

   public int getInstanceId () {
      return instanceId;
   }

   #region Private Variables

   // The main sprite renderer of the dropped item
   [SerializeField]
   private SpriteRenderer _sr = null;

   // ZSnap component
   [SerializeField]
   private ZSnap _zSnap = null;

   // Buffer for checking collisions
   private Collider2D[] _collisionCheckBuffer = new Collider2D[8];

   // Did the item pick up happen already
   private bool _isPickedUp = false;

   // Is item appearing
   private bool _appearing = true;

   // Time at which item started its life
   private float _startTime;

   // Maximum height that item will reach at its peak during appearance
   private float _appearArchHeight;

   // How long will the appearance animation last for
   private float _appearLifeTime;

   // Rotation value for full flight
   private float _appearTotalRotation;

   // Initial position of item
   private Vector2 _appearStartPos;

   // End position of item appearance - it will freeze here
   private Vector2 _appearEndPos;

   // Time at which item was picked up
   private float _pickupTime;

   // By who was the item picked up
   private NetEntity _pickedUpBy;

   // At what position was the item when it was picked up
   private Vector3 _pickedUpAt;

   #endregion
}
