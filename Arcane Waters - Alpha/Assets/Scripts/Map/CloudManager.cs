using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CloudManager : ClientMonoBehaviour {
   #region Public Variables

   // The prefab we use for creating clouds
   public Cloud cloudPrefab;

   // The direction our clouds should move
   public Vector3 direction = new Vector2(-1f, -1f);

   // Some bounds that we can use for wrapping the objects as they move
   [HideInInspector]
   public Bounds expandedBounds;

   // The weather effect type
   public WeatherEffectType weatherEffectType = WeatherEffectType.Cloud;

   #endregion

   void Start () {
      // There is no need to create clouds on servers
      if (Util.isServerNonHost()) {
         this.enabled = false;
         return;
      }

      // Look up components
      _area = GetComponentInParent<Area>();

      // Look up our area's camera bounds, and expand that for the area we're going to work with
      expandedBounds = GetComponentInParent<Area>().cameraBounds.bounds;
      expandedBounds.Expand(3f);

      // Store any clouds that we already made in the Editor
      _clouds = new List<Cloud>(GetComponentsInChildren<Cloud>());

      float addPosVal = 2.0f;
      if (_area.isSea) {
         addPosVal = 4.0f;
      }

      // Create clouds in a random grid
      for (float y = expandedBounds.min.y; y <= expandedBounds.max.y; y += addPosVal) {
         for (float x = expandedBounds.min.x; x <= expandedBounds.max.x; x += addPosVal) {
            Vector2 spawnPos = Util.randFromCenter(x, y, 0.375f * addPosVal);
            Cloud cloud = Instantiate(cloudPrefab, spawnPos, Quaternion.identity);
            cloud.weatherEffectType = weatherEffectType;
            cloud.transform.parent = this.transform;
            cloud.area = _area;
            _clouds.Add(cloud);
         }
      }
   }

   void Update () {
      // Slowly change the direction over time
      direction = Quaternion.Euler(0, 0, Time.smoothDeltaTime) * direction;

      // Move the clouds
      foreach (Cloud cloud in _clouds) {
         cloud.transform.position += direction * Time.smoothDeltaTime * SPEED;
      }
   }

   #region Private Variables

   // Our associated Area
   protected Area _area;

   // The clouds we manage
   protected List<Cloud> _clouds = new List<Cloud>();

   // The speed at which the clouds move
   protected static float SPEED = .10f;

   #endregion
}
