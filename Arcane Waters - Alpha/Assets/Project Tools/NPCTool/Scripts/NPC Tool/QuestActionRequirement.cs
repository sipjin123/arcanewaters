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

   #endregion

   public QuestActionRequirement () {

   }
}
