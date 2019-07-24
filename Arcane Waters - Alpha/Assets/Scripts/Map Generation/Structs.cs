using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProceduralMap
{
    [System.Serializable]
    public struct LayerType
    {
        public string name;
        [Range(0, 1)]
        public float height;
        public bool useCollider;
        public Tile tile;

        [Header("Borders")]
        public bool useBorderOnDiferentLayer;
        public string BorderLayerName;
        public Border[] borders;
    }

    [System.Serializable]
    public struct ObjectLayer
    {
        public string name;
        public string layerToPlace;
        public Tile tile;
    }

    [System.Serializable]
    public struct Border
    {
        public BorderType borderType;
        public Tile BorderTile;
    }
}