﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

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

   // The biome of the instance the site is in
   [SyncVar]
   public Biome.Type instanceBiome;

   // The target spawn to return from treasure site
   [SyncVar]
   public string spawnTarget;

   // The destination area this site warps to
   [SyncVar]
   public string destinationArea;

   // The destination instance this site warps to
   [SyncVar]
   public int destinationInstanceId = -1;

   // The destination spawn this site warps to
   [SyncVar]
   public string destinationSpawn;

   // The voyage group that has ownership of this site, and has captured or is capturing it
   [SyncVar]
   public int voyageGroupId = -1;

   // The voyage group currently in range of the site, either capturing or stealing it
   [SyncVar]
   public int inRangeVoyageGroupId = -1;

   // The percentage of capture
   [SyncVar]
   public float capturePoints = 0f;

   // Gets set to true when the destination instance is cleared of enemies
   [SyncVar]
   public bool isClearedOfEnemies = false;

   // The difficulty of the voyage where this site is
   [SyncVar]
   public Voyage.Difficulty difficulty = Voyage.Difficulty.None;

   // The status of the site
   [SyncVar]
   public Status status = Status.Idle;

   // The capture circle
   public TreasureSiteCaptureCircle captureCircle;

   // Players currently in this treasure site
   public List<int> playerListInSite = new List<int>();

   #endregion

   public void Awake () {
      _spriteRenderer = GetComponent<SpriteRenderer>();
      _captureCollider = GetComponent<CircleCollider2D>();
      _animator = GetComponent<Animator>();
   }

   public virtual void Start () {
      // Choose the random destination of this site
      chooseRandomDestinationArea();

      // Make the site a child of the Area
      StartCoroutine(CO_SetAreaParent());
   }

   public virtual void Update () {
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

   public virtual void OnTriggerEnter2D (Collider2D other) {
      if (isServer) {
         // Determine if the colliding object is a ship
         ShipEntity ship = other.transform.GetComponent<ShipEntity>();
         if (ship != null && ship.instanceId == instanceId && VoyageGroupManager.isInGroup(ship)) {
            // Add the ship to the list of capturing ships
            _capturingShips.Add(ship);

            // Capturing a treasure site is considered a PvP action
            if (!isCaptured()) {
               ship.hasEnteredPvP = true;
            }
         }
      }

      if (isClient) {
         // Determine if the local player collided
         PlayerShipEntity playerShip = other.transform.GetComponent<PlayerShipEntity>();
         if (Global.player != null && playerShip != null && playerShip.instanceId == instanceId && Global.player == playerShip) {
            // Trigger the tutorial
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.EnterTreasureSiteRange);
         }
      }
   }

   public virtual void OnTriggerExit2D (Collider2D other) {
      if (isServer) {
         // Determine if the colliding object is a ship
         ShipEntity ship = other.transform.GetComponent<ShipEntity>();
         if (ship != null) {
            // Remove the ship from the list of capturing ships
            _capturingShips.Remove(ship);
         }
      }
   }

   private void chooseRandomDestinationArea () {
      if (!isServer) {
         return;
      }

      // Find potential maps
      if (_randomTreasureSites.Count == 0) {
         foreach (string key in AreaManager.self.getAreaKeys()) {
            if (AreaManager.self.getAreaSpecialType(key) == Area.SpecialType.TreasureSite) {
               _randomTreasureSites.Add(key);
            }
         }
      }

      // Choose map and target spawn
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<MapSpawn> mapSpawns = DB_Main.getMapSpawns();

         // Process downloaded data
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            while (_randomTreasureSites.Count > 0) {
               destinationArea = _randomTreasureSites.ChooseRandom();
               foreach (MapSpawn spawn in mapSpawns) {
                  if (spawn.mapName == destinationArea) {
                     destinationSpawn = spawn.name;
                     break;
                  }
               }

               // If map doesn't have any spawn, try another treasure site map
               if (destinationSpawn == "") {
                  _randomTreasureSites.Remove(destinationArea);
                  destinationArea = "";
               } else {
                  break;
               }
            }

            // If there are no existing treasure site maps or no treasure site map has correct spawn target
            if (destinationArea == "" || destinationSpawn == "") {
               D.error("No treasure site maps available");
            }
         });
      });
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

   public Direction getWarpDirection () {
      return _warp == null ? Direction.North : _warp.newFacingDirection;
   }

   public int getWarpHashCode () {
      return _warp ? _warp.GetHashCode() : 0;
   }

   private IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return 0;
      }

      // Set the site as a child of the area
      Area area = AreaManager.self.getArea(this.areaKey);
      setBiome(instanceBiome);
      bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2) transform.position);
      setAreaParent(area, worldPositionStays);

      // Disable the treasure site capture if the instance is not a voyage instance
      if (isServer) {
         Instance instance = InstanceManager.self.getInstance(instanceId);
         if (instance == null || instance.voyageId <= 0) {
            enabled = false;
            _captureCollider.enabled = false;
            yield break;
         }
      }

      // Get all the nearby colliders
      Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 0.8f);

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

      if (isServer) {
         setSpawnTarget();
      }
   }

   private void setSpawnTarget () {
      if (_warp == null) {
         D.debug("Warp is missing!");
         return;
      }
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<MapSpawn> mapSpawns = DB_Main.getMapSpawns();
         MapSpawn finalSpawn = null;
         float minDist = float.MaxValue;

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (MapSpawn spawn in mapSpawns) {
               if (spawn.mapName == areaKey) {
                  if (Vector2.Distance(new Vector2(spawn.posX, spawn.posY), _warp.transform.localPosition) < minDist) {
                     minDist = Vector2.Distance(new Vector2(spawn.posX, spawn.posY), _warp.transform.localPosition);
                     finalSpawn = spawn;
                  }
               }
            }

            if (finalSpawn != null) {
               spawnTarget = finalSpawn.name;
            }
         });
      });
   }

   public virtual bool isOwnedByGroup (int voyageGroupId) {
      return voyageGroupId == this.voyageGroupId;
   }

   public virtual bool isActive () {
      return true;
   }

   #region Private Variables

   // The list of ships currently capturing the site
   private HashSet<ShipEntity> _capturingShips = new HashSet<ShipEntity>();

   // The sprite renderer component
   protected SpriteRenderer _spriteRenderer;

   // The collider, which will detect capturing ships
   private CircleCollider2D _captureCollider;

   // The animator component
   protected Animator _animator;

   // The associated warp
   private Warp _warp = null;

   // Maps that can be chosen as destination of this warp
   private static List<string> _randomTreasureSites = new List<string>();

   #endregion
}
