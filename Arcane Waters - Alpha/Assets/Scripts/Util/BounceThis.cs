using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BounceThis : MonoBehaviour {
   #region Public Variables

   // The speed at which we want to bounce this object
   public float speed = 1f;

   // The vertical distance we want to move
   public float distanceY = .16f;

   #endregion

   void Start () {
      _startY = this.transform.localPosition.y;
   }

   void Update () {
      Util.setLocalY(this.transform, _startY + Mathf.Sin(Time.time * speed) * distanceY);
   }

   #region Private Variables

   // Our starting Y
   protected float _startY;

   #endregion
}
