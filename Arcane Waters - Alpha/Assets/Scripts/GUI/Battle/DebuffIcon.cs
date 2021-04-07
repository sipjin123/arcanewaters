using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class DebuffIcon : MonoBehaviour {
   #region Public Variables

   // Icon of the debuff 
   public Image debuffIcon;

   // The debuff sprite list
   public List<DebuffSpritePair> debuffSpritePair = new List<DebuffSpritePair>();

   // The current status type
   public Status.Type statusType;

   #endregion

   #region Private Variables

   #endregion
}

[Serializable]
public class DebuffSpritePair {
   // Type of status
   public Status.Type statusType;
 
   // Sprite reference of the status
   public Sprite statusSprite;
}