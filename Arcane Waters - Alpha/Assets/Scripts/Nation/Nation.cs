using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Nation : MonoBehaviour {
   #region Public Variables

   public enum Type {  None = 0, Neutral = 1, Pirate = 2, Noble = 3, Trader = 4, Explorer = 5 }

   #endregion

   public static ColorType getColor1 (Type nationType) {
      switch (nationType) {
         case Type.Neutral:
            return ColorType.FlagWhite;
         case Type.Pirate:
            return ColorType.FlagBlack;
         case Type.Noble:
            return ColorType.FlagBlue;
         case Type.Trader:
            return ColorType.FlagYellow;
         case Type.Explorer:
            return ColorType.DarkGreen;
         default:
            return ColorType.FlagWhite;
      }
   }

   public static ColorType getColor2 (Type nationType) {
      return ColorType.PureBlack;

      /*switch (nationType) {
         case Type.Neutral:
            return ColorType.FlagBrown2;
         case Type.Pirate:
            return ColorType.White;
         case Type.Noble:
            return ColorType.White;
         case Type.Trader:
            return ColorType.FlagBrown2;
         case Type.Explorer:
            return ColorType.White;
         default:
            return ColorType.FlagBrown2;
      }*/
   }

   public static string getName (Type nationType) {
      switch (nationType) {
         case Type.Neutral:
            return "Neutral";
         case Type.Pirate:
            return "Pirate";
         case Type.Noble:
            return "Noble";
         case Type.Trader:
            return "Trader";
         case Type.Explorer:
            return "Explorer";
         default:
            return "Unknown";
      }
   }

   #region Private Variables

   #endregion
}
