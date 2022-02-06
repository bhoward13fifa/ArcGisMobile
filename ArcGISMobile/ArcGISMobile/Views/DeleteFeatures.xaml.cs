using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ArcGISMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeleteFeatures : ContentPage
    {
        private ServiceFeatureTable _serviceFeatureTable;
        private FeatureLayer _serviceLayer;
        private Feature _tappedFeature;
        private const string FeatureServiceUrl = "https://services7.arcgis.com/OrD0y9T7jEt4KcV9/arcgis/rest/services/properties_in_need_of_repairs/FeatureServer/0";
        private bool _isMapToggled = false;

        public DeleteFeatures()
        {
            InitializeComponent();
            Initialize();
        }

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

        private void Initialize()
        {
            DeleteMapView.Map = new Map(Basemap.CreateImageryWithLabels());

            _serviceFeatureTable = new ServiceFeatureTable(new Uri(FeatureServiceUrl));
            _serviceLayer = new FeatureLayer(_serviceFeatureTable);

            DeleteMapView.Map.OperationalLayers.Add(_serviceLayer);

            DeleteMapView.SetViewpointCenterAsync(new MapPoint(-10800000, 4500000, SpatialReferences.WebMercator), 3e7);
        }

        private async void DeleteMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!IsMapToggled)
            {
                _isMapToggled = true;

                _serviceLayer.ClearSelection();

                DeleteButton.IsEnabled = false;
                DeleteButton.Text = "Delete Feature";

                try
                {
                    IdentifyLayerResult identifyResult = await DeleteMapView.IdentifyLayerAsync(_serviceLayer, e.Position, 2, false);

                    if (!identifyResult.GeoElements.Any())
                    {
                        return;
                    }

                    long featureId = (long)identifyResult.GeoElements.First().Attributes["objectid"];

                    QueryParameters queryParameters = new QueryParameters();

                    queryParameters.ObjectIds.Add(featureId);
                    FeatureQueryResult queryResult = await _serviceLayer.FeatureTable.QueryFeaturesAsync(queryParameters);
                    _tappedFeature = queryResult.First();

                    _serviceLayer.SelectFeature(_tappedFeature);

                    ConfigureDeletionButton(_tappedFeature);
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", ex.ToString(), "OK");
                }
            }
        }

        private void ConfigureDeletionButton(Feature tappedFeature)
        {
            DeleteButton.Text = $"Delete Feature {tappedFeature.Attributes["objectid"]}";
            DeleteButton.IsEnabled = true;
        }

        private async void Delete_Clicked(object sender, EventArgs e)
        {
            DeleteButton.IsEnabled = false;
            DeleteButton.Text = "Delete Feature";

            if (_tappedFeature == null)
            {
                return ;
            }

            try
            {
                await _serviceLayer.FeatureTable.DeleteFeatureAsync(_tappedFeature);

                await _serviceFeatureTable.ApplyEditsAsync();

                await Application.Current.MainPage.DisplayAlert("Success!", $"Deleted Feature {_tappedFeature.Attributes["objectid"]}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.ToString(), "OK");
            }
            finally
            {
                _isMapToggled = false;
            }
        }
    }
}