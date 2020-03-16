using System;
using System.Collections.Generic;

namespace MapCreationTool
{
   public class RemoteData<T>
   {
      public Action OnLoaded;
      public bool loaded { get; protected set; }

      public void load () {
         loaded = false;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            Exception dbEx = null;
            List<T> data = null;
            try {
               data = fetchData();
            } catch (Exception ex) {
               dbEx = ex;
            }
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (dbEx != null) {
                  Utilities.warning($"Failed to fetch data of { GetType().Name } from the database. Exception:\n" + dbEx);
                  UI.errorDialog.display($"Failed to fetch data of { GetType().Name } from the database. Exception:\n" + dbEx);
               } else {
                  try {
                     setData(data);
                  } catch (Exception ex) {
                     Utilities.warning($"Failed to apply retrieved data of { GetType().Name }. Exception:\n" + ex);
                     UI.errorDialog.display($"Failed to apply retrieved data of { GetType().Name }. Exception:\n" + ex);
                  }
               }
            });
         });

         loaded = true;
         OnLoaded?.Invoke();
      }

      protected virtual List<T> fetchData () {
         return null;
      }

      protected virtual void setData (List<T> data) {

      }
   }
}