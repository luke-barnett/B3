using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace IndiaTango.Models
{
    /// <summary>
    /// Taken from
    /// http://www.felicepollano.com/CommentView.aspx?guid=23695e9b-819c-43a8-96ae-8bcd04b55854#commentstart
    /// </summary>
    public class InputBindingTrigger : TriggerBase<FrameworkElement>, ICommand
    {
        public InputBinding InputBinding
        {
            get { return (InputBinding)GetValue(InputBindingProperty); }
            set { SetValue(InputBindingProperty, value); }
        }
        public static readonly DependencyProperty InputBindingProperty =
            DependencyProperty.Register("InputBinding", typeof(InputBinding)
            , typeof(InputBindingTrigger)
            , new UIPropertyMetadata(null));
        protected override void OnAttached()
        {
            if (InputBinding != null)
            {
                InputBinding.Command = this;
                AssociatedObject.InputBindings.Add(InputBinding);
            }
            base.OnAttached();
        }

        #region ICommand Members
        public bool CanExecute(object parameter)
        {
            // action is anyway blocked by Caliburn at the invoke level
            return true;
        }
        public event EventHandler CanExecuteChanged = delegate { };

        public void Execute(object parameter)
        {
            InvokeActions(parameter);
        }

        #endregion
    }
}
