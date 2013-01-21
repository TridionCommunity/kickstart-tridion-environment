using System.Security.Principal;
using System.ServiceModel;
using ContentClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Tridion.ContentManager.CoreService.Client;

namespace CreateAnEnvironmentForMe
{
    class Program
    {
        private static void Main(string[] args)
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("You must run this application as Administrator. Please restart as administrator");
                Console.Read();
                return;
            }
            string schemaLocation;
            if (args.Length > 0)
            {
                schemaLocation = args[0];
            }
            else
            {
                schemaLocation = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Schemas";
            }
            //Console.WriteLine("Loaded, will use " + schemaLocation + " as path to schema location.");

            //Console.WriteLine("Validating schemas exist...");
            if (!File.Exists(schemaLocation + Path.DirectorySeparatorChar + Configuration.ArticleSchemaFileName))
            {
                Console.WriteLine("Could not find Article schema at " + schemaLocation + Path.DirectorySeparatorChar +
                                  Configuration.ArticleSchemaFileName);
                return;
            }

            if (
                !File.Exists(schemaLocation + Path.DirectorySeparatorChar +
                             Configuration.InformationSourceSchemaFileName))
            {
                Console.WriteLine("Could not find Information Source schema at " + schemaLocation +
                                  Path.DirectorySeparatorChar +
                                  Configuration.InformationSourceSchemaFileName);
                return;
            }

            if (!File.Exists(schemaLocation + Path.DirectorySeparatorChar + Configuration.PersonSchemaFileName))
            {
                Console.WriteLine("Could not find Person schema at " + schemaLocation + Path.DirectorySeparatorChar +
                                  Configuration.PersonSchemaFileName);
                return;
            }

            if (!File.Exists(schemaLocation + Path.DirectorySeparatorChar + Configuration.OrganizationSchemaFileName))
            {
                Console.WriteLine("Could not find Organization schema at " + schemaLocation +
                                  Path.DirectorySeparatorChar +
                                  Configuration.OrganizationSchemaFileName);
                return;
            }

            if (!File.Exists(schemaLocation + Path.DirectorySeparatorChar + Configuration.EmbeddedLinkSchemaFileName))
            {
                Console.WriteLine("Could not find Embedded Link schema at " + schemaLocation +
                                  Path.DirectorySeparatorChar +
                                  Configuration.EmbeddedLinkSchemaFileName);
                return;
            }

            SessionAwareCoreServiceClient client = new SessionAwareCoreServiceClient("netTcp_2011");
            Console.WriteLine("Connected to Tridion with version: " + client.GetApiVersion());

            // Create Blueprint
            CoreServiceHelper helper = new CoreServiceHelper(client) { CreateIfNewItem = true };
            Configuration.TopPublicationId = helper.GetPublication("000 System Parent");
            helper.CreateOrIgnoreRootStructureGroup("Root", Configuration.TopPublicationId);
            Configuration.SchemaPublicationId = helper.GetPublication("010 Schemas", Configuration.TopPublicationId);
            Configuration.TemplatePublicationId = helper.GetPublication("020 Design", Configuration.SchemaPublicationId);
            Configuration.ContentPublicationId = helper.GetPublication("020 Content", Configuration.SchemaPublicationId);
            Configuration.WebsitePublicationId = helper.GetPublication("050 Website EN",
                                                                       Configuration.TemplatePublicationId,
                                                                       Configuration.ContentPublicationId);

            // Create top level folders
            Configuration.BuildingBlocksFolderId = helper.GetBuildingBlocksFolderId(Configuration.SchemaPublicationId);

            Configuration.SystemFolderId = helper.GetFolder("System", Configuration.BuildingBlocksFolderId);
            Configuration.SchemasFolderId = helper.GetFolder("Schemas", Configuration.SystemFolderId);

            Configuration.ContentFolderId = helper.GetFolder("Content", Configuration.BuildingBlocksFolderId);

            // Create Template folder
            Configuration.TemplateFolderId = helper.GetFolder("Templates", Configuration.SystemFolderId);

            // Create ContentCategory
            Configuration.ContentCategoryId = helper.GetCategory("ContentCategory", Configuration.SchemaPublicationId);

            // Create Schemas
            string file = schemaLocation + Path.DirectorySeparatorChar + Configuration.EmbeddedLinkSchemaFileName;
            string xsd = File.ReadAllText(file);
            Configuration.EmbeddedLinksSchemaId = helper.GetSchema("EmbeddedLinksSchema", Configuration.SchemasFolderId,
                                                                   xsd, SchemaPurpose.Embedded, "Links");

            // Organization Schema

            file = schemaLocation + Path.DirectorySeparatorChar + Configuration.OrganizationSchemaFileName;
            xsd = File.ReadAllText(file);
            xsd = xsd.Replace("tcm:25-2678-8",
                              helper.GetDefaultMultimediaSchemaForPublication(Configuration.SchemaPublicationId));

            Configuration.OrganizationSchemaId = helper.GetSchema("Organization", Configuration.SchemasFolderId, xsd,
                                                                  SchemaPurpose.Component);

            // Person Schema
            file = schemaLocation + Path.DirectorySeparatorChar + Configuration.PersonSchemaFileName;
            xsd = File.ReadAllText(file);
            xsd = xsd.Replace("tcm:25-3472-8", Configuration.OrganizationSchemaId);
            xsd = xsd.Replace("tcm:25-2678-8",
                              helper.GetDefaultMultimediaSchemaForPublication(Configuration.SchemaPublicationId));

