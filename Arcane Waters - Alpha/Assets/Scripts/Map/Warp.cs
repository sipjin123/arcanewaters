using UnityEngine;
using MapCreationTool;
using MapCreationTool.Serialization;

public class Warp : MonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

   // Hard coded quest index
   public const int GET_DRESSED_QUEST_INDEX = 1;
   public const int HEAD_TO_DOCKS_QUEST_INDEX = 8;
   public const int ENTER_TREASURE_SITE_QUEST_INDEX = 14;

   #endregion

   void Awake() {
      _collider = GetComponent<BoxCollider2D>();
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      if (player == null) {
         return;
      }

      // If a player entered this warp on the server, move them
      if (player.isServer && player.connectionToClient != null) {
         SpawnID spawnID = new SpawnID(areaTarget, spawnTarget);
         Vector2 localPos = SpawnManager.self.getSpawnLocalPosition(spawnID);
         player.spawnInNewMap(areaTarget, localPos, newFacingDirection);
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.WARP_TARGET_MAP_KEY:
               areaTarget = field.v.Trim(' ');
               break;
            case DataField.WARP_TARGET_SPAWN_KEY:
               spawnTarget = field.v.Trim(' ');
               break;
            case DataField.WARP_WIDTH_KEY:
               _collider.size = new Vector2(float.Parse(field.v), _collider.size.y);
               break;
            case DataField.WARP_HEIGHT_KEY:
               _collider.size = new Vector2(_collider.size.x, float.Parse(field.v));
               break;
            case DataField.WARP_ARRIVE_FACING_KEY:
               Direction? dir = MapImporter.parseDirection(field.v);
               if (dir != null)
                  newFacingDirection = dir.Value;
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }


   #region Private Variables

   // The the collider, which will trigger the warp to activate
   protected BoxCollider2D _collider;

   #endregion
}
