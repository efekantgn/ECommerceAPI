namespace ProductService.Models
{
    public class UpdateStockRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
