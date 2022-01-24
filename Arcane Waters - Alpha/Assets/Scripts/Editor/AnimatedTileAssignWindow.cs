using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Tilemaps;

public class AnimatedTileAssignWindow : EditorWindow
{
   #region Public Variables

   #endregion

   [MenuItem("Window/2D/Animated Tile Assign")]
   static void init () {
      // Get existing open window or if none, make a new one:
      AnimatedTileAssignWindow window = (AnimatedTileAssignWindow) EditorWindow.GetWindow(typeof(AnimatedTileAssignWindow));
      window.Show();
   }

   void OnGUI () {
      GUILayout.Label("Texture path");
      texturePath = EditorGUILayout.TextField(texturePath);

      GUILayout.Label("Tile rows (format: 5, 3, 4, 8, 6)");
      tileRows = EditorGUILayout.TextField(tileRows);

      GUILayout.Label("How many sprites are in an animation");
      animFrameCount = EditorGUILayout.IntField(animFrameCount);

      if (GUILayout.Button("Assign")) {
         Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>().ToArray();

         Debug.Log("found " + sprites.Length + " sprites");

         AnimatedTile[] tiles = Selection.objects.Where(g => g is AnimatedTile).Select(g => g as AnimatedTile).ToArray();

         Debug.Log("found " + tiles.Length + " tiles");

         string[] vals = tileRows.Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries);
         int[] rowCounts = vals.Select(v => int.Parse(v)).ToArray();
         int spriteIndex = 0;
         int updatedTiles = 0;

         for (int k = 0; k < rowCounts.Length; k++) {
            for (int i = 0; i < rowCounts[k]; i++) {
               tiles[updatedTiles + i].m_AnimatedSprites = new Sprite[animFrameCount];
            }

            for (int f = 0; f < animFrameCount; f++) {
               for (int i = 0; i < rowCounts[k]; i++) {
                  tiles[updatedTiles + i].m_AnimatedSprites[f] = sprites[spriteIndex++];
               }
            }

            for (int i = 0; i < rowCounts[k]; i++) {
               EditorUtility.SetDirty(tiles[updatedTiles + i]);
            }
            updatedTiles += rowCounts[k];
         }

         Debug.Log("Finished. Updated " + updatedTiles + " tiles");
      }
   }

   #region Private Variables

   // The texture we are taking sprites from
   private string texturePath;

   // Tiles in a row of sprite sheet
   private string tileRows;

   // How many sprites are in an animation
   private int animFrameCount;

   #endregion
}
