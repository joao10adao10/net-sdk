// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yarp.Kubernetes.Controller.Configuration;
using Yarp.Kubernetes.Controller.Hosting;
using Yarp.Kubernetes.Controller.Rate;

namespace Yarp.Kubernetes.Protocol;

public class Receiver : BackgroundHostedService
{
    private readonly ReceiverOptions _options;
    private readonly Limiter _limiter;
    private readonly IUpdateConfig _proxyConfigProvider;

    public Receiver(
        IOptions<ReceiverOptions> options,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Receiver> logger,
        IUpdateConfig proxyConfigProvider) : base(hostApplicationLifetime, logger)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options.Value;

        // two requests per second after third failure
        _limiter = new Limiter(new Limit(2), 3);
        _proxyConfigProvider = proxyConfigProvider;
    }

    public override async Task RunAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        while (!cancellationToken.IsCancellationRequested)
        {
            await _limiter.WaitAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Logger.LogInformation("Connecting with {ControllerUrl}", _options.ControllerUrl.ToString());

            try
            {
                using var stream = await client.GetStreamAsync(_options.ControllerUrl, cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                using var cancellation = cancellationToken.Register(stream.Close);
                while (true)
                {
                    var json = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrEmpty(json))
                    {
                        break;
                    }

                    var message = System.Text.Json.JsonSerializer.Deserialize<Message>(json);
                    Logger.LogInformation("Received {MessageType} for {MessageKey}", message.MessageType, message.Key);

                    Logger.LogInformation(json);
                    Logger.LogInformation(message.MessageType.ToString());

                    if (message.MessageType == MessageType.Update)
                    {
                        await _proxyConfigProvider.UpdateAsync(message.Routes, message.Cluster, cancellation.Token).ConfigureAwait(false);
                    }
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.LogInformation(ex, "Stream ended");
            }
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }
    }
}
