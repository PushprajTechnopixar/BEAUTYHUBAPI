using System.Threading.Tasks;

namespace BeautyHubAPI.Firebase
{
    public interface IMobileMessagingClient
    {
        Task<string> SendNotificationAsync(string token, string title, string body);
    }
}
