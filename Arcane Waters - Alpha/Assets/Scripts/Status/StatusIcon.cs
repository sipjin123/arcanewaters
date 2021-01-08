using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StatusIcon : MonoBehaviour {
   #region Public Variables

   // What type of status this icon represents
   public Status.Type statusType;

   #endregion

   public void setLifetime (float newLifetime) {
      _lifetime = newLifetime;
   }

   private void Update () {
      _lifetime -= Time.deltaTime;

      if (_lifetime <= 0.0f) {
         Destroy(this.gameObject);
      }
   }

   #region Private Variables

   // How much time this icon has before it is destroyed
   private float _lifetime = 0.5f;

   #endregion
}
