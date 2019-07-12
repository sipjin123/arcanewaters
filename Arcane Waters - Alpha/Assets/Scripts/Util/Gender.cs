using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Gender {
   #region Public Variables

   // The Type of Gender
   public enum Type {  Male = 1, Female = 2 }

   #endregion

   public static List<Gender.Type> getTypes () {
      return new List<Gender.Type>() { Gender.Type.Male, Gender.Type.Female };
   }

   #region Private Variables

   #endregion
}
