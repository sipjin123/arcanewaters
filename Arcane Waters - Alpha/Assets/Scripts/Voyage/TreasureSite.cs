using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureSite : NetworkBehaviour
{
   #region Public Variables

   // The seconds needed to capture the site, depending on the number of ships capturing it
   public static float CAPTURE_MAX_DURATION = 30;
   public static float CAPTURE_MIN_DURATION = 10;

   // The seconds for the site to lose all its capture points when no ship is capturing it
   public static float CAPTURE_RESET_DURATION = 60;

   // The status of the site
   public enum Status { Idle = 0, Captured = 1, Capturing = 2, Blocked = 3, Stealing = 4, Resetting = 5 }

   // The id of the Instance that this site is in
   [SyncVar]
   public int instanceId;

   // The key of the area this site is in
   [SyncVar]
   public string areaKey;

   // The voyage group that has ownership of this site, and has captured or is capturing it
   [SyncVar]
   public int voyageGroupId = -1;

   // The voyage group currently in range of the site, either capturing or stealing it
   [SyncVar]
   public int inRangeVoyageGroupId = -1;

   // The percentage of capture
   [SyncVar]
   public float capturePoints = 0f;

   // The difficulty of the voyage where this site is
   [SyncVar]
   public Voyage.Difficulty difficulty = Voyage.Difficulty.None;

   // The status of the site
   [SyncVar]
   public Status status = Status.Idle;

   // The capture circle
   public TreasureSiteCaptureCircle captureCircle;

   #endregion

   public void Awake () {
      _spriteRenderer = GetComponent<SpriteRenderer>();
      _captureCollider = GetComponent<CircleCollider2D>();
      _animator = GetComponent<Animator>();
   }

   public void Start () {
      // Make the site a child of the Area
      StartCoroutine(CO_SetAreaParent());
   }

   public void Update () {
      if (isCaptured()) {
         // Play the captured animation
         _animator.SetBool("captured", true);
      }
      
      if (!isServer || isCaptured()) {
         return;
      }

      // Check if at least one ship is in capture range
      if (_capturingShips.Count > 0) {
         // Get the group id of the first ship
         int groupInRange = -1;
         foreach (ShipEntity ship in _capturingShips) {
            groupInRange = ship.voyageGroupId;
            break;
         }

         // Determine if the ships in range belong to the same voyage group
         bool sameGroup = true;
         foreach (ShipEntity ship in _capturingShips) {
            if (groupInRange != ship.voyageGroupId) {
               sameGroup = false;
               break;
            }
         }

         // If all the ships belong to the same group
         if (sameGroup) {
            // Set the id of the group in range
            inRangeVoyageGroupId = groupInRange;

            // Get the number of ships in range
            int shipCount = _capturingShips.Count;

            // Get the maximum number of ships per group for this voyage difficulty
            int maxShipCount = Voyage.getMaxGroupSize(difficulty);

            // Calculate the capture speed
            float captureDuration = Mathf.Lerp(CAPTURE_MAX_DURATION, CAPTURE_MIN_DURATION, ((float) shipCount) / maxShipCount);
            float captureSpeed = 1f / captureDuration;

            // Check if the site was already being captured by the same group
            if (groupInRange == voyageGroupId) {
               // Update the status
               status = Status.Capturing;

               // Increase the capture points
               capturePoints += captureSpeed * Time.deltaTime;

               // If the points reached the maximum, assign the site to the voyage group
               if (capturePoints >= 1) {
                  capturePoints = 1;
                  status = Status.Captured;

                  // Increase the number of captured treasure sites in the instance
                  Instance instance = InstanceManager.self.getInstance(instanceId);
                  if (instance != null) {
                     instance.capturedTreasureSiteCount++;
                  }
               }
            }
            // If the site was being captured by another group
            else {
               // Set the status
               status = Status.Stealing;

               // Reduce the capture points
               capturePoints -= captureSpeed * Time.deltaTime;

               // If the points reached 0, change the ownership and start capturing
               if (capturePoints <= 0) {
                  capturePoints = -capturePoints;
                  voyageGroupId = groupInRange;
                  status = Status.Capturing;
               }
            }
         }
         // If the ships in range belong to different groups
         else {
            status = Status.Blocked;
            inRangeVoyageGroupId = -1;
         }
      }
      // If no ship is in range
      else {
         if (capturePoints > 0) {
            // Set the status
            status = Status.Resetting;
            inRangeVoyageGroupId = -1;

            // Slowly reduce the capture points
            float captureSpeed = 1f / CAPTURE_RESET_DURATION;
            capturePoints -= captureSpeed * Time.deltaTime;
            if (capturePoints <= 0) {
               capturePoints = 0;
            }
         }
      }
   }

   public void OnTriggerEnter2D (Collider2D other) {
      if (isServer) {
         // Determine if the colliding object is a ship
         ShipEntity ship = other.transform.GetComponent<ShipEntity>();
         if (ship != null && ship.instanceId == instanceId && VoyageManager.isInVoyage(ship)) {
            // Add the ship to the list of capturing ships
            _capturingShips.Add(ship);
         }
      }
   }

   public void OnTriggerExit2D (Collider2D other) {
      if (isServer) {
         // Determine if the colliding object is a ship
         ShipEntity ship = other.transform.GetComponent<ShipEntity>();
         if (ship != null) {
            // Remove the ship from the list of capturing ships
            _capturingShips.Remove(ship);
         }
      }
   }

   public void setBiome (Biome.Type biomeType) {
      string spriteName = "site_" + biomeType.ToString().ToLower() + "-new";
      _spriteRenderer.sprite = ImageManager.getSprite("Assets/Sprites/Treasure Sites/" + spriteName);
   }
   
   public bool isCaptured () {
      return status == Status.Captured;
   }

   public float getCaptureRadius() {
      return _captureCollider.radius;
   }

   public void OnDestroy () {
      // Remove the link between the treasure site and its warp
      if (_warp != null) {
         _warp.removeTreasureSite(instanceId);
      }
   }

   public void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.treasureSiteParent, worldPositionStays);
   }

   private IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return 0;
      }

      // Set the site as a child of the area
      Area area = AreaManager.self.getArea(this.areaKey);
      bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2) transform.position);
      setAreaParent(area, worldPositionStays);

      // Get all the nearby colliders
      Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 0.6f);

      // Find the warp associated with this treasure site
      bool found = false;
      foreach (Collider2D c in nearbyColliders) {
         Warp warp = c.gameObject.GetComponent<Warp>();
         if (warp != null) {
            // Set this treasure site as controller of the warp
            warp.setTreasureSite(instanceId, this);
            _warp = warp;
            found = true;
            break;
         }
      }

      if (!found) {
         D.debug("Could not find the warp associated with a treasure site in area " + areaKey);
      }
   }

   #region Private Variables

   // The list of ships currently capturing the site
   private HashSet<ShipEntity> _capturingShips = new HashSet<ShipEntity>();

   // The sprite renderer component
   private SpriteRenderer _spriteRenderer;

   // The collider, which will detect capturing ships
   private CircleCollider2D _captureCollider;

   // The animator component
   private Animator _animator;

   // The associated warp
   private Warp _warp = null;

   #endregion
}
