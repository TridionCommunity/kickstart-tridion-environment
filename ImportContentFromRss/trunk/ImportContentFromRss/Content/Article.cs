using System;
using System.Collections.Generic;
using System.Xml;
using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;
using ItemType = Tridion.ContentManager.CoreService.Client.ItemType;

namespace ImportContentFromRss.Content
{
    public class Article : ContentItem
    {
        public Article(SessionAwareCoreServiceClient client, TcmUri location)
            : base(client)
        {
            ComponentData component = (ComponentData)Client.GetDefaultData(ItemType.Component, location, ReadOptions);
            component.Schema = new LinkToSchemaData { IdRef = ContentManager.ResolveUrl(Constants.ArticleSchemaUrl) };
            Content = component;
        }

        public Article(ComponentData component, SessionAwareCoreServiceClient client) : base(component, client)
        {
            
        }

        public Article(TcmUri contentItemUri, SessionAwareCoreServiceClient client)
            : base(contentItemUri, client)
        {
        }

        public string DisplayTitle
        {
            get
            {
                return Fields["ArticleTitle"].Value;
            }
            set
            {
                Fields["ArticleTitle"].Value = value;
            }
        }

        public string Summary
        {
            get
            {

                return Fields["ArticleSummary"].Value;
            }
            set
            {
                Fields["ArticleSummary"].Value = value;
            }
        }

        public string Body
        {
            get
            {
                return Fields["ArticleBody"].Value;
            }
            set
            {
                Fields["ArticleBody"].Value = value;
            }
        }

        public DateTime Date
        {
            get
            {

                return Convert.ToDateTime(Fields["Date"].Value);
            }
            set { Fields["Date"].Value = XmlConvert.ToString(value, XmlDateTimeSerializationMode.Unspecified); }
        }

        public List<Person> Authors
        {
            get
            {
                List<Person> result = new List<Person>();
                foreach (var id in Fields["Author"].Values)
                {
                    result.Add(new Person((ComponentData)Client.Read(id, ReadOptions), Client));
                }
                return result;
            }
            set
            {
                foreach (var id in Fields["Author"].Values)
                {
                    // No .Clear?
                    Fields["Author"].RemoveValue(id);
                }
                foreach (var person in value)
                {
                    Fields["Author"].AddValue(person.Id);
                }
            }
        }

        public string Url
        {
            get
            {
                return Fields["ArticleUrl"].Value;
            }
            set
            {
                Fields["ArticleUrl"].Value = value;
            }
        }

        public Source Source
        {
            get
            {
                if (Fields["ArticleSource"].Values.Count > 0)
                    return new Source((ComponentData)Client.Read(Fields["ArticleSource"].Value, ReadOptions), Client);
                return null;
            }
            set
            {
                Fields["ArticleSource"].Value = value.Id;
            }
        }

        public List<string> Categories
        {
            get
            {
                List<string> result = new List<string>();
                foreach (var keyword in Fields["ContentCategory"].Values)
                {
                    result.Add(keyword);
                }
                return result;
            }
            set
            {
                //KeywordField contentCategory = (KeywordField)Fields["ContentCategory"];
                //Category category = (Category)Session.GetObject(Constants.ContentCategoryUrl);
                /*
                OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Keyword } };

                XElement existingKeywords = Client.GetListXml(Constants.ContentCategoryUrl, filter);

                foreach (string cat in value)
                {
                    if (string.IsNullOrEmpty(cat)) continue;
                    string catName = cat.ToLower();
                    //Keyword keyword = null;

                    foreach (XElement node in existingKeywords.Nodes())
                    {
                        if (node.Attribute("Title").Value.Equals(catName))
                        {
                            keyword = (Keyword)Session.GetObject(node.Attributes["ID"].Value);
                            break;
                        }
                    }
                    if (keyword == null)
                    {
                        keyword = new Keyword(Session, category.Id) { Title = catName };
                        keyword.Save();
                    }
                    contentCategory.Values.Add(keyword);
                }
                 * */

                List<string> keywords = new List<string>();
                foreach (var keyword in value)
                {
                    // Prevent duplicates
                    string clean = keyword.ToLower().Trim();
                    if(!keywords.Contains(clean)) keywords.Add(clean);
                }

                ContentManager.CreateKeywords(keywords);
                

                foreach (var k in Fields["ContentCategory"].Values)
                {
                    Fields["ContentCategory"].RemoveValue(k);
                }
                foreach (var k in keywords)
                {
                    Fields["ContentCategory"].AddValue(k);
                }
            }
        }

        public string StackOverFlowQuestionId
        {
            get
            {
                if (!Source.IsStackOverflow) return null;
                return Fields["StackOverFlowQuestionId"].Value;
            }
            set
            {
                Fields["StackOverFlowQuestionId"].Value = value;
            }
        }

        public string StackOverflowQuestionRank
        {
            get
            {
                if (!Source.IsStackOverflow) return null;
                return Fields["StackOverflowQuestionRank"].Value;
            }
            set
            {
                Fields["StackOverflowQuestionRank"].Value = value;
            }
        }
    }
}
