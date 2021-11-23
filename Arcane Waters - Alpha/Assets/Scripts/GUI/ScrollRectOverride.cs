using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ScrollRectOverride : MonoBehaviour {

   #region Public Variables

   // List of scroll rect in the game
   public List<ScrollRect> scrollList = new List<ScrollRect>();

   // The parent object containing all of the canvas of the game
   public GameObject canvasParent;

   // The sscroll magnitude of linux inverted scroll override
   public const float SCROLL_MAGNITUDE = 3;

   #endregion 

   private void Start () {
      if (Util.isLinux()) {
         scrollList = new List<ScrollRect>();

         // Get the list of canvas children of the canvas holder
         List<Canvas> canvasList = canvasParent.GetComponentsInChildren<Canvas>(true).ToList();
         foreach (Canvas canvasRef in canvasList) {
            // The list of scroll rect within this canvas
            List<ScrollRect> scrollListInCanvas = canvasRef.GetComponentsInChildren<ScrollRect>(true).ToList();
            foreach (ScrollRect scrollRect in scrollListInCanvas) {
               scrollList.Add(scrollRect);
            }
         }

         foreach (ScrollRect scroller in scrollList) {
            scroller.scrollSensitivity *= -SCROLL_MAGNITUDE;
         }
      }
   }
}