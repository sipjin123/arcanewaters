using System;
using UnityEngine;

namespace ItemDefinitionTool
{
   public abstract class TypeSpecificAttributes : MonoBehaviour
   {
      #region Public Variables

      // Type that can be altered by this section
      public abstract Type targetType { get; }

      #endregion

      public abstract void setValuesWithoutNotify (ItemDefinition itemDefinition);

      public abstract void applyAttributeValues (ItemDefinition target);

      #region Private Variables

      #endregion
   }
}