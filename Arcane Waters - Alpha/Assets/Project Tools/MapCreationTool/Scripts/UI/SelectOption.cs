using System;
using System.Collections.Generic;
using System.Linq;

namespace MapCreationTool
{
   [Serializable]
   public class SelectOption
   {
      public string displayText;
      public string value;

      public SelectOption (string value) {
         displayText = value;
         this.value = value;
      }

      public SelectOption (string value, string displayText) {
         this.displayText = displayText;
         this.value = value;
      }

      public static SelectOption[] formOptions (params string[] options) {
         return options.Select(o => new SelectOption(o)).ToArray();
      }

      public static SelectOption[] formOptions (IEnumerable<string> options) {
         return options.Select(o => new SelectOption(o)).ToArray();
      }

      public SelectOption[] toArray () {
         return new SelectOption[] { this };
      }
   }
}