using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif
using System;

public class Metric
{
   #region Public Variables

   // The id of the metric
   public int id;

   // The name of the metric
   public string name;

   // The value of the metric
   public string value;

   // The identifier of the machine this metric was gathered from
   public string machineId;

   // The identifier of the process this metric was gathered from
   public string processId;

   // The name of the process this metric was gathered from
   public string processName;

   // The description of the setting
   public string description;

   // Last Update Time
   public DateTime timestamp;

   // The type of the value
   public MetricValueType valueType;

   // Represents the type of a Metric
   public enum MetricValueType
   {
      // Used to represent any custom data type
      OTHER = 0,

      // Integer type
      INT = 1,

      // Boolean type
      BOOL = 2,

      // Float/Double type
      FLOAT = 3,

      // String type
      STRING = 4,

      // Unsigned integer type
      UINT = 5
   }

   #endregion

   public Metric () { }

   #if IS_SERVER_BUILD

   public static Metric create (MySqlDataReader dataReader) {
      Metric metric = new Metric();
      try {
         metric.id = DataUtil.getInt(dataReader, "id");
         metric.name = DataUtil.getString(dataReader, "metName");
         metric.value = DataUtil.getString(dataReader, "metValue");
         metric.description = DataUtil.getString(dataReader, "metDescription");
         metric.machineId = DataUtil.getString(dataReader, "metMachineId");
         metric.processId = DataUtil.getString(dataReader, "metProcessId");
         metric.processName = DataUtil.getString(dataReader, "metProcessName");
         metric.timestamp = DataUtil.getDateTime(dataReader, "metTimestamp");
         metric.valueType = (MetricValueType) DataUtil.getInt(dataReader, "metValueType");
      } catch (Exception ex) {
         D.debug("Error in parsing MySqlData for Metric " + ex.ToString());
         return null;
      }
      return metric;
   }

   #endif

   public static Metric create (int id, string metName, string metValue, string metDescription, string metMachineId, string metProcessId, string metProcessName, MetricValueType metValueType) {
      Metric metric = new Metric();
      metric.id = id;
      metric.name = metName;
      metric.value = metValue;
      metric.description = metDescription;
      metric.machineId = metMachineId;
      metric.processId = metProcessId;
      metric.processName = metProcessName;
      metric.valueType = metValueType;
      metric.timestamp = DateTime.UtcNow;
      return metric;
   }

   #region Private Variables

   #endregion
}
