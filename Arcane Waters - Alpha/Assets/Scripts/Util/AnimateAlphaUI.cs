using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AnimateAlphaUI : ClientMonoBehaviour
{
   #region Public Variables

   // The min alpha we should ever have
   public float minAlpha = .3f;

   // The max alpha we should ever have
   public float maxAlpha = .6f;

   // The speed at which we should animate
   public float animationSpeed = 1f;

   #endregion

   void Start () {
      _image = GetComponent<Image>();
   }

   void Update () {
      float lerpTime = Mathf.Abs(Mathf.Sin(Time.time * animationSpeed));
      float newAlpha = Mathf.Lerp(minAlpha, maxAlpha, lerpTime);

      Util.setAlpha(_image, newAlpha);
   }

   #region Private Variables

   // Our Sprite Renderer
   protected Image _image;

   #endregion
}
