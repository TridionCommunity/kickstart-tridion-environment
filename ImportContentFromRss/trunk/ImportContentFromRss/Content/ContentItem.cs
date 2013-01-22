using System;
using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;

namespace ImportContentFromRss.Content
{
    public abstract class ContentItem
    {
        public ComponentData Content;
        public readonly SessionAwareCoreServiceClient Client;
        public readonly ReadOptions ReadOptions;
        public ContentManager ContentManager;

        private Fields _fields;

        protected ContentItem(ComponentData content, SessionAwareCoreServiceClient client)
        {
            Content = content;
            Client = client;
            ReadOptions = new ReadOptions();
            ContentManager = new ContentManager(Client);
        }

        protected ContentItem(TcmUri itemId, SessionAwareCoreServiceClient client)
        {
            ReadOptions = new ReadOptions();
            Client = client;
            Content = (ComponentData) client.Read(itemId, ReadOptions);
            ContentManager = new ContentManager(Client);
        }

        protected ContentItem(SessionAwareCoreServiceClient client)
        {
            Client = client;
            ReadOptions = new ReadOptions();
            ContentManager = new ContentManager(Client);
        }
        
        
        public Fields Fields
        {
            get
            {
                if (_fields == null)
                {
                    var schemaData = Client.ReadSchemaFields(Content.Schema.IdRef, false, ReadOptions);
                    
                    if (Content.Id != TcmUri.UriNull)
                    {
                        var component = (ComponentData)Client.Read(Content.Id, ReadOptions);
                        _fields = Fields.ForContentOf(schemaData, component);
                    }
                    else
                    {
                        _fields = Fields.ForContentOf(schemaData);
                    }
                }
                return _fields;
            }
        }

        public string Title
        {
            get { return Content.Title; }
            set { Content.Title = value; }
        }

        public TcmUri Id
        {
            get{return new TcmUri(Content.Id);}
        }

        public void Save(bool checkOutIfNeeded = false)
        {
            if (checkOutIfNeeded)
            {
                if (!Content.IsEditable.GetValueOrDefault())
                {
                    Client.CheckOut(Content.Id, true, null);
                }
            }
            if (string.IsNullOrEmpty(Content.Title))
                Content.Title = "No title specified!";
            // Item titles cannot contain backslashes :)
            if (Content.Title.Contains("\\")) Content.Title = Content.Title.Replace("\\", "/");
            Content.Content = _fields.ToString();
            TcmUri contentId = new TcmUri(Content.Id);
            if(!contentId.IsVersionless)
            {
                contentId = new TcmUri(contentId.ItemId, contentId.ItemType, contentId.PublicationId);
                Content.Id = contentId.ToString();
            }
            try
            {
                Content = (ComponentData)Client.Save(Content, ReadOptions);
                Client.CheckIn(Content.Id, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ooops, something went wrong saving component " + Content.Title);
                Console.WriteLine(ex.Message);
            }
            
        }
    }
}
