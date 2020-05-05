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

   // The xml id reference 
   public int xmlID;

   #endregion

   public void Start () {
      _spots.Clear();

      // Look up the Battle Spots for this board
      foreach (BattleSpot spot in GetComponentsInChildren<BattleSpot>()) {
         _spots.Add(spot);
      }

      // The layers between the battlers and the midground elements
      int layerOffset = 2;

      // Adjust z axis to battle manager position to interact with battler sprites
      centerPoint.transform.position = new Vector3(centerPoint.transform.position.x, centerPoint.transform.position.y, BattleManager.self.transform.position.z + layerOffset);
   }

   public void recalibrateBattleSpots (List<GameObject> defenderSpots, List<GameObject> attackerSpots, int newXmlId) {
      xmlID = newXmlId;
      int boardIndex = 0;
      foreach (GameObject attackerSpot in attackerSpots) {
         if (attackersSpotHolder.childCount < boardIndex) {
            Transform attackSpot = attackersSpotHolder.GetChild(boardIndex);
            attackSpot.transform.position = attackerSpot.transform.position;
         } else {
            break;
         }
         boardIndex++;
      }

      boardIndex = 0;
      foreach (GameObject defendersSpot in defenderSpots) {
         if (defendersSpotHolder.childCount < boardIndex) {
            Transform defenderSpot = defendersSpotHolder.GetChild(boardIndex);
            defenderSpot.transform.position = defendersSpot.transform.position;
         } else {
            break;
         }
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
   [SerializeField] protected List<BattleSpot> _spots = new List<BattleSpot>();

   #endregion
}
