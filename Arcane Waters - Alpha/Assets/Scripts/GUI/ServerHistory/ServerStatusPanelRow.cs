using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;

public class ServerStatusPanelRow : MonoBehaviour
{
   #region Public Variables

   // The event time
   public Text timeField;

   // The event description
   public Text descriptionField;

   // The server version
   public Text serverVersionField;

   // The event icon
   public Image iconImage;

   // The sprites used for specific events
   public Sprite serverStartedUpSprite;
   public Sprite serverShutDownSprite;
   public Sprite serverRestartRequestedSprite;
   public Sprite serverRestartCanceledSprite;

   // The tooltip displaying the event date
   public ToolTipComponent timeTooltip;

   #endregion

   public void setRowForServerEvent (ServerHistoryInfo info) {
      // Calculates the time since the event took place
      DateTime utcTime = DateTime.FromBinary(info.eventDate);
      DateTime estTime = Util.getTimeInEST(utcTime);
      TimeSpan timeInterval = DateTime.UtcNow.Subtract(utcTime);

      // Show the seconds if it happened in the last 60s, the
      // minutes in the last 60m, etc, up to the days.
      if (timeInterval.TotalMinutes <= 60) {
         timeField.text = timeInterval.Minutes.ToString() + "m";
      } else {
         if (timeInterval.Minutes < 10) {
            timeField.text = timeInterval.Hours.ToString() + "h 0" + timeInterval.Minutes.ToString() + "m";
         } else {
            timeField.text = timeInterval.Hours.ToString() + "h" + timeInterval.Minutes.ToString() + "m";
         }
      }

      switch (info.eventType) {
         case ServerHistoryInfo.EventType.ServerStart:
            descriptionField.text = "Started Up";
            iconImage.sprite = serverStartedUpSprite;
            break;
         case ServerHistoryInfo.EventType.ServerStop:
            descriptionField.text = "Shut Down";
            iconImage.sprite = serverShutDownSprite;
            break;
         case ServerHistoryInfo.EventType.RestartRequested:
            descriptionField.text = "Restart Requested";
            iconImage.sprite = serverRestartRequestedSprite;
            break;
         case ServerHistoryInfo.EventType.RestartCanceled:
            descriptionField.text = "Restart Canceled";
            iconImage.sprite = serverRestartCanceledSprite;
            break;
         default:
            iconImage.enabled = false;
            break;
      }

      serverVersionField.text = info.serverVersion.ToString();
      timeTooltip.message = estTime.ToString() + " EST";
   }

   #region Private Variables

   #endregion
}
