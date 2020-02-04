using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BattleBoard : MonoBehaviour {
   #region Public Variables

   // The Biome Type that this Battle Board is for
   public Biome.Type biomeType = Biome.Type.Pine;

   // Central spot where assets will be spawned
   public Transform centerPoint;

   #endregion

   public void Start () {
      _spots.Clear();

      // Look up the Battle Spots for this board
      foreach (BattleSpot spot in GetComponentsInChildren<BattleSpot>()) {
         _spots.Add(spot);
      }
   }

   public BattleSpot getSpot (Battle.TeamType teamType, int boardPosition) {
      foreach (BattleSpot spot in _spots) {
         if (spot.teamType == teamType && spot.boardPosition == boardPosition) {
            return spot;
         }
      }

      return null;
   }

   #region Private Variables

   // Stores a list of Battle Spots for this Battle Board
   protected List<BattleSpot> _spots = new List<BattleSpot>();

   #endregion
}
