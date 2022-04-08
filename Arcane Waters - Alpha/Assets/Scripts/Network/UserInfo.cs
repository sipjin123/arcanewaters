using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class UserInfo {
   #region Public Variables

   // The user ID
   public int userId;

   // The account ID
   public int accountId;

   // The account name
   public string accountName;

   // The user name
   public string username;

   // Our Gender
   [XmlElement(Namespace = "GenderType")]
   public Gender.Type gender;

   // The Body type
   [XmlElement(Namespace = "BodyType")]
   public BodyLayer.Type bodyType;

   // The Facing direction
   public int facingDirection;

   // The area key we're in, if any
   public string areaKey;

   // The id of our assigned ship
   public int flagshipId;

   // Our position
   public Vector2 localPos;

   // Whether we're an admin
   public int adminFlag;

   // The XP amount
   public int XP;

   // The amount of gold
   public int gold;

   // The amount of gems
   public int gems;

   // The armor ID
   public int armorId;

   // The weapon ID
   public int weaponId;

   // The hat ID
   public int hatId;

   // The gear ID used
   public int ringId;
   public int necklaceId;
   public int trinketId;

   // The hair type
   [XmlElement(Namespace = "HairType")]
   public HairLayer.Type hairType;

   // The primary hair color id
   public string hairPalettes = "";

   // The eyes ID
   [XmlElement(Namespace = "EyesType")]
   public EyesLayer.Type eyesType;

   // The primary eyes color id
   public string eyesPalettes = "";

   // The character spot on the character creation screen
   public int charSpot;

   // The Guild we're in
   public int guildId;

   // The pvp state
   public int pvpState;

   // The Guild Name
   public string guildName;

   // The guild icon layers
   public string iconBorder;
   public string iconBackground;
   public string iconSigil;

   // The guild icon palettes
   public string iconBackPalettes;
   public string iconSigilPalettes;

   // The user rank within guild
   public int guildRankId;

   // The custom guild map layout our guild has chosen
   public int guildMapBaseId;

   // The custom guild house layout our guild has chosen
   public int guildHouseBaseId;

   // The house layout map we've chosen
   public int customHouseBaseId;

   // The farm layout map we've chosen
   public int customFarmBaseId;

   // The last login time
   public System.DateTime lastLoginTime;

   // Gets set to true when the user is online (this is not stored in the DB)
   public bool isOnline = false;

   #endregion

   public UserInfo () { }

   #if IS_SERVER_BUILD

   public UserInfo (MySqlDataReader dataReader) {
      this.userId = dataReader.GetInt32("usrId");
      this.accountId = dataReader.GetInt32("accId");
      this.accountName = dataReader.GetString("accName");
      this.username = dataReader.GetString("usrName");
      this.gender = (Gender.Type)dataReader.GetInt32("usrGender");
      this.bodyType = (BodyLayer.Type)dataReader.GetInt32("bodyType");
      this.facingDirection = dataReader.GetInt32("usrFacing");
      this.areaKey = dataReader.GetString("areaKey");
      this.localPos = new Vector2(dataReader.GetFloat("localX"), dataReader.GetFloat("localY"));
      this.adminFlag = dataReader.GetInt32("usrAdminFlag");
      this.armorId = dataReader.GetInt32("armId");
      this.weaponId = dataReader.GetInt32("wpnId");
      this.ringId = dataReader.GetInt32("ringId");
      this.necklaceId = dataReader.GetInt32("necklaceId");
      this.trinketId = dataReader.GetInt32("trinketId");
      this.hatId = dataReader.GetInt32("hatId");
      this.hairType = (HairLayer.Type)dataReader.GetInt32("hairType");
      this.hairPalettes = dataReader.GetString("hairPalettes");
      this.XP = dataReader.GetInt32("usrXP");
      this.gold = dataReader.GetInt32("usrGold");
      this.gems = dataReader.GetInt32("accGems");
      this.eyesType = (EyesLayer.Type)dataReader.GetInt32("eyesType");
      this.eyesPalettes = dataReader.GetString("eyesPalettes");
      this.flagshipId = dataReader.GetInt32("shpId");
      this.charSpot = dataReader.GetInt32("charSpot");
      this.guildId = dataReader.GetInt32("gldId");
      try {
         this.pvpState = dataReader.GetInt32("pvpState");
      } catch {
         D.debug("Does not have pvp state for UserInfo {" + userId + "}");
      }

      this.guildRankId = dataReader.GetInt32("gldRankId");
      this.customHouseBaseId = dataReader.GetInt32("customHouseBase");
      this.customFarmBaseId = dataReader.GetInt32("customFarmBase");
      this.lastLoginTime = dataReader.GetDateTime("lastLoginTime");

      if (this.guildId > 0) {
         try {
            this.iconBorder = dataReader.GetString("gldIconBorder");
            this.iconBackground = dataReader.GetString("gldIconBackground");
            this.iconSigil = dataReader.GetString("gldIconSigil");
            this.iconBackPalettes = dataReader.GetString("gldIconBackPalettes");
            this.iconSigilPalettes = dataReader.GetString("gldIconSigilPalettes");
         } catch {
            D.warning("Problem with loading guild information");
         }
      }
}

#endif

   public override bool Equals (object rhs) {
      if (rhs is UserInfo) {
         var other = rhs as UserInfo;
         return userId == other.userId;
      }
      return false;
   }

   public override int GetHashCode () {
      return userId.GetHashCode();
   }

   public object[] serialize () {
      object[] serialized = new object[23];

      serialized[0] = this.userId;
      serialized[1] = this.accountId;
      serialized[2] = this.username;
      serialized[3] = this.gender;
      serialized[4] = this.bodyType;
      serialized[5] = this.facingDirection;
      serialized[6] = this.areaKey;
      serialized[7] = this.localPos;
      serialized[8] = this.adminFlag;
      serialized[9] = this.XP;
      serialized[10] = this.gold;
      serialized[11] = this.armorId;
      serialized[12] = this.weaponId;
      serialized[13] = this.hairType;
      serialized[14] = this.hairPalettes;
      serialized[15] = this.eyesType;
      serialized[16] = this.eyesPalettes;
      serialized[17] = this.accountName;
      serialized[18] = this.flagshipId;
      serialized[19] = this.gems;
      serialized[20] = this.charSpot;
      serialized[21] = this.ringId;
      serialized[22] = this.necklaceId;
      serialized[22] = this.trinketId;

      return serialized;
   }

   public static UserInfo deseralize (object[] serialized) {
      UserInfo userInfo = new UserInfo();

      userInfo.userId = (int) serialized[0];
      userInfo.accountId = (int) serialized[1];
      userInfo.username = (string) serialized[2];
      userInfo.gender = (Gender.Type) serialized[3];
      userInfo.bodyType = (BodyLayer.Type) serialized[4];
      userInfo.facingDirection = (int) serialized[5];
      userInfo.areaKey = (string) serialized[6];
      userInfo.localPos = (Vector2) serialized[7];
      userInfo.adminFlag = (int) serialized[8];
      userInfo.XP = (int) serialized[9];
      userInfo.gold = (int) serialized[10];
      userInfo.armorId = (int) serialized[11];
      userInfo.weaponId = (int) serialized[12];
      userInfo.hairType = (HairLayer.Type) serialized[13];
      userInfo.hairPalettes = (string) serialized[14];
      userInfo.eyesType = (EyesLayer.Type) serialized[15];
      userInfo.eyesPalettes = (string) serialized[16];
      userInfo.accountName = (string) serialized[17];
      userInfo.flagshipId = (int) serialized[18];
      userInfo.gems = (int) serialized[19];
      userInfo.charSpot = (int) serialized[20];
      userInfo.ringId = (int) serialized[21];
      userInfo.necklaceId = (int) serialized[22];
      userInfo.trinketId = (int) serialized[23];

      return userInfo;
   }

   #region Private Variables

   #endregion
}
