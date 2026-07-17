namespace TheatreHub.Models.Enums;

public enum BudgetTransactionCategory
{
    // Доходи
    Tickets = 0,
    InstitutionPayment = 1,
    Donation = 2,
    Grant = 3,
    Partnership = 4,

    // Витрати
    Props = 20,
    Costumes = 21,
    Makeup = 22,
    Decorations = 23,
    Rent = 24,
    Transport = 25,
    Fees = 26,
    Marketing = 27,
    Equipment = 28,

    Other = 99
}