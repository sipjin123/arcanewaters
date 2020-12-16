public class ProjectileStatData  {
   #region Public Variables

   // The primary id that is referenced by dependent objects, this is a unique id
   public int projectileId;

   // The speed of the projectile
   public float projectileSpeed;

   // The scale of the projectile
   public float projectileScale;

   // The path of the projectile sprite
   public float projectileSpritePath;

   // If the projectile is a still sprite or animated
   public bool isAnimating;

   // The status type that will take effect after collision
   public Status.Type statusType;

   #endregion
}