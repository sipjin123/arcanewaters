public enum LandPowerupType {
   None = 0,
   DamageBoost = 1,
   SpeedBoost = 2,
}

public enum LandPowerupExpiryType {
   None = 0,
   Time = 1,
   BossKills = 2,
}

public class LandPowerupData {
   // Where the icon sprites for the powerups are located
   public static string ICON_SPRITES_LOCATION = "Sprites/Powerups/LandPowerUpIcons";

   // Where the border sprites for the powerups are located
   public static string BORDER_SPRITES_LOCATION = "Sprites/Powerups/LandPowerUpBorders";

   // The user id
   public int userId = -1;

   // The type of powerup this is
   public LandPowerupType landPowerupType = LandPowerupType.None;

   // The expiry type
   public LandPowerupExpiryType expiryType = LandPowerupExpiryType.None; 

   // The expiry counter
   public int counter = 0;

   // The value of the powerup
   public int value;
}