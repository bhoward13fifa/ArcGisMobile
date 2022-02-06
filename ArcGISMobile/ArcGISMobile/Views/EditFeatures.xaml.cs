using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Esri.ArcGISRuntime.Xamarin.Forms;
using System.Web.Mvc;
using Xamarin.Forms.Xaml;
using FileResult = Xamarin.Essentials.FileResult;

#if __IOS__
using Foundation;
using UIKit
#else
using Xamarin.Essentials;
using Map = Esri.ArcGISRuntime.Mapping.Map;
#endif

namespace ArcGISMobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EditFeatures : ContentPage
    {
        private ServiceFeatureTable _serviceFeatureTable;
        private FeatureLayer _serviceLayer;
        private ArcGISFeature _tappedFeature;
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

        public EditFeatures()
        {
            InitializeComponent();
            BindingContext = this;
            Initialize();
        }

        private void Initialize()
        {
            EditMapView.Map = new Map(Basemap.CreateImageryWithLabels());

            _serviceFeatureTable = new ServiceFeatureTable(new Uri(FeatureServiceUrl));
            _serviceLayer = new FeatureLayer(_serviceFeatureTable);

            EditMapView.Map.OperationalLayers.Add(_serviceLayer);

            EditMapView.SetViewpointCenterAsync(new MapPoint(-10800000, 4500000, SpatialReferences.WebMercator), 3e7);
        }

        private async void EditMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (!IsMapToggled)
            {
                _isMapToggled = false;

                _serviceLayer.ClearSelection();
                _tappedFeature = null;

                AttachmentsListBox.IsEnabled = false;
                AttachmentsListBox.ItemsSource = null;
                AddAttachmentButton.IsEnabled = false;

                try
                {
                    IdentifyLayerResult identifyResult = await EditMapView.IdentifyLayerAsync(_serviceLayer, e.Position, 2, false);

                    if (!identifyResult.GeoElements.Any())
                    {
                        return;
                    }

                    GeoElement selectedElement = identifyResult.GeoElements.First();
                    ArcGISFeature selectedFeature = (ArcGISFeature)selectedElement;

                    _serviceLayer.SelectFeature(selectedFeature);
                    _tappedFeature = selectedFeature;

                    await selectedFeature.LoadAsync();

                    EditPropertyName.IsEnabled = true;
                    EditEvaluatorName.IsEnabled = true;
                    EditDescription.IsEnabled = true;
                    SubmitEditButton.IsEnabled = true;
                    EditMapView.IsEnabled = false;

                    AttachmentsListBox.ItemsSource = await GetJpegAttachmentsAsync(selectedFeature);
                    AttachmentsListBox.IsEnabled = true;
                    AddAttachmentButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error selecting feature", ex.ToString(), "OK");
                }
            }
        }

        private async void AddAttachment_Click(object sender, EventArgs e)
        {
            if (_tappedFeature == null)
            {
                return;
            }

            // Adjust the UI.
            AddAttachmentButton.IsEnabled = false;
            AttachmentActivityIndicator.IsVisible = true;

            // Get the file.
            string contentType = "image/jpeg";

            try
            {
                byte[] attachmentData;
                string filename;

                // Xamarin.Plugin.FilePicker shows the iCloud picker (not photo picker) on iOS.
                // This iOS code shows the photo picker.
#if __IOS__
                Stream imageStream = await GetImageStreamAsync();
                if (imageStream == null)
                {
                    return;
                }

                attachmentData = new byte[imageStream.Length];
                imageStream.Read(attachmentData, 0, attachmentData.Length);
                filename = _filename ?? "file1.jpeg";
#else
                // Show a file picker - this uses the Xamarin.Plugin.FilePicker NuGet package.
                FileResult fileData = await FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Jpeg });
                if (fileData == null)
                {
                    return;
                }

                if (!fileData.FileName.EndsWith(".jpg") && !fileData.FileName.EndsWith(".jpeg"))
                {
                    await Application.Current.MainPage.DisplayAlert("Try again!", "This sample only allows uploading jpg files.", "OK");
                    return;
                }

                using (Stream fileStream = await fileData.OpenReadAsync())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await fileStream.CopyToAsync(memoryStream);
                        attachmentData = memoryStream.ToArray();
                    }
                }
                filename = fileData.FileName;
