﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PerkToolListTemplate : GenericEntryTemplate
{
   #region Public Variables

   #endregion

   private void OnEnable () {
      setNameRestriction(nameText.text);
   }

   #region Private Variables

   #endregion
}
