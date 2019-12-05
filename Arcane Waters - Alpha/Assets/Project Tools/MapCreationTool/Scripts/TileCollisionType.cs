namespace MapCreationTool
{
   public enum TileCollisionType
   {
      Enabled,
      Disabled,

      /// <summary>
      /// This tile will cancel all 'ForceDisabled' type lower-priority tiles
      /// </summary>
      ForceEnabled,

      /// <summary>
      /// This tile will cancel all 'ForceEnabled' type lower-priority tiles
      /// </summary>
      ForceDisabled
   }
}