#endif
                // Add the attachment.
                // The contentType string is the MIME type for JPEG files, image/jpeg.
                await _tappedFeature.AddAttachmentAsync(filename, contentType, attachmentData);

                // Get a reference to the feature's service feature table.
                ServiceFeatureTable serviceTable = (ServiceFeatureTable)_tappedFeature.FeatureTable;

                // Apply the edits to the service feature table.
                await serviceTable.ApplyEditsAsync();

                // Update UI.
                _tappedFeature.Refresh();
                AttachmentsListBox.ItemsSource = await GetJpegAttachmentsAsync(_tappedFeature);

                await Application.Current.MainPage.DisplayAlert("Success!", "Successfully added attachment", "OK");
            }
            catch (Exception exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error adding attachment", exception.ToString(), "OK");
            }
            finally
            {
                // Adjust the UI.
                AddAttachmentButton.IsEnabled = true;
                AttachmentActivityIndicator.IsVisible = false;
            }
        }

        private async void SubmitEdit_Click(object sender, EventArgs e)
        {
            try
            {
                _tappedFeature.Attributes["property_name"] = EditPropertyName.Text;
                _tappedFeature.Attributes["evaluator_name"] = EditEvaluatorName.Text;
                _tappedFeature.Attributes["description"] = EditDescription.Text;

                await _tappedFeature.FeatureTable.UpdateFeatureAsync(_tappedFeature);

                var editResults = await _serviceFeatureTable.ApplyEditsAsync();

                await Application.Current.MainPage.DisplayAlert("Success!", $"Edited feature " + _tappedFeature.Attributes["objectid"], "OK");
            }
            catch(Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(ex.ToString(), "Error editing feature", "OK");
            }
            finally
            {
                EditPropertyName.IsEnabled = false;
                EditEvaluatorName.IsEnabled = false;
                EditDescription.IsEnabled = false;
                EditMapView.IsEnabled = true;
            }

            
        }

        private async void DeleteAttachment_Click(object sender, EventArgs e)
        {
            AttachmentActivityIndicator.IsVisible = true;

            try
            {
                // Get the attachment that should be deleted.
                Button sendingButton = (Button)sender;
                Attachment selectedAttachment = (Attachment)sendingButton.BindingContext;

                // Delete the attachment.
                await _tappedFeature.DeleteAttachmentAsync(selectedAttachment);

                // Get a reference to the feature's service feature table.
                ServiceFeatureTable serviceTable = (ServiceFeatureTable)_tappedFeature.FeatureTable;

                // Apply the edits to the service feature table.
                await serviceTable.ApplyEditsAsync();

                // Update UI.
                _tappedFeature.Refresh();
                AttachmentsListBox.ItemsSource = await GetJpegAttachmentsAsync(_tappedFeature);

                // Show success message.
                await Application.Current.MainPage.DisplayAlert("Success!", "Successfully deleted attachment", "OK");
            }
            catch (Exception exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error deleting attachment", exception.ToString(), "OK");
            }
            finally
            {
                AttachmentActivityIndicator.IsVisible = false;
            }
        }

        private async void DownloadAttachment_Click(object sender, EventArgs e)
        {
            try
            {
                // Get a reference to the button that raised the event.
                Button sendingButton = (Button)sender;

                // Get the attachment from the button's DataContext. The button's DataContext is set by the list view.
                Attachment selectedAttachment = (Attachment)sendingButton.BindingContext;

                if (selectedAttachment.ContentType.Contains("image"))
                {
                    // Create a preview and show it.
                    ContentPage previewPage = new ContentPage();
                    previewPage.Title = "Attachment preview";
                    Image imageView = new Image();
                    Stream contentStream = await selectedAttachment.GetDataAsync();
                    imageView.Source = ImageSource.FromStream(() => contentStream);
                    previewPage.Content = imageView;
                    await Navigation.PushAsync(previewPage);
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Can't show attachment", "This sample can only show image attachments.", "OK");
                }
            }
            catch (Exception exception)
            {
                await Application.Current.MainPage.DisplayAlert("Error reading attachment", exception.ToString(), "OK");
            }
        }

        private static async Task<IEnumerable<Attachment>> GetJpegAttachmentsAsync(ArcGISFeature feature)
        {
            IReadOnlyList<Attachment> attachments = await feature.GetAttachmentsAsync();
            return attachments.Where(attachment => attachment.ContentType == "image/jpeg").ToList();
        }

        // Image picker implementation.
        // Xamarin.Plugin.FilePicker shows an iCloud file picker; comment this out
        // and use the cross-platform implementation if that's what you want.
        // Note: code adapted from https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/dependency-service/photo-picker
#if __IOS__
        private TaskCompletionSource<Stream> _taskCompletionSource;
        private UIImagePickerController _imagePicker;
        private string _filename;

        private Task<Stream> GetImageStreamAsync()
        {
            // Create and define UIImagePickerController.
            _imagePicker = new UIImagePickerController
            {
                SourceType = UIImagePickerControllerSourceType.PhotoLibrary,
                MediaTypes = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary)
            };

            // Set event handlers.
            _imagePicker.FinishedPickingMedia += OnImagePickerFinishedPickingMedia;
            _imagePicker.Canceled += OnImagePickerCancelled;

            // Present UIImagePickerController.
            UIWindow window = UIApplication.SharedApplication.KeyWindow;
            var viewController = window.RootViewController;
            viewController.PresentModalViewController(_imagePicker, true);

            // Return Task object.
            _taskCompletionSource = new TaskCompletionSource<Stream>();
            return _taskCompletionSource.Task;
        }

        void OnImagePickerFinishedPickingMedia(object sender, UIImagePickerMediaPickedEventArgs args)
        {
            UIImage image = args.EditedImage ?? args.OriginalImage;
            _filename = args.ImageUrl.LastPathComponent;
            if (image != null)
            {
                // Convert UIImage to .NET Stream object.
                NSData data = image.AsJPEG(1);
                Stream stream = data.AsStream();

                UnregisterEventHandlers();

                // Set the Stream as the completion of the Task.
                _taskCompletionSource.SetResult(stream);
            }
            else
            {
                UnregisterEventHandlers();
                _taskCompletionSource.SetResult(null);
            }

            _imagePicker.DismissModalViewController(true);
        }

        void OnImagePickerCancelled(object sender, EventArgs args)
        {
            UnregisterEventHandlers();
            _taskCompletionSource.SetResult(null);
            _imagePicker.DismissModalViewController(true);
        }

        void UnregisterEventHandlers()
        {
            _imagePicker.FinishedPickingMedia -= OnImagePickerFinishedPickingMedia;
            _imagePicker.Canceled -= OnImagePickerCancelled;
        }
#endif
    }
}