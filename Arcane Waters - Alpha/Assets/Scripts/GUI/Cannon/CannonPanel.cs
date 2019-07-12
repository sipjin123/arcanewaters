using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CannonPanel : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static CannonPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Start () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _boxes = GetComponentsInChildren<CannonBox>();
   }

   private void Update () {
      // Hide this panel when we don't have a body
      _canvasGroup.alpha = (Global.player is PlayerShipEntity) ? 1f : 0f;
      _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our cannon boxes
   protected CannonBox[] _boxes;

   #endregion
}
