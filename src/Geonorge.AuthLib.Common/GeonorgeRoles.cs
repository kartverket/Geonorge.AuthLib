namespace Geonorge.AuthLib.Common
{
    /// <summary>
    /// All user roles in Geonorge should be defined here. Use these constants instead of magic strings through out the application. 
    /// </summary>
    public static class GeonorgeRoles
    {
        public const string MetadataAdmin = "nd.metadata_admin";
        public const string MetadataEditor = "nd.metadata_editor";
        public const string DokAdmin = "nd.dok_admin";
        public const string DokEditor = "nd.dok_editor";
        public const string MetadataManager = "nd.metadata_forvalter";
    }
}