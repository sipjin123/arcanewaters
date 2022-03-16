using System;

namespace MapObjectStateVariables
{
   /// <summary>
   /// Defines an expression in the state variables system.
   /// Similar to the notion of 'expression' in programming languages.
   /// </summary>
   [Serializable]
   public class Expression
   {
      // The types of expression we can have
      public enum Type
      {
         None = 0,
         Constant = 1,
         Reference = 2,
         Operation_IF = 3,
         Operation_EQUALS = 4,
         Operation_OR = 5,
         Operation_AND = 6,
         Operation_NOT = 7
      };

      // What type of expression this is
      public Type type;

      // If type is 'constant', this will provide that constant
      public string constant;

      // The condition expression in IF operations
      public Expression conditionExpression;

      // The left side expression where needed
      public Expression leftSideExpression;

      // The right side expression where needed
      public Expression rightSideExpression;

      // The object id we are referencing, if needed
      public string referenceId;
   }
}
