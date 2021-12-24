using UnityEngine;

public class PlantableTreeInstanceData
{
   #region Public Variables

   // The types of state the tree can be in
   public enum StateType
   {
      None = 0,
      Planted = 1,
      Watered = 2,
      Tethered = 3,
      Untethered = 4
   }

   // Unique identifier
   public int id;

   // Id of the tree definition we are based on
   public int treeDefinitionId;

   // The area this tree belongs to
   public string areaKey;

   // User that planted this crop
   public int planterUserId;

   // The position this tree is placed at
   public Vector2 position;

   // The state this tree is at
   public StateType state;

   // When was the last time we made an update to this tree
   public long lastUpdateTime;

   // Marks if this instance has been deleted, not stored in DB
   public bool deleted = false;

   #endregion

   public PlantableTreeInstanceData () {

   }

#if IS_SERVER_BUILD

   public PlantableTreeInstanceData (MySql.Data.MySqlClient.MySqlDataReader dataReader) {
      id = DataUtil.getInt(dataReader, "id");
      treeDefinitionId = DataUtil.getInt(dataReader, "treeDefinitionId");
      areaKey = DataUtil.getString(dataReader, "areaKey");
      planterUserId = DataUtil.getInt(dataReader, "planterUserId");
      position = new Vector2(DataUtil.getFloat(dataReader, "position_x"), DataUtil.getFloat(dataReader, "position_y"));
      state = (StateType) DataUtil.getInt(dataReader, "state");
      lastUpdateTime = DataUtil.getLong(dataReader, "lastUpdateTime");
   }

#endif

   #region Private Variables

   #endregion
}
