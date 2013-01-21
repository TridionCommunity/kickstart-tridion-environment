using System.Collections.Generic;
using Tridion.ContentManager.CoreService.Client;

namespace ImportContentFromRss.Content
{
    public class Person : ContentItem
    {
        public Person(ComponentData content, SessionAwareCoreServiceClient client) : base(content, client)
        {
        }

        public Person(SessionAwareCoreServiceClient client):base(client)
        {
            Content = (ComponentData)Client.GetDefaultData(ItemType.Component, Constants.PersonLocationUrl);
            Content.Schema = new LinkToSchemaData {IdRef = ContentManager.ResolveUrl(Constants.PersonSchemaUrl)};
        }

        public string Name
        {
            get
            {
                var name = Fields["PersonName"];
                return name.Value;
            }
            set
            {
                var name = Fields["PersonName"];
                Content.Title = value;
                name.Value = value;
            }
        }
        public string TwitterHandle
        {
            get
            {
                return Fields["PersonTwitterHandle"].Value;
            }
            set
            {
                Fields["PersonTwitterHandle"].Value = value;
            }
        }

        public string StackOverflowId
        {
            get { return Fields["StackOverflowId"].Value; }
            set { Fields["StackOverflowId"].Value = value; }
        }
        public string LinkedInProfile
        {
            get { return Fields["PersonLinkedInProfile"].Value; }
            set { Fields["PersonLinkedInProfile"].Value = value; }
        }

        public List<string> AlternateNames
        {
            get
            {
                List<string> result = new List<string>();
                foreach (var value in Fields["AlternateNames"].Values)
                {
                    result.Add(value);
                }
                return result;
            }
        }
    }
}
