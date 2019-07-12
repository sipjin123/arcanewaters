using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SceneObjectManager : NetworkBehaviour {
   #region Public Variables
      
   #endregion

   public void Start () {
      // Keep track of our Network Instance ID so that it can be used for client-server communication
      Global.netId = this.netId;
   }

   #region Private Variables

   #endregion
}
