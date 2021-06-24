using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dynamo.Annotations;

namespace Dynamo.GraphMetadata.Models
{
    /// <summary>
    /// Used by the GraphMetadataViewExtension to create properties that appear as hard-coded options in the ViewExtension panel.
    /// </summary>
    public class RequiredProperty : INotifyPropertyChanged
    {
        private string requiredPropertyName;
        private string requiredPropertyValue;
        
        /// <summary>
        /// Name of the required property.
        /// </summary>
        public string RequiredPropertyName
        {
            get => requiredPropertyName;
            set
            {
                requiredPropertyName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Value of the required property.
        /// </summary>
        public string RequiredPropertyValue
        {
            get => requiredPropertyValue;
            set
            {
                requiredPropertyValue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
