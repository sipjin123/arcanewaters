using System;
using System.Collections.Generic;

[Serializable]
public class PvpAnnouncementClass {
   // The state of the pvp session
   public PvpGame.State pvpState;

   // The pvp id
   public int pvpId;

   // The instance id this pvp belongs to
   public int instanceId;

   // Last time the announcement has been triggered
   public DateTime lastAnnouncementTime;

   // The timestamp per player that receives the announcement
   public Dictionary<int, DateTime> playerTimeStamp = new Dictionary<int, DateTime>();
}