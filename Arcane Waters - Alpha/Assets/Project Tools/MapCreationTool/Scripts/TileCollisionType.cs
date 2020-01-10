namespace MapCreationTool
{
   public enum TileCollisionType
   {
      Enabled,
      Disabled,

      /// <summary>
      /// This tile will cancel all enabled tiles with lower priority
      /// </summary>
      CancelEnabled,
      /// <summary>
      /// This tile will cancel all enabled tiles with lower priority
      /// </summary>
      CancelDisabled
   }
}