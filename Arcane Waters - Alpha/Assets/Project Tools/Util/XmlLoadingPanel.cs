using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class XmlLoadingPanel : MonoBehaviour {
   #region Public Variables

   // Self reference
   public static XmlLoadingPanel self;

   // The canvas that will block the view if it is loading
   public Canvas loadBlocker;

   #endregion

   private void Awake () {
      self = this;
   }

   public void startLoading () {
      loadBlocker.enabled = true;
   }

   public void finishLoading() {
      loadBlocker.enabled = false;
   }

   #region Private Variables

   #endregion
}
