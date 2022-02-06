using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ArcGISMobile.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        void View_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ViewFeatures());
        }

        void Add_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new AddFeatures());
        }

        void Edit_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new EditFeatures());
        }

        void Delete_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new DeleteFeatures());
        }

    }
}