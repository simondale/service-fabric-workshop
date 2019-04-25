using System;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        [Display(Name="Name")]
        [Required]
        [DataType(DataType.Text)]
        public string Name { get; set; }
        [Display(Name="Price")]
        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }
    }
}
