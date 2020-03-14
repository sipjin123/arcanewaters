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

   // The key of the area this site is in
   [SyncVar]
   public string areaKey;

   // The voyage group that has captured this site
   [SyncVar]
   public int voyageGroupId = -1;

   // The voyage group currently capturing the site
   [SyncVar]
   public int capturingVoyageGroupId = -1;

   // The percentage of capture
   [SyncVar]
   public float capturePoints = 0f;

   // The sprite renderer component
   public SpriteRenderer spriteRenderer;

   // The collider, which will detect capturing ships
   public CircleCollider2D captureCollider;

   // The renderer of the capture circle
   public MeshRenderer captureCircleRenderer;

   #endregion

   public void Start () {
      // Make the site a child of the Area
      StartCoroutine(CO_SetAreaParent());

      // Get the voyage info
      _voyage = VoyageManager.self.getVoyage(areaKey);

      // By default, hide the capture circle
      captureCircleRenderer.enabled = false;
   }

   public void Update () {
      if (isCaptured()) {
         // Hide the capture circle if the site has been captured
         captureCircleRenderer.enabled = false;
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
            // Get the number of ships in range
            int shipCount = _capturingShips.Count;

            // Get the maximum number of ships per group for this voyage difficulty
            int maxShipCount = Voyage.getMaxGroupSize(_voyage.difficulty);

            // Calculate the capture speed
            float captureDuration = Mathf.Lerp(CAPTURE_MAX_DURATION, CAPTURE_MIN_DURATION, ((float) shipCount) / maxShipCount);
            float captureSpeed = 1f / captureDuration;

            // Check if the site was already being captured by the same group
            if (capturingVoyageGroupId == groupInRange) {
               // Increase the capture points
               capturePoints += captureSpeed * Time.deltaTime;

               // If the points reached the maximum, assign the site to the voyage group
               if (capturePoints >= 1) {
                  capturePoints = 1;
                  voyageGroupId = groupInRange;
               }
            }
            // If the site was being captured by another group
            else {
               // Reduce the capture points
               capturePoints -= captureSpeed * Time.deltaTime;

               // If the points reached 0, then assign the site to the group in range
               if (capturePoints <= 0) {
                  capturingVoyageGroupId = groupInRange;
                  capturePoints = -capturePoints;
               }
            }
         }
      }
      // If no ship is in range
      else {
         if (capturePoints > 0) {
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
      if (isClient && !isCaptured()) {
         // Determine if the colliding object is a player
         PlayerShipEntity player = other.transform.GetComponent<PlayerShipEntity>();
         
         if (player != null && player.isLocalPlayer && VoyageManager.isInVoyage(player)) {
            // Enable the capture circle visualization
            captureCircleRenderer.enabled = true;
         }
      }

      if (isServer) {
         // Determine if the colliding object is a ship
         ShipEntity ship = other.transform.GetComponent<ShipEntity>();
         if (ship != null && VoyageManager.isInVoyage(ship)) {
            // Add the ship to the list of capturing ships
            _capturingShips.Add(ship);
         }
      }
   }

   public void OnTriggerExit2D (Collider2D other) {
      if (isClient && !isCaptured()) {
         // Determine if the colliding object is a player
         PlayerShipEntity player = other.transform.GetComponent<PlayerShipEntity>();

         if (player != null && player.isLocalPlayer) {
            // Disable the capture circle visualization
            captureCircleRenderer.enabled = false;
         }
      }

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
      spriteRenderer.sprite = ImageManager.getSprite("Assets/Sprites/Treasure Sites/" + spriteName);
   }
   
   public bool isCaptured () {
      return voyageGroupId != -1;
   }

   private IEnumerator CO_SetAreaParent () {
      Vector3 initialPosition = transform.position;

      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return 0;
      }

      Area area = AreaManager.self.getArea(areaKey);
      this.transform.SetParent(area.transform, false);

      // Set the z position of the capture circle, above the sea and below the land
      Util.setZ(captureCircleRenderer.transform, area.waterZ - 0.01f);

      // Set the parameters in the capture circle shader
      captureCircleRenderer.material.SetVector("_Position", captureCircleRenderer.transform.position);
      captureCircleRenderer.material.SetFloat("_Radius", captureCollider.radius);

      if (isServer) {
         // Get all the nearby colliders
         Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 0.6f);

         // Find the warp associated with this treasure site
         bool found = false;
         foreach (Collider2D c in nearbyColliders) {
            Warp warp = c.gameObject.GetComponent<Warp>();
            if (warp != null) {
               // Set this treasure site as controller of the warp
               warp.setTreasureSite(this);
               found = true;
               break;
            }
         }

         if (!found) {
            D.error("Could not find the warp associated with a treasure site in area " + areaKey);
         }
      }
   }

   #region Private Variables

   // The list of ships currently capturing the site
   private HashSet<ShipEntity> _capturingShips = new HashSet<ShipEntity>();

   // The voyage associated with this area and treasure site
   private Voyage _voyage = null;

   #endregion
}
