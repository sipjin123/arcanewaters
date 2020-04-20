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
      if (type > _rugColorLookup.Length) {
         D.warning("Not enough rug colors specified");
         return;
      }
      gameObject.name = _rugColorLookup[type];

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
      return Minimap.self.mapEditorConfig.rugLookup[type];
   }

   #region Private Variables

   // Rug name based on tile type
   private static string[] _rugColorLookup = { "", "Red", "Yellow", "Green", "Violet" };
   
   // Minimum tile position
   private Vector2Int _minBounds;

   // Maximum tile position
   private Vector2Int _maxBounds;

   #endregion
}
