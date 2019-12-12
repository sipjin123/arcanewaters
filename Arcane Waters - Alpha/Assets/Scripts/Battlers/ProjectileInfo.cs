using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class ProjectileInfo
{
   // The type of projectile
   public ProjectileType projectileType;

   // The sprite associated to the projectile
   public Sprite sprite;
}

public enum ProjectileType
{
   Bullet = 0,
   ElectricBall = 1,
   FireBall = 2,
   WaterBall = 3
}