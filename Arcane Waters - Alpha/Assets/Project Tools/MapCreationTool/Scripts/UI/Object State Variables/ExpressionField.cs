using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

namespace MapCreationTool.MapObjectStateVariables
{
   public class ExpressionField : MonoBehaviour
   {
      #region Public Variables

      // Various containers this field can have for child fields
      public Transform[] childExpressionContainers = new Transform[3];

      // Dropdown for selecting the type of expression
      public Dropdown expressionTypeDropdown = null;

      // Input field for setting constant state
      public InputField constantInputField = null;

      // Dropdown for setting the reference to another object
      public Dropdown referenceDropdown = null;

      // Label for setting the operator type
      public Text operatorLabel = null;

      // The ids reference dropdown represents
      public string[] referenceDropdownIds;

      #endregion

      #region Private Variables

      #endregion
   }
}
