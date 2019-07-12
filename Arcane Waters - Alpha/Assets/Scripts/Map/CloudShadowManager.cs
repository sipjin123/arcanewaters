using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CloudShadowManager : ClientMonoBehaviour {
   #region Public Variables

   // The prefab we use for creating Cloud Shadows
   public CloudShadow shadowPrefab;

   // The direction our clouds should move
   public Vector3 direction = new Vector2(-1f, -1f);

   // Some bounds that we can use for wrapping the objects as they move
   [HideInInspector]
   public Bounds expandedBounds;

   #endregion

   private void Start () {
      // Look up components
      _area = GetComponentInParent<Area>();

      // Look up our area's camera bounds, and expand that for the area we're going to work with
      expandedBounds = GetComponentInParent<Area>().cameraBounds.bounds;
      expandedBounds.Expand(3f);

      // Store any clouds that we already made in the Editor
      _shadows = new List<CloudShadow>(GetComponentsInChildren<CloudShadow>());

      // How spread out things are
      const float SPREAD = 2.7f;

      // Create clouds in a random grid
      for (float y = expandedBounds.min.y; y < expandedBounds.max.y; y += SPREAD) {
         for (float x = expandedBounds.min.x; x < expandedBounds.max.x; x += SPREAD) {
            Vector2 spawnPos = Util.randFromCenter(x, y, .75f);
            CloudShadow shadow = Instantiate(shadowPrefab, spawnPos, Quaternion.identity);
            shadow.transform.parent = this.transform;
            _shadows.Add(shadow);
         }
      }
   }

   private void FixedUpdate () {
      // Slowly change the direction over time
      direction = Quaternion.Euler(0, 0, Time.smoothDeltaTime) * direction;

      // Move the shadows
      foreach (CloudShadow shadow in _shadows) {
         shadow.transform.position += direction * Time.smoothDeltaTime * SPEED;
      }
   }

   #region Private Variables

   // Our associated Area
   protected Area _area;

   // The cloud shadows we manage
   protected List<CloudShadow> _shadows = new List<CloudShadow>();

   // The speed at which the clouds move
   protected static float SPEED = .05f;

   #endregion
}
