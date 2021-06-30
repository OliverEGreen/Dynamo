using System.Windows.Media;
using Dynamo.Core;

namespace Dynamo.GraphMetadata.Models
{
    /// <summary>
    /// A property that is set by the user and appears in the GraphMetadataViewExtension
    /// </summary>
    public class ExtensionRequiredProperty : NotificationObject
    {
        private string key;
        private string value;
        private bool isReadOnly;
        
        /// <summary>
        /// The visible name of this ExtensionRequiredProperty
        /// </summary>
        public string Key
        {
            get => key;
            set
            {
                key = value;
                RaisePropertyChanged(nameof(Key));
            }
        }

        /// <summary>
        /// The user-assigned value of this ExtensionRequiredProperty
        /// </summary>
        public string Value
        {
            get => value;
            set
            {
                this.value = value;
                RaisePropertyChanged(nameof(Value));
            }
        }

        /// <summary>
        /// Determined whether this value can be set by users, or is locked for all graphs
        /// </summary>
        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                isReadOnly = value;
                RaisePropertyChanged(nameof(IsReadOnly));
            }
        }

        /// <summary>
        /// The ID of this ExtensionRequiredProperty -> Should match with a RequiredProperty
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// Public constructor for ExtensionRequiredProperty
        /// </summary>
        /// <param name="uniqueId"></param>
        public ExtensionRequiredProperty(string uniqueId)
        {
            UniqueId = uniqueId;
        }
    }
}
