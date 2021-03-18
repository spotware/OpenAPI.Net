using Prism.Events;
using Trading.UI.Demo.Models;

namespace Trading.UI.Demo.Events
{
    public class AccountChangedEvent : PubSubEvent<AccountModel>
    {
    }
}