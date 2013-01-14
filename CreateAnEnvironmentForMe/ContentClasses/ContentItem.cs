using Tridion.ContentManager;
using Tridion.ContentManager.CoreService.Client;

namespace ContentClasses
{
    public abstract class ContentItem
    {
        public ComponentData Content;
        public readonly SessionAwareCoreServiceClient Client;
        public readonly ReadOptions ReadOptions;

        private Fields _fields;

        protected ContentItem(ComponentData content, SessionAwareCoreServiceClient client)
        {
            Content = content;
            Client = client;
            ReadOptions = new ReadOptions();
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
            Content.Content = _fields.ToString();
            Content = (ComponentData)Client.Save(Content, ReadOptions);
            Client.CheckIn(Content.Id, null);
        }
    }
}
