using UnityEngine;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using MapCreationTool;
using System.Linq;
using Mirror;

public class WorldMapDBManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static WorldMapDBManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initialize () {
      // Fetches the spots previously uploaded to the database, and stores them locally
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         setWorldMapSpots(DB_Main.fetchWorldMapSpots().ToList());
      });
   }


   public void setWorldMapSpots (List<WorldMapSpot> spots) {
      _worldMapSpots = spots;
   }

   public List<WorldMapSpot> getWorldMapSpots () {
      return _worldMapSpots;
   }

   #region Private Variables

   // Server cache for the world spots
   private List<WorldMapSpot> _worldMapSpots = new List<WorldMapSpot>();

   #endregion
}
