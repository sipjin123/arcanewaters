using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class TradeHistoryInfo
{
   #region Public Variables

   // The trade ID from the database
   public int tradeId;

   // The user ID that made this trade
   public int userId;

   // The ship ID
   public int shipId;

   // The area where the trade was made
   public string areaKey;

   // The cargo type that was traded (= crop type)
   public Crop.Type cargoType;

   // The amount traded
   public int amount;

   // The price per unit sold
   public int pricePerUnit;

   // The total price
   public int totalPrice;

   // The experience per unit sold
   public int xpPerUnit;

   // The total experience gained by the trade
   public int totalXP;

   // The time at which the trade took place
   public long tradeTime;

   #endregion

   public TradeHistoryInfo () { }

#if IS_SERVER_BUILD

   public TradeHistoryInfo (MySqlDataReader dataReader) {
      this.tradeId = DataUtil.getInt(dataReader, "traId");
      this.userId = DataUtil.getInt(dataReader, "usrId");
      this.shipId = DataUtil.getInt(dataReader, "shpId");
      this.areaKey = DataUtil.getString(dataReader, "areaKey");
      this.cargoType = (Crop.Type) DataUtil.getInt(dataReader, "crgType");
      this.amount = DataUtil.getInt(dataReader, "amount");
      this.pricePerUnit = DataUtil.getInt(dataReader, "unitPrice");
      this.totalPrice = DataUtil.getInt(dataReader, "totalPrice");
      this.xpPerUnit = DataUtil.getInt(dataReader, "unitXP");
      this.totalXP = DataUtil.getInt(dataReader, "totalXP");
      this.tradeTime = DataUtil.getDateTime(dataReader, "tradeTime").ToBinary();
   }

#endif

   public TradeHistoryInfo (int userId, int shipId, string areaKey, Crop.Type cargoType, int amount, int pricePerUnit,
      int totalPrice, int xpPerUnit, int totalXP, DateTime tradeTime) {

      this.userId = userId;
      this.shipId = shipId;
      this.areaKey = areaKey;
      this.cargoType = cargoType;
      this.amount = amount;
      this.pricePerUnit = pricePerUnit;
      this.totalPrice = totalPrice;
      this.xpPerUnit = xpPerUnit;
      this.totalXP = totalXP;
      this.tradeTime = tradeTime.ToBinary();
   }

   public override bool Equals (object rhs) {
      if (rhs is TradeHistoryInfo) {
         var other = rhs as TradeHistoryInfo;
         return tradeId == other.tradeId;
      }
      return false;
   }

   public override int GetHashCode () {
      return tradeId.GetHashCode();
   }

   #region Private Variables

   #endregion
}
