using System;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dynamo.Extensions;
using Dynamo.Graph;
using Dynamo.GraphMetadata.Properties;
using Dynamo.Wpf.Extensions;
using System.Windows;
using Dynamo.Configuration;
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
            // Don't allow the extension to show in anything that isnt a HomeWorkspaceModel
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
            // There are multiple places where ExtensionRequiredProperties' values may be set
            // If the value is defined globally, this is saved in the DynamoSettings.xml file and is loaded
            // in the constructor of the GraphMetadataViewModel
            // However, ExtensionRequiredProperty values may also be graph-specific, in which case they live in the 
            // JSON data of the .dyn file format. In this case, they are loaded in here.
            
            // For reference, we load the list of ExtensionRequiredProperty names as loaded in from the XML
            List<string> requiredPropertyKeys = this.viewModel.ExtensionRequiredProperties.Select(x => x.Key).ToList();

            Dictionary<string, string> extensionRequiredPropertiesToBuild = new Dictionary<string, string>();
            Dictionary<string, string> customPropertiesToBuild = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> keyValuePair in extensionData)
            {
                if (string.IsNullOrEmpty(keyValuePair.Key)) continue;

                // Should a key conflict arise between an ExtensionRequiredProperty and a CustomProperty, the ExtensionRequiredProperty takes primacy
                if (requiredPropertyKeys.Contains(keyValuePair.Key))
                {
                    // Removing any duplicates
                    if (extensionRequiredPropertiesToBuild.ContainsKey(keyValuePair.Key)) continue;
                    extensionRequiredPropertiesToBuild.Add(keyValuePair.Key, keyValuePair.Value);
                }
                else
                {
                    // Removing any duplicates
                    if (customPropertiesToBuild.ContainsKey(keyValuePair.Key)) continue;
                    customPropertiesToBuild.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            // To prevent duplicate keys being added
            List<string> resolvedKeys = new List<string>();
            
            foreach (KeyValuePair<string, string> keyValuePair in extensionRequiredPropertiesToBuild)
            {
                // Looking through the already-instantiated ExtensionRequiredProperties (from the XML) to find match by key.
                ExtensionRequiredProperty extensionRequiredProperty = this.viewModel.ExtensionRequiredProperties.FirstOrDefault(x => x.Key == keyValuePair.Key);
                
                // Here, we are just setting a value for the ones that have locally-defined values.
                // However, if an already-instantiated ExtensionRequiredProperty has .IsReadOnly as false, this means its value is defined globally.
                if (extensionRequiredProperty != null && extensionRequiredProperty.IsReadOnly && !resolvedKeys.Contains(keyValuePair.Key))
                {
                    // A match was found in the .dyn/JSON data and the value is defined locally. We set its value here.
                    extensionRequiredProperty.Value = keyValuePair.Value;
                    resolvedKeys.Add(extensionRequiredProperty.Key);
                }
                else
                {
                    // If the key is already taken by an XML-defined ExtensionRequiredProperty, we disregard anything else encountered.
                    if (requiredPropertyKeys.Contains(keyValuePair.Key) || resolvedKeys.Contains(keyValuePair.Key)) continue;
                    
                    // A new ExtensionRequiredProperty is instantiated and given the key/value as defined in the .dyn/JSON data.
                    this.viewModel.ExtensionRequiredProperties.Add(new ExtensionRequiredProperty(Guid.NewGuid().ToString())
                    {
                        Key = keyValuePair.Key,
                        Value = keyValuePair.Value,
                        IsReadOnly = false
                    });
                    resolvedKeys.Add(keyValuePair.Key);
                }
            }

            // The instantiation of CustomProperties comes last. If there are values in the .dyn/JSON data which aren't 
            // either globally-set or locally-set ExtensionRequiredProperties, they'll be loaded in as CustomProperties.
            foreach (KeyValuePair<string, string> keyValuePair in customPropertiesToBuild)
            {
                if (resolvedKeys.Contains(keyValuePair.Key)) continue;
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

            foreach (ExtensionRequiredProperty extensionRequiredProperty in viewModel.ExtensionRequiredProperties)
            {
                extensionData[extensionRequiredProperty.Key] = extensionRequiredProperty.Value;
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
