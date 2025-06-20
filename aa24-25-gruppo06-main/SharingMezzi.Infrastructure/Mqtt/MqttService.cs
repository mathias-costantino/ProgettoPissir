using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using SharingMezzi.Core.Interfaces.Services;

namespace SharingMezzi.Infrastructure.Mqtt
{
    public class MqttService : IMqttService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;

        public MqttService(string server, int port, string clientId)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            
            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(server, port)
                .WithClientId(clientId)
                .WithCleanSession()
                .Build();
        }

        public bool IsConnected => _mqttClient.IsConnected;

        public async Task ConnectAsync()
        {
            if (!_mqttClient.IsConnected)
            {
                await _mqttClient.ConnectAsync(_options);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }
        }

        public async Task PublishAsync<T>(string topic, T message)
        {
            var json = JsonSerializer.Serialize(message);
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(json))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage);
        }

        public async Task SubscribeAsync(string topic, Func<string, Task> messageHandler)
        {
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                if (e.ApplicationMessage.Topic == topic)
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    await messageHandler(payload);
                }
            };

            await _mqttClient.SubscribeAsync(topic);
        }
    }
}
