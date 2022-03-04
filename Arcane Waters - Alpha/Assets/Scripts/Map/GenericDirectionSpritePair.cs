using UnityEngine;
using System;

[Serializable]
public class GenericDirectionSpritePair {
   // The biome type
   public Direction direction;

   // The sprite
   public Sprite sprite;
}

[Serializable]
public class GenericDirectionObjPair
{
   // The biome type
   public Direction direction;

   // The sprite
   public GameObject obj;
}