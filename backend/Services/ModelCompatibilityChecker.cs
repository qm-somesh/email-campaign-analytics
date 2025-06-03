using System.IO;
using System.Text;

namespace EmailCampaignReporting.API.Services
{
    /// <summary>
    /// Utility class to check GGUF model compatibility before loading
    /// </summary>
    public static class ModelCompatibilityChecker
    {
        private const uint GGUF_MAGIC = 0x46554747; // "GGUF" in little-endian
        
        /// <summary>
        /// Check if a GGUF model file appears to be valid and compatible
        /// </summary>
        /// <param name="modelPath">Path to the model file</param>
        /// <returns>Compatibility result with details</returns>
        public static ModelCompatibilityResult CheckModel(string modelPath)
        {
            var result = new ModelCompatibilityResult
            {
                FilePath = modelPath,
                IsCompatible = false
            };

            try
            {
                if (string.IsNullOrEmpty(modelPath))
                {
                    result.Issues.Add("Model path is null or empty");
                    return result;
                }

                if (!File.Exists(modelPath))
                {
                    result.Issues.Add($"Model file not found: {modelPath}");
                    return result;
                }

                var fileInfo = new FileInfo(modelPath);
                result.FileSizeBytes = fileInfo.Length;
                
                // Check minimum file size (should be at least a few MB for any valid model)
                if (fileInfo.Length < 1024 * 1024) // 1MB
                {
                    result.Issues.Add("File size too small to be a valid model");
                    return result;
                }

                // Check GGUF magic number
                using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(stream);
                
                var magic = reader.ReadUInt32();
                if (magic != GGUF_MAGIC)
                {
                    result.Issues.Add($"Invalid GGUF magic number. Expected: {GGUF_MAGIC:X8}, Found: {magic:X8}");
                    return result;
                }

                // Read version
                var version = reader.ReadUInt32();
                result.GGUFVersion = version;
                
                // Check supported versions (LLamaSharp supports GGUF v2 and v3)
                if (version < 2 || version > 3)
                {
                    result.Issues.Add($"Unsupported GGUF version: {version}. Supported versions: 2, 3");
                    return result;
                }

                // Read tensor count and metadata count
                var tensorCount = reader.ReadUInt64();
                var metadataCount = reader.ReadUInt64();
                
                result.TensorCount = tensorCount;
                result.MetadataCount = metadataCount;
                
                // Basic sanity checks
                if (tensorCount == 0)
                {
                    result.Issues.Add("Model has no tensors");
                    return result;
                }
                
                if (tensorCount > 10000) // Arbitrary large number
                {
                    result.Issues.Add($"Suspicious tensor count: {tensorCount}");
                    return result;
                }

                // Try to read some metadata to verify file structure
                try
                {
                    for (ulong i = 0; i < Math.Min(metadataCount, 10); i++)
                    {
                        // Read key length and key
                        var keyLength = reader.ReadUInt64();
                        if (keyLength > 1024) // Keys shouldn't be too long
                        {
                            result.Issues.Add($"Suspicious metadata key length: {keyLength}");
                            break;
                        }
                        
                        var keyBytes = reader.ReadBytes((int)keyLength);
                        var key = Encoding.UTF8.GetString(keyBytes);
                        
                        // Read value type and skip value
                        var valueType = reader.ReadUInt32();
                        
                        // Skip value based on type (simplified)
                        switch (valueType)
                        {
                            case 8: // GGUF_TYPE_STRING
                                var stringLength = reader.ReadUInt64();
                                reader.ReadBytes((int)stringLength);
                                break;
                            case 9: // GGUF_TYPE_ARRAY
                                // Skip array reading for now
                                var arrayType = reader.ReadUInt32();
                                var arrayLength = reader.ReadUInt64();
                                // This is getting complex, just break
                                goto SkipMetadata;
                            default:
                                // Skip simple types
                                var typeSize = valueType switch
                                {
                                    0 => 1,  // UINT8
                                    1 => 1,  // INT8
                                    2 => 2,  // UINT16
                                    3 => 2,  // INT16
                                    4 => 4,  // UINT32
                                    5 => 4,  // INT32
                                    6 => 4,  // FLOAT32
                                    7 => 1,  // BOOL
                                    10 => 8, // UINT64
                                    11 => 8, // INT64
                                    12 => 8, // FLOAT64
                                    _ => 0
                                };
                                if (typeSize > 0)
                                {
                                    reader.ReadBytes(typeSize);
                                }
                                break;
                        }
                    }
                    SkipMetadata:;
                }
                catch (Exception ex)
                {
                    result.Issues.Add($"Error reading metadata: {ex.Message}");
                    return result;
                }

                // If we got here, the file structure looks valid
                result.IsCompatible = true;
                result.Issues.Add("File appears to be a valid GGUF model");
                
                // Add file size warning for very large models
                if (fileInfo.Length > 8L * 1024 * 1024 * 1024) // 8GB
                {
                    result.Issues.Add("Warning: Large model file may require significant RAM");
                }

            }
            catch (Exception ex)
            {
                result.Issues.Add($"Error checking model compatibility: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Result of model compatibility check
    /// </summary>
    public class ModelCompatibilityResult
    {
        public string FilePath { get; set; } = string.Empty;
        public bool IsCompatible { get; set; }
        public List<string> Issues { get; set; } = new();
        public long FileSizeBytes { get; set; }
        public uint? GGUFVersion { get; set; }
        public ulong? TensorCount { get; set; }
        public ulong? MetadataCount { get; set; }
        
        public string FileSizeMB => $"{FileSizeBytes / 1024.0 / 1024.0:F2} MB";
    }
}
