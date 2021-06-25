using Dynamo.Core;

namespace Dynamo.GraphMetadata.Models
{
    public class RequiredProperty : NotificationObject
    {
        private string requiredPropertyKey;
        private string requiredPropertyValue;

        public string RequiredPropertyKey
        {
            get => requiredPropertyKey;
            set
            {
                requiredPropertyKey = value;
                RaisePropertyChanged(RequiredPropertyKey);
            }
        }

        public string RequiredPropertyValue
        {
            get => requiredPropertyValue;
            set
            {
                requiredPropertyValue = value;
                RaisePropertyChanged(RequiredPropertyValue);
            }
        }
    }
}
