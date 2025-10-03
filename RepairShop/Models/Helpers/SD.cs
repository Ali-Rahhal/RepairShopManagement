namespace RepairShop.Models.Helpers
{
    public static class SD
    {
        public const string Role_Admin = "Admin";
        public const string Role_User = "User";

        public const string Status_Job_New = "New";
        public const string Status_Job_InProgress = "InProgress";
        public const string Status_Job_Completed = "Completed";
        public const string Status_Job_Cancelled = "Cancelled";

        public const string Status_Part_Pending_Repair = "PendingForRepair";
        public const string Status_Part_Pending_Replace = "PendingForReplacement";
        public const string Status_Part_Fixed = "Fixed";
        public const string Status_Part_Replaced = "Replaced";
        public const string Status_Part_NotRepairable = "NotRepairable";
        public const string Status_Part_NotReplaceable = "NotReplaceable";
    }
}
