using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using MapCreationTool.Serialization;

public class GraveToolTip : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The message upon hover
   public string toolTipMessage;

   // The tooltip panel
   public GameObject toolTipPanel;

   // The text of the tooltip
   public TextMeshProUGUI toolTipText;

   // The message
   public static string MESSAGE = "Here lies ";

   #endregion

   protected virtual void Awake () {
      toolTipText.SetText(toolTipMessage);
   }

   public virtual void toggleToolTip (bool isActive) {
      toolTipPanel.SetActive(isActive);
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.GRAVE_NAME) == 0) {
            string graveName = field.v.Split(':')[0];
            toolTipMessage = MESSAGE + graveName;
            toolTipText.SetText(toolTipMessage);
         }
      }
   }

   #region Private Variables

   #endregion
}
