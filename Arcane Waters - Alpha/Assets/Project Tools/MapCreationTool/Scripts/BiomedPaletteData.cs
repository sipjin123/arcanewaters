using System.Collections.Generic;
using MapCreationTool.PaletteTilesData;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool {
   public class BiomedPaletteData : MonoBehaviour
   {
      public BiomePaletteResources[] biomePaletteResources = new BiomePaletteResources[0];

      [HideInInspector]
      public TileSetupContainer tileSetupContainer;


      // The dictionary all the different biome palette datas are stored in
      private Dictionary<BiomeType, PaletteData> datas;

      /// <summary>
      /// Collects and arranges all the asigned information so that the data becomes usable
      /// </summary>
      public void collectInformation() {
         datas = new Dictionary<BiomeType, PaletteData>();
      }



      public PaletteData this[BiomeType type]
      {
         get { return datas[type]; }
      }

      [System.Serializable]
      public class BiomePaletteResources
      {
         public BiomeType biome = BiomeType.Forest;
         public Tilemap tilesTilemap = null;
      }
      
      [System.Serializable]
      public class TileSetupContainer
      {
         public TileSetup[] tileSetups = new TileSetup[0];
         public int matrixHeight = 0;

         public TileSetup this[int x, int y]
         {
            get
            {
               if (x < 0 || y < 0)
                  return null;

               encapsulate(x, y);

               if (tileSetups[x * matrixHeight + y] == null)
                  tileSetups[x * matrixHeight + y] = new TileSetup();

               return tileSetups[x * matrixHeight + y];
            }
         }

         public bool contains(float x, float y) {
            int i = Mathf.FloorToInt(x);
            int j = Mathf.FloorToInt(y);

            return x >= 0 && y >= 0 && i < size.x && j < size.y;
         }

         public Vector2Int size
         {
            get { return new Vector2Int(matrixHeight == 0 ? 0 : tileSetups.Length / matrixHeight, matrixHeight); }
         }

         public void encapsulate(int x, int y) {
            if (y < matrixHeight && x < (matrixHeight == 0 ? 0 : tileSetups.Length / matrixHeight))
               return;

            int oldWidth = matrixHeight == 0 ? 0 : tileSetups.Length / matrixHeight;
            resize(Mathf.Max(x + 1, oldWidth), Mathf.Max(y + 1, matrixHeight));
         }

         public void resize(int width, int height) {
            TileSetup[] old = tileSetups.Clone() as TileSetup[];
            int oldHeight = matrixHeight;
            int oldWidth = oldHeight == 0 ? 0 : old.Length / oldHeight;

            matrixHeight = height;
            int matrixWidth = width;
            tileSetups = new TileSetup[Mathf.Max(matrixHeight * matrixWidth, old.Length)];

            for (int i = 0; i < tileSetups.Length; i++) {
               // Extract matrix x and y coors
               Vector2Int coors = new Vector2Int(i / (matrixHeight - 1), i % matrixHeight);

               if (coors.x < oldWidth && coors.y < oldHeight) {
                  // Apply old element
                  tileSetups[i] = old[coors.x * oldHeight + coors.y];
               }
            }
         }
      }

      [System.Serializable]
      public class TileSetup
      {
         public string layer;
         public int sublayer;
         public int cluster;
         public TileCollisionType collisionType;
      }
   }
}

