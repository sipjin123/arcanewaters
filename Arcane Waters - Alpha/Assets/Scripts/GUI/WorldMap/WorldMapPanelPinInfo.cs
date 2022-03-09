public class WorldMapPanelPinInfo
{
   #region Public Variables

   // Area width
   public int areaWidth;

   // Area height
   public int areaHeight;

   // Area position x
   public int areaX;

   // Area position y
   public int areaY;

   // Pin type
   public WorldMapPanelPin.PinTypes pinType;

   // Special Type
   public int specialType;

   // X coord of the relative position of the pin within the area
   public float x;

   // Y coord of the relative position of the pin within the area
   public float y;

   // The target of the pin
   public string target = "";
   
   // The spawn target of the pin
   public string spawnTarget = "";

   // The id of the discovery
   public int discoveryId;

   // Computed discovery status
   public bool discovered;

   // Display name for the pin
   public string displayName = "";

   #endregion

   #region Private Variables

   #endregion
}
