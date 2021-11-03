public enum LandPowerupType {
   None = 0,
   DamageBoost = 1,
   DefenseBoost = 2,
   SpeedBoost = 3,
}

public enum LandPowerupExpiryType {
   None = 0,
   Time = 1,
   BossKills = 2,
   OnWarp = 3
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

public class LandPowerupInfo {
   // The type of powerup
   public LandPowerupType powerupType;

   // The expiry type
   public LandPowerupExpiryType expiryType = LandPowerupExpiryType.None;

   // The name of the powerup
   public string powerupName;

   // The info of the powerup
   public string powerupInfo;

   // The path of the icon
   public string iconPath = "";

   // The base attribute this powerup provides
   public int baseAttribute = 1;

   // The default counter of this powerup
   public int baseCounter = 1;
}