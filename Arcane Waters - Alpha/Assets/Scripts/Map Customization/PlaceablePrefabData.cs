using UnityEngine;

namespace MapCustomization
{
   public struct PlaceablePrefabData
   {
      #region Public Variables

      // Id that is used for this prefab when serializing it
      public int serializationId { get; set; }

      // The actual prefab that gets instantiated in the scene
      public CustomizablePrefab prefab { get; set; }

      // Sprite which should be showed for the user in the UI
      public Sprite displaySprite { get; set; }

      #endregion

      #region Private Variables

      #endregion
   }
}
