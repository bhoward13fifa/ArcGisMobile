using ArcGISMobile.Views;
using Esri.ArcGISRuntime;
using Xamarin.Forms;

namespace ArcGISMobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            //****************
            //
            // Authentication:
            // Use of Esri location services, including basemaps and geocoding, requires either an ArcGIS identity or an API key. 
            // For more information see https://links.esri.com/arcgis-runtime-security-auth.
            //
            // Licensing:
            // Production deployment of applications built with ArcGIS Runtime requires you to license ArcGIS Runtime functionality.
            // For more information see https://links.esri.com/arcgis-runtime-license-and-deploy.
            //
            //****************
            ArcGISRuntimeEnvironment.ApiKey = "AAPK269a27b1b2fd47288bc8422353fd0b18vGUxgW1upbHoWWzL9ETZbsKYPjSHXB066TPIQHuU_8yYKgSv7lcBYVnymABt8XgT";
            // Initialize the ArcGIS Runtime before any components are created.
            ArcGISRuntimeEnvironment.Initialize();

            // The root page of your application
            MainPage = new NavigationPage(new HomePage());
        }
    }
}