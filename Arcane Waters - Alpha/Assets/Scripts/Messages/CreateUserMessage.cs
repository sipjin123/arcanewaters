using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CreateUserMessage : MessageBase {
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The User Info
   public UserInfo userInfo;

   // The armor type we'll start with
   public Armor.Type armorType;
   public ColorType armorColor1;
   public ColorType armorColor2;

   // The spot for this character
   public int characterSpot;

   #endregion

   public CreateUserMessage () { }

   public CreateUserMessage (uint netId) {
      this.netId = netId;
   }

   public CreateUserMessage (uint netId, UserInfo userInfo, Armor.Type armorType, ColorType armorColor1, ColorType armorColor2) {
      this.netId = netId;
      this.userInfo = userInfo;
      this.armorType = armorType;
      this.armorColor1 = armorColor1;
      this.armorColor2 = armorColor2;
      this.characterSpot = userInfo.charSpot;
   }
}