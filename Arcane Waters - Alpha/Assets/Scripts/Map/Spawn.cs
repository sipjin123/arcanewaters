using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Spawn : MonoBehaviour {
   #region Public Variables

   // The Type of Spawn this is
   public enum Type { None = 0,
      StartingLocation = 1, HouseEntrance = 2, Farm = 3, HouseExit = 4, ForestTownTop = 5,
      ForestTownDock = 6, DesertTownDock = 7, DesertTownMerchantOutside = 8, SeaTopLower = 9, SeaMiddleDesertTownExit = 10,
      SeaMiddleLower = 11, SeaMiddleUpper = 12, SeaBottomForestTownExit = 13, SeaBottomUpper = 14, DesertMerchantInside = 15,
      TreasurePine = 16, OutsideTreasureSite = 17, ForestMerchantInside = 18, ForestTownMerchantOutside = 19,
      ForestTownWeaponsOutside = 20, ForestWeaponsInside = 21, DesertWeaponsInside = 22, DesertWeaponsOutside = 23,
      ForestShipyardOutside = 24, ForestShipyardInside = 25,
   }

   // The Type of Spawn this is
   public Type spawnType;

   // Convenient area type property
   public Area.Type AreaType {
      get { return _areaType; }
   }

   #endregion

   void Start () {
      // Note the Area Type that we're in
      _areaType = GetComponentInParent<Area>().areaType;

      // If a spawn box was specified in the Editor, look it up now
      _spawnBox = GetComponent<BoxCollider2D>();

      // Keep track of this spawn
      SpawnManager.self.store(this.spawnType, this);
   }

   public Vector3 getSpawnPosition () {
      // Some spawn types specify a box that objects can spawn within
      return (_spawnBox == null) ? this.transform.position : Util.RandomPointInBounds(_spawnBox.bounds);

      
   }

   protected Area.Type getAreaType () {
      return _areaType;
   }

   #region Private Variables

   // The Area Type this spawn is in
   protected Area.Type _areaType;

   // The area in which objects will be spawned, which can be optionally included in the scene
   protected BoxCollider2D _spawnBox;

   #endregion
}
