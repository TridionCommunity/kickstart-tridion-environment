using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Xml.Linq;
using Tridion.ContentManager;

namespace CreateAnEnvironmentForMe
{
    internal class WebsiteHelper
    {
        internal enum TargetLanguage
        {
            Jsp,
            Aspnet,
            REL
        }

        internal enum Role
        {
            Upload,
            PreviewWeb,
            PreviewWebService,
            WebService
        }


        internal void CopyFilesForWebsite(string websiteRoot, string configurationFile, Role role, TargetLanguage language)
        {
            if (!File.Exists(configurationFile))
            {
                Console.WriteLine("Could not find configuration file " + configurationFile);
            }
            // Find corresponding webapp
            // Get Database driver

            string theRole = null;
            string theLanguage = null;

            switch (role)
            {
                case Role.PreviewWebService:
                    theRole = "preview-webservice";
                    break;
                case Role.PreviewWeb:
                    theRole = "preview-web";
                    break;
                case Role.Upload:
                    theRole = "upload";
                    break;
                case Role.WebService:
                    theRole = "webservice";
                    break;
            }
            switch (language)
            {
                case TargetLanguage.Aspnet:
                    theLanguage = "Aspnet";
                    break;
                case TargetLanguage.Jsp:
                    theLanguage = "Jsp";
                    break;
            }

            XDocument config = XDocument.Load(configurationFile);
            string jdbc = config.Root.Element("Folder").Attribute("source").Value;
            jdbc += Path.DirectorySeparatorChar + config.Root.Element("Folder").Element("File").Value;

            string version = string.Empty;
            if (Configuration.ServerVersion == ServerVersion.Version7)
                version = "7.0.0";
            else if (Configuration.ServerVersion == ServerVersion.Version6)
                version = "6.1.0";
            else
            {
                Console.WriteLine("Unknown server version.");
                return;

            }


            XElement webapps = null;
            foreach (XElement node in config.Root.Elements("Webapps"))
            {
                if (node.Attribute("Version").Value.Equals(version))
                    webapps = node;
            }

            if (webapps == null)
            {
                Console.WriteLine("Could not find any webapp configured for version " + version);
                return;

            }

            string webapp = null;

            foreach (XElement node in webapps.Nodes())
            {
                if (node.Attribute("role").Value.Equals(theRole) && node.Attribute("language").Value.Equals(theLanguage))
                {
                    webapp = node.Element("File").Value;
                    break;
                }
            }
            if (webapp == null)
            {
                Console.WriteLine("Could not find webapp for role " + theRole + " and language " + theLanguage);
                return;
            }
            FastZip zip = new FastZip();
            zip.ExtractZip(webapp, websiteRoot, FastZip.Overwrite.Never, null, null, null, false);

            // TODO: Copy/Modify Configuration files for role
            string configFolder = null;
            string libFolder = null;
            if (language == TargetLanguage.Aspnet)
            {
                configFolder = websiteRoot + Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar +
                               "config";
                libFolder = websiteRoot + Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar + "lib";
            }
            else
            {
                configFolder = websiteRoot + Path.DirectorySeparatorChar + "WEB-INF" + Path.DirectorySeparatorChar +
                               "classes";
                libFolder = websiteRoot + Path.DirectorySeparatorChar + "WEB-INF" + Path.DirectorySeparatorChar + "lib";
            }
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

            if (!File.Exists(libFolder + Path.DirectorySeparatorChar + Path.GetFileName(jdbc)))
                File.Copy(jdbc, libFolder + Path.DirectorySeparatorChar + Path.GetFileName(jdbc));


            // Set permissions if web site
            if (role == Role.PreviewWeb)
            {
                DirectoryInfo info = new DirectoryInfo(websiteRoot);
                DirectorySecurity security = info.GetAccessControl();
                security.AddAccessRule(new FileSystemAccessRule("Network Service", FileSystemRights.Write | FileSystemRights.Read | FileSystemRights.ListDirectory | FileSystemRights.ReadAndExecute | FileSystemRights.Modify | FileSystemRights.CreateFiles | FileSystemRights.CreateDirectories | FileSystemRights.WriteAttributes | FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.InheritOnly, AccessControlType.Allow));
                info.SetAccessControl(security);
            }

        }

        private void WriteLogBack(string filePath, string name)
        {
            string logbackContent = File.ReadAllText("ConfigSamples\\logback.xml");
            logbackContent = logbackContent.Replace("##ROLE##", name);
            XDocument logBack = XDocument.Parse(logbackContent);
            logBack.Save(filePath);
        }

