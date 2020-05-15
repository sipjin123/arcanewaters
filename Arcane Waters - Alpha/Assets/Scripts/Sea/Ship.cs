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
   public enum Type { None = 0, Type_1 = 100, Type_2 = 101, Type_3 = 102, Type_4 = 103, Type_5 = 104, Type_6 = 105, Type_7 = 106, Type_8 = 107 }

   // The Type of Skin
   public enum SkinType {
      None = 0,
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
   public enum MastType { Type_1 = 100 }

   // The Type of Sail
   public enum SailType { Type_1 = 100 }

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

   public static string getSkinPath (Ship.Type shipType, Ship.SkinType skinType = SkinType.None, bool isPirate = false) {
      string skinName;
      if (!isPirate) {
         skinName = (skinType == Ship.SkinType.None) ? shipType + "_1" : skinType + "";
      } else {
         skinName = shipType + "_pirate";
      }
      string basePath = "Ships/" + shipType + "/";
      string skinPath = basePath + skinName;

      return skinPath;
   }

   public static string getRipplesPath (Ship.Type shipType) {
      string basePath = "Ships/" + shipType + "/";

      return basePath + shipType + "_ripples";
   }

   public static string getRipplesMovingPath (Ship.Type shipType) {
      string basePath = "Ships/" + shipType + "/";

      return basePath + shipType + "_ripples";
   }
}
