using Visiblox.Charts;

namespace IndiaTango.Models
{
    class CustomSelectionBehaviour : BehaviourBase
    {
        private readonly ZoomRectangle _selectionRectangle = new ZoomRectangle();

        public CustomSelectionBehaviour() : base("Selection Behaviour")
        {
        }

        protected override void Init()
        {
            
        }

        public override void DeInit()
        {
            
        }
    }
}
