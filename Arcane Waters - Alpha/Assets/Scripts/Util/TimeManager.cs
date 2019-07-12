﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class TimeManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static TimeManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void setLastServerDateTime (long serverDateTime) {
      _lastReceivedServerDateTime = serverDateTime;
   }

   public DateTime getLastServerDateTime () {
      return Util.isServer() ? DateTime.UtcNow : DateTime.FromBinary(_lastReceivedServerDateTime);
   }

   public long getLastServerUnixTimestamp () {
      return ((DateTimeOffset) getLastServerDateTime()).ToUnixTimeSeconds();
   }

   public float getSyncedTime () {
      // On the server, we just return the time
      if (NetworkServer.active) {
         return Time.time;
      }

      // On the client, we need to calculate the difference between our time and the server time
      return Time.time + _lastOffset;
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
