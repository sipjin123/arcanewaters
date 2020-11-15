using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[CreateAssetMenu(fileName = "New animal petting config", menuName = "Data/Animal petting config")]
public class AnimalPettingConfig : ScriptableObject
{
   #region Public Variables

   public List<string> heartReaction = new List<string>();
   public List<string> angryReaction = new List<string>();
   public List<string> confusedReaction = new List<string>();

   #endregion

}
