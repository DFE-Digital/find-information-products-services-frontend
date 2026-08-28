namespace FipsFrontend.Models
{
    public enum DataClassification
    {
        Public = 0,
        Internal = 1,
        Confidential = 2,
        Restricted = 3
    }

    public class DataClassificationAttribute : Attribute
    {
        public DataClassification Classification { get; }
        public string Description { get; }

        public DataClassificationAttribute(DataClassification classification, string? description = null)
        {
            Classification = classification;
            Description = description ?? GetDefaultDescription(classification);
        }

        private static string GetDefaultDescription(DataClassification classification)
        {
            return classification switch
            {
                DataClassification.Public => "Public information - can be freely shared",
                DataClassification.Internal => "Internal information - for DfE use only",
                DataClassification.Confidential => "Confidential information - restricted access",
                DataClassification.Restricted => "Restricted information - highly sensitive",
                _ => "Unknown classification"
            };
        }
    }

    public class DataHandlingPolicy
    {
        public static readonly Dictionary<DataClassification, DataHandlingRules> Rules = new()
        {
            [DataClassification.Public] = new DataHandlingRules
            {
                CanStore = true,
                CanTransmit = true,
                CanLog = true,
                RetentionPeriod = TimeSpan.FromDays(365),
                EncryptionRequired = false,
                AccessLoggingRequired = false
            },
            [DataClassification.Internal] = new DataHandlingRules
            {
                CanStore = true,
                CanTransmit = true,
                CanLog = true,
                RetentionPeriod = TimeSpan.FromDays(90),
                EncryptionRequired = false,
                AccessLoggingRequired = true
            },
            [DataClassification.Confidential] = new DataHandlingRules
            {
                CanStore = true,
                CanTransmit = true,
                CanLog = false,
                RetentionPeriod = TimeSpan.FromDays(30),
                EncryptionRequired = true,
                AccessLoggingRequired = true
            },
            [DataClassification.Restricted] = new DataHandlingRules
            {
                CanStore = false,
                CanTransmit = false,
                CanLog = false,
                RetentionPeriod = TimeSpan.Zero,
                EncryptionRequired = true,
                AccessLoggingRequired = true
            }
        };
    }

    public class DataHandlingRules
    {
        public bool CanStore { get; set; }
        public bool CanTransmit { get; set; }
        public bool CanLog { get; set; }
        public TimeSpan RetentionPeriod { get; set; }
        public bool EncryptionRequired { get; set; }
        public bool AccessLoggingRequired { get; set; }
    }
}
