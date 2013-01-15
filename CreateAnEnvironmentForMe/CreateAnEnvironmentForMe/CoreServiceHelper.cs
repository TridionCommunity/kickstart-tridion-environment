using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using ContentClasses;
using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;
using ItemType = Tridion.ContentManager.CoreService.Client.ItemType;

namespace CreateAnEnvironmentForMe
{
    internal class CoreServiceHelper
    {
        private readonly SessionAwareCoreServiceClient _client;
        private readonly ReadOptions _readOptions;

        internal bool CreateIfNewItem = true;

        internal CoreServiceHelper(SessionAwareCoreServiceClient client)
        {
            _client = client;
            _readOptions = new ReadOptions();
        }

        internal string GetPublication(string publicationTitle, params string[] parentIds)
        {
            // Console.WriteLine("Getting Publication " + publicationTitle);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            string publicationId = TcmUri.UriNull;

            PublicationsFilterData publicationsFilter = new PublicationsFilterData();
            foreach (XElement pub in _client.GetSystemWideListXml(publicationsFilter).Nodes())
            {
                if (!pub.Attribute("Title").Value.Equals(publicationTitle)) continue;
                publicationId = pub.Attribute("ID").Value;
                Console.WriteLine("Found publication with ID " + publicationId);
                break;
            }
            if (publicationId.Equals(TcmUri.UriNull) && CreateIfNewItem)
            {
                // New Publication
                PublicationData newPublication =
                    (PublicationData)_client.GetDefaultData(ItemType.Publication, null, _readOptions);
                newPublication.Title = publicationTitle;
                newPublication.Key = publicationTitle;

                if (parentIds.Length > 0)
                {
                    List<LinkToRepositoryData> parents = new List<LinkToRepositoryData>();
                    foreach (string parentId in parentIds)
                    {
                        LinkToRepositoryData linkToParent = new LinkToRepositoryData { IdRef = parentId };
                        parents.Add(linkToParent);
                    }

                    newPublication.Parents = parents.ToArray();
                }

                newPublication = (PublicationData)_client.Save(newPublication, _readOptions);
                publicationId = newPublication.Id;
                Console.WriteLine("Created publication with ID " + publicationId);

            }
            watch.Stop();
            Console.WriteLine("GetPublication finished in " + watch.ElapsedMilliseconds + " milliseconds.");
            return publicationId;
        }

        internal void CreateOrIgnoreRootStructureGroup(string rootStructureGroupTitle, string publicationId)
        {
            PublicationData publication = (PublicationData)_client.Read(publicationId, _readOptions);
            if (publication.RootStructureGroup.IdRef == TcmUri.UriNull)
            {
                StructureGroupData structureGroup =
                    (StructureGroupData)_client.GetDefaultData(ItemType.StructureGroup, publicationId, _readOptions);
                structureGroup.Title = rootStructureGroupTitle;
                _client.Save(structureGroup, null);
            }
        }

        internal string GetBuildingBlocksFolderId(string publicationId)
        {
            PublicationData publication = (PublicationData)_client.Read(publicationId, _readOptions);
            return publication.RootFolder.IdRef;
        }

        internal string GetFolder(string folderTitle, string parentFolderId)
        {
            //Console.WriteLine("Searching for folder with name " + folderTitle + " in folder " + parentFolderId);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            string folderId = TcmUri.UriNull;
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData
                                                           {
                                                               ItemTypes =
                                                                   new[]
                                                                       {
                                                                           ItemType
                                                                               .Folder
                                                                       }
                                                           };
            foreach (XElement node in _client.GetListXml(parentFolderId, filter).Nodes())
            {
                if (!node.Attribute("Title").Value.Equals(folderTitle)) continue;
                folderId = node.Attribute("ID").Value;
                Console.WriteLine("Found folder with ID " + folderId);
                break;
            }
            if (folderId.Equals(TcmUri.UriNull) && CreateIfNewItem)
            {
                // New folder
                FolderData folder = (FolderData)_client.GetDefaultData(ItemType.Folder, parentFolderId, _readOptions);
                folder.Title = folderTitle;
                folder = (FolderData)_client.Save(folder, _readOptions);
                folderId = folder.Id;
                Console.WriteLine("Created folder with ID " + folderId);
            }
            watch.Stop();
            Console.WriteLine("GetFolder finished in " + watch.ElapsedMilliseconds + " milliseconds.");
            return folderId;
        }

