using System;
using System.Windows.Controls;
using System.Collections.Generic;
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
            
            // The list of RequiredProperty names as loaded in from the XML
            List<string> requiredPropertyNames = viewLoadedParamsReference.PreferenceSettings.RequiredPropertyNames;

            Dictionary<string, string> requiredPropertiesToBuild = new Dictionary<string, string>();

            this.viewLoadedParamsReference = viewLoadedParams;
            this.viewModel = new GraphMetadataViewModel(viewLoadedParams, this);
            this.graphMetadataView = new GraphMetadataView();
            graphMetadataView.DataContext = viewModel;

            // Add a button to Dynamo View menu to manually show the window
            this.graphMetadataMenuItem = new MenuItem { Header = Resources.MenuItemText, IsCheckable = true };
            this.graphMetadataMenuItem.Checked += MenuItemCheckHandler;
            this.graphMetadataMenuItem.Unchecked += MenuItemUnCheckedHandler;
            this.viewLoadedParamsReference.AddExtensionMenuItem(this.graphMetadataMenuItem);
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
        /// Adds required + custom properties serialized in the graph to the viewModels CustomProperty collection
        /// </summary>
        /// <param name="extensionData"></param>
        public void OnWorkspaceOpen(Dictionary<string, string> extensionData)
        {
            // The list of RequiredProperty names as loaded in from the XML
            List<string> requiredPropertyNames = viewLoadedParamsReference.PreferenceSettings.RequiredPropertyNames;

            Dictionary<string, string> requiredPropertiesToBuild = new Dictionary<string, string>();
            Dictionary<string, string> customPropertiesToBuild = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> keyValuePair in extensionData)
            {
                if (string.IsNullOrEmpty(keyValuePair.Key)) continue;
                
                if(requiredPropertyNames.Contains(keyValuePair.Key))
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

            foreach (string value in viewLoadedParamsReference.PreferenceSettings.RequiredPropertyNames)
            {
                if (requiredPropertiesToBuild.ContainsKey(value)) continue;
                requiredPropertiesToBuild.Add(value, "");
            }

            foreach (KeyValuePair<string, string> keyValuePair in requiredPropertiesToBuild)
            {
                this.viewModel.RequiredProperties.Add(new RequiredProperty { RequiredPropertyKey = keyValuePair.Key, RequiredPropertyValue = keyValuePair.Value });
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
