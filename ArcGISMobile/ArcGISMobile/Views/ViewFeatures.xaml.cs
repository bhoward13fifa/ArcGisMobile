using System;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ArcGISMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ViewFeatures : ContentPage
    {
        private ServiceFeatureTable _serviceFeatureTable;
        private FeatureLayer _serviceLayer;
        private Feature _tappedFeature;
        private ArcGISFeature _selectedFeature;
        public const string FeatureServiceUrl = "https://services7.arcgis.com/OrD0y9T7jEt4KcV9/arcgis/rest/services/properties_in_need_of_repairs/FeatureServer/0";
        private bool _isMapToggled = false;

        private bool IsMapToggled
        {
            get { return _isMapToggled; }
            set
            {
                if (_isMapToggled != value)
                {
                    _isMapToggled = value;
                    OnPropertyChanged();
                }
            }
        }

        public ViewFeatures()
        {
            InitializeComponent();
            BindingContext = this;
            Initialize();
        }

        private void Initialize()
        {
            MyMapView.Map = new Map(Basemap.CreateImageryWithLabels());

            _serviceFeatureTable = new ServiceFeatureTable(new Uri(FeatureServiceUrl));
            _serviceLayer = new FeatureLayer(_serviceFeatureTable);

            MyMapView.Map.OperationalLayers.Add(_serviceLayer);

            MyMapView.SetViewpointCenterAsync(new MapPoint(-10800000, 4500000, SpatialReferences.WebMercator), 3e7);
        }

        private async void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!IsMapToggled)
            {
                _isMapToggled = true;

                _serviceLayer.ClearSelection();
                _selectedFeature = null;

                IdentifyLayerResult identifyResult = await MyMapView.IdentifyLayerAsync(_serviceLayer, e.Position, 2, false);

                if (!identifyResult.GeoElements.Any())
                {
                    return;
                }
                ViewId.IsEnabled = true;
                ViewEditDate.IsEnabled = true;
                ViewPropertyName.IsEnabled = true;
                ViewEvaluatorName.IsEnabled = true;
                ViewDescription.IsEnabled = true;

                ViewId.Text = identifyResult.GeoElements.First().Attributes["objectid"].ToString();
                ViewEditDate.Text = identifyResult.GeoElements.First().Attributes["EditDate"].ToString();
                ViewPropertyName.Text = (string)identifyResult.GeoElements.First().Attributes["property_name"];
                ViewEvaluatorName.Text = (string)identifyResult.GeoElements.First().Attributes["evaluator_name"];
                ViewDescription.Text = (string)identifyResult.GeoElements.First().Attributes["description"];

                _isMapToggled = false;
            }
        }
    }
}
