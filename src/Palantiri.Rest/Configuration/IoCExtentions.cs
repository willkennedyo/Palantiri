namespace Palantiri.Rest.Configuration
{
    public static class IoCExtentions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration config)
        {         
            return services;
        }
    }
}
