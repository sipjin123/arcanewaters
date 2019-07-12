using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BotManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static BotManager self;

   #endregion

   void Awake () {
      self = this;

      // Store references to all of our spots
      foreach (BotSpot spot in FindObjectsOfType<BotSpot>()) {
         Area area = spot.GetComponentInParent<Area>();
         Area.Type areaType = area.areaType;
         List<BotSpot> list = _spots.ContainsKey(areaType) ? _spots[areaType] : new List<BotSpot>();
         list.Add(spot);
         _spots[areaType] = list;
      }
   }

   public List<BotSpot> getSpots (Area.Type areaType) {
      if (_spots.ContainsKey(areaType)) {
         return _spots[areaType];
      }

      return new List<BotSpot>();
   }

   #region Private Variables

   // The spots we know about
   protected Dictionary<Area.Type, List<BotSpot>> _spots = new Dictionary<Area.Type, List<BotSpot>>();

   #endregion
}
