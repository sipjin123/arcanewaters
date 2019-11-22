using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class TradeHistoryRow : MonoBehaviour
{
   #region Public Variables

   // The trade ID associated with this row
   public int tradeId;

   // The name of the area where the trade took place
   public Text areaName;

   // The crop icon
   public Image cropImage;

   // The crop text
   public Text cropName;

   // The gold amount
   public Text goldAmount;

   // The experience gained with the trade
   public Text experienceAmount;

   // The time at which the trade took place
   public Text tradeTime;

   // The tooltip of the trade time
   public Tooltipped tradeTimeTooltip;

   #endregion

   public void setRowForItem (TradeHistoryInfo trade) {
      tradeId = trade.tradeId;
      areaName.text = Area.getName(trade.areaKey);
      cropImage.sprite = ImageManager.getSprite("Cargo/" + trade.cargoType);
      cropName.text = Util.UppercaseFirst(trade.cargoType.ToString());
      goldAmount.text = trade.totalPrice + "";
      experienceAmount.text = trade.totalXP + "";

      // Calculates the time since the trade took place
      DateTime tradeDateTime = DateTime.FromBinary(trade.tradeTime);
      TimeSpan timeInterval = DateTime.UtcNow.Subtract(tradeDateTime);

      // Show the seconds if it happened in the last 60s, the
      // minutes in the last 60m, etc, up to the days.
      if (timeInterval.TotalSeconds <= 60) {
         tradeTime.text = timeInterval.Seconds.ToString() + " s";
      } else if (timeInterval.TotalMinutes <= 60) {
         tradeTime.text = timeInterval.Minutes.ToString() + " min";
      } else if (timeInterval.TotalHours <= 24) {
         if (timeInterval.Hours <= 1) {
            tradeTime.text = timeInterval.Hours.ToString() + " hour";
         } else {
            tradeTime.text = timeInterval.Hours.ToString() + " hours";
         }
      } else {
         if (timeInterval.Days <= 1) {
            tradeTime.text = timeInterval.Days.ToString() + " day";
         } else {
            tradeTime.text = timeInterval.Days.ToString() + " days";
         }
      }

      // Sets the full date + time as a tooltip
      tradeTimeTooltip.text = tradeDateTime.ToLocalTime().ToString("dddd, dd MMMM yyyy hh:mm tt");
   }

   #region Private Variables

   #endregion
}
