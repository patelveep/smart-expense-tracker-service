using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AICode.Contracts
{
    public class ExpenseModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int UserId { get; set; } // Foreign key to User table

        public int CategoryId { get; set; } // Foreign key to Category table
        public string CategoryName { get; set; } // Foreign key to Category table

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
