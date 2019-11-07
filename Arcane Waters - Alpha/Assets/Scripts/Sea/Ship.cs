using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Ship : SeaEntity {
   #region Public Variables

   // If our supplies reach this amount, our crew abandons ship and we die
   public static int SUPPLIES_STARVE_COUNT = -3;

   // The Type of Ship
   public enum Type { Caravel = 100, Brigantine = 101, Carrack = 102, Nao = 103, Buss = 104, Galleon = 105, Cutter = 106, Barge = 107,  }

   // The Type of Skin
   public enum SkinType {
      None = 0,
      Caravel_Fancy = 100, Caravel_Armored = 101, Caravel_Ghost = 102, Caravel_Green = 103, Caravel_Ice = 104,
      Caravel_Marlin = 105, Caravel_Oak = 106, Caravel_Oceanic = 107, Caravel_Orange = 108, Caravel_Painted = 109,
      Caravel_Pale = 110, Caravel_Pink = 111, Caravel_Purple = 112, Caravel_Shadow = 113, Caravel_Snow = 114,
      Caravel_Sunset = 115, Caravel_Teal = 116, Caravel_Tiger = 117, Caravel_Tropical = 118, Caravel_Zebra = 119,

      Brigantine_Blue = 200, Brigantine_Emerald = 201, Brigantine_Orange = 202, Brigantine_Pale = 203, Brigantine_Royal = 204,

      Nao_Armored = 300, Nao_Crimson = 301, Nao_Dragon = 302, Nao_Emerald = 303, Nao_Fancy = 304, Nao_Green = 305,
      Nao_Grey = 306, Nao_Ice = 307, Nao_Lagoon = 308, Nao_Magma = 309, Nao_Oak = 310, Nao_Oceanic = 311,
      Nao_Pine = 312, Nao_Pink = 313, Nao_Purple = 314, Nao_Royal = 315, Nao_Tiger = 316, Nao_War = 317,
      Nao_Zebra = 318,

      Carrack_Green = 400, Carrack_Harbringer = 401, Carrack_Oak = 402, Carrack_Pale = 403, Carrack_Pink = 404,
      Carrack_Purple = 405,

      Cutter_Dark = 500, Cutter_Dragon = 501, Cutter_Orange = 502, Cutter_Purple = 503, Cutter_Toxic = 504,

      Buss_Blue = 600, Buss_Green = 601, Buss_Marlin = 602, Buss_Orange = 603, Buss_Purple = 604,

      Galleon_Armored = 700, Galleon_Dark = 701, Galleon_Frost = 702, Galleon_Light = 703,

      Barge_Dragon = 800, Barge_Regal = 801, Barge_Royal = 802, Barge_Tropical = 803, Barge_Warlord = 804,
   }

   // Our ship ID
   public int shipId = -1;

   // The Type of Ship
   [SyncVar]
   public Type shipType;

   // The custom name of this Ship
   public string shipName;

   // How many supplies this ship currently has
   public int currentSupplies = -1;
   public int suppliesMax = -1;

   // The Type of Mast
   public enum MastType { Caravel_1 = 100, Carrack_1 = 101, Galleon_1 = 102 }

   // The Type of Sail
   public enum SailType { Caravel_1 = 100, Carrack_1 = 101, Galleon_1 = 102 }

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   protected override void Start () {
      base.Start();
      // Set our name to something meaningful
      this.name = "Ship - " + this.shipType + " (user: " + this.userId + ")";
   }

   public static int getBaseDamage (Ship.Type shipType) {
      switch (shipType) {
         case Type.Caravel:
            return 10;
         case Type.Brigantine:
            return 25;
         case Type.Nao:
            return 35;
         case Type.Carrack:
            return 50;
         case Type.Cutter:
            return 70;
         case Type.Buss:
            return 90;
         case Type.Galleon:
            return 150;
         case Type.Barge:
            return 200;
         default:
            return 100;
      }
   }

   public static int getBaseAttackRange (Ship.Type shipType) {
      switch (shipType) {
         case Type.Caravel:
            return 100;
         case Type.Brigantine:
            return 105;
         case Type.Nao:
            return 110;
         case Type.Carrack:
            return 115;
         case Type.Cutter:
            return 120;
         case Type.Buss:
            return 125;
         case Type.Galleon:
            return 130;
         case Type.Barge:
            return 135;
         default:
            return 100;
      }
   }

   public static int getBaseHealth (Ship.Type shipType) {
      switch (shipType) {
         case Type.Caravel:
            return 100;
         case Type.Brigantine:
            return 150;
         case Type.Nao:
            return 200;
         case Type.Carrack:
            return 350;
         case Type.Cutter:
            return 275;
         case Type.Buss:
            return 450;
         case Type.Galleon:
            return 800;
         case Type.Barge:
            return 1500;
         default:
            return 100;
      }
   }

   public static int getBaseSailors (Ship.Type shipType) {
      switch (shipType) {
         case Type.Caravel:
            return 10;
         case Type.Brigantine:
            return 12;
         case Type.Nao:
            return 25;
         case Type.Carrack:
            return 35;
         case Type.Cutter:
            return 45;
         case Type.Buss:
            return 100;
         case Type.Galleon:
            return 120;
         case Type.Barge:
            return 250;
         default:
            return 100;
      }
   }

   public static int getBaseSuppliesRoom (Ship.Type shipType) {
      switch (shipType) {
         case Type.Cutter:
            return getBaseSailors(shipType) * 15;
         case Type.Carrack:
            return getBaseSailors(shipType) * 20;
         case Type.Buss:
            return getBaseSailors(shipType) * 8;
         case Type.Galleon:
            return getBaseSailors(shipType) * 7;
         case Type.Barge:
            return getBaseSailors(shipType) * 6;
         default:
            return getBaseSailors(shipType) * 10;
      }
   }

   public static int getBaseSpeed (Ship.Type shipType) {
      switch (shipType) {
         case Type.Caravel:
            return 90;
         case Type.Brigantine:
            return 100;
         case Type.Nao:
            return 110;
         case Type.Carrack:
            return 95;
         case Type.Cutter:
            return 115;
         case Type.Buss:
            return 85;
         case Type.Galleon:
            return 80;
         case Type.Barge:
            return 75;
         default:
            return 100;
      }
   }

   public static int getBaseCargoRoom (Ship.Type shipType) {
      switch (shipType) {
         case Type.Caravel:
            return 18;
         case Type.Brigantine:
            return 22;
         case Type.Nao:
            return 25;
         case Type.Carrack:
            return 30;
         case Type.Cutter:
            return 30;
         case Type.Buss:
            return 30;
         case Type.Galleon:
            return 30;
         case Type.Barge:
            return 30;
         default:
            return 30;
      }
   }

   public static int getBasePrice (Ship.Type shipType) {
      switch (shipType) {
         case Type.Caravel:
            return 5000;
         case Type.Brigantine:
            return 15000;
         case Type.Nao:
            return 25000;
         case Type.Carrack:
            return 100000;
         case Type.Cutter:
            return 150000;
         case Type.Buss:
            return 250000;
         case Type.Galleon:
            return 400000;
         case Type.Barge:
            return 600000;
         default:
            return 100000;
      }
   }

   public static string getSkinPath (Ship.Type shipType, Ship.SkinType skinType=SkinType.None) {
      string skinName = (skinType == Ship.SkinType.None) ? shipType + "_1" : skinType + "";
      string basePath = "Ships/" + shipType + "/";
      string skinPath = basePath + skinName;

      return skinPath;
   }

   public static string getRipplesPath (Ship.Type shipType) {
      string basePath = "Ships/" + shipType + "/";

      return basePath + shipType + "_ripples";
   }

   #region Private Variables

   #endregion
}
