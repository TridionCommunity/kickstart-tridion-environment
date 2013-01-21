using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ImportContentFromRss.Content;
using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;
using ItemType = Tridion.ContentManager.CoreService.Client.ItemType;

namespace ImportContentFromRss
{
    public class ContentManager
    {
        private List<Person> _persons;
        private List<Source> _sources;
        private List<string> _keywords;
        private bool _keywordDirty = false;
        private static readonly Dictionary<string, string> ResolvedUrls = new Dictionary<string, string>();
        private readonly SessionAwareCoreServiceClient _client;
        private readonly ReadOptions _readOptions;

        public ContentManager(SessionAwareCoreServiceClient client)
        {
            _client = client;
            _readOptions = new ReadOptions();
        }

        //public List<Page> GetNewsletterPages()
        //{
        //    List<Page> result = new List<Page>();
        //    StructureGroup sg = (StructureGroup)_session.GetObject(Constants.NewsletterStructureGroupUrl);
        //    OrganizationalItemItemsFilter filter = new OrganizationalItemItemsFilter(_session) { ItemTypes = new[] { ItemType.Page } };

        //    foreach (Page page in sg.GetItems(filter))
        //    {
        //        result.Add(page);
        //    }
        //    return result;
        //}

        public List<Article> GetArticlesForDate(DateTime date)
        {
            List<Article> result = new List<Article>();
            string folderId = GetFolderForDate(date);
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Component } };

            foreach (XElement node in _client.GetListXml(folderId, filter).Nodes())
            {
                result.Add(new Article((ComponentData)_client.Read(node.Attribute("ID").Value, _readOptions), _client));
            }

