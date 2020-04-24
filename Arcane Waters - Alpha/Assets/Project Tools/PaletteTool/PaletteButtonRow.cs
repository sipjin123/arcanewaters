﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PaletteButtonRow : MonoBehaviour {
   #region Public Variables

   // Holds reference to icon image
   public Image icon;

   // Holds reference to palette name UI Text
   public Text paletteName;

   // Holds reference to size UI Text (in texture height)
   public Text size;

   // Holds reference to UI button allowing to edit palette
   public Button editButton;

   // Holds reference to UI button allowing to delete palette
   public Button deleteButton;

   // Index of local cached data about all palettes
   [HideInInspector]
   public int dataIndex;

   #endregion

   #region Private Variables

   #endregion
}
