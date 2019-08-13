using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class DirectionalArrow
{
   // Which direction the arrow should show
   public Direction direction;

   // The arrow object
   public GameObject gameObj;
}