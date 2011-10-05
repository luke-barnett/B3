namespace IndiaTango.ViewModels
{
    class UseSelectedRangeViewModel : BaseViewModel
    {
        public string Title { get { return "Would you like to use?"; } }

        public bool UseSelectedRange { get; private set; }

        public void Yes()
        {
            UseSelectedRange = true;
            TryClose();
        }

        public void No()
        {
            UseSelectedRange = false;
            TryClose();
        }
    }
}
