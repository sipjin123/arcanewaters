using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[CreateAssetMenu(fileName ="BookData.asset", menuName ="Data/Book Data", order = 1)]
public class BookData : ScriptableObject {
   #region Public Variables

   // The book title
   public string title;

   // The book content (raw)
   public string content;

   #endregion

   #region Private Variables

   #endregion
}
