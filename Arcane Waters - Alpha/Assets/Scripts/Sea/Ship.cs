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

   #region Get stat by type

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

   #endregion

   #region Get stat by id

   public static int getBaseDamage (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).baseDamage;
   }

   public static int getBaseAttackRange (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).baseRange;
   }

   public static int getBaseHealth (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).baseHealth;
   }

   public static int getBaseSailors (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).baseSailors;
   }

   public static int getBaseSuppliesRoom (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).baseSupplyRoom;
   }

   public static int getBaseSpeed (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).baseSpeed;
   }

   public static int getBaseCargoRoom (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).baseCargoRoom;
   }

   public static int getBasePrice (int xmlId) {
      return ShipDataManager.self.getShipData(xmlId).basePrice;
   }

   #endregion

   public static string getSkinPath (Ship.Type shipType, Ship.SkinType skinType = SkinType.None, bool isPirate = false) {
      string skinName;
      if (!isPirate) {
         skinName = (skinType == Ship.SkinType.None) ? shipType + "_1":  skinType + "";
      } else {
         skinName = shipType + "_pirate";
      }
      
      string basePath = "Ships/"; 
      string skinPath = basePath + skinName;
      return skinPath;
   }

   public static string getRipplesPath (Ship.Type shipType) {
      string basePath = "Ships/";
      return basePath + shipType + "_ripples";
   }

   public static string getRipplesMovingPath (Ship.Type shipType) {
      string basePath = "Ships/";
      return basePath + shipType + "_ripples";
   }

   public static string getDisplayName (Type shipType) {
      switch (shipType) {
         case Type.None:
            return "None";
         case Type.Type_1:
            return "Caravel";
         case Type.Type_2:
            return "Brigantine";
         case Type.Type_3:
            return "Nao";
         case Type.Type_4:
            return "Carrack";
         case Type.Type_5:
            return "Cutter";
         case Type.Type_6:
            return "Galleon";
         case Type.Type_7:
            return "Buss";
         case Type.Type_8:
            return "Barge";
         default:
            return "none";
      }
   }

   public static ShipInfo generateNewShip (Ship.Type shipType, Rarity.Type rarity) {
      ShipData fetchedShipData = ShipDataManager.self.getShipData(shipType);
      int sailors = Random.Range(fetchedShipData.baseSailorsMin, fetchedShipData.baseSailorsMax);
      int suppliesRoom = Random.Range(fetchedShipData.baseSupplyRoomMin, fetchedShipData.baseSupplyRoomMax);
      int cargoRoom = Random.Range(fetchedShipData.baseCargoRoomMin, fetchedShipData.baseCargoRoomMax);
      int damage = Random.Range(fetchedShipData.baseDamageMin, fetchedShipData.baseDamageMax);
      int health = Random.Range(fetchedShipData.baseHealthMin, fetchedShipData.baseHealthMax);
      int price = getBasePrice(shipType);
      int attackRange = Random.Range(fetchedShipData.baseRangeMin, fetchedShipData.baseRangeMax);
      int speed = Random.Range(fetchedShipData.baseSpeedMin, fetchedShipData.baseSpeedMax);
      speed = Mathf.Clamp(speed, 70, 130);

      // Let's use nice numbers
      sailors = Util.roundToPrettyNumber(sailors);
      suppliesRoom = Util.roundToPrettyNumber(suppliesRoom);
      cargoRoom = Util.roundToPrettyNumber(cargoRoom);
      damage = Util.roundToPrettyNumber(damage);
      health = Util.roundToPrettyNumber(health);
      price = Util.roundToPrettyNumber(price);
      attackRange = Util.roundToPrettyNumber(attackRange);

      ShipInfo ship = new ShipInfo(-1, 0, shipType, SkinType.None, MastType.Type_1, SailType.Type_1, getDisplayName(shipType),
         "", "", "", "", suppliesRoom, suppliesRoom, cargoRoom, health, health, damage, attackRange, speed, sailors, rarity, new ShipAbilityInfo(true));
      ship.price = price;

      return ship;
   }
}
