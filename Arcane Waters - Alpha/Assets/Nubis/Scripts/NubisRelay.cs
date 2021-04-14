﻿//#define NUBIS
#if NUBIS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static NubisLogger;

public class NubisRelay
{

   private static string getFunctionNameFromUri (Uri uri) {
      if (uri == null) return string.Empty;
      if (uri.Segments == null || uri.Segments.Length < 3) return string.Empty;
      string funcName = uri.Segments[2].Replace("/", "");
      return System.Net.WebUtility.UrlDecode(funcName);
   }

   private static Dictionary<string, string> getArgumentsFromUri (Uri uri) {
      if (uri == null) return new Dictionary<string, string>();
      if (string.IsNullOrEmpty(uri.Query)) return new Dictionary<string, string>();
      string[] keyValuePairs = uri.Query.Substring(1).Split(new[] { '&' });
      Dictionary<string, string> arguments = new Dictionary<string, string>();
      foreach (string keyValuePair in keyValuePairs) {
         if (string.IsNullOrEmpty(keyValuePair)) continue;
         string[] keyValue = keyValuePair.Split(new[] { '=' });
         if (keyValue == null || keyValue.Length < 2) continue;
         string key = keyValue[0];
         string value = keyValue[1];
         key = System.Net.WebUtility.UrlDecode(key);
         value = System.Net.WebUtility.UrlDecode(value);
         arguments.Add(key, value);
      }
      return arguments;
   }

   private static string callImpl (string function, params string[] args) {

      if (string.Equals(function, "NubisDirect-getUserInventoryPage", StringComparison.OrdinalIgnoreCase)) {
         return NubisDirect.getUserInventoryPage(args[0], args[1], args[2], args[3], args[4]);
      }
      // TODO: Ken: Any function that needs to be called directly should be added here.

      string result = string.Empty;
      string argString = (args != null && args.Length > 0) ? string.Join(",", args) : string.Empty;
      i($"Invoking '{TYPE_NAME}.{function}({argString})' ...");
      try {
         if (!_methodCache.ContainsKey(function)) {
            _assembly = _assembly ?? Assembly.GetExecutingAssembly();
            if (_assembly != null) {
               _type = _type ?? _assembly.GetType(TYPE_NAME);
               if (_type != null) {
                  MethodInfo _methodInfo = _type.GetMethod(function);
                  if (_methodInfo != null) {
                     _methodCache.Add(function, _methodInfo);
                  } else {
                     i($"Invoking '{TYPE_NAME}.{function}({argString})' : FAILED - Couldn't locate method.");
                  }
               } else {
                  i($"Invoking '{TYPE_NAME}.{function}({argString})' : FAILED - Couldn't locate type.");
               }
            } else {
               i($"Invoking '{TYPE_NAME}.{function}({argString})' : FAILED - Couldn't locate assembly.");
            }
         }

         if (_methodCache.ContainsKey(function)) {
            MethodInfo methodInfo = _methodCache[function];
            if (methodInfo != null) {
               result = methodInfo.Invoke(null, args) as string;
               if (result != null) {
                  i($"Invoking '{TYPE_NAME}.{function}({argString})' : OK");
               } else {
                  i($"Invoking '{TYPE_NAME}.{function}({argString})' : FAILED - The call to the method returned null.");
               }
            }
         }
      } catch (Exception ex) {
         i($"Invoking '{TYPE_NAME}.{function}({argString})' : FAILED - There was an exception.");
         e(ex);
      }
      return result;
   }

   public static string call (string url) {
      var start = DateTime.Now;
      i($"New url received. url: '{url}");
      Uri uri = new Uri(url);
      string function = getFunctionNameFromUri(uri);
      Dictionary<string, string> arguments = getArgumentsFromUri(uri);
      string[] argsValues = new string[arguments.Values.Count];
      arguments.Values.CopyTo(argsValues, 0);
      string result = callImpl(function, argsValues);
      return result;
   }


   #region Private Variables

   // The name of the type to delegate calls to.
   private const string TYPE_NAME = "DB_Main";

   // Reference to current assembly.
   private static Assembly _assembly;

   // Reference to Type
   private static Type _type;

   // Method cache
   //private static Dictionary<string, Delegate> _methodCache = new Dictionary<string, Delegate>();
   private static Dictionary<string, MethodInfo> _methodCache = new Dictionary<string, MethodInfo>();

   #endregion
}
#endif

