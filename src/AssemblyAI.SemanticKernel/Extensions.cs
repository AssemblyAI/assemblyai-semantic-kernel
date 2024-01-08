using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace AssemblyAI.SemanticKernel
{
    public static class Extensions
    {
        /// <summary>
        /// Configure the AssemblyAI plugins using the specified configuration section path.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IKernelBuilder AddAssemblyAIPlugin(
            this IKernelBuilder builder
        ) => AddAssemblyAIPlugin(builder, "AssemblyAI");

        /// <summary>
        /// Configure the AssemblyAI plugins using the specified configuration section path.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configSectionPath">The path of the configuration section to bind options to</param>
        /// <returns></returns>
        public static IKernelBuilder AddAssemblyAIPlugin(
            this IKernelBuilder builder,
            string configSectionPath
        )
        {
            var services = builder.Services;
            var optionsBuilder = services.AddOptions<AssemblyAIPluginOptions>();
            optionsBuilder.BindConfiguration(configSectionPath);
            ValidateOptions(optionsBuilder);
            AddPlugin(builder);
            return builder;
        }

        /// <summary>
        /// Configure the AssemblyAI plugins using the specified configuration section path.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuration">The configuration to bind options to</param>
        /// <returns></returns>
        public static IKernelBuilder AddAssemblyAIPlugin(
            this IKernelBuilder builder,
            IConfiguration configuration
        )
        {
            var services = builder.Services;
            var optionsBuilder = services.AddOptions<AssemblyAIPluginOptions>();
            optionsBuilder.Bind(configuration);
            ValidateOptions(optionsBuilder);
            AddPlugin(builder);
            return builder;
        }

        /// <summary>
        /// Configure the AssemblyAI plugins using the specified options.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Options to configure plugin with</param>
        /// <returns></returns>
        public static IKernelBuilder AddAssemblyAIPlugin(
            this IKernelBuilder builder,
            AssemblyAIPluginOptions options
        )
        {
            var services = builder.Services;
            var optionsBuilder = services.AddOptions<AssemblyAIPluginOptions>();
            optionsBuilder.Configure(optionsToConfigure =>
            {
                optionsToConfigure.ApiKey = options.ApiKey;
                optionsToConfigure.AllowFileSystemAccess = options.AllowFileSystemAccess;
            });
            ValidateOptions(optionsBuilder);
            AddPlugin(builder);
            return builder;
        }

        /// <summary>
        /// Configure the AssemblyAI plugins using the specified options.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <returns></returns>
        public static IKernelBuilder AddAssemblyAIPlugin(
            this IKernelBuilder builder,
            Action<AssemblyAIPluginOptions> configureOptions
        )
        {
            var services = builder.Services;
            var optionsBuilder = services.AddOptions<AssemblyAIPluginOptions>();
            optionsBuilder.Configure(configureOptions);
            ValidateOptions(optionsBuilder);
            AddPlugin(builder);
            return builder;
        }

        /// <summary>
        /// Configure the AssemblyAI plugins using the specified options.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <returns></returns>
        public static IKernelBuilder AddAssemblyAIPlugin(
            this IKernelBuilder builder,
            Action<IServiceProvider, AssemblyAIPluginOptions> configureOptions
        )
        {
            var services = builder.Services;
            var optionsBuilder = services.AddOptions<AssemblyAIPluginOptions>();
            optionsBuilder.Configure<IServiceProvider>((options, provider) => configureOptions(provider, options));
            ValidateOptions(optionsBuilder);
            AddPlugin(builder);
            return builder;
        }

        private static void ValidateOptions(OptionsBuilder<AssemblyAIPluginOptions> optionsBuilder)
        {
            optionsBuilder.Validate(
                options => !string.IsNullOrEmpty(options.ApiKey),
                "AssemblyAI:ApiKey must be configured."
            );
        }

        private static void AddPlugin(IKernelBuilder builder)
        {
            using (var sp = builder.Services.BuildServiceProvider())
            {
                var config = sp.GetRequiredService<IOptions<AssemblyAIPluginOptions>>().Value;
                var pluginName = string.IsNullOrEmpty(config.PluginName) ? null : config.PluginName;
                builder.Plugins.AddFromType<AssemblyAIPlugin>(pluginName);
            }
        }
    }
}