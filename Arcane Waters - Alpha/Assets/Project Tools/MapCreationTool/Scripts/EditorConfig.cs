using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    [CreateAssetMenu(fileName = "New config", menuName = "Data/Editor config")]
    public class EditorConfig : ScriptableObject
    {
        public Tile[] layerTiles;
        public Tile[] clusterTiles;
        public TileBase transparentTile;
    }
}


