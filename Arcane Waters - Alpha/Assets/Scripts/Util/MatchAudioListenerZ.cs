using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MatchAudioListenerZ : MonoBehaviour {
   #region Public Variables

   #endregion

   private void OnEnable () {
      AudioListenerManager.self.onListenerChanged += setZ;
   }

   private void OnDisable () {
      AudioListenerManager.self.onListenerChanged -= setZ;
   }

   private void setZ () {
      Util.setZ(transform, AudioListenerManager.self.getActiveListenerZ());
   }

   #region Private Variables

   #endregion
}
