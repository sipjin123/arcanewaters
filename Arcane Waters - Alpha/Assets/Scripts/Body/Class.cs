using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Class : MonoBehaviour {
   #region Public Variables

   // The Types of class
   public enum Type {
      None = 0, Fighter = 1, Marksman = 2, Mystic = 3, Healer = 4, TestEntry = 5
   }
      
   #endregion

   public static string getDescription (Type type) {
      PlayerClassData classData = ClassManager.self.getClassData(type);
      if (classData != null) {
         return classData.description;
      }

      // TODO: Confirm if these descriptions will be modified in the tool
      switch (type) {
         case Type.Fighter:
            return "Fighters are good with melee weapons such as swords and known for being tough.";
         case Type.Marksman:
            return "Marksmen are less durable but attack from a distance with pistols and rifles.";
         case Type.Mystic:
            return "Mystics study the arcane arts to conjure powerful magic.";
         case Type.Healer:
            return "Healers are noble adventurers who guard the health and safety of their companions.";
         default:
            return "";
      }
   }

   #region Private Variables
      
   #endregion
}
