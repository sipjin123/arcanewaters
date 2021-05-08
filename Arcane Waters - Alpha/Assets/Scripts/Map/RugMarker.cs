using UnityEngine;

public class RugMarker : MonoBehaviour
{
   #region Public Variables

   // The type of the rug, where 0 = None
   public int type;

   // Center position of the rug
   public Vector2 center;

   // Size of the rug
   public Vector2 size;

   #endregion

   public void processData() {
      Vector2 minBoundsFloat = (center * 6.25f - size * 6.25f * 0.5f);
      Vector2 maxBoundsFloat = (center * 6.25f + size * 6.25f * 0.5f);

      _minBounds = new Vector2Int(Mathf.RoundToInt(minBoundsFloat.x), Mathf.RoundToInt(minBoundsFloat.y));
      _maxBounds = new Vector2Int(Mathf.RoundToInt(maxBoundsFloat.x) - 1, Mathf.RoundToInt(maxBoundsFloat.y) - 1);
   }

   public Vector2Int getMinBounds () {
      return _minBounds;
   }

   public Vector2Int getMaxBounds () {
      return _maxBounds;
   }

   public int getWidth () {
      return (_maxBounds.x - _minBounds.x) + 1;
   }

   public int getHeight () {
      return (_maxBounds.y - _minBounds.y) + 1;
   }

   public int getPixelCount () {
      return getWidth() * getHeight();
   }

   public Color getRugColor () {
      Color[] rugLookup = null;
      Biome.Type biome = Global.player.getInstance().biome;
      
      // Handle special case when user is inside his own house
      if (biome == Biome.Type.None && AreaManager.self.isHouseOfUser(Global.player.areaKey, Global.player.userId)) {
         biome = Biome.Type.Forest;
      }

      switch (biome) {
         case Biome.Type.Forest:
            rugLookup = Minimap.self.mapEditorConfig.rugLookupForest;
            break;

         case Biome.Type.Desert:
            rugLookup = Minimap.self.mapEditorConfig.rugLookupDesert;
            break;

         case Biome.Type.Lava:
            rugLookup = Minimap.self.mapEditorConfig.rugLookupLava;
            break;

         case Biome.Type.Mushroom:
            rugLookup = Minimap.self.mapEditorConfig.rugLookupShroom;
            break;

         case Biome.Type.Pine:
            rugLookup = Minimap.self.mapEditorConfig.rugLookupPine;
            break;

         case Biome.Type.Snow:
            rugLookup = Minimap.self.mapEditorConfig.rugLookupSnow;
            break;

         default:
            D.warning("Player's current biome not found");
            return Color.magenta;
      }

      if (rugLookup.Length <= type) {
         D.warning("Existing rug type is not existing");
         return Color.magenta;
      }
      return rugLookup[type];
   }

   #region Private Variables

   // Minimum tile position
   private Vector2Int _minBounds;

   // Maximum tile position
   private Vector2Int _maxBounds;

   #endregion
}
