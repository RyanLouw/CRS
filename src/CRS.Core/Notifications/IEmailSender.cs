using CRS.Core.Models.DTO;

namespace CRS.Core.Notifications;

public interface IEmailSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken);
}
