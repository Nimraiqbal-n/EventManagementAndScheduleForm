using System;
using System.ComponentModel.DataAnnotations;

namespace FACULTY_PORTAL.Models
{
    public class VCCommitment
    {
        [Required(ErrorMessage = "Event date is required.")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Activity is required.")]
        public string Activity { get; set; }

        [Required(ErrorMessage = "Responsible person is required.")]
        public string Responsible { get; set; }

        [Required(ErrorMessage = "Start time is required.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Venue is required.")]
        public string Venue { get; set; }

        [Required(ErrorMessage = "Members are required.")]
        public string Members { get; set; }
    }
}
