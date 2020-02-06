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

   // Parent of the spawn points
   public Transform attackersSpotHolder, defendersSpotHolder;

   #endregion

   public void Start () {
      _spots.Clear();

      // Look up the Battle Spots for this board
      foreach (BattleSpot spot in GetComponentsInChildren<BattleSpot>()) {
         _spots.Add(spot);
      }
   }

   public void recalibrateBattleSpots (List<GameObject> defenderSpots, List<GameObject> attackerSpots) {
      _spots.Clear();
      attackersSpotHolder.gameObject.DestroyChildren();
      defendersSpotHolder.gameObject.DestroyChildren();

      int boardIndex = 1;
      foreach (GameObject attackerSpot in attackerSpots) {
         BattleSpot spot = attackerSpot.AddComponent<BattleSpot>();
         spot.transform.SetParent(attackersSpotHolder);
         spot.teamType = Battle.TeamType.Attackers;
         spot.boardPosition = boardIndex;
         _spots.Add(spot);
         boardIndex++;
      }

      boardIndex = 1;
      foreach (GameObject defendersSpot in defenderSpots) {
         BattleSpot spot = defendersSpot.AddComponent<BattleSpot>();
         spot.transform.SetParent(defendersSpotHolder);
         spot.teamType = Battle.TeamType.Defenders;
         spot.boardPosition = boardIndex;
         _spots.Add(spot);
         boardIndex++;
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
