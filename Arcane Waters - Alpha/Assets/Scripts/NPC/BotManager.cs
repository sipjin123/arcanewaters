using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BotManager : GenericGameManager {
   #region Public Variables

   // Self
   public static BotManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      // Store references to all of our spots
      foreach (BotSpot spot in FindObjectsOfType<BotSpot>()) {
         Area area = spot.GetComponentInParent<Area>();
         string areaKey = area.areaKey;
         List<BotSpot> list = _spots.ContainsKey(areaKey) ? _spots[areaKey] : new List<BotSpot>();
         list.Add(spot);
         _spots[areaKey] = list;
      }
   }

   public List<BotSpot> getSpots (string areaKey) {
      if (_spots.ContainsKey(areaKey)) {
         return _spots[areaKey];
      }

      return new List<BotSpot>();
   }

   #region Private Variables

   // The spots we know about in each area
   protected Dictionary<string, List<BotSpot>> _spots = new Dictionary<string, List<BotSpot>>();

   #endregion
}
