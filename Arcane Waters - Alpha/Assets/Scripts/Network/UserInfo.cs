using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
   public Gender.Type gender;

   // The Body type
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

   // The hair type
   public HairLayer.Type hairType;

   // The primary hair color id
   public string hairPalette1 = "";

   // The secondary hair color id
   public string hairPalette2 = "";

   // The eyes ID
   public EyesLayer.Type eyesType;

   // The primary eyes color id
   public string eyesPalette1 = "";

   // The secondary eyes color id
   public string eyesPalette2 = "";

   // The character spot on the character creation screen
   public int charSpot;

   // The Guild we're in
   public int guildId;

   // The house layout map we've chosen
   public int customHouseBaseId;

   // The farm layout map we've chosen
   public int customFarmBaseId;

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
      this.hatId = dataReader.GetInt32("hatId");
      this.hairType = (HairLayer.Type)dataReader.GetInt32("hairType");
      this.hairPalette1 = dataReader.GetString("hairPalette1");
      this.hairPalette2 = dataReader.GetString("hairPalette2");
      this.XP = dataReader.GetInt32("usrXP");
      this.gold = dataReader.GetInt32("usrGold");
      this.gems = dataReader.GetInt32("accGems");
      this.eyesType = (EyesLayer.Type)dataReader.GetInt32("eyesType");
      this.eyesPalette1 = dataReader.GetString("eyesPalette1");
      this.eyesPalette2 = dataReader.GetString("eyesPalette2");
      this.flagshipId = dataReader.GetInt32("shpId");
      this.charSpot = dataReader.GetInt32("charSpot");
      this.guildId = dataReader.GetInt32("gldId");
      this.customHouseBaseId = dataReader.GetInt32("customHouseBase");
      this.customFarmBaseId = dataReader.GetInt32("customFarmBase");
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
      serialized[14] = this.hairPalette1;
      serialized[15] = this.hairPalette2;
      serialized[16] = this.eyesType;
      serialized[17] = this.eyesPalette1;
      serialized[18] = this.eyesPalette2;
      serialized[19] = this.accountName;
      serialized[20] = this.flagshipId;
      serialized[21] = this.gems;
      serialized[22] = this.charSpot;

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
      userInfo.hairPalette1 = (string) serialized[14];
      userInfo.hairPalette2 = (string) serialized[15];
      userInfo.eyesType = (EyesLayer.Type) serialized[16];
      userInfo.eyesPalette1 = (string) serialized[17];
      userInfo.eyesPalette2 = (string) serialized[18];
      userInfo.accountName = (string) serialized[19];
      userInfo.flagshipId = (int) serialized[20];
      userInfo.gems = (int) serialized[21];
      userInfo.charSpot = (int) serialized[22];

      return userInfo;
   }

   #region Private Variables

   #endregion
}
