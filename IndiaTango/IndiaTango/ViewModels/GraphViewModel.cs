using System;
using System.Linq;
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

        private string _chartTitle = String.Empty;
        public string ChartTitle { get { return _chartTitle; } set { _chartTitle = value; NotifyOfPropertyChange(()=> ChartTitle); } }

        private string _yAxisTitle = String.Empty;
        public string YAxisTitle { get { return _yAxisTitle; } set { _yAxisTitle = value; NotifyOfPropertyChange(() => YAxisTitle); } }

        private DoubleRange _range = new DoubleRange(0,40);
        public DoubleRange Range { get { return _range; } set { _range = value; NotifyOfPropertyChange(() => Range); } }

        private IBehaviour _behaviour = new BehaviourManager();
        public IBehaviour Behaviour { get { return _behaviour; } set { _behaviour = value; NotifyOfPropertyChange(() => Behaviour); } }

        public Sensor Sensor { set
        {
            ChartSeries = new DataSeries<DateTime, float>(value.Name, (from dataValue in value.CurrentState.Values select new DataPoint<DateTime, float>(dataValue.Timestamp, dataValue.Value)));
            ChartTitle = value.Name;
            YAxisTitle = value.Unit;
        }}
    }
}
