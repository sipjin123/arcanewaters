using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class DirectionalTransform
{
   // Used for determining which direction ID the spawn point belongs to
   public Direction direction;

   // The transform where the projectile should spawn
   public Transform spawnTransform;
}

[Serializable]
public class DirectionalPositions
{
   // Used for determining which direction ID the spawn point belongs to
   public Direction direction;

   // The coordinates where the projectile should spawn
   public Vector3 spawnTransform;
}