using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using Crosstales.BWF.Manager;
using System.Text.RegularExpressions;

public class NameUtil {
   #region Public Variables

   // Max length of character names
   public static int MAX_NAME_LENGTH = 12;

   #endregion

   public static bool isValid (string name, bool allowSpaces = false) {
      // Can't be null or empty
      if (Util.isEmpty(name)) {
         return false;
      }

      // Can't be too short or too long
      if (name.Length <= 0 || name.Length > MAX_NAME_LENGTH) {
         return false;
      }

      // No bogus characters
      if (allowSpaces) {
         string pattern = @"^[0-9A-Za-z ]+$";
         Regex regex = new Regex(pattern);
         if (!regex.IsMatch(name)) {
            return false;
         }

      } else {
         if (!name.All(char.IsLetterOrDigit)) {
            return false;
         }
      }

      // No bad words
      if (BadWordManager.Contains(name)) {
         return false;
      }

      return true;
   }

   #region Private Variables

   #endregion
}
