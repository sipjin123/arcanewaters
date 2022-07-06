using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GroupMemberCellInfo
{
   #region Public Variables

   // The user id
   public int userId;

   // The user name
   public string userName;

   // The user XP
   public int userXP;

   // The user area key
   public string areaKey;

   // The user gender
   public Gender.Type gender;

   // The user body type
   public BodyLayer.Type bodyType;

   // The user eyes type
   public EyesLayer.Type eyesType;

   // The user hair type
   public HairLayer.Type hairType;

   // The eyes palettes
   public string eyesPalettes;

   // The hair palettes
   public string hairPalettes;

   // The equipped armor
   public Item armor;

   // The equipped weapon
   public Item weapon;

   // The equipped hat
   public Item hat;

   #endregion

   public GroupMemberCellInfo () { }

   public GroupMemberCellInfo (UserObjects userObjects) {
      this.userId = userObjects.userInfo.userId;
      this.userName = userObjects.userInfo.username;
      this.userXP = userObjects.userInfo.XP;
      this.areaKey = userObjects.userInfo.areaKey;
      this.gender = userObjects.userInfo.gender;
      this.bodyType = userObjects.userInfo.bodyType;
      this.eyesType = userObjects.userInfo.eyesType;
      this.hairType = userObjects.userInfo.hairType;
      this.eyesPalettes = userObjects.userInfo.eyesPalettes;
      this.hairPalettes = userObjects.userInfo.hairPalettes;
      this.armor = userObjects.armor;
      this.weapon = userObjects.weapon;
      this.hat = userObjects.hat;
   }

   #region Private Variables

   #endregion
}