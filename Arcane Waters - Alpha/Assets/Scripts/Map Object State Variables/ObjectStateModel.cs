using System;
using Newtonsoft.Json;

namespace MapObjectStateVariables
{
   /// <summary>
   /// Used to define how and object reacts to state changes in a scene
   /// </summary>
   [Serializable]
   public class ObjectStateModel
   {
      // Unique ID for a prefab (only within it's area) provided by map editor
      public string mapEditorId;

      // The current or initial state of an object
      public string state;

      // How our state reacts to other objects' state changes
      public Expression stateExpression;

      public static ObjectStateModel deserializeOrDefault (string data) {
         ObjectStateModel result = null;

         try {
            result = JsonConvert.DeserializeObject<ObjectStateModel>(data);
         } catch { }

         if (result == null) {
            result = new ObjectStateModel {
               state = "",
               stateExpression = new Expression {
                  type = Expression.Type.Constant,
                  constant = ""
               }
            };
         }

         return result;
      }

      public string serialize () {
         return JsonConvert.SerializeObject(this);
      }
   }
}
