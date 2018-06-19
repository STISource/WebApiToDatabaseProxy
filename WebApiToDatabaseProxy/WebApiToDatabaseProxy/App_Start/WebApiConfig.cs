using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Web.Http;
using Unity;
using Unity.Lifetime;
using WebApiToDatabaseProxy.Database;
using WebApiToDatabaseProxy.Managers;

namespace WebApiToDatabaseProxy
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            //configure dependency injection
            var container = new UnityContainer();

            container.RegisterInstance<IDatabaseSession>(new DatabaseSession(
                                                                ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString,
                                                                con => new OdbcConnection(con)));
            container.RegisterType<ILexwareManager, LexwareManager>(new HierarchicalLifetimeManager());            

            config.DependencyResolver = new UnityDependencyResolver(container);

            // Web API data formatter configuration. Throw away json. When using Excel as client it can't process it properly.
            config.Formatters.Remove(config.Formatters.JsonFormatter);

            // Web API routes
            config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{action}/"                
            //);
        }
    }
}
