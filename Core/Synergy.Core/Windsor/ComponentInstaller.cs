using System;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy.Internal;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using JetBrains.Annotations;
using Synergy.Contracts;
using Synergy.Extensions;

namespace Synergy.Core.Windsor
{
    /// <summary>
    ///     Default Windsor installer. It searches for any component with any interface in a <see cref="Library" />
    ///     and registers the component under all interfaces it implements.
    /// </summary>
    public class ComponentInstaller : IWindsorInstaller
    {
        [CanBeNull]
        private Assembly assembly;

        [CanBeNull]
        private Type[] excludeInterfaces;

        public void Install([NotNull] IWindsorContainer container, [NotNull] IConfigurationStore store)
        {
            Fail.IfArgumentNull(container, nameof(container));

            Assembly assemblyToScan = this.assembly.OrFail(nameof(this.assembly));

            BasedOnDescriptor allClassesWithAnyInterface = Classes
                    .FromAssembly(assemblyToScan)
                    .Pick()
                    .If(this.ShouldRegisterComponent)
                    .WithService.Select(this.GetComponentInterfaces())
                ;

            container.Register(allClassesWithAnyInterface);
        }

        public void Init([NotNull] Library library)
        {
            Fail.IfArgumentNull(library, nameof(library));

            this.assembly = library
                .GetAssembly()
                .FailIfNull("{0}.{1}() returned null", library, nameof(Library.GetAssembly));

            this.excludeInterfaces = library
                .IgnoreInterfaces()
                .FailIfNull("{0}.{1}() returned null", library, nameof(Library.IgnoreInterfaces))
                .ToArray();
        }

        [Pure]
        private bool ShouldRegisterComponent([NotNull] Type componentType)
        {
            Fail.IfArgumentNull(componentType, nameof(componentType));

            Type[] interfacesToExclude = this.excludeInterfaces.OrFail(nameof(this.excludeInterfaces));

            Type[] interfaces = componentType
                .GetInterfaces()
                .Except(interfacesToExclude)
                .ToArray();

            return interfaces.IsNotEmpty();
        }

        [NotNull]
        private ServiceDescriptor.ServiceSelector GetComponentInterfaces()
        {
            Type[] interfacesToExclude = this.excludeInterfaces.OrFail(nameof(this.excludeInterfaces));

            return (componentType, basTypes) => componentType
                .GetAllInterfaces()
                .Except(interfacesToExclude);
        }
    }
}