            return result;
        }

        public string GetFolderForDate(DateTime date)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            //Console.WriteLine("Searching folder for date " + date.ToString("yyyy-MM-dd"));
            //Folder start = (Folder)_session.GetObject(Constants.ArticleLocationUrl);
            OrganizationalItemItemsFilterData folders = new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Folder } };

            string year = null;
            string month = null;
            string day = null;

            string yearName = date.Year.ToString(CultureInfo.InvariantCulture);
            string monthName;
            if (date.Month < 10)
                monthName = "0" + date.Month;
            else
                monthName = date.Month.ToString(CultureInfo.InvariantCulture);
            monthName += " " + date.ToString("MMMM");

            string dayName;
            if (date.Day < 10)
                dayName = "0" + date.Day;
            else
                dayName = date.Day.ToString(CultureInfo.InvariantCulture);

            foreach (XElement folderElement in _client.GetListXml(Constants.ArticleLocationUrl, folders).Nodes()) //start.GetListItems(folders))
            {
                if (folderElement.Attribute("Title").Value.Equals(yearName))
                {
                    year = folderElement.Attribute("ID").Value;
                    break;
                }
            }
            if (year == null)
            {
                FolderData f =
                    (FolderData)_client.GetDefaultData(ItemType.Folder, Constants.ArticleLocationUrl);
                f.Title = yearName;
                f = (FolderData)_client.Save(f, _readOptions);
                year = f.Id;
            }

            foreach (XElement monthFolder in _client.GetListXml(year, folders).Nodes())//year.GetListItems(folders))
            {
                if (monthFolder.Attribute("Title").Value.Equals(monthName))
                {

                    month = monthFolder.Attribute("ID").Value;
                    break;
                }
            }
            if (month == null)
            {
                FolderData f = (FolderData)_client.GetDefaultData(ItemType.Folder, year);
                f.Title = monthName;
                f = (FolderData)_client.Save(f, _readOptions);
                month = f.Id;
            }
            foreach (XElement dayFolder in _client.GetListXml(month, folders).Nodes())//month.GetListItems(folders))
            {
                if (dayFolder.Attribute("Title").Value.Equals(dayName))
                {
                    day = dayFolder.Attribute("ID").Value;
                    break;
                }
            }
            if (day == null)
            {
                FolderData f = (FolderData)_client.GetDefaultData(ItemType.Folder, month);
                f.Title = dayName;
                f = (FolderData)_client.Save(f, _readOptions);
                day = f.Id;
            }
            watch.Stop();
            //Console.WriteLine("Returning folder " + day + " in " + watch.ElapsedMilliseconds + " milliseconds");
            return day;
        }


        public Person FindPersonByNameOrAlternate(string name, bool refresh = false)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            //Console.WriteLine("Searching for author " + name);
            foreach (Person person in GetPersons(refresh))
            {
                if (person.Name.ToLower().Equals(name.ToLower()))
                {
                    watch.Stop();
                    //Console.WriteLine("Found author in " + watch.ElapsedMilliseconds + " milliseconds");
                    return person;
                }
                if (person.AlternateNames.Count > 0)
                {
                    if (person.AlternateNames.Any(alternateName => alternateName.ToLower().Equals(name.ToLower())))
                    {
                        watch.Stop();
                        //Console.WriteLine("Found author in " + watch.ElapsedMilliseconds + " milliseconds");
                        return person;
                    }
                }
            }
            watch.Stop();
            //Console.WriteLine("Could not find author (" + watch.ElapsedMilliseconds + " milliseconds)");
            return null;
        }

        public string ResolveUrl(string webdavUrl)
        {
            if (ResolvedUrls.ContainsKey(webdavUrl))
            {
                return ResolvedUrls[webdavUrl];
            }
            Stopwatch watch = new Stopwatch(); watch.Start();
            IdentifiableObjectData i = _client.Read(webdavUrl, _readOptions);
            ResolvedUrls.Add(webdavUrl, i.Id);
            watch.Stop();
            Console.WriteLine("Resolved URL in " + watch.ElapsedMilliseconds + " milliseconds (" + webdavUrl + ")");
            return i.Id;
        }

        public List<Source> GetSources()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            //Console.WriteLine("Getting list of sources...");
            if (_sources == null)
            {
                _sources = new List<Source>();
                //Folder sourceFolder = (Folder)_session.GetObject(Constants.SourceLocationUrl);
                OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Component } };
                foreach (XElement node in _client.GetListXml(Constants.SourceLocationUrl, filter).Nodes())
                {
                    _sources.Add(new Source((ComponentData)_client.Read(node.Attribute("ID").Value, _readOptions), _client));
                }

            }
            watch.Stop();
            //Console.WriteLine("Returning " + _sources.Count + " sources in " + watch.ElapsedMilliseconds + " milliseconds");
            return _sources;
        }

        public List<Person> GetPersons(bool refresh = false)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            //Console.WriteLine("Getting List of people...");
            if (refresh) _persons = null;
            if (_persons == null)
            {
                _persons = new List<Person>();
                //Folder people = (Folder)_session.GetObject(Constants.PersonLocationUrl);
                OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Component } };
                foreach (XElement node in _client.GetListXml(Constants.PersonLocationUrl, filter).Nodes())
                {
                    _persons.Add(new Person((ComponentData)_client.Read(node.Attribute("ID").Value, _readOptions), _client));
                }
            }
            watch.Stop();
            //Console.WriteLine("Returning " + _persons.Count + " authors in " + watch.ElapsedMilliseconds + " milliseconds");
            return _persons;
        }

        public List<string> GetExistingKeywords(bool refresh = false)
        {
            if (_keywords == null || refresh)
            {
                _keywords = new List<string>();
                OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
                filter.ItemTypes = new[] { ItemType.Keyword };
                foreach (XElement node in _client.GetListXml(ResolveUrl(Constants.ContentCategoryUrl), filter).Nodes())
                {
                    _keywords.Add(node.Attribute("Title").Value);
                }
            }
            return _keywords;
        }

        public void CreateKeywords(List<string> keywords)
        {

            List<string> currentKeywords = GetExistingKeywords(_keywordDirty);
            foreach (var keyword in keywords)
            {
                if (currentKeywords.Contains(keyword)) continue;
                // Must create a new one
                _keywordDirty = true;
                KeywordData newKeyword = (KeywordData)_client.GetDefaultData(ItemType.Keyword,
                                                                             ResolveUrl(Constants.ContentCategoryUrl));
                newKeyword.Title = keyword;
                _client.Save(newKeyword, null);
            }
        }

        public string GetStructureGroup(string sgTitle, string parentStructureGroup)
        {
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            filter.ItemTypes = new[] { ItemType.StructureGroup };
            foreach (XElement node in _client.GetListXml(parentStructureGroup, filter).Nodes())
            {
                if (node.Attribute("Title").Value.Equals(sgTitle))
                    return node.Attribute("ID").Value;
            }
            StructureGroupData sg =
                (StructureGroupData)
                _client.GetDefaultData(ItemType.StructureGroup, parentStructureGroup);
            sg.Title = sgTitle;
            sg.Directory = Regex.Replace(sgTitle, "\\W", "").ToLowerInvariant().Replace("á", "a").Replace("ó", "o");
            sg = (StructureGroupData)_client.Save(sg, _readOptions);
            return sg.Id;
        }

        internal string GetUriInBlueprintContext(string itemId, string publicationId)
        {
            TcmUri itemUri = new TcmUri(itemId);
            TcmUri publicationUri = new TcmUri(publicationId);
            TcmUri inContext = new TcmUri(itemUri.ItemId, itemUri.ItemType, publicationUri.ItemId);
            return inContext.ToString();
        }

        public void AddToPage(string sg, Article article)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            string foldername;
            if (article.Date.Month < 10)
                foldername = "0" + article.Date.Month;
            else
                foldername = article.Date.Month.ToString(CultureInfo.InvariantCulture);
            foldername += " " + article.Date.ToString("MMMM");
            PageData page = (PageData)_client.Read(GetPage(sg, foldername), _readOptions);
            if (!page.IsEditable.GetValueOrDefault())
            {
                page = (PageData)_client.CheckOut(page.Id, true, _readOptions);
            }

            List<ComponentPresentationData> componentPresentations = page.ComponentPresentations.ToList();
            string articleId = GetUriInBlueprintContext(article.Id, ResolveUrl(Constants.WebSitePublication));
            string ctId = GetUriInBlueprintContext(ResolveUrl(Constants.ArticleComponentTemplateUrl),
                                                   ResolveUrl(Constants.WebSitePublication));
            ComponentPresentationData cp = new ComponentPresentationData();
            cp.Component = new LinkToComponentData { IdRef = articleId };
            cp.ComponentTemplate = new LinkToComponentTemplateData { IdRef = ctId };
            componentPresentations.Add(cp);
            page.ComponentPresentations = componentPresentations.ToArray();

            _client.Save(page, null);
            _client.CheckIn(page.Id, null);
            watch.Stop();
            Console.WriteLine("Added component presentation in " + watch.ElapsedMilliseconds + " milliseconds");
        }

        public string GetPage(string sg, string pageTitle)
        {
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            filter.ItemTypes = new[] { ItemType.Page };
            foreach (XElement node in _client.GetListXml(sg, filter).Nodes())
            {
                if (node.Attribute("Title").Value.Equals(pageTitle))
                    return node.Attribute("ID").Value;
            }
            PageData page = (PageData)_client.GetDefaultData(ItemType.Page, sg);
            page.Title = pageTitle;
            page.FileName = Regex.Replace(pageTitle, "\\W", "").ToLowerInvariant();
            page = (PageData)_client.Save(page, _readOptions);
            _client.CheckIn(page.Id, null);
            return GetVersionlessUri(page.Id);
        }

        public string GetVersionlessUri(string id)
        {
            TcmUri uri = new TcmUri(id);
            if (uri.IsVersionless) return id;
            uri = new TcmUri(uri.ItemId, uri.ItemType, uri.PublicationId);
            return uri.ToString();
        }
    }
}