        internal string GetSchema(string schemaTitle, string parentFolderId, string xsd = null, SchemaPurpose purpose = SchemaPurpose.UnknownByClient, string rootElementName = "Content")
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Console.WriteLine("Creating schema " + schemaTitle);
            string schemaId = TcmUri.UriNull;
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData
                                                           {
                                                               ItemTypes =
                                                                   new[]
                                                                       {
                                                                           ItemType
                                                                               .Schema
                                                                       }
                                                           };
            foreach (XElement node in _client.GetListXml(parentFolderId, filter).Nodes())
            {
                if (!node.Attribute("Title").Value.Equals(schemaTitle)) continue;
                schemaId = node.Attribute("ID").Value;
            }
            if (schemaId.Equals(TcmUri.UriNull) && xsd != null && CreateIfNewItem && purpose != SchemaPurpose.UnknownByClient)
            {
                SchemaData schema = (SchemaData)_client.GetDefaultData(ItemType.Schema, parentFolderId, _readOptions);
                schema.Title = schemaTitle;
                schema.Description = schemaTitle;
                schema.Purpose = purpose;
                schema.RootElementName = rootElementName;
                schema.Xsd = xsd;
                schema = (SchemaData)_client.Save(schema, _readOptions);
                schema = (SchemaData)_client.CheckIn(schema.Id, _readOptions);
                schemaId = schema.Id;

            }
            watch.Stop();
            Console.WriteLine("Returning Schema ID " + schemaId + " (" + watch.ElapsedMilliseconds + " milliseconds)");
            return schemaId;
        }

        internal string GetDefaultMultimediaSchemaForPublication(string publicationId)
        {
            PublicationData publication = (PublicationData)_client.Read(publicationId, _readOptions);
            return publication.DefaultMultimediaSchema.IdRef;
        }

        internal string GetCategory(string categoryTitle, string publicationId)
        {
            string categoryId = TcmUri.UriNull;
            CategoriesFilterData filter = new CategoriesFilterData();
            foreach (XElement node in _client.GetListXml(publicationId, filter).Nodes())
            {
                if (!node.Attribute("Title").Value.Equals(categoryTitle)) continue;
                categoryId = node.Attribute("ID").Value;
                break;
            }
            if (categoryId.Equals(TcmUri.UriNull))
            {
                CategoryData category =
                    (CategoryData)_client.GetDefaultData(ItemType.Category, publicationId, _readOptions);
                category.Title = categoryTitle;
                category.XmlName = categoryTitle;
                category = (CategoryData)_client.Save(category, _readOptions);
                categoryId = category.Id;
            }
            return categoryId;
        }

        internal string GetUriInBlueprintContext(string itemId, string publicationId)
        {
            TcmUri itemUri = new TcmUri(itemId);
            TcmUri publicationUri = new TcmUri(publicationId);
            TcmUri inContext = new TcmUri(itemUri.ItemId, itemUri.ItemType, publicationUri.ItemId);
            return inContext.ToString();
        }

        internal List<Source> GetListSources()
        {
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData
                                                           {
                                                               ItemTypes =
                                                                   new[]
                                                                       {
                                                                           ItemType
                                                                               .Component
                                                                       }
                                                           };
            List<Source> sources = new List<Source>();
            foreach (XElement node in _client.GetListXml(Configuration.InformationSourceFolderId, filter).Nodes())
            {
                ComponentData component = (ComponentData)_client.Read(node.Attribute("ID").Value, _readOptions);
                sources.Add(new Source(component, _client));
            }
            return sources;
        }

        internal Source CreateSource(string name, string rssFeedUrl, string websiteUrl)
        {
            string informationSourceFolder = GetUriInBlueprintContext(Configuration.InformationSourceFolderId,
                                                                      Configuration.ContentPublicationId);
            string informationSourceSchema = GetUriInBlueprintContext(Configuration.InformationSourceSchemaId,
                                                                      Configuration.ContentPublicationId);


            Console.WriteLine("Creating new source " + name);
            ComponentData component =
                (ComponentData)
                _client.GetDefaultData(ItemType.Component, informationSourceFolder, _readOptions);
            component.Schema = new LinkToSchemaData { IdRef = informationSourceSchema };
            component.Title = name;
            component.Id = TcmUri.UriNull;

            Source source = new Source(component, _client);
            source.RssFeedUrl = rssFeedUrl;
            source.WebsiteUrl = websiteUrl;
            source.Save(true);
            return source;
        }

