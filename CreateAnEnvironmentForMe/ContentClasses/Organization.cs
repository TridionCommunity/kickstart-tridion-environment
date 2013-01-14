using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tridion.ContentManager.CoreService.Client;

namespace ContentClasses
{
   public class Organization : ContentItem
    {
       public Organization(ComponentData content, SessionAwareCoreServiceClient client) : base(content, client)
       {
       }
    }
}
