using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BeamLayer : MonoBehaviour {
   #region Public Variables

   // The speed at which we change
   public static float SPEED = 50f;

   // An optional time offset so that multiple layers aren't exactly in sync
   public float timeOffset = 0f;

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   #endregion

   void Start () {
      _image = GetComponent<Image>();
   }

   void Update () {
      Color color = _image.color;
      color.a = (.08f * Mathf.Sin(timeOffset + Time.time)) + .08f;
      _image.color = color;
   }

   void OnEnable () {
      StartCoroutine(CO_fadeIn());
   }

   protected IEnumerator CO_fadeIn () {
      canvasGroup.alpha = 0f;

      while (canvasGroup.alpha < 1f) {
         canvasGroup.alpha += .01f;
         yield return new WaitForSeconds(.05f);
      }

      canvasGroup.alpha = 1f;
   }

   #region Private Variables

   // The Image layer we manage
   protected Image _image;

   #endregion
}
