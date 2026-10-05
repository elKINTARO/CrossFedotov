namespace Core.Domain;

// дод 3 - явний стан замовлення замість набору булевих прапорців.
public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled
}