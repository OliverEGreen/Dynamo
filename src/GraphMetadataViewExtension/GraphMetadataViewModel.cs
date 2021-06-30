using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Dynamo.Configuration;
using Dynamo.Core;
using Dynamo.Graph.Workspaces;
using Dynamo.GraphMetadata.Controls;
using Dynamo.GraphMetadata.Models;
using Dynamo.UI.Commands;
using Dynamo.Wpf.Extensions;

namespace Dynamo.GraphMetadata
{
    public class GraphMetadataViewModel : NotificationObject
    {
        private readonly ViewLoadedParams viewLoadedParams;
        private readonly GraphMetadataViewExtension extension;
        private HomeWorkspaceModel currentWorkspace;

        private Visibility extensionRequiredPropertiesVisibilityVisibility = Visibility.Collapsed;
        private ObservableCollection<ExtensionRequiredProperty> extensionRequiredProperties;

        /// <summary>
        /// Command used to add new custom properties to the CustomProperty collection
        /// </summary>
        public DelegateCommand AddCustomPropertyCommand { get; set; }


        /// <summary>
        /// Description of the current workspace
        /// </summary>
        public string GraphDescription
        {
            get { return currentWorkspace.Description; }
            set
            {
                if (currentWorkspace != null && GraphDescription != value)
                {
                    MarkCurrentWorkspaceModified();
                    currentWorkspace.Description = value;
                    RaisePropertyChanged(nameof(GraphDescription));
                }
            }
        }

        /// <summary>
        /// Author name of the current workspace
        /// </summary>
        public string GraphAuthor
        {
            get { return currentWorkspace.Author; }
            set
            {
                if (currentWorkspace != null && GraphAuthor != value)
                {
                    MarkCurrentWorkspaceModified();
                    currentWorkspace.Author = value;
                    RaisePropertyChanged(nameof(GraphAuthor));
                }
            }
        }

        /// <summary>
        /// Link to documentation page for current workspace
        /// </summary>
        public Uri HelpLink
        {
            get { return currentWorkspace.GraphDocumentationURL; }
            set
            {
                if (currentWorkspace != null && HelpLink != value)
                {
                    MarkCurrentWorkspaceModified();
                    currentWorkspace.GraphDocumentationURL = value;
                    RaisePropertyChanged(nameof(HelpLink));
                }
            }
        }

        /// <summary>
        /// Workspace thumbnail as BitmapImage.
        /// </summary>
        public BitmapImage Thumbnail
        {
            get
            {
                var bitmap = ImageFromBase64(currentWorkspace.Thumbnail);
                return bitmap;
            }
            set
            {
                var base64 = value is null ? string.Empty : Base64FromImage(value);
                if (currentWorkspace != null && base64 != currentWorkspace.Thumbnail)
                {
                    MarkCurrentWorkspaceModified();
                    currentWorkspace.Thumbnail = base64;
                    RaisePropertyChanged(nameof(Thumbnail));
                }
            }
        }

        public Visibility ExtensionRequiredPropertiesVisibility
        {
            get => extensionRequiredPropertiesVisibilityVisibility;
            set
            {
                extensionRequiredPropertiesVisibilityVisibility = value;
                RaisePropertyChanged(nameof(ExtensionRequiredPropertiesVisibility));
            }
        }


        /// <summary>
        /// Collection of CustomProperties
        /// </summary>
        public ObservableCollection<CustomPropertyControl> CustomProperties { get; set; }

        /// <summary>
        /// Collection of Properties Required by this ViewExtension
        /// </summary>
        public ObservableCollection<ExtensionRequiredProperty> ExtensionRequiredProperties
        {
            get => extensionRequiredProperties;
            set
            {
                extensionRequiredProperties = value;
                RaisePropertyChanged(nameof(ExtensionRequiredProperties));
            }
        }

