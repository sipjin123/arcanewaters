using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MSVScratchpad : MonoBehaviour {
   #region Public Variables
      
   #endregion

   #region Private Variables
      
   #endregion
}
//Lever 7
//Lever 8
//Lightbulb 15


//When lever 7 is open,
//Lever 8 closes
//Lightbulb 15 turns off

//When lever 8 opens,
//lever 7 closes,
//lightbulb 15 turns on

//When lever 7 changes value,
//counter++

//When level 8 changes value,
//counter--


// 3 stage lever, only 1 stage turns lightbulb on

// 2 levers, 

// Lever has a state
// Lever's state can react to things
// Lever can change it's own state

namespace MapStateVariables
{
   public class MapStateVariablesManager
   {
      bool wereChanges = false;

      public void stateChanged (int objectId, string newState) {
         wereChanges = true;
      }

      private void Update () {
         if (wereChanges) {
            wereChanges = false;
            recalculateModel();
         }
      }

      private void recalculateModel () {

      }
   }

   public class ObjectWithState// : MonoBehaviour
   {
      int mapEditorId;
      private State state;
      private Expression stateExpression; // How our state reacts to other objects' state changes
   }

   public class State
   {
      public Type type;
      public string value;

      public enum Type
      {
         None = 0,
         String = 1,
         Bool = 2,
         Float = 3,
         Int = 4
      }
   }

   public class Expression
   {
      public enum Type { 
         None = 0, 
         Constant = 1, 
         Reference = 2,
         Operation_OR,
         Operation_NOT
      };

      public Type type;
      public State constant;

      public int leftObjectId;
      public int rightObjectId;
   }
}

class LeverDataDefinition
{
   
}

class Lever
{
   public string state;




   public string controlsVariable;

   bool _isOn = false;

   void toggle () {
      _isOn = !_isOn;
   }

   void setIsOn () {

   }
}