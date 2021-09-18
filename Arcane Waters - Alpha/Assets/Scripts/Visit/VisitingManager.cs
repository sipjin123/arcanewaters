using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VisitingManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static VisitingManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   #region Private Variables

   // The visit request data
   private Dictionary<int, int> visitRequests = new Dictionary<int, int>();

   #endregion
}
