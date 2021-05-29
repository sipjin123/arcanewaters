using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScrollRectOverride : MonoBehaviour {

   #region Public Variables

   // List of scroll rect in the game
   public List<ScrollRect> scrollList = new List<ScrollRect>();

   #endregion 

   private void Start () {
      if (Util.isLinux()) {
         foreach (ScrollRect scroller in scrollList) {
            scroller.scrollSensitivity *= -1;
         }
      }
   }
}