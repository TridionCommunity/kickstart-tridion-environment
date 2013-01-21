using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using ImportContentFromRss.Content;
using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;

namespace ImportContentFromRss
{
    class Program
    {
        static void Main(string[] args)
        {

            SessionAwareCoreServiceClient client = new SessionAwareCoreServiceClient("netTcp_2011");

            ContentManager cm = new ContentManager(client);

            List<Source> sources = cm.GetSources();
            int countSources = sources.Count;
            Console.WriteLine("Loaded " + countSources + " sources. Starting to process.");
            XmlNamespaceManager nm = new XmlNamespaceManager(new NameTable());
            nm.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");

            Dictionary<Source, List<Article>> addedContent = new Dictionary<Source, List<Article>>();

            foreach (var source in sources)
            {
                Console.WriteLine("Loading content for source " + source.Title);
                XmlDocument feedXml = null;

                WebRequest wrq = WebRequest.Create(source.RssFeedUrl);
                wrq.Proxy = WebRequest.DefaultWebProxy;
                wrq.Proxy.Credentials = CredentialCache.DefaultCredentials;

                XmlTextReader reader = null;
                try
                {
                    reader = new XmlTextReader(wrq.GetResponse().GetResponseStream());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not read response from source" + ex.Message);
                }
                if (reader == null) continue;

                SyndicationFeed feed = SyndicationFeed.Load(reader);

                int countItems = feed.Items.Count();
                Console.WriteLine("Loaded " + countItems + " items from source. Processing");
                int count = 0;
                List<Article> newArticles = new List<Article>();
                foreach (var item in feed.Items)
                {
                    count++;
                    Person author = null;
                    if (item.Authors.Count == 0)
                    {
                        //Console.WriteLine("Could not find an author in feed source, checking for default");
                        if (source.DefaultAuthor != null)
                        {
                            author = source.DefaultAuthor;
                            //Console.WriteLine("Using default author " + author.Name);
                        }
                        else
                        {
                            //Console.WriteLine("Could not find default author, being creative");
                            if (feedXml == null)
                            {
                                try
                                {
                                    feedXml = new XmlDocument();
                                    feedXml.Load(source.RssFeedUrl);
                                }catch (Exception ex)
                                {
                                    Console.WriteLine("Something went wrong loading " + source.RssFeedUrl);
                                    Console.WriteLine(ex.ToString());
                                }

                            }
                            if (feedXml != null)
                            {
                                string xpath = "/rss/channel/item[" + count + "]/dc:creator";
                                if (feedXml.SelectSingleNode(xpath, nm) != null)
                                {
                                    author =
                                        cm.FindPersonByNameOrAlternate(feedXml.SelectSingleNode(xpath, nm).InnerText);
                                    if (author == null)
                                    {
                                        author = new Person(client);
                                        author.Name = feedXml.SelectSingleNode(xpath, nm).InnerText;
                                        author.Save();
                                        author =
                                            cm.FindPersonByNameOrAlternate(
                                                feedXml.SelectSingleNode(xpath, nm).InnerText, true);
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        string nameOrAlternate;
                        SyndicationPerson syndicationPerson = item.Authors.First();
                        if (string.IsNullOrEmpty(syndicationPerson.Name))
                            nameOrAlternate = syndicationPerson.Email;
                        else
                            nameOrAlternate = syndicationPerson.Name;

                        author = cm.FindPersonByNameOrAlternate(nameOrAlternate);
                    }
                    if (author == null)
                    {
                        string name = string.Empty;
                        if (item.Authors.Count > 0)
                        {
                            if (item.Authors[0].Name != null)
                                name = item.Authors[0].Name;
                            else
                                name = item.Authors[0].Email;
                        }
                        author = new Person(client) { Name = name };
                        if (source.IsStackOverflow)
                        {
                            author.StackOverflowId = item.Authors[0].Uri;
                        }
                        author.Save();
                        author = cm.FindPersonByNameOrAlternate(name, true);
                    }

                    List<Person> authors = new List<Person>();
                    authors.Add(author);
                    //Console.WriteLine("Using author: " + author.Name);

                    if (source.IsStackOverflow)
                    {
                        if (string.IsNullOrEmpty(author.StackOverflowId))
                        {
                            author.StackOverflowId = item.Authors[0].Uri;
                            author.Save(true);
                        }
                    }

                    if (item.PublishDate.DateTime > DateTime.MinValue)
                    {
                        // Organize content by Date
                        // Year
                        // Month
                        // Day
                        string store = cm.GetFolderForDate(item.PublishDate.DateTime);
                        string articleTitle;
                        if (string.IsNullOrEmpty(item.Title.Text))
                        {
                            articleTitle = "No title specified";
                        }
                        else
                        {
                            articleTitle = item.Title.Text;
                        }
                        articleTitle = articleTitle.Trim();

                        OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
                        bool alreadyExists = false;
                        foreach (XElement node in client.GetListXml(store, filter).Nodes())
                        {
                            if (!node.Attribute("Title").Value.Equals(articleTitle)) continue;
                            alreadyExists = true;
                            break;
                        }
                        if (!alreadyExists)
                        {

                            //Console.WriteLine(articleTitle + " is a new article. Saving");
                            Console.Write(".");
                            Article article = new Article(client, new TcmUri(store));

                            string content = "";
                            string summary = "";
                            try
                            {
                                if (item.Content != null)
                                {
                                    content = ((TextSyndicationContent)item.Content).Text;
                                }
                                if (item.Summary != null)
                                {
                                    summary = item.Summary.Text;
                                }
                                if (!string.IsNullOrEmpty(content))
                                {
                                    try
                                    {
                                        content = Utilities.ConvertHtmlToXhtml(content);
                                    }
                                    catch (Exception)
                                    {
                                        content = null;
                                    }
                                }
                                if (!string.IsNullOrEmpty(summary))
                                {
                                    try
                                    {
                                        summary = Utilities.ConvertHtmlToXhtml(summary);
                                    }
                                    catch (Exception)
                                    {
                                        summary = null;
                                    }

                                }

                                if (string.IsNullOrEmpty(summary))
                                {
                                    summary = !string.IsNullOrEmpty(content) ? content : "Could not find summary";
                                }
                                if (string.IsNullOrEmpty(content))
                                {
                                    content = !string.IsNullOrEmpty(summary) ? summary : "Could not find content";
                                }

                            }
                            catch (Exception ex)
                            {
                                content = "Could not convert source description to XHtml. " + ex.Message;
                                content += ((TextSyndicationContent)item.Content).Text;
                            }
                            article.Authors = authors;
                            article.Body = content;
                            article.Date = item.PublishDate.DateTime;
                            article.DisplayTitle = item.Title.Text;
                            article.Title = articleTitle;
                            article.Summary = summary;
                            article.Url = item.Links.First().Uri.AbsoluteUri;
                            List<string> categories = new List<string>();
                            foreach (var category in item.Categories)
                            {
                                categories.Add(category.Name);
                            }
                            article.Categories = categories;
                            article.Source = source;
                            article.Save();
                            newArticles.Add(article);
                        }
                    }

                }
                if (newArticles.Count > 0)
                {
                    addedContent.Add(source, newArticles);
                }
            }

            if (addedContent.Count > 0)
            {
                Console.WriteLine("============================================================");
                Console.WriteLine("Added content");
                foreach (Source source in addedContent.Keys)
                {
                    string sg = cm.GetStructureGroup(source.Title, cm.ResolveUrl(Constants.RootStructureGroup));
                    Console.WriteLine("Source: " + source.Content.Title + "(" + addedContent[source].Count + ")");
                    foreach (Article article in addedContent[source])
                    {
                        string yearSg = cm.GetStructureGroup(article.Date.Year.ToString(CultureInfo.InvariantCulture), sg);
                        cm.AddToPage(yearSg, article);
                        Console.WriteLine(article.Title + ", " + article.Authors[0].Name);
                    }
                    Console.WriteLine("-------");
                }
                Console.WriteLine("============================================================");
            }

            
            Console.WriteLine("Finished, press any key to exit");
            Console.Read();
        }
    }
}
