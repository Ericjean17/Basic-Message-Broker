using System.ComponentModel.DataAnnotations;

namespace MessageBroker.Models 
{
  public class Topic
  {
    [Key] // primary key
    public int Id { get; set; }
    [Required] // cannot be null when saving to database
    public int MyProperty { get; set; }
  }
}