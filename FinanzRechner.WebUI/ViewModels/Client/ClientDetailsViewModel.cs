namespace FinanzRechner.WebUI.ViewModels.Client
{
    public class ClientDetailsViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public List<ClientOrderViewModel> Orders { get; set; } = new();
    }
}