        internal string GetComponentTemplateForSchema(string schemaId, string componentTemplateTitle)
        {
            string container = GetUriInBlueprintContext(Configuration.TemplateFolderId,
                                                        Configuration.TemplatePublicationId);
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            filter.ItemTypes = new[] { ItemType.ComponentTemplate };
            foreach (XElement node in _client.GetListXml(container, filter).Nodes())
            {
                if (node.Attribute("Title").Value.Equals(componentTemplateTitle))
                    return node.Attribute("ID").Value;
            }
            ComponentTemplateData ct = (ComponentTemplateData)_client.GetDefaultData(ItemType.ComponentTemplate, container, _readOptions);
            ct.Title = componentTemplateTitle;
            List<LinkToSchemaData> schemaLinks = new List<LinkToSchemaData>();
            schemaLinks.Add(new LinkToSchemaData { IdRef = GetUriInBlueprintContext(schemaId, Configuration.TemplatePublicationId) });
            ct.RelatedSchemas = schemaLinks.ToArray();
            ct = (ComponentTemplateData)_client.Save(ct, _readOptions);
            _client.CheckIn(ct.Id, null);
            return ct.Id;

        }

        internal void AddSiteEditToTemplate(string templateId)
        {
            TcmUri uri = new TcmUri(templateId);
            templateId = uri.GetVersionlessUri();
            TemplateData template = (TemplateData)_client.Read(templateId, _readOptions);
            // find siteedit templates
            string context = template.BluePrintInfo.OwningRepository.IdRef;
            TemplateBuildingBlockData tbb;
            if (template is ComponentTemplateData)
            {
                tbb =
                    (TemplateBuildingBlockData)
                    _client.Read(Configuration.UrlEnableInlineEditingForContentTbb, _readOptions);
            }
            else
            {
                tbb = (TemplateBuildingBlockData)_client.Read(Configuration.UrlEnableInlineEditingForPagesTbb, _readOptions);
            }

            string tbbId = GetUriInBlueprintContext(tbb.Id, context);
            XDocument content = XDocument.Parse(template.Content);
            // Find default finish actions
            XElement dfa = null;
            XNamespace xlink = XNamespace.Get("http://www.w3.org/1999/xlink");
            XNamespace modularTemplate = XNamespace.Get("http://www.tridion.com/ContentManager/5.3/CompoundTemplate");

            foreach (XElement node in content.Root.Nodes())
            {
                if (node.Element(modularTemplate+"Template").Attribute(xlink+"title").Value.Equals("Default Finish Actions"))
                {
                    dfa = node;
                    break;
                }
            }
            if(dfa != null)
            {
                // Must create Element and insert before
                //  <TemplateInvocation><Template xlink:href="tcm:3-32-2048" xmlns:xlink="http://www.w3.org/1999/xlink" xlink:title="Default Finish Actions" /></TemplateInvocation>

                string fakeSe =
                    "<TemplateInvocation xmlns=\"http://www.tridion.com/ContentManager/5.3/CompoundTemplate\"><Template xlink:href=\"##TBBID##\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xlink:title=\"##TBBTITLE##\" /></TemplateInvocation>";
                if(template is PageTemplateData)
                {
                    fakeSe = "<TemplateInvocation xmlns=\"http://www.tridion.com/ContentManager/5.3/CompoundTemplate\"><Template xlink:href=\"##TBBID##\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xlink:title=\"##TBBTITLE##\"/><TemplateParameters><Parameters xmlns=\"##PARAMETERSCHEMANAMESPACE##\"><SiteEditURL>http://localhost/WebUI/Editors/SiteEdit/</SiteEditURL></Parameters></TemplateParameters></TemplateInvocation>";
                    string schemaId = tbb.ParameterSchema.IdRef;
                    SchemaData schema = (SchemaData)_client.Read(schemaId, _readOptions);
                    fakeSe = fakeSe.Replace("##PARAMETERSCHEMANAMESPACE##", schema.NamespaceUri);
                }
                fakeSe = fakeSe.Replace("##TBBID##", tbbId);
                fakeSe = fakeSe.Replace("##TBBTITLE##", tbb.Title);
                //Avoid adding SiteEdit TBB a million times...
                if (!template.Content.Contains(tbb.Title))
                {
                    XElement element = XElement.Parse(fakeSe);
                    dfa.AddBeforeSelf(element);
                    template.Content = content.ToString();
                    _client.CheckOut(template.Id, false, null);
                    _client.Save(template, null);
                    _client.CheckIn(template.Id, null);
                }
            }

        }

