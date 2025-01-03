using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AICode.Contracts;

public class UpdateExpenseRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    public string Description { get; set; }
}