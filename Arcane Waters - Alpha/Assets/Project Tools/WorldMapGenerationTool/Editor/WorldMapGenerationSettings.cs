using UnityEngine;

[CreateAssetMenu(fileName = "WorldMapGenerationSettings", menuName = "World Map Generation Tool - Settings")]
public class WorldMapGenerationSettings : ScriptableObject
{
   #region Public Variables

   // Reference to the source texture
   public Texture2D sourceTexture;

   // Number of columns in the source texture;
   public int columns;

   // Number of rows in the source texture;
   public int rows;

   // Should clean up temporary files
   public bool shouldCleanUp;

   #endregion

   #region Private Variables

   #endregion
}
