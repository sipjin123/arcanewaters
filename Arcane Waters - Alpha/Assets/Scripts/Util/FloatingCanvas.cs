using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FloatingCanvas : MonoBehaviour {
   #region Public Variables

   // How long this should live
   public float lifetime = 1.75f;

   // How fast this should float up
   public static float RISE_SPEED = .0025f;

   #endregion

   void Start () {
      _startTime = Time.time;

      // Our Canvas Group
      _canvasGroup = GetComponentInChildren<CanvasGroup>();

      // Make sure we show up in front
      Util.setZ(this.transform, -.32f);

      // Start floating upwards
      InvokeRepeating("floatUp", 0f, .02f);

      // Destroy after a couple seconds
      Destroy(this.gameObject, lifetime);
   }

   protected void floatUp () {
      float timeAlive = Time.time - _startTime;

      // Slowly move upwards
      Vector3 currentPos = this.transform.position;
      currentPos.y += RISE_SPEED;
      this.transform.position = currentPos;

      // Also fade out
      _canvasGroup.alpha = 1f - (timeAlive / lifetime);
   }

   #region Private Variables

   // The time at which we were created
   protected float _startTime;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
