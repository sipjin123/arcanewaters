using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TextUtil : MonoBehaviour {
   #region Public Variables
      
   #endregion

   public static string colorText (Color color, string text) {
      return string.Format("<color={0}>{1}</color>", "#" + ColorUtility.ToHtmlStringRGBA(color), text);
   }

   #region Private Variables
      
   #endregion
}
