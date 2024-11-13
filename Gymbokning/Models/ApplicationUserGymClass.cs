namespace Gymbokning.Models
{
    public class ApplicationUserGymClass
    {
        // primary key
        public string ApplicationUserId { get; set; }
        public int GymClassId { get; set; }


        // nav
        public ApplicationUser ApplicationUser { get; set; }
        public GymClass GymClass { get; set; }
    }
}