        internal void CreatePublicationTarget(XElement site, WebsiteHelper.TargetLanguage language)
        {
            string targetName = site.Element("Name").Value;
            string targetTypeId = TcmUri.UriNull;
            string url = string.Empty;
            if (language == WebsiteHelper.TargetLanguage.Aspnet)
                url = "http://localhost:" + site.Element("Port").Value + "/httpupload.aspx";
            else
                url = "http://localhost:" + site.Element("Port").Value + "/" + site.Element("ContextRoot").Value + "/httpupload";

            TargetTypesFilterData filter = new TargetTypesFilterData();
            //filter.ForRepository.IdRef = Configuration.WebsitePublicationId;
            foreach (XElement node in _client.GetSystemWideListXml(filter).Nodes())
            {
                if (node.Attribute("Title").Value.Equals(targetName))
                {
                    targetTypeId = node.Attribute("ID").Value;
                    break;
                }
            }
            if (targetTypeId.Equals(TcmUri.UriNull))
            {
                TargetTypeData targetType =
                    (TargetTypeData)_client.GetDefaultData(ItemType.TargetType, null, _readOptions);
                targetType.Title = targetName;
                targetType.Description = targetName;
                targetType = (TargetTypeData)_client.Save(targetType, _readOptions);
                targetTypeId = targetType.Id;
            }

            PublicationTargetsFilterData pFilter = new PublicationTargetsFilterData();
            string pTargetId = TcmUri.UriNull;
            foreach (XElement node in _client.GetSystemWideListXml(pFilter).Nodes())
            {
                if (node.Attribute("Title").Value.Equals(targetName))
                {
                    pTargetId = node.Attribute("ID").Value;
                    break;
                }
            }
            if (pTargetId.Equals(TcmUri.UriNull))
            {
                PublicationTargetData pt =
                    (PublicationTargetData)_client.GetDefaultData(ItemType.PublicationTarget, null, _readOptions);
                pt.Title = targetName;
                pt.Description = targetName;
                pt.Publications = new[] { new LinkToPublicationData { IdRef = Configuration.WebsitePublicationId } };
                pt.TargetTypes = new[] { new LinkToTargetTypeData { IdRef = targetTypeId } };
                TargetDestinationData destination = new TargetDestinationData
                                                        {
                                                            ProtocolSchema = new LinkToSchemaData { IdRef = "tcm:0-6-8" },
                                                            Title = targetName,
                                                            ProtocolData =
                                                                "<HTTPS xmlns=\"http://www.tridion.com/ContentManager/5.0/Protocol/HTTPS\"><UserName>notused</UserName><Password>notused</Password><URL>" +
                                                                url + "</URL></HTTPS>"
                                                        };
                pt.Destinations = new[] { destination };
                if (language == WebsiteHelper.TargetLanguage.Aspnet)
                    pt.TargetLanguage = "ASP.NET";
                else
                    pt.TargetLanguage = "JSP";

                _client.Save(pt, null);
            }


        }

        internal string GetDefaultPageTemplate(string publicationId)
        {
            PublicationData pub = (PublicationData)_client.Read(publicationId, _readOptions);
            return pub.DefaultPageTemplate.IdRef;
        }

        internal string CreateTaskProcess(string processName, string publicationId)
        {
            //TODO: To implement as a repeating task
            return null;
            ProcessDefinitionsFilterData filter = new ProcessDefinitionsFilterData();
            filter.ContextRepository.IdRef = publicationId;

            foreach (XElement node in _client.GetSystemWideListXml(filter).Nodes())
            {
                if (node.Attribute("Title").Value.Equals(processName))
                    return node.Attribute("ID").Value;
            }

            ProcessDefinitionData processDefinition =
                (ProcessDefinitionData)_client.GetDefaultData(ItemType.ProcessDefinition, publicationId, _readOptions);

            processDefinition.Title = processName;
            processDefinition.StoreSnapshot = false;

        }

    }
}
