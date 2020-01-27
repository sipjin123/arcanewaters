using System;

[Serializable]
public class MapData
{
   #region Public Variables

   // The unique name by which map is identified
   public string name;

   // The version of the map data
   public int version;

   // The serialized data, out of which an area can be instantiated
   public string serializedData;

   #endregion

   #region Private Variables

   #endregion
}
