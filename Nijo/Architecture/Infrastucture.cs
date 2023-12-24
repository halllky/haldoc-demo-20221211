using Nijo.Core;
using Nijo.Util.DotnetEx;
using Nijo.Architecture.WebServer;
using static Nijo.Util.CodeGenerating.TemplateTextHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nijo.Util.CodeGenerating;
using Nijo.Features.Debugging;
using Nijo.Features.Logging;
using Nijo.Architecture.WebClient;

namespace Nijo.Architecture {
    public sealed class Infrastucture : NijoFeatureBaseNonAggregate {

        // DefaultConfigure
        public List<Func<string, string>> ConfigureServices { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureServicesWhenWebServer { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureServicesWhenBatchProcess { get; } = new List<Func<string, string>>();
        public List<Func<string, string>> ConfigureWebApp { get; } = new List<Func<string, string>>();

        // react
        public List<IReactPage> ReactPages { get; } = new List<IReactPage>();

        internal readonly Dictionary<GraphNode<Aggregate>, ByAggregate> _itemsByAggregate = new();
        public sealed class ByAggregate {
            // DbContext
            public bool HasDbSet { get; set; }
            public List<Func<string, string>> OnModelCreating { get; } = new();

            // AggregateRenderer
            public List<string> ControllerActions { get; } = new();
            public List<string> AppServiceMethods { get; } = new();
            public List<string> DataClassDeclaring { get; } = new();

            // react
            public List<string> TypeScriptDataTypes { get; } = new List<string>();
        }


        public void Aggregate(GraphNode<Aggregate> aggregate, Action<ByAggregate> fn) {
            ByAggregate? item;
            if (!_itemsByAggregate.TryGetValue(aggregate, out item)) {
                item = new ByAggregate();
                _itemsByAggregate.Add(aggregate, item);
            }
            fn(item);
        }

        public override void GenerateCode(ICodeRenderingContext context) {
            context.EditWebApiDirectory(genDir => {
                genDir.Generate(Configure.Render(context, this));
                genDir.Generate(EnumDefs.Render(context));
                genDir.Generate(new ApplicationService().Render(context));

                genDir.Directory("Util", utilDir => {
                    utilDir.Generate(RuntimeSettings.Render(context));
                    utilDir.Generate(Utility.DotnetExtensions.Render(context));
                    utilDir.Generate(Utility.FromTo.Render(context));
                    utilDir.Generate(Utility.UtilityClass.RenderJsonConversionMethods(context));
                    utilDir.Generate(HttpResponseExceptionFilter.Render(context));
                    utilDir.Generate(DefaultLogger.Render(context));
                });
                genDir.Directory("Web", controllerDir => {
                    controllerDir.Generate(MultiView.RenderCSharpSearchConditionBaseClass(context));
                    controllerDir.Generate(DebuggerController.Render(context));
                });
                genDir.Directory("EntityFramework", efDir => {
                    efDir.Generate(new DbContextClass(context.Config).RenderDeclaring(context, this));
                });
            });

            foreach (var item in _itemsByAggregate) {
                RenderWebapiAggregateFile(context, item.Key, item.Value);
            }

            context.EditReactDirectory(reactDir => {
                var reactProjectTemplate = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ApplicationTemplates", "REACT_AND_WEBAPI", "react");

                reactDir.CopyFrom(Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "nijo.css"));
                reactDir.Generate(new SourceFile {
                    FileName = "types.ts",
                    RenderContent = () => $$"""
                        import { UUID } from 'uuidjs'

                        {{_itemsByAggregate.SelectTextTemplate(item => $$"""
                        // ------------------ {{item.Key.Item.DisplayName}} ------------------
                        {{item.Value.TypeScriptDataTypes.SelectTextTemplate(source => $$"""
                        {{source}}

                        """)}}

                        """)}}
                        """,
                });
                reactDir.Generate(new SourceFile {
                    FileName = "index.tsx",
                    RenderContent = () => $$"""
                        import './nijo.css';
                        import 'ag-grid-community/styles/ag-grid.css';
                        import 'ag-grid-community/styles/ag-theme-alpine.css';

                        {{ReactPages.SelectTextTemplate(page => $$"""
                        import {{page.ComponentPhysicalName}} from './{{REACT_PAGE_DIR}}/{{page.DirNameInPageDir}}/{{Path.GetFileNameWithoutExtension(page.GetSourceFile().FileName)}}'
                        """)}}

                        export const THIS_APPLICATION_NAME = '{{context.Schema.ApplicationName}}' as const

                        export const routes: { url: string, el: JSX.Element }[] = [
                        {{ReactPages.SelectTextTemplate(page => $$"""
                          { url: '{{page.Url}}', el: <{{page.ComponentPhysicalName}} /> },
                        """)}}
                        ]
                        export const menuItems: { url: string, text: string }[] = [
                        {{ReactPages.Where(p => p.ShowMenu).SelectTextTemplate(page => $$"""
                          { url: '{{page.Url}}', text: '{{page.LabelInMenu}}' },
                        """)}}
                        ]
                        """,
                });

                reactDir.Directory("application", reactApplicationDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "application");
                    foreach (var file in Directory.GetFiles(source)) reactApplicationDir.CopyFrom(file);
                });
                reactDir.Directory("decoration", decorationDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "decoration");
                    foreach (var file in Directory.GetFiles(source)) decorationDir.CopyFrom(file);
                });
                reactDir.Directory("layout", layoutDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "layout");
                    foreach (var file in Directory.GetFiles(source)) layoutDir.CopyFrom(file);
                });
                reactDir.Directory("user-input", userInputDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "user-input");
                    foreach (var file in Directory.GetFiles(source)) userInputDir.CopyFrom(file);

                    // TODO: どの集約がコンボボックスを作るのかをNijoFeatureBaseに主導権握らせたい
                    userInputDir.Generate(ComboBox.RenderDeclaringFile(context));
                });
                reactDir.Directory("util", reactUtilDir => {
                    var source = Path.Combine(reactProjectTemplate, "src", "__autoGenerated", "util");
                    foreach (var file in Directory.GetFiles(source)) reactUtilDir.CopyFrom(file);
                    reactUtilDir.Generate(DummyDataGenerator.Render(context));
                });
                reactDir.Directory(REACT_PAGE_DIR, pageDir => {
                    foreach (var group in ReactPages.GroupBy(p => p.DirNameInPageDir)) {
                        pageDir.Directory(group.Key, aggregatePageDir => {
                            foreach (var page in group) {
                                aggregatePageDir.Generate(page.GetSourceFile());
                            }
                        });
                    }
                });
            });
        }

        internal const string REACT_PAGE_DIR = "pages";

        public interface IReactPage {
            string Url { get; }
            string DirNameInPageDir { get; }
            string ComponentPhysicalName { get; }
            bool ShowMenu { get; }
            string? LabelInMenu { get; }
            SourceFile GetSourceFile();
        }

        private void RenderWebapiAggregateFile(ICodeRenderingContext context, GraphNode<Aggregate> aggregate, ByAggregate byAggregate) {

            context.EditWebApiDirectory(dir => {
                var appSrv = new ApplicationService();
                var controller = new WebClient.Controller(aggregate.Item);

                dir.Generate(new SourceFile {
                    FileName = $"{aggregate.Item.DisplayName.ToFileNameSafe()}.cs",
                    RenderContent = () => $$"""
                        namespace {{context.Config.RootNamespace}} {
                            using System;
                            using System.Collections;
                            using System.Collections.Generic;
                            using System.ComponentModel;
                            using System.ComponentModel.DataAnnotations;
                            using System.Linq;
                            using Microsoft.AspNetCore.Mvc;
                            using Microsoft.EntityFrameworkCore;
                            using Microsoft.EntityFrameworkCore.Infrastructure;
                            using {{context.Config.EntityNamespace}};

                            [ApiController]
                            [Route("{{WebClient.Controller.SUBDOMAIN}}/[controller]")]
                            public partial class {{controller.ClassName}} : ControllerBase {
                                public {{controller.ClassName}}(ILogger<{{controller.ClassName}}> logger, {{appSrv.ClassName}} applicationService) {
                                    _logger = logger;
                                    _applicationService = applicationService;
                                }
                                protected readonly ILogger<{{controller.ClassName}}> _logger;
                                protected readonly {{appSrv.ClassName}} _applicationService;

                                {{WithIndent(byAggregate.ControllerActions, "        ")}}
                            }


                            partial class {{appSrv.ClassName}} {
                                {{WithIndent(byAggregate.AppServiceMethods, "        ")}}
                            }


                        #region データ構造クラス
                            {{WithIndent(byAggregate.DataClassDeclaring, "    ")}}
                        #endregion データ構造クラス
                        }

                        namespace {{context.Config.DbContextNamespace}} {
                            using {{context.Config.RootNamespace}};
                            using Microsoft.EntityFrameworkCore;

                            partial class {{context.Config.DbContextName}} {
                        {{If(byAggregate.HasDbSet, () => aggregate.EnumerateThisAndDescendants().SelectTextTemplate(agg => $$"""
                                public virtual DbSet<{{agg.Item.EFCoreEntityClassName}}> {{agg.Item.DbSetName}} { get; set; }
                        """))}}

                        {{If(byAggregate.OnModelCreating.Any(), () => $$"""
                                private void OnModelCreating_{{aggregate.Item.ClassName}}(ModelBuilder modelBuilder) {
                                    {{WithIndent(byAggregate.OnModelCreating.SelectTextTemplate(fn => fn.Invoke("modelBuilder")), "            ")}}
                                }
                        """)}}
                            }
                        }
                        """,
                });
            });
        }
    }
}
