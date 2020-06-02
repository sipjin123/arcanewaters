using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class StandaloneInputModuleV2 : StandaloneInputModule
{
   #region Public Variables

   // Self
   public static StandaloneInputModuleV2 self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public GameObject getGameObjectUnderPointer (int pointerId) {
      var lastPointer = GetLastPointerEventData(pointerId);
      if (lastPointer != null)
         return lastPointer.pointerCurrentRaycast.gameObject;
      return null;
   }

   public GameObject getGameObjectUnderPointer () {
      return getGameObjectUnderPointer(PointerInputModule.kMouseLeftId);
   }

   #region Private Variables

   #endregion

}
