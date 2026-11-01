using UnityEngine;

namespace EchoStation.InfiltrationArt
{
    /// <summary>Traceable art instance; never grants gameplay or AI permissions.</summary>
    [DisallowMultipleComponent]
    public sealed class ArtSampleModel : MonoBehaviour
    {
        [SerializeField] string role;
        [SerializeField] string sourceAsset;
        public string Role => role;
        public string SourceAsset => sourceAsset;
        public void Configure(string value, string source)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(source))
                throw new System.ArgumentException("Art role and original model path required.");
            role = value;
            sourceAsset = source;
        }
    }
}
