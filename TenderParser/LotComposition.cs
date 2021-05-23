namespace TenderParser
{
    public class LotComposition
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public float Quantity { get; set; }
        public float Price { get; set; }

        public LotComposition() { }

        public override string ToString()
        {
            return $"Name: {Name}, Unit: {Unit}, Quantity: {Quantity}, Price per unit: {Price}";
        }
    }
}
