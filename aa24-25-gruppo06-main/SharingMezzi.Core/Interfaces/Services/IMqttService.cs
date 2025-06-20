namespace SharingMezzi.Core.Interfaces.Services
{
    public interface IMqttService
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        Task PublishAsync<T>(string topic, T message);
        Task SubscribeAsync(string topic, Func<string, Task> messageHandler);
        bool IsConnected { get; }
    }
}