using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TextAssetInitializer : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      XmlManager[] xmlManagers = GetComponentsInChildren<XmlManager>();
      foreach (XmlManager manager in xmlManagers) {
         manager.loadAllXMLData();
      }
   }

   #region Private Variables

   #endregion
}
