using System;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dynamo.Extensions;
using Dynamo.Graph;
using Dynamo.GraphMetadata.Properties;
using Dynamo.Wpf.Extensions;
using System.Windows;
using Dynamo.Graph.Workspaces;
using Dynamo.GraphMetadata.Controls;
using Dynamo.GraphMetadata.Models;
using Dynamo.ViewModels;

namespace Dynamo.GraphMetadata
{
    public class GraphMetadataViewExtension : ViewExtensionBase, IExtensionStorageAccess
    {
        internal static string extensionName = "Properties";
        internal GraphMetadataViewModel viewModel;
        private GraphMetadataView graphMetadataView;
        private ViewLoadedParams viewLoadedParamsReference;
        private MenuItem graphMetadataMenuItem;

        public override string UniqueId => "28992e1d-abb9-417f-8b1b-05e053bee670";

        public override string Name => extensionName;

        public override void Loaded(ViewLoadedParams viewLoadedParams)
        {
            if (viewLoadedParams == null) throw new ArgumentNullException(nameof(viewLoadedParams));

            // The collection of RequiredProperty names as loaded in from the XML
            // Needs to populate the collection before the ViewModel constructor gets called
            ObservableCollection<string> requiredPropertyNames = viewLoadedParams.PreferenceSettings.RequiredPropertyNames;

            this.viewLoadedParamsReference = viewLoadedParams;
            this.viewModel = new GraphMetadataViewModel(viewLoadedParams, this);
            this.graphMetadataView = new GraphMetadataView();
            graphMetadataView.DataContext = viewModel;

            // Add a button to Dynamo View menu to manually show the window
            this.graphMetadataMenuItem = new MenuItem { Header = Resources.MenuItemText, IsCheckable = true };
            this.graphMetadataMenuItem.Checked += MenuItemCheckHandler;
            this.graphMetadataMenuItem.Unchecked += MenuItemUnCheckedHandler;
            this.viewLoadedParamsReference.AddExtensionMenuItem(this.graphMetadataMenuItem);

            // The UI is updated with keys only - values are populated in OnWorkspaceOpen
            foreach (string requiredPropertyName in requiredPropertyNames)
            {
                this.viewModel.RequiredProperties.Add(new RequiredProperty { RequiredPropertyKey = requiredPropertyName, RequiredPropertyValue = "" });
            }
        }

        private void MenuItemUnCheckedHandler(object sender, RoutedEventArgs e)
        {
            viewLoadedParamsReference.CloseExtensioninInSideBar(this);
        }

        private void MenuItemCheckHandler(object sender, RoutedEventArgs e)
        {
            // Dont allow the extension to show in anything that isnt a HomeWorkspaceModel
            if (!(this.viewLoadedParamsReference.CurrentWorkspaceModel is HomeWorkspaceModel))
            {
                this.Closed();
                return;
            }

            this.viewLoadedParamsReference?.AddToExtensionsSideBar(this, this.graphMetadataView);
        }

        #region Storage Access implementation

        /// <summary>
        /// Adds required property values and instantiates custom properties based on data saved in the .dyn (JSON) file.
        /// </summary>
        /// <param name="extensionData"></param>
        public void OnWorkspaceOpen(Dictionary<string, string> extensionData)
        {
            // The list of (unique) RequiredProperty names as loaded in from the XML
            List<string> requiredPropertyNames = viewLoadedParamsReference.PreferenceSettings.RequiredPropertyNames.ToList();

            Dictionary<string, string> requiredPropertiesToBuild = new Dictionary<string, string>();
            Dictionary<string, string> customPropertiesToBuild = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> keyValuePair in extensionData)
            {
                if (string.IsNullOrEmpty(keyValuePair.Key)) continue;

                if (requiredPropertyNames.Contains(keyValuePair.Key))
                {
                    // Removing any duplicates
                    if (requiredPropertiesToBuild.ContainsKey(keyValuePair.Key)) continue;
                    requiredPropertiesToBuild.Add(keyValuePair.Key, keyValuePair.Value);
                }
                else
                {
                    // Removing any duplicates
                    if (customPropertiesToBuild.ContainsKey(keyValuePair.Key)) continue;
                    customPropertiesToBuild.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            foreach (KeyValuePair<string, string> keyValuePair in requiredPropertiesToBuild)
            {
                // Looking to find match by key. If we find a match, set its value.
                RequiredProperty requiredProperty = this.viewModel.RequiredProperties.FirstOrDefault(x => x.RequiredPropertyKey == keyValuePair.Key);
                
                if (requiredProperty != null)
                {
                    // A match was found, we set its value here.
                    requiredProperty.RequiredPropertyValue = keyValuePair.Value;
                }
                else
                {
                    // No match found, we instantiate it here.
                    this.viewModel.RequiredProperties.Add(new RequiredProperty { RequiredPropertyKey = keyValuePair.Key, RequiredPropertyValue = keyValuePair.Value });
                }
            }

            foreach (KeyValuePair<string, string> keyValuePair in customPropertiesToBuild)
            {
                this.viewModel.AddCustomProperty(keyValuePair.Key, keyValuePair.Value, false);
            }
        }

        /// <summary>
        /// Adds all CustomProperties to this extensions extensionData
        /// </summary>
        /// <param name="extensionData"></param>
        /// <param name="saveContext"></param>
        public void OnWorkspaceSaving(Dictionary<string, string> extensionData, SaveContext saveContext)
        {
            // Clearing the extensionData dictionary before adding new values
            // as the GraphMetadataViewModel.CustomProperties is the true source of the custom properties
            extensionData.Clear();
            foreach (var p in this.viewModel.CustomProperties)
            {
                extensionData[p.PropertyName] = p.PropertyValue;
            }

            foreach (RequiredProperty requiredProperty in viewModel.RequiredProperties)
            {
                extensionData[requiredProperty.RequiredPropertyKey] = requiredProperty.RequiredPropertyValue;
            }
        }


        #endregion

        public override void Closed()
        {
            if (this.graphMetadataMenuItem != null)
            {
                this.graphMetadataMenuItem.IsChecked = false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            viewModel.Dispose();

            this.graphMetadataMenuItem.Checked -= MenuItemCheckHandler;
            this.graphMetadataMenuItem.Unchecked -= MenuItemUnCheckedHandler;
        }

        public override void Dispose()
        {
            Dispose(true);
        }
    }
}
