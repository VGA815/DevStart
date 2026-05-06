using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Payments.Webhooks
{
    public sealed record HandleYookassaWebhookCommand(string Body) : ICommand;
}
