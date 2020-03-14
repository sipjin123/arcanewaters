using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CloudShadow : MonoBehaviour {
   #region Public Variables

   // The manager
   [HideInInspector]
   public CloudShadowManager manager;

   #endregion

   void Start () {
      _renderer = GetComponent<SpriteRenderer>();

      // Pick a random offset for this cloud
      _randomOffset = Random.Range(0f, 100f);

      // Pick a random shadow type
      string shadowName = "cloud_shadow_" + Random.Range(1, 4);
      _renderer.sprite = ImageManager.getSprite("Map/" + shadowName);
   }

   void Update () {
      Vector3 pos = this.transform.position;

      // The manager's cloud bounds
      Bounds bounds = manager.expandedBounds;

      // Add a bit of a buffer so that we don't see the clouds jump around
      bounds.Expand(3f);

      // Wrap the clouds if they get outside the bounds
      if (this.transform.position.x < bounds.min.x) {
         pos.x += bounds.size.x;
      } else if (this.transform.position.x > bounds.max.x) {
         pos.x -= bounds.size.x;
      } else if (this.transform.position.y < bounds.min.y) {
         pos.y += bounds.size.y;
      } else if (this.transform.position.y > bounds.max.y) {
         pos.y -= bounds.size.y;
      }

      // Set the new position
      this.transform.position = pos;

      // Update the opacity
      float newOpacity = Mathf.Sin(Time.time * OPACITY_CHANGE_SPEED + _randomOffset) * MAX_OPACITY;
      Util.setAlpha(_renderer, Mathf.Abs(newOpacity));
   }

   #region Private Variables

   // Our renderer
   protected SpriteRenderer _renderer;

   // A random numerical offset for this cloud
   protected float _randomOffset;

   // The max opacity
   protected static float MAX_OPACITY = .15f;

   // How fast we changed opacity
   protected static float OPACITY_CHANGE_SPEED = .05f;

   #endregion
}
