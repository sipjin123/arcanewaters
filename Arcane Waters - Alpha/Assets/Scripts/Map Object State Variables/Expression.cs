using System;
using System.Collections.Generic;

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

      public string evaluate (Dictionary<int, ObjectStateModel> objectModels) {
         return evaluateRecursive(this, objectModels, 0);
      }

      private static string evaluateRecursive (Expression exp, Dictionary<int, ObjectStateModel> objectModels, int searchDepth = 0) {
         if (searchDepth > 100) {
            D.error("CRITICAL ERROR: Exceeded expression eval depth!");
            return "";
         }

         if (!int.TryParse(exp.referenceId, out int referenceId)) {
            referenceId = 0;
         }

         switch (exp.type) {
            case Type.Constant:
               return exp.constant;
            case Type.Reference:
               if (objectModels.TryGetValue(referenceId, out ObjectStateModel model)) {
                  return model.state;
               }
               break;
            case Type.Operation_IF:
               string conString = evaluateRecursive(exp.conditionExpression, objectModels, searchDepth + 1);
               bool con = evaluateBool(conString);
               if (con) {
                  return evaluateRecursive(exp.leftSideExpression, objectModels, searchDepth + 1);
               } else {
                  return evaluateRecursive(exp.rightSideExpression, objectModels, searchDepth + 1);
               }
            case Type.Operation_EQUALS:
               return string.Equals(
                  evaluateRecursive(exp.leftSideExpression, objectModels, searchDepth + 1),
                  evaluateRecursive(exp.rightSideExpression, objectModels, searchDepth + 1))
                  ? "1"
                  : "0";
            case Type.Operation_OR:
               string leftString = evaluateRecursive(exp.leftSideExpression, objectModels, searchDepth + 1);
               string rightString = evaluateRecursive(exp.rightSideExpression, objectModels, searchDepth + 1);
               return (evaluateBool(leftString) || evaluateBool(rightString)) ? "1" : "0";
            case Type.Operation_AND:
               string leftString1 = evaluateRecursive(exp.leftSideExpression, objectModels, searchDepth + 1);
               string rightString1 = evaluateRecursive(exp.rightSideExpression, objectModels, searchDepth + 1);
               return (evaluateBool(leftString1) && evaluateBool(rightString1)) ? "1" : "0";
            case Type.Operation_NOT:
               string leftString2 = evaluateRecursive(exp.leftSideExpression, objectModels, searchDepth + 1);
               return (!evaluateBool(leftString2)) ? "1" : "0";
         }

         return "";
      }

      private static bool evaluateBool (string boolString) {
         if (boolString.ToLower().Equals("false")) {
            return false;
         }
         if (boolString.Equals("0")) {
            return false;
         }
         return true;
      }

      public HashSet<int> findDependantObjectIds () {
         HashSet<int> result = new HashSet<int>();
         findDependantObjectIdsRecursive(this, result, 0);
         return result;
      }

      private static void findDependantObjectIdsRecursive (Expression target, HashSet<int> results, int searchDepth = 0) {
         if (searchDepth > 100) {
            D.error("CRITICAL ERROR: Exceeded expression search depth!");
            return;
         }

         if (target == null || target.type == Type.None || target.type == Type.Constant) {
            return;
         }

         if (int.TryParse(target.referenceId, out int id) && id > 0 && !results.Contains(id)) {
            results.Add(id);
         }

         findDependantObjectIdsRecursive(target.conditionExpression, results, searchDepth + 1);
         findDependantObjectIdsRecursive(target.leftSideExpression, results, searchDepth + 1);
         findDependantObjectIdsRecursive(target.rightSideExpression, results, searchDepth + 1);
      }

      public static bool hasExpressionRecursive (Expression target, Expression searchIn, int searchDepth = 0) {
         if (searchDepth > 100) {
            throw new Exception("CRITICAL ERROR: Exceeded expression search depth!");
         }

         if (target == searchIn) {
            return true;
         }

         if (searchIn == null || searchIn.type == Type.None) {
            return false;
         }

         if (hasExpressionRecursive(target, searchIn.conditionExpression, searchDepth + 1)) {
            return true;
         }

         if (hasExpressionRecursive(target, searchIn.leftSideExpression, searchDepth + 1)) {
            return true;
         }

         if (hasExpressionRecursive(target, searchIn.rightSideExpression, searchDepth + 1)) {
            return true;
         }

         return false;
      }
   }
}
