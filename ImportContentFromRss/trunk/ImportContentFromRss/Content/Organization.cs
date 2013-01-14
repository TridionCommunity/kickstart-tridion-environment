using Tridion.ContentManager.CoreService.Client;

namespace ImportContentFromRss.Content
{
   public class Organization : ContentItem
    {
       public Organization(ComponentData content, SessionAwareCoreServiceClient client) : base(content, client)
       {
       }
    }
}