        private void WriteConfigFile(string source, string target, string name = null, XElement site = null)
        {
            string content = File.ReadAllText(source);
            XElement storage = null;
            string port = null;
            if (site != null)
            {
                storage = site.Element("Storage");
                port = site.Element("Port").Value;
            }
            if (storage != null)
            {
                content = content.Replace("##DATABASE_NAME##", storage.Element("Name").Value);
                content = content.Replace("##USER_NAME##", storage.Element("UserName").Value);
                content = content.Replace("##PASSWORD##", storage.Element("Password").Value);
                if (storage.Element("SessionPreview") != null)
                {
                    content = content.Replace("##SESSION_PREVIEW_DATABASE_NAME##",
                                              storage.Element("SessionPreview").Element("Name").Value);
                    content = content.Replace("##SESSION_PREVIEW_USER_NAME##",
                                              storage.Element("SessionPreview").Element("UserName").Value);
                    content = content.Replace("##SESSION_PREVIEW_PASSWORD##",
                                              storage.Element("SessionPreview").Element("Password").Value);
                }
                if (storage.Element("RootPath") != null)
                    content = content.Replace("##ROOT_PATH##", storage.Element("RootPath").Value);
            }

            if (name != null) content = content.Replace("##NAME##", name);
            if (Path.GetFileName(target).Equals("cd_dynamic_conf.xml"))
            {
                if (port != null) content = content.Replace("##PORT##", port);
                content = content.Replace("##WEBSITEPUBLICATIONID##",
                                          Convert.ToString(new TcmUri(Configuration.WebsitePublicationId).ItemId));
            }

            if (Path.GetFileName(target).Equals("cd_ambient_conf.xml"))
            {
                if (Configuration.ServerVersion == ServerVersion.Version7)
                    content = content.Replace("##CLAIMSTORE##",
                                              "com.tridion.preview.web.ambient.PreviewClaimStoreProvider");
                else
                    content = content.Replace("##CLAIMSTORE##",
                                              "com.tridion.siteedit.preview.PreviewClaimStoreProvider");
            }

            string version = string.Empty;
            if (Configuration.ServerVersion == ServerVersion.Version6)
            {
                content = content.Replace("##VERSION##", "6.1");
            }
            else if (Configuration.ServerVersion == ServerVersion.Version7)
                content = content.Replace("##VERSION##", "7.0");
            else
            {
                Console.WriteLine("Unknown Tridion server version. You may have to fix the configuration file version by hand.");
            }

            XDocument doc = XDocument.Parse(content);
            doc.Save(target);
        }

        internal void CreateConfigFiles(string rootFolder, string role, string name, XElement site, TargetLanguage language)
        {
            string configPath = string.Empty;
            if (language == TargetLanguage.Aspnet)
                configPath = rootFolder + "\\bin\\config";
            else
                configPath = rootFolder + "\\WEB-INF\\classes";

            // Write Logback
            WriteLogBack(configPath + Path.DirectorySeparatorChar + "logback.xml", name);

            List<string> filesToCopy = new List<string>();
            if (Directory.Exists("ConfigSamples\\" + role))
            {
                filesToCopy.AddRange(Directory.GetFiles("ConfigSamples\\" + role));
            }

            foreach (string file in filesToCopy)
            {
                string filename = Path.GetFileName(file);
                WriteConfigFile(file, configPath + Path.DirectorySeparatorChar + filename, name, site);
            }

            /*
            switch (role)
            {
                case "upload":
                    WriteConfigFile("ConfigSamples\\cd_deployer_conf.xml", configPath + Path.DirectorySeparatorChar + "cd_deployer_conf.xml", name);
                    WriteConfigFile("ConfigSamples\\cd_storage_conf-deployer.xml", configPath + Path.DirectorySeparatorChar + "cd_storage_conf.xml", null, storage);
                    break;
                case "preview":
                    WriteConfigFile("ConfigSamples\\cd_storage_conf-preview.xml", configPath + Path.DirectorySeparatorChar + "cd_storage_conf.xml", null, storage);
                    WriteConfigFile("ConfigSamples\\cd_ambient_conf-preview.xml", configPath + Path.DirectorySeparatorChar + "cd_ambient_conf.xml");
                    WriteConfigFile("ConfigSamples\\cd_dynamic_conf-preview.xml", configPath + Path.DirectorySeparatorChar + "cd_dynamic_conf.xml");
                    WriteConfigFile("ConfigSamples\\cd_link_conf.xml", configPath + Path.DirectorySeparatorChar + "cd_link_conf.xml");
                    WriteConfigFile("ConfigSamples\\cd_wai_conf.xml", configPath + Path.DirectorySeparatorChar + "cd_wai_conf.xml");
                    break;
            }*/

        }

        internal void CreateWebsite(string root, string name, int port)
        {
            Console.WriteLine("Creating Site & Application pool for " + name);
            ServerManager iis = new ServerManager();
            if (iis.Sites[name] == null)
            {
                ApplicationPool pool = iis.ApplicationPools.Add(name);
                pool.AutoStart = true;
                pool.Enable32BitAppOnWin64 = false;
                pool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                pool.ProcessModel.UserName = "Network Service";
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;
                Site site = iis.Sites.Add(name, root, port);
                site.Applications[0].VirtualDirectories[0].PhysicalPath = root;
                site.Applications[0].ApplicationPoolName = name;

                iis.CommitChanges();
            }
        }
    }
}
