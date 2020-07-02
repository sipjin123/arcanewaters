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
   public Item armor;
   public Item weapon;
   public Item hat;

   // We have to send these separately because of a Unity serialization bug with class inheritance
   public string armorPalette1;
   public string armorPalette2;
   public string weaponPalette1;
   public string weaponPalette2;

   #endregion

   #region Private Variables

   #endregion
}
