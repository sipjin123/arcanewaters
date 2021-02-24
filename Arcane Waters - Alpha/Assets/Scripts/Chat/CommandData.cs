using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Text;

public class CommandData {
   #region Public Variables

   #endregion

   public CommandData (string commandString, string description, System.Action<string> command, CommandType requiredPrefix = CommandType.None, List<string> parameterNames = null, List<string> parameterAutocompletes = null) {
      _commandString = commandString;
      _description = description;
      _requiredPrefix = requiredPrefix;
      _parameterNames = parameterNames;
      _parameterAutoCompletes = parameterAutocompletes;
      _stringCommand += command;
   }

   public CommandData (string commandString, string description, System.Action command, CommandType requiredPrefix = CommandType.None, List<string> parameterNames = null, List<string> parameterAutocompletes = null) {
      _commandString = commandString;
      _description = description;
      _requiredPrefix = requiredPrefix;
      _parameterNames = parameterNames;
      _parameterAutoCompletes = parameterAutocompletes;
      _command += command;
   }

   public void invoke (string parameter) {
      if (_requiredPrefix == CommandType.Admin) {
         if (!checkAdminCommand(parameter)) {
            return;
         }
      }

      if (_stringCommand != null && parameter != null) {
         _stringCommand.Invoke(parameter);
      } else {
         _command.Invoke();
      }
   }

   private bool checkAdminCommand (string parameter) {
      return Global.player.admin.checkAdminCommand(getPrefix(), parameter);
   }

   public bool matchesInput (string input, bool mustEqual) {
      if (!input.StartsWith("/")) {
         return false;
      }

      int inputIndex = 0;
      string[] inputParts = input.Split(' ');

      if (inputParts.Length < 1) {
         return false;
      }

      // If there is a required prefix, we will check for it
      if (_requiredPrefix != CommandType.None) {

         if (inputParts.Length < 2) {
            return false;
         }

         bool matchesPrefix = false;
         foreach (string prefix in ChatUtil.commandTypePrefixes[_requiredPrefix]) {
            if (inputParts[0].ToUpper() == prefix.ToUpper()) {
               matchesPrefix = true;
               break;
            }
         }

         if (!matchesPrefix) {
            return false;
         }

         inputIndex = 1;
      }

      // If the strings must be equal
      if (mustEqual) {
         if (!_commandString.ToLower().Equals(inputParts[inputIndex].ToLower())) {
            return false;
         }
      // If it just needs to start with the string
      } else {
         if (!_commandString.ToLower().StartsWith(inputParts[inputIndex].ToLower())) {
            return false;
         }      
      }

      return true;
   }

   public bool containsWholePrefix (string input) {
      // Account for shortened versions of prefix
      if (_requiredPrefix != CommandType.None) {
         List<string> possibleRequiredPrefixes = ChatUtil.commandTypePrefixes[_requiredPrefix];

         foreach (string prefix in possibleRequiredPrefixes) {
            if (input.ToLower().Contains((prefix + " " + _commandString).ToLower())) {
               return true;
            }
         }

      } else {
         return (input.ToLower().Contains(getPrefix().ToLower()));
      }

      return false;
   }

   public string getParameters () {
      StringBuilder parameters = new StringBuilder();

      if (_parameterNames != null) {
         foreach (string param in _parameterNames) {
            parameters.Append(" [");
            parameters.Append(param);
            parameters.Append("],");
         }
      }

      return parameters.ToString();
   }

   public List<string> getParameterAutoCompletes () {
      if (_parameterAutoCompletes == null) {
         return new List<string>();
      }

      return _parameterAutoCompletes;
   }

   public List<string> getParameterAutoCompletes (string input) {
      List<string> autoCompletes = new List<string>();

      if (_parameterAutoCompletes == null) {
         return autoCompletes;
      }

      foreach (string autoComplete in _parameterAutoCompletes) {
         if (autoComplete.ToLower().StartsWith(input.ToLower())) {
            autoCompletes.Add(autoComplete);
         }
      }

      return autoCompletes;
   }

   public string getCommandInfo () {
      return getPrefix() + getParameters();
   }

   public string getDescription () {
      return getPrefix() + " - " + _description;
   }

   public string getRequiredPrefix () {
      string prefix = "";

      if (_requiredPrefix != CommandType.None) {
         prefix += ChatUtil.commandTypePrefixes[_requiredPrefix][0];
      }

      return prefix;
   }

   public string getPrefix () {
      string prefix = getRequiredPrefix();

      if (prefix != "") {
         prefix += " ";
      }

      prefix += _commandString;

      return prefix;
   }

   #region Private Variables

   // The string that represents this command
   protected string _commandString;

   // The description of what this command does
   protected string _description;

   // The action executed when this command is run, if it has parameters
   protected System.Action<string> _stringCommand;

   // The action executed when this command is run, if it has no parameters
   protected System.Action _command;

   // The required prefix for this command, if not 'None', this  command won't show up until the prefix is entered
   protected CommandType _requiredPrefix;

   // The names of all parameters for this command
   protected List<string> _parameterNames;

   // Autocompletes to display for the parameters of this command
   protected List<string> _parameterAutoCompletes = new List<string>();

   #endregion
}
