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
      bool isLinuxBuild = false;
#if UNITY_STANDALONE_LINUX
      isLinuxBuild = true;
#endif

      if (isLinuxBuild) {
         foreach (ScrollRect scroller in scrollList) {
            scroller.scrollSensitivity *= -1;
         }
      }
   }
}