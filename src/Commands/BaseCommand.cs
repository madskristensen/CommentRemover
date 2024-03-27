using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace CommentRemover
{
    internal abstract class BaseCommand<T> where T : BaseCommand<T>, new()
    {
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            Instance = new T
            {
                DTE = await package.GetServiceAsync(typeof(DTE)) as DTE2,
                CommandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
            };

            Assumes.Present(Instance.CommandService);
            Instance.SetupCommands();
        }

        protected abstract void SetupCommands();

        protected void RegisterCommand(Guid commandGuid, int commandId)
        {
            var id = new CommandID(commandGuid, commandId);
            var command = new OleMenuCommand(Callback, id);
            CommandService.AddCommand(command);
        }

        protected DTE2 DTE { get; private set; }

        private OleMenuCommandService CommandService { get; set; }

        public static T Instance { get; protected set; }

        private void Callback(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            Execute(button);
        }

        protected abstract void Execute(OleMenuCommand button);

        protected static IEnumerable<IMappingSpan> GetClassificationSpans(IWpfTextView view, string classificationName)
        {
            if (view == null)
            {
                return Enumerable.Empty<IMappingSpan>();
            }

            Microsoft.VisualStudio.ComponentModelHost.IComponentModel componentModel = ProjectHelpers.GetComponentModel();
            IBufferTagAggregatorFactoryService service = componentModel.GetService<IBufferTagAggregatorFactoryService>();
            ITagAggregator<IClassificationTag> classifier = service.CreateTagAggregator<IClassificationTag>(view.TextBuffer);
            SnapshotSpan snapshot = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, 0, view.TextBuffer.CurrentSnapshot.Length);

            IEnumerable<IMappingSpan> mappingSpans =
                from s in classifier.GetTags(snapshot).Reverse()
                where s.Tag.ClassificationType.Classification.IndexOf(classificationName, StringComparison.OrdinalIgnoreCase) > -1
                select s.Span;
            if (mappingSpans.Any())
            {
                return mappingSpans;
            }

            IViewTagAggregatorFactoryService serviceViewTag = componentModel.GetService<IViewTagAggregatorFactoryService>();
            ITagAggregator<IClassificationTag> classifierViewTag = serviceViewTag.CreateTagAggregator<IClassificationTag>(view);
            return from s in classifierViewTag.GetTags(snapshot).Reverse()
                   where s.Tag.ClassificationType.Classification.IndexOf(classificationName, StringComparison.OrdinalIgnoreCase) > -1
                   select s.Span;
        }

        protected static IEnumerable<IMappingSpan> GetSelectionClassificationSpans(IWpfTextView view, string classificationName)
        {
            if (view == null)
            {
                return Enumerable.Empty<IMappingSpan>();
            }

            Microsoft.VisualStudio.ComponentModelHost.IComponentModel componentModel = ProjectHelpers.GetComponentModel();
            IBufferTagAggregatorFactoryService service = componentModel.GetService<IBufferTagAggregatorFactoryService>();
            ITagAggregator<IClassificationTag> classifier = service.CreateTagAggregator<IClassificationTag>(view.TextBuffer);
            IEnumerable<IMappingSpan> mappingSpans =
                from s in classifier.GetTags(view.Selection.SelectedSpans).Reverse()
                where s.Tag.ClassificationType.Classification.IndexOf(classificationName, StringComparison.OrdinalIgnoreCase) > -1
                select s.Span;
            if (mappingSpans.Any())
            {
                return mappingSpans;
            }


            IViewTagAggregatorFactoryService serviceViewTag = componentModel.GetService<IViewTagAggregatorFactoryService>();
            ITagAggregator<IClassificationTag> classifierViewTag = serviceViewTag.CreateTagAggregator<IClassificationTag>(view);
            return from s in classifierViewTag.GetTags(view.Selection.SelectedSpans).Reverse()
                   where s.Tag.ClassificationType.Classification.IndexOf(classificationName, StringComparison.OrdinalIgnoreCase) > -1
                   select s.Span;
        }

        protected static bool IsLineEmpty(ITextSnapshotLine line)
        {
            var text = line.GetText().Trim();

            return (string.IsNullOrWhiteSpace(text)
                   || text == "<!--"
                   || text == "-->"
                   || text == "<%%>"
                   || text == "<%"
                   || text == "%>"
                   || Regex.IsMatch(text, @"<!--(\s+)?-->"));
        }

        protected static bool IsXmlDocComment(ITextSnapshotLine line)
        {
            var text = line.GetText().Trim();
            Microsoft.VisualStudio.Utilities.IContentType contentType = line.Snapshot.TextBuffer.ContentType;

            if (contentType.IsOfType("CSharp") && text.StartsWith("///"))
            {
                return true;
            }

            if (contentType.IsOfType("FSharp") && text.StartsWith("///"))
            {
                return true;
            }

            if (contentType.IsOfType("Basic") && text.StartsWith("'''"))
            {
                return true;
            }

            return false;
        }
    }
}