        public GraphMetadataViewModel(ViewLoadedParams viewLoadedParams, GraphMetadataViewExtension extension)
        {
            this.viewLoadedParams = viewLoadedParams;
            this.extension = extension;
            this.currentWorkspace = viewLoadedParams.CurrentWorkspaceModel as HomeWorkspaceModel;

            this.viewLoadedParams.CurrentWorkspaceChanged += OnCurrentWorkspaceChanged;
            // using this as CurrentWorkspaceChanged wont trigger if you:
            // Close a saved workspace and open a New homeworkspace..
            // This means that properties defined in the previous opened workspace will still be showed in the extension.
            // CurrentWorkspaceCleared will trigger everytime a graph is closed which allows us to reset the properties. 
            this.viewLoadedParams.CurrentWorkspaceCleared += OnCurrentWorkspaceChanged;
            
            CustomProperties = new ObservableCollection<CustomPropertyControl>();
            ExtensionRequiredProperties = new ObservableCollection<ExtensionRequiredProperty>();
            ExtensionRequiredProperties.CollectionChanged += UpdateRequiredPropertiesVisibility;

            viewLoadedParams.PreferenceSettings.RequiredProperties.CollectionChanged += RequiredPropertyNamesOnCollectionChanged;

            // To prevent duplicate keys being added
            List<string> resolvedKeys = new List<string>();

            // Initializing the ExtensionRequiredProperties based on the RequiredProperties saved in the XML
            foreach (RequiredProperty requiredProperty in viewLoadedParams.PreferenceSettings.RequiredProperties)
            {
                // Removing blanks and duplicates
                if(string.IsNullOrEmpty(requiredProperty.Key) || resolvedKeys.Contains(requiredProperty.Key)) continue;

                // We only want to set values when RequiredProperties' values are defined globally.
                string requiredPropertyValue = requiredProperty.ValueIsGlobal ? requiredProperty.Value : null;

                ExtensionRequiredProperty extensionRequiredProperty = new ExtensionRequiredProperty(requiredProperty.UniqueId)
                {
                    Key = requiredProperty.Key,
                    Value = requiredPropertyValue,
                    IsReadOnly = requiredProperty.ValueIsGlobal
                };

                requiredProperty.PropertyChanged += RequiredPropertyOnPropertyChanged;

                ExtensionRequiredProperties.Add(extensionRequiredProperty);
                resolvedKeys.Add(extensionRequiredProperty.Key);
            }
            
            InitializeCommands();
        }