            Configuration.PersonSchemaId = helper.GetSchema("Person", Configuration.SchemasFolderId, xsd,
                                                            SchemaPurpose.Component);

            // Information Source
            file = schemaLocation + Path.DirectorySeparatorChar + Configuration.InformationSourceSchemaFileName;
            xsd = File.ReadAllText(file);
            xsd = xsd.Replace("tcm:25-3472-8", Configuration.OrganizationSchemaId);
            xsd = xsd.Replace("tcm:25-3473-8", Configuration.PersonSchemaId);

            Configuration.InformationSourceSchemaId = helper.GetSchema("InformationSource",
                                                                       Configuration.SchemasFolderId, xsd,
                                                                       SchemaPurpose.Component);

            // Article

            file = schemaLocation + Path.DirectorySeparatorChar + Configuration.ArticleSchemaFileName;
            xsd = File.ReadAllText(file);
            // Replacements
            xsd = xsd.Replace("tcm:25-2681-8", Configuration.EmbeddedLinksSchemaId);
            xsd = xsd.Replace("tcm:0-25-1", Configuration.SchemaPublicationId);

            xsd = xsd.Replace("tcm:25-3473-8", Configuration.PersonSchemaId);
            xsd = xsd.Replace("tcm:25-3474-8", Configuration.InformationSourceSchemaId);

            Configuration.ArticleSchemaId = helper.GetSchema("Article", Configuration.SchemasFolderId, xsd,
                                                             SchemaPurpose.Component);

            // Create Content Folders
            string contentFolder = helper.GetUriInBlueprintContext(Configuration.ContentFolderId,
                                                                   Configuration.ContentPublicationId);
            Configuration.ArticleFolderId = helper.GetFolder("Articles", contentFolder);

            Configuration.OrganizationFolderId = helper.GetFolder("Organizations", contentFolder);

            Configuration.PersonFolderId = helper.GetFolder("People", contentFolder);

            Configuration.InformationSourceFolderId = helper.GetFolder("Information Sources", contentFolder);


            Console.WriteLine("Finished creating schemas & Publications. Creating content sources...");

            string[] sourceFiles = Directory.GetFiles(Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Sources");
            List<Source> sources = helper.GetListSources();

            foreach (var sourceFile in sourceFiles)
            {
                string sourceName = Path.GetFileNameWithoutExtension(sourceFile);
                Console.WriteLine("Creating information source " + sourceName);
                // Let's see if the source already exists
                bool sourceExists = false;
                foreach (Source source in sources)
                {
                    if (source.Title.Equals(sourceName))
                    {
                        sourceExists = true;
                        break;
                    }

                }
                if (sourceExists) continue;
                XDocument sourceContent = XDocument.Parse(File.ReadAllText(sourceFile));
                XNamespace sourceNs = "http://www.sdtridionworld.com/Content/Source";
                XNamespace xlinkNs = "http://www.w3.org/1999/xlink";
                string rssFeedUrl = sourceContent.Element(sourceNs + "Content").Element(sourceNs + "RssFeedUrl").Attribute(xlinkNs + "href").Value;
                string websiteUrl = string.Empty;
                if (sourceContent.Element(sourceNs + "Content").Element(sourceNs + "WebsiteUrl") != null)
                    websiteUrl = sourceContent.Element(sourceNs + "Content").Element(sourceNs + "WebsiteUrl").Value;

                Source newSource = helper.CreateSource(sourceName, rssFeedUrl, websiteUrl);

            }

            // Create a component template
            string templateId = helper.GetComponentTemplateForSchema(Configuration.ArticleSchemaId, "Default Article Template");
            helper.AddSiteEditToTemplate(templateId);
            helper.AddSiteEditToTemplate(helper.GetDefaultPageTemplate(Configuration.TopPublicationId));

            // Try to create task definition...

            // Setup a deployer
            // Fun starts now

            WebsiteHelper websiteHelper = new WebsiteHelper();
            XDocument sites = XDocument.Load("SitesToCreate.xml");
            foreach (XElement site in sites.Elements("Sites").Elements("Site"))
            {
                // Let's default to PreviewWeb... have to start somewhere
                WebsiteHelper.Role theRole = WebsiteHelper.Role.PreviewWeb;

                string root = site.Element("Root").Value;
                string name = site.Element("Name").Value;
                WebsiteHelper.TargetLanguage language;
                if (site.Element("Language").Value.ToLower().Equals(".net"))
                    language = WebsiteHelper.TargetLanguage.Aspnet;
                else
                    language = WebsiteHelper.TargetLanguage.Jsp;
                string role = site.Element("Role").Value.ToLower();
                switch (role.ToLower())
                {
                    case "upload":
                        theRole = WebsiteHelper.Role.Upload;
                        helper.CreatePublicationTarget(site, language);
                        break;
                    case "preview-web":
                        theRole = WebsiteHelper.Role.PreviewWeb;
                        break;
                    case "preview-webservice":
                        theRole = WebsiteHelper.Role.PreviewWebService;
                        break;
                }


                int port = 0;
                if (site.Element("Port") != null)
                    port = Convert.ToInt32(site.Element("Port").Value);

                Console.WriteLine("Creating " + role + " root folder in " + root);

                websiteHelper.CopyFilesForWebsite(root, "WebsiteFileList.xml", theRole, language);

                websiteHelper.CreateConfigFiles(root, role, name, site, language);
                if (language == WebsiteHelper.TargetLanguage.Aspnet)
                    websiteHelper.CreateWebsite(root, name, port);
            }
            Console.WriteLine("Done");
            Console.Read();

        }
    }
}
