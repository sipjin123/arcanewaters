using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;

public class QuestActionRequirement {
   #region Public Variables

   [XmlElement("actionType")]
   public int actionTypeIndex;

   [XmlElement("actionTypeTitle")]
   public string actionTitle;

   #endregion

   public QuestActionRequirement () {

   }
}
