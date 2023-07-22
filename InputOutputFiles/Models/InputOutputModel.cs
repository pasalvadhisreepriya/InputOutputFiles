using System.ComponentModel.DataAnnotations;

namespace InputOutputFiles.Models
{
    public class InputOutputModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
       [Required]
       public string Prompt { get; set; }
        public string Response { get; set; }
    }
}