using System;
using System.Windows.Forms;
using Caliburn.Micro;
using IndiaTango.Models;
using Visiblox.Charts;
using Visiblox.Charts.Primitives;

namespace IndiaTango.ViewModels
{
    class GraphViewModel : BaseViewModel
    {
        private readonly SimpleContainer _container;
        private readonly IWindowManager _windowManager;

        public GraphViewModel(IWindowManager windowManager, SimpleContainer container)
        {
            _windowManager = windowManager;
            _container = container;
            var b = new BehaviourManager();
            b.AllowMultipleEnabled = true;
            b.Behaviours.Add(new ZoomBehaviour(){ IsEnabled = true});
            b.Behaviours.Add(new PanBehaviour(){IsEnabled = true});
            b.Behaviours.Add(new TrackballBehaviour(){IsEnabled = true});
            Behaviour = b;
        }
        
        private DataSeries<DateTime, float> _chartSeries = new DataSeries<DateTime, float>();
        public DataSeries<DateTime, float> ChartSeries { get { return _chartSeries; } set { _chartSeries = value; NotifyOfPropertyChange("ChartSeries"); } }

        public string ChartTitle { get; set; }
        public string YAxisTitle { get; set; }

        private DoubleRange _range = new DoubleRange(0,40);
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        private IBehaviour _behaviour = new BehaviourManager();
        public IBehaviour Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }
    }
}
