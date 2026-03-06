namespace NewPlay.ArcadeIdle
{
    [System.Serializable]
    public class RestaurantData
    {
        public string RestaurantID { get; set; }

        public long Money { get; set; }

        public int EmployeeSpeed { get; set; }
        public int EmployeeCapacity { get; set; }
        public int EmployeeAmount { get; set; }
        public int PlayerSpeed { get; set; }
        public int PlayerCapacity { get; set; }
        public int Profit { get; set; }

        public int UnlockCount { get; set; }  // The number of unlockables that have already been purchased.
        public int PaidAmount { get; set; } // The amount of money paid towards the most recent unlockable.
        public bool IsUnlocked { get; set; } // Whether the restaurant is unlocked. This is determined by whether the previous restaurant has been unlocked.

        public RestaurantData(string restaurantID, long money)
        {
            RestaurantID = restaurantID;
            Money = money;
        }
    }
}
