namespace GOGOFOOD.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public string ControllerName { get; set; } = string.Empty;

        public string ActionName { get; set; } = string.Empty;

        public string? SearchText { get; set; }

        public double? MaxDistanceKm { get; set; }

        public string? StatusFilter { get; set; }

        public double? MinTotal { get; set; }

        public double? MaxTotal { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }

        public string? Tab { get; set; }
    }
}