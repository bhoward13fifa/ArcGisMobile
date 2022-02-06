using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Xamarin.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ArcGISMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddFeatures : ContentPage
    {
        private MapPoint _mapPoint;
        private ServiceFeatureTable _serviceFeatureTable;
        private FeatureLayer _serviceLayer;
        private const string FeatureServiceUrl = "https://services7.arcgis.com/OrD0y9T7jEt4KcV9/arcgis/rest/services/properties_in_need_of_repairs/FeatureServer/0";
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
        public AddFeatures()
        {
            InitializeComponent();
            BindingContext = this;
            Initialize();
        }

        private void Initialize()
        {
            AddMapView.Map = new Map(Basemap.CreateImageryWithLabels());

            _serviceFeatureTable = new ServiceFeatureTable(new Uri(FeatureServiceUrl));
            _serviceLayer = new FeatureLayer(_serviceFeatureTable);

            AddMapView.Map.OperationalLayers.Add(_serviceLayer);

            AddMapView.SetViewpointCenterAsync(new MapPoint(-10800000, 4500000, SpatialReferences.WebMercator), 3e7);
        }

        private async void AddMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!IsMapToggled)
            {
                _isMapToggled = true;

                try
                {
                    MapPoint tappedPoint = (MapPoint)GeometryEngine.NormalizeCentralMeridian(e.Location);

                    AddPropertyName.IsEnabled = true;
                    AddEvaluatorName.IsEnabled = true;
                    AddDescription.IsEnabled = true;
                    SubmitButton.IsEnabled = true;
                    AddMapView.IsEnabled = false;

                    _mapPoint = tappedPoint;
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error adding feature", ex.ToString(), "OK");
                }
            }
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                ArcGISFeature feature = (ArcGISFeature)_serviceFeatureTable.CreateFeature();

                feature.Geometry = _mapPoint;

                feature.Attributes["property_name"] = AddPropertyName.Text;
                feature.Attributes["evaluator_name"] = AddEvaluatorName.Text;
                feature.Attributes["description"] = AddDescription.Text;
                await _serviceFeatureTable.AddFeatureAsync(feature);

                await _serviceFeatureTable.ApplyEditsAsync();

                feature.Refresh();

                await Application.Current.MainPage.DisplayAlert("Success!", $"Created feature " + feature.Attributes["objectid"], "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(ex.ToString(), "Error submitting feature", "OK");
            }
            finally
            {
                AddMapView.IsEnabled = true;
                AddPropertyName.IsEnabled = false;
                AddEvaluatorName.IsEnabled = false;
                AddDescription.IsEnabled = false;
                SubmitButton.IsEnabled = false;
                _isMapToggled = false;
            }
        }

        /// <summary>
        /// Raises the <see cref="MapViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var propertyChangedHandler = PropertyChanged;
            if (propertyChangedHandler != null)
                propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}