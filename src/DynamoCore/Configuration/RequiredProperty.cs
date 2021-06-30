using System;
using Dynamo.Core;

namespace Dynamo.Configuration
{
    /// <summary>
    /// A property that is set by the user and appears in the Preferences window
    /// </summary>
    public class RequiredProperty : NotificationObject
    {
        private string key;
        private string value;
        private bool valueIsGlobal;
        
        /// <summary>
        /// The ID of this RequiredProperty - should have a counterpart ExtensionRequiredProperty with the same ID
        /// </summary>
        public string UniqueId { get; set; }

        /// <summary>
        /// The name of this RequiredProperty
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
        /// The user-assigned value of this RequiredProperty
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
        public bool ValueIsGlobal
        {
            get => valueIsGlobal;
            set
            {
                valueIsGlobal = value;
                RaisePropertyChanged(nameof(ValueIsGlobal));
            }
        }
        
        /// <summary>
        /// Public constructor for RequiredProperty. Sets the ID.
        /// </summary>
        public RequiredProperty()
        {
            UniqueId = Guid.NewGuid().ToString();
        }
    }
}
