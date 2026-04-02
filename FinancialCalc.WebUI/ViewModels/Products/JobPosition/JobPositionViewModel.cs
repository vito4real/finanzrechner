using FinancialCalc.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinancialCalc.WebUI.ViewModels.Products.JobPosition
{
    public class JobPositionViewModel
    {
        public Guid Id {  get; set; }

        [Required(ErrorMessage="Введите название должности")]
        [Display(Name = "Название должности")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Укажите базовую ставку")]
        [Range(0.01, 10000, ErrorMessage = "Ставка должна быть больше нуля")]
        [Display(Name ="Базовая ставка (руб/час)")]
        public decimal BaseHourlyRate { get; set; }

        [Required(ErrorMessage = "Выберите категорию тяжести")]
        [Display(Name = "Категория тяжести (Охрана труда)")]
        public SeverityCategory Severity {  get; set; }

        public decimal FinalRate { get; set; }
        public string? SeverityDisplay { get; set; }
    }
}
