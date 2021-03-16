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

   // The text component of the line
   public Text eventLine;
   
   #endregion

   public void setRowForServerEvent (ServerHistoryInfo info) {
      DateTime localTime = DateTime.FromBinary(info.eventDate).ToLocalTime(); 

      switch (info.eventType) {
         case ServerHistoryInfo.EventType.ServerStart:
            eventLine.text = "Started up at " + localTime.ToString("hh:mm tt") + " (v" + info.serverVersion.ToString() + ")";
            break;
         case ServerHistoryInfo.EventType.ServerStop:
            eventLine.text = "Shut down at " + localTime.ToString("hh:mm tt") + " (v" + info.serverVersion.ToString() + ")";
            break;
         default:
            break;
      }
   }

   #region Private Variables

   #endregion
}
