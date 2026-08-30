using NextDrop.Modules.Notifications.Application.Abstractions;
using NextDrop.Modules.Notifications.Domain.Aggregates;

namespace NextDrop.Modules.Notifications.Application.Services;

public class SimpleTemplateRenderer : INotificationTemplateRenderer
{
    public (string Title, string Body) Render(NotificationTemplate template, IDictionary<string, string> variables)
    {
        var title = template.TitleTemplate;
        var body = template.BodyTemplate;

        if (variables != null)
        {
            foreach (var (key, value) in variables)
            {
                var placeholder = "{" + key + "}";
                title = title.Replace(placeholder, value ?? string.Empty);
                body = body.Replace(placeholder, value ?? string.Empty);
            }
        }

        return (title, body);
    }
}
