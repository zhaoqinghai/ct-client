using CTClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTService
{
    public class MqttRecieveWorker : IRunServer
    {
        const string MQTT_CLIENT_ID = "CTClient";
        private readonly MqttServerSettings _settings;
        private readonly ILogger<MqttRecieveWorker> _logger;

        public event Action<MqttMsgInfo>? NotifyMqttServer;

        public MqttRecieveWorker(IOptions<LocalSettings> settings, ILogger<MqttRecieveWorker> logger)
        {
            _settings = settings.Value.MqttServerSettings;
            _logger = logger;
        }

        public async Task Run()
        {
            var mqttFactory = new MqttFactory();

            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(_settings.ServerIp, _settings.ServerPort).Build();
                mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    try
                    {
                        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                        _logger.LogInformation(string.Concat(e.ApplicationMessage.Topic, " | ", payload));
                        NotifyMqttServer?.Invoke(new MqttMsgInfo(e.ApplicationMessage.Topic, payload));
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }

                    return Task.CompletedTask;
                };
                mqttClient.DisconnectedAsync += async e =>
                {
                    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                    foreach (var topic in _settings.Topics)
                    {
                        await mqttClient.SubscribeAsync(topic);
                    }
                };

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                foreach (var topic in _settings.Topics)
                {
                    await mqttClient.SubscribeAsync(topic);
                }
                await Task.Delay(Timeout.InfiniteTimeSpan);
            }
        }
    }

    public record MqttMsgInfo(string _topic, string _message)
    {
        public string Topic => _topic;

        public string Message => _message;
    }
}