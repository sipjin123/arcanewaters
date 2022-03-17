using UnityEngine;
using System.Collections;
using Mirror;

public class DroppedItem : NetworkBehaviour, IObserver
{
   #region Public Variables

   // The instance ID of this mine
   [HideInInspector]
   [SyncVar]
   public int instanceId;

   // If not 0, this item will only be able to be picked up by this user
   [SyncVar]
   public int limitToUserId = 0;

   // After what time in seconds will this item despawn
   [SyncVar]
   public float lifetimeSeconds = 600;

   // How much will the projectile travel from appearance
   [SyncVar]
   public Vector2 appearTravel = Vector2.left;

   // The item we represent
   [SyncVar]
   [HideInInspector]
   public Item targetItem;

   // In case we want to override item's sprite
   [SyncVar]
   public string itemSpriteOverride = null;

   // How long will the appearance animation last for
   [SyncVar]
   public float appearLifeTime = 1;

   // Maximum height that item will reach at its peak during appearance
   [SyncVar]
   public float appearArchHeight = 0.32f;

   // Rotations for full flight
   [SyncVar]
   public float appearRotations = 2;

   // Initial position of item
   [SyncVar]
   public Vector2 appearStartPos;

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
      float lerpTime = (float) (timeAlive / appearLifeTime);

      if (lerpTime >= 1f) {
         _appearing = false;
         lerpTime = 1f;
      }

      float easedLerpTime = lerpTime < 0.5f
         ? Util.getSinOfAngle(lerpTime * 180f) * 0.5f
         : 1f - (Util.getSinOfAngle(lerpTime * 180f) * 0.5f);

      _sr.transform.rotation = Quaternion.Euler(0, 0, lerpTime * appearRotations * 360f);

      Instance instance = null;
      if (NetworkServer.active) {
         InstanceManager.self.tryGetInstance(instanceId, out instance);
      } else if (Global.player != null) {
         instance = Global.player.getInstance();
      }

      if (!blockMovement && instance != null && AreaManager.self.tryGetArea(instance.areaKey, out Area a)) {
         Vector2 pos = Vector2.zero;

         if (easedLerpTime < 0.5f) {
            pos = new Vector2(Mathf.Lerp(appearStartPos.x, _appearMidPos.x, lerpTime * 2), Mathf.Lerp(appearStartPos.y, _appearMidPos.y, easedLerpTime * 2));
         } else {
            pos = new Vector2(Mathf.Lerp(_appearMidPos.x, _appearEndPos.x, (lerpTime - 0.5f) * 2), Mathf.Lerp(_appearMidPos.y, _appearEndPos.y, (easedLerpTime - 0.5f) * 2));
         }

         Util.setXY(transform, a.transform.TransformPoint(pos));
         Util.setXY(_shadow, a.transform.TransformPoint(Vector2.Lerp(appearStartPos, _appearEndPos, lerpTime)));
      }

      // This prevents the item from bounding over obstructed areas
      ContactFilter2D contactFilter = new ContactFilter2D();
      int colliderCount = colliderComponent.OverlapCollider(contactFilter, _collisionCheckBuffer);
      if (colliderCount > 0) {
         for (int i = 0; i < colliderCount; i++) {
            if (_collisionCheckBuffer[i].GetComponent<CompositeCollider2D>() != null) {
               // Enable to block movement after collision
               //blockMovement = true;
               colliderComponent.enabled = false;
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
      _shadow.position = Vector3.Lerp(_pickedUpAt, targetPos + Vector3.down * 0.08f, lerpTime);

      if (Vector3.Distance(_sr.transform.position, targetPos) < 0.16f) {
         // End it
         EffectManager.self.create(Effect.Type.Pickup_Effect, _sr.transform.position);
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.TRIUMPH_HARVEST, transform.position);
         if (_pickedUpBy == Global.player) {
            FloatingCanvas.instantiateAt(_sr.transform.position).asPickedUpItem(targetItem.getCastItem());
         }
         _sr.enabled = false;
         _shadow.gameObject.SetActive(false);

         _pickedUpBy = null;
      }
   }

   private void setAnimationProperties () {
      if (InstanceManager.self.tryGetInstance(instanceId, out Instance i)) {
         transform.parent = i.transform;
      }

      if (targetItem != null) {
         name = "Dropped item: " + targetItem.getCastItem().getName();
      }

      _startTime = Time.time;

      _appearEndPos = appearStartPos + appearTravel;
      _appearMidPos = (appearStartPos + _appearEndPos) * 0.5f + Vector2.up * appearArchHeight;
   }

   public override void OnStartClient () {
      if (targetItem == null) {
         D.error("A dropped item spawned without representing any item.");
         return;
      }

      string iconpath = string.IsNullOrWhiteSpace(itemSpriteOverride)
         ? targetItem.getCastItem().getBorderlessIconPath()
         : itemSpriteOverride;

      Sprite sprite = ImageManager.getSprite(iconpath);

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

   // The shadow renderer
   [SerializeField]
   private Transform _shadow = null;

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

   // Midway point of item's appearance - the peak of arch
   private Vector2 _appearMidPos;

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
