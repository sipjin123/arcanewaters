using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericGameManager : MonoBehaviour {
   #region Public Variables

   #endregion

   protected virtual void Awake () {
      D.debug(GetType().ToString() + ".Awake");
   }

   #region Private Variables
      
   #endregion
}
