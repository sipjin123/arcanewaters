using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class Tooltip : MonoBehaviour {
   #region Public Variables

   // The various types of tooltips
   public enum Type {  None = 0, Ping = 1 }

   // The Text that holds our tooltip
   public TextMeshProUGUI text;

   // Our Rect Transform
   public RectTransform rectTransform;

   #endregion


   #region Private Variables

   #endregion
}
