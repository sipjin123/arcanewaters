using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ChatLine : MonoBehaviour {
   #region Public Variables

   // The Chat Info associated with this Chat Line
   public ChatInfo chatInfo;

   // The time at which this component was created
   public float creationTime;

   #endregion

   public void Awake () {
      creationTime = Time.time;
   }

   #region Private Variables

   #endregion
}
