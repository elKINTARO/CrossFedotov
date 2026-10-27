namespace Core.Events;

// Які зміни стану замовлення вважаються подіями.
public enum OrderChangeKind
{
    OrderCreated,
    LineAdded,
    OrderConfirmed,
    OrderCancelled
}