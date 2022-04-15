using Prism.Events;
using Trading.UI.Sample.Models;

namespace Trading.UI.Sample.Events
{
    public class ApiConfigurationFinishedEvent : PubSubEvent<ApiConfigurationModel>
    {
    }
}