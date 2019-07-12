using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class NameManager : MonoBehaviour {
   #region Public Variables

   // The text file with male names
   public TextAsset maleNamesFile;

   // The text file with female names
   public TextAsset femaleNamesFile;

   // A convenient self reference
   public static NameManager self;

   #endregion

   void Awake () {
      self = this;

      // Generate our lists of random names
      _maleNames = getNameList(Gender.Type.Male);
      _femaleNames = getNameList(Gender.Type.Female);

      // Remove any female names that already exist in male names
      HashSet<string> maleSet = new HashSet<string>(_maleNames);
      HashSet<string> femaleSet = new HashSet<string>(_femaleNames);
      femaleSet.ExceptWith(maleSet);
      _femaleNames = new List<string>(femaleSet);
   }

   public string getRandomName (Gender.Type genderType, Area.Type areaType, NPC.Type npcType) {
      // Pick a random NPC name, in such a way that it won't change later on
      int randomInt = ((int) areaType * 50) + (int) npcType;

      // Choose a unique name, and remove it from the list of available names
      if (genderType == Gender.Type.Male) {
         randomInt %= _maleNames.Count;
         return _maleNames[randomInt];
      } else {
         randomInt %= _femaleNames.Count;
         return _femaleNames[randomInt];
      }
   }

   protected List<string> getNameList (Gender.Type genderType) {
      // A text info for the purpose of adjusting string casing
      TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

      // Read in the names using a HashSet to avoid duplicates
      HashSet<string> names = new HashSet<string>();
      TextAsset textAsset = (genderType == Gender.Type.Male) ? maleNamesFile : femaleNamesFile;
      string[] nameArray = textAsset.text.Split('\n');
      foreach (string name in nameArray) {
         names.Add(textInfo.ToTitleCase(name.ToLower().Trim()));
      }

      List<string> list = new List<string>(names);

      return list;
   }

   #region Private Variables

   // A list of male first names to choose from
   protected List<string> _maleNames = new List<string>();

   // A list of female first names to choose from
   protected List<string> _femaleNames = new List<string>();

   #endregion
}
