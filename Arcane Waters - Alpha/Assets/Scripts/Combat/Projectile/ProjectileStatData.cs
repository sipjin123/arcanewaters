using System;

[Serializable]
public class ProjectileStatData  {
   #region Public Variables

   // The primary id that is referenced by dependent objects, this is a unique id
   public int projectileId = -1;

   // The mass of the projectile, this directly affects the speed of the projectile, the higher the value the faster to reach target
   public float projectileMass = .5f;

   // The scale of the projectile
   public float projectileScale = 1;

   // The damage of the projectile
   public float projectileDamage = 25;

   // The speed of the projectile animation
   public float animationSpeed = 1;

   // The path of the projectile sprite
   public string projectileSpritePath;

   // The sfx directories when colliding with various terrains
   public string defaultHitSFX = "";
   public string waterHitSFX = "";
   public string landHitSFX = "";
   public float defaultHitVol = 1;
   public float waterHitVol = 1;
   public float landHitVol = 1;

   // If the projectile is a still sprite or animated
   public bool isAnimating;

   // The status type that will take effect after collision
   public Status.Type statusType;

   #endregion
}

public class ProjectileStatPair {
   // The xml id
   public int xmlId;

   // The stat data
   public ProjectileStatData projectileData;
}