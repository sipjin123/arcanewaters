using UnityEngine;

// The coordinates of an area within the World Map Panel
public struct WorldMapPanelAreaCoords
{
   #region Public Variables

   // x
   public int x;

   // y
   public int y;

   #endregion

   public WorldMapPanelAreaCoords (int x, int y) {
      this.x = x;
      this.y = y;
   }

   public override string ToString () {
      return $"{x}_{y}";
   }

   public override bool Equals (object obj) {
      return x == ((WorldMapPanelAreaCoords) obj).x && y == ((WorldMapPanelAreaCoords) obj).y;
   }

   public override int GetHashCode () {
      int hashCode = 1502939027;
      hashCode = hashCode * -1521134295 + x.GetHashCode();
      hashCode = hashCode * -1521134295 + y.GetHashCode();
      return hashCode;
   }

   public static bool operator ==(WorldMapPanelAreaCoords a, WorldMapPanelAreaCoords b){
      return a.Equals(b);
   }

   public static bool operator != (WorldMapPanelAreaCoords a, WorldMapPanelAreaCoords b) {
      return !a.Equals(b);
   }
}
