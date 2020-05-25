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
   public int armorType;
   public string armorPalette1;
   public string armorPalette2;

   // The spot for this character
   public int characterSpot;

   // The perks for this user
   public int[] perkAnswers;

   #endregion

   public CreateUserMessage () { }

   public CreateUserMessage (uint netId) {
      this.netId = netId;
   }

   public CreateUserMessage (uint netId, UserInfo userInfo, int armorType, string armorPalette1, string armorPalette2, List<int> perks) {
      this.netId = netId;
      this.userInfo = userInfo;
      this.armorType = armorType;
      this.armorPalette1 = armorPalette1;
      this.armorPalette2 = armorPalette2;
      this.characterSpot = userInfo.charSpot;
      this.perkAnswers = perks.ToArray();
   }
}