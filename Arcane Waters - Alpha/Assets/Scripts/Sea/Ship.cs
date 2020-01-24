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
   public enum Type { None = 0, Caravel = 100, Brigantine = 101, Carrack = 102, Nao = 103, Buss = 104, Galleon = 105, Cutter = 106, Barge = 107, NewEntryTest1 = 108, NewEntryTest2 = 109 }

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
      return ShipDataManager.self.getShipData(shipType).baseDamage;
   }

   public static int getBaseAttackRange (Ship.Type shipType) {
      return ShipDataManager.self.getShipData(shipType).baseRange;
   }

   public static int getBaseHealth (Ship.Type shipType) {
      return ShipDataManager.self.getShipData(shipType).baseHealth;
   }

   public static int getBaseSailors (Ship.Type shipType) {
      return ShipDataManager.self.getShipData(shipType).baseSailors;
   }

   public static int getBaseSuppliesRoom (Ship.Type shipType) {
      return ShipDataManager.self.getShipData(shipType).baseSupplyRoom;
   }

   public static int getBaseSpeed (Ship.Type shipType) {
      return ShipDataManager.self.getShipData(shipType).baseSpeed;
   }

   public static int getBaseCargoRoom (Ship.Type shipType) {
      return ShipDataManager.self.getShipData(shipType).baseCargoRoom;
   }

   public static int getBasePrice (Ship.Type shipType) {
      return ShipDataManager.self.getShipData(shipType).basePrice;
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
}
