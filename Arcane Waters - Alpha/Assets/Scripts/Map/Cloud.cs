using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Cloud : ClientMonoBehaviour {
   #region Public Variables

   // Our cloud sprite
   public SpriteRenderer cloudSprite;

   // Our shadow sprite
   public SpriteRenderer shadowSprite;

   // The Group of Sprites that we manage
   [HideInInspector]
   public SpriteGroup spriteGroup;

   // Gets set to true when we have a sea entity nearby
   public bool hasSomethingNearby = false;

   // The weather effect type
   public WeatherEffectType weatherEffectType;

   // The dark cloud sprite
   public Sprite darkCloudSprite, darkCloudShadow;
   public const int darkCloudAnimSpriteMax = 5;

   // Area into which this cloud is spawned
   public Area area;

   #endregion

   void Start () {
      // Look up components
      spriteGroup = GetComponent<SpriteGroup>();
      _cloudManager = GetComponentInParent<CloudManager>();

      // Pick a random offset for this cloud
      _randomOffset = Random.Range(0f, 100f);

      // Pick a random sprite type
      if (weatherEffectType != WeatherEffectType.Cloud) {
         cloudSprite.GetComponent<SimpleAnimation>().setNewTexture(darkCloudSprite.texture);
         shadowSprite.GetComponent<SimpleAnimation>().setNewTexture(darkCloudShadow.texture); 
         cloudSprite.GetComponent<SimpleAnimation>().updateIndexMinMax(0, darkCloudAnimSpriteMax);
      } else {
         if (area.isSea) {
            int randomType = new List<int>() { 1, 2, 3, 4 }.ChooseRandom();
            cloudSprite.GetComponent<SimpleAnimation>().setNewTexture(ImageManager.getTexture("Map/cloud_resize_" + randomType));
            shadowSprite.GetComponent<SimpleAnimation>().setNewTexture(ImageManager.getTexture("Map/cloud_resize_shadow_" + randomType));
         } else {
            int randomType = new List<int>() { 3, 4 }.ChooseRandom();
            cloudSprite.GetComponent<SimpleAnimation>().setNewTexture(ImageManager.getTexture("Map/cloud_" + randomType));
            shadowSprite.GetComponent<SimpleAnimation>().setNewTexture(ImageManager.getTexture("Map/cloud_" + randomType + "_shadow"));
         }
      }

      // Check for nearby objects
      InvokeRepeating(nameof(checkNearby), 0f, .7f);
   }

   void Update () {
      Vector3 pos = this.transform.position;
      Bounds bounds = _cloudManager.expandedBounds;

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
      float targetOpacity = hasSomethingNearby ? .5f : 1f;
      spriteGroup.alpha += (spriteGroup.alpha < targetOpacity) ? Time.smoothDeltaTime * OPACITY_CHANGE_SPEED : Time.smoothDeltaTime * -OPACITY_CHANGE_SPEED;
   }

   protected void checkNearby () {
      Collider2D[] results = new Collider2D[1];
      int count = Physics2D.OverlapCircleNonAlloc(cloudSprite.transform.position, .7f, results, LayerMask.GetMask(LayerUtil.SHIPS));

      // If we found a ship, then there's something near us
      hasSomethingNearby = count > 0;
   }

   #region Private Variables

   // Our associated Cloud Manager
   protected CloudManager _cloudManager;

   // A random numerical offset for this cloud
   protected float _randomOffset;

   // The max opacity
   protected static float MAX_OPACITY = .65f;

   // How fast we changed opacity
   protected static float OPACITY_CHANGE_SPEED = .60f;

   #endregion
}
