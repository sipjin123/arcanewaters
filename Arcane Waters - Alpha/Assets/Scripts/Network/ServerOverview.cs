using UnityEngine;
using System.Collections.Generic;

public class ServerOverview
{
   #region Public Variables

   // Network id of the server this data is about
   public ulong serverNetworkId = 0;

   // Network port of this server
   public int port = 0;

   // Name of the machine that is running this server
   public string machineName = "";

   // Name of this process
   public string processName = "";

   // Frames per second
   public int fps = 0;

   // Ping to master
   public long toMasterRTT = 0;

   // How many players are in this server
   public int playerCount = 0;

   // How many instances are in this server
   public int instanceCount;

   // Uptime in seconds
   public double uptime;

   #endregion

   #region Private Variables

   #endregion
}
