using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CTService
{
    public class RemindSpeechWorker(ILogger<RemindSpeechWorker> _logger, [FromKeyedServices(RemindSpeechWorker.REMIND_SPEECH_CHANNEL)] Channel<string> _channel) : IRunServer
    {
        public const string REMIND_SPEECH_CHANNEL = "REMIND_SPEECH";
        private readonly SpeechSynthesizer _speech = new SpeechSynthesizer();

        private DateTime _lastSpeechTime;

        private string? _lastSpeechWord;

        public async Task Run()
        {
            await foreach (var remind in _channel.Reader.ReadAllAsync())
            {
                if ((DateTime.Now - _lastSpeechTime).TotalSeconds < 1 && string.Equals(remind, _lastSpeechWord))
                {
                    continue;
                }
                _logger.LogInformation("start speech {0}", remind);
                _lastSpeechWord = remind;
                for (var i = 0; i < 3; i++)
                {
                    _speech.Speak(remind);
                    await Task.Delay(500);
                    _lastSpeechTime = DateTime.Now;
                }
            }
        }
    }
}