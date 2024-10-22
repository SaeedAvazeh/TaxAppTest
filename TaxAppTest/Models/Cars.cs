namespace TaxAppTest.Models
{
    public class Cars
    {
        public int Id { get; set; } 
        public string CarPlate { get; set; }
        public string CarType { get; set; }
        public DateTime CarDatetime {  get; set; }     
        public int CarTax { get; set; }
        public Cars() 
        {
            
        }   
    }
}
