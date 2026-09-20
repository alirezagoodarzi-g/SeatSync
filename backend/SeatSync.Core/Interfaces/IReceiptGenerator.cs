namespace SeatSync.Core.Interfaces;

public interface IReceiptGenerator
{
    byte[] GenerateBookingReceipt(SeatSync.Core.DTOs.ReceiptData data);
}