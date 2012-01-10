using System.Windows;
using System.Windows.Controls;

namespace IndiaTango.Models
{
    public class CustomTabItem : TabItem
    {
        public static readonly RoutedEvent WasSelectedEvent = EventManager.RegisterRoutedEvent("WasSelected",
                                                                                               RoutingStrategy.Bubble,
                                                                                               typeof (
                                                                                                   RoutedEventHandler),
                                                                                               typeof (CustomTabItem));

        public event RoutedEventHandler WasSelected
        {
            add { AddHandler(WasSelectedEvent, value);}
            remove { RemoveHandler(WasSelectedEvent, value);}
        }

        private void RaiseWasSelectedEvent()
        {
            var newEventArgs = new RoutedEventArgs(WasSelectedEvent);
            RaiseEvent(newEventArgs);
        }

        protected override void OnSelected(System.Windows.RoutedEventArgs e)
        {
            base.OnSelected(e);
            RaiseWasSelectedEvent();
        }
    }
}
