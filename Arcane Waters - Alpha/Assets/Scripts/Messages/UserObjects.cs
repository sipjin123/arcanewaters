using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class UserObjects {
   #region Public Variables

   // The various objects that we want to look up all at the same time during the login process
   public int accountId;
   public string accountEmail;
   public bool isSinglePlayer;
   public long accountCreationTime;
   public UserInfo userInfo;
   public ShipInfo shipInfo;
   public GuildInfo guildInfo;
   public GuildRankInfo guildRankInfo;
   public Item armor;
   public Item weapon;
   public Item hat;
   public Item ring;
   public Item necklace;
   public Item trinket;

   // We have to send these separately because of a Unity serialization bug with class inheritance
   public string armorPalettes = "";
   public string weaponPalettes = "";
   public string ringPalettes = "";
   public string trinketPalettes = "";
   public string necklacePalettes = "";

   #endregion

   #region Private Variables

   #endregion
}
