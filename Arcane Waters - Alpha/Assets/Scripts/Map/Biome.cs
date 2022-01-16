using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class Biome
{
   #region Public Variables

   // The Type of Biome
   public enum Type { None = 0, Forest = 1, Desert = 2, Pine = 3, Snow = 4, Lava = 5, Mushroom = 6 }

   // All the valid biomes in the game
   public static Type[] gameBiomes = new Type[] { Type.Forest, Type.Desert, Type.Pine, Type.Snow, Type.Lava, Type.Mushroom };

   // Default Biome Names
   public const string FOREST = "Tropical Shores";
   public const string DESERT = "Desert Isles";
   public const string PINE = "Misty Forests";
   public const string SNOW = "Snowy Peaks";
   public const string LAVA = "Lava Lands";
   public const string MUSHROOM = "Shroom Seas";

   #endregion

   public static string getName (Type biomeType) {
      switch (biomeType) {
         case Type.Forest: return FOREST;
         case Type.Desert: return DESERT;
         case Type.Pine: return PINE;
         case Type.Snow: return SNOW;
         case Type.Lava: return LAVA;
         case Type.Mushroom: return MUSHROOM;
         default: return "";
      }
   }

   public static Type fromName (string name) {
      if (Util.isEmpty(name) || Util.areStringsEqual(name, "Unknown")) {
         return Type.None;
      }

      if (Util.areStringsEqual(name, "Forest") || Util.areStringsEqual(name, FOREST)) {
         return Type.Forest;
      }

      if (Util.areStringsEqual(name, "Desert") || Util.areStringsEqual(name, DESERT)) {
         return Type.Desert;
      }

      if (Util.areStringsEqual(name, "Pine") || Util.areStringsEqual(name, PINE)) {
         return Type.Pine;
      }

      if (Util.areStringsEqual(name, "Snow") || Util.areStringsEqual(name, SNOW)) {
         return Type.Snow;
      }

      if (Util.areStringsEqual(name, "Lava") || Util.areStringsEqual(name, LAVA)) {
         return Type.Lava;
      }

      if (Util.areStringsEqual(name, "Mushroom") || Util.areStringsEqual(name, MUSHROOM)) {
         return Type.Mushroom;
      }

      return Type.None;
   }

   public static List<Type> getAllTypes () {
      List<Type> list = Util.getAllEnumValues<Type>();
      list.Remove(Type.None);
      return list;
   }

   public static Type getNextBiome (Type currentBiome) {
      int nextBiomeInt = ((int) currentBiome) + 1;
      if (Enum.IsDefined(typeof(Type), nextBiomeInt)) {
         return (Type) nextBiomeInt;
      } else {
         return currentBiome;
      }
   }

   #region Private Variables

   #endregion
}
