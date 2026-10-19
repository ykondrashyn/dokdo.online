using System;

namespace Dokdo
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int schemaVersion = SaveDataV1.SchemaVersion;
        public string payload = string.Empty;
        public string checksum = string.Empty;
    }
}
