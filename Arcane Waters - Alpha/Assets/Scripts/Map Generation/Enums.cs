namespace ProceduralMap
{
   public enum BorderDirection
   {
      ////////////////////////////////////////Four directions

      /// <summary>
      /// all directions
      /// </summary>
      N_E_S_W,

      ////////////////////////////////////////Three directions

      /// <summary>
      /// Top Lateral
      /// </summary>
      N_E_W, //Top Lateral

      /// <summary>
      /// Down Lateral
      /// </summary>
      E_S_W,

      /// <summary>
      /// Left Top Down
      /// </summary>
      N_S_W,

      /// <summary>
      /// Right Top Down
      /// </summary>
      N_E_S,

      ////////////////////////////////////////////////Two directions

      /// <summary>
      /// Top Down
      /// </summary>
      N_S,

      /// <summary>
      /// Lateral
      /// </summary>
      E_W,

      /// <summary>
      /// Top Left
      /// </summary>
      N_W,

      /// <summary>
      /// Down Left
      /// </summary>
      S_W,

      /// <summary>
      /// Top Right
      /// </summary>
      N_E,

      /// <summary>
      /// Down Right
      /// </summary>
      S_E,

      //////////////////////////////////////////////////////One direction

      /// <summary>
      /// Top
      /// </summary>
      N,

      /// <summary>
      /// Down
      /// </summary>
      S,

      /// <summary>
      /// Right
      /// </summary>
      E,

      /// <summary>
      /// Left
      /// </summary>
      W
   }
   public enum CornerDirection
   {
      NE,
      SE,
      SW,
      NW
   }
   public enum RiverDirection
   {
      N_E_S_W,
      E_S,
      E_W,
      EBorder_W,
      E_WBorder,
      S_W,
      N_S,
      NBorder_S,
      N_SBorder,
      N_W,
      N_E,
      N_E_S,
      N_S_W,
      E_S_W,
      N_E_W
   }
   //public enum Biome
   //{
   //   none,
   //   desert,
   //   forest,
   //   lava,
   //   mushroom,
   //   pine,
   //   snow
   //}

   public enum NodeType
   {
      Water,
      Land,
      LandBorder_N,
      LandBorder_E,
      LandBorder_S,
      LandBorder_W,
      Wall
   }
}