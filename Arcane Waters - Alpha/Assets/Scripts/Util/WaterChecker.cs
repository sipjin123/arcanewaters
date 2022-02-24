using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using MapCreationTool.Serialization;

public class WaterChecker : ClientMonoBehaviour
{
   #region Public Variables

   // Whether we're currently in full water or not
   public bool isInFullWater = false;

   // Whether we're currently in partial water or not
   public bool isInPartialWater = false;

   // The back water ripple
   public GameObject waterRippleBack;

   // The front water ripple
   public GameObject waterRippleFront;

   // The shadow obj
   public GameObject shadowObj;

   // Heights used for the water cover shader
   public float fullWaterHeight = 0.48f;
   public float waterHeight = 0.42f;

   #endregion

   void Start () {
      // Look up components
      _entity = GetComponent<NetEntity>();

      // Repeatedly check for water
      InvokeRepeating("checkForWater", 0f, .1f);
   }

   private void Update () {
      // Move the water overlay up or down based on whether we're in water
      foreach (SpriteRenderer renderer in _entity.getRenderers()) {
         // Default water alpha
         renderer.material.SetFloat("_WaterAlpha", 1f);

         if (isInFullWater) {
            renderer.material.SetFloat("_WaterHeight", fullWaterHeight);
            renderer.material.SetFloat("_WaterAlpha", 0f);
         } else if (isInPartialWater) {
            renderer.material.SetFloat("_WaterHeight", waterHeight);
         } else {
            renderer.material.SetFloat("_WaterHeight", 0f);
         }
      }

      // Show or hide the water ripples
      waterRippleBack.SetActive(isInFullWater);
      waterRippleFront.SetActive(isInFullWater);

      // Make note of the last time we were in water
      if (inWater()) {
         shadowObj.SetActive(false);
         _lastWaterTime = Time.time;
      } else {
         shadowObj.SetActive(true);
      }
   }

   protected void checkForWater () {
      // Default
      isInFullWater = false;
      isInPartialWater = false;

      // Look up the Area and Grid that we're currently in
      Area area = AreaManager.self.getArea(_entity.areaKey);
      if (area != null) {
         isInFullWater = area.hasTileAttribute(TileAttributes.Type.WaterFull, _entity.transform.position);
         isInPartialWater = area.hasTileAttribute(TileAttributes.Type.WaterPartial, _entity.transform.position);
      }
   }

   public bool inWater () {
      return (isInFullWater || isInPartialWater);
   }

   public float getTimeSinceWater () {
      return Time.time - _lastWaterTime;
   }

   public bool recentlyInWater () {
      return getTimeSinceWater() < 8f;
   }

   #region Private Variables

   // Our associated player
   protected NetEntity _entity;

   private float _lastWaterTime = float.MinValue;

   #endregion;
}
