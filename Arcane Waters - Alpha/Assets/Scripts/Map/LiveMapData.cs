// The map parameters for queueing
using System;
using MapCustomization;
using UnityEngine;

[Serializable]
public class LiveMapData {
   public string areaKey;
   public MapInfo mapInfo;
   public Vector3 mapPos;
   public MapCustomizationData mapCustomData;
   public Biome.Type biome;
}
