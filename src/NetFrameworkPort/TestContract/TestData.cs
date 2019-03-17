using System;

namespace TestContract
{
    [Serializable]
    public class TestData
    {
        public TestData()
        {
            Id = Guid.NewGuid();
            Description = $"Description of {Id.ToString()}";
            Date = DateTime.Now;
        }
        
        public Guid Id { get; set; }
        
        public string Description { get; set; }
        
        public DateTime Date { get; set; }
    }
}