using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Nation : MonoBehaviour {
   #region Public Variables

   public enum Type {  None = 0, Neutral = 1, Pirate = 2, Noble = 3, Trader = 4, Explorer = 5 }

   #endregion

   public static string getPalette1 (Type nationType) {
      switch (nationType) {
         case Type.Neutral:
            return PaletteDef.Flag.White;
         case Type.Pirate:
            return PaletteDef.Flag.Black;
         case Type.Noble:
            return PaletteDef.Flag.Blue;
         case Type.Trader:
            return PaletteDef.Flag.Yellow;
         case Type.Explorer:
            return PaletteDef.Flag.DarkGreen;
         default:
            return PaletteDef.Flag.White;
      }
   }

   public static string getPalette2 (Type nationType) {
      return PaletteDef.Flag.PureBlack;
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
