using System.ComponentModel.DataAnnotations;

namespace FipsFrontend.Models;

public class FeedbackSubmissionModel
{
    [Required]
    [StringLength(400, ErrorMessage = "Feedback cannot exceed 400 characters")]
    public string FeedbackFormInput { get; set; } = string.Empty;
}
