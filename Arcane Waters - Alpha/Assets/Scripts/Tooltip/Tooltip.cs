using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Tooltip : MonoBehaviour {
   #region Public Variables

   // The various types of tooltips
   public enum Type {  None = 0, Ping = 1 }

   // The Text that holds our tooltip
   public Text text;

   // Our Rect Transform
   public RectTransform rectTransform;

   // The component that handles keeping our size even so the text isn't blurry
   public MakeSizeEven resizer;

   #endregion

   private void Update () {
      // If our text changed, update our size
      if (_previousText != text.text) {
         resizer.makeSizeEven();
      }

      // Keep track of our text for the next frame
      _previousText = text.text;
   }

   #region Private Variables

   // What our text was in the previous frame
   protected string _previousText;

   #endregion
}
