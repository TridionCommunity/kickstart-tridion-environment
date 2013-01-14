using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateAnEnvironmentForMe
{
    public static class Configuration
    {
        public static string TopPublicationId { get; set; }
        public static string SchemaPublicationId { get; set; }
        public static string TemplatePublicationId { get; set; }
        public static string ContentPublicationId { get; set; }
        public static string WebsitePublicationId { get; set; }

        public static string BuildingBlocksFolderId { get; set; }
        public static string SystemFolderId { get; set; }
        public static string SchemasFolderId { get; set; }
        public static string ContentFolderId { get; set; }
        public static string ArticleFolderId { get; set; }
        public static string OrganizationFolderId { get; set; }
        public static string InformationSourceFolderId { get; set; }
        public static string PersonFolderId { get; set; }
        public static string TemplateFolderId { get; set; }


        public static string EmbeddedLinksSchemaId { get; set; }
        public static string OrganizationSchemaId { get; set; }
        public static string PersonSchemaId { get; set; }
        public static string InformationSourceSchemaId { get; set; }
        public static string ArticleSchemaId { get; set; }

        public static string ContentCategoryId { get; set; }

        public const string ArticleSchemaFileName = "Article(tcm-25-2679-8)-Source2.xsd";
        public const string InformationSourceSchemaFileName = "Information-Source(tcm-25-3474-8)-Source.xsd";
        public const string PersonSchemaFileName = "Person(tcm-25-3473-8)-Source.xsd";
        public const string OrganizationSchemaFileName = "Organization(tcm-25-3472-8)-Source.xsd";
        public const string EmbeddedLinkSchemaFileName = "EmbeddedLinksSchema(tcm-25-2681-8)-Source.xsd";

        public const string UrlEnableInlineEditingForPagesTbb =
            "/webdav/000%20System%20Parent/Building%20Blocks/Default%20Templates/Enable%20inline%20editing%20for%20Page.tbbcs";
        public const string UrlEnableInlineEditingForContentTbb =
            "/webdav/000%20System%20Parent/Building%20Blocks/Default%20Templates/Enable%20inline%20editing%20for%20content.tbbcs";
    }
}
