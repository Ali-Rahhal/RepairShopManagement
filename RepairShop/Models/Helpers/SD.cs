namespace RepairShop.Models.Helpers
{
    public static class SD
    {
        public const string Role_Admin = "Admin";
        public const string Role_User = "User";

        public const string Status_Job_New = "New";
        public const string Status_Job_InProgress = "InProgress";
        public const string Status_Job_Completed = "Completed";
        public const string Status_Job_OutOfService = "OutOfService";
        public const string Status_Job_Delivered = "Delivered";
        public const string Status_Job_Processed = "Processed";

        public const string Status_Part_Pending_Repair = "PendingForRepair";
        public const string Status_Part_Pending_Replace = "PendingForReplacement";
        public const string Status_Part_Waiting_Part = "WaitingForPart";
        public const string Status_Part_Fixed = "Fixed";
        public const string Status_Part_Replaced = "Replaced";
        public const string Status_Part_NotRepairable = "NotRepairable";
        public const string Status_Part_NotReplaceable = "NotReplaceable";

        public const string Status_DU_Reported = "Reported";
        public const string Status_DU_UnderRepair = "UnderRepair";
        public const string Status_DU_Fixed = "Fixed";
        public const string Status_DU_OutOfService = "OutOfService";
    }
}
