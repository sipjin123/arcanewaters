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

   // The weather spawn center point
   public Transform weatherSpawnRight, weatherSpawnLeft;

   // Parent of the spawn points
   public Transform attackersSpotHolder, defendersSpotHolder;

   // The xml id reference 
   public int xmlID;

   // If weather is active in this background
   public bool isWeatherActive;

   // The weather effect type
   public WeatherEffectType weatherEffectType;

   // The cloud object prefab
   public CloudObject cloudObjectPrefab;

   // List of cloud objects
   public List<CloudObject> cloudObjList;

   // The max horizontal position
   public const float maxRightPos = 3.5f, maxLeftPos = -3.5f;

   // The max vertical position
   public const float maxUpPos = .4f, maxDownPos = -.2f;

   // The object reference for the particle based weather system
   public GameObject rainObjectHolder, snowObjectHolder;

   // The z axis position where the player battler should snap in order to render between key battle positions
   public static float PLAYER_BATTLE_Z_POS = -.12f;

   // Reference to the ship battle background elements holder
   public Transform shipBattleBackgoundHolder;

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

   public void setWeather (WeatherEffectType weather, Biome.Type biomeType) {
      isWeatherActive = true;
      weatherEffectType = weather;
      weatherSpawnRight.gameObject.DestroyChildren();
      weatherSpawnLeft.gameObject.DestroyChildren();
      rainObjectHolder.SetActive(false);
      snowObjectHolder.SetActive(false);

      int randomCloudCount = Random.Range(3, 6);
      int randomDirection = Random.Range(1, 3);
      cloudObjList = new List<CloudObject>();

      switch (weather) {
         case WeatherEffectType.Cloud:
         case WeatherEffectType.DarkCloud:
            for (int i = 0; i < randomCloudCount; i++) {
               Transform parentObj = randomDirection == 1 ? weatherSpawnRight : weatherSpawnLeft;
               float randomYPosition = Random.Range(maxUpPos, maxDownPos);
               float randomXPosition = Random.Range(maxRightPos, maxLeftPos);
               Vector3 newPosition = new Vector3(parentObj.position.x + randomXPosition, parentObj.position.y + randomYPosition, weatherSpawnRight.position.z);
               Vector3 newCenterPosition = new Vector3(centerPoint.transform.position.x, parentObj.position.y, parentObj.position.z);

               CloudObject cloudObj = Instantiate(cloudObjectPrefab, parentObj);
               cloudObj.direction = randomDirection == 1 ? Direction.West : Direction.East;
               cloudObj.resetObject(weatherEffectType, cloudObj.direction, newPosition, newCenterPosition, true, biomeType);
              
               cloudObjList.Add(cloudObj);
            }
            break;
         case WeatherEffectType.Rain:
            rainObjectHolder.SetActive(true);
            break;
         case WeatherEffectType.Snow:
            snowObjectHolder.SetActive(true);
            break;
      }
   }

   private void Update () {
      // Skip update for server in batch mode
      if (Util.isBatch()) {
         return;
      }

      if (isWeatherActive) {
         switch (weatherEffectType) {
            case WeatherEffectType.Cloud:
            case WeatherEffectType.DarkCloud:
               foreach(CloudObject cloudObj in cloudObjList) {
                  cloudObj.move();
               }
               break;
         }
      }
   }

   public void recalibrateBattleSpots (List<GameObject> defenderSpots, List<GameObject> attackerSpots, int newXmlId) {
      xmlID = newXmlId;
      int boardIndex = 0;

      foreach (GameObject attackerSpot in attackerSpots) {
         // If the attacker battle spot objects are equal or less than the spots assigned in the battle xml, proceed with the position recalibration
         if (attackersSpotHolder.childCount <= attackerSpots.Count) {
            Transform attackSpot = attackersSpotHolder.GetChild(boardIndex);
            attackSpot.transform.position = attackerSpot.transform.position;
            attackSpot.transform.localPosition = new Vector3(attackSpot.transform.localPosition.x, attackSpot.transform.localPosition.y, PLAYER_BATTLE_Z_POS);
         } else {
            break;
         }
         boardIndex++;
      }

      boardIndex = 0;
      foreach (GameObject defendersSpot in defenderSpots) {
         // If the defender battle spot objects are equal or less than the spots assigned in the battle xml, proceed with the position recalibration
         if (defendersSpotHolder.childCount <= defenderSpots.Count) {
            Transform defenderSpot = defendersSpotHolder.GetChild(boardIndex);
            defenderSpot.transform.position = defendersSpot.transform.position;
            defenderSpot.transform.localPosition = new Vector3(defenderSpot.transform.localPosition.x, defenderSpot.transform.localPosition.y, PLAYER_BATTLE_Z_POS);
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

   public void toggleShipBattleBackgroundElements(bool show) {
      if (shipBattleBackgoundHolder == null) {
         return;
      }

      shipBattleBackgoundHolder.gameObject.SetActive(show);

      if (centerPoint == null) {
         return;
      }

      centerPoint.gameObject.SetActive(!show);
   }

   #region Private Variables

   // Stores a list of Battle Spots for this Battle Board
   [SerializeField] protected List<BattleSpot> _spots = new List<BattleSpot>();

   #endregion
}
