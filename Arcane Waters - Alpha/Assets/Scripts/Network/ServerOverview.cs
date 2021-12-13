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

   // Information about all instances
   public List<InstanceOverview> instances = new List<InstanceOverview>();

   #endregion

   #region Private Variables

   #endregion
}
