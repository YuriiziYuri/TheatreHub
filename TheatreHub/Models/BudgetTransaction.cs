using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class BudgetTransaction
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть виставу.")]
    public int PerformanceId { get; set; }

    [ValidateNever]
    public Performance Performance { get; set; } = null!;

    public int? PerformanceShowId { get; set; }

    [ValidateNever]
    public PerformanceShow? PerformanceShow { get; set; }

    public int? ProductionItemId { get; set; }

    [ValidateNever]
    public ProductionItem? ProductionItem { get; set; }

    [Required]
    public BudgetTransactionType Type { get; set; } = BudgetTransactionType.Expense;

    [Required]
    public BudgetTransactionCategory Category { get; set; } = BudgetTransactionCategory.Other;

    [Required]
    public BudgetTransactionStatus Status { get; set; } = BudgetTransactionStatus.Planned;

    [Required(ErrorMessage = "Вкажіть назву операції.")]
    [StringLength(200, ErrorMessage = "Назва не може бути довшою за 200 символів.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Опис не може бути довшим за 2000 символів.")]
    public string? Description { get; set; }

    [Range(0, 100000000, ErrorMessage = "Планова сума не може бути від’ємною.")]
    public decimal PlannedAmount { get; set; }

    [Range(0, 100000000, ErrorMessage = "Фактична сума не може бути від’ємною.")]
    public decimal? ActualAmount { get; set; }

    [Required(ErrorMessage = "Вкажіть валюту.")]
    [StringLength(10)]
    public string Currency { get; set; } = "UAH";

    [Required(ErrorMessage = "Вкажіть дату операції.")]
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    public bool IsAutoCalculated { get; set; }

    [Range(0, 1000000, ErrorMessage = "Кількість глядачів не може бути від’ємною.")]
    public int? AudienceCount { get; set; }

    [Range(0, 100000000, ErrorMessage = "Ціна квитка не може бути від’ємною.")]
    public decimal? TicketPrice { get; set; }

    [StringLength(200)]
    public string? ResponsibleName { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}