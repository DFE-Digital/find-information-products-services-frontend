using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models;

public class FeedbackSubmissionModel
{
    [Required]
    [StringLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters")]
    public string FeedbackFormInput { get; set; } = string.Empty;
}
