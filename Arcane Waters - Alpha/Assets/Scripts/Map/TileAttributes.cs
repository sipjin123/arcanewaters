using System;
using System.Collections.Generic;

[Serializable]
public class TileAttributes
{
   #region Public Variables

   // The type of attribute a tile can have
   // Note: I recommend not ever deleting entries from here for serialization8,
   // instead use ex. '[Obsolete] Grass = 1'
   public enum Type : byte
   {
      None = 0,
      WaterPartial = 1,
      WaterFull = 2,
      Grass = 3,
      Stone = 4,
      Wood = 5
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
