public class UserSearchResultCollection
{
   #region Public Variables

   // The search that generated this results collection
   public UserSearchInfo searchInfo;

   // The results
   public UserSearchResult[] results;

   // The index of the page returned
   public int page;

   // The total number of pages
   public int totalPages;

   #endregion
}
