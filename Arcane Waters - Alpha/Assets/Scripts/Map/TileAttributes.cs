using System;
using System.Collections.Generic;

[Serializable]
public class TileAttributes
{
   #region Public Variables

   // The type of attribute a tile can have
   // Note: I recommend not ever deleting entries from here for serialization,
   // instead use ex. '[Obsolete] Grass = 1'
   public enum Type : byte
   {
      None = 0,
      WaterPartial = 1,
      WaterFull = 2,
      Grass = 3,
      Stone = 4,
      Wood = 5,
      Wooden_Bridge = 6,
      Vine = 7,
      Generic = 8,
      DeepWater = 9,
      ShallowWater = 10,
      MidWater = 11,
      Carpet = 12,
      Dirt = 13,
      OutpostBridgeSnap_S = 14,
      OutpostBridgeSnap_N = 15,
      OutpostBridgeSnap_E = 16,
      OutpostBridgeSnap_W = 17,
      OutpostBridgeSnap_SW = 18,
      OutpostBridgeSnap_SE = 19,
      OutpostBridgeSnap_NW = 20,
      OutpostBridgeSnap_NE = 21,
      OutpostBaseSpot = 22,
      OutpostBasePrevent = 23,
      LandInSea = 24
   }

   // The attributes that are assigned to a tile
   public List<Type> attributes = new List<Type>();

   #endregion

   public bool hasAttribute (Type target) {
      for (int i = 0; i < attributes.Count; i++) {
         if (attributes[i] == target) {
            return true;
         }
      }

      return false;
   }

   public bool addAttribute (Type target) {
      if (target == Type.None) {
         return false;
      }

      int index = attributes.IndexOf(target);
      if (index >= 0) {
         return false;
      }

      attributes.Add(target);

      return true;
   }

   public bool removeAttribute (Type target) {
      if (target == Type.None) {
         return false;
      }

      int index = attributes.IndexOf(target);
      if (index < 0) {
         return false;
      }

      attributes.RemoveAt(index);

      return true;
   }

   #region Private Variables

   #endregion
}
