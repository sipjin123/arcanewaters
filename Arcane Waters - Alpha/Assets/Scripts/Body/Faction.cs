using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Faction : MonoBehaviour {
   #region Public Variables

   // The Faction types
   public enum Type {
      None = 0, Neutral = 1, Pirates = 2, Privateers = 3, Pillagers = 4, Cartographers = 5,
      Merchants = 6, Builders = 7, Naturalists = 8, TestEntry = 9
   }

   #endregion

   public static string toString (Type type) {
      switch (type) {
         case Type.Pirates:
            return "Skull and Bones";
         default:
            return type + "";
      }
   }

   public static string getDescription (Type type) {
      PlayerFactionData factionData = FactionManager.self.getFactionData(type);
      if (factionData != null) {
         return factionData.description;
      }

      // TODO: Confirm if these descriptions will be modified in the tool
      switch (type) {
         case Type.Neutral:
            return "Neutral characters are safe from other players but receive no bonuses.";
         case Type.Pirates:
            return "A loose collection of thieves and scoundrels who have little patience for rules or authority.";
         case Type.Privateers:
            return "Noble peacekeepers who seek to maintain order by hunting down pirates.";
         case Type.Pillagers:
            return "A group of treasure hunters not afraid to fight their way through anything.";
         case Type.Cartographers:
            return "An association of mapmakers who seek to explore the furthest unknown areas.";
         case Type.Merchants:
            return "Traders whose singular focus is making the largest profit from their precious cargo.";
         case Type.Builders:
            return "Peaceful builders who seek to make the highest quality items they can.";
         case Type.Naturalists:
            return "Green thumbs who enjoy tending their crops and appreciating nature.";
         default:
            return "";
      }
   }

   public static Sprite getIcon (Type type) {
      return ImageManager.getSprite("Icons/Factions/faction_" + type);
   }

   public static Sprite getShipIcon (Type type) {
      return ImageManager.getSprite("Icons/Factions/Ships/faction_ship_" + type);
   }

   #region Private Variables

   #endregion
}
