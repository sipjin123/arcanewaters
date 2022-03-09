using UnityEngine;
using UnityEngine.UI;

public class WorldMapPanelCloud : MonoBehaviour
{
   #region Public Variables

   // The position of the cloud
   public Vector2Int position;

   // Cloud Image
   public Image cloudImage;

   // Shadow Image
   public Image cloudShadowImage;

   // The configuration of the cloud
   public CloudConfiguration configuration;

   // The possible layout configuration of the cloud
   [System.Flags]
   public enum CloudConfiguration
   {
      // None
      None = 0,

      // There is a cloud north of this cloud
      North = 1,

      // There is a cloud northeast
      NorthEast = 2,

      // There is a cloud east
      East = 4,

      // There is a cloud south east
      SouthEast = 8,

      // There is a cloud south
      South = 16,

      // There is a cloud south west
      SouthWest = 32,

      // There is a cloud west
      West = 64,

      // There is a cloud north west
      NorthWest = 128
   }

   #endregion

   public void toggle (bool show) {
      gameObject.SetActive(show);
   }

   public void setCloudSprite (Sprite sprite) {
      cloudImage.sprite = sprite;
   }

   public void setShadowSprite (Sprite sprite) {
      cloudShadowImage.sprite = sprite;
   }

   public static bool cardinalsAll (CloudConfiguration source) {
      foreach (CloudConfiguration cardinal in new[] { CloudConfiguration.North, CloudConfiguration.East, CloudConfiguration.West, CloudConfiguration.South }) {
         if (!source.HasFlag(cardinal)) {
            return false;
         }
      }
      return true;
   }

   public static bool cardinalsNone (CloudConfiguration source) {
      foreach (CloudConfiguration cardinal in new[] { CloudConfiguration.North, CloudConfiguration.East, CloudConfiguration.West, CloudConfiguration.South }) {
         if (source.HasFlag(cardinal)) {
            return false;
         }
      }
      return true;
   }

   public static bool cardinalsBut (CloudConfiguration source, params CloudConfiguration[] test) {
      foreach (CloudConfiguration cardinal in new[] { CloudConfiguration.North, CloudConfiguration.East, CloudConfiguration.West, CloudConfiguration.South }) {
         if ((!source.HasFlag(cardinal) && !test.Contains(cardinal)) || (source.HasFlag(cardinal) && test.Contains(cardinal))) {
            return false;
         }
      }
      return true;
   }

   public static bool cardinalsOnly (CloudConfiguration source, params CloudConfiguration[] test) {

      foreach (CloudConfiguration cardinal in new[] { CloudConfiguration.North, CloudConfiguration.East, CloudConfiguration.West, CloudConfiguration.South }) {
         if ((test.Contains(cardinal) && !source.HasFlag(cardinal)) || (!test.Contains(cardinal) && source.HasFlag(cardinal))) {
            return false;
         }
      }

      return true;
   }

   // OK
   public static bool nonCardinalsAll (CloudConfiguration source) {
      foreach (CloudConfiguration nonCardinal in new[] { CloudConfiguration.NorthEast, CloudConfiguration.SouthEast, CloudConfiguration.NorthWest, CloudConfiguration.SouthWest }) {
         if (!source.HasFlag(nonCardinal)) {
            return false;
         }
      }
      return true;
   }

   // OK
   public static bool nonCardinalsNone (CloudConfiguration source) {
      foreach (CloudConfiguration nonCardinal in new[] { CloudConfiguration.NorthEast, CloudConfiguration.SouthEast, CloudConfiguration.NorthWest, CloudConfiguration.SouthWest }) {
         if (source.HasFlag(nonCardinal)) {
            return false;
         }
      }
      return true;
   }

   // OK
   public static bool nonCardinalsBut (CloudConfiguration source, params CloudConfiguration[] test) {
      foreach (CloudConfiguration nonCardinal in new[] { CloudConfiguration.NorthEast, CloudConfiguration.SouthEast, CloudConfiguration.NorthWest, CloudConfiguration.SouthWest }) {
         if ((!test.Contains(nonCardinal) && !source.HasFlag(nonCardinal)) || (test.Contains(nonCardinal) && source.HasFlag(nonCardinal))) {
            return false;
         }
      }
      return true;
   }

   public static bool nonCardinalsOnly (CloudConfiguration source, params CloudConfiguration[] test) {
      foreach (CloudConfiguration nonCardinal in new[] { CloudConfiguration.NorthEast, CloudConfiguration.SouthEast, CloudConfiguration.NorthWest, CloudConfiguration.SouthWest }) {
         if ((test.Contains(nonCardinal) && !source.HasFlag(nonCardinal)) || (!test.Contains(nonCardinal) && source.HasFlag(nonCardinal))) {
            return false;
         }
      }
      return true;
   }

   public static bool hasDirections (CloudConfiguration source, params CloudConfiguration[] test) {
      foreach (CloudConfiguration direction in test) {
         if (!source.HasFlag(direction)) {
            return false;
         }
      }

      return true;
   }

   public static bool excludesDirections (CloudConfiguration source, params CloudConfiguration[] test) {
      foreach (CloudConfiguration direction in test) {
         if (source.HasFlag(direction)) {
            return false;
         }
      }

      return true;
   }

   public bool isVisible () {
      return gameObject.activeInHierarchy;
   }

   #region Private Variables

   #endregion
}
