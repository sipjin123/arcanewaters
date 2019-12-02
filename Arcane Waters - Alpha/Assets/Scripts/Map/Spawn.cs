using UnityEngine;
using MapCreationTool.Serialization;

public class Spawn : MonoBehaviour, MapCreationTool.IMapEditorDataReceiver {
   #region Public Variables

   // Hardcoded spawn keys
   public static string FOREST_TOWN_DOCK = "ForestTownDock";
   public static string HOUSE_EXIT = "HouseExit";

   // The key determining the type of spawn this is
   public string spawnKey;

   // Convenient area key property
   public string AreaKey {
      get { return _areaKey; }
   }

   #endregion

   void Awake () {
      // Note the Area Type that we're in
      _areaKey = GetComponentInParent<Area>().areaKey;

      // If a spawn box was specified in the Editor, look it up now
      _spawnBox = GetComponent<BoxCollider2D>();

      // Keep track of this spawn if it is defined
      if (!string.IsNullOrWhiteSpace(spawnKey)) {
         SpawnManager.get().store(_areaKey, spawnKey, this);
      }
   }

   public Vector3 getSpawnPosition () {
      // Some spawn types specify a box that objects can spawn within
      return (_spawnBox == null) ? this.transform.position : Util.RandomPointInBounds(_spawnBox.bounds);
   }

   protected string getAreaKey () {
      return _areaKey;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case "name":
               spawnKey = field.v.Trim(' ');
               SpawnManager.get().store(_areaKey, spawnKey, this);
               break;
            case "width":
               _spawnBox.size = new Vector2(float.Parse(field.v), _spawnBox.size.y);
               break;
            case "height":
               _spawnBox.size = new Vector2(_spawnBox.size.x, float.Parse(field.v));
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }

   #region Private Variables

   // The Area Key this spawn is in
   protected string _areaKey;

   // The area in which objects will be spawned, which can be optionally included in the scene
   protected BoxCollider2D _spawnBox;

   #endregion
}
