﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class BooksManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static BooksManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   #region Private Variables
   
   #endregion
}