[System.Serializable]
public class MapChangeComment
{
   #region Public Variables

   // Map id of the target map
   public int mapId;

   // The id of the version that was changed
   public int mapVersion;

   // The user id of the user that made the change
   public int userId;

   // The account name of the user that made the change
   public string accName;

   // The map name of the map that was changed
   public string mapName;

   // The comment of the change
   public string comment;

   #endregion

   #region Private Variables

   #endregion
}
