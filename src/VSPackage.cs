using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using task = System.Threading.Tasks.Task;

namespace CommentRemover
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidPackageString)]
    public sealed class CommentRemoverPackage : AsyncPackage
    {
        protected override async task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await RemoveAllCommentsCommand.InitializeAsync(this);
            await RemoveAllSelectedComments.InitializeAsync(this);
            await RemoveRegionsCommand.InitializeAsync(this);
            await RemoveXmlDocComments.InitializeAsync(this);
            await RemoveAllExceptXmlDocComments.InitializeAsync(this);
            await RemoveTasksCommand.InitializeAsync(this);
            await RemoveAllExceptTaskComments.InitializeAsync(this);
        }
    }
}
