using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class MaterialManager : MonoBehaviour {
   #region Public Variables

   public Material paletteMaterial;
   public Material paletteMaterialGUI;

   // Self
   public static MaterialManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public Material get () {
      return paletteMaterial;
   }

   public Material getGUIMaterial () {
      return paletteMaterialGUI;
   }

   #region Private Variables

   #endregion
}
