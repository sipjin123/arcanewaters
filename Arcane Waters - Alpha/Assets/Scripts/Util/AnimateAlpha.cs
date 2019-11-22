using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AnimateAlpha : ClientMonoBehaviour {
   #region Public Variables

   // The min alpha we should ever have
   public float minAlpha = .3f;

   // The max alpha we should ever have
   public float maxAlpha = .6f;

   // The speed at which we should animate
   public float animationSpeed = 1f;

   // The player area that this animation is intended for
   public string enabledArea;

   #endregion

   void Start () {
      _renderer = GetComponent<SpriteRenderer>();
   }

   void Update () {
      if (Global.player == null || Global.player.areaKey != enabledArea) {
         return;
      }

      float lerpTime = Mathf.Abs(Mathf.Sin(Time.time * animationSpeed));
      float newAlpha = Mathf.Lerp(minAlpha, maxAlpha, lerpTime);

      Util.setAlpha(_renderer, newAlpha);
   }

   #region Private Variables

   // Our Sprite Renderer
   protected SpriteRenderer _renderer;

   #endregion
}