        private void RequiredPropertyOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is RequiredProperty requiredProperty)) return;

            ExtensionRequiredProperty extensionRequiredProperty = ExtensionRequiredProperties.FirstOrDefault(x => x.UniqueId == requiredProperty.UniqueId);
            if (extensionRequiredProperty == null) return;

            switch (e.PropertyName)
            {
                case "Key":
                    // The Preferences window can rename any ExtensionRequiredProperty / RequiredProperty key
                    if (string.IsNullOrEmpty(requiredProperty.Key)) return;
                    extensionRequiredProperty.Key = requiredProperty.Key;
                    break;
                case "Value":
                    // The Preferences window can only set a value if it's being set globally
                    if (!requiredProperty.ValueIsGlobal || string.IsNullOrEmpty(requiredProperty.Key)) return;
                    extensionRequiredProperty.Value = requiredProperty.Value;
                    break;
                case "ValueIsGlobal":
                    extensionRequiredProperty.IsReadOnly = requiredProperty.ValueIsGlobal;
                    break;
            }
        }

        /// <summary>
        /// Sets listener to the PropertyChanged event of new RequiredProperties as they are added/removed from the PreferenceSettings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RequiredPropertyNamesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (RequiredProperty requiredProperty in e.NewItems)
                {
                    requiredProperty.PropertyChanged += RequiredPropertyOnPropertyChanged;
                    ExtensionRequiredProperty extensionRequiredProperty = new ExtensionRequiredProperty(requiredProperty.UniqueId)
                    {
                        Key = requiredProperty.Key,
                        Value = requiredProperty.Value,
                        IsReadOnly = requiredProperty.ValueIsGlobal
                    };
                    ExtensionRequiredProperties.Add(extensionRequiredProperty);
                }
            }

            if (e.OldItems != null)
            {
                foreach (RequiredProperty requiredProperty in e.OldItems)
                {
                    requiredProperty.PropertyChanged -= RequiredPropertyOnPropertyChanged;
                    ExtensionRequiredProperty extensionRequiredProperty = ExtensionRequiredProperties.FirstOrDefault(x => x.Key == requiredProperty.Key);
                    if (extensionRequiredProperty == null) continue;
                    ExtensionRequiredProperties.Remove(extensionRequiredProperty);
                }
            }
        }
        
        private void OnCurrentWorkspaceChanged(IWorkspaceModel obj)
        {
            if (!(obj is HomeWorkspaceModel hwm))
            {
                extension.Closed();
                return;
            }

            if (string.IsNullOrEmpty(hwm.FileName))
            {
                GraphDescription = string.Empty;
                GraphAuthor = string.Empty;
                HelpLink = null;
                Thumbnail = null;
            }

            else
            {
                currentWorkspace = hwm;
                RaisePropertyChanged(nameof(GraphDescription));
                RaisePropertyChanged(nameof(GraphAuthor));
                RaisePropertyChanged(nameof(HelpLink));
                RaisePropertyChanged(nameof(Thumbnail));
            }

            CustomProperties.Clear();
        }

        /// <summary>
        /// Hides or shows RequiredProperties in the extension window as is needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateRequiredPropertiesVisibility(object sender, NotifyCollectionChangedEventArgs e)
        {
            ExtensionRequiredPropertiesVisibility = ExtensionRequiredProperties == null || ExtensionRequiredProperties.Count < 1
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private static BitmapImage ImageFromBase64(string b64string)
        {
            if (string.IsNullOrEmpty(b64string))
            {
                return null;
            }

            var bytes = Convert.FromBase64String(b64string);

            using (var stream = new MemoryStream(bytes))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private static string Base64FromImage(BitmapImage source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = ms.ToArray();
            }

            return Convert.ToBase64String(data);
        }

        private void InitializeCommands()
        {
            this.AddCustomPropertyCommand = new DelegateCommand(AddCustomPropertyExecute);
        }

        private void AddCustomPropertyExecute(object obj)
        {
            var propName = $"Custom Property {CustomProperties.Count + 1}";
            AddCustomProperty(propName, string.Empty);
        }

        internal void AddCustomProperty(string propertyName, string propertyValue, bool markChange = true)
        {
            var control = new CustomPropertyControl
            {
                PropertyName = propertyName,
                PropertyValue = propertyValue
            };

            control.RequestDelete += HandleDeleteRequest;
            control.PropertyChanged += HandlePropertyChanged;
            CustomProperties.Add(control);

            if (markChange)
            {
                MarkCurrentWorkspaceModified();
            }
        }

        private void HandlePropertyChanged(object sender, EventArgs e)
        {
            MarkCurrentWorkspaceModified();
        }

        private void HandleDeleteRequest(object sender, EventArgs e)
        {
            if (sender is CustomPropertyControl customProperty)
            {
                customProperty.RequestDelete -= HandleDeleteRequest;
                customProperty.PropertyChanged -= HandlePropertyChanged;
                CustomProperties.Remove(customProperty);
                MarkCurrentWorkspaceModified();
            }
        }

        private void MarkCurrentWorkspaceModified()
        {
            if (currentWorkspace != null && !string.IsNullOrEmpty(currentWorkspace.FileName))
            {
                currentWorkspace.HasUnsavedChanges = true;
            }
        }

        public void Dispose()
        {
            this.viewLoadedParams.CurrentWorkspaceChanged -= OnCurrentWorkspaceChanged;

            foreach (var cp in CustomProperties)
            {
                cp.RequestDelete -= HandleDeleteRequest;
                cp.PropertyChanged -= HandlePropertyChanged;
            }
        }
    }
}
