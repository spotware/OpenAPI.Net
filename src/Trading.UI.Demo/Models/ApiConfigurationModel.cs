using Prism.Mvvm;
using System.IO;
using System.Xml.Serialization;

namespace Trading.UI.Demo.Models
{
    public class ApiConfigurationModel : BindableBase
    {
        private string _clientId;
        private string _secret;
        private string _redirectUri;

        public string ClientId { get => _clientId; set => SetProperty(ref _clientId, value); }

        public string Secret { get => _secret; set => SetProperty(ref _secret, value); }

        public string RedirectUri { get => _redirectUri; set => SetProperty(ref _redirectUri, value); }

        [XmlIgnore]
        public bool IsLoaded { get; set; }

        public void SaveToFile(string filePath)
        {
            using FileStream fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

            var serializer = new XmlSerializer(typeof(ApiConfigurationModel));

            serializer.Serialize(fileStream, this);
        }

        public static ApiConfigurationModel LoadFromFile(string filePath)
        {
            using FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var serializer = new XmlSerializer(typeof(ApiConfigurationModel));

            var result = serializer.Deserialize(fileStream) as ApiConfigurationModel;

            result.IsLoaded = true;

            return result;
        }
    }
}