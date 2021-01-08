using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class TimeManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static TimeManager self;

   // If the initial data was received
   public bool hasReceivedInitialData;

   #endregion

   private void Awake () {
      self = this;
   }

   public void setLastServerDateTime (long serverDateTime) {
      _lastReceivedServerDateTime = serverDateTime;
      hasReceivedInitialData = true;
   }

   public DateTime getLastServerDateTime () {
      return NetworkServer.active ? DateTime.UtcNow : DateTime.FromBinary(_lastReceivedServerDateTime);
   }

   public long getLastServerUnixTimestamp () {
      return ((DateTimeOffset) getLastServerDateTime()).ToUnixTimeSeconds();
   }

   public void setTimeOffset (float serverTime, float roundTripTime) {
      _lastOffset = serverTime - Time.time + (roundTripTime / 1000f);
   }

   #region Private Variables

   // The last offset between our time and the server time
   protected float _lastOffset;

   // The last Date Time we received from the Server
   protected long _lastReceivedServerDateTime;

   #endregion
}
