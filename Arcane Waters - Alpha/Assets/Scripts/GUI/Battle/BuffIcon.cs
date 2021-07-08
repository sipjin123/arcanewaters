using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class BuffIcon : MonoBehaviour {
   #region Public Variables

   // Icon of the buff 
   public Image buffIcon;

   // The buff sprite list
   public List<BuffSpritePair> buffSpritePair = new List<BuffSpritePair>();

   // The current status type
   public Status.Type statusType;

   // Reference to the simple anim component
   public SimpleAnimation simpleAnim;

   #endregion

   #region Private Variables

   #endregion
}

[Serializable]
public class BuffSpritePair {
   // Type of status
   public Status.Type statusType;
 
   // Sprite reference of the status
   public Sprite statusSprite;
}