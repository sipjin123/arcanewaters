using UnityEngine;

namespace MapCreationTool
{
    public class MountainGroupConfig : MonoBehaviour
    {
        public BiomeTilemaps[] biomeTileMaps;
        public BoundsInt innerBounds;
        public BoundsInt outerBounds;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawCube(transform.position + innerBounds.center, innerBounds.size);
            Gizmos.color = new Color(0, 0, 1, 0.4f);
            Gizmos.DrawCube(transform.position + outerBounds.center, outerBounds.size);
        }

        [System.Serializable]
        public class BiomeTilemaps
        {
            public BiomeType biome;
            public UnityEngine.Tilemaps.Tilemap outerTilemap;
            public UnityEngine.Tilemaps.Tilemap innerTilemap;
        }
    }
}