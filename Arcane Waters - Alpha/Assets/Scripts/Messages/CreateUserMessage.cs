using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CreateUserMessage : NetworkMessage
{
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The User Info
   public UserInfo userInfo;

   // The armor type we'll start with
   public int armorType;
   public string armorPalettes;

   // The spot for this character
   public int characterSpot;

   // The perks for this user
   public Perk[] perks;

   // Client's machine identifier
   public string machineIdentifier;

   // False if the player has already logged in successfully.
   public bool isFirstLogin;

   // The steam user id of the player
   public string steamUserId = "";

   #endregion

   public CreateUserMessage () { }

   public CreateUserMessage (uint netId) {
      this.netId = netId;
   }

   public CreateUserMessage (uint netId, UserInfo userInfo, int armorType, string armorPalettes, Perk[] perks, string machineIdentifier, bool isFirstLogin, string steamUserId) {
      this.netId = netId;
      this.userInfo = userInfo;
      this.armorType = armorType;
      this.armorPalettes = armorPalettes;
      this.characterSpot = userInfo.charSpot;
      this.perks = perks;
      this.machineIdentifier = machineIdentifier;
      this.isFirstLogin = isFirstLogin;
      this.steamUserId = steamUserId;
   }